using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Sequence;
using UnityEngine;

namespace RAXY.Narrative
{
    public class ProgrammableCutsceneRunner
    {
        public static async UniTask RunAsync(
            ProgrammableCutsceneSO cutscene,
            CancellationToken ct = default)
        {
            if (cutscene == null)
            {
                Debug.LogWarning("[ProgrammableCutsceneRunner] Cutscene is null.");
                return;
            }

            var callPath = new HashSet<ProgrammableCutsceneSO>();
            await RunInternalAsync(cutscene, callPath, ct);
        }

        public static async UniTask RunActionsAsync(
            IReadOnlyList<ISequenceable> actions,
            CancellationToken ct = default)
        {
            await RunActionsInternalAsync(actions, new HashSet<ProgrammableCutsceneSO>(), ct);
        }

        internal static async UniTask RunInternalAsync(
            ProgrammableCutsceneSO cutscene,
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct = default)
        {
            if (cutscene == null)
            {
                Debug.LogWarning("[ProgrammableCutsceneRunner] Cutscene is null.");
                return;
            }

            if (callPath != null && callPath.Contains(cutscene))
            {
                Debug.LogError($"[ProgrammableCutsceneRunner] Recursive cutscene reference detected: '{cutscene.name}'. Aborting execution to prevent infinite recursion.");
                return;
            }

            var nextCallPath = callPath != null
                ? new HashSet<ProgrammableCutsceneSO>(callPath) { cutscene }
                : new HashSet<ProgrammableCutsceneSO> { cutscene };

            await RunActionsInternalAsync(cutscene.actions, nextCallPath, ct);
        }

        internal static async UniTask RunActionsInternalAsync(
            IReadOnlyList<ISequenceable> actions,
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct = default)
        {
            if (actions == null || actions.Count == 0)
                return;

            var startedTasks = new List<UniTask>();
            Exception caughtException = null;

            try
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var action = actions[i];
                    if (action == null)
                    {
                        Debug.LogWarning($"[ProgrammableCutsceneRunner] Action at index {i} is null — skipping.");
                        continue;
                    }

                    UniTask actionTask = ExecuteActionWithContextAsync(action, callPath, ct);
                    startedTasks.Add(actionTask);

                    if (i < actions.Count - 1)
                    {
                        if (!action.OverrideNextActionTime)
                        {
                            await actionTask;
                        }
                        else
                        {
                            float delay = Mathf.Max(0f, action.NextActionTime);
                            if (delay > 0f)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            if (startedTasks.Count > 0)
            {
                try
                {
                    await UniTask.WhenAll(startedTasks);
                }
                catch (Exception ex)
                {
                    if (caughtException == null)
                        caughtException = ex;
                }
            }

            if (caughtException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caughtException).Throw();
            }
        }

        static UniTask ExecuteActionWithContextAsync(
            ISequenceable action,
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct)
        {
            if (action is PlayProgrammableCutscene playCutscene)
            {
                return playCutscene.ExecuteWithCallPathAsync(callPath, ct);
            }

            if (action is ActionGroup group)
            {
                return group.ExecuteWithCallPathAsync(callPath, ct);
            }

            return action.ExecuteAsync(ct);
        }
    }
}
