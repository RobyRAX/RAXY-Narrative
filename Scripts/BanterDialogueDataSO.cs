using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace RAXY.Narrative
{
    [CreateAssetMenu(fileName = "BanterDialogueDataSO", menuName = "RAXY/Narrative/Banter Dialogue Data")]
    public class BanterDialogueDataSO : ScriptableObject
    {
        [TitleGroup("Actors")]
        [ListDrawerSettings(Expanded = true)]
        [OnValueChanged("RefreshActors")]
        public List<DialogueActorSO> actors;

#if UNITY_EDITOR
        [TitleGroup("Actors")]
        [Button]
        void RefreshActors()
        {
            if (dialogueCollection == null)
                return;

            // foreach (var collection in dialogueCollections)
            //     collection?.SetupActors(actors);

            dialogueCollection?.SetupActors(actors);
        }

        void OnCollectionsChanged(CollectionChangeInfo info)
        {
            if (info.ChangeType != CollectionChangeType.Add && info.ChangeType != CollectionChangeType.Insert)
                return;

            RefreshActors();
        }
#endif

        [TitleGroup("Dialogue Collection")]
        [HideLabel]
        //[ListDrawerSettings(ListElementLabelName = "dialogueCollectionId", Expanded = true)]
        //[OnCollectionChanged(After = nameof(OnCollectionsChanged))]
        public DialogueCollectionWithBanter dialogueCollection;
    }
}
