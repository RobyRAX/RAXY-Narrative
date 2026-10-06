using System;
using System.Collections.Generic;
using RAXY.Utility;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineCutscene : MonoBehaviour
    {
        public PlayableDirector PlayableDirector { get; set; }

        [TitleGroup("Timelines")]
        [SerializeField]
        [PropertyOrder(-2)]
        [Tooltip("Destroy this GameObject when the cutscene completes. Default true (typical for prefab cutscenes).")]
        bool destroyOnComplete = true;

        public bool DestroyOnComplete => destroyOnComplete;

        [TitleGroup("Unity Events")]
        [FoldoutGroup("Unity Events/Events")]
        public UnityEvent onStarted = new();

        [FoldoutGroup("Unity Events/Events")]
        public UnityEvent onEnded = new();

        [TitleGroup("Timelines")]
        [PropertyOrder(-2)]
        [SerializeField]
        [TableList(AlwaysExpanded = true)]
        List<TimelineEntry> timelines = new List<TimelineEntry>();

        public IEnumerable<string> TimelineIds
        {
            get
            {
                if (timelines == null)
                    yield break;

                for (int i = 0; i < timelines.Count; i++)
                {
                    var entry = timelines[i];
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                        continue;

                    yield return entry.id;
                }
            }
        }

        [TitleGroup("Timelines")]
        [PropertyOrder(-1)]
        [Button]
        public void EnsureDirectorTimelineRegistered()
        {
            if (PlayableDirector == null)
                PlayableDirector = this.GetOrAddComponent<PlayableDirector>();

            var asset = PlayableDirector.playableAsset as TimelineAsset;
            if (asset == null)
            {
                Debug.LogWarning(
                    "[TimelineCutscene] PlayableDirector tidak punya TimelineAsset.",
                    this);
                return;
            }

            if (timelines == null)
                timelines = new List<TimelineEntry>();

            for (int i = 0; i < timelines.Count; i++)
            {
                var entry = timelines[i];
                if (entry == null || entry.timeline != asset)
                    continue;

                if (!string.IsNullOrEmpty(entry.id))
                    ActiveTimelineId = entry.id;
                return;
            }

            string id = asset.name;
            if (string.IsNullOrEmpty(id))
                id = "Timeline";

            if (IsTimelineIdTaken(id))
            {
                string baseId = id;
                int suffix = 1;
                do
                {
                    id = $"{baseId}_{suffix}";
                    suffix++;
                }
                while (IsTimelineIdTaken(id));
            }

            timelines.Add(new TimelineEntry
            {
                id = id,
                timeline = asset
            });
            ActiveTimelineId = id;
        }

        bool IsTimelineIdTaken(string id)
        {
            if (timelines == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < timelines.Count; i++)
            {
                var entry = timelines[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                if (string.Equals(entry.id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        [TitleGroup("Runtime")]
        [PropertyOrder(-1)]
        [ShowInInspector, ReadOnly]
        public string ActiveTimelineId { get; private set; }

        [TitleGroup("Runtime")]
        [ShowInInspector]
        [PropertyOrder(-1)]
        public TimelineAsset Timeline
        {
            get
            {
                if (PlayableDirector != null)
                    return PlayableDirector.playableAsset as TimelineAsset;
                else
                    return null;
            }
        }

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        public bool IsDialogueHoldActive { get; private set; }

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(-1)]
        public bool IsPlayheadWrapActive { get; private set; }

        [TitleGroup("Runtime")]
        [ShowInInspector]
        [PropertyOrder(1)]
        [HideReferenceObjectPicker]
        readonly List<PlayheadRegionStatus> playheadRegions = new List<PlayheadRegionStatus>();

#if UNITY_EDITOR
        [TitleGroup("Editor Helper")]
        [SerializeField]
        List<GameObject> editorHelperPrefabs = new List<GameObject>();

        [SerializeField, HideInInspector]
        List<GameObject> editorHelperSpawned = new List<GameObject>();

        /// <summary>
        /// Used by Create Cutscene Timeline / Project Hub defaults.
        /// </summary>
        public void ApplyEditorHelperPrefabs(IEnumerable<GameObject> prefabs)
        {
            if (editorHelperPrefabs == null)
                editorHelperPrefabs = new List<GameObject>();
            else
                editorHelperPrefabs.Clear();

            if (prefabs == null)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                editorHelperPrefabs.Add(prefab);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [TitleGroup("Editor Helper")]
        [HorizontalGroup("Editor Helper/Actions")]
        [Button("Spawn")]
        void SpawnEditorHelperPrefabs()
        {
            if (editorHelperPrefabs == null || editorHelperPrefabs.Count == 0)
                return;

            if (editorHelperSpawned == null)
                editorHelperSpawned = new List<GameObject>();

            for (int i = 0; i < editorHelperPrefabs.Count; i++)
            {
                var prefab = editorHelperPrefabs[i];
                if (prefab == null)
                    continue;

                if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab))
                {
                    Debug.LogWarning(
                        $"[TimelineCutscene] Editor Helper skip '{prefab.name}' — bukan prefab asset.",
                        this);
                    continue;
                }

                var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
                if (instance == null)
                    continue;

                UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Spawn Editor Helper Prefab");
                editorHelperSpawned.Add(instance);
            }

            TryBindEditorHelperSpawned();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [TitleGroup("Editor Helper")]
        [HorizontalGroup("Editor Helper/Actions")]
        [Button("Destroy")]
        void DestroyEditorHelperPrefabs()
        {
            if (editorHelperSpawned == null)
                return;

            for (int i = editorHelperSpawned.Count - 1; i >= 0; i--)
            {
                var go = editorHelperSpawned[i];
                if (go != null)
                    UnityEditor.Undo.DestroyObjectImmediate(go);
            }

            editorHelperSpawned.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [TitleGroup("Editor Helper")]
        [Button("Try Bind")]
        void TryBindEditorHelperSpawned()
        {
            if (PlayableDirector == null)
                PlayableDirector = this.GetOrAddComponent<PlayableDirector>();

            if (PlayableDirector.playableAsset as TimelineAsset == null)
            {
                Debug.LogWarning(
                    "[TimelineCutscene] Try Bind gagal — PlayableDirector belum punya TimelineAsset.",
                    this);
                return;
            }

            if (editorHelperSpawned == null || editorHelperSpawned.Count == 0)
            {
                Debug.LogWarning(
                    "[TimelineCutscene] Try Bind gagal — belum ada object hasil Spawn.",
                    this);
                return;
            }

            var binders = new List<TimelineCutsceneTrackBinder>();
            for (int i = 0; i < editorHelperSpawned.Count; i++)
            {
                var go = editorHelperSpawned[i];
                if (go == null)
                    continue;

                binders.AddRange(go.GetComponentsInChildren<TimelineCutsceneTrackBinder>(true));
            }

            if (binders.Count == 0)
            {
                Debug.LogWarning(
                    "[TimelineCutscene] Try Bind: spawned objects tidak punya TimelineCutsceneTrackBinder.",
                    this);
                return;
            }

            UnityEditor.Undo.RecordObject(PlayableDirector, "Try Bind Cutscene Tracks");
            BindTrackBinders(binders);
            UnityEditor.EditorUtility.SetDirty(PlayableDirector);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        int playheadDirection = 1;

        bool cutsceneEndRaised;
        bool hubEventsBound;
        PlayheadRegionStatus pendingDialogueRegion;
        bool pendingDialogueHold;

        public event Action<TimelineCutscene> OnStarted;
        public event Action<TimelineCutscene> OnEnded;

        void OnValidate()
        {
            if (PlayableDirector == null)
                PlayableDirector = this.GetOrAddComponent<PlayableDirector>();

            if (timelines == null)
                return;

            var seenIds = new HashSet<string>();
            for (int i = 0; i < timelines.Count; i++)
            {
                var entry = timelines[i];
                if (entry == null)
                    continue;

                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogWarning(
                        $"[TimelineCutscene] Registered timeline di index {i} punya id kosong.",
                        this);
                    continue;
                }

                if (!seenIds.Add(entry.id))
                {
                    Debug.LogWarning(
                        $"[TimelineCutscene] Registered timeline id duplikat: '{entry.id}'.",
                        this);
                }
            }
        }

        void Awake()
        {
#if UNITY_EDITOR
            DestroyEditorHelperPrefabs();
#endif

            if (PlayableDirector == null)
                PlayableDirector = this.GetOrAddComponent<PlayableDirector>();

            BindHubEvents(true);
            Refresh();
        }

        void OnDestroy()
        {
            BindHubEvents(false);
        }

        void Update()
        {
            if (PlayableDirector == null || PlayableDirector.playableAsset == null)
                return;

            if (PlayableDirector.state != PlayState.Playing)
                return;

            if (IsDialogueHoldActive)
                return;

            if (PlayableDirector.timeUpdateMode != DirectorUpdateMode.Manual)
                PlayableDirector.timeUpdateMode = DirectorUpdateMode.Manual;

            RefreshPlayheadRegions();

            double previousTime = PlayableDirector.time;
            double time = previousTime + Time.deltaTime * playheadDirection;
            bool timeJumped = false;

            bool wasInside = TryGetActiveWrapRegion(previousTime, out PlayheadRegionStatus previousRegion);
            bool inside = TryGetActiveWrapRegion(time, out PlayheadRegionStatus region);

            if (inside && !wasInside)
                IsPlayheadWrapActive = true;

            if (IsPlayheadWrapActive && (inside || wasInside))
            {
                PlayheadRegionStatus activeRegion = inside ? region : previousRegion;

                if (activeRegion.mode == CutsceneDialogueMode.Repeat)
                {
                    if (time >= activeRegion.end)
                    {
                        time = activeRegion.start;
                        playheadDirection = 1;
                        timeJumped = true;
                    }
                }
                else
                {
                    if (time >= activeRegion.end)
                    {
                        time = activeRegion.end;
                        playheadDirection = -1;
                    }
                    else if (time <= activeRegion.start)
                    {
                        time = activeRegion.start;
                        playheadDirection = 1;
                    }
                }
            }
            else
            {
                playheadDirection = 1;
                if (PlayableDirector.duration > 0 && time >= PlayableDirector.duration)
                {
                    time = PlayableDirector.duration;
                    Raise_OnEnded();
                }
                if (time < 0)
                    time = 0;
            }

            if (!timeJumped)
                CheckPlayheadPassesTriggerTime(previousTime, time);

            PlayableDirector.time = time;
            PlayableDirector.Evaluate();
        }

        void BindHubEvents(bool bind)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
                return;

            if (bind)
            {
                if (hubEventsBound)
                    return;

                hub.OnFullscreenDialogueEnd += HandleFullscreenDialogueEnd;
                hubEventsBound = true;
            }
            else if (hubEventsBound)
            {
                hub.OnFullscreenDialogueEnd -= HandleFullscreenDialogueEnd;
                hubEventsBound = false;
            }
        }

        void Refresh()
        {
            IsPlayheadWrapActive = false;
            IsDialogueHoldActive = false;
            cutsceneEndRaised = false;
            pendingDialogueRegion = null;
            pendingDialogueHold = false;
            playheadDirection = 1;
            RefreshPlayheadRegions(resetPlayed: true);
            RefreshTrackBindings();

            PlayableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            PlayableDirector.extrapolationMode = DirectorWrapMode.Hold;
            PlayableDirector.time = 0;

            if (TryGetActiveWrapRegion(0, out PlayheadRegionStatus loopRegionAtStart)
                && loopRegionAtStart != null
                && !loopRegionAtStart.isDialogueRegion)
            {
                IsPlayheadWrapActive = true;
            }
        }

        /// <summary>
        /// Returns <paramref name="timelineId"/> when set; otherwise the id of the first timeline entry in the list.
        /// </summary>
        public string ResolveTimelineId(string timelineId)
        {
            if (!string.IsNullOrEmpty(timelineId))
                return timelineId;

            if (timelines == null || timelines.Count == 0)
                return null;

            var first = timelines[0];
            if (first == null || string.IsNullOrEmpty(first.id))
                return null;

            return first.id;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void Play(string id)
        {
            id = ResolveTimelineId(id);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[TimelineCutscene] Play dipanggil tanpa timeline id yang valid.", this);
                return;
            }

            if (timelines == null)
            {
                Debug.LogWarning("[TimelineCutscene] Belum ada registered timelines.", this);
                return;
            }

            TimelineEntry match = null;
            for (int i = 0; i < timelines.Count; i++)
            {
                var entry = timelines[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                if (string.Equals(entry.id, id, StringComparison.Ordinal))
                {
                    match = entry;
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogWarning(
                    $"[TimelineCutscene] Timeline id '{id}' tidak terdaftar.",
                    this);
                return;
            }

            if (match.timeline == null)
            {
                Debug.LogWarning(
                    $"[TimelineCutscene] Timeline id '{id}' terdaftar tapi TimelineAsset null.",
                    this);
                return;
            }

            if (PlayableDirector == null)
                PlayableDirector = this.GetOrAddComponent<PlayableDirector>();

            PlayableDirector.playableAsset = match.timeline;
            ActiveTimelineId = match.id;
            PlayActiveFromStart();
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayFromStart()
        {
            PlayActiveFromStart();
        }

        void PlayActiveFromStart()
        {
            if (PlayableDirector == null || PlayableDirector.playableAsset == null)
                return;

            Refresh();

            PlayableDirector.Play();
            PlayableDirector.Evaluate();
            Raise_OnStarted();
        }

        void Raise_OnStarted()
        {
            OnStarted?.Invoke(this);
            onStarted?.Invoke();
            NarrativeHubManager.Instance?.NotifyTimelineCutsceneStart(this);
        }

        void Raise_OnEnded()
        {
            if (cutsceneEndRaised)
                return;

            cutsceneEndRaised = true;
            RestoreAllTrackBinderComponentToggles();
            OnEnded?.Invoke(this);
            onEnded?.Invoke();
            NarrativeHubManager.Instance?.NotifyTimelineCutsceneEnd(this);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void DisablePlayheadWrap()
        {
            IsPlayheadWrapActive = false;
            playheadDirection = 1;
        }

        void RefreshTrackBindings()
        {
            if (PlayableDirector == null || Timeline == null)
                return;

            BindTrackBinders(FindObjectsByType<TimelineCutsceneTrackBinder>(FindObjectsSortMode.None));
            ApplyAllTrackBinderComponentToggles();
        }

        void BindTrackBinders(IEnumerable<TimelineCutsceneTrackBinder> trackBinders)
        {
            if (PlayableDirector == null || Timeline == null || trackBinders == null)
                return;

            var tracksByName = new Dictionary<string, TrackAsset>();
            foreach (var track in Timeline.GetOutputTracks())
            {
                if (tracksByName.ContainsKey(track.name))
                {
                    Debug.LogWarning(
                        $"[TimelineCutscene] Track duplikat bernama '{track.name}' diabaikan.",
                        this);
                    continue;
                }

                tracksByName[track.name] = track;
            }

            foreach (var binder in trackBinders)
            {
                if (binder == null || binder.trackBinds == null)
                    continue;

                foreach (var entry in binder.trackBinds)
                {
                    if (string.IsNullOrEmpty(entry.trackName))
                        continue;

                    if (!tracksByName.TryGetValue(entry.trackName, out var track))
                    {
                        Debug.LogWarning(
                            $"[TimelineCutscene] Track '{entry.trackName}' tidak ditemukan di Timeline.",
                            binder);
                        continue;
                    }

                    switch (entry.trackBindType)
                    {
                        case TrackBindType.Animation:
                            if (track is not AnimationTrack)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] Track '{entry.trackName}' bukan AnimationTrack.",
                                    binder);
                                break;
                            }

                            if (binder.animator == null)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] TrackBinder untuk '{entry.trackName}' tidak punya Animator.",
                                    binder);
                                break;
                            }

                            PlayableDirector.SetGenericBinding(track, binder.animator);
                            break;

                        case TrackBindType.CinemachineBrain:
                            if (track is not CinemachineTrack)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] Track '{entry.trackName}' bukan CinemachineTrack.",
                                    binder);
                                break;
                            }

                            if (binder.cinemachineBrain == null)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] TrackBinder untuk '{entry.trackName}' tidak punya CinemachineBrain.",
                                    binder);
                                break;
                            }

                            PlayableDirector.SetGenericBinding(track, binder.cinemachineBrain);
                            break;

                        case TrackBindType.ColorOverlayManager:
                            if (track is not ColorOverlayTrack)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] Track '{entry.trackName}' bukan ColorOverlayTrack.",
                                    binder);
                                break;
                            }

                            if (binder.colorOverlayManager == null)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] TrackBinder untuk '{entry.trackName}' tidak punya ColorOverlayManager.",
                                    binder);
                                break;
                            }

                            PlayableDirector.SetGenericBinding(track, binder.colorOverlayManager);
                            break;

                        case TrackBindType.TextOverlayManager:
                            if (track is not TextOverlayTrack)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] Track '{entry.trackName}' bukan TextOverlayTrack.",
                                    binder);
                                break;
                            }

                            if (binder.textOverlayManager == null)
                            {
                                Debug.LogWarning(
                                    $"[TimelineCutscene] TrackBinder untuk '{entry.trackName}' tidak punya TextOverlayManager.",
                                    binder);
                                break;
                            }

                            PlayableDirector.SetGenericBinding(track, binder.textOverlayManager);
                            break;
                    }
                }
            }
        }

        static void ApplyAllTrackBinderComponentToggles()
        {
            var binders = FindObjectsByType<TimelineCutsceneTrackBinder>(FindObjectsSortMode.None);
            for (var i = 0; i < binders.Length; i++)
                binders[i]?.ApplyComponentToggles();
        }

        public static void RestoreAllTrackBinderComponentToggles()
        {
            var binders = FindObjectsByType<TimelineCutsceneTrackBinder>(FindObjectsSortMode.None);
            for (var i = 0; i < binders.Length; i++)
                binders[i]?.RestoreComponentToggles();
        }

        void RefreshPlayheadRegions(bool resetPlayed = false)
        {
            var previousPlayed = resetPlayed
                ? null
                : BuildPlayedLookup();

            playheadRegions.Clear();

            if (Timeline == null)
                return;

            foreach (var track in Timeline.GetOutputTracks())
            {
                if (track is CutsceneDialogueTrack)
                {
                    AppendDialoguePlayheadRegions(track, previousPlayed);
                    continue;
                }

                if (track is PlayheadLoopTrack)
                    AppendPlayheadLoopRegions(track);
            }
        }

        void AppendDialoguePlayheadRegions(TrackAsset track, Dictionary<string, bool> previousPlayed)
        {
            foreach (var clip in track.GetClips())
            {
                var asset = clip.asset as CutsceneDialogueClip;
                if (asset == null)
                    continue;

                var status = new PlayheadRegionStatus
                {
                    start = clip.start,
                    end = clip.end,
                    mode = asset.mode,
                    triggerTime = asset.triggerTime,
                    startOffset = asset.startOffset,
                    dialogueSO = asset.dialogueSO,
                    dialogueCollectionId = asset.dialogueCollectionId,
                    isDialogueRegion = true,
                    isPlayed = false
                };

                if (previousPlayed != null
                    && previousPlayed.TryGetValue(MakeClipKey(status.start, status.end), out bool wasPlayed))
                {
                    status.isPlayed = wasPlayed;
                }

                playheadRegions.Add(status);
            }
        }

        void AppendPlayheadLoopRegions(TrackAsset track)
        {
            foreach (var clip in track.GetClips())
            {
                var asset = clip.asset as PlayheadLoopClip;
                if (asset == null)
                    continue;

                if (asset.mode == CutsceneDialogueMode.Pause)
                    continue;

                playheadRegions.Add(new PlayheadRegionStatus
                {
                    start = clip.start,
                    end = clip.end,
                    mode = asset.mode,
                    isDialogueRegion = false,
                    isPlayed = true
                });
            }
        }

        Dictionary<string, bool> BuildPlayedLookup()
        {
            var lookup = new Dictionary<string, bool>();
            for (int i = 0; i < playheadRegions.Count; i++)
            {
                var status = playheadRegions[i];
                if (status == null)
                    continue;

                lookup[MakeClipKey(status.start, status.end)] = status.isPlayed;
            }

            return lookup;
        }

        static string MakeClipKey(double start, double end)
            => $"{start:R}|{end:R}";

        void CheckPlayheadPassesTriggerTime(double previousTime, double time)
        {
            if (previousTime == time)
                return;

            bool forward = time > previousTime;

            for (int i = 0; i < playheadRegions.Count; i++)
            {
                var status = playheadRegions[i];
                if (status == null || !status.isDialogueRegion || status.isPlayed)
                    continue;

                double triggerAt = status.GetTriggerTime();
                bool passes = forward
                    ? previousTime < triggerAt && time >= triggerAt
                    : previousTime > triggerAt && time <= triggerAt;

                if (passes)
                    HandleCutsceneDialogueClip(status);
            }
        }

        void HandleCutsceneDialogueClip(PlayheadRegionStatus status)
        {
            if (status == null || status.isPlayed)
                return;

            status.isPlayed = true;

            if (status.dialogueSO == null)
            {
                Debug.LogWarning("[TimelineCutscene] CutsceneDialogueClip tanpa dialogueSO.", this);
                return;
            }

            BindHubEvents(true);

            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[TimelineCutscene] NarrativeHubManager tidak tersedia.", this);
                return;
            }

            pendingDialogueRegion = status;
            pendingDialogueHold = status.mode == CutsceneDialogueMode.Pause;
            if (pendingDialogueHold)
                IsDialogueHoldActive = true;
            else if (status.mode == CutsceneDialogueMode.PingPong || status.mode == CutsceneDialogueMode.Repeat)
                IsPlayheadWrapActive = true;

            hub.PlayFullscreenDialogue(status.dialogueSO, status.dialogueCollectionId);
        }

        void HandleFullscreenDialogueEnd(FullscreenDialogueDataSO data, string collectionId)
        {
            if (pendingDialogueRegion == null)
                return;

            if (pendingDialogueRegion.dialogueSO != data)
                return;

            if (!string.Equals(pendingDialogueRegion.dialogueCollectionId, collectionId, StringComparison.Ordinal))
                return;

            var clip = pendingDialogueRegion;
            pendingDialogueRegion = null;

            if (pendingDialogueHold)
            {
                IsDialogueHoldActive = false;
                pendingDialogueHold = false;
            }

            if (clip.mode == CutsceneDialogueMode.PingPong || clip.mode == CutsceneDialogueMode.Repeat)
                DisablePlayheadWrap();
        }

        bool TryGetActiveWrapRegion(double time, out PlayheadRegionStatus region)
        {
            for (int i = 0; i < playheadRegions.Count; i++)
            {
                var candidate = playheadRegions[i];
                if (candidate == null)
                    continue;

                if (candidate.mode != CutsceneDialogueMode.PingPong
                    && candidate.mode != CutsceneDialogueMode.Repeat)
                    continue;

                if (time >= candidate.start && time <= candidate.end)
                {
                    region = candidate;
                    return true;
                }
            }

            region = null;
            return false;
        }
    }

    [Serializable]
    public class TimelineEntry
    {
        public string id;
        public TimelineAsset timeline;

        [Button]
        void SetNameAsId()
        {
            if (timeline == null)
            {
                Debug.LogWarning("[TimelineEntry] TimelineAsset belum di-assign.");
                return;
            }

            id = timeline.name;
        }
    }
}
