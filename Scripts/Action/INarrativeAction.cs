using System.Threading;
using Cysharp.Threading.Tasks;

namespace RAXY.Narrative
{
    public interface INarrativeAction
    {
        string Label { get; }

        UniTask ExecuteAsync(CancellationToken ct = default);
    }
}
