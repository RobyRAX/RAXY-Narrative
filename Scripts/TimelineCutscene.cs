using System;
using System.Collections.Generic;
using RAXY.Utility;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineCutscene : MonoBehaviour
    {
        public PlayableDirector PlayableDirector { get; set; }

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

        [TitleGroup("Cutscene Dialogue")]
        [ShowInInspector, ReadOnly]
        public bool IsDialogueHoldActive { get; private set; }

        [TitleGroup("Cutscene Dialogue")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(-1)]
        public bool IsPlayheadWrapActive { get; private set; }

        [TitleGroup("Cutscene Dialogue")]
        [ShowInInspector]
        [PropertyOrder(1)]
        [HideReferenceObjectPicker]
        readonly List<CutsceneDialogueClipStatus> dialogueClipStatuses = new List<CutsceneDialogueClipStatus>();

        int playheadDirection = 1;

        bool cutsceneEndRaised;
        bool hubEventsBound;
        CutsceneDialogueClipStatus pendingDialogueClip;
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

            RefreshDialogueClipStatuses();

            double previousTime = PlayableDirector.time;
            double time = previousTime + Time.deltaTime * playheadDirection;
            bool timeJumped = false;

            bool wasInside = TryGetActiveWrapRegion(previousTime, out CutsceneDialogueClipStatus previousRegion);
            bool inside = TryGetActiveWrapRegion(time, out CutsceneDialogueClipStatus region);

            if (inside && !wasInside)
                IsPlayheadWrapActive = true;

            if (IsPlayheadWrapActive && (inside || wasInside))
            {
                CutsceneDialogueClipStatus activeRegion = inside ? region : previousRegion;

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
            pendingDialogueClip = null;
            pendingDialogueHold = false;
            playheadDirection = 1;
            RefreshDialogueClipStatuses(resetPlayed: true);
            RefreshTrackBindings();

            PlayableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            PlayableDirector.extrapolationMode = DirectorWrapMode.None;
            PlayableDirector.time = 0;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void Play(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[TimelineCutscene] Play dipanggil dengan id kosong.", this);
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
            NarrativeHubManager.Instance?.NotifyTimelineCutsceneStart(this);
        }

        void Raise_OnEnded()
        {
            if (cutsceneEndRaised)
                return;

            cutsceneEndRaised = true;
            OnEnded?.Invoke(this);
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

            var trackBinders = FindObjectsByType<TimelineCutsceneTrackBinder>(FindObjectsSortMode.None);
            foreach (var binder in trackBinders)
            {
                if (binder.trackBinds == null)
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

                        case TrackBindType.Cinemachine:
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
                    }
                }
            }
        }

        void RefreshDialogueClipStatuses(bool resetPlayed = false)
        {
            var previousPlayed = resetPlayed
                ? null
                : BuildPlayedLookup();

            dialogueClipStatuses.Clear();

            if (Timeline == null)
                return;

            foreach (var track in Timeline.GetOutputTracks())
            {
                if (track is not CutsceneDialogueTrack)
                    continue;

                foreach (var clip in track.GetClips())
                {
                    var asset = clip.asset as CutsceneDialogueClip;
                    if (asset == null)
                        continue;

                    var status = new CutsceneDialogueClipStatus
                    {
                        start = clip.start,
                        end = clip.end,
                        mode = asset.mode,
                        triggerTime = asset.triggerTime,
                        dialogueSO = asset.dialogueSO,
                        dialogueCollectionId = asset.dialogueCollectionId,
                        isPlayed = false
                    };

                    if (previousPlayed != null
                        && previousPlayed.TryGetValue(MakeClipKey(status.start, status.end), out bool wasPlayed))
                    {
                        status.isPlayed = wasPlayed;
                    }

                    dialogueClipStatuses.Add(status);
                }
            }
        }

        Dictionary<string, bool> BuildPlayedLookup()
        {
            var lookup = new Dictionary<string, bool>();
            for (int i = 0; i < dialogueClipStatuses.Count; i++)
            {
                var status = dialogueClipStatuses[i];
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

            for (int i = 0; i < dialogueClipStatuses.Count; i++)
            {
                var status = dialogueClipStatuses[i];
                if (status == null || status.isPlayed)
                    continue;

                double triggerAt = status.GetTriggerTime();
                bool passes = forward
                    ? previousTime < triggerAt && time >= triggerAt
                    : previousTime > triggerAt && time <= triggerAt;

                if (passes)
                    HandleCutsceneDialogueClip(status);
            }
        }

        void HandleCutsceneDialogueClip(CutsceneDialogueClipStatus status)
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

            pendingDialogueClip = status;
            pendingDialogueHold = status.mode == CutsceneDialogueMode.Pause;
            if (pendingDialogueHold)
                IsDialogueHoldActive = true;
            else if (status.mode == CutsceneDialogueMode.PingPong || status.mode == CutsceneDialogueMode.Repeat)
                IsPlayheadWrapActive = true;

            hub.PlayFullscreenDialogue(status.dialogueSO, status.dialogueCollectionId);
        }

        void HandleFullscreenDialogueEnd(FullscreenDialogueDataSO data, string collectionId)
        {
            if (pendingDialogueClip == null)
                return;

            if (pendingDialogueClip.dialogueSO != data)
                return;

            if (!string.Equals(pendingDialogueClip.dialogueCollectionId, collectionId, StringComparison.Ordinal))
                return;

            var clip = pendingDialogueClip;
            pendingDialogueClip = null;

            if (pendingDialogueHold)
            {
                IsDialogueHoldActive = false;
                pendingDialogueHold = false;
            }

            if (clip.mode == CutsceneDialogueMode.PingPong || clip.mode == CutsceneDialogueMode.Repeat)
                DisablePlayheadWrap();
        }

        bool TryGetActiveWrapRegion(double time, out CutsceneDialogueClipStatus region)
        {
            for (int i = 0; i < dialogueClipStatuses.Count; i++)
            {
                var candidate = dialogueClipStatuses[i];
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

    [Serializable]
    public class CutsceneDialogueClipStatus
    {
        public double start;
        public double end;
        public CutsceneDialogueMode mode;
        public CutsceneDialogueTriggerTime triggerTime;
        public FullscreenDialogueDataSO dialogueSO;
        public string dialogueCollectionId;
        public bool isPlayed;

        public double GetTriggerTime()
        {
            return triggerTime switch
            {
                CutsceneDialogueTriggerTime.Middle => (start + end) * 0.5,
                CutsceneDialogueTriggerTime.End => end,
                _ => start
            };
        }
    }
}
