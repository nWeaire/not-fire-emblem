# Not Fire Emblem — Technical Implementation Plan

How every system in `GDD.md` will be coded, in what order, and how each step is verified. `GDD.md` says *what* the game is; this file says *how* it is built. Work proceeds milestone by milestone (§10). Each milestone ends in a runnable, tested state.

---

## 1. Environment & Project Setup

| Item | Decision |
|---|---|
| Engine | Godot 4.7.2 Mono (path in `CLAUDE.md`) |
| Game project | `Not Fire Emblem.csproj` (`Godot.NET.Sdk/4.7.2`, `net8.0`, `RootNamespace=NotFireEmblem`, `Nullable=enable`) |
| Test project | `tests/NotFireEmblem.Tests/` (xUnit, `net10.0`, since the machine only has the .NET 10 SDK/runtime) |
| Solution | `Not Fire Emblem.sln` containing both projects |
| Installed SDK | .NET 10.0.300 only. It can build `net8.0`. Milestone 0 checks that the Godot editor builds and runs with it. If it doesn't, install the .NET 8 runtime. |
| Version control | `git init` at the start of Milestone 0 (with the user's OK). `.gitignore` adds `bin/`, `obj/`, `.vs/`, `*.user`, `tests/**/bin`, `tests/**/obj`. |

**Commands** (added to `CLAUDE.md` in Milestone 0):
```
dotnet build "Not Fire Emblem.sln"                          # compile game + tests
dotnet test tests/NotFireEmblem.Tests                       # all unit tests
dotnet test tests/NotFireEmblem.Tests --filter "FullyQualifiedName~Pathfinder"   # one test class
godot --headless --path . --import                          # reimport + script errors
godot --headless --path . --quit-after 120 res://scenes/battle/Battle.tscn       # smoke-run a scene ~2s
```

---

## 2. The Core Architectural Rule: Pure Logic vs. Godot Layer

All game rules are **plain C# with no `using Godot;`**. The engine-facing code (nodes, resources, input, visuals) lives in `Nodes/` and `Resources/` subfolders.

```
src/<System>/            pure C#. Compiled by BOTH the game and the test project
src/<System>/Nodes/      Godot nodes (partial classes). Game project only
src/<System>/Resources/  Godot [GlobalClass] Resources. Game project only
```

- The test project compiles `src/**/*.cs` **excluding** `**/Nodes/**` and `**/Resources/**` (linked `<Compile Include>` globs). If pure code accidentally uses a Godot type, the test build fails. **The boundary enforces itself.**
- The game csproj adds `<Compile Remove="tests/**" />`, since Godot's SDK otherwise globs every `.cs` file in the folder.
- Pure code targets C# 12 / `net8.0` APIs (it is compiled for both TFMs).
- Namespaces follow the system folder, not the subfolder: `NotFireEmblem.Grid`, `NotFireEmblem.Combat`, etc.

### Logic resolves instantly; presentation replays events
This is the most important runtime pattern:

1. The UI or AI submits a command to the `TurnManager` (pure).
2. The `TurnManager` validates and executes it **synchronously**, changing `BattleState`. It returns a `CommandResult` containing an ordered list of `BattleEvent` records, such as `UnitMoved`, `StrikeResolved`, `UnitDefeated`, `LevelUp`, `DialogueRequested`, `PhaseChanged` and `BattleEnded`.
3. The Godot-side `BattleEventPlayer` plays those events one at a time with `async`/`await` (tweens, animations, popups, dialogue). Input is locked while it plays.

This keeps the rules testable without the engine and keeps animation from mixing with game state. The ordered `BattleEvent` stream **is** the GDD's EventBus for battle. A small Godot-side `EventBus` autoload carries only events that cross scenes (session changes, scene transitions).

---

## 3. Folder Layout (final)

```
src/
  Core/        Ids, StatBlock, Rng, GameSession model, SaveData, ContentDatabase
    Nodes/     GameSessionNode (autoload), SceneRouter (autoload), InputSetup (autoload), ContentLoader
  Grid/        GridPos, Cell, BattleGrid, Pathfinder, RangeService, FogOfWar
    Nodes/     MapView, CellOverlay, GridCursor, TacticsCamera
  Units/       UnitDef/ClassDef/WeaponDef… records, UnitState, BattleUnit, StatCalculator, ExpCalculator, LevelUpService
    Resources/ UnitDefinition, ClassDefinition, WeaponDefinition, ArtDefinition, SpellDefinition, ItemDefinition, TerrainDefinition
    Nodes/     UnitView
  Combat/      CombatCalculator, CombatForecast, CombatResult, Strike, WeaponTriangle, ActionChoice
  Battle/      BattleState, TurnManager, commands, BattleEvents, ObjectiveEvaluator, EventDirector, MapDef records
    Resources/ MapDefinition, SpawnDefinition, ObjectiveDefinition, TriggerDefinition (+ subclasses)
    Nodes/     BattleController, BattleEventPlayer, PlayerInputController
  AI/          AiController, behaviour profiles, ActionScorer, candidate generation
  UI/          (pure view-models where useful, e.g. ForecastViewModel)
    Nodes/     HUD panels, menus, DialoguePlayer, hub screens
  Dialogue/    DialogueScene/DialogueLine records
    Resources/ DialogueSceneResource
data/          .tres content: units/ classes/ weapons/ arts/ spells/ items/ terrain/ maps/ dialogue/
scenes/        boot/ title/ prep/ battle/ results/ hub/ ui/
assets/        models/ textures/ audio/ fonts/ (placeholders at first)
tests/NotFireEmblem.Tests/
```

---

## 4. Data & Content Pipeline

### Authoring: Godot Resources (`.tres`)
Each definition is a `[GlobalClass] public partial class XDefinition : Resource` with `[Export]` properties and a stable `[Export] public string Id`. Claude writes the `.tres` files as text, and the user can edit them in the inspector.

### Runtime: plain records
Pure code never sees Resources. At boot, `ContentLoader` (Godot) scans `res://data/<type>/` and converts each Resource into an immutable record (`UnitDef`, `ClassDef`, `WeaponDef`, `ArtDef`, `SpellDef`, `ItemDef`, `TerrainDef`, `MapDef`, `DialogueScene`). These go into a pure `ContentDatabase` (`Dictionary<string, T>` per type, looked up by Id).
- Tests build a `ContentDatabase` directly with test-data builders (`TestContent.cs`).
- **Gotcha:** in exported builds, `DirAccess` lists `X.tres.remap`. The loader strips `.remap` before calling `ResourceLoader.Load`.
- The loader validates references (unknown class/weapon ids, map rows with different lengths) and fails loudly at boot.

### Map format (refines GDD's "GridMap" visual layer)
Maps are **text grids inside `MapDefinition`**, not painted GridMaps. This makes them readable, diffable, and writable by Claude.
```
TerrainRows = ["PPPFFPPP", "PPFFMMPP", ...]   # one char per cell
HeightRows  = ["00011000", "00112100", ...]   # 0–9
(no per-map legend: each TerrainDefinition has a one-character Symbol — P plain, F forest, M mountain, W wall, T fort, ~ water, B bridge)
Spawns, DeploySlots, Objectives, Triggers, FogEnabled, VisionBonus, PlayerStartCamera
```
`MapView` builds the 3D terrain procedurally from this: one box mesh per cell, colored or textured from `TerrainDefinition`, with height in steps. It also adds a collision shape on each cell for mouse picking. Hand-painted GridMap maps can be added later as an import path.

### Core data shapes
```csharp
enum Stat { HP, Str, Mag, Dex, Spd, Lck, Def, Res, Mov, Cha }
readonly record struct StatBlock(...)            // indexer by Stat, +, clamp to caps
enum WeaponType { Sword, Lance, Axe, Bow, Fist, BlackMagic, WhiteMagic }
enum MovementType { Infantry, Armored, Cavalry, Flying }
enum Faction { Player, Enemy, Ally }             // Player and Ally are friendly
record ClassDef(Id, Name, StatBlock Bases, StatBlock Growths, StatBlock Caps, MovementType,
                WeaponType[] Usable, string[] PromotesTo, bool IsPromoted, string[] Effectiveness)
record WeaponDef(Id, Name, WeaponType, Might, Hit, Crit, Weight, MinRange, MaxRange,
                 bool Magic, string[] EffectiveAgainst, int Price)
record ArtDef(Id, Name, WeaponType, MightMod, HitMod, CritMod, RangeMod, Uses, ArtEffect[] Effects)
record SpellDef(Id, Name, Kind(Attack|Heal), Might, Hit, Crit, Weight, MinRange, MaxRange, Uses, Effects)
record ItemDef(Id, Name, Kind(Heal|Promotion|StatBoost), int Amount, string[] ValidClasses, int Price)
record TerrainDef(Id, Name, Dictionary<MovementType,int> MoveCost /*0 = impassable*/, Avoid, Def, HealPercent, BlocksSight)
record UnitDef(Id, Name, Portrait, Model, string ClassId, int Level, StatBlock Bases,
               StatBlock Growths, string[] Inventory, string[] Arts, string[] Spells,
               SupportPartner[] Supports, bool IsLord)
```

---

## 5. System Implementations

### 5.1 Core (`src/Core`)
- **`Rng`**: a small seeded PCG32 with serializable state. `Next(100)`, `Roll1(pct)`, `Roll2(pct)` (2RN: the average of two rolls is less than pct). Never `System.Random` in logic.
- **`UnitState`** (persistent, owned by the session): Id → `UnitDef`, current `ClassId`, Level, Exp, `StatBlock Gains` (level-up gains + stat boosters), `List<string> Inventory` (max 5), equipped index, known arts and spells, `Dictionary<string,int> SupportPoints`, `Dictionary<string,SupportRank> SupportRanks`, `Status` (Active / Dead / Retreated).
- **`GameSession`** (pure model): roster, convoy, gold, chapter index, `Mode` (Classic/Casual), `Difficulty`, `HashSet<string> Flags`, `HashSet<string> SeenSupports`.
- **`SaveData`**: a versioned DTO built from `GameSession`. Serialized with `System.Text.Json` (source-generated context). Pure functions `ToSave()` / `FromSave()` round-trip it.
- **Godot side**:
  - `GameSessionNode` autoload holds the current `GameSession` and `ContentDatabase`.
  - `SaveService` writes `user://saves/slot{n}.json` using `ProjectSettings.GlobalizePath`.
  - `SceneRouter` autoload: `GoTo(SceneId, object? payload)` with a fade, and a payload such as `BattleLaunch(chapterId, deployedUnitIds)`.
  - `InputSetup` autoload registers input actions in code at startup instead of hand-editing serialized InputEvents in `project.godot`: `confirm, cancel, cursor_up/down/left/right, cam_rotate_left/right, zoom_in/out, end_turn, toggle_danger, next_unit, info`. Keyboard, mouse and gamepad are all bound.

### 5.2 Grid & Movement (`src/Grid`)
- **`GridPos(int X, int Y)`**: a record struct with neighbors (4-dir) and Manhattan distance. **`BattleGrid`**: `Width`, `Height`, `Cell[,]` (TerrainDef, Height 0–9). Immutable after load.
- **Occupancy** lives in `BattleState` (`Dictionary<GridPos, BattleUnit>`), not in the grid, so there is a single source of truth.
- **`Pathfinder.GetReachable(state, unit) → ReachMap`** uses Dijkstra with `PriorityQueue<GridPos,int>`:
  - A step costs `terrain.MoveCost[unit.MoveType]`. 0 means impassable.
  - **Height:** a step is only allowed if the height difference ≤ 1. Flying ignores both rules.
  - A unit can pass through friendly units but not enemies. It can only *end* its move on an empty cell.
  - **Zone of control:** a cell next to an (enemy-visible) hostile unit can be entered, but movement can't continue from it.
  - `ReachMap` stores cost and predecessor per cell, so `PathTo(cell)` is just walking predecessors backwards. No separate A* is needed for player moves.
- **`Pathfinder.FindPath(state, unit, goal)`** uses A* with ZOC ignored and occupancy treated as passable. The AI uses it to move toward far-off goals: it walks along the path to the furthest reachable cell.
- **`RangeService`**:
  - `AttackCells(unit, reach, choice)` = every cell within `[min,max]` Manhattan range of any cell where the unit can end its move.
  - `TargetsFrom(unit, pos, choice)` lists the targets that can be hit from one cell.
  - `DangerZone(state, faction)` = the union of every hostile unit's attack cells. It is cached per phase and recalculated after each move.
- **`FogOfWar`**: `VisibleCells(state, faction)` is computed from each unit's position with radius `3 + VisionBonus` (Manhattan). Line of sight isn't modelled at first. It is recomputed after each `UnitMoved` and `UnitSpawned` event.
- **Godot side**:
  - **`MapView`** builds the terrain meshes, plus a `CellOverlay` (a MultiMesh of colored quads: blue for movement, red for attack, green for heal, purple for danger, white path arrow).
  - **`GridCursor`**: keyboard or gamepad input moves it relative to the camera's rotation, and mouse input raycasts against the cell colliders.
  - **`TacticsCamera`**: a pivot `Node3D` at the focus point with a child `Camera3D`. Yaw changes in tweened 90° steps, pitch is fixed at about 55°, distance is clamped between a min and max (zoom), and it pans smoothly to follow the cursor or the active unit. It exposes `YawStep` so the cursor can rotate its input to match.

### 5.3 Units (`src/Units`)
- **`BattleUnit`** (battle-only wrapper):
  - **Links:** `UnitState` (or a generated enemy state), `Faction`, `Pos`.
  - **Turn and AI:** `ActionState` (Ready, Moved, Done), `AiProfile`, `IsBoss`.
  - **Resources:** `CurrentHp`, `Dictionary<string,int> UsesLeft` for arts and spells, refilled at battle start.
  - **Damage and death:** `TookDamageThisBattle`, `IsDefeated`.
- Enemies are spawned from a `SpawnDefinition` (unit or generic class + level + inventory, with level-ups generated using the difficulty modifiers). They never go into the roster.
- **`StatCalculator.Effective(unit)`** = class bases + personal bases + gains, clamped to class caps. Mov comes from the class. It is the single place where stat sources are combined.
- **`StatCalculator.CombatModifiers(state, unit, pos)`** = terrain Avoid/Def, support bonuses from partners within 2 tiles (C: +5 hit/avoid; B: +10/+10, +5 crit; A: +15/+15, +10 crit), and the height bonus.
- **`ExpCalculator`** uses an FE7-style formula with the level difference, counting promoted classes as +20 levels. A kill gives a bonus, a boss gives an extra bonus, healing gives a flat amount. The minimum is 1. Units at the level cap gain none.
- **`LevelUpService.Apply(unitState, rng)`** rolls `personal growth + class growth` for each stat (with caps) and returns a `LevelUpGains` record for the popup.
- **`PromotionService`**: requires Lv ≥ 10, a matching promotion item, and that the class has `PromotesTo`. It changes the class, applies the difference in base stats as gains, and resets the level to 1. Leveling continues afterwards.
- **Godot side — `UnitView`**: a placeholder capsule mesh in the faction color (later a model) with a floating HP bar and a greyed-out "Done" tint. It exposes `async Task MoveAlong(path)`, `PlayAttack()`, `PlayHit()`, `PlayDeath()`, and is driven only by `BattleEventPlayer`.

### 5.4 Combat (`src/Combat`)
```csharp
abstract record ActionChoice;                    // WeaponAttack(weaponId) | ArtAttack(artId, weaponId) | SpellCast(spellId)
CombatForecast Forecast(BattleState s, BattleUnit atk, GridPos atkPos, BattleUnit def, ActionChoice choice);
CombatResult   Resolve(CombatForecast f, Rng rng);
HealForecast   ForecastHeal(...);  HealResult ResolveHeal(...);
```
- **Forecast**: computes AS, Atk, Damage, Hit, Crit and Crit Avoid with the GDD §3 formulas, plus `WeaponTriangle.Modifier(a,b)` (±15 hit, ±1 dmg). Effectiveness multiplies Might ×3.
- **Counters**: the defender counters with their equipped weapon (or their first offensive spell if they have no weapon) if the distance is within its range. **An art or spell used by the attacker doesn't stop the defender from countering**, unless the art has the NoCounter effect.
- The forecast stores the **strike sequence**: A, D, then the follow-up from whichever side has AS at least 4 higher. Damage, hit and crit are precomputed for each strike.
- **Resolve**: goes through the strikes and rolls `Roll2` for hit and `Roll1` for crit. It applies damage to copies of the HP values and stops when someone dies. It returns `Strike` records plus the HP after combat, deaths, and the art or spell use consumed (only one use, by the initiator, per combat).
- **Applying the result** is done by the command, not by the calculator: HP, uses, defeats, then EXP, level-up and support points through `Units` services.
- **Tests**: exact formula cases, every triangle direction, follow-up thresholds, effectiveness, out-of-range defender with no counter, deterministic resolve with a scripted `Rng`, and a 2RN distribution sanity check.

### 5.5 Battle / Turn Manager (`src/Battle`)
- **`BattleState`**:
  - **Map and content:** `BattleGrid`, `ContentDatabase`, `MapDef`.
  - **Units and occupancy:** `List<BattleUnit>` and the occupancy map.
  - **Turn tracking:** `Turn`, `Phase` (Player, Enemy, Ally), `Outcome` (InProgress, Victory, Defeat).
  - **Map rules:** `Rng`, objective state, fired trigger ids, `HashSet<string>` battle flags, fog caches.
  - **History:** `CommandLog`.
- **Commands**, all following `IBattleCommand { ValidationResult Validate(BattleState); IReadOnlyList<BattleEvent> Execute(BattleState); }`:
  - `ActCommand(unitId, path, BattleAction)` moves the unit and then acts, **atomically**. The `BattleAction` is one of `Wait`, `Attack(targetId, ActionChoice)`, `Heal(targetId, spellId)`, `UseItem(slot)`, `Seize`, `Escape`.
    - The UI shows the move visually first, and Cancel puts the unit back. Nothing changes in the logic until the player confirms an action, so **move-cancel needs no undo system.**
  - `TradeCommand(unitId, path, partnerId, swaps)` commits the move and leaves the unit in `Moved`. A later `ActCommand` with an empty path finishes its turn.
  - `EndPhaseCommand` is used for the player's "End Turn" and internally by the AI.
- **`TurnManager`**:
  - `Submit(cmd) → CommandResult`: validate, execute, append to `CommandLog`, then pass the events produced to `EventDirector` and `ObjectiveEvaluator` and append the events they generate.
  - **Phase flow:** when the last Player unit is Done, or on `EndPhaseCommand`, `AdvancePhase()` runs: Player → Enemy → Ally → Turn+1 → Player. Phases with no units are skipped.
  - **At the start of each phase:** reset that faction's action states, apply terrain healing, and fire `PhaseStarted`/`TurnStarted` triggers.
- **`ObjectiveEvaluator`**: evaluates `ObjectiveDef`s (Rout, DefeatBoss(unitId), Survive(turns), Seize(cell, lord only), Escape(cells, count/all) and the loss conditions LordDies, UnitDies(id), TurnLimit(n)) after every command and phase change. When the outcome is decided it emits `BattleEnded(Victory|Defeat)`.
- **`EventDirector`**: `TriggerDef` = `Condition` (TurnStart(n, phase), UnitEntersRegion(cells, faction), UnitDefeated(id), FlagSet(name), EnemyCountBelow(n)) + `Effects` (SpawnUnits(spawnGroup), Dialogue(sceneId), SetObjective(...), SetFlag(name)) + `Once`. Spawns that land on an occupied cell move to the nearest free cell. Its effects produce events (`UnitSpawned`, `DialogueRequested`).
- **Deaths**: in Classic, a defeated player unit's `UnitState.Status = Dead` when the battle is committed. In Casual it is `Retreated` (back afterwards). The lord's death is always a loss.
- **Godot side**:
  - **`BattleController`**: the scene root. It builds `BattleState` from the launch payload, owns the `TurnManager`, and runs the phase loop. On the Player phase it gives control to `PlayerInputController`; on AI phases it calls `AiController` one unit at a time and plays each result.
  - **`BattleEventPlayer`** has `async Task Play(IReadOnlyList<BattleEvent>)` with one handler per event type.
  - **`PlayerInputController`**: a UI state machine (`Browse → UnitSelected → MovePreview → ActionMenu → TargetSelect → ForecastConfirm`, plus `ItemMenu`, `TradeMenu`). Cancel steps back one state.
  - **After the battle:** the controller writes the results back to `GameSession` and calls `SceneRouter.GoTo(Results)`.

### 5.6 AI (`src/AI`)
- **`AiController.ChooseCommand(state, unit) → IBattleCommand`** is pure and deterministic. It gets a seeded tie-breaker from `Rng`.
- **Profiles**:
  - `Aggressive`: attack the best target in reach, otherwise path toward the nearest target.
  - `Stationary`: attack only from the current cell.
  - `GuardRegion(cells)`: idle until a hostile unit enters the region, then Aggressive.
  - `Boss`: Stationary, but may move within N cells.
  - `TargetUnit(id)` / `TargetCell(pos)`: go to the given unit or cell.
  - `Healer`: heal the most injured ally in reach, otherwise follow allies.
- **Candidate generation**: every reachable end cell × every target in range from that cell × every usable choice (weapon, art with uses left, spell).
- **Scoring with `ActionScorer`**: `w1·expectedDamage + w2·P(kill)·killValue − w3·expectedCounterDamage − w4·danger(cell) + targetPriority + terrainBonus`.
  - `targetPriority` favours low HP, healers, the lord, and units with a triangle disadvantage.
  - The weights come from `AiWeights`, a record. Hard difficulty swaps in an aggressive preset.
- **Order within a phase**: units that can attack now go first (highest score first), then everyone else. The AI recomputes after each command, because state changes.
- **Fog**: candidates and targets are filtered by the AI faction's `VisibleCells`.
- **Tests**: scenario maps built in code. The AI should take a guaranteed kill, avoid a suicidal attack when a safe one exists, stay put as a Stationary unit, wake when its guard region is entered, and heal correctly.

### 5.7 UI (`src/UI`)
All screens are Godot `Control` scenes under `scenes/ui/` with one shared `Theme` (`assets/ui/main_theme.tres`). The UI is only ever driven by `PlayerInputController` or `BattleEventPlayer` and **never changes logic state**.

- **Battle HUD**:
  - `UnitInfoPanel`: hover or selected unit, with HP, stats, weapon and terrain.
  - `TerrainPanel`.
  - `ActionMenu`: only the actions that are valid right now.
  - `ForecastPanel`: both sides' HP → HP after combat, damage ×strikes, hit and crit. It reads a `ForecastViewModel` built from `CombatForecast`.
  - `PhaseBanner` (animated) and `ObjectivePanel` (goal, turn, remaining enemies).
  - `LevelUpPopup` (stat roll-up), `ExpBar`, damage numbers.
  - `ItemMenu`, `TradeMenu`, and the `DangerZone` toggle.
- **Combat animation**: on-map. The attacker lunges toward the defender, a hit flashes and shakes, a crit flashes the screen. This is all tween-based in `BattleEventPlayer`. Full combat cut-scenes are out of scope.
- **`DialoguePlayer`**: shows the speaker portrait, name and line with a typewriter effect; Confirm advances and Cancel skips. It is `await`able and used by battle triggers, story scenes and supports.
- **Hub screens**: `PartyScreen` (unit details and inventories), `ConvoyShopScreen`, `PromotionScreen`, `SupportsScreen` (lists the conversations that are unlocked but not yet seen and plays them), `SaveScreen`.
- **Flow screens**: `TitleScreen` (New Game with mode and difficulty, Continue), `PrepScreen` (pick units up to the deploy slots, swap starting cells, manage items), `ResultsScreen`.

### 5.8 Supports & Dialogue
- **`SupportService`** (pure) awards points from battle events. +1 goes to each support pair within 1 tile of a unit that attacks, is attacked or heals. +5 goes to every deployed pair that survives the battle.
- Rank thresholds are set per pair in `UnitDef.Supports` (C, B, A). Crossing a threshold unlocks the conversation. The rank only rises once it is viewed in the hub, which is what the GDD specifies.
- **Dialogue data**:
  - `DialogueScene(Id, Line[])` where each `Line` is `(SpeakerId, Expression, Text)` or a command (`Wait`, `FocusCell`, `SetFlag`).
  - Conversations are named `support_<a>_<b>_<rank>`.

---

## 6. Scenes

| Scene | Root script | Notes |
|---|---|---|
| `scenes/boot/Boot.tscn` | `Boot` | Main scene. Loads content, then goes to Title |
| `scenes/title/Title.tscn` | `TitleScreen` | |
| `scenes/prep/Prep.tscn` | `PrepScreen` | |
| `scenes/battle/Battle.tscn` | `BattleController` | MapView, units root, TacticsCamera, GridCursor, HUD CanvasLayer, DialoguePlayer |
| `scenes/results/Results.tscn` | `ResultsScreen` | |
| `scenes/hub/Hub.tscn` | `HubScreen` | Tabs or buttons into the hub sub-screens |
| `scenes/ui/*.tscn` | per panel | Reusable panels instanced into the above |

Autoloads registered in `project.godot`: `GameSessionNode`, `SceneRouter`, `InputSetup`, `EventBus`.

---

## 7. Placeholder Art & Starter Content

- **Art**: no art assets for now. Terrain uses colored boxes, units use capsules with a cone "facing" marker in faction colors, and UI uses the default font plus the theme. Real art can be swapped in through `TerrainDefinition.Mesh` and `UnitDefinition.Model`, with no code changes.
- **Content for the vertical slice (Milestone 2–4)**:
  - **Terrain:** plain, forest, mountain, wall, fort, water.
  - **Classes:** about 8 — Lord, Myrmidon, Soldier, Fighter, Archer, Mage, Priest, Cavalier, plus 3 promoted classes.
  - **Equipment:** about 12 weapons, 4 arts, 4 spells.
  - **Player units:** 5 — Lord, Fighter, Mage, Cavalier, Priest.
  - **Maps:** Ch1 "Rout", 16×12. Ch2 "Defeat Boss", with reinforcements and fog. Ch3 "Seize".

---

## 8. Testing Strategy

- **Unit tests (xUnit)** cover every pure service: Pathfinder, RangeService, FogOfWar, CombatCalculator, ExpCalculator, LevelUpService, PromotionService, StatCalculator, TurnManager phase flow, each command's validation, ObjectiveEvaluator, EventDirector, SupportService, AI scenarios, and the SaveData round trip.
- **Scenario helpers**: `TestContent` (minimal content database) and `MapBuilder.FromAscii("P..E\n.F..")` to put units on small maps quickly.
- **Deterministic replay test**: run a whole scripted battle twice from the same seed and command list. The final states must match.
- **Smoke runs**: `godot --headless --quit-after` on Boot and Battle scenes. The run fails if stderr contains `ERROR` or a script exception.
- **Definition of done for each milestone**: `dotnet build` is clean, `dotnet test` is green, the smoke run is clean, and the user has done a manual playtest checklist.

---

## 9. Conventions (to add to CLAUDE.md in Milestone 0)

- Pure logic folders must never contain `using Godot;`. Engine code goes in `Nodes/` or `Resources/`.
- Godot scripts are `partial`, and the file name matches the class name. Resources use `[GlobalClass]`. Node references use `[Export]` or `GetNode<T>("%UniqueName")` (unique names in scenes).
- Battle state changes only through `TurnManager.Submit`. Visuals change only in response to `BattleEvent`s.
- No `System.Random` and no `DateTime` in logic. All randomness goes through `BattleState.Rng`.
- Content is referenced by string `Id`, never by resource path, in logic and in saves.

---

## 10. Milestones (build order)

Each milestone is meant to be one prompt, e.g. *"Build Milestone 2 from IMPLEMENTATION.md."*

### M0 — Scaffolding
- Ask before running `git init`; add the `.gitignore` additions.
- Create the csproj and the test project (with the pure/Godot glob split) and the sln. Set up autoloads, `InputSetup`, and `Boot.tscn` as the main scene.
- Create the empty folders from §3.
- Update `CLAUDE.md`: fix the truncated `[Export]` block and add the commands and conventions from §1 and §9. Update the `GDD.md` map/command refinements: text-grid maps and atomic `ActCommand`.
- **Done when:** the solution builds, one placeholder test passes, and the Boot scene runs headless without errors.

### M1 — Grid, Map & Camera
- `GridPos`, `BattleGrid`, `TerrainDefinition` → `TerrainDef`, `MapDefinition` (terrain/height rows), `ContentLoader`/`ContentDatabase`.
- `Pathfinder` (Dijkstra + A*), `RangeService` (movement only for now).
- `MapView`, `CellOverlay`, `GridCursor`, `TacticsCamera`.
- Place placeholder units from spawns, select a unit, show its movement range and path, and move it with a tweened walk. There are no turns yet.
- **Tests:** movement costs per movement type, height limits, ZOC, pass-through allies, impassable cells, path reconstruction.
- **Done when:** you can rotate and zoom around a 16×12 height map and move units with the mouse or keyboard.

### M2 — Units, Turns & Combat
- *As built:* offensive tomes (Fire, Thunder, Wind) are `WeaponDefinition`s with `Magic = true` and no uses; the separate `SpellDef` with uses-per-battle arrives in M5.
- Unit resources and records, `UnitState`, `BattleUnit`, `StatCalculator`, `Rng`.
- `CombatCalculator` (weapons + offensive spells, triangle, follow-ups, 2RN), `BattleState`, `TurnManager`, `ActCommand` (Wait/Attack), `EndPhaseCommand`, `BattleEventPlayer`.
- HUD: `UnitInfoPanel`, `ActionMenu`, `ForecastPanel`, `PhaseBanner`, attack range overlays.
- The Enemy phase exists, but enemies just Wait.
- **Tests:** the formula and triangle tables, strike sequences, deterministic resolve, phase transitions, action-state rules, command validation.
- **Done when:** the player can fight through a full Player phase, the phases cycle, and units die.

### M3 — AI
- *As built:* `GuardRegion` wakes when a hostile unit is attackable from its reach (spawns have no region data until M4); `TargetUnit`/`TargetCell`/`Healer` currently behave as Aggressive.
- `AiController`, the profiles, `ActionScorer`, the danger zone (logic + overlay toggle), AI turns playing one at a time with the camera following.
- **Tests:** the AI scenario suite (§5.6).
- **Done when:** Chapter 1 is a winnable fight against enemies that make sensible choices.

### M4 — Map Rules, Events, Fog & Dialogue
- *As built:* objectives and triggers are text lines in each `MapDefinition` (`Victory`, `Defeat`, `Triggers`), parsed by `MapScript` (`src/Battle/MapRules.cs`) instead of `ObjectiveDefinition`/`TriggerDefinition` resources. Extra story mechanics landed here too: Talk, Visit, Seize and Escape actions, Defend (`protect TAG N`), crest Dominion and Severance (`CrestBearer`, `DominatedBy`, Beast trait), reinforcement groups and Hard-only spawns. Dialogue scripts are plain text in the DIALOGUE.md format (`data/dialogue/*.txt`, `DialogueParser`). The playable chapters are the Prologue and Ch 1–3 from `docs/story/CHAPTERS.md`.
- `ObjectiveEvaluator` (all win/loss types), `EventDirector` (triggers, reinforcements), `FogOfWar` (logic, rendering, AI filtering).
- `DialogueScene` + `DialoguePlayer`, Classic/Casual death handling, victory and defeat screens.
- Create the Ch1–Ch3 maps.
- **Tests:** each objective type, trigger conditions, spawns displaced from occupied cells, fog visibility.
- **Done when:** all three chapters can be played on their own with their objectives, and Ch2's reinforcements and fog work.

### M5 — Progression & Abilities
- *As built:* combat arts are `ArtDefinition`s used with a weapon (`ArtAttack`); healing spells are `SpellDefinition`s; consumables, boosters and seals are `ItemDefinition`s. Crest skills (Ward, Rally, Mend, Dawn), Rowan's crest aura and crest-tier promotion gates follow STORY §4.3. Trade is one item (or one swap) per command and leaves the unit able to act.
- `ExpCalculator`, `LevelUpService` + popup, combat arts (uses, effects), healing spells + Healer AI, items (vulneraries, stat boosters), `TradeCommand`.
- `PromotionService`, `SupportService` points and combat bonuses.
- **Tests:** EXP table, growth rolls with a scripted RNG, promotion rules, uses refilled per battle, support thresholds and bonuses.
- **Done when:** units level up, use arts and spells within their limits, heal, and get support bonuses in a full chapter.

### M6 — Campaign Loop
- *As built:* the story is a step script (`data/campaign/main.txt`, `CampaignParser`, `CampaignRunner`). Battles work on clones of roster units and write back only on victory. Scene routing is `CampaignFlow` plus a fade in the `GameSessionNode` autoload (no separate SceneRouter). The hub is one screen with tabs (Party/convoy/equip, Shop, Smithy, Promotion, Supports, Save) and Osmund. The game autosaves on reaching the hub.
- `GameSession`, `SaveData` + `SaveService`, `SceneRouter` flow (Title → Story → Prep → Battle → Results → Hub → next).
- Hub screens: Party, Convoy/Shop, Promotion, Supports viewer, Save. Mode and difficulty selection on New Game; Hard difficulty modifiers.
- Chain Ch1–Ch3 with story dialogue.
- **Tests:** save round trip, roster write-back (Classic deaths vs Casual retreats), shop and convoy rules.
- **Done when:** a new game can be played from Title through all three chapters, saved, and continued after quitting.

### After M6 (polish backlog)
Real models and animations, audio, combat close-up camera, suspend saves, settings menu, more chapters, and the out-of-scope features in GDD §8.

---

## 11. Decisions Made Here (veto any before building)

1. **Logic split:** pure-C# logic compiled into an engine-free test project, with folder globs enforcing the boundary.
2. **Instant resolve + event replay:** logic resolves instantly, and the presentation replays `BattleEvent`s asynchronously.
3. **Atomic `ActCommand`:** the move and the action are one command, so cancelling a move needs no undo.
4. **Text-grid maps:** maps are text grids in `.tres`, rendered procedurally, instead of GridMap painting.
5. **Code-registered input:** input actions are registered in code (`InputSetup`), not in `project.godot`.
6. **Test framework:** the test project targets `net10.0` with xUnit, matching the installed SDK.
7. **On-map combat animations only** for now; there are no separate battle cut-scenes.
8. **Milestone order:** dialogue moves to M4 (map triggers need it), and arts and healing move to M5 (M2 covers weapons and offensive magic).
