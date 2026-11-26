using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace RAXY.Narrative
{
    [DisallowMultipleComponent]
    public class BarkBubble : MonoBehaviour
    {
        [TitleGroup("Identity")]
        [ValueDropdown(nameof(GetDropdownIds), AppendNextDrawer = true)]
        [SerializeField]
        string bubbleId = string.Empty;

        public TextMeshProUGUI dialogueTmp;
        public TextTyper textTyper;

        [Min(0f)]
        public float fadeInDuration = 0.2f;

        [Min(0f)]
        public float fadeOutDuration = 0.2f;

        CanvasGroup _canvasGroup;
        int _fadeVersion;
        string _registeredId;

        public string BubbleId => bubbleId;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponentInChildren<CanvasGroup>();

            if (textTyper == null)
                textTyper = GetComponentInChildren<TextTyper>(true);

            ApplyCanvasGroupImmediate(0f);

            if (dialogueTmp != null)
                dialogueTmp.text = string.Empty;
        }

        void OnEnable()
        {
            EnsureRegistered();
        }

        void OnDisable()
        {
            UnregisterFromManager();
        }

        public void SetBubbleId(string newId)
        {
            var normalized = newId?.Trim() ?? string.Empty;
            if (string.Equals(_registeredId, normalized, StringComparison.Ordinal) &&
                string.Equals(bubbleId, normalized, StringComparison.Ordinal))
                return;

            UnregisterFromManager();
            bubbleId = normalized;

            if (isActiveAndEnabled)
                EnsureRegistered();
        }

        internal void EnsureRegistered()
        {
            if (string.IsNullOrWhiteSpace(bubbleId))
                return;

            var normalized = bubbleId.Trim();
            if (string.Equals(_registeredId, normalized, StringComparison.Ordinal))
                return;

            var manager = BarkDialogueManager.Instance;
            if (manager == null)
                return;

            if (manager.RegisterBubble(normalized, this))
                _registeredId = normalized;
        }

        void UnregisterFromManager()
        {
            if (string.IsNullOrWhiteSpace(_registeredId))
                return;

            BarkDialogueManager.Instance?.UnregisterBubble(_registeredId, this);
            _registeredId = null;
        }

        public async UniTask ShowAsync(CancellationToken ct = default)
        {
            if (_canvasGroup == null)
            {
                Debug.LogWarning("[BarkBubble] CanvasGroup is missing; cannot fade in.", this);
                return;
            }

            await FadeCanvasGroupAsync(1f, fadeInDuration, ct);
        }

        public async UniTask HideAsync(CancellationToken ct = default)
        {
            if (_canvasGroup == null)
            {
                Debug.LogWarning("[BarkBubble] CanvasGroup is missing; cannot fade out.", this);
                return;
            }

            await FadeCanvasGroupAsync(0f, fadeOutDuration, ct);
        }

        public async UniTask PlayDialogueTextAsync(string text, CancellationToken ct = default)
        {
            if (textTyper != null)
            {
                textTyper.StartTyping(text ?? string.Empty);

                if (textTyper.IsTyping)
                    await UniTask.WaitUntil(() => !textTyper.IsTyping, cancellationToken: ct);
                return;
            }

            if (dialogueTmp != null)
                dialogueTmp.text = text ?? string.Empty;
        }

        public void ClearDialogueText()
        {
            if (textTyper != null)
                textTyper.StopAllCoroutines();

            if (dialogueTmp != null)
                dialogueTmp.text = string.Empty;
        }

        void ApplyCanvasGroupImmediate(float alpha)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = alpha;
            var visible = alpha > 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        async UniTask FadeCanvasGroupAsync(float targetAlpha, float duration, CancellationToken ct)
        {
            var fadeVersion = ++_fadeVersion;
            float startAlpha = _canvasGroup.alpha;
            duration = Mathf.Max(0f, duration);

            var visible = targetAlpha > 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;

            if (duration <= 0f || Mathf.Approximately(startAlpha, targetAlpha))
            {
                if (fadeVersion == _fadeVersion)
                    ApplyCanvasGroupImmediate(targetAlpha);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                if (fadeVersion != _fadeVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (fadeVersion != _fadeVersion)
                return;

            ApplyCanvasGroupImmediate(targetAlpha);
        }

#if UNITY_EDITOR
        IEnumerable<string> GetDropdownIds() => BarkBubbleEditorIdProvider.GetDropdownIds();
#else
        IEnumerable<string> GetDropdownIds() => null;
#endif
    }
}
