using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    public class DialoguePortraitTest : MonoBehaviour
    {
        public DialogueActorSO actorSO;
        public DialoguePortrait portrait;

        [TitleGroup("State Setter")]
        [SerializeField]
        [HideLabel]
        PortraitStateSetter stateSetter;

#if UNITY_EDITOR
        [TitleGroup("State Setter")]
        [Button]
        void SetupEditor()
        {
            stateSetter.SetupEditor(actorSO);
        }
#endif

        [TitleGroup("Debug Function")]
        [Button]
        void SetPortraitState()
        {
            portrait.ProcessPortraitStateSetter(stateSetter);
        }
    }
}
