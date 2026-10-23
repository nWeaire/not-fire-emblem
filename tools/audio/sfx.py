"""The game's sound effects, synthesized for synth.py's 22050 Hz GBA-flavoured output.

Gameplay sounds (menus, swings, hits, spells) plus the story cues the scripts ask for (@sfx gloam_howl ...).
"""
import numpy as np

from synth import SR, bandpass, bell, harp, highpass, hz, lowpass, midi, noise, t_axis


def sweep(f0, f1, seconds, curve=1.0):
    """A sine whose pitch glides from f0 to f1 (curve > 1 glides late, < 1 early)."""
    t = t_axis(seconds)
    f = f0 + (f1 - f0) * (t / seconds) ** curve
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def decay(seconds, rate, attack=0.003):
    t = t_axis(seconds)
    return np.exp(-t * rate) * np.minimum(1, t / attack)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[: len(p)] += p
    return out


def whoosh(seconds, lo, hi, falling=True):
    """Air movement: noise band-passed through a moving window, built from short overlapping slices."""
    n = noise(seconds)
    out = np.zeros_like(n)
    slices = 12
    size = len(n) // slices
    for i in range(slices):
        k = i / (slices - 1)
        centre = (hi + (lo - hi) * k) if falling else (lo + (hi - lo) * k)
        seg = n[i * size:(i + 2) * size]
        seg = bandpass(seg, centre * 0.6, centre * 1.4) * np.hanning(len(seg))
        out[i * size:i * size + len(seg)] += seg
    t = t_axis(seconds)
    return out * np.sin(np.pi * t / seconds) ** 0.7


# ------------------------------------------------------------------ interface

def cursor():
    return mix(np.sin(2 * np.pi * 1500 * t_axis(0.05)) * decay(0.05, 80), np.sin(2 * np.pi * 3000 * t_axis(0.03)) * decay(0.03, 120) * 0.3) * 0.5


def confirm():
    a = np.sin(2 * np.pi * 880 * t_axis(0.07)) * decay(0.07, 30)
    b = np.sin(2 * np.pi * 1320 * t_axis(0.12)) * decay(0.12, 25)
    return np.concatenate([a, b]) * 0.5


def cancel():
    a = np.sin(2 * np.pi * 660 * t_axis(0.07)) * decay(0.07, 30)
    b = np.sin(2 * np.pi * 440 * t_axis(0.12)) * decay(0.12, 25)
    return np.concatenate([a, b]) * 0.5


def select():
    return bell(hz(midi("C6")), 0, 0.9)[: int(0.5 * SR)] * decay(0.5, 6)


def step():
    return lowpass(noise(0.06), 900) * decay(0.06, 60) * 0.6


def phase_change():
    return mix(*(bell(hz(midi(n)), 0, 0.7) for n in ("D5", "F#5", "A5", "D6")), lowpass(noise(1.6), 2000) * decay(1.6, 6) * 0.05)


def visit():
    return np.concatenate([bell(hz(midi("E6")), 0, 0.8)[: int(0.15 * SR)], bell(hz(midi("A6")), 0, 0.8)])


def dialogue_blip():
    return np.sin(2 * np.pi * 700 * t_axis(0.03)) * decay(0.03, 90) * 0.3


# ------------------------------------------------------------------ combat

def sword_swing():
    return whoosh(0.2, 1200, 5000) * 0.9


def axe_swing():
    return whoosh(0.3, 500, 2200) * 1.0


def lance_thrust():
    return mix(whoosh(0.16, 1500, 4000, falling=False) * 0.8, highpass(noise(0.2), 5000) * decay(0.2, 25) * 0.15)


def miss():
    return whoosh(0.25, 2000, 6000) * 0.5


def hit():
    thump = sweep(160, 60, 0.18) * decay(0.18, 22)
    crack = bandpass(noise(0.12), 800, 5000) * decay(0.12, 40)
    ring = np.sin(2 * np.pi * 2100 * t_axis(0.2)) * decay(0.2, 30) * 0.15
    return mix(thump * 0.9, crack * 0.8, ring)


def hit_heavy():
    thump = sweep(200, 45, 0.35) * decay(0.35, 10)
    crack = bandpass(noise(0.25), 500, 6000) * decay(0.25, 18)
    ring = mix(np.sin(2 * np.pi * 1700 * t_axis(0.4)), np.sin(2 * np.pi * 2550 * t_axis(0.4)) * 0.6) * decay(0.4, 12) * 0.2
    return mix(thump, crack, ring) * 1.1


def critical():
    """The crit wind-up: a rising shimmer."""
    shimmer = mix(*(sweep(f, f * 2.2, 0.45, 2) * 0.25 for f in (600, 900, 1350)))
    return shimmer * np.linspace(0.2, 1, len(shimmer)) * decay(0.45, 0.5) + highpass(noise(0.45), 6000) * np.linspace(0, 0.15, int(0.45 * SR))


def kill():
    """A unit falls: a soft falling tone and a scatter of sparkle."""
    fall = sweep(700, 180, 0.6, 0.6) * decay(0.6, 4) * 0.5
    sparkle = highpass(noise(0.6), 6000) * decay(0.6, 6) * 0.15
    return mix(fall, sparkle)


def heal():
    notes = ("C6", "E6", "G6", "C7")
    out = np.zeros(int(0.9 * SR))
    for i, n in enumerate(notes):
        b = bell(hz(midi(n)), 0, 0.6)[: int(0.6 * SR)]
        start = int(i * 0.07 * SR)
        out[start:start + len(b)] += b[: len(out) - start]
    return out


def bow_shot():
    """A sharp, high string snap and the arrow's whoosh. (A low A2 twang read as a drone; keep it all above 600 Hz.)"""
    snap = harp(hz(midi("E6")), 0.05, 1.0)[: int(0.25 * SR)] * decay(0.25, 18)
    return highpass(mix(snap * 0.8, whoosh(0.22, 2500, 7000) * 0.6), 600)


def fire_cast():
    roar = lowpass(noise(0.9), 1200) * np.sin(np.pi * t_axis(0.9) / 0.9) ** 0.5
    crackle = highpass(noise(0.9), 3000) * (np.random.default_rng(3).random(int(0.9 * SR)) > 0.97) * 3
    return mix(roar * 0.9, crackle * decay(0.9, 2) * 0.4)


def dark_cast():
    t = t_axis(1.0)
    drone = (np.sin(2 * np.pi * 110 * t * (1 - 0.15 * t)) + np.sin(2 * np.pi * 116.5 * t * (1 - 0.15 * t))) * 0.4
    whisper = bandpass(noise(1.0), 1500, 4000) * 0.25
    return mix(drone, whisper) * np.sin(np.pi * t / 1.0) ** 0.6


def claw():
    a = whoosh(0.14, 1500, 5000)
    gap = np.zeros(int(0.05 * SR))
    scratch = highpass(noise(0.08), 3000) * decay(0.08, 40) * 0.5
    return np.concatenate([a, gap, a * 0.9, scratch])


# ------------------------------------------------------------------ story cues

def crest_rise():
    t = t_axis(1.8)
    choir = mix(*(np.sin(2 * np.pi * f * t) * 0.18 for f in (hz(midi("D5")), hz(midi("A5")), hz(midi("D6")), hz(midi("F#6")))))
    rise = sweep(400, 1600, 1.8, 1.5) * 0.15
    sparkle = highpass(noise(1.8), 7000) * 0.08
    return mix(choir, rise, sparkle) * np.minimum(1, t / 0.8) * np.exp(-np.maximum(0, t - 1.2) * 4)


def crest_burn():
    t = t_axis(1.3)
    sizzle = highpass(noise(1.3), 2500) * 0.4
    swell = (np.sin(2 * np.pi * 220 * t) + np.sin(2 * np.pi * 233 * t)) * 0.3
    return mix(sizzle, swell) * np.minimum(1, t / 0.15) * np.exp(-t * 1.6)


def floorboards():
    t = t_axis(0.7)
    f = 140 + 40 * np.sin(2 * np.pi * 3 * t) + 25 * np.sin(2 * np.pi * 7.3 * t)
    creak = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * 0.25
    return lowpass(creak, 1500) * np.sin(np.pi * t / 0.7) ** 2 + lowpass(noise(0.7), 600) * 0.05


def gloam_howl():
    t = t_axis(1.8)
    f = np.interp(t, [0, 0.4, 1.2, 1.8], [330, 620, 560, 380]) * (1 + 0.012 * np.sin(2 * np.pi * 6 * t))
    phase = 2 * np.pi * np.cumsum(f) / SR
    voice = sum(np.sin(k * phase) / k for k in range(1, 7))
    voice = bandpass(voice, 300, 2600)
    return (voice * 0.6 + bandpass(noise(1.8), 500, 1800) * 0.08) * np.minimum(1, t / 0.25) * np.minimum(1, (1.8 - t) / 0.5)


def gloam_roar():
    t = t_axis(1.3)
    f = 75 + 20 * np.sin(2 * np.pi * 2 * t)
    growl = sum(np.sin(k * 2 * np.pi * np.cumsum(f) / SR) / k for k in range(1, 12))
    rough = 1 + 0.6 * np.sin(2 * np.pi * 31 * t)
    return lowpass(growl * rough * 0.5 + lowpass(noise(1.3), 900) * 0.4, 2200) * np.minimum(1, t / 0.08) * np.exp(-t * 1.4)


def sword_fall():
    """Ch 15: a sword dropped on a stone floor, far off: a bright clang, two smaller bounces, a long ring."""
    out = np.zeros(int(SR * 1.6))
    for start, vel in ((0.0, 1.0), (0.22, 0.45), (0.36, 0.25)):
        t = t_axis(1.2)
        ring = mix(np.sin(2 * np.pi * 1870 * t), np.sin(2 * np.pi * 2810 * t) * 0.6, np.sin(2 * np.pi * 4230 * t) * 0.3)
        clang = ring * decay(1.2, 3.5) * 0.35 + highpass(noise(1.2), 3000) * decay(1.2, 40) * 0.5
        s = int(start * SR)
        out[s:s + len(clang)] += (clang * vel)[: len(out) - s]
    return lowpass(out, 6000) * 0.8


def crest_corrupt():
    """Ch 15: Rowan's crest flickers purple: the crest's rising choir, soured by a beating, sinking drone."""
    t = t_axis(2.0)
    choir = mix(*(np.sin(2 * np.pi * f * t) * 0.16 for f in (hz(midi("D5")), hz(midi("A5")), hz(midi("D6")))))
    sour = (np.sin(2 * np.pi * 146.8 * t * (1 - 0.1 * t)) + np.sin(2 * np.pi * 155.6 * t * (1 - 0.1 * t))) * 0.35
    whisper = bandpass(noise(2.0), 1200, 3500) * 0.12
    return mix(choir * np.exp(-t * 1.2), sour * np.minimum(1, t / 0.6), whisper) * np.minimum(1, t / 0.1) * np.exp(-np.maximum(0, t - 1.4) * 3)


def wing_beat():
    """Wyverns overhead (Ch 10): three heavy, slowing gusts, each a falling whoosh over a leathery thump."""
    out = np.zeros(int(SR * 1.5))
    for i, start in enumerate((0.0, 0.45, 0.95)):
        seg = whoosh(0.5, 110, 650) * (0.9 - 0.2 * i) + lowpass(noise(0.5), 180) * decay(0.5, 16) * 0.7
        s = int(start * SR)
        out[s:s + len(seg)] += seg[:len(out) - s]
    return out * 0.8


SFX = {f.__name__: f for f in (cursor, confirm, cancel, select, step, phase_change, visit, dialogue_blip,
                               sword_swing, axe_swing, lance_thrust, miss, hit, hit_heavy, critical, kill, heal,
                               bow_shot, fire_cast, dark_cast, claw, crest_rise, crest_burn, floorboards,
                               gloam_howl, gloam_roar, wing_beat, sword_fall, crest_corrupt)}
