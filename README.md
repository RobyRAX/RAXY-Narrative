# RAXY Narrative

RAXY Narrative provides hub-driven narrative playback for Unity: fullscreen and banter dialogue, player choices, animated portraits, and timeline cutscenes.

## Features

- **NarrativeHubManager** — central hub for dialogue, choices, cutscenes, and narrative actions
- **FullscreenDialogueView / BanterDialogueView** — typewriter dialogue UI with portrait support
- **DialogueChoiceView** — async player choice buttons
- **DialoguePortrait / PortraitStateSetter** — animated multi-part portraits (DOTween)
- **FullscreenDialogueDataSO / BanterDialogueDataSO / DialogueActorSO** — ScriptableObject dialogue data
- **TimelineCutscene / CutsceneDialogueTrack** — timeline cutscenes with dialogue clips

## Setup

1. Add `NarrativeHubManager` to your scene and assign fullscreen/banter/choice views and cutscene runner.
2. Create assets via **Create > RAXY > Narrative**.
3. For timeline dialogue, use `CutsceneDialogueTrack` clips bound through `TimelineCutscene` / `TimelineCutsceneTrackBinder`.

## Dependencies

- **RAXY Event** (`com.raxy.event`) — `EventSoRaiser` narrative actions
- **RAXY UI** (`com.raxy.ui`) — `TextTyper`
- **RAXY Utility** (`com.raxy.utility`) — `Singleton`
- **RAXY Localization** (`com.raxy.utility.localization`) — localized lines and choices
- **RAXY Core** (`com.raxy.core`) — Addressable portrait providers
- **UniTask** (`com.cysharp.unitask`) — async play APIs
- **Unity Addressables** — actor portrait references
- **Unity Timeline** — cutscene tracks and clips
- **Unity Cinemachine** — cinemachine track binding
- **Unity UGUI** — TextMeshPro and UI components
- **DOTween** (project plugin) — portrait and UI tweens; required in consuming project
- **Odin Inspector** (project plugin) — editor attributes and custom drawers

## Verify after import

1. Open the project in Unity and confirm assemblies `RAXY.Narrative` / `RAXY.Narrative.Editor` compile with no errors.
2. Confirm Create Asset menu shows `RAXY/Narrative/*`.
3. Re-open any Narrative test scene and confirm hub/view script references still resolve.

## Notes

Game-specific content (scenes, portrait prefabs, dialogue assets) should live in your project, not in this package.
