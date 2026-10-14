# CLAUDE.md

This file provides context, execution rules, and architectural guidelines for Claude Code (claude.ai/code) when working in this repository.

See `GDD.md` for the game design and system architecture (core loop, systems, data flow, folder layout).
See `IMPLEMENTATION.md` for the technical implementation plan and milestone build order (M0–M6).
See `docs/NEXT_STEPS.md` first: where the last session stopped and how to resume.
See `docs/story/` for the story canon: `STORY.md` (world, crests, characters, plot), `CHAPTERS.md` (every mission's map, objectives, units and triggers) and `DIALOGUE.md` (script format, character voices, scripts). New chapters, units and dialogue must follow these.

---

## 1. Project & Technical Stack

- **Game Concept:** "Not Fire Emblem" — Tactical 2.5D / 3D Grid-Based RPG.
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
`--map=<id>` picks the battle map (`prologue` by default; also `ch1` and the systems test map `test_ford`), `--seed` fixes the battle RNG, `--speed` sets `Engine.TimeScale`, `--press` injects input actions at given frames, and `--screenshot` saves the viewport at `--frames` and quits. The battle prints one log line per `BattleEvent`.

---

## 5. Art Pipeline (PixelLab)

Art is generated with the PixelLab API by `tools/art/generate.py` (prompts and asset list in `tools/art/manifest.py`). The token lives in a gitignored `pixellab.env` (or `.env`) (`PIXELLAB_API_TOKEN=...`, see `.env.example`). `tools/art/state.json` records every request so re-runs never pay twice; commit it. Run `plan` (free) before `run`, and pilot new prompts with `run --pilot` or `run --only <id>`. Output goes to `assets/sprites/units/<id>/` (map sprites), `assets/sprites/battle/<id>/` (side-view battle-screen sprites, `--set battle`, east-facing only and mirrored in game), `assets/portraits/` and `assets/textures/terrain/`. Each unit finds its art through `BattleUnit.SpriteCandidates()`: `<spriteId>_<classId>` first, then `<spriteId>` (where the sprite id is the spawn's `SpriteId`, else the unit id, else `enemy_<classId>`). Outfits follow class tier: playable characters start in plain country clothes and get grander per tier (`OUTFITS` in `tools/art/manifest.py`; higher tiers are marked `planned` until their classes exist).
