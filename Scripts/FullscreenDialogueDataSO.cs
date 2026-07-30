using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace RAXY.Narrative
{
    [CreateAssetMenu(fileName = "FullscreenDialogueDataSO", menuName = "RAXY/Narrative/Fullscreen Dialogue Data")]
    public class FullscreenDialogueDataSO : ScriptableObject
    {
        [TitleGroup("Actors")]
        [ListDrawerSettings(Expanded = true)]
        [OnValueChanged("RefreshEditor")]
        public List<DialogueActorSO> actors;

#if UNITY_EDITOR
        [TitleGroup("Actors")]
        [Button("Refresh Editor")]
        void RefreshEditor()
        {
            if (dialogueCollections == null)
                return;

            foreach (var collection in dialogueCollections)
                collection?.SetupEditor(this, actors);
        }

        void OnValidate()
        {
            RefreshEditor();
        }

        void OnCollectionsChanged(CollectionChangeInfo info)
        {
            if (info.ChangeType != CollectionChangeType.Add && info.ChangeType != CollectionChangeType.Insert)
                return;

            RefreshEditor();
        }
#endif

        [TitleGroup("Dialogue Collections")]
        [ListDrawerSettings(ListElementLabelName = "dialogueCollectionId", Expanded = true)]
        [OnCollectionChanged(After = nameof(OnCollectionsChanged))]
        public List<DialogueCollectionWithPortrait> dialogueCollections;

        public List<string> CollectionIds => dialogueCollections.Select(x => x.DialogueCollectionId).ToList();
    }
}
