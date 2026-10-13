# Not Fire Emblem — Game Design Document

High-level design and architecture for a Fire Emblem: Three Houses–style tactical RPG built in Godot 4.7 (C#, .NET 8). This document defines *what* the systems are, *what data each one owns*, and *how data flows between them*. Implementation details belong in code.

---

## 1. Vision & Pillars

A grid-based tactics game on 3D terrain: a party of named units fights through a chain of story battles, growing stronger and closer between them.

1. **Readable decisions.** The combat forecast is exact. Every number the player sees is the number the game uses.
2. **Units matter.** Permadeath (Classic mode), growth, promotion, and supports make each unit someone the player cares about.
3. **Data-driven content.** Units, classes, weapons, maps, and dialogue are data assets, not code. A new chapter should need no new C#.

---

## 2. Core Game Loop

### Campaign loop
```
Story scene → Battle Prep → Battle → Results → Hub → (next chapter)
```
- **Battle Prep:** choose deployed units, set starting positions, adjust inventories.
- **Results:** EXP and level-ups are already applied in battle. Results shows losses, rewards (gold, items), and unlocked supports.
- **Hub (light):** party overview, convoy and shop, promotion, support conversations, save. It is a menu, not a free-roam area, and is built so a monastery/calendar layer can be added later.

### Battle loop
```
Battle Start → [Player Phase → Enemy Phase → Ally Phase] → Turn +1 → … → Victory / Defeat
```
- In **Player Phase**, the player moves each unit once and takes one action, in any order. The phase ends when every unit has acted or the player ends it.
- **Enemy Phase** and **Ally Phase** are AI-controlled.
- Objectives are checked after every action and at every phase change.

---

## 3. Game Rules

### Stats
HP, Str, Mag, Dex, Spd, Lck, Def, Res, Mov (plus Cha reserved for future use).

### Combat formulas
| Value | Formula |
|---|---|
| Attack Speed (AS) | `Spd − max(0, WeaponWt − Str/5)` |
| Attack | `Str` (physical) or `Mag` (magic) `+ Might` `± triangle` |
| Damage | `Attack − Def` (physical) or `Attack − Res` (magic), min 0 |
| Hit | `Dex×2 + Lck/2 + WeaponHit + triangle + support` |
| Avoid | `AS×2 + Lck/2 + terrain + support` |
| Displayed Hit | `clamp(Hit − Avoid, 0, 100)` |
| Crit | `(Dex + Lck)/2 + WeaponCrit + support` |
| Crit Avoid | `Lck` |
| Follow-up | Attacker strikes twice if `AS − enemy AS ≥ 4` |
| Crit damage | ×3 |

- **Hit rolls use 2RN** (the average of two rolls), so the displayed hit is skewed but stays the true chance the game uses. Crit uses a single roll.
- **Combat order:** attacker strikes, defender counters (if in range), then the follow-up from whichever unit is faster.

### Weapon triangle
**Sword > Axe > Lance > Sword.** Advantage: +15 Hit, +1 Damage; disadvantage: the reverse. Bows, magic, and fists sit outside the triangle. Bows deal bonus damage to flying units.

### Combat arts & magic
- **Combat arts** are tied to a weapon type: special attacks with modified Might, Hit, Crit, or range, or extra effects (effective damage, or no counterattack).
- **Magic** comes in two kinds: offensive spells (Mag vs Res) and healing (staff-style, range 1–2).
- Arts and spells share **one uses-per-battle system**. Each has N uses that refill after every map. There is **no weapon durability.**

### Terrain & movement
- Each `TerrainDefinition` has a movement cost per **movement type** (Infantry, Armored, Cavalry, Flying), an Avoid bonus, a Def bonus, and optional healing.
- **Height** affects movement (only cells in adjacent height bands can be reached) and gives +Hit when attacking downhill.
- **Zone of control:** a unit must stop when it moves next to an enemy. Flying units ignore terrain cost but not zones of control.

### Progression
- 100 EXP per level. EXP gained depends on the level difference and the action: a hit, a kill, or healing.
- On level up, each stat rolls against `unit growth + class growth`.
- **Promotion:** at Lv10 or higher, a promotion item moves the unit to an advanced class. The unit gets the class's base stat bonuses and may gain a new movement type and new weapon types.

### Modes & difficulty
- **Classic:** fallen units are permanently lost. **Casual:** fallen units retreat and return after the battle.
- **Normal / Hard:** Hard raises enemy stats, uses more aggressive AI profiles, and adds reinforcements.
- Losing the lord ends the battle in Defeat in both modes.

### Map objectives
- **Victory:** Rout (defeat all enemies), Defeat Boss, Survive N turns, Seize a tile, Escape (move units to exit tiles).
- **Defeat:** lord dies, a protected unit dies, or the turn limit is exceeded.
- **Reinforcements & events:** scripted spawns and dialogue, set off by a turn number, a unit stepping on a tile, a unit dying, or a phase starting.
- **Fog of war:** on some maps, each faction sees only what its units can see (`Mov`-based vision radius plus class and item modifiers).

---

## 4. Technical Systems

Godot node scripts are a thin layer over **pure C# logic**. Rules (grid, combat, turn state, AI) live in plain classes with no `Node` dependency, so they can be unit-tested and replayed deterministically. Nodes handle input, presentation, and wiring.

### 4.1 Core — `src/Core`
**Role:** state that lasts across scenes, plus the plumbing that connects systems.

| Component | Responsibility |
|---|---|
| `GameSession` (autoload) | Roster of `UnitState`s, convoy, gold, chapter progress, unlocked supports, mode and difficulty. |
| `EventBus` (autoload) | Typed C# events. Systems publish and subscribe here instead of holding references to each other. |
| `SaveSystem` | Serializes `GameSession` to JSON in `user://saves/`. Saves in the hub (and optionally a battle suspend save). |
| `SceneRouter` | Moves between Story → Prep → Battle → Results → Hub and passes along the chapter context. |

### 4.2 Grid & Movement — `src/Grid`
**Role:** the battle map's spatial model and all movement queries.

| Component | Responsibility |
|---|---|
| `BattleGrid` (pure C#) | Cell array: terrain, height, occupant, whether it blocks sight. Built from a `MapDefinition`. |
| `Pathfinder` (pure C#) | `GetReachable(unit)` uses Dijkstra over move cost per movement type, with zones of control. `GetPath(unit, target)` uses A*. |
| `RangeService` | Attack/heal/art range = cells the unit can move to plus the weapon/art range band. Used for overlays and the AI. |
| `FogOfWar` | Vision mask per faction, recomputed after every move. |
| `MapView` (Node3D) | A `GridMap` for the visible terrain, plus cell overlays (move, attack, path, danger zone). |
| `TacticsCamera` | Tilted camera with 90° rotation steps, zoom, follow/focus on a unit. Turns screen input into grid input relative to the camera. |

### 4.3 Turn / Phase Manager — `src/Battle`
**Role:** the battle state machine and the one place where game state changes.

- **`TurnManager`** has the states `BattleStart → PlayerPhase → EnemyPhase → AllyPhase → (Turn+1) → Victory | Defeat`. It tracks each unit's action state: `Ready → Moved → Acted`. A unit can cancel a move until it acts.
- **Commands:** every action from the player or the AI is an `IBattleCommand`:
  ```csharp
  interface IBattleCommand {
      bool Validate(BattleState s);
      CommandResult Execute(BattleState s); // mutates state, returns what happened
  }
  ```
  The commands are `MoveCommand`, `AttackCommand`, `ArtCommand`, `SpellCommand`, `ItemCommand`, `TradeCommand`, `WaitCommand`, `SeizeCommand` and `EscapeCommand`. Every executed command goes into a **command log**. The log makes replays and debugging possible, and later a Divine Pulse–style rewind.
- **`ObjectiveEvaluator`** checks the map's victory and defeat conditions after each command and at each phase change.
- **`EventDirector`** runs the map's scripted triggers: reinforcement spawns, dialogue, and changes to objectives.

### 4.4 Unit Data & Stats — `src/Units`
**Role:** what a unit *is* (static data) and what it *currently is* (runtime state).

**Static data** is stored as Godot `Resource` files. They are `[GlobalClass]` classes saved as `.tres` in `data/`.
| Resource | Contents |
|---|---|
| `UnitDefinition` | Name, portrait/model, base stats, personal growths, starting class/level/inventory, support partners |
| `ClassDefinition` | Stat modifiers, class growths, movement type, Mov, usable weapon types, promotion targets |
| `WeaponDefinition` | Type, Might, Hit, Crit, Weight, range, effectiveness, physical or magic |
| `ArtDefinition` / `SpellDefinition` | Weapon type or school, modifiers, range, uses per battle, effect |
| `TerrainDefinition` | Move costs per movement type, Avoid, Def, healing |
| `MapDefinition` | Grid layout, deploy slots, enemy spawns + AI profiles, objectives, triggers, fog flag |

**Runtime state** is plain C#:
- **`UnitState`** holds a reference to its definition, class, level, EXP, current HP, stat gains, inventory, equipped weapon, known arts and spells, remaining uses, support points, faction, position, and action state.
- **`StatCalculator`** works out effective stats from base + level-up gains + class + equipment + support bonus + terrain. It is the **only** place that combines stat sources.
- **`UnitView`** (Node3D) only presents the unit: model, HP bar, animations. It reads `UnitState` and never changes it.

### 4.5 Combat Calculation — `src/Combat`
**Role:** turns two units and a chosen action into a forecast and a result. It is pure and deterministic.

```csharp
CombatForecast Forecast(UnitState attacker, UnitState defender, ActionChoice action, Cell from, Cell to);
CombatResult   Resolve(CombatForecast forecast, IRng rng);
```
- **`CombatForecast`** holds each side's damage, displayed hit, crit, number of strikes, and whether the defender can counter. **The UI forecast panel and the AI read the same object.**
- **`CombatResult`** is an ordered list of `Strike`s (who strikes, hit or miss, crit or not, damage, HP afterwards), plus deaths and EXP awarded.
- **`IRng`** is injected and seeded per battle. Tests can use a fixed sequence, and a suspend save can store the seed.
- Healing and other non-combat effects follow the same Forecast/Resolve pattern.

### 4.6 AI — `src/AI`
**Role:** picks commands for Enemy and Ally phases.

- **Behaviour profiles** are set per unit in `MapDefinition`:
  - **Aggressive** — always moves toward the best target.
  - **Stationary** — attacks only targets it can reach from where it stands.
  - **Guard Area** — becomes active when a player unit enters a region.
  - **Boss** — holds its tile and attacks in range.
  - **Target** — goes after a specific unit or tile.
- **Utility scoring:** for each candidate `(move cell, target, weapon/art)`, the AI gets a `CombatForecast` and scores it:
  - expected damage
  - chance to kill
  - minus the expected damage from the counterattack
  - plus target priority (low HP, healer, lord)
  - plus terrain value

  If no attack is possible, the unit moves toward its profile's goal.
- **Fair play:** the AI queries the grid through the enemy faction's fog mask, and acts only through `IBattleCommand`s, just like the player.
- **Order:** units that can attack this phase act first, then the rest.

### 4.7 UI — `src/UI`
**Role:** turns input into commands and state into visuals. It never changes game state directly.

- **Battle:** grid cursor, overlays for movement, attack and danger range, action menu (Attack / Art / Magic / Item / Trade / Wait), combat forecast panel, unit info panel, phase banner, objective and turn display, map combat animations, level-up popup.
- **Hub:** party and unit details, convoy/shop, promotion, support viewer, save/load.
- **Dialogue:** `DialoguePlayer` with portraits, names, and text, used both for story scenes and for in-battle triggers.
- The UI subscribes to `EventBus` events and queries pure services (`Pathfinder`, `CombatCalculator`). On confirm, it creates an `IBattleCommand` and passes it to `TurnManager`.

---

## 5. Supports & Dialogue

- **Support points:** units gain points by attacking, being attacked, or healing while next to a support partner, and when they finish a battle together.
- **Ranks C → B → A:** reaching a threshold unlocks a conversation, which the player watches in the Hub. Watching it raises the rank.
- **Bonuses:** while within 2 tiles of each other, partners get +Hit, +Avoid and +Crit, scaling with rank. `StatCalculator` applies these.
- **Dialogue data:** `DialogueScene` resources: an ordered list of lines (speaker, portrait expression, text) plus simple commands (wait, camera focus, flag set). Story scenes, map triggers, and support conversations all use the same format.

---

## 6. Data Flow

### System overview
```
              ┌────────────── GameSession (roster, progress) ◄──── SaveSystem
              │                      ▲
   MapDefinition + deployed units    │ results written back
              ▼                      │
          BattleState ───────────────┘
   (BattleGrid + UnitStates + turn data)
          ▲         │ queries
 commands │         ▼
   ┌──────┴──┐   Pathfinder / RangeService / FogOfWar / CombatCalculator
   │TurnMgr  │         ▲                    ▲
   └──┬──────┘         │                    │
      │ events     UI (player)          AI (enemy/ally)
      ▼                │                    │
   EventBus ───► UI views, ObjectiveEvaluator, EventDirector
                       └── both create IBattleCommands ──► TurnManager
```

### Walkthrough: a player attack
1. **UI:** the player selects a unit with the cursor. `Pathfinder.GetReachable` and `RangeService` return cells, which `MapView` draws as overlays.
2. **UI:** the player picks a destination and a target. `CombatCalculator.Forecast` fills the forecast panel.
3. **UI:** on confirm, it builds `MoveCommand` + `AttackCommand` and passes them to `TurnManager`.
4. **TurnManager:** validates the commands, then executes them. `AttackCommand` calls `CombatCalculator.Resolve` and applies the `CombatResult` to both `UnitState`s (HP, uses, EXP, support points).
5. **EventBus:** publishes `UnitMoved`, `CombatResolved`, `UnitDied` and `LevelUp`.
6. **Subscribers:** UI plays the movement and combat animations and the level-up popup. `FogOfWar` recomputes vision. `ObjectiveEvaluator` checks for victory or defeat. `EventDirector` checks its triggers and may queue dialogue or reinforcements.
7. **Phase change:** when every player unit has acted, `TurnManager` moves to Enemy Phase. The AI produces commands through the same pipeline, starting at step 3.

### Campaign flow
1. **Prep:** `SceneRouter` loads the chapter's `MapDefinition`. Prep picks units from `GameSession`.
2. **Battle start:** `BattleState` is built from the map's `BattleGrid`, the deployed `UnitState`s, and the spawned enemies.
3. **Victory:** surviving `UnitState`s are written back to `GameSession`. Fallen units are removed (Classic) or marked as retreated (Casual). Uses are refilled.
4. **After battle:** Results → Hub → `SaveSystem` → next chapter.

---

## 7. Project Layout

```
src/
  Core/     GameSession, EventBus, SaveSystem, SceneRouter
  Grid/     BattleGrid, Pathfinder, RangeService, FogOfWar, MapView, TacticsCamera
  Battle/   TurnManager, commands, ObjectiveEvaluator, EventDirector
  Units/    Resource definitions, UnitState, StatCalculator, UnitView
  Combat/   CombatCalculator, forecast/result types, IRng
  AI/       Behaviour profiles, utility scoring
  UI/       Battle HUD, menus, hub screens, DialoguePlayer
data/       .tres content: units, classes, weapons, arts, spells, terrain, maps, dialogue
scenes/     Battle, Hub, Prep, Results, Dialogue, Title
assets/     models, textures, audio, fonts
```
- C# namespaces follow the folders (for example `NotFireEmblem.Grid`).
- Pure-logic folders (`Grid`, `Combat`, `Battle`, `AI`, parts of `Units`) keep their `Node` code in clearly separate classes.

---

## 8. Out of Scope (for now)

- Monastery calendar, free-roam, and teaching (the Hub is designed to be extended into this)
- Battalions and gambits
- Divine Pulse / turn rewind (the command log keeps this possible)
- Weapon durability and repair
- Branching routes and house selection

---

## 9. Milestones

1. **Grid & camera:** load a map, cursor, movement range, pathing, rotating camera.
2. **Turns & combat:** phases, commands, forecast, combat resolution, triangle, follow-ups.
3. **AI:** behaviour profiles and utility scoring.
4. **Map rules:** objectives, reinforcements and events, fog of war.
5. **Progression:** EXP, level-ups, promotion, arts and spell uses, Classic/Casual.
6. **Campaign:** Prep, Results, Hub, supports, dialogue, save/load.
