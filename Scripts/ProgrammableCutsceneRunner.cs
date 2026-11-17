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

            bool trace = cutscene.name == "KirianFreed_Programmable";
            if (trace)
                Debug.Log($"[CutsceneDebug] START {cutscene.name}; actions={cutscene.actions?.Count ?? 0}; frame={Time.frameCount}.", cutscene);

            try
            {
                await RunActionsInternalAsync(cutscene.actions, nextCallPath, ct, trace);
            }
            finally
            {
                if (trace)
                    Debug.Log($"[CutsceneDebug] END {cutscene.name}; canceled={ct.IsCancellationRequested}; frame={Time.frameCount}.", cutscene);
            }
        }

        internal static async UniTask RunActionsInternalAsync(
            IReadOnlyList<ISequenceable> actions,
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct = default,
            bool trace = false)
        {
            if (actions == null || actions.Count == 0)
                return;

            var pendingTasks = new List<UniTask>();
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

                    UniTask actionTask = trace
                        ? TraceActionAsync(action, i, actions.Count, callPath, ct)
                        : ExecuteActionWithContextAsync(action, callPath, ct);

                    if (i < actions.Count - 1 && !action.OverrideNextActionTime)
                    {
                        await actionTask;
                    }
                    else
                    {
                        pendingTasks.Add(actionTask);

                        if (i < actions.Count - 1)
                        {
                            float delay = Mathf.Max(0f, action.NextActionTimeOverrider);
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

            if (pendingTasks.Count > 0)
            {
                try
                {
                    await UniTask.WhenAll(pendingTasks);
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

        static async UniTask TraceActionAsync(
            ISequenceable action,
            int index,
            int count,
            HashSet<ProgrammableCutsceneSO> callPath,
            CancellationToken ct)
        {
            Debug.Log($"[CutsceneDebug] Action {index + 1}/{count} START {action.GetType().Name} ({action.Label}); overrideNext={action.OverrideNextActionTime}; frame={Time.frameCount}.");
            try
            {
                await ExecuteActionWithContextAsync(action, callPath, ct);
                Debug.Log($"[CutsceneDebug] Action {index + 1}/{count} END {action.GetType().Name}; canceled={ct.IsCancellationRequested}; frame={Time.frameCount}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CutsceneDebug] Action {index + 1}/{count} FAILED {action.GetType().Name}: {ex}");
                throw;
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
                return RunActionsInternalAsync(group.actions, callPath, ct);
            }

            return action.ExecuteAsync(ct);
        }
    }
}
