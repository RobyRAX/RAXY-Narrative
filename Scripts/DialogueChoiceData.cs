using System;
using System.Collections.Generic;
using RAXY.Utility.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class DialogueChoiceEntry
    {
        public StringProvider lineProvider;

        // SerializeReference breaks the NarrativeAction ↔ DialogueChoiceEntry
        // value-nesting cycle that triggers Unity's inspector recursion cutoff.
        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(
            Expanded = true,
            ListElementLabelName = "Label",
            CustomAddFunction = nameof(AddNarrativeAction))]
        public List<NarrativeAction> narrativeActions = new();

        public string Label => lineProvider.String;

#if UNITY_EDITOR
        NarrativeAction AddNarrativeAction() => new NarrativeAction();
#endif
    }
}
