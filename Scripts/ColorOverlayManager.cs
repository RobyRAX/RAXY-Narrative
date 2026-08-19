using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using RAXY.Utility;

namespace RAXY.Narrative
{
    public class ColorOverlayManager : Singleton<ColorOverlayManager>
    {
        [TitleGroup("Refs")]
        [SerializeField] Canvas overlayCanvas;

        [TitleGroup("Refs")]
        [SerializeField] Image overlayImage;

        [TitleGroup("Defaults")]
        [SerializeField] Color defaultColor = Color.black;

        [TitleGroup("Defaults")]
        [SerializeField] float defaultFadeDuration = 0.5f;

        Tween _fadeTween;
        bool _warnedMissingImage;

        public Image OverlayImage => overlayImage;

        public Color CurrentColor
        {
            get
            {
                if (overlayImage == null)
                    return defaultColor;
                return overlayImage.color;
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

        public void SetColorOverlay(Color endColor, float duration, bool force = false)
        {
            if (!EnsureImage())
                return;

            if (!force && IsInForceTransition)
                return;

            if (endColor.a > 0f || overlayImage.color.a > 0f)
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

            _fadeTween = overlayImage
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
        void DebugShow() => SetColorOverlay(defaultColor, defaultFadeDuration);

        [TitleGroup("Debug")]
        [Button("Hide (Default)")]
        void DebugHide()
        {
            var clearColor = CurrentColor;
            clearColor.a = 0f;
            SetColorOverlay(clearColor, defaultFadeDuration);
        }

        void ApplyImmediate(Color color)
        {
            if (overlayImage == null)
                return;

            overlayImage.color = color;
            overlayImage.raycastTarget = color.a > 0f;
            SyncCanvasVisibility();
        }

        void SyncRaycastFromCurrentAlpha()
        {
            if (overlayImage == null)
                return;

            overlayImage.raycastTarget = overlayImage.color.a > 0f;
        }

        void SyncCanvasVisibility()
        {
            if (overlayCanvas == null)
                return;

            overlayCanvas.gameObject.SetActive(overlayImage != null && overlayImage.color.a > 0f);
        }

        void KillFadeTween()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
                _fadeTween.Kill();
            _fadeTween = null;
        }

        bool EnsureImage()
        {
            if (overlayImage != null)
                return true;

            if (!_warnedMissingImage)
            {
                Debug.LogWarning($"[{nameof(ColorOverlayManager)}] Overlay Image is not assigned.", this);
                _warnedMissingImage = true;
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
