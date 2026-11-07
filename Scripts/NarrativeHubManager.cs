using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Sequence;
using RAXY.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace RAXY.Narrative
{
    [Serializable]
    public class FullscreenDialogueUnityEvent : UnityEvent<FullscreenDialogueDataSO, string> { }

    [Serializable]
    public class BanterDialogueUnityEvent : UnityEvent<BanterDialogueDataSO> { }

    [Serializable]
    public class TimelineCutsceneUnityEvent : UnityEvent<TimelineCutscene> { }

    [Serializable]
    public class TimelineCutsceneNameUnityEvent : UnityEvent<string> { }

    [Serializable]
    public class DialogueChoiceUnityEvent : UnityEvent<int> { }

    [Serializable]
    public class ProgrammableCutsceneUnityEvent : UnityEvent<ProgrammableCutsceneSO> { }

    public class NarrativeHubManager : Singleton<NarrativeHubManager>
    {
        [TitleGroup("Component")]
        public FullscreenDialogueView FullscreenDialogueView;

        [TitleGroup("Component")]
        public BanterDialogueView BanterDialogueView;

        [TitleGroup("Component")]
        public DialogueChoiceView DialogueChoiceView;

        [TitleGroup("Component")]
        public TimelineCutsceneRunner TimelineCutsceneRunner;

        [TitleGroup("Unity Events")]
        [FoldoutGroup("Unity Events/Fullscreen Dialogue")]
        public FullscreenDialogueUnityEvent onFullscreenDialogueStart = new();

        [FoldoutGroup("Unity Events/Fullscreen Dialogue")]
        public FullscreenDialogueUnityEvent onFullscreenDialogueEnd = new();

        [FoldoutGroup("Unity Events/Banter Dialogue")]
        public BanterDialogueUnityEvent onBanterDialogueStart = new();

        [FoldoutGroup("Unity Events/Banter Dialogue")]
        public BanterDialogueUnityEvent onBanterDialogueEnd = new();

        [FoldoutGroup("Unity Events/Timeline Cutscene")]
        public TimelineCutsceneUnityEvent onTimelineCutsceneStart = new();

        [FoldoutGroup("Unity Events/Timeline Cutscene")]
        public TimelineCutsceneUnityEvent onTimelineCutsceneEnd = new();

        [FoldoutGroup("Unity Events/Timeline Cutscene")]
        public TimelineCutsceneNameUnityEvent onTimelineCutsceneStartName = new();

        [FoldoutGroup("Unity Events/Timeline Cutscene")]
        public TimelineCutsceneNameUnityEvent onTimelineCutsceneEndName = new();

        [FoldoutGroup("Unity Events/Dialogue Choice")]
        public DialogueChoiceUnityEvent onDialogueChoiceSelected = new();

        [FoldoutGroup("Unity Events/Programmable Cutscene")]
        public ProgrammableCutsceneUnityEvent onProgrammableCutsceneStart = new();

        [FoldoutGroup("Unity Events/Programmable Cutscene")]
        public ProgrammableCutsceneUnityEvent onProgrammableCutsceneEnd = new();

        [TitleGroup("Test")]
        [SerializeReference]
        [HideReferenceObjectPicker]
        ISequenceable test_Action;

        [TitleGroup("Test")]
        [Button]
        void Test_NarrativeAction()
        {
            Process_NarrativeActionAsync(test_Action).Forget();
        }

        public event Action<FullscreenDialogueDataSO, string> OnFullscreenDialogueStart;
        public event Action<FullscreenDialogueDataSO, string> OnFullscreenDialogueEnd;
        public event Action<BanterDialogueDataSO> OnBanterDialogueStart;
        public event Action<BanterDialogueDataSO> OnBanterDialogueEnd;
        public event Action<TimelineCutscene> OnTimelineCutsceneStart;
        public event Action<TimelineCutscene> OnTimelineCutsceneEnd;
        public event Action<int> OnDialogueChoiceSelected;
        public event Action<ProgrammableCutsceneSO> OnProgrammableCutsceneStart;
        public event Action<ProgrammableCutsceneSO> OnProgrammableCutsceneEnd;

        void OnEnable()
        {
            BindViewEvents(true);
        }

        void OnDisable()
        {
            BindViewEvents(false);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayFullscreenDialogue(FullscreenDialogueDataSO data, string collectionId = null)
        {
            if (FullscreenDialogueView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] FullscreenDialogueView belum di-assign.", this);
                return;
            }

            FullscreenDialogueView.Play(data, collectionId);
        }

        public UniTask PlayFullscreenDialogueAsync(
            FullscreenDialogueDataSO data,
            string collectionId,
            CancellationToken ct = default)
        {
            if (FullscreenDialogueView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] FullscreenDialogueView belum di-assign.", this);
                return UniTask.CompletedTask;
            }

            return FullscreenDialogueView.PlayAsync(data, collectionId, ct);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void EndFullscreenDialogue()
        {
            if (FullscreenDialogueView == null)
                return;

            FullscreenDialogueView.EndDialogue();
        }

        // public UniTask EndFullscreenDialogueAsync(CancellationToken ct = default)
        // {
        //     if (FullscreenDialogueView == null)
        //     {
        //         Debug.LogWarning("[NarrativeHubManager] FullscreenDialogueView belum di-assign.", this);
        //         return UniTask.CompletedTask;
        //     }

        //     return FullscreenDialogueView.EndDialogueAsync(ct);
        // }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayBanterDialogue(BanterDialogueDataSO data)
        {
            if (BanterDialogueView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] BanterDialogueView belum di-assign.", this);
                return;
            }

            BanterDialogueView.Play(data);
        }

        public UniTask PlayBanterDialogueAsync(BanterDialogueDataSO data, CancellationToken ct = default)
        {
            if (BanterDialogueView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] BanterDialogueView belum di-assign.", this);
                return UniTask.CompletedTask;
            }

            return BanterDialogueView.PlayAsync(data, ct);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void EndBanterDialogue()
        {
            if (BanterDialogueView == null)
                return;

            BanterDialogueView.EndDialogue();
        }

        public void PlayDialogueChoice(List<DialogueChoiceEntry> choiceEntries)
        {
            if (DialogueChoiceView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] DialogueChoiceView belum di-assign.", this);
                return;
            }

            if (choiceEntries == null || choiceEntries.Count == 0)
            {
                Debug.LogWarning("[NarrativeHubManager] choiceEntries kosong.", this);
                return;
            }

            DialogueChoiceView.Setup(choiceEntries);
        }

        public UniTask<int> PlayDialogueChoiceAsync(
            List<DialogueChoiceEntry> choiceEntries,
            CancellationToken ct = default)
        {
            if (DialogueChoiceView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] DialogueChoiceView belum di-assign.", this);
                return UniTask.FromResult(-1);
            }

            if (choiceEntries == null || choiceEntries.Count == 0)
            {
                Debug.LogWarning("[NarrativeHubManager] choiceEntries kosong.", this);
                return UniTask.FromResult(-1);
            }

            return DialogueChoiceView.WaitForChoiceAsync(choiceEntries, ct);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayTimelineCutscene(TimelineCutscene cutscene, string timelineId = null)
        {
            if (TimelineCutsceneRunner == null)
            {
                Debug.LogWarning("[NarrativeHubManager] TimelineCutsceneRunner belum di-assign.", this);
                return;
            }

            if (cutscene == null)
            {
                Debug.LogWarning("[NarrativeHubManager] TimelineCutscene null.", this);
                return;
            }

            timelineId = cutscene.ResolveTimelineId(timelineId);
            if (string.IsNullOrEmpty(timelineId))
            {
                Debug.LogWarning(
                    "[NarrativeHubManager] timelineId kosong dan tidak ada timeline entry pertama yang valid.",
                    cutscene);
                return;
            }

            TimelineCutsceneRunner.PlayCutscene(cutscene, timelineId);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayProgrammableCutscene(ProgrammableCutsceneSO cutscene)
        {
            PlayProgrammableCutsceneAsync(cutscene).Forget();
        }

        public async UniTask PlayProgrammableCutsceneAsync(
            ProgrammableCutsceneSO cutscene,
            CancellationToken ct = default)
        {
            if (cutscene == null)
            {
                Debug.LogWarning("[NarrativeHubManager] cutscene is null.");
                return;
            }

            OnProgrammableCutsceneStart?.Invoke(cutscene);
            onProgrammableCutsceneStart?.Invoke(cutscene);

            try
            {
                await ProgrammableCutsceneRunner.RunAsync(cutscene, ct);
            }
            finally
            {
                OnProgrammableCutsceneEnd?.Invoke(cutscene);
                onProgrammableCutsceneEnd?.Invoke(cutscene);
            }
        }

        void BindViewEvents(bool bind)
        {
            if (FullscreenDialogueView != null)
            {
                if (bind)
                {
                    FullscreenDialogueView.OnDialogueStart += HandleFullscreenDialogueStart;
                    FullscreenDialogueView.OnDialogueEnd += HandleFullscreenDialogueEnd;
                }
                else
                {
                    FullscreenDialogueView.OnDialogueStart -= HandleFullscreenDialogueStart;
                    FullscreenDialogueView.OnDialogueEnd -= HandleFullscreenDialogueEnd;
                }
            }

            if (BanterDialogueView != null)
            {
                if (bind)
                {
                    BanterDialogueView.OnDialogueStart += HandleBanterDialogueStart;
                    BanterDialogueView.OnDialogueEnd += HandleBanterDialogueEnd;
                }
                else
                {
                    BanterDialogueView.OnDialogueStart -= HandleBanterDialogueStart;
                    BanterDialogueView.OnDialogueEnd -= HandleBanterDialogueEnd;
                }
            }

            if (DialogueChoiceView != null)
            {
                if (bind)
                    DialogueChoiceView.OnChoiceSelected += HandleDialogueChoiceSelected;
                else
                    DialogueChoiceView.OnChoiceSelected -= HandleDialogueChoiceSelected;
            }
        }

        void HandleFullscreenDialogueStart(FullscreenDialogueDataSO data, string collectionId)
        {
            OnFullscreenDialogueStart?.Invoke(data, collectionId);
            onFullscreenDialogueStart?.Invoke(data, collectionId);
        }

        void HandleFullscreenDialogueEnd(FullscreenDialogueDataSO data, string collectionId)
        {
            OnFullscreenDialogueEnd?.Invoke(data, collectionId);
            onFullscreenDialogueEnd?.Invoke(data, collectionId);
        }

        void HandleBanterDialogueStart(BanterDialogueDataSO data)
        {
            OnBanterDialogueStart?.Invoke(data);
            onBanterDialogueStart?.Invoke(data);
        }

        void HandleBanterDialogueEnd(BanterDialogueDataSO data)
        {
            OnBanterDialogueEnd?.Invoke(data);
            onBanterDialogueEnd?.Invoke(data);
        }

        void HandleDialogueChoiceSelected(int index)
        {
            OnDialogueChoiceSelected?.Invoke(index);
            onDialogueChoiceSelected?.Invoke(index);
        }

        /// <summary>
        /// Dipanggil TimelineCutscene (termasuk instance dinamis).
        /// </summary>
        public void NotifyTimelineCutsceneStart(TimelineCutscene cutscene)
        {
            OnTimelineCutsceneStart?.Invoke(cutscene);
            onTimelineCutsceneStart?.Invoke(cutscene);
            onTimelineCutsceneStartName?.Invoke(cutscene != null ? cutscene.gameObject.name : null);
        }

        public void NotifyTimelineCutsceneEnd(TimelineCutscene cutscene)
        {
            OnTimelineCutsceneEnd?.Invoke(cutscene);
            onTimelineCutsceneEnd?.Invoke(cutscene);
            onTimelineCutsceneEndName?.Invoke(cutscene != null ? cutscene.gameObject.name : null);
        }

        public void Process_NarrativeAction(ISequenceable action)
        {
            Process_NarrativeActionAsync(action).Forget();
        }

        public void Process_NarrativeActions(IReadOnlyList<ISequenceable> actions)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
                Process_NarrativeAction(action);
        }

        [Obsolete("Use overload with IReadOnlyList<ISequenceable> instead.")]
        public void Process_NarrativeActions(List<INarrativeAction> actions)
        {
            Process_NarrativeActions((IReadOnlyList<ISequenceable>)actions);
        }

        public async UniTask Process_NarrativeActionAsync(
            ISequenceable action,
            CancellationToken ct = default)
        {
            if (action == null)
                return;

            await action.ExecuteAsync(ct);
        }

        public async UniTask Process_NarrativeActionsAsync(
            IReadOnlyList<ISequenceable> actions,
            CancellationToken ct = default)
        {
            await SequenceRunner.RunAsync(actions, ct);
        }

        [Obsolete("Use overload with IReadOnlyList<ISequenceable> instead.")]
        public UniTask Process_NarrativeActionsAsync(
            List<INarrativeAction> actions,
            CancellationToken ct = default)
        {
            return Process_NarrativeActionsAsync((IReadOnlyList<ISequenceable>)actions, ct);
        }
    }
}
