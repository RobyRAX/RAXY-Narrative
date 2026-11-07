using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace RAXY.Narrative
{
    public static class CutsceneTweenExtensions
    {
        public static async UniTask AwaitTweenAsync(this Tween tween, CancellationToken ct = default)
        {
            if (tween == null || !tween.IsActive())
                return;

            var tcs = new UniTaskCompletionSource();

            using (ct.Register(() =>
            {
                if (tween.IsActive())
                    tween.Kill();
                tcs.TrySetCanceled(ct);
            }))
            {
                tween.OnComplete(() => tcs.TrySetResult());
                tween.OnKill(() =>
                {
                    if (ct.IsCancellationRequested)
                        tcs.TrySetCanceled(ct);
                    else
                        tcs.TrySetResult();
                });

                await tcs.Task;
            }
        }
    }
}
