using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class PlayTimelineCutscene : INarrativeAction, ISerializationCallbackReceiver
    {
        [OnValueChanged(nameof(SyncCutsceneName))]
        public TimelineCutscene cutscene;

        [HideInInspector]
        [SerializeField]
        string cutsceneName;

        [ValueDropdown(nameof(TimelineIds))]
        public string timelineId;

        public string Label =>
            !string.IsNullOrEmpty(timelineId)
                ? $"PlayTimelineCutscene ({timelineId})"
                : "PlayTimelineCutscene";

        public string CutsceneName => cutsceneName;

        public UniTask ExecuteAsync(CancellationToken ct = default)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[PlayTimelineCutscene] NarrativeHubManager.Instance is null.");
                return UniTask.CompletedTask;
            }

            var resolved = ResolveCutscene();
            if (resolved == null)
            {
                Debug.LogWarning(
                    $"[PlayTimelineCutscene] cutscene null (name='{cutsceneName}').");
                return UniTask.CompletedTask;
            }

            if (string.IsNullOrEmpty(timelineId))
            {
                Debug.LogWarning("[PlayTimelineCutscene] timelineId kosong.");
                return UniTask.CompletedTask;
            }

            hub.PlayTimelineCutscene(resolved, timelineId);
            return UniTask.CompletedTask;
        }

        public TimelineCutscene ResolveCutscene()
        {
            if (cutscene != null)
            {
                SyncCutsceneName();
                return cutscene;
            }

            if (string.IsNullOrEmpty(cutsceneName))
                return null;

            var go = GameObject.Find(cutsceneName);
            if (go == null)
                return null;

            return go.GetComponent<TimelineCutscene>();
        }

        void SyncCutsceneName()
        {
            if (cutscene != null)
                cutsceneName = cutscene.gameObject.name;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
            => SyncCutsceneName();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

#if UNITY_EDITOR
        IEnumerable<string> TimelineIds => cutscene != null ? cutscene.TimelineIds : null;
#else
        IEnumerable<string> TimelineIds => null;
#endif
    }
}
