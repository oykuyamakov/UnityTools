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
