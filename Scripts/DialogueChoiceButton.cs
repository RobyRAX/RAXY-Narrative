using System;
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

        [TitleGroup("Event")]
        public UnityEvent OnSetup;

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
            OnSetup?.Invoke();
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
            OnSetup?.Invoke();
        }

        public void BindClick(UnityAction onClick) => BindClick(onClick, 0f);

        internal void BindClick(UnityAction onClick, float inputDelay)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (onClick == null)
                return;

            button.onClick.AddListener(onClick);
            if (inputDelay > 0f && button.enabled)
                DelayInputAsync(inputDelay).Forget();
        }

        async UniTask DelayInputAsync(float seconds)
        {
            // A press that starts during the delay must not select the choice on release.
            button.enabled = false;
            bool canceled = await UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                ignoreTimeScale: true,
                cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();

            if (!canceled && button != null)
                button.enabled = true;
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}
