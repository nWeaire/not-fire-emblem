# CLAUDE.md

This file provides context, execution rules, and architectural guidelines for Claude Code (claude.ai/code) when working in this repository.

See `GDD.md` for the game design and system architecture (core loop, systems, data flow, folder layout).
See `IMPLEMENTATION.md` for the technical implementation plan and milestone build order (M0–M6).
See `docs/NEXT_STEPS.md` first: where the last session stopped and how to resume.
See `docs/ART_DIRECTION.md` for the approved art style (GBA look, sizes, PixelLab recipes, battle effects, crests). New art must follow it.
See `docs/story/` for the story canon: `STORY.md` (world, crests, characters, plot), `CHAPTERS.md` (every mission's map, objectives, units and triggers) and `DIALOGUE.md` (script format, character voices, scripts). New chapters, units and dialogue must follow these.

---

## 1. Project & Technical Stack

- **Game:** "The Radiant Crest" (working title until session 5: "Not Fire Emblem", which the solution, assembly and `NotFireEmblem` namespaces still use) — Tactical 2.5D / 3D Grid-Based RPG.
- **Engine:** Godot **4.7.2 Stable (Mono / .NET Build)**.
- **Primary Language:** **C# (.NET 8.0)**.
- **Physics Engine:** Godot Jolt (3D Physics).
- **Renderer:** Forward Plus with D3D12 (Windows).
- **Display Stretch:** `canvas_items` mode with `expand` aspect.
- **Editor Binary Path:**
  `C:\Users\nweai\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`

---

## 2. Terminal Commands & Build Workflows

When editing C# code, ALWAYS verify compilation with `dotnet build` before completing a task.

- **Build C# Solution:** `dotnet build` or `godot --headless --path . --build-solutions --quit`
- **Headless Validation & Asset Reimport:** `godot --headless --path . --import`
- **Open Godot Editor:** `godot --path . -e`
- **Run Game Project:** `godot --path .` (Requires `application/run/main_scene` configured in `project.godot`)
- **Run Specific Scene:** `godot --path . res://path/to/scene.tscn`

---

## 3. Architecture & Code Conventions

### Godot 4 C# Guidelines
- **Framework Target:** Godot 4.x (.NET 8). Do NOT generate Godot 3.x or Unity C# code.
- **Namespace & Classes:**
  - Standard scripts inheriting from Godot classes must use `using Godot;` and `[GlobalClass]` where applicable.
  - Custom data structures, interfaces, and algorithms (e.g., A* pathfinding, turn queues) should be pure C# classes where possible to decouple them from node trees.
- **Inspector Variables:** Use `[Export]` attribute syntax:
  ```csharp
  [Export] public int MoveRange { get; set; } = 5;
  [Export] public float MoveSpeed { get; set; } = 4.0f;
  ```

### Project Conventions (see IMPLEMENTATION.md §2, §9)
- Pure logic folders (`src/<System>/`) must never contain `using Godot;`. Engine code goes in `src/<System>/Nodes/` or `src/<System>/Resources/`. The test project compiles everything else, so a leak breaks the test build.
- Godot scripts are `partial`, and the file name matches the class name. Resources use `[GlobalClass]`. Node references use `[Export]` or `GetNode<T>("%UniqueName")`.
- Namespaces follow the system folder, not the subfolder (`NotFireEmblem.Grid`, not `NotFireEmblem.Grid.Nodes`).
- Battle state changes only through `TurnManager.Submit`. Visuals change only in response to `BattleEvent`s.
- No `System.Random` and no `DateTime` in logic. All randomness goes through `BattleState.Rng`.
- Content is referenced by string `Id`, never by resource path, in logic and in saves.
- Input actions are registered in code by the `InputSetup` autoload, not in `project.godot`.
- Content lives in `data/`: `.tres` resources per type (`classes/ units/ weapons/ armor/ items/ arts/ spells/ shops/ terrain/ maps/`), plus two plain-text formats parsed by pure code:
  - `data/dialogue/*.txt`: scenes in the `docs/story/DIALOGUE.md` script format (`DialogueParser`).
  - `data/campaign/main.txt`: the story as ordered steps: scene, battle, join, crest, hub, end (`CampaignParser`).
- Map objectives and events are text lines in each `MapDefinition`: `Victory`, `Defeat` and `Triggers`, e.g. `turn 2 => scene pro_aldric_reveal; join aldric`, plus `Highlights` (`19,10-19,11 => caption` or `KEY => caption`) for points of interest the objective tour shows before turn 1. The grammar is documented on `MapScript` in `src/Battle/MapRules.cs`. Unit keys are a spawn's `Tag`, else its `UnitId`.
- Art still needed (placeholders today) is listed in `docs/ART_NEEDED.md`.

---

## 4. Verification Commands

```
dotnet build "Not Fire Emblem.sln"                                                # compile game + tests
dotnet test tests/NotFireEmblem.Tests                                             # all unit tests
dotnet test tests/NotFireEmblem.Tests --filter "FullyQualifiedName~Pathfinder"    # one test class
godot --headless --path . --import                                                # reimport + script errors
godot --headless --path . --quit-after 120 res://scenes/battle/Battle.tscn        # smoke-run a scene ~2s
```
Use the `_console.exe` variant of the editor binary for headless runs so output reaches the terminal.

**Dev flags** (the `DevTools` autoload; pass after `--`), for checking visuals and flows without a human at the keyboard:
```
godot --path . --resolution 1280x720 -- --seed=123 --speed=4 --press=100:end_turn,500:confirm --screenshot=out.png --frames=900
```
`--map=<id>` jumps straight into a single battle (`test_ford`, `prologue`, `ch1` … `ch14`), `--seed` fixes the battle RNG, `--speed` sets `Engine.TimeScale`, `--press` injects input actions at given frames, and `--screenshot` saves the viewport at `--frames` and quits. `--focus=X,Y` starts the camera on a cell and `--hideunits` hides units and HUD (map-art screenshots). `--level=N` (or `--level=auto`, the map's campaign level: Prologue 1, Ch 1 2 … Ch 7 8, Ch 8 9, Ch 9 10, Ch 10 11, Ch 11 12, Ch 12 13, Ch 13 14, Ch 14 15) raises the single-battle party to that level with average gains, promoting at 10 whenever the crest allows (Act II units are tier 2), so a `--map` test plays like the campaign; use it for balance checks. Single battles also use the campaign's crest tier for that map. Single battles also apply the campaign's `class` steps before that map (Rowan is a Lord after Ch 1) and, after Ch 1, give Mira her Fire and Rowan a Steel Sword. The battle prints one log line per `BattleEvent`.
Campaign flags: `--newgame` / `--load=N` / `--resume` (the suspended battle) start from the title screen (`--autoplay` new games are Casual); `--skipdialogue`, `--autoadvance` (prep, results and hub continue by themselves) and `--autoplay` (the AI plays the player's side). Together they soak-test the whole campaign:
```
godot --path . -- --newgame --autoplay --skipdialogue --autoadvance --speed=10     # prints AUTOPLAY RESULT lines and CAMPAIGN COMPLETE
```

---

## 5. Art Pipeline (PixelLab)

Art is generated with the PixelLab API by `tools/art/generate.py` in the approved GBA style (prompts and asset list in `tools/art/manifest.py`; the recipes are in `docs/ART_DIRECTION.md` §2). The token lives in a gitignored `pixellab.env` (or `.env`) (`PIXELLAB_API_TOKEN=...`, see `.env.example`). `tools/art/state.json` records every request (under `"gba"`) so re-runs never pay twice; commit it. Only run one `generate.py` at a time: each run rewrites the whole state file. Run `plan` (free) first, and pilot new prompts with `chars --only <id> --set map --no-anims` before a batch. Commands: `chars` (map 48 px + battle 64 px sprites and their animations), `portraits` (4 takes each into `tools/art/takes/portraits/`; copy the chosen one to `assets/portraits/<id>.png`), `backgrounds` (`assets/backgrounds/<id>.png`, used by story scenes and the battle screen), `tiles` (32 px sets into `tools/art/takes/tiles/`), and `download`. Battle sprites in the newer "E" look are animated from a chosen still with `tools/art/animate_still.py` (ART_DIRECTION §2). Output goes to `assets/sprites/units/<id>/` (map sprites), `assets/sprites/battle/<id>/` (side-view battle-screen sprites, east-facing only and mirrored in game), `assets/portraits/`, `assets/backgrounds/` and `assets/textures/terrain/`. Superseded art (session 2 and 4) was deleted in session 7; recover it from git history if ever needed. Each unit finds its art through `BattleUnit.SpriteCandidates()`: `<spriteId>_<classId>` first, then `<spriteId>` (where the sprite id is the spawn's `SpriteId`, else the unit id, else `enemy_<classId>`). Outfits follow class tier: playable characters start in plain country clothes and get grander per tier (`PLANNED` in `tools/art/manifest.py` lists the tier-2 looks still to generate).

## 6. Audio

Music and sound effects are synthesized by `tools/audio/build.py` (GBA-style sampled-orchestra voices in `synth.py`,
tracks in `songs.py`, effects in `sfx.py`; `pip install -r tools/audio/requirements.txt`). Output:
`assets/audio/music/<id>.ogg` and `assets/audio/sfx/<id>.wav`. The `AudioDirector` autoload plays them by id
(`AudioDirector.PlayMusic("hub")`, `AudioDirector.PlaySfx("hit")`); `AudioCues` picks ids for game moments. Dialogue
scripts use `@music <id>` and `@sfx <id>`.
