# Project Summary — CadiKazani BaseProject

> Last updated: 2026-04-08

---

## Overview

A first-person Unity 3D game set in contemporary Istanbul. The player explores urban environments, talks to NPCs, picks up and carries items, rides a minibus, and progresses a story through a branching dialogue system. The codebase is structured around a custom dialogue graph system as its centrepiece, with supporting systems for inventory, sound, UI, events, and player interaction. Architecture follows a ScriptableObject-driven, event-based, component-composition design philosophy throughout.

- **Engine:** Unity (URP 17.3.0)
- **Render pipeline:** Universal Render Pipeline
- **Input:** Unity new Input System 1.18.0
- **Scripts:** ~171 C# files across ~39 directories under `Assets/Cadi/Scripts/`
- **Scenes:** One gameplay scene (`Assets/Scenes/SampleScene.unity`)
- **Platform target:** PC (60 fps, mouse + keyboard)

---

## World and Cast

The game world is urban Istanbul. Named NPCs include **Batu**, **Neris**, **Ahmet**, **Berfin**, **Ceren**, **Deniz**, **Emre**, **Gokalp**, **Ezgi**, plus service characters — BusDriver, TaxiDriver, Barista, Barmen, Bakkal (shopkeeper), StreetSinger, Police. Items include Beer, Phone, Earphones, Chips, IstanbulCard, Cigarette, Key, PetFood, BusTicket, Simit, Coffee, Cake. Planned locations (currently commented out): Minibus, Home, Park, Bar, Pier, Street, Cafe, Bakkal, BusStop.

---

## Systems Overview

### 1. Dialogue System

The most complex and central system. 27 scripts split across data, runtime, and a custom Unity Editor graph tool.

**Data architecture:**
```
DialogueContainer (ScriptableObject)
├── StartNodeData[]   — entry points, each with optional ConditionSet
├── NpcNodeData[]     — NPC speech nodes
│   ├── NpcLine[]     — one or more conditional lines per node
│   ├── ExecutionMode — Single | Repeating | AllOnce
│   └── OwnerNpc      — which NPC speaks these lines
├── PlayerNodeData[]  — player choice nodes
│   └── PlayerLine[]  — one choice per line, with TargetNodeGuid
└── Runtime cached dictionaries (GUID → node, OlayType → index lists)
```

**What works:**
- Node graph authored in a custom Unity Editor window (`DialogueGraph.cs`)
- Three NPC execution modes: `Single` (best conditional line), `Repeating` (loop), `AllOnce` (all satisfied lines in sequence)
- Condition/trigger system via `Olay` — lines gated by items in inventory, player proximity/look direction, location, music state, or specific GameObjects
- Group dialogue: multiple NPCs coordinate through `GroupDialogueController` and `DiaGlyphController`
- Barking: NPCs idle-loop lines at random intervals when player is nearby but not in conversation
- JSON serialization via `DialogueJsonHandler` (Newtonsoft.Json)
- `DiaGlyphController` (~1000 lines) — full dialogue state machine, NPC state, bark scheduling, group dialogue, Olay wiring

**Known gaps:**
- `Location` enum: all named locations are commented out in `Identifiers.cs` — location-based conditions are non-functional
- `AnimationAction` enum has only `Default` and `Idle` — full NPC animation on dialogue lines is a stub; `TriggerComponent.cs` (old runner) is 150+ lines of dead code
- `NpcLine.Priority` field: not editable in graph editor — always 0
- No validation for broken `TargetNodeGuid`, circular node loops, or orphaned nodes — silent failures possible
- `GroupDialogueController` uses `Conditional.Wait(5f)` polling hack — race conditions possible
- `DialogueJsonHandler` saves to `Assets/` path — editor-only, not build-safe

---

### 2. Interaction / Glyph System

Interactable objects display floating world-space UI bubbles ("Glyphs"). Player looks at them and presses interact. ~20 scripts.

**Key classes:**
- `IInteractable` — interface: `Interact()`, `OnLookAt()`, `OnLookAway()`
- `InteractionRaycast` — singleton, raycast from camera on `Interactable` layer
- `Glyph` — abstract base for all world-space bubbles; oscillation, scale/alpha animations, text sizing
- `GlyphController` / `GlyphControllerBase` — abstract base for objects owning Glyph sets; manages proximity, staggered reveal, info text
- `GlyphEffector` — singleton, one centralized `Update()` loop for all Glyph oscillations
- `ActGlyph` / `ActGlyphController` — generic world interactions (Open, Sit, Feed, etc.)
- `DialogueGlyph` / `DiaGlyphController` — NPC dialogue choices
- `TextAnimator` — DOTween per-character text animations (reveal, scale-in, fade)

**Concrete interaction types** (all extend `ActGlyphController`):
| Class | Behaviour |
|---|---|
| `Door` | DOTween rotate open/close; optional `KeyComponent` requirement |
| `Chair` | Delegates sit/stand to `FirstPersonController` |
| `LightSwitch` | Toggles a Unity `Light` |
| `Animal` | Feed (`AnimalGiftComponent`) and Pet; Dog, Cat, Seagull types |
| `ThoughtInteraction` | Displays observation text |
| `SoundInteraction` | Thought + plays/stops a `SpatialAudioPlayer` |
| `MinibusDoor` | DOTween slide; only opens when `BusMover.MinibusStop == true`; auto-closes after 4s |
| `ShazamPoint` | Adds current spatial audio track to player playlist |
| `Limit` | Boundary marker; switches to solid collider on exit — ghost class, all IInteractable methods empty |

**Known gaps:**
- `Animal.cs` has a dead-code branch due to `Random.Range(int, int)` always returning 0
- `Limit.cs` is a ghost — all interface methods are empty
- `InteractionRaycast` contains a misplaced `SetAnswerUI` coroutine (TODO)

---

### 3. Inventory System

13 scripts. Component-based item design.

- `InventoryManager` — singleton; manages bag and held-hand item
- `InventoryItem` — `ActGlyphController` subclass; pick-up, hold, bag, drop, consume, purchase; beer drinking triggers `ChromaticAberration` DOTween via URP Volume
- Item components: `ConsumableComponent`, `PurchasableComponent`, `EngagerComponent`, `KeyComponent`, `AnimalGiftComponent`, `NotDroppableComponent`
- `InventoryUI` — displays bag contents
- Items fire Olay conditions when picked up, presented, or consumed — feeds directly into dialogue conditions

**Known gaps:**
- No persistence — inventory resets on restart
- `AnimalGiftComponent` references `Animal.cs` directly, breaking component isolation

---

### 4. NPC System

14 scripts for NPC visuals, animations, and state.

- `NpcDetails` — maps `NpcName` enum to display colors
- `AnimController` — wraps `Animator`, sets clips by name-string
- `NpcVisualController` / `ClothDataBinder` — outfit and material per NPC
- `MatPropertyBlockSetter` — per-renderer material overrides without material instantiation
- `BarkDialogueUI` — floating idle text above NPC head
- `HumanoidCustomAnimator` — randomises idle variants
- `GroupDialogueController` — multi-NPC conversation coordinator

**Known gaps:**
- Animation is name-string only — no blending or state machine nodes
- Idle animation randomisation has no cross-NPC coordination — adjacent NPCs can mirror each other

---

### 5. Sound System

9 scripts.

- `SoundManager` — singleton with `AudioMixer`; linear→dB volume, low-pass filter, headphone mode
- Mixer groups: Master, Headphone, Spatial, SFX, Music, Ambience
- `SpatialAudioPlayer` / `SpatialMusicPlayer` — 3D-positioned audio
- `HeadphoneManager` — headphone-aware audio mix; low-pass spatial when headphones on
- `ForcedMusicPlayer` — `OnTriggerEnter` music trigger, one-shot
- `SoundDatabase` / `AudioData` / `MusicData` — ScriptableObject audio data
- Integrates with `MusicOlay` for music-state dialogue conditions

---

### 6. Player System

4 scripts.

- `FirstPersonController` — WASD + sprint movement, mouse look, sit/stand via DOTween, inventory camera teleport, look-at-target rotation; reacts to `OlayTriggeredEvent`
- `InputManager` — singleton wrapping new Input System
- `BasicRigidBodyPush` — physics push on CharacterController collision
- `PlayerDiegeticUpdater` — updates Glyph world-space positions and tilt relative to camera each frame

---

### 7. Olay System (Conditions & Triggers)

The condition/trigger architecture used by dialogue, inventory, and world events.

`Olay` is an abstract base with five concrete types:

| Type | Matches on |
|---|---|
| `ItemOlay` | `ItemOlayType` (Hold, Own, Give, Take, Use, Pay, Highlight, Ping, Unlock) + `ItemName` |
| `PlayerOlay` | `PlayerOlayType` (Any, Away, Present, Sit, GetUp, LookAt) |
| `LocationOlay` | Player entering/exiting a location trigger |
| `MusicOlay` | Music playing/stopped state |
| `ObjectOlay` | Specific `GameObject` state |

`OlaySet` holds a list of Olays; it is satisfied when all its members are satisfied. Satisfaction is tracked per-Olay in a `bool[]`; cancelled via `OlayCancelledEvent`. Per-type index lists are cached at load time — matching is O(1) lookup + type-subset iteration.

---

### 8. Event System

5 scripts. Custom pooled event dispatcher (not Unity's `UnityEvent`).

- `EM` — static global dispatcher; context+channel keyed
- `Event` — base pooled event; `Get()` factory with object pool to avoid GC
- `EventListenerCollection` — priority-sorted listener list
- `VoidEvent` — zero-payload signal event

**Known gap:** Fully static global scope — no namespacing; every listener of a type receives every event of that type globally.

---

### 9. Variables System

21 scripts. ScriptableObject-based typed variables for inspector data wiring.

- Types: `Bool`, `Int`, `Float`, `Double`, `Long`, `String`, `Vector3`, `Color`, `Sprite`, `GameObject`, `Animator`, `AnimationCurve`, `LayerMask`
- `Reference<T>` — inspector field that can be a constant or a Variable asset
- `VariablesModule` — groups variables; `ValueChangedEvent` fires on change

---

### 10. UI System

16 scripts.

- `RootCanvas` / `UICamera` — canvas/camera setup
- `PopUpManager` / `PopUp` / `PopupCatalog` — pooled popup queue
- `SelectiveImage` / `ScImgGroupHandler` / `GlobalScImgHandler` — color-selective image rendering for stylised UI effects
- `UIFxImage` / `UIEffectPooler` — pooled visual effects
- `UIRevealAnimUtil` — text reveal animations
- `BetterGridLayoutGroup` — extended grid layout
- `AutoScrollScrollRect` — auto-scroll to keep target in view
- `UIOutline` — outline shader helper
- `RadialSpriteSetter` — radial fill UI (circular progress bars)

---

### 11. Conditionals System

4 scripts. Deferred/conditional action scheduling.

- `ConditionalExecution` — executes an `Action` when a condition is true or after a delay
- `ConditionalHandle` — cancellable handle
- `ConditionalSequence` — chains multiple steps

---

### 12. Cacher System

4 scripts. Auto-caches component references marked with `[CachedField]` to avoid repeated `GetComponent`.

- `CacherMonoBehaviour` / `CacherSingleton` — base classes
- `CacherEditor` / `CacherBehavioursResolveMenu` / `CacherReferenceBuildCheck` — editor tooling including pre-build validation

---

### 13. Custom Attributes

- `[Button]` — inspector button to invoke a method
- `[ShowIf]` — conditionally show/hide inspector fields
- `[DynamicRange]` — slider with runtime-computed range
- `[SpritePreview]` — inline sprite thumbnail in inspector

---

### 14. Utility

27 helper scripts: `SingletonBehaviour`, `StringBuilderPool`, `SerializedDictionary`, `PriorityList`, debug overlays (`DebugSphere`, `DebugText`, `RuntimeDiagOverlay`), transform preserver, skinned mesh fader, extension methods for `Color`, `GameObject`, `Transform`, `Vector3`, `String`, `Enum`, `RectTransform`, `Collection`.

---

## Third-Party Plugins

| Plugin | Purpose |
|---|---|
| **DOTween Pro** (Demigiant) | Tweening — used for text animations, sit/stand, doors, chromatic aberration, minibus |
| **Odin Inspector** (Sirenix) | Enhanced inspector, custom drawers, validation |
| **QuickOutline** | MeshRenderer/SkinnedMeshRenderer outline shader for held item highlight |
| **TextMesh Pro** | All in-game text rendering |
| **Newtonsoft.Json** (3.2.2) | Dialogue JSON serialization |
| **Unity AI Navigation** (2.0.10) | NavMesh (included, no project scripts use it yet) |
| **Unity Timeline** (1.8.10) | Timeline (included, no project scripts use it yet) |

---

## Known Gaps and Flaws

| System | Issue |
|---|---|
| Dialogue | All named `Location` values commented out — location conditions non-functional |
| Dialogue | `AnimationAction` is a stub — NPC animation on dialogue lines not implemented |
| Dialogue | `TriggerComponent.cs` / `SpecialCondition.cs` — 150+ lines of dead commented code |
| Dialogue | `NpcLine.Priority` not editable in graph editor — always 0 |
| Dialogue | No validation for broken GUIDs, circular loops, or orphaned nodes |
| Dialogue | `GroupDialogueController` uses 5-second `Conditional.Wait` polling hack |
| Dialogue | `DialogueJsonHandler` saves to `Assets/` path — editor-only, not build-safe |
| Dialogue | `DiaGlyphController` is ~1000 lines — too many responsibilities |
| Interaction | `Animal.cs` has a dead-code branch (`Random.Range(int,int)` always returns 0) |
| Interaction | `Limit.cs` is a ghost class — all interface methods empty |
| Inventory | No persistence — inventory resets on restart |
| Inventory | `AnimalGiftComponent` couples directly to `Animal.cs` |
| NPC | Animation is name-string only — no blending |
| NPC | Idle randomisation has no cross-NPC coordination |
| Event System | Fully global static scope — no namespacing |
| UI | `PopUpManager` queue is not priority-aware |
| General | No scene management or scene transition system |
| General | No save/load system — all state resets on restart |
| General | `BusMover` location transitions depend on commented-out `Location` enum |