using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Sequence;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class ActionGroup : NarrativeActionBase
    {
        [TitleGroup("Group Actions")]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true, ListElementLabelName = "Label")]
        public List<ISequenceable> actions = new();

        public override string Label =>
            $"ActionGroup ({(actions != null ? actions.Count : 0)} actions)";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            return ExecuteWithCallPathAsync(new HashSet<ProgrammableCutsceneSO>(), ct);
        }

        internal UniTask ExecuteWithCallPathAsync(
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct = default)
        {
            return ProgrammableCutsceneRunner.RunActionsInternalAsync(actions, callPath, ct);
        }
    }
}
