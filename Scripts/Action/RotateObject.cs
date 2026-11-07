using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class RotateObject : NarrativeActionBase
    {
        [HideLabel]
        public NarrativeObjectTarget target = new();

        [Tooltip("Target rotation in Euler angles.")]
        public Vector3 destinationRotation;

        [MinValue(0f)]
        [SuffixLabel("s", true)]
        public float duration;

        [Tooltip("If true, rotates in local space (localEulerAngles); otherwise world space (eulerAngles).")]
        public bool isLocal;

        public RotateMode rotateMode = RotateMode.Fast;

        public Ease ease = Ease.Linear;

        public override string Label =>
            $"RotateObject ({(target != null ? target.TargetDescription : "None")}) -> {destinationRotation} ({(isLocal ? "Local" : "World")}, {duration}s)";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var t = target?.Resolve();
            if (t == null)
            {
                Debug.LogWarning("[RotateObject] Skipping rotate because target could not be resolved.");
                return UniTask.CompletedTask;
            }

            if (duration <= 0f)
            {
                if (isLocal)
                    t.localEulerAngles = destinationRotation;
                else
                    t.eulerAngles = destinationRotation;

                return UniTask.CompletedTask;
            }

            Tween tween = isLocal
                ? t.DOLocalRotate(destinationRotation, duration, rotateMode).SetEase(ease)
                : t.DORotate(destinationRotation, duration, rotateMode).SetEase(ease);

            return tween.AwaitTweenAsync(ct);
        }
    }
}
