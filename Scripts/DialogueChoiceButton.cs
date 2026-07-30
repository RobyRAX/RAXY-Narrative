using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RAXY.Narrative
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [TitleGroup("UI Ref")]
        [SerializeField]
        Button button;

        [TitleGroup("UI Ref")]
        [SerializeField]
        TextMeshProUGUI choiceTxt;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        DialogueChoiceEntry entry;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        int choiceIndex;

        public int ChoiceIndex => choiceIndex;
        public DialogueChoiceEntry Entry => entry;

        public void Setup(DialogueChoiceEntry entry, int index)
        {
            this.entry = entry;
            choiceIndex = index;

            if (choiceTxt == null)
                return;

            choiceTxt.text = entry?.lineProvider != null ? entry.lineProvider.String : string.Empty;
        }

        public async UniTask SetupAsync(DialogueChoiceEntry entry, int index)
        {
            this.entry = entry;
            choiceIndex = index;

            if (choiceTxt == null)
                return;

            if (entry?.lineProvider == null)
            {
                choiceTxt.text = string.Empty;
                return;
            }

            choiceTxt.text = await entry.lineProvider.GetStringAsync();
        }

        public void BindClick(UnityAction onClick)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(onClick);
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}
