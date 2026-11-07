using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class PlayProgrammableCutscene : NarrativeActionBase
    {
        public ProgrammableCutsceneSO cutscene;

        public override string Label =>
            cutscene != null ? $"PlayCutscene ({cutscene.name})" : "PlayCutscene";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            return ExecuteWithCallPathAsync(new HashSet<ProgrammableCutsceneSO>(), ct);
        }

        internal UniTask ExecuteWithCallPathAsync(
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct = default)
        {
            return ProgrammableCutsceneRunner.RunInternalAsync(cutscene, callPath, ct);
        }
    }
}
