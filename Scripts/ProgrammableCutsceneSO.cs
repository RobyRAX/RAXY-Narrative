using System.Collections.Generic;
using RAXY.Sequence;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [CreateAssetMenu(fileName = "NewProgrammableCutscene", menuName = "RAXY/Narrative/Programmable Cutscene")]
    public class ProgrammableCutsceneSO : ScriptableObject
    {
        [TitleGroup("Cutscene Actions")]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true, ListElementLabelName = "Label")]
        public List<ISequenceable> actions = new();

#if UNITY_EDITOR
        [TitleGroup("Cutscene Actions")]
        [Button("Play Cutscene (Play Mode)", ButtonSizes.Medium)]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        void PlayInEditor()
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[ProgrammableCutsceneSO] NarrativeHubManager is not available.", this);
                return;
            }

            hub.PlayProgrammableCutscene(this);
        }
#endif
    }
}
