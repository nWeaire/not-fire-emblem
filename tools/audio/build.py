"""Renders the game's music and sound effects.

Usage (from the repo root; needs `pip install -r tools/audio/requirements.txt`):
    python tools/audio/build.py                 # everything
    python tools/audio/build.py music [ID ...]  # assets/audio/music/<id>.ogg (looping tracks loop seamlessly)
    python tools/audio/build.py sfx [ID ...]    # assets/audio/sfx/<id>.wav

Music is composed in songs.py and sound effects in sfx.py; synth.py has the instruments.
"""
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).parent))
from sfx import SFX  # noqa: E402
from songs import SONGS  # noqa: E402
from synth import SR, finish  # noqa: E402

ROOT = Path(__file__).resolve().parents[2] / "assets" / "audio"
# Jingles that play over the music (not instead of it) are written as sound effects.
JINGLE_SFX = {"level_up"}


def build_music(ids):
    out = ROOT / "music"
    out.mkdir(parents=True, exist_ok=True)
    for sid in ids:
        song, loops = SONGS[sid]()
        wave = song.render(loop=loops)
        if sid in JINGLE_SFX:
            (ROOT / "sfx").mkdir(parents=True, exist_ok=True)
            sf.write(ROOT / "sfx" / f"{sid}.wav", wave.astype(np.float32), SR, subtype="PCM_16")
            print(f"  jingle {sid}: {len(wave) / SR:.1f}s (sfx)")
            continue
        # libsndfile's Vorbis encoder crashes the process on one large write; feed it in blocks.
        with sf.SoundFile(out / f"{sid}.ogg", "w", SR, 1, format="OGG", subtype="VORBIS") as f:
            for i in range(0, len(wave), 4096):
                f.write(wave[i:i + 4096].astype(np.float32))
        print(f"  music {sid}: {len(wave) / SR:.1f}s{' loop' if loops else ''}")


def build_sfx(ids):
    out = ROOT / "sfx"
    out.mkdir(parents=True, exist_ok=True)
    for sid in ids:
        wave = finish(SFX[sid](), peak=0.8)
        sf.write(out / f"{sid}.wav", wave.astype(np.float32), SR, subtype="PCM_16")
        print(f"  sfx {sid}: {len(wave) / SR:.2f}s")


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    ids = sys.argv[2:]
    if what in ("all", "music"):
        build_music(ids or list(SONGS))
    if what in ("all", "sfx"):
        build_sfx(ids or list(SFX))
