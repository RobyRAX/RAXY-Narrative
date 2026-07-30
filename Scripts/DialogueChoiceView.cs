using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    public class DialogueChoiceView : MonoBehaviour
    {
        [TitleGroup("UI Ref")]
        [SerializeField]
        Transform choiceContainer;

        [TitleGroup("Prefab Ref")]
        [SerializeField]
        DialogueChoiceButton choiceBtnPref;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        List<DialogueChoiceButton> spawnedChoices = new();

        CancellationTokenSource _waitCts;
        UniTaskCompletionSource<int> _choiceTcs;

        public event Action<int> OnChoiceSelected;
        public int SpawnedCount => spawnedChoices?.Count ?? 0;

        [TitleGroup("Test")]
        [SerializeField]
        List<DialogueChoiceEntry> test_Entries;

        [TitleGroup("Test")]
        [Button]
        void TestSetup()
        {
            Setup(test_Entries);
        }

        public void Setup(List<DialogueChoiceEntry> choiceEntries)
        {
            ClearChoices();
            gameObject.SetActive(true);

            if (!TryValidate(choiceEntries))
                return;

            for (int i = 0; i < choiceEntries.Count; i++)
            {
                if (choiceEntries[i] == null)
                    continue;

                int index = i;
                var btn = Instantiate(choiceBtnPref, choiceContainer);
                btn.Setup(choiceEntries[i], index);
                btn.BindClick(() => HandleChoiceSelected(index));
                spawnedChoices.Add(btn);
            }
        }

        public async UniTask SetupAsync(List<DialogueChoiceEntry> choiceEntries)
        {
            ClearChoices();
            gameObject.SetActive(true);

            if (!TryValidate(choiceEntries))
                return;

            for (int i = 0; i < choiceEntries.Count; i++)
            {
                if (choiceEntries[i] == null)
                    continue;

                int index = i;
                var btn = Instantiate(choiceBtnPref, choiceContainer);
                await btn.SetupAsync(choiceEntries[i], index);
                btn.BindClick(() => HandleChoiceSelected(index));
                spawnedChoices.Add(btn);
            }
        }

        bool TryValidate(List<DialogueChoiceEntry> choiceEntries)
        {
            if (choiceEntries == null || choiceEntries.Count == 0)
            {
                Debug.LogWarning("[DialogueChoiceView] choiceEntries kosong.", this);
                return false;
            }

            if (choiceContainer == null || choiceBtnPref == null)
            {
                Debug.LogWarning("[DialogueChoiceView] choiceContainer atau choiceBtnPref tidak di-assign.", this);
                return false;
            }

            return true;
        }

        public void ClearChoices()
        {
            if (spawnedChoices != null)
            {
                foreach (var btn in spawnedChoices)
                {
                    if (btn != null)
                        Destroy(btn.gameObject);
                }

                spawnedChoices.Clear();
            }

            if (choiceContainer != null)
            {
                foreach (Transform child in choiceContainer)
                    Destroy(child.gameObject);
            }
        }

        void HandleChoiceSelected(int index)
        {
            OnChoiceSelected?.Invoke(index);
            Debug.Log("Selected choice index - " + index);
            _choiceTcs?.TrySetResult(index);
            ClearChoices();
        }

        public async UniTask<int> WaitForChoiceAsync(
            List<DialogueChoiceEntry> choiceEntries,
            CancellationToken ct = default)
        {
            _waitCts?.Cancel();
            _waitCts?.Dispose();
            _waitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _waitCts.Token;

            _choiceTcs = new UniTaskCompletionSource<int>();

            try
            {
                await SetupAsync(choiceEntries);

                if (SpawnedCount == 0)
                {
                    Debug.LogWarning("[DialogueChoiceView] Tidak ada choice button yang di-spawn.", this);
                    return -1;
                }

                return await _choiceTcs.Task.AttachExternalCancellation(token);
            }
            finally
            {
                _choiceTcs = null;
                _waitCts?.Dispose();
                _waitCts = null;
                ClearChoices();
            }
        }

        void OnDestroy()
        {
            _waitCts?.Cancel();
            _waitCts?.Dispose();
            _waitCts = null;
            _choiceTcs = null;
            ClearChoices();
        }
    }
}
