using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class MoveObject : NarrativeActionBase
    {
        [HideLabel]
        public NarrativeObjectTarget target = new();

        public Vector3 destination;

        [MinValue(0f)]
        [SuffixLabel("s", true)]
        public float duration;

        [Tooltip("If true, moves in local space (localPosition); otherwise world space (position).")]
        public bool isLocal;

        public Ease ease = Ease.Linear;

        public override string Label =>
            $"MoveObject ({(target != null ? target.TargetDescription : "None")}) -> {destination} ({(isLocal ? "Local" : "World")}, {duration}s)";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var t = target?.Resolve();
            if (t == null)
            {
                Debug.LogWarning("[MoveObject] Skipping move because target could not be resolved.");
                return UniTask.CompletedTask;
            }

            if (duration <= 0f)
            {
                if (isLocal)
                    t.localPosition = destination;
                else
                    t.position = destination;

                return UniTask.CompletedTask;
            }

            Tween tween = isLocal
                ? t.DOLocalMove(destination, duration).SetEase(ease)
                : t.DOMove(destination, duration).SetEase(ease);

            return tween.AwaitTweenAsync(ct);
        }
    }
}
