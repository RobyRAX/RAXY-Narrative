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

        [TitleGroup("State Setter")]
        [Button]
        void SetupEditor()
        {
            stateSetter.SetupEditor(actorSO);
        }

        [TitleGroup("Debug Function")]
        [Button]
        void SetPortraitState()
        {
            portrait.ProcessPortraitStateSetter(stateSetter);
        }
    }
}
