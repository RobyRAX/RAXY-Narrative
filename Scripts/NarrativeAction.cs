using System;
using System.Collections.Generic;
using RAXY.Event;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class NarrativeAction
    {
        public NarrativeActionType action;

        [TitleGroup("Action Paramater")]
        [HideLabel]
        [ShowIf("IsPlayDialogue")]
        public PlayDialogueParameter playDialogueParameter;

        [TitleGroup("Action Paramater")]
        [HideLabel]
        [ShowIf("IsToggleDialogueBar")]
        public ToggleDialogueBarParameter toggleDialogueBarParameter;

        [TitleGroup("Action Paramater")]
        [HideLabel]
        [ShowIf("IsTriggerDialogueChoice")]
        public TriggerDialogueChoiceParameter triggerDialogueChoiceParameter;

        [TitleGroup("Action Paramater")]
        [HideLabel]
        [ShowIf("IsTriggerEventSO")]
        public EventSoRaiser triggerEventSOParamater;

        [TitleGroup("Action Paramater")]
        [HideLabel]
        [ShowIf("IsPlayTimelineCutscene")]
        public PlayTimelineCutsceneParameter playTimelineCutsceneParameter;

        string Label => action.ToString();

#if UNITY_EDITOR
        bool IsPlayDialogue => action == NarrativeActionType.PlayDialogue;
        bool IsToggleDialogueBar => action == NarrativeActionType.ToggleDialogueBar;
        bool IsTriggerDialogueChoice => action == NarrativeActionType.TriggerDialogueChoice;
        bool IsTriggerEventSO => action == NarrativeActionType.TriggerEventSO;
        bool IsPlayTimelineCutscene => action == NarrativeActionType.PlayTimelineCutscene;

        public static void BindPlayDialogueToParent(List<NarrativeAction> actions, FullscreenDialogueDataSO parentSO)
        {
            if (actions == null || parentSO == null)
                return;

            // foreach (var action in actions)
            // {
            //     if (action == null)
            //         continue;

            //     action.playDialogueParameter ??= new PlayDialogueParameter();
            //     action.playDialogueParameter.SetDialogueSO(parentSO, enableDrawer: false);
            // }
        }
#endif
    }

    public enum NarrativeActionType
    {
        EndDialogue,
        PlayDialogue,
        ToggleDialogueBar,
        TriggerDialogueChoice,
        TriggerEventSO,
        PlayTimelineCutscene,
    }

    [Serializable]
    public class PlayDialogueParameter
    {
        public FullscreenDialogueDataSO dialogueSO;

        [ValueDropdown("CollectionIds")]
        public string collectionId;

#if UNITY_EDITOR
        List<string> CollectionIds => dialogueSO?.CollectionIds;
#endif
    }

    [Serializable]
    public class TriggerDialogueChoiceParameter
    {
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true, ListElementLabelName = "Label")]
        public List<DialogueChoiceEntry> choiceEntries = new();
    }

    [Serializable]
    public class ToggleDialogueBarParameter
    {
        public bool setActive;
    }

    [Serializable]
    public class PlayTimelineCutsceneParameter : ISerializationCallbackReceiver
    {
        [OnValueChanged(nameof(SyncCutsceneName))]
        public TimelineCutscene cutscene;

        [HideInInspector]
        [SerializeField]
        string cutsceneName;

        [ValueDropdown("TimelineIds")]
        public string timelineId;

        public string CutsceneName => cutsceneName;

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
#endif
    }
}
