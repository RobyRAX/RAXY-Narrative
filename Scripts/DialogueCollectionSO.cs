using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [CreateAssetMenu(fileName = "DialogueCollectionSO", menuName = "RAXY/Narrative/Dialogue Collection")]
    public class DialogueCollectionSO : ScriptableObject
    {
        [HideLabel]
        public DialogueCollection dialogueCollection;
    }
}
