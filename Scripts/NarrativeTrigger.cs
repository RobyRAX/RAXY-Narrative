using System.Collections.Generic;
using RAXY.Narrative;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace RAXY.Narrative
{
    public class NarrativeTrigger : MonoBehaviour
    {
        [TitleGroup("Settings")]
        [SerializeField]
        NarrativeType narrativeType;

        [TitleGroup("Settings")]
        [ShowIf(nameof(IsTimelineCutscene))]
        [SerializeField]
        TimelineCutscene timelineCutscene;

        [TitleGroup("Settings")]
        [ShowIf(nameof(IsTimelineCutscene))]
        [ValueDropdown(nameof(TimelineIds))]
        [SerializeField]
        string timelineId;

        [TitleGroup("Settings")]
        [ShowIf(nameof(IsFullscreenDialogue))]
        [SerializeField]
        FullscreenDialogueDataSO fullscreenDialogueDataSO;

        [TitleGroup("Settings")]
        [ShowIf(nameof(IsFullscreenDialogue))]
        [ValueDropdown(nameof(CollectionIds))]
        [SerializeField]
        string collectionId;

        [TitleGroup("Settings")]
        [ShowIf(nameof(IsBanterDialogue))]
        [SerializeField]
        BanterDialogueDataSO banterDialogueDataSO;

        [TitleGroup("Events")]
        public UnityEvent onComplete = new();

        TimelineCutscene _expectedCutscene;

        // void OnDisable()
        // {
        //     UnsubscribeComplete();
        // }

        [TitleGroup("Debug Functions")]
        [Button]
        public void Trigger()
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[NarrativeTrigger] NarrativeHubManager tidak tersedia.", this);
                return;
            }

            UnsubscribeComplete();

            switch (narrativeType)
            {
                case NarrativeType.TimelineCutscene:
                    if (TriggerTimelineCutscene(hub))
                        hub.OnTimelineCutsceneEnd += HandleTimelineCutsceneEnd;
                    break;
                case NarrativeType.FullscreenDialogue:
                    if (TriggerFullscreenDialogue(hub))
                        hub.OnFullscreenDialogueEnd += HandleFullscreenDialogueEnd;
                    break;
                case NarrativeType.BanterDialogue:
                    if (TriggerBanterDialogue(hub))
                        hub.OnBanterDialogueEnd += HandleBanterDialogueEnd;
                    break;
            }
        }

        bool TriggerTimelineCutscene(NarrativeHubManager hub)
        {
            if (timelineCutscene == null)
            {
                Debug.LogWarning("[NarrativeTrigger] TimelineCutscene belum di-assign.", this);
                return false;
            }

            if (hub.TimelineCutsceneRunner == null)
            {
                Debug.LogWarning("[NarrativeTrigger] TimelineCutsceneRunner belum di-assign di NarrativeHubManager.", this);
                return false;
            }

            hub.TimelineCutsceneRunner.PlayCutscene(timelineCutscene, timelineId);
            _expectedCutscene = hub.TimelineCutsceneRunner.CurrentCutscene;
            return _expectedCutscene != null;
        }

        bool TriggerFullscreenDialogue(NarrativeHubManager hub)
        {
            if (fullscreenDialogueDataSO == null)
            {
                Debug.LogWarning("[NarrativeTrigger] FullscreenDialogueDataSO belum di-assign.", this);
                return false;
            }

            hub.PlayFullscreenDialogue(fullscreenDialogueDataSO, collectionId);
            return hub.FullscreenDialogueView != null;
        }

        bool TriggerBanterDialogue(NarrativeHubManager hub)
        {
            if (banterDialogueDataSO == null)
            {
                Debug.LogWarning("[NarrativeTrigger] BanterDialogueDataSO belum di-assign.", this);
                return false;
            }

            hub.PlayBanterDialogue(banterDialogueDataSO);
            return hub.BanterDialogueView != null;
        }

        void HandleTimelineCutsceneEnd(TimelineCutscene cutscene)
        {
            if (cutscene != _expectedCutscene)
                return;

            Complete();
        }

        void HandleFullscreenDialogueEnd(FullscreenDialogueDataSO data, string endedCollectionId)
        {
            if (data != fullscreenDialogueDataSO || endedCollectionId != collectionId)
                return;

            Complete();
        }

        void HandleBanterDialogueEnd(BanterDialogueDataSO data)
        {
            if (data != banterDialogueDataSO)
                return;

            Complete();
        }

        void Complete()
        {
            UnsubscribeComplete();
            onComplete?.Invoke();
        }

        void UnsubscribeComplete()
        {
            _expectedCutscene = null;

            var hub = NarrativeHubManager.Instance;
            if (hub == null)
                return;

            hub.OnTimelineCutsceneEnd -= HandleTimelineCutsceneEnd;
            hub.OnFullscreenDialogueEnd -= HandleFullscreenDialogueEnd;
            hub.OnBanterDialogueEnd -= HandleBanterDialogueEnd;
        }

        bool IsTimelineCutscene => narrativeType == NarrativeType.TimelineCutscene;
        bool IsFullscreenDialogue => narrativeType == NarrativeType.FullscreenDialogue;
        bool IsBanterDialogue => narrativeType == NarrativeType.BanterDialogue;

#if UNITY_EDITOR
        IEnumerable<string> TimelineIds => timelineCutscene != null ? timelineCutscene.TimelineIds : null;
        List<string> CollectionIds => fullscreenDialogueDataSO?.CollectionIds;
#else
        IEnumerable<string> TimelineIds => null;
        List<string> CollectionIds => null;
#endif
    }

    public enum NarrativeType
    {
        TimelineCutscene,
        FullscreenDialogue,
        BanterDialogue
    }
}