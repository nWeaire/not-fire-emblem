"""A small GBA-flavoured synthesizer: sample-style instruments, a note sequencer and a reverb, in numpy.

GBA Fire Emblem music is low-fi *sampled* orchestra (strings, brass, flute, harp, timpani) played at a low sample
rate, not square-wave chiptune. These voices imitate that with additive synthesis and simple envelopes. Everything
renders mono at 22050 Hz, close to the rates GBA games mixed at.
"""
from __future__ import annotations

import re

import numpy as np

SR = 22050
RNG = np.random.default_rng(7)  # fixed, so builds are reproducible

NOTE_INDEX = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(name: str) -> int:
    """'C4' = 60, 'F#3', 'Bb5'."""
    m = re.fullmatch(r"([A-G])([#b]?)(-?\d)", name)
    if not m:
        raise ValueError(f"bad note {name!r}")
    n = NOTE_INDEX[m.group(1)] + {"#": 1, "b": -1, "": 0}[m.group(2)]
    return 12 * (int(m.group(3)) + 1) + n


def hz(m: float) -> float:
    return 440.0 * 2 ** ((m - 69) / 12)


def t_axis(seconds: float) -> np.ndarray:
    return np.arange(int(seconds * SR)) / SR


def adsr(n: int, a: float, d: float, s: float, r: float, hold: float) -> np.ndarray:
    """Attack/decay/sustain level/release (seconds); the note is held for `hold` seconds, then released."""
    t = np.arange(n) / SR
    env = np.where(t < a, t / max(a, 1e-4), 1.0)
    env = np.where((t >= a) & (t < a + d), 1 - (1 - s) * (t - a) / max(d, 1e-4), env)
    env = np.where(t >= a + d, s, env)
    env = np.where(t >= hold, env * np.clip(1 - (t - hold) / max(r, 1e-4), 0, 1), env)
    return env


def additive(freq: float, t: np.ndarray, weights, vib: float = 0.0, vib_rate: float = 5.5, phase_mod=None):
    """Sum of harmonics k*freq with the given weights, stopping below Nyquist. Optional delayed vibrato."""
    out = np.zeros_like(t)
    vib_env = np.clip((t - 0.25) / 0.4, 0, 1) * vib
    # vib is the pitch swing as a fraction of the frequency (0.005 = half a percent).
    phase = 2 * np.pi * freq * t + vib_env * freq / vib_rate * np.sin(2 * np.pi * vib_rate * t)
    for k, w in enumerate(weights, start=1):
        if k * freq > SR / 2 - 500 or w == 0:
            break
        out += w * np.sin(k * phase + (0 if phase_mod is None else phase_mod * k))
    return out


def lowpass(x: np.ndarray, cutoff: float) -> np.ndarray:
    """Gentle FFT low-pass (a smooth roll-off, not a brick wall)."""
    if len(x) < 8:
        return x
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    spec *= 1 / np.sqrt(1 + (f / cutoff) ** 4)
    return np.fft.irfft(spec, len(x))


def highpass(x: np.ndarray, cutoff: float) -> np.ndarray:
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    spec *= 1 - 1 / np.sqrt(1 + (f / cutoff) ** 4)
    return np.fft.irfft(spec, len(x))


def bandpass(x: np.ndarray, lo: float, hi: float) -> np.ndarray:
    return highpass(lowpass(x, hi), lo)


def noise(seconds: float) -> np.ndarray:
    return RNG.uniform(-1, 1, int(seconds * SR))


# ------------------------------------------------------------------ instruments: (freq, hold seconds, velocity) -> wave

def strings(f, hold, vel):
    t = t_axis(hold + 0.35)
    saw = [1 / k for k in range(1, 14)]
    w = 0.5 * additive(f * 1.0023, t, saw, vib=0.005) + 0.5 * additive(f * 0.9977, t, saw, vib=0.005)
    return lowpass(w, 2600 + 900 * vel) * adsr(len(t), 0.09, 0.2, 0.85, 0.3, hold) * vel * 0.32


def brass(f, hold, vel):
    t = t_axis(hold + 0.2)
    bright = np.clip(t / 0.08, 0.25, 1.0)
    out = np.zeros_like(t)
    for k in range(1, 16):
        if k * f > SR / 2 - 500:
            break
        out += np.sin(2 * np.pi * k * f * t) * (bright ** (k * 0.6)) / k
    out = lowpass(out, 2200 + 1400 * vel)
    return out * adsr(len(t), 0.035, 0.12, 0.8, 0.16, hold) * vel * 0.36


def flute(f, hold, vel):
    t = t_axis(hold + 0.15)
    tone = additive(f, t, [1, 0.18, 0.06], vib=0.006, vib_rate=5.0)
    breath = lowpass(RNG.uniform(-1, 1, len(t)), 3000) * 0.05
    return (tone + breath) * adsr(len(t), 0.05, 0.1, 0.9, 0.12, hold) * vel * 0.42


def harp(f, hold, vel):
    t = t_axis(max(hold, 0.2) + 1.2)
    out = sum(np.sin(2 * np.pi * k * f * t) * np.exp(-t * (1.8 + k * 1.4)) / k ** 1.4 for k in range(1, 9)
              if k * f < SR / 2 - 500)
    return out * np.minimum(1, t / 0.003) * vel * 0.4


def bass(f, hold, vel):
    t = t_axis(hold + 0.12)
    out = additive(f, t, [1, 0.35, 0.18, 0.08, 0.04])
    return lowpass(out, 900) * adsr(len(t), 0.01, 0.25, 0.6, 0.1, hold) * vel * 0.5


def pizz(f, hold, vel):
    t = t_axis(0.45)
    out = additive(f, t, [1, 0.5, 0.25, 0.12])
    return lowpass(out, 1800) * np.exp(-t * 7) * np.minimum(1, t / 0.004) * vel * 0.5


def choir(f, hold, vel):
    t = t_axis(hold + 0.5)
    # An "ah" vowel: harmonics near 700 Hz and 1200 Hz are stronger.
    w = [np.exp(-((k * f - 700) / 300) ** 2) + 0.6 * np.exp(-((k * f - 1200) / 350) ** 2) + 0.15 / k for k in range(1, 20)]
    out = additive(f * 1.003, t, w, vib=0.004) + additive(f * 0.997, t, w, vib=0.004)
    return out * adsr(len(t), 0.35, 0.3, 0.9, 0.5, hold) * vel * 0.12


def bell(f, hold, vel):
    t = t_axis(1.6)
    out = sum(a * np.sin(2 * np.pi * f * r * t) * np.exp(-t * d) for r, a, d in
              ((1, 1, 2.2), (2.76, 0.45, 4), (5.4, 0.25, 7), (8.9, 0.1, 11)) if f * r < SR / 2 - 500)
    return out * vel * 0.3


def timpani(f, hold, vel):
    t = t_axis(1.3)
    tone = np.sin(2 * np.pi * f * t * (1 + 0.03 * np.exp(-t * 12))) * np.exp(-t * 2.8)
    hit = lowpass(noise(1.3), 600) * np.exp(-t * 22) * 0.6
    return (tone + hit) * vel * 0.6


# Percussion ignores pitch.
def kick(f, hold, vel):
    t = t_axis(0.3)
    sweep = 50 + 90 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(sweep) / SR) * np.exp(-t * 11) * vel * 0.9


def snare(f, hold, vel):
    t = t_axis(0.22)
    body = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30) * 0.5
    rattle = bandpass(noise(0.22), 1500, 7000) * np.exp(-t * 18)
    return (body + rattle) * vel * 0.55


def hat(f, hold, vel):
    t = t_axis(0.06)
    return highpass(noise(0.06), 7000) * np.exp(-t * 70) * vel * 0.25


def crash(f, hold, vel):
    t = t_axis(1.4)
    return highpass(noise(1.4), 4000) * np.exp(-t * 3) * vel * 0.3


def tom(f, hold, vel):
    t = t_axis(0.4)
    sweep = 110 + 60 * np.exp(-t * 15)
    return (np.sin(2 * np.pi * np.cumsum(sweep) / SR) * np.exp(-t * 8) + lowpass(noise(0.4), 800) * np.exp(-t * 30) * 0.3) * vel * 0.6


INSTRUMENTS = {"strings": strings, "brass": brass, "flute": flute, "harp": harp, "bass": bass, "pizz": pizz,
               "choir": choir, "bell": bell, "timpani": timpani, "kick": kick, "snare": snare, "hat": hat,
               "crash": crash, "tom": tom}
DRUM_KEYS = {"k": "kick", "s": "snare", "h": "hat", "c": "crash", "t": "tom", "T": "timpani"}

# ------------------------------------------------------------------ chords

QUALITIES = {"": (0, 4, 7), "m": (0, 3, 7), "7": (0, 4, 7, 10), "m7": (0, 3, 7, 10), "maj7": (0, 4, 7, 11),
             "sus4": (0, 5, 7), "dim": (0, 3, 6), "aug": (0, 4, 8)}


def chord(name: str) -> tuple[int, tuple[int, ...]]:
    """'F#m' -> (pitch class of the root, intervals)."""
    m = re.fullmatch(r"([A-G][#b]?)(.*)", name)
    root = (NOTE_INDEX[m.group(1)[0]] + {"#": 1, "b": -1}.get(m.group(1)[1:], 0)) % 12
    return root, QUALITIES[m.group(2)]


def chord_notes(name: str, octave: int) -> list[int]:
    root, ivs = chord(name)
    base = 12 * (octave + 1) + root
    return [base + i for i in ivs]


# ------------------------------------------------------------------ song

class Song:
    """Beats-based sequencer. Bars are 4 beats. Parts add (start beat, midi, beats, instrument, velocity) events."""

    def __init__(self, bpm: float, bars: int, beats_per_bar: int = 4):
        self.bpm, self.bars, self.bpb = bpm, bars, beats_per_bar
        self.events: list[tuple[float, float, float, str, float]] = []

    @property
    def spb(self) -> float:
        return 60.0 / self.bpm

    @property
    def length(self) -> float:
        return self.bars * self.bpb * self.spb

    def note(self, beat, pitch, beats, inst, vel=0.8):
        self.events.append((beat, pitch, beats, inst, vel))

    def melody(self, text: str, inst: str, vel=0.8, start_bar=0, transpose=0, legato=0.92):
        """'A4:1.5 D5:.5 r:2' — note:beats; 'r' is a rest. Checks that each bar line ('|') falls on a bar."""
        beat = start_bar * self.bpb
        for bar_text in text.split("|"):
            bar_start = beat
            for tok in bar_text.split():
                name, length = tok.split(":")
                length = float(length)
                if name != "r":
                    self.note(beat, midi(name) + transpose, length * legato, inst, vel)
                beat += length
            if bar_text.strip() and abs(beat - bar_start - self.bpb) > 1e-6:
                raise ValueError(f"bar '{bar_text.strip()}' has {beat - bar_start} beats")
        return beat

    def chords(self, names: list[str]) -> list[str]:
        """Expands 'Dm:2' into two bars of Dm."""
        out = []
        for n in names:
            name, _, count = n.partition(":")
            out += [name] * int(count or 1)
        return out

    def pad(self, names, inst="strings", octave=4, vel=0.45, start_bar=0):
        for i, c in enumerate(self.chords(names)):
            for p in chord_notes(c, octave)[:3]:
                self.note((start_bar + i) * self.bpb, p, self.bpb * 0.98, inst, vel)

    def bassline(self, names, pattern=((0, 2), (2, 2)), inst="bass", octave=2, vel=0.7, fifth_on=(), start_bar=0):
        """pattern: (beat, length) pairs per bar; beats listed in fifth_on play the fifth instead of the root."""
        for i, c in enumerate(self.chords(names)):
            root, ivs = chord(c)
            base = 12 * (octave + 1) + root
            for b, length in pattern:
                p = base + (7 if b in fifth_on else 0)
                self.note((start_bar + i) * self.bpb + b, p, length * 0.9, inst, vel)

    def arp(self, names, inst="harp", octave=4, step=0.5, order=(0, 1, 2, 1), vel=0.45, start_bar=0, up_octave=True):
        for i, c in enumerate(self.chords(names)):
            notes = chord_notes(c, octave)[:3] + ([chord_notes(c, octave)[0] + 12] if up_octave else [])
            n = int(self.bpb / step)
            for j in range(n):
                self.note((start_bar + i) * self.bpb + j * step, notes[order[j % len(order)] % len(notes)], step, inst, vel)

    def drums(self, pattern: str, bars: int, start_bar=0, vel=0.7, steps_per_bar=16):
        """One character per step: k kick, s snare, h hat, c crash, t tom, T timpani (low D), '.' rest.
        Upper/lower case is not accent; use separate patterns for accents."""
        step = self.bpb / steps_per_bar
        for b in range(bars):
            for j, ch in enumerate(pattern.replace(" ", "")[:steps_per_bar]):
                if ch in DRUM_KEYS:
                    pitch = midi("D2") if ch == "T" else 60
                    self.note((start_bar + b) * self.bpb + j * step, pitch, step, DRUM_KEYS[ch], vel)

    def render(self, loop: bool = True, reverb: float = 0.18, tail: float = 3.0) -> np.ndarray:
        total = self.length + tail
        out = np.zeros(int(total * SR) + SR)
        for beat, pitch, beats, inst, vel in self.events:
            wave = INSTRUMENTS[inst](hz(pitch), beats * self.spb, vel)
            start = int(beat * self.spb * SR)
            end = min(len(out), start + len(wave))
            out[start:end] += wave[: end - start]
        out = master(out, reverb)
        n = int(self.length * SR)
        if loop:
            # Fold the tail (release + reverb) back onto the start so the loop point is seamless.
            tail_part = out[n:]
            out = out[:n].copy()
            out[: len(tail_part)] += tail_part[: n]
        else:
            out = out[: int((self.length + tail) * SR)]
            fade = min(len(out), int(0.4 * SR))
            out[-fade:] *= np.linspace(1, 0, fade)
        return finish(out)


def reverb_ir(seconds=1.6, decay=3.2) -> np.ndarray:
    t = t_axis(seconds)
    ir = lowpass(RNG.normal(0, 1, len(t)), 4500) * np.exp(-t * decay)
    ir[0] = 0
    return ir / np.sqrt(np.sum(ir ** 2))


def convolve(x: np.ndarray, ir: np.ndarray) -> np.ndarray:
    n = len(x) + len(ir)
    size = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[: len(x)]


def master(x: np.ndarray, reverb: float) -> np.ndarray:
    if reverb > 0:
        x = x + reverb * convolve(x, reverb_ir())
    return lowpass(x, 7500)


def finish(x: np.ndarray, peak=0.89) -> np.ndarray:
    """Soft-clip, normalise, then a touch of GBA grit (10-bit steps)."""
    x = np.tanh(x * 1.2) / np.tanh(1.2)
    x = x / max(1e-9, np.max(np.abs(x))) * peak
    return np.round(x * 512) / 512
