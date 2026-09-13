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

        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(Expanded = true, ListElementLabelName = "Label")]
        public List<INarrativeAction> narrativeActions = new();

        public string Label => lineProvider.String;
    }
}
