"""The game's music, composed as code for synth.py. Each function returns (Song, loops).

Ids match what the game asks for: story scripts (@music calm_farm ...), plus title, hub, prep, the battle phases and
the victory / defeat / level-up jingles.
"""
from synth import Song


def title():
    """The Radiant Crest: the main theme. Heroic, D major, brass over strings."""
    s = Song(92, 16)
    ch = ["D", "A", "Bm", "G", "D", "A", "G", "A", "Bm", "G", "D", "A", "G", "A", "D", "D"]
    s.melody("A4:1.5 D5:.5 F#5:1 E5:1 | E5:1.5 C#5:.5 A4:2 | B4:1.5 D5:.5 F#5:1 A5:1 | G5:2 F#5:1 E5:1 |"
             "F#5:1.5 E5:.5 D5:1 A4:1 | C#5:1 E5:1 A5:2 | B5:1 A5:1 G5:1 F#5:1 | E5:3 r:1 |"
             "F#5:1.5 G5:.5 A5:1 F#5:1 | D5:1.5 E5:.5 G5:2 | F#5:1 E5:1 D5:1 F#5:1 | E5:2 C#5:2 |"
             "D5:1 E5:1 G5:1 B5:1 | A5:2 G5:1 E5:1 | D5:4 | r:2 A4:.5 B4:.5 C#5:1", "brass", 0.85)
    s.melody("A4:1.5 D5:.5 F#5:1 E5:1 | E5:1.5 C#5:.5 A4:2 | B4:1.5 D5:.5 F#5:1 A5:1 | G5:2 F#5:1 E5:1 |"
             "F#5:1.5 E5:.5 D5:1 A4:1 | C#5:1 E5:1 A5:2 | B5:1 A5:1 G5:1 F#5:1 | E5:3 r:1 |"
             "F#5:1.5 G5:.5 A5:1 F#5:1 | D5:1.5 E5:.5 G5:2 | F#5:1 E5:1 D5:1 F#5:1 | E5:2 C#5:2 |"
             "D5:1 E5:1 G5:1 B5:1 | A5:2 G5:1 E5:1 | D5:4 | r:2 A4:.5 B4:.5 C#5:1", "strings", 0.4, transpose=-12)
    s.pad(ch, "strings", 4, 0.4)
    s.pad(ch, "choir", 4, 0.5)
    s.bassline(ch, ((0, 1.5), (1.5, .5), (2, 2)), fifth_on=(2,))
    s.arp(ch, "harp", 4, 0.5, (0, 1, 2, 3), 0.35)
    s.drums("T...s.......s.s.", 16, vel=0.5)
    for bar in (0, 8):
        s.drums("c...............", 1, start_bar=bar, vel=0.5)
    return s, True


def calm_farm():
    """Ashford farm at evening: G major flute with harp and pizzicato."""
    s = Song(84, 16)
    ch = ["G", "C", "G", "D", "Em", "C", "Am", "D", "G", "C", "G", "Em", "C", "D", "G", "G"]
    s.melody("D5:1 G5:1 B5:1.5 A5:.5 | G5:1 E5:1 C5:2 | D5:1 G5:1 B5:1 D6:1 | C6:1.5 B5:.5 A5:2 |"
             "B5:1 G5:1 E5:1 G5:1 | A5:1.5 G5:.5 E5:2 | C5:1 E5:1 A5:1 G5:1 | F#5:3 r:1 |"
             "D5:1 G5:1 B5:1.5 A5:.5 | G5:1 E5:1 C6:2 | B5:1 A5:1 G5:1 D5:1 | E5:1 G5:1 B5:2 |"
             "C6:1.5 B5:.5 A5:1 G5:1 | A5:1 B5:1 A5:1 F#5:1 | G5:4 | r:4", "flute", 0.75)
    s.arp(ch, "harp", 4, 0.5, (0, 1, 2, 1), 0.4)
    s.bassline(ch, ((0, 1), (2, 1)), "pizz", 3, 0.6, fifth_on=(2,))
    s.pad(ch, "strings", 3, 0.22)
    s.drums("..h...h...h...h.", 16, vel=0.35)
    return s, True


def hub():
    """Brookhollow's village hall: warm F major strings over harp and choir."""
    s = Song(88, 12)
    ch = ["F", "Dm", "Bb", "C", "F", "Am", "Bb", "C", "Dm", "Bb", "C", "F"]
    s.melody("C5:1 F5:1 A5:2 | G5:1 F5:1 D5:2 | D5:1 F5:1 Bb5:1.5 A5:.5 | G5:3 r:1 |"
             "A5:1 G5:1 F5:1 C5:1 | E5:1.5 F5:.5 A5:2 | Bb5:1 A5:1 G5:1 F5:1 | E5:2 G5:2 |"
             "F5:1 A5:1 D6:2 | C6:1 Bb5:1 A5:1 G5:1 | E5:1 G5:1 C6:1 Bb5:1 | A5:3 r:1", "strings", 0.8)
    s.arp(ch, "harp", 4, 0.5, (0, 2, 1, 3), 0.38)
    s.pad(ch, "choir", 4, 0.45)
    s.bassline(ch, ((0, 2), (2, 2)), octave=2, vel=0.55)
    return s, True


def prep():
    """Battle preparations: a steady, determined Bb major march (shares the march's tune, lighter)."""
    s = Song(100, 8)
    ch = ["Bb", "F", "Gm", "Eb", "Bb", "F", "Eb", "F"]
    s.melody("F4:1 Bb4:1 D5:1.5 C5:.5 | C5:1 A4:1 F4:2 | G4:1 Bb4:1 D5:1 G5:1 | F5:1.5 Eb5:.5 C5:2 |"
             "D5:1 F5:1 Bb5:2 | A5:1 G5:1 F5:2 | G5:1 F5:1 Eb5:1 C5:1 | D5:1 C5:1 A4:2", "flute", 0.7)
    s.arp(ch, "harp", 4, 0.5, (0, 1, 2, 1), 0.35)
    s.bassline(ch, ((0, 1), (1, 1), (2, 1), (3, 1)), "pizz", 2, 0.55, fifth_on=(1, 3))
    s.drums("s.......s.......", 8, vel=0.3)
    return s, True


def tension_low():
    """Something is wrong: slow D minor, low strings, sparse harp, soft timpani."""
    s = Song(68, 8)
    ch = ["Dm", "Dm", "Bb", "A", "Dm", "Gm", "Bb", "A"]
    s.melody("A4:4 | Bb4:2 A4:2 | F4:4 | E4:3 r:1 | A4:2 C5:2 | Bb4:2 G4:2 | F4:2 D4:2 | C#4:4", "strings", 0.6)
    s.pad(ch, "strings", 3, 0.35)
    s.pad(ch, "choir", 3, 0.3)
    s.bassline(ch, ((0, 4),), octave=1, vel=0.6)
    s.arp(ch, "harp", 3, 1.0, (0, 2, 1, 2), 0.3)
    s.drums("T...............", 8, vel=0.35)
    return s, True


def tension_high():
    """Danger close: E minor string ostinato, brass melody, driving drums."""
    s = Song(108, 8)
    ch = ["Em", "C", "Am", "B", "Em", "C", "Am", "B"]
    s.melody("E5:1.5 F#5:.5 G5:2 | E5:1 G5:1 C6:2 | B5:1 A5:1 G5:1 E5:1 | F#5:3 D#5:1 |"
             "E5:1.5 G5:.5 B5:2 | C6:1 B5:1 G5:2 | A5:1 G5:1 F#5:1 E5:1 | D#5:2 F#5:2", "brass", 0.8)
    s.arp(ch, "strings", 3, 0.5, (0, 0, 2, 3), 0.45)
    s.bassline(ch, ((0, .5), (.5, .5), (1, .5), (1.5, .5), (2, .5), (2.5, .5), (3, .5), (3.5, .5)), octave=2, vel=0.5)
    s.drums("k...s..kk...s...", 8, vel=0.6)
    s.drums("h.h.h.h.h.h.h.h.", 8, vel=0.4)
    return s, True


def battle_urgent():
    """Boss and crisis battles: C minor at 138 bpm, brass lead over a string ostinato."""
    s = Song(138, 16)
    ch = ["Cm", "Ab", "Bb", "G", "Cm", "Ab", "Fm", "G", "Ab", "Bb", "Cm", "Ab", "Fm", "Bb", "G", "G"]
    s.melody("C5:1 Eb5:1 G5:2 | Ab5:1.5 G5:.5 Eb5:2 | F5:1 G5:1 Bb5:2 | B4:2 D5:2 |"
             "C5:1 Eb5:1 G5:1 C6:1 | Bb5:1 Ab5:1 G5:1 Eb5:1 | F5:1 Ab5:1 C6:1 Ab5:1 | G5:3 r:1 |"
             "Eb5:1 Ab5:1 C6:2 | D6:1 C6:1 Bb5:2 | G5:1 C6:1 Eb6:2 | D6:1 C6:1 Ab5:2 |"
             "F5:1 Ab5:1 C6:1 F6:1 | Eb6:1 D6:1 Bb5:2 | B5:1 D6:1 G6:2 | F6:1 D6:1 B5:1 G5:1", "brass", 0.85)
    s.arp(ch, "strings", 3, 0.5, (0, 2, 0, 2, 0, 2, 3, 2), 0.45)
    s.bassline(ch, ((0, .5), (.5, .5), (1, .5), (1.5, .5), (2, .5), (2.5, .5), (3, .5), (3.5, .5)), octave=2,
               vel=0.55, fifth_on=(1.5, 3.5))
    s.drums("k.s.k.s.kks.k.s.", 16, vel=0.65)
    s.drums("hhhhhhhhhhhhhhhh", 16, vel=0.25)
    for bar in (0, 8):
        s.drums("c...............", 1, start_bar=bar, vel=0.6)
    s.drums("T.......T.......", 16, vel=0.4)
    return s, True


def aftermath():
    """After the fire: slow A minor, solo strings and choir."""
    s = Song(64, 8)
    ch = ["Am", "F", "C", "G", "Am", "Dm", "E", "Am"]
    s.melody("E5:2 C5:1 B4:1 | A4:3 C5:1 | E5:2 G5:1 E5:1 | D5:4 | C5:2 B4:1 A4:1 | F4:2 A4:1 D5:1 | B4:2 G#4:2 | A4:4",
             "strings", 0.7)
    s.pad(ch, "choir", 4, 0.45)
    s.pad(ch, "strings", 3, 0.25)
    s.bassline(ch, ((0, 4),), octave=2, vel=0.45)
    s.arp(ch, "harp", 4, 1.0, (0, 1, 2, 3), 0.25)
    return s, True


def loss():
    """Ch 15, Aldric's door: a slow D minor lament, solo strings over choir, the bass barely moving."""
    s = Song(56, 8)
    ch = ["Dm", "Bb", "F", "C", "Dm", "Gm", "A", "Dm"]
    s.melody("A4:2 F4:1 D4:1 | D5:3 C5:1 | A4:2 C5:1 A4:1 | G4:4 | F4:2 G4:1 A4:1 | Bb4:2 D5:1 G4:1 | A4:2 C#5:2 | D5:4",
             "strings", 0.7)
    s.pad(ch, "choir", 4, 0.5)
    s.bassline(ch, ((0, 4),), octave=2, vel=0.4)
    return s, True


def march():
    """The road out: Bb major brass march with snare."""
    s = Song(104, 8)
    ch = ["Bb", "F", "Gm", "Eb", "Bb", "F", "Eb", "F"]
    s.melody("F4:1 Bb4:1 D5:1.5 C5:.5 | C5:1 A4:1 F4:2 | G4:1 Bb4:1 D5:1 G5:1 | F5:1.5 Eb5:.5 C5:2 |"
             "D5:1 F5:1 Bb5:2 | A5:1 G5:1 F5:2 | G5:1 F5:1 Eb5:1 C5:1 | D5:1 C5:1 A4:2", "brass", 0.8)
    s.pad(ch, "strings", 4, 0.35)
    s.bassline(ch, ((0, 1), (1, 1), (2, 1), (3, 1)), octave=2, vel=0.55, fifth_on=(1, 3))
    s.drums("s.ss..s.s.ss..s.", 8, vel=0.45)
    s.drums("k.......k.......", 8, vel=0.5)
    return s, True


def forest_tension():
    """The Thornwood: E minor flute over pizzicato, harp and hand drums."""
    s = Song(90, 8)
    ch = ["Em", "D", "Em", "C", "Em", "D", "C", "B"]
    s.melody("B4:1.5 E5:.5 F#5:1 G5:1 | F#5:2 D5:2 | E5:1 G5:1 B5:1 A5:1 | G5:3 r:1 |"
             "B5:1.5 A5:.5 G5:1 E5:1 | F#5:1 A5:1 D5:2 | E5:1 C5:1 G4:2 | F#4:2 D#5:2", "flute", 0.7)
    s.bassline(ch, ((0, 1), (1.5, .5), (2, 1), (3, 1)), "pizz", 2, 0.6, fifth_on=(2,))
    s.arp(ch, "harp", 4, 0.5, (0, 2, 1, 2), 0.3)
    s.pad(ch, "strings", 3, 0.25)
    s.drums("t..t..t.t..t....", 8, vel=0.45)
    return s, True


def player_phase():
    """Player phase: a hopeful, driving D major theme."""
    s = Song(122, 16)
    ch = ["D", "G", "A", "D", "Bm", "G", "Em", "A", "D", "G", "A", "F#m", "G", "A", "D", "A"]
    tune = ("D5:1 F#5:1 A5:1.5 F#5:.5 | G5:1 B5:1 D6:2 | C#6:1 B5:1 A5:1 E5:1 | F#5:3 r:1 |"
            "D5:1 F#5:1 B5:1.5 A5:.5 | G5:1 F#5:1 E5:1 D5:1 | E5:1 G5:1 B5:1 G5:1 | A5:3 r:1 |"
            "F#5:1 A5:1 D6:1.5 C#6:.5 | B5:1 G5:1 D5:2 | E5:1 A5:1 C#6:2 | C#6:1 B5:1 A5:1 F#5:1 |"
            "G5:1 B5:1 D6:1 B5:1 | C#6:1.5 B5:.5 A5:1 E5:1 | D6:4 | r:2 A4:1 C#5:1")
    s.melody(tune, "strings", 0.75)
    s.melody(tune, "brass", 0.4, transpose=-12)
    s.pad(ch, "strings", 4, 0.3)
    s.bassline(ch, ((0, 1), (1, .5), (1.5, .5), (2, 1), (3, 1)), octave=2, vel=0.55, fifth_on=(2,))
    s.arp(ch, "harp", 4, 0.5, (0, 1, 2, 3), 0.3)
    s.drums("k...s...k.k.s...", 16, vel=0.55)
    s.drums("h.h.h.h.h.h.h.h.", 16, vel=0.3)
    return s, True


def enemy_phase():
    """Enemy phase: G minor, low brass, heavy drums."""
    s = Song(116, 8)
    ch = ["Gm", "Eb", "F", "D", "Gm", "Cm", "D", "D"]
    tune = ("G3:1 Bb3:1 D4:2 | Eb4:1.5 D4:.5 Bb3:2 | C4:1 F4:1 A4:2 | F#4:3 r:1 |"
            "G4:1 Bb4:1 D5:1 Bb4:1 | C5:1.5 Bb4:.5 G4:2 | A4:1 F#4:1 D4:2 | D4:2 F#4:1 A4:1")
    s.melody(tune, "brass", 0.85)
    s.melody(tune, "strings", 0.45, transpose=12)
    s.pad(ch, "strings", 3, 0.3)
    s.bassline(ch, ((0, .5), (.5, .5), (1.5, .5), (2, .5), (2.5, .5), (3.5, .5)), octave=1, vel=0.6)
    s.drums("k..k..s.k..k..s.", 8, vel=0.6)
    s.drums("T.......T...T...", 8, vel=0.45)
    return s, True


def victory():
    """Map cleared: a short Bb fanfare (doesn't loop)."""
    s = Song(120, 3)
    s.melody("F4:.5 F4:.5 F4:.5 Bb4:1.5 C5:1 | D5:1 C5:.5 D5:.5 F5:2 | Bb5:4", "brass", 0.9)
    s.pad(["Bb", "F", "Bb"], "strings", 4, 0.45)
    s.bassline(["Bb", "F", "Bb"], ((0, 1), (2, 2)), octave=2, vel=0.6)
    s.drums("T.T.T...T.......", 1, vel=0.6)
    s.drums("c...............", 1, start_bar=2, vel=0.6)
    return s, False


def defeat():
    """The lord has fallen: slow D minor descent (doesn't loop)."""
    s = Song(66, 3)
    s.melody("A4:2 G4:2 | F4:2 E4:2 | D4:4", "strings", 0.75)
    s.pad(["Dm", "Bb", "Dm"], "choir", 3, 0.5)
    s.bassline(["Dm", "Bb", "Dm"], ((0, 4),), octave=1, vel=0.5)
    s.drums("T...............", 1, vel=0.4)
    return s, False


def level_up():
    """Level up: a quick rising harp-and-bell flourish (doesn't loop)."""
    s = Song(140, 2)
    s.melody("C5:.25 E5:.25 G5:.25 C6:.25 E6:.25 G6:.25 C7:2.5 | r:4", "bell", 0.7)
    s.melody("C5:.25 E5:.25 G5:.25 C6:.25 E6:.25 G6:.25 C7:2.5 | r:4", "harp", 0.6, transpose=-12)
    s.pad(["C", "C"], "strings", 4, 0.3)
    return s, False


SONGS = {f.__name__: f for f in (title, calm_farm, hub, prep, tension_low, tension_high, battle_urgent, aftermath, loss,
                                 march, forest_tension, player_phase, enemy_phase, victory, defeat, level_up)}
