# CLAUDE.md — UnityTools (CadiKazani BaseProject)

> This file is the operating manual for Claude when working in this repo. The
> upper half defines behavior and conventions Claude must follow. The lower
> half is a reference map of what already exists in the codebase — consult it
> before writing anything new.

---

## Rules of engagement

### Reuse before creating

Before writing any new class, extension, singleton, event, attribute, drawer,
or shader, check the "Codebase reference" section below and inspect the
relevant folder. If something close to what's needed already exists:

- Extend it, don't duplicate it.
- If unsure whether to extend or create new, ask before proceeding.
- Never re-implement functionality that already exists in `Cadi/Scripts/Utility/Extensions/`,
  `EventSystem/`, `CacherSystem/`, or `GraphicSystems/`.

### Trigger tags

The user may add tags to a request. When present, they change how you work.

- **`-usecadi`** — The user is explicitly asking for a Cadi-conformant
  implementation. This means:
    - Inherit from `CacherMonoBehaviour` (or `SingletonBehaviour<T>` /
      `CacherSingleton<T>` for singletons), not plain `MonoBehaviour`.
    - Wire component references via `[CachedField]`, not `[SerializeField]` +
      manual assignment.
    - Use `EM` / `Event` for cross-system communication, not `UnityEvent` or
      direct method calls.
    - Use existing extension methods (`GC<T>()`, `GCIC<T>()`, etc.) for
      component access instead of raw `GetComponent`.
    - Wrap all Odin attributes in `#if ODIN_INSPECTOR` and DOTween code in
      `#if CADI_DOTWEEN`.
    - Follow the field naming conventions (`m_`, `s_`, `c_`) without exception.
    - Load ScriptableObject data through the `Get()` pattern documented below,
      not `Resources.Load` scattered across call sites.

- **`-plain`** — Ignore Cadi conventions; write vanilla Unity code. Useful
  for quick prototypes or scripts that will live outside this project.

- **`-review`** — Do not write code. Review the referenced code against the
  conventions in this file and report deviations.

- **`-ask`** — Do not write code yet. Ask clarifying questions first.

No tag means: default to Cadi conventions for anything inside `Assets/Cadi/`,
default to plain Unity for anything outside it.

### Non-negotiables

- The project must compile without Odin Inspector installed. Every use of
  `Sirenix.OdinInspector.*` must be inside `#if ODIN_INSPECTOR`.
- The project must compile without DOTween installed. Every use of
  `DG.Tweening.*` must be inside `#if CADI_DOTWEEN`, including method
  signatures that return `Tween`, `Sequence`, or use `Ease`.
- Do not add `.asmdef` files. The project intentionally uses the default
  Assembly-CSharp. Adding one requires a namespace restructure conversation
  first.
- Do not manually edit the `CADI_DOTWEEN` scripting define. It is managed
  by `CadiDotweenDefineSetter.cs`.
- Do not use `public` fields for serialization. Always `[SerializeField] private`.

### Style and formatting

- Prose over bullets in comments. XML doc comments only on public APIs.
- No emojis in code, comments, logs, or generated docs.
- No excessive `[Header]` decoration — group with `[FoldoutGroup]` (Odin) when
  helpful, wrapped in `#if ODIN_INSPECTOR`.
- Prefer explicit types over `var` when the type isn't obvious from the RHS.
- `#nullable enable` is opt-in per file, not project-wide. If you enable it in
  a new file, handle nullability throughout — don't half-do it.

### When writing new features

1. Restate the request in one sentence and identify which existing systems
   are involved.
2. List which files will be created or modified.
3. Wait for confirmation before writing code, unless the request is a
   one-liner or the tag is `-usecadi` with a clear spec.
4. After writing, note any TODO/uncertain areas explicitly rather than
   guessing.

### When you don't know

- If the codebase has two conflicting patterns for the same thing, ask which
  to follow rather than picking one silently.
- If a convention above conflicts with what the codebase actually does, flag
  the mismatch — the codebase may have drifted from the rules.
- Never invent a system, extension, or attribute that isn't in the reference
  below. If it seems useful, propose it first.

---

## Codebase reference

A reusable Unity toolkit / base project that provides foundational systems for game development: component caching, event dispatch, UI graphic systems, custom inspector attributes, and a library of extension methods. The project is structured as a single `Cadi/` folder meant to be dropped into game projects. Built on Unity 6 with URP 17.3.0, targeting PC (1920x1080, 60 fps). All code lives under `Assets/Cadi/Scripts/`.

## Folder structure

```
Assets/
  Cadi/
    Prefabs/                  -- reusable prefabs
    Scripts/
      CacherSystem/           -- auto-resolve component references
        Editor/               -- CacherEditor, resolve menu, pre-build check
      CustomAttributes/       -- [Button], [ShowIf], [DynamicRange], [SpritePreview]
        Editor/               -- property drawers for above attributes
      DataSaving/             -- ScriptableObject data containers (Content.cs)
      Editor/                 -- project-wide editor utilities (CadiDotweenDefineSetter)
      EventSystem/            -- pooled event dispatcher (EM, Event, EventHub)
      UI/
        Extensions/           -- BetterGridLayoutGroup, UISlicedSpriteAnimator, etc.
        FX/                   -- UI visual effects (FXGraphix)
        GraphicSystems/       -- Graphix, Slot, SelectiveGraphix, SelectionController
        ScreenSystem/         -- screen management
        Shaders/              -- UIAlphaOutline shader + companion scripts
      Utility/
        DebugHelpers/         -- DebugSphere, DebugText, RuntimeDiagOverlay
        Extensions/           -- extension method classes (see list below)
        GameObjectHelpers/    -- ObjectDuplicator, etc.
    Shaders/
      2D/                     -- 2DFrost, TestFullscreenRed
      3D/                     -- SketchLook
      UI/                     -- BlurKit (separable blur), RadialDash, TriangularDash
  Resources/
    Content/                  -- ScriptableObject assets loaded at runtime
```

There are no Assembly Definition (.asmdef) files in this project. All scripts compile into the default `Assembly-CSharp` and `Assembly-CSharp-Editor` assemblies. This means any script change triggers a full recompilation.

## Optional dependencies and define symbols

The project uses `#if` guards so it compiles with or without certain third-party packages. Two custom defines are relevant:

- `CADI_DOTWEEN` -- Guards all DOTween-dependent code (tween extensions in `UIExtensions.cs`, animations in `Slot.cs`). Auto-managed: `CadiDotweenDefineSetter.cs` runs on every domain reload via `[InitializeOnLoad]`, checks whether `DG.Tweening.DOTween` exists in loaded assemblies via reflection, and adds/removes the define from Player Settings automatically. Never set this manually.

- `ODIN_INSPECTOR` -- Guards all Odin Inspector features (foldout groups, inline properties, read-only lists, custom drawers). Auto-set by the Odin package itself on import; removed when the package is uninstalled. Used in 16+ files, mostly in the UI/GraphicSystems and CacherSystem folders. The `ButtonAttribute` class has a particularly notable pattern: when Odin is present, it inherits from `Sirenix.OdinInspector.ButtonAttribute` so Odin renders it natively; when absent, it falls back to a plain `System.Attribute` with a custom drawer.

Files that use these guards (non-exhaustive):
- `CADI_DOTWEEN`: `UIExtensions.cs`, `Slot.cs`
- `ODIN_INSPECTOR`: `CanvasOrderPolice.cs`, `CanvasPD.cs`, `Graphix.cs`, `NestedGraphix.cs`, `NestedSelectiveGraphix.cs`, `SelectiveGraphix.cs`, `SelectiveGraphixGroup.cs`, `SelectionController.cs`, `Slot.cs`, `ButtonAttribute.cs`, `ButtonEditor.cs`, `CacherEditor.cs`

## Namespace conventions

The root namespace is `Cadi.Scripts` and sub-namespaces mirror the folder hierarchy: `Cadi.Scripts.UI`, `Cadi.Scripts.Utility.Extensions`, `Cadi.Scripts.CacherSystem`, etc. Editor scripts use a parallel `.Editor` suffix (e.g., `Cadi.Scripts.CustomAttributes.Editor`). Shader companion scripts use `Cadi.Shaders.*` (e.g., `Cadi.Shaders.UI.BlurKit`).

## Coding conventions

### Field naming

Private fields use Hungarian-style prefixes consistently throughout the codebase:
- `m_` for instance fields: `m_BlurAmount`, `m_IsResolved`, `m_RectTransform`
- `s_` for static fields: `s_Instance`, `s_BindingsCache`, `s_StringBuilder`
- `c_` for constants: `c_ResourcesAssetPath`, `c_DefaultChannel`

Properties are PascalCase, parameters are camelCase.

### Serialization

The standard pattern is `[SerializeField] private` (or `protected`). Public fields are not used for serialization. `[HideInInspector]` is added to serialized fields that should be set programmatically (e.g., Cacher-resolved references). Odin attributes like `[FoldoutGroup]`, `[InlineProperty]`, `[ReadOnly]` are always wrapped in `#if ODIN_INSPECTOR` blocks.

### Nullable reference types

`#nullable enable` is used in `UIExtensions.cs` only. The rest of the codebase does not enable nullable context. This is selective, not project-wide.

### Base classes

Most MonoBehaviours that need component references should inherit from `CacherMonoBehaviour` rather than plain `MonoBehaviour`. This gives them access to the `[CachedField]` auto-resolution system (see Key systems below). Singletons inherit from `SingletonBehaviour<T>` or `CacherSingleton<T>` (which combines both patterns).

## Key systems

### Cacher system (`CacherSystem/`)

The central pattern for component wiring. Mark fields with `[CachedField]` and they get auto-resolved via reflection on `Reset`, `OnValidate` (editor), and `Awake` (runtime fallback). The attribute accepts a `RefSearch` enum (`Self`, `Parent`, `Children`), an `includeInactive` flag, and an `addComponentIfMissing` option. The system caches reflection bindings per-type in a static dictionary, cleared on domain reload via `[RuntimeInitializeOnLoadMethod]`.

A pre-build validation step (`CacherReferenceBuildCheck.cs`) runs before builds and can block the build if required references are missing. The editor inspector (`CacherEditor.cs`) shows resolve status and provides a manual resolve button.

### Singleton (`SingletonBehaviour<T>`)

Generic singleton base at `Utility/SingletonBehaviour.cs`. Auto-creates if missing on first `Instance` access, destroys duplicates, handles application quit to prevent ghost recreation, and supports `DontDestroyOnLoad` (opt-out via `m_PersistAcrossScenes`). Runs at execution order `-10000` (defined in `ExecOrder.SINGLETON`).

`CacherSingleton<T>` extends `CacherMonoBehaviour` with the same singleton semantics, giving singletons access to `[CachedField]` resolution.

### Event system (`EventSystem/`)

A pooled, priority-based event dispatcher. `Event` is the base class with a built-in object pool (`Rent()` / `Dispose()` pattern to avoid GC allocations). `EventHub` manages priority-sorted listener lists. `EM` is the static entry point, keyed by context object + integer channel. Events are dispatched via `EM.SendEvent<T>(evt, context, channel)` or the extension method `evt.SendGlobal()`.

The system is fully global/static. There is no namespace scoping: every listener of a type receives every event of that type unless filtered by context or channel.

### Resources loading convention

ScriptableObject data containers follow a consistent pattern: a private `const string` holds the Resources path, a static `Get()` method calls `Resources.Load<T>()` with that path, and an editor-only fallback auto-creates the asset if it doesn't exist. Current containers:

- `Content` (general game data): `Resources.Load<Content>("Content/GeneralContent")`
- `UIContent` (UI prefabs/FX): `Resources.Load<UIContent>("Content/UIContent")`

Assets live at `Assets/Resources/Content/`. The `Get()` methods are effectively lazy singletons. If you add a new data container, follow this same pattern.

### StringBuilderPool (`Utility/StringBuilderPool.cs`)

A single shared `StringBuilder` instance, initialized via `[RuntimeInitializeOnLoadMethod(BeforeSplashScreen)]`. Call `StringBuilderPool.Get()` to get a cleared builder. Not thread-safe; designed for main-thread use only.

### GraphicSystems (`UI/GraphicSystems/`)

A hierarchy for UI graphics management. `Graphix` is the base class (extends `CacherMonoBehaviour`), wrapping a `Slot` which handles sprite/texture assignment on `Image` and `RawImage` components. `SelectiveGraphix` adds multi-state graphics with selection controllers. `NestedGraphix` and `NestedSelectiveGraphix` handle parent-child graphic hierarchies.

## Extension method classes

- `GoExtensions` -- shorthand for GetComponent calls: `GC<T>()`, `GCs<T>()`, `GCIC<T>()`, `GCsIC<T>()`, `GCIP<T>()`, `GCsIP<T>()`, plus `ForEachChild`. These abbreviations are used throughout the codebase.
- `CollectionExtensions`
- `ColorExtensions`
- `EnumExtensions`
- `StringExtensions`
- `TransformExtensions`
- `Vector3Extensions`
- `UIExtensions`

## Custom attributes and their drawers

Each custom attribute in `CustomAttributes/` has a corresponding property drawer in `CustomAttributes/Editor/`:

- `[Button]` (attribute) + `ButtonEditor.cs` / `ButtonDrawerUtility.cs` (drawer) -- places a clickable button in the inspector that invokes the decorated method. When Odin is present, the attribute inherits from Odin's own `ButtonAttribute` and Odin handles rendering; the custom drawer only activates without Odin. A header hook (`CadiButtonHeaderHook`) ensures buttons render even when other editor extensions override the default inspector.
- `[ShowIf]` (attribute) + `ShowIfDrawer.cs` (drawer) -- conditionally shows/hides a field based on another field's value.
- `[DynamicRange]` (attribute) + `DynamicRangeDrawer.cs` (drawer) -- a range slider whose min/max are read from other fields at draw time.
- `[SpritePreview]` (attribute) + `SpritePreviewDrawer.cs` (drawer) -- renders an inline sprite thumbnail next to the field.

## Custom shaders

Seven custom shaders live under `Cadi/Shaders/`, organized by target: `UI/`, `2D/`, `3D/`. The UI and 2D shaders target URP (`"RenderPipeline"="UniversalPipeline"` or `"UniversalRenderPipeline"`). The UI shaders (RadialDash, TriangularDash variants, BlurKit, UIAlphaOutline) are used by the GraphicSystems and UI FX systems. `Blur.cs` in `Shaders/UI/BlurKit/` is a MonoBehaviour companion that drives the separable blur shader at runtime. Shader properties follow Unity conventions (`_Color`, `_Speed`, `_DashCount`, etc.). `TestFullscreenRed.shader` is a debug/test shader.

TODO: Confirm whether `SketchLook.shader` (3D) is actively used or experimental.

## Gotchas

The Cacher pre-build check (`CacherReferenceBuildCheck.cs`) will block builds if any `[CachedField(required: true)]` references are unresolved. If a build fails with missing-reference warnings, open the relevant GameObjects and click "Resolve References" in the inspector, or use the context menu `Cadi > Resolve All Cacher References`.

`CadiDotweenDefineSetter` mutates Player Settings scripting defines on every domain reload. If DOTween is removed from the project, the `CADI_DOTWEEN` define is automatically stripped. If you see unexpected recompilation loops after adding/removing DOTween, this script is the cause.

`GoExtensions` uses terse shorthand (`GC`, `GCIC`, etc.) that won't be obvious without context. These map directly to `GetComponent`, `GetComponentInChildren`, etc. Check `GoExtensions.cs` for the full mapping.

Nullable reference types are enabled in `UIExtensions.cs` only. The rest of the codebase uses traditional null checks.

## What not to do

- Do not add `.asmdef` files without restructuring namespaces first. The current namespace layout assumes everything compiles together.
- Do not remove `#if ODIN_INSPECTOR` guards. The project must compile cleanly without Odin installed.
- Do not manually edit scripting define symbols for `CADI_DOTWEEN`. The `CadiDotweenDefineSetter` manages this automatically and will fight manual changes on the next domain reload.
- Do not call `StringBuilderPool.Get()` in code that might be invoked re-entrantly (e.g., inside a `ToString()` that could be called during string building).