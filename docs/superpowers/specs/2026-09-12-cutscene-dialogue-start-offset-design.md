# Cutscene Dialogue Clip — Start Offset

**Date:** 2026-09-12  
**Status:** Approved  
**Scope:** `CutsceneDialogueClip`, `CutsceneDialogueClipEditor`, `CutsceneDialogueClipStatus` / `TimelineCutscene`

## Goal

Allow `CutsceneDialogueClip` with **Trigger Time = Start** to fire fullscreen dialogue *before* the Timeline clip start, via a Start Offset (seconds).

Example: clip starts at `2.0`, Start Offset `1.0` → dialogue triggers at `1.0`.

## Decisions

| Topic | Choice |
|-------|--------|
| Offset meaning | Positive = earlier (pre-trigger): `start - offset` |
| Where it applies | **Start only** (not Middle / End) |
| Before time 0 | Clamp trigger to `0` |
| Wrap / Pause region | Unchanged — still `clip.start` … `clip.end` |
| Timeline markers | Out of scope |

## Data model

On `CutsceneDialogueClip`:

```csharp
[Min(0f)]
public float startOffset = 0f; // seconds; used only when triggerTime == Start
```

- Default `0` preserves current behavior (trigger at `clip.start`).
- Non-negative via `[Min(0f)]`.
- Copied into `CutsceneDialogueClipStatus` when rebuilding dialogue clip statuses.

## Runtime

`CutsceneDialogueClipStatus.GetTriggerTime()`:

| Trigger Time | Result |
|--------------|--------|
| Start | `Math.Max(0, start - startOffset)` |
| Middle | `(start + end) * 0.5` (unchanged) |
| End | `end` (unchanged) |

`CheckPlayheadPassesTriggerTime` stays as-is; only `triggerAt` changes.

PingPong / Repeat wrap and Pause hold still use the clip’s start/end bounds, not the early trigger time.

## Inspector

In `CutsceneDialogueClipEditor`:

1. Draw **Trigger Time** as today.
2. If `triggerTime == Start`, draw **Start Offset** (float, seconds, min 0).
3. Mode / Dialogue SO / Collection Id unchanged.

Optional short help: *"Trigger dialogue this many seconds before clip start."*

No Timeline track gizmo or marker UI.

## Verification (manual)

1. Start + offset `0` → trigger at clip start (same as today).
2. Start + offset `0.5`, clip start `2.0` → trigger at `1.5`.
3. Start + large offset (clip start `0.2`, offset `1`) → trigger clamped to `0`.
4. Middle / End → Start Offset hidden; behavior unchanged.

## Out of scope

- Offset for Middle or End
- Visual marker on the Timeline track
- Changing wrap or Pause region to follow the offset
