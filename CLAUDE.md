# CLAUDE.md

This file provides context, execution rules, and architectural guidelines for Claude Code (claude.ai/code) when working in this repository.

See `GDD.md` for the game design and system architecture (core loop, systems, data flow, folder layout).
See `IMPLEMENTATION.md` for the technical implementation plan and milestone build order (M0–M6).

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
`--seed` fixes the battle RNG, `--speed` sets `Engine.TimeScale`, `--press` injects input actions at given frames, and `--screenshot` saves the viewport at `--frames` and quits. The battle prints one log line per `BattleEvent`.
