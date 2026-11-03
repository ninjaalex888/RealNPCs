# RealNPCs' local voice server (2026-11-03; Lex: "voiced thoughts and dialogue"). The mod asks it, the way it asks Ollama,
# to say a line in a person's own voice: the voice is cloned on this PC with Chatterbox from one of the game's own
# recordings for that person's model (the player's own OpenIV export of the voice banks; nothing of Rockstar's ships),
# and played here, mixed, with the volume and pan the mod keeps sending as the speaker moves.
#
#   tts-venv\Scripts\python.exe -I voice_server.py --root C:\mods\voice-test [--port 5005] [--min-free-gb 1.0]
#
# --root holds refs.json (pick_refs.py) and the banks\ folder it points into. Plain HTTP on 127.0.0.1 only:
#   GET  /health                -> {"ready":..,"model":..,"free_gb":..,"queued":..,"playing":..,"made":..,"held":..}
#   POST /say    {"id","model","sex","age","person","text","kind":"thought"|"said","volume","pan","wait"}
#                               -> {"ok":true} or {"held":"why"}   (never waits for the voice: the mod asks /status)
#   POST /place  {"id","volume","pan"}   the speaker moved
#   POST /stop   {"id"} or {}            one voice, or every voice
#   GET  /status?id=..          -> {"state":"queued|making|ready|playing|done|dropped|failed|held|unknown","make_ms":..,"seconds":..}
import argparse
import collections
import hashlib
import json
import os
import queue
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

import numpy as np

ARGS = None
MODEL = None
MODEL_NAME = "chatterbox"
SR = 24000
READY = False
LOAD_ERROR = None
LOCK = threading.Lock()
JOBS = {}                      # id -> job dict (state, times)
WORK = queue.Queue()
CONDS = collections.OrderedDict()   # reference clip -> Chatterbox conditionals (preparing them is most of a short line's cost)
MADE = collections.OrderedDict()    # (ref, kind, text) -> samples, so a line said again plays at once
COUNTS = collections.Counter()
REFS_BY_MODEL = {}
REFS_BY_KIND = {}


def log(*a):
    print(time.strftime("%H:%M:%S"), *a, flush=True)


# ---- which recording a person's voice is cloned from ----

def load_refs(root):
    path = os.path.join(root, "refs.json")
    with open(path, encoding="utf-8") as f:
        refs = json.load(f)
    for bank, e in refs.items():
        if not e.get("ref") or e.get("tier", 0) <= 0:
            continue
        clip = os.path.join(root, e["ref"])
        if not os.path.exists(clip):
            continue
        spanish = "spanish" in (e.get("notes") or "").lower()
        item = (bank, clip, e.get("tier", 4), spanish)
        REFS_BY_MODEL.setdefault((e.get("model") or "").upper(), []).append(item)
        REFS_BY_KIND.setdefault((e.get("kind") or "", e.get("age") or ""), []).append(item)
    for d in (REFS_BY_MODEL, REFS_BY_KIND):
        for k in d:
            d[k].sort()
    log("voices: %d models, %d banks with a reference clip" % (len(REFS_BY_MODEL), sum(len(v) for v in REFS_BY_MODEL.values())))


def stable(s):
    return int(hashlib.md5(s.encode("utf-8")).hexdigest()[:8], 16)


def pick_ref(model, sex, age, person):
    """The same person always gets the same voice: one of their model's banks, chosen by who they are; English first,
    the better tiers first. A model with no bank of its own borrows one of the same sex and age."""
    person = person or model or "?"
    parts = (model or "").upper().split("_")
    if len(parts) > 3 and parts[0] in ("A", "S", "U", "G", "CS") and parts[1] in ("M", "F") and parts[2] in ("M", "O", "Y"):
        sex = sex or ("male" if parts[1] == "M" else "female")
        age = {"M": "adult", "O": "old", "Y": "young"}[parts[2]] if not age else age
    cands = REFS_BY_MODEL.get((model or "").upper())
    how = "own"
    if not cands:
        how = "borrowed"
        cands = REFS_BY_KIND.get(((sex or "male").lower(), (age or "adult").lower())) or REFS_BY_KIND.get(("male", "adult"))
        cands = [c for c in cands if c[2] <= 2] or cands
    if not cands:
        return None, None, how
    english = [c for c in cands if not c[3]] or cands
    best = min(c[2] for c in english)
    pool = [c for c in english if c[2] <= max(best, 2)]
    c = pool[stable(person) % len(pool)]
    return c[0], c[1], how


# ---- making the voice ----

def load_model():
    global MODEL, SR, READY, LOAD_ERROR, MODEL_NAME
    try:
        import torch
        t = time.time()
        if ARGS.turbo:
            from chatterbox.tts_turbo import ChatterboxTurboTTS
            MODEL = ChatterboxTurboTTS.from_pretrained(device=ARGS.device)
            MODEL_NAME = "chatterbox-turbo"
        else:
            from chatterbox.tts import ChatterboxTTS
            MODEL = ChatterboxTTS.from_pretrained(device=ARGS.device)
        SR = MODEL.sr
        if SR != 24000:
            log("note: the model speaks at %d Hz, the mixer plays at 24000" % SR)
        log("model %s loaded in %.1f s on %s%s" % (MODEL_NAME, time.time() - t, ARGS.device,
            (", %.2f GB" % (torch.cuda.memory_allocated() / 1e9)) if ARGS.device == "cuda" else ""))
        warm = next(iter(REFS_BY_MODEL.values()), None)
        if warm:   # the first line said is slow (kernels, caches): say one nobody hears
            try:
                t = time.time()
                conds_for(warm[0][1], 0.5)
                with torch.inference_mode():
                    MODEL.generate("Warming up.") if ARGS.turbo else MODEL.generate("Warming up.", exaggeration=0.5, cfg_weight=0.5)
                log("warmed up in %.1f s" % (time.time() - t))
            except Exception as e:
                log("warm-up failed:", repr(e)[:200])
        READY = True
    except Exception as e:  # the mod sees "not ready" and stays with its own voices
        LOAD_ERROR = repr(e)
        log("model failed to load:", LOAD_ERROR)


def free_gb():
    if ARGS.device != "cuda":
        return 99.0
    try:
        import torch
        free, _total = torch.cuda.mem_get_info()
        return free / 1e9
    except Exception:
        return -1.0


def murmur(w):
    """A thought, not a speech: the top of the voice taken off (low-pass near 3.2 kHz) and softer."""
    try:
        from scipy.signal import lfilter
        a = np.exp(-2.0 * np.pi * 3200.0 / SR)
        return lfilter([1 - a], [1, -a], w).astype(np.float32) * ARGS.thought_gain
    except Exception:
        import torch
        import torchaudio.functional as F
        return (F.lowpass_biquad(torch.from_numpy(w).unsqueeze(0), SR, 3200.0).squeeze(0).numpy() * ARGS.thought_gain).astype(np.float32)


def conds_for(clip, exaggeration):
    c = CONDS.get(clip)
    if c is None:
        MODEL.prepare_conditionals(clip, exaggeration=exaggeration) if not ARGS.turbo else MODEL.prepare_conditionals(clip)
        c = MODEL.conds
        CONDS[clip] = c
        while len(CONDS) > 48:
            CONDS.popitem(last=False)
        COUNTS["prepared"] += 1
    else:
        CONDS.move_to_end(clip)
    MODEL.conds = c


def make(job):
    key = (job["clip"], job["kind"], job["text"])
    w = MADE.get(key)
    if w is not None:
        MADE.move_to_end(key)
        COUNTS["again"] += 1
        return w
    import torch
    ex = float(job.get("exaggeration") or 0.5)
    conds_for(job["clip"], ex)
    with torch.inference_mode():
        if ARGS.turbo:
            wav = MODEL.generate(job["text"])
        else:
            wav = MODEL.generate(job["text"], exaggeration=ex, cfg_weight=0.5)
    w = wav.detach().cpu().squeeze(0).numpy().astype(np.float32)
    peak = float(np.max(np.abs(w))) if len(w) else 0.0
    if peak > 0.98:
        w = w * (0.98 / peak)
    if job["kind"] == "thought":
        w = murmur(w)
    MADE[key] = w
    while len(MADE) > 64:
        MADE.popitem(last=False)
    return w


def worker():
    while True:
        job = WORK.get()
        if job is None:
            return
        with LOCK:
            if job["state"] != "queued":
                continue
            age = time.time() - job["asked"]
            if age > job["wait"]:
                job["state"] = "dropped"
                COUNTS["dropped"] += 1
                continue
            f = free_gb()
            if f < ARGS.min_free_gb:
                job["state"] = "held"
                job["why"] = "vram %.2f GB free" % f
                COUNTS["held"] += 1
                continue
            job["state"] = "making"
        t = time.time()
        try:
            w = make(job)
        except Exception as e:
            with LOCK:
                job["state"] = "failed"
                job["why"] = repr(e)[:200]
                COUNTS["failed"] += 1
            log("failed:", job["id"], repr(e)[:200])
            continue
        with LOCK:
            job["make_ms"] = int((time.time() - t) * 1000)
            job["seconds"] = round(len(w) / SR, 2)
            if job["state"] != "making":     # stopped while it was being made
                continue
            job["samples"] = w
            job["pos"] = 0
            job["state"] = "ready"       # the mixer plays one voice at a time, in the order they were asked
            job["ready_at"] = time.time()
            COUNTS["made"] += 1
        log("said %-22s %-38s %4.1f s in %5d ms (%s): %s" % (job["id"][:22], job["bank"][:38], job["seconds"], job["make_ms"], job["kind"], job["text"][:80]))


# ---- playing: every voice at once, each at its own volume and pan, eased so a move doesn't click ----

def mix(outdata, frames, _time, _status):
    out = np.zeros((frames, 2), dtype=np.float32)
    with LOCK:
        now = time.time()
        if not any(j["state"] == "playing" for j in JOBS.values()):
            ready = sorted((j for j in JOBS.values() if j["state"] == "ready"), key=lambda j: j["asked"])
            for j in ready:
                if now - j["asked"] > j["wait"] + ARGS.grace:   # its moment passed while another spoke
                    j["state"] = "dropped"
                    j.pop("samples", None)
                    COUNTS["dropped"] += 1
                    continue
                j["state"] = "playing"
                j["started"] = now
                break
        for job in JOBS.values():
            if job["state"] != "playing":
                continue
            w = job["samples"]
            p = job["pos"]
            n = min(frames, len(w) - p)
            if n <= 0:
                job["state"] = "done"
                job.pop("samples", None)
                continue
            seg = w[p:p + n]
            vol, pan = job["volume"], max(-1.0, min(1.0, job["pan"]))
            lv, rv = vol * np.cos((pan + 1) * np.pi / 4), vol * np.sin((pan + 1) * np.pi / 4)
            plv, prv = job.get("_lv", lv), job.get("_rv", rv)
            ramp = np.linspace(0.0, 1.0, n, dtype=np.float32)
            out[:n, 0] += seg * (plv + (lv - plv) * ramp)
            out[:n, 1] += seg * (prv + (rv - prv) * ramp)
            job["_lv"], job["_rv"] = lv, rv
            job["pos"] = p + n
            if job["pos"] >= len(w):
                job["state"] = "done"
                job.pop("samples", None)
    np.clip(out, -1.0, 1.0, out=out)
    outdata[:] = out


def tidy():
    """Old jobs forgotten after a minute (the mod has asked its last /status by then)."""
    while True:
        time.sleep(10)
        now = time.time()
        with LOCK:
            for k in [k for k, j in JOBS.items() if j["state"] not in ("queued", "making", "ready", "playing") and now - j["asked"] > 60]:
                del JOBS[k]


# ---- the HTTP side ----

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def reply(self, obj, code=200):
        b = json.dumps(obj, separators=(",", ":")).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def body(self):
        n = int(self.headers.get("Content-Length") or 0)
        try:
            return json.loads(self.rfile.read(n).decode("utf-8")) if n else {}
        except Exception:
            return {}

    def do_GET(self):
        u = urlparse(self.path)
        if u.path == "/health":
            with LOCK:
                q = sum(1 for j in JOBS.values() if j["state"] in ("queued", "making", "ready"))
                pl = sum(1 for j in JOBS.values() if j["state"] == "playing")
            self.reply({"ready": READY, "model": MODEL_NAME, "error": LOAD_ERROR, "free_gb": round(free_gb(), 2), "queued": q, "playing": pl,
                        "made": COUNTS["made"], "held": COUNTS["held"], "dropped": COUNTS["dropped"], "failed": COUNTS["failed"], "again": COUNTS["again"]})
        elif u.path == "/status":
            jid = (parse_qs(u.query).get("id") or [""])[0]
            with LOCK:
                j = JOBS.get(jid)
                if j is None:
                    self.reply({"state": "unknown"})
                else:
                    left = (len(j["samples"]) - j["pos"]) / SR if j["state"] == "playing" and "samples" in j else 0
                    self.reply({"state": j["state"], "make_ms": j.get("make_ms", 0), "seconds": j.get("seconds", 0), "left": round(left, 2), "why": j.get("why"), "bank": j.get("bank")})
        else:
            self.reply({"error": "no such path"}, 404)

    def do_POST(self):
        u = urlparse(self.path)
        b = self.body()
        if u.path == "/say":
            if not READY:
                return self.reply({"held": "loading" if LOAD_ERROR is None else "no model"})
            text = (b.get("text") or "").strip()
            jid = str(b.get("id") or "")
            if not text or not jid:
                return self.reply({"held": "no text or id"})
            bank, clip, how = pick_ref(b.get("model"), b.get("sex"), b.get("age"), b.get("person"))
            if clip is None:
                return self.reply({"held": "no voice"})
            with LOCK:
                waiting = sum(1 for j in JOBS.values() if j["state"] == "queued")
                if waiting >= ARGS.most_waiting:
                    COUNTS["held"] += 1
                    return self.reply({"held": "busy"})
                JOBS[jid] = {"id": jid, "text": text[:400], "kind": "thought" if b.get("kind") == "thought" else "said", "clip": clip, "bank": bank,
                             "volume": float(b.get("volume", 1.0)), "pan": float(b.get("pan", 0.0)), "wait": float(b.get("wait", 8.0)),
                             "exaggeration": b.get("exaggeration"), "asked": time.time(), "state": "queued"}
            WORK.put(JOBS[jid])
            self.reply({"ok": True, "bank": bank, "voice": how})
        elif u.path == "/place":
            with LOCK:
                j = JOBS.get(str(b.get("id") or ""))
                if j is not None:
                    if "volume" in b:
                        j["volume"] = max(0.0, min(2.0, float(b["volume"])))
                    if "pan" in b:
                        j["pan"] = float(b["pan"])
            self.reply({"ok": j is not None})
        elif u.path == "/stop":
            jid = b.get("id")
            with LOCK:
                for k, j in JOBS.items():
                    if (jid is None or k == jid) and j["state"] in ("queued", "making", "ready", "playing"):
                        j["state"] = "dropped"
                        j.pop("samples", None)
            self.reply({"ok": True})
        else:
            self.reply({"error": "no such path"}, 404)


def main():
    global ARGS
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=r"C:\mods\voice-test")
    ap.add_argument("--port", type=int, default=5005)
    ap.add_argument("--device", default="cuda")
    ap.add_argument("--min-free-gb", type=float, default=0.8, help="say nothing while the card has less free than this")
    ap.add_argument("--most-waiting", type=int, default=3)
    ap.add_argument("--thought-gain", type=float, default=0.6, help="a thought's loudness against a said line")
    ap.add_argument("--grace", type=float, default=4.0, help="seconds past its wait a made line may still start")
    ap.add_argument("--turbo", action="store_true", help="Chatterbox Turbo (faster; its own download)")
    ARGS = ap.parse_args()
    load_refs(ARGS.root)
    import sounddevice as sd
    stream = sd.OutputStream(samplerate=24000, channels=2, dtype="float32", callback=mix, blocksize=1024)
    threading.Thread(target=load_model, daemon=True).start()
    threading.Thread(target=worker, daemon=True).start()
    threading.Thread(target=tidy, daemon=True).start()
    srv = ThreadingHTTPServer(("127.0.0.1", ARGS.port), Handler)
    log("listening on http://127.0.0.1:%d" % ARGS.port)
    with stream:
        try:
            srv.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
