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