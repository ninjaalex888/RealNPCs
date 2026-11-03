# RealNPCs' local voice server (2026-11-03; Lex: "voiced thoughts and dialogue"). The mod asks it, the way it asks Ollama,
# to say a line in a person's own voice: the voice is cloned on this PC with Chatterbox from one of the game's own
# recordings for that person's model (the player's own OpenIV export of the voice banks; nothing of Rockstar's ships),
# and played here, mixed, with the volume and pan the mod keeps sending as the speaker moves.
#
#   tts-venv\Scripts\python.exe -I voice_server.py --root C:\mods\voice-test [--port 5005] [--min-free-gb 1.0]
#
# --root holds refs.json (pick_refs.py) and the banks\ folder it points into. Plain HTTP on 127.0.0.1 only:
#   GET  /health                -> {"ready":..,"model":..,"free_gb":..,"queued":..,"playing":..,"made":..,"held":..}
#   POST /say    {"id","model","sex","age","person","origin","text","kind":"thought"|"said","volume","pan","wait"}
#                               -> {"ok":true,"bank":..,"voice":"own|model|borrowed","need":..} or {"held":"why","need":..}
#                               (never waits for the voice: the mod asks /status). "need" names the person's own bank
#                               when it has no clip yet, for the mod to have them record it (own_bank: one of their
#                               model's banks chosen by who they are, for good).
#   POST /taint  {"id","why"}    another voice spoke into a recording's window: the clip goes under captured\poor
#   GET  /needed?most=600       -> {"banks":"a,b,c","none":..,"weak":..,"clips":..}: the banks still worth recording
#   POST /place  {"id","volume","pan"}   the speaker moved
#   POST /stop   {"id"} or {}            one voice, or every voice
#   GET  /status?id=..          -> {"state":"queued|making|ready|playing|done|dropped|failed|held|unknown","make_ms":..,"seconds":..}
#   POST /capture {"id","bank"}  -> {"ok":true,"line":..,"take":..,"seconds":..} or {"held":"why"}: the server records what this
#                               PC plays for the line's length and a margin (the mod has the person say that line, the game's
#                               own, the moment it hears back), keeps the spoken part, judges it against the sound around it
#                               and saves it as <root>\captured\<bank>.wav; /status then says captured|poor|failed, with "snr".
#                               That way a player's own game gives the clips, and nothing of Rockstar's ships.
# The clips: refs.json in --root (pick_refs.py over the player's own export) where it is; voices.json beside this script (the
# shipped list: each bank's line by name, its model, sex and age; no clips, no words) for the rest; captured\ for banks the
# export doesn't cover, or every bank with --prefer-captured.
import argparse
import collections
import hashlib
import json
import os
import queue
import re
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
INFO = {}                      # bank -> model, kind (sex), age, tier, spanish, tag (the people it was recorded for, off its name)
LINES = {}                     # bank -> (line, take, seconds): the game's own recording the voice is cloned from, by name
CLIPS = {}                     # bank -> (clip, tier, "export"|"recorded"): the clip its voice is cloned from
BANKS_BY_MODEL = {}            # model -> every bank the game has for it, clip or no clip
INDEX = {}                     # bank -> {"snr","seconds","when","tries","last"}: the recordings so far (captured\index.json)
CAPTURING = False              # the mixer starts no voice while the output tap records
TAG = re.compile(r"_(WHITE|BLACK|HISPANIC|ASIAN|NATIVE|IRISH)(?:_|$)")   # mid-name too: A_M_M_CIV_POOR_WHITE_AVOID_14
ORIGIN_TAG = {"white": "WHITE", "french": "WHITE", "italian": "WHITE", "german": "WHITE", "scandinavian": "WHITE", "black": "BLACK",
              "hispanic": "HISPANIC", "chinese": "ASIAN", "native": "NATIVE", "irish": "IRISH"}


def log(*a):
    print(time.strftime("%H:%M:%S"), *a, flush=True)


# ---- which recording a person's voice is cloned from ----

def line_of(e):
    """The line a bank's clip is, by name and take, from the shipped fields or from the export's file name
    (CHAT_RESPONSE_B_01.wav: line CHAT_RESPONSE_B, take 1); a hashed name (0x03595055) is no use, so the second choice then."""
    if e.get("line"):
        return e["line"], int(e.get("take") or 1), float(e.get("seconds") or 0)
    for key, secs in (("ref", "seconds"), ("alt", "alt_seconds")):
        base = os.path.splitext(os.path.basename(e.get(key) or ""))[0]
        if "_" in base and base.rsplit("_", 1)[1].isdigit() and not base.startswith("0x"):
            name, take = base.rsplit("_", 1)
            return name, int(take), float(e.get(secs) or 0)
    return None


def load_refs(root):
    entries = {}
    for path in (os.path.join(root, "refs.json"), os.path.join(os.path.dirname(os.path.abspath(__file__)), "voices.json")):
        if os.path.exists(path):
            with open(path, encoding="utf-8") as f:
                for bank, e in json.load(f).items():
                    entries.setdefault(bank, {}).update(e)
    index_path = os.path.join(root, "captured", "index.json")
    if os.path.exists(index_path):
        try:
            with open(index_path, encoding="utf-8") as f:
                INDEX.update(json.load(f))
        except Exception as e:
            log("captured\\index.json unreadable, starting over:", repr(e)[:80])
    exported = recorded = 0
    for bank, e in entries.items():
        m = TAG.search(bank)
        INFO[bank] = {"model": (e.get("model") or "").upper(), "kind": (e.get("kind") or "").lower(), "age": (e.get("age") or "").lower(),
                      "tier": int(e.get("tier") or 4), "spanish": bool(e.get("spanish")) or "spanish" in (e.get("notes") or "").lower(),
                      "tag": m.group(1) if m else None}
        BANKS_BY_MODEL.setdefault(INFO[bank]["model"], []).append(bank)
        l = line_of(e)
        if l:
            LINES[bank] = l
        clip = os.path.join(root, e["ref"]) if e.get("ref") and int(e.get("tier") or 0) > 0 else None
        if clip and not os.path.exists(clip):
            clip = None
        cap = os.path.join(root, "captured", bank + ".wav")
        if os.path.exists(cap) and (clip is None or ARGS.prefer_captured):
            snr = (INDEX.get(bank) or {}).get("snr", 0)
            add_ref(bank, cap, 2 if snr >= 18 else 3, "recorded")
            recorded += 1
        elif clip:
            add_ref(bank, clip, INFO[bank]["tier"], "export")
            exported += 1
    for lst in BANKS_BY_MODEL.values():
        lst.sort()
    log("voices: %d banks known, %d with a line to record; clips: %d from the export, %d recorded in game; %d models" %
        (len(entries), len(LINES), exported, recorded, len(REFS_BY_MODEL)))


def add_ref(bank, clip, tier, source):
    """A clip for the bank, into the tables pick_ref chooses from (in place of the bank's old clip, if it had one)."""
    i = INFO.get(bank) or {}
    item = (bank, clip, tier, i.get("spanish", False))
    for d, key in ((REFS_BY_MODEL, i.get("model", "")), (REFS_BY_KIND, (i.get("kind", ""), i.get("age", "")))):
        lst = [c for c in d.get(key, []) if c[0] != bank]
        lst.append(item)
        lst.sort()
        d[key] = lst
    CLIPS[bank] = (clip, tier, source)
    CONDS.pop(clip, None)


def save_index():
    try:
        d = os.path.join(ARGS.root, "captured")
        os.makedirs(d, exist_ok=True)
        with open(os.path.join(d, "index.json"), "w", encoding="utf-8") as f:
            json.dump(INDEX, f, indent=1, sort_keys=True)
    except Exception as e:
        log("captured\\index.json not written:", repr(e)[:80])


def wanted(bank):
    """Whether this bank should be recorded (again): no clip yet, or one recorded in game under the good bar, and not
    tried lately. The export's clips are never recorded over (unless --prefer-captured puts the recordings first)."""
    if bank not in LINES:
        return False
    e = INDEX.get(bank) or {}
    if time.time() - e.get("last", 0) < ARGS.retry_minutes * 60:
        return False
    c = CLIPS.get(bank)
    if c is None:
        return True
    if c[2] == "export":
        return bool(ARGS.prefer_captured)   # recordings first: the export's banks are recorded too, once
    return e.get("snr", 0) < ARGS.good_snr


def own_bank(model, person, origin):
    """The bank a person's voice is, for good: one of their model's banks chosen by who they are, among those recorded
    for their people when known (the bank names say: _HISPANIC_01), and never a Spanish-speaking bank for anyone else."""
    banks = BANKS_BY_MODEL.get(model) or []
    if not banks:
        return None
    tag = ORIGIN_TAG.get((origin or "").lower())
    pool = [b for b in banks if tag is None or INFO[b]["tag"] in (None, tag)] or banks
    if tag != "HISPANIC":
        pool = [b for b in pool if not INFO[b]["spanish"]] or pool
    return pool[stable(person) % len(pool)]


def stable(s):
    return int(hashlib.md5(s.encode("utf-8")).hexdigest()[:8], 16)


def pick_ref(model, sex, age, person, origin=None):
    """The same person always gets the same voice: their own bank (own_bank), when it has a clip. Until it has one, a
    stand-in: another of their model's banks with a clip (English first, the better tiers first), else one borrowed from
    a model of the same sex and age; and "need" names their own bank, for the mod to have them record it."""
    person = person or model or "?"
    model = (model or "").upper()
    parts = model.split("_")
    if len(parts) > 3 and parts[0] in ("A", "S", "U", "G", "CS") and parts[1] in ("M", "F") and parts[2] in ("M", "O", "Y"):
        sex = sex or ("male" if parts[1] == "M" else "female")
        age = {"M": "adult", "O": "old", "Y": "young"}[parts[2]] if not age else age
    own = own_bank(model, person, origin)
    need = own if own and wanted(own) else None
    if own and own in CLIPS:
        return own, CLIPS[own][0], "own", need
    cands = REFS_BY_MODEL.get(model)
    how = "model"
    if not cands:
        how = "borrowed"
        cands = REFS_BY_KIND.get(((sex or "male").lower(), (age or "adult").lower())) or REFS_BY_KIND.get(("male", "adult")) or []
        cands = [c for c in cands if c[2] <= 2] or cands
    if not cands:   # no clip at all yet (a player's first minutes, before any voice is recorded)
        return None, None, how, need
    english = [c for c in cands if not c[3]] or cands
    best = min(c[2] for c in english)
    pool = [c for c in english if c[2] <= max(best, 2)]
    c = pool[stable(person) % len(pool)]
    return c[0], c[1], how, need


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
        try:   # the first line said is slow (kernels, caches): say one nobody hears, in a clip's voice or the model's own
            t = time.time()
            if warm:
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


# ---- recording the game's own clips off this PC's output (the player's own game; nothing shipped) ----

def clean_noise(x, sr):
    """The steady sound under a voice (the street, wind, rain, the game's room tone) taken off by spectral subtraction:
    the noise's spectrum is the quiet fifth of the frames, per frequency; what's left of a frame is at least a tenth of
    it, so there are no holes. Lex, 2026-11-03: the clips the game gave cloned well but carried the street."""
    from scipy.signal import stft, istft
    n = 1024 if sr >= 32000 else 512
    _f, _t, Z = stft(x, fs=sr, nperseg=n, noverlap=n * 3 // 4)
    mag, ph = np.abs(Z), np.angle(Z)
    energy = mag.mean(axis=0)
    quiet = mag[:, energy <= np.percentile(energy, 20)]
    noise = np.median(quiet, axis=1, keepdims=True) if quiet.shape[1] else np.zeros((mag.shape[0], 1))
    cleaned = np.maximum(mag - 1.5 * noise, 0.1 * mag)
    _t2, y = istft(cleaned * np.exp(1j * ph), fs=sr, nperseg=n, noverlap=n * 3 // 4)
    return y[:len(x)].astype(np.float32)


def capture_fail(job, why):
    job["state"] = "failed"
    job["why"] = why
    COUNTS["capfail"] += 1
    log("capture %s: failed, %s" % (job["bank"], why))


def capture(job):
    """Records what this PC plays for the line's length and a margin, keeps the spoken part, judges it against the sound
    around it (the street, rain, music) and saves it as the bank's clip when it stands clear enough of that."""
    global CAPTURING
    bank, secs = job["bank"], job["for"]
    try:
        import pyaudiowpatch as pa
        import soundfile as sf
    except Exception as e:
        return capture_fail(job, "no recorder (pip install pyaudiowpatch soundfile): " + repr(e)[:80])
    chunks = []
    p = pa.PyAudio()
    try:
        api = p.get_host_api_info_by_type(pa.paWASAPI)
        out = p.get_device_info_by_index(api["defaultOutputDevice"])
        tap = next((d for d in p.get_loopback_device_info_generator() if out["name"] in d["name"]), None)
        if tap is None:
            return capture_fail(job, "no output tap for " + out["name"])
        sr, ch = int(tap["defaultSampleRate"]), int(tap["maxInputChannels"])

        def heard(data, _frames, _t, _status):
            chunks.append(np.frombuffer(data, dtype=np.float32).reshape(-1, ch).mean(axis=1))
            return (None, pa.paContinue)

        s = p.open(format=pa.paFloat32, channels=ch, rate=sr, input=True, input_device_index=tap["index"], frames_per_buffer=2048, stream_callback=heard)
        CAPTURING = True
        deadline = time.time() + secs
        while time.time() < deadline and job["state"] == "recording":
            time.sleep(0.05)
        s.stop_stream()
        s.close()
    except Exception as e:
        return capture_fail(job, "the output tap failed: " + repr(e)[:120])
    finally:
        CAPTURING = False
        p.terminate()
    if job["state"] != "recording":
        return
    if not chunks:
        return capture_fail(job, "the output tap gave nothing (is the game's sound on another device?)")
    x = np.concatenate(chunks)
    e = INDEX.setdefault(bank, {})
    e["tries"] = e.get("tries", 0) + 1
    e["last"] = time.time()
    pre = x[:int(sr * 0.3)]
    job["floor"] = round(float(20 * np.log10(np.sqrt(np.mean(pre * pre)) + 1e-9)), 1) if len(pre) else None
    blk = max(1, sr // 20)   # 50 ms blocks
    n = len(x) // blk
    if n < 10:
        return capture_fail(job, "too short")
    db = 20 * np.log10(np.sqrt(np.mean(x[:n * blk].reshape(n, blk) ** 2, axis=1)) + 1e-9)
    floor, loud = float(np.percentile(db, 10)), float(np.percentile(db, 95))
    if loud < -50:
        return capture_fail(job, "nothing heard (loudest %.0f dB)" % loud)
    snr = loud - floor
    on = np.where(db > max(floor + 10, loud - 35))[0]
    a, b = max(0, (on[0] - 3) * blk), min(len(x), (on[-1] + 4) * blk)
    y = x[a:b]
    length = len(y) / sr
    if length < 1.5:
        return capture_fail(job, "only %.1f s heard" % length)
    raw = y
    try:
        y = clean_noise(y, sr)
    except Exception as e:
        log("capture %s: not cleaned (%s)" % (bank, repr(e)[:80]))
    job.update({"seconds": round(length, 2), "snr": round(snr, 1)})
    kept = CLIPS.get(bank)
    kept_snr = e.get("snr") if kept and kept[2] == "recorded" else None
    why = None   # a clip under the bar is kept too, in captured\poor, to listen to and to set the bar by; never cloned from
    if job.get("tainted"):
        why = "another voice in it (%s)" % job["tainted"]
    elif snr < ARGS.capture_snr:
        why = "%.0f dB over the sound around it, %.0f needed" % (snr, ARGS.capture_snr)
    elif kept_snr is not None and snr <= kept_snr:
        why = "%.0f dB over the sound around it, the clip kept has %.0f" % (snr, kept_snr)
    d = os.path.join(ARGS.root, "captured", "poor" if why else "")
    os.makedirs(os.path.join(d, "raw"), exist_ok=True)
    path = os.path.join(d, bank + ".wav")
    for p, w in ((path, y), (os.path.join(d, "raw", bank + ".wav"), raw)):   # the raw one kept, to clean again another way
        peak = float(np.max(np.abs(w)))
        sf.write(p, (w * (0.9 / peak) if peak > 0 else w).astype(np.float32), sr, subtype="PCM_16")
    job["path"] = path
    if why:
        job["state"] = "poor"
        job["why"] = why
        COUNTS["poor"] += 1
        save_index()
        log("capture %s: poor, %s (floor %s dB) -> %s" % (bank, why, job["floor"], path))
        return
    e.update({"snr": round(snr, 1), "seconds": round(length, 2), "when": time.strftime("%Y-%m-%d %H:%M")})
    save_index()
    add_ref(bank, path, 2 if snr >= 18 else 3, "recorded")
    job["state"] = "captured"
    COUNTS["captured"] += 1
    log("capture %s: %.1f s, %.0f dB over the sound around it (floor %s dB) -> %s" % (bank, length, snr, job["floor"], path))


# ---- playing: every voice at once, each at its own volume and pan, eased so a move doesn't click ----

def mix(outdata, frames, _time, _status):
    out = np.zeros((frames, 2), dtype=np.float32)
    with LOCK:
        now = time.time()
        if not CAPTURING and not any(j["state"] == "playing" for j in JOBS.values()):
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
            for k in [k for k, j in JOBS.items() if j["state"] not in ("queued", "making", "ready", "playing", "recording") and now - j["asked"] > 60]:
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
                        "made": COUNTS["made"], "held": COUNTS["held"], "dropped": COUNTS["dropped"], "failed": COUNTS["failed"], "again": COUNTS["again"],
                        "captured": COUNTS["captured"], "poor": COUNTS["poor"]})
        elif u.path == "/needed":   # the banks still worth recording, those without a clip first (the batch row)
            most = int((parse_qs(u.query).get("most") or ["600"])[0])
            none = [b for b in sorted(LINES) if b not in CLIPS or (ARGS.prefer_captured and CLIPS[b][2] == "export")]
            weak = [b for b in sorted(LINES) if b in CLIPS and CLIPS[b][2] == "recorded" and (INDEX.get(b) or {}).get("snr", 0) < ARGS.good_snr]
            self.reply({"banks": ",".join((none + weak)[:most]), "none": len(none), "weak": len(weak), "clips": len(CLIPS)})
        elif u.path == "/status":
            jid = (parse_qs(u.query).get("id") or [""])[0]
            with LOCK:
                j = JOBS.get(jid)
                if j is None:
                    self.reply({"state": "unknown"})
                else:
                    left = (len(j["samples"]) - j["pos"]) / SR if j["state"] == "playing" and "samples" in j else 0
                    self.reply({"state": j["state"], "make_ms": j.get("make_ms", 0), "seconds": j.get("seconds", 0), "left": round(left, 2), "why": j.get("why"), "bank": j.get("bank"),
                                "snr": j.get("snr"), "path": j.get("path")})
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
            bank, clip, how, need = pick_ref(b.get("model"), b.get("sex"), b.get("age"), b.get("person"), b.get("origin"))
            if clip is None:
                return self.reply({"held": "no voice", "need": need})
            with LOCK:
                waiting = sum(1 for j in JOBS.values() if j["state"] == "queued")
                if waiting >= ARGS.most_waiting:
                    COUNTS["held"] += 1
                    return self.reply({"held": "busy"})
                JOBS[jid] = {"id": jid, "text": text[:400], "kind": "thought" if b.get("kind") == "thought" else "said", "clip": clip, "bank": bank,
                             "volume": float(b.get("volume", 1.0)), "pan": float(b.get("pan", 0.0)), "wait": float(b.get("wait", 8.0)),
                             "exaggeration": b.get("exaggeration"), "asked": time.time(), "state": "queued"}
            WORK.put(JOBS[jid])
            self.reply({"ok": True, "bank": bank, "voice": how, "need": need})
        elif u.path == "/taint":   # the mod heard another voice in the recording's window: the clip goes under poor
            with LOCK:
                j = JOBS.get(str(b.get("id") or ""))
                if j is not None and j.get("kind") == "capture":
                    j["tainted"] = str(b.get("why") or "another voice")
            self.reply({"ok": j is not None})
        elif u.path == "/place":
            with LOCK:
                j = JOBS.get(str(b.get("id") or ""))
                if j is not None:
                    if "volume" in b:
                        j["volume"] = max(0.0, min(2.0, float(b["volume"])))
                    if "pan" in b:
                        j["pan"] = float(b["pan"])
            self.reply({"ok": j is not None})
        elif u.path == "/capture":
            bank, jid = str(b.get("bank") or ""), str(b.get("id") or "")
            l = LINES.get(bank)
            if not bank or not jid:
                return self.reply({"held": "no bank or id"})
            if l is None:
                return self.reply({"held": "no line known for " + bank})
            with LOCK:
                if CAPTURING or any(j["state"] in ("recording", "playing") for j in JOBS.values()):
                    return self.reply({"held": "a voice is playing or recording"})
                secs = min(16.0, (l[2] or 6.0) + 3.0)
                JOBS[jid] = {"id": jid, "bank": bank, "kind": "capture", "line": l[0], "take": l[1], "for": secs, "text": "",
                             "asked": time.time(), "state": "recording", "volume": 0.0, "pan": 0.0, "wait": secs}
            threading.Thread(target=capture, args=(JOBS[jid],), daemon=True).start()
            self.reply({"ok": True, "line": l[0], "take": l[1], "seconds": l[2], "for": secs})
        elif u.path == "/stop":
            jid = b.get("id")
            with LOCK:
                for k, j in JOBS.items():
                    if (jid is None or k == jid) and j["state"] in ("queued", "making", "ready", "playing", "recording"):
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
    ap.add_argument("--capture-snr", type=float, default=12.0, help="a clip recorded in game is kept when it stands this many dB over the sound around it")
    ap.add_argument("--good-snr", type=float, default=25.0, help="a recorded clip this clear isn't recorded again")
    ap.add_argument("--retry-minutes", type=float, default=10.0, help="a bank isn't recorded again sooner than this")
    ap.add_argument("--prefer-captured", action="store_true", help="the clips recorded in game before the export's, where both exist")
    ap.add_argument("--check", action="store_true", help="load the model (its first download), say one line to nobody, report and exit (install-voice.cmd)")
    ARGS = ap.parse_args()
    load_refs(ARGS.root)
    if ARGS.check:
        load_model()
        if LOAD_ERROR is not None:
            log("the voice model couldn't start:", LOAD_ERROR)
            sys.exit(1)
        try:
            import torch
            with torch.inference_mode():
                wav = MODEL.generate("The voices are ready.") if ARGS.turbo else MODEL.generate("The voices are ready.", exaggeration=0.5, cfg_weight=0.5)
            log("ready: %s said %.1f s in its own voice; %.1f GB free on the card" % (MODEL_NAME, wav.shape[-1] / SR, free_gb()))
        except Exception as e:
            log("the voice model loaded but couldn't speak:", repr(e)[:200])
            sys.exit(1)
        sys.exit(0)
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
