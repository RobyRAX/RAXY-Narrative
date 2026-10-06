# Changelog

## [1.2.0] - 2026-10-06

- Add `PlayheadLoopTrack` / `PlayheadLoopClip` for playhead Repeat/Ping Pong without dialogue; proceed via `TimelineCutscene.DisablePlayheadWrap()`.
- Rename runtime wrap data to `PlayheadRegionStatus` and `playheadRegions` on `TimelineCutscene`.
- `TimelineCutscene.ResolveTimelineId` — optional timeline id falls back to the first timeline entry.
- `NarrativeHubManager.PlayTimelineCutscene` — `timelineId` parameter optional (default: first entry).

## [1.0.0] - 2026-07-31

- Initial package prep for RAXY Narrative (hub, fullscreen/banter dialogue, choices, timeline cutscenes).
