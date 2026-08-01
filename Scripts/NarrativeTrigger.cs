using System.Collections.Generic;
using RAXY.Narrative;
using Sirenix.OdinInspector;
using UnityEngine;

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

            switch (narrativeType)
            {
                case NarrativeType.TimelineCutscene:
                    TriggerTimelineCutscene(hub);
                    break;
                case NarrativeType.FullscreenDialogue:
                    TriggerFullscreenDialogue(hub);
                    break;
                case NarrativeType.BanterDialogue:
                    TriggerBanterDialogue(hub);
                    break;
            }
        }

        void TriggerTimelineCutscene(NarrativeHubManager hub)
        {
            if (timelineCutscene == null)
            {
                Debug.LogWarning("[NarrativeTrigger] TimelineCutscene belum di-assign.", this);
                return;
            }

            if (hub.TimelineCutsceneRunner == null)
            {
                Debug.LogWarning("[NarrativeTrigger] TimelineCutsceneRunner belum di-assign di NarrativeHubManager.", this);
                return;
            }

            hub.TimelineCutsceneRunner.PlayCutscene(timelineCutscene, timelineId);
        }

        void TriggerFullscreenDialogue(NarrativeHubManager hub)
        {
            if (fullscreenDialogueDataSO == null)
            {
                Debug.LogWarning("[NarrativeTrigger] FullscreenDialogueDataSO belum di-assign.", this);
                return;
            }

            hub.PlayFullscreenDialogue(fullscreenDialogueDataSO, collectionId);
        }

        void TriggerBanterDialogue(NarrativeHubManager hub)
        {
            if (banterDialogueDataSO == null)
            {
                Debug.LogWarning("[NarrativeTrigger] BanterDialogueDataSO belum di-assign.", this);
                return;
            }

            hub.PlayBanterDialogue(banterDialogueDataSO);
        }

        bool IsTimelineCutscene => narrativeType == NarrativeType.TimelineCutscene;
        bool IsFullscreenDialogue => narrativeType == NarrativeType.FullscreenDialogue;
        bool IsBanterDialogue => narrativeType == NarrativeType.BanterDialogue;

#if UNITY_EDITOR
        IEnumerable<string> TimelineIds => timelineCutscene != null ? timelineCutscene.TimelineIds : null;
        List<string> CollectionIds => fullscreenDialogueDataSO?.CollectionIds;
#endif
    }

    public enum NarrativeType
    {
        TimelineCutscene,
        FullscreenDialogue,
        BanterDialogue
    }
}