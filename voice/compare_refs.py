# The A/B for the clips recorded in game (2026-11-03): the same sentence said by a voice cloned from the export's clip and
# from the clip the game gave through the output tap (captured\<bank>.wav), side by side, to listen to. Nothing of this
# ships; it's for deciding whether a player's own game can give the clips (no OpenIV export needed).
#
#   tts-venv\Scripts\python.exe -I compare_refs.py --root C:\mods\voice-test [--out C:\mods\voice-test\compare] [bank ...]
#
# With no banks named, every bank in captured\ is compared. The out folder gets, per bank:
#   <bank>.ref-export.wav, <bank>.ref-captured.wav    the two clips the voice was cloned from
#   <bank>.said-export.wav, <bank>.said-captured.wav   the sentence in each clone
import argparse
import json
import os
import shutil
import sys
import time

TEXT = "Can't say I know you, mister, but you look like a man with somewhere to be. Fine morning for it."


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=r"C:\mods\voice-test")
    ap.add_argument("--out", default=None)
    ap.add_argument("--text", default=TEXT)
    ap.add_argument("--device", default="cuda")
    ap.add_argument("banks", nargs="*")
    a = ap.parse_args()
    out = a.out or os.path.join(a.root, "compare")
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(a.root, "refs.json"), encoding="utf-8") as f:
        refs = json.load(f)
    cap_dir = os.path.join(a.root, "captured")
    found = {}   # bank -> its clip, the kept ones first, then the poor ones (captured\poor, under the bar)
    for d in (os.path.join(cap_dir, "poor"), cap_dir):
        if os.path.isdir(d):
            for n in os.listdir(d):
                if n.lower().endswith(".wav"):
                    found[os.path.splitext(n)[0]] = os.path.join(d, n)
    banks = a.banks or sorted(found)
    if not banks:
        print("nothing in", cap_dir)
        return 1
    import torch
    import torchaudio
    from chatterbox.tts import ChatterboxTTS
    t = time.time()
    model = ChatterboxTTS.from_pretrained(device=a.device)
    print("model loaded in %.1f s" % (time.time() - t), flush=True)
    for bank in banks:
        pair = {"captured": found.get(bank) or os.path.join(cap_dir, bank + ".wav")}
        e = refs.get(bank) or {}
        if e.get("ref"):
            pair["export"] = os.path.join(a.root, e["ref"])
        for how, clip in pair.items():
            if not os.path.exists(clip):
                print(bank, how, "clip missing:", clip)
                continue
            shutil.copyfile(clip, os.path.join(out, "%s.ref-%s.wav" % (bank, how)))
            t = time.time()
            model.prepare_conditionals(clip, exaggeration=0.5)
            with torch.inference_mode():
                wav = model.generate(a.text, exaggeration=0.5, cfg_weight=0.5)
            path = os.path.join(out, "%s.said-%s.wav" % (bank, how))
            torchaudio.save(path, wav.detach().cpu(), model.sr)
            print("%s %-9s %.1f s of speech in %.1f s -> %s" % (bank, how, wav.shape[-1] / model.sr, time.time() - t, path), flush=True)
    print("listen in", out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
