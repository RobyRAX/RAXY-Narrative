using DG.Tweening;
using RAXY.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace RAXY.Narrative
{
    public class TextOverlayManager : Singleton<TextOverlayManager>
    {
        [TitleGroup("Refs")]
        [SerializeField] Canvas overlayCanvas;

        [TitleGroup("Refs")]
        [SerializeField] TextMeshProUGUI textTmp;

        [TitleGroup("Defaults")]
        [SerializeField] Color defaultColor = Color.white;

        [TitleGroup("Defaults")]
        [SerializeField] string defaultText = "";

        [TitleGroup("Defaults")]
        [SerializeField] float defaultFadeDuration = 0.5f;

        Tween _fadeTween;
        bool _warnedMissingText;

        public TextMeshProUGUI TextTmp => textTmp;

        public Color CurrentColor
        {
            get
            {
                if (textTmp == null)
                    return defaultColor;
                return textTmp.color;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            var initColor = defaultColor;
            initColor.a = 0f;
            ApplyImmediate(initColor);
            SyncCanvasVisibility();
        }

        public bool IsInForceTransition { get; private set; }

        public void SetText(string text)
        {
            if (!EnsureText())
                return;

            textTmp.text = text ?? string.Empty;
        }

        public void SetTextOverlay(string text, Color endColor, float duration, bool force = false)
        {
            SetText(text);
            SetTextOverlay(endColor, duration, force);
        }

        public void SetTextOverlay(Color endColor, float duration, bool force = false)
        {
            if (!EnsureText())
                return;

            if (!force && IsInForceTransition)
                return;

            if (endColor.a > 0f || textTmp.color.a > 0f)
                overlayCanvas?.gameObject.SetActive(true);

            KillFadeTween();
            IsInForceTransition = force;

            if (duration <= 0f)
            {
                ApplyImmediate(endColor);
                if (force)
                    IsInForceTransition = false;
                return;
            }

            _fadeTween = textTmp
                .DOColor(endColor, duration)
                .SetUpdate(true)
                .OnUpdate(SyncRaycastFromCurrentAlpha)
                .OnComplete(() =>
                {
                    SyncRaycastFromCurrentAlpha();
                    SyncCanvasVisibility();
                    if (force)
                        IsInForceTransition = false;
                });
        }

        [TitleGroup("Debug")]
        [Button("Show (Default)")]
        void DebugShow()
        {
            SetTextOverlay(defaultText, defaultColor, defaultFadeDuration);
        }

        [TitleGroup("Debug")]
        [Button("Hide (Default)")]
        void DebugHide()
        {
            var clearColor = CurrentColor;
            clearColor.a = 0f;
            SetTextOverlay(clearColor, defaultFadeDuration);
        }

        void ApplyImmediate(Color color)
        {
            if (textTmp == null)
                return;

            textTmp.color = color;
            textTmp.raycastTarget = color.a > 0f;
            SyncCanvasVisibility();
        }

        void SyncRaycastFromCurrentAlpha()
        {
            if (textTmp == null)
                return;

            textTmp.raycastTarget = textTmp.color.a > 0f;
        }

        void SyncCanvasVisibility()
        {
            if (overlayCanvas == null)
                return;

            overlayCanvas.gameObject.SetActive(textTmp != null && textTmp.color.a > 0f);
        }

        void KillFadeTween()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
                _fadeTween.Kill();
            _fadeTween = null;
        }

        bool EnsureText()
        {
            if (textTmp != null)
                return true;

            if (!_warnedMissingText)
            {
                Debug.LogWarning($"[{nameof(TextOverlayManager)}] Text Tmp is not assigned.", this);
                _warnedMissingText = true;
            }

            return false;
        }

        protected override void OnDestroy()
        {
            KillFadeTween();
            base.OnDestroy();
        }
    }
}
