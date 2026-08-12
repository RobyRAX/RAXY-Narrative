using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Event;
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

        [TitleGroup("Test")]
        [SerializeField]
        NarrativeAction test_Action;

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
        public void PlayTimelineCutscene(TimelineCutscene cutscene, string timelineId)
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

            TimelineCutsceneRunner.PlayCutscene(cutscene, timelineId);
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

        public void Process_NarrativeAction(NarrativeAction action)
        {
            Process_NarrativeActionAsync(action).Forget();
        }

        public void Process_NarrativeActions(List<NarrativeAction> actions)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
                Process_NarrativeAction(action);
        }

        public async UniTask Process_NarrativeActionAsync(
            NarrativeAction action,
            CancellationToken ct = default)
        {
            if (action == null)
                return;

            switch (action.action)
            {
                case NarrativeActionType.PlayDialogue:
                    await Process_PlayDialogueActionAsync(action.playDialogueParameter, ct);
                    break;
                case NarrativeActionType.EndDialogue:
                    FullscreenDialogueView.EndDialogue();
                    break;
                case NarrativeActionType.ToggleDialogueBar:
                    Process_ToggleDialogueBarAction(action.toggleDialogueBarParameter);
                    break;
                case NarrativeActionType.TriggerDialogueChoice:
                    await Process_TriggerDialogueChoiceActionAsync(action.triggerDialogueChoiceParameter, ct);
                    break;
                case NarrativeActionType.TriggerEventSO:
                    Process_TriggerEventSoAction(action.triggerEventSOParamater);
                    break;
                case NarrativeActionType.PlayTimelineCutscene:
                    Process_PlayTimelineCutsceneAction(action.playTimelineCutsceneParameter);
                    break;
            }
        }

        public async UniTask Process_NarrativeActionsAsync(
            List<NarrativeAction> actions,
            CancellationToken ct = default)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
            {
                ct.ThrowIfCancellationRequested();
                await Process_NarrativeActionAsync(action, ct);
            }
        }

        async UniTask Process_PlayDialogueActionAsync(PlayDialogueParameter param, CancellationToken ct)
        {
            if (param == null)
                return;

            // Jangan teruskan token playthrough lama.
            // Nested PlayAsync akan cancel _playCts outer; kalau token itu di-link ke play baru,
            // play baru langsung cancelled dan stuck.
            await PlayFullscreenDialogueAsync(param.dialogueSO, param.collectionId);
        }

        async UniTask Process_TriggerDialogueChoiceActionAsync(
            TriggerDialogueChoiceParameter param,
            CancellationToken ct)
        {
            if (param == null)
                return;

            if (DialogueChoiceView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] DialogueChoiceView belum di-assign — TriggerDialogueChoice di-skip.", this);
                return;
            }

            var entries = param.choiceEntries;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning("[NarrativeHubManager] TriggerDialogueChoice choiceEntries kosong.", this);
                return;
            }

            int selectedIndex = await PlayDialogueChoiceAsync(entries, ct);
            if (selectedIndex < 0 || selectedIndex >= entries.Count)
                return;

            var choice = entries[selectedIndex];
            if (choice?.narrativeActions == null || choice.narrativeActions.Count == 0)
            {
                Debug.LogWarning(
                    $"[NarrativeHubManager] Choice index {selectedIndex} tidak punya narrativeActions.",
                    this);
                return;
            }

            // Jangan ikat ke token dialogue outer — nested PlayTimelineCutscene bisa
            // memicu PlayFullscreenDialogue baru yang cancel _playCts lama.
            await Process_NarrativeActionsAsync(choice.narrativeActions, CancellationToken.None);
        }

        void Process_ToggleDialogueBarAction(ToggleDialogueBarParameter param)
        {
            if (param == null)
                return;

            if (FullscreenDialogueView == null)
            {
                Debug.LogWarning("[NarrativeHubManager] FullscreenDialogueView belum di-assign.", this);
                return;
            }

            if (param.setActive)
                FullscreenDialogueView.ShowDialogueBar();
            else
                FullscreenDialogueView.HideDialogueBar();
        }

        void Process_TriggerEventSoAction(EventSoRaiser raiser)
        {
            if (raiser == null)
                return;

            raiser.Raise();
        }

        void Process_PlayTimelineCutsceneAction(PlayTimelineCutsceneParameter param)
        {
            if (param == null)
            {
                Debug.LogWarning("[NarrativeHubManager] PlayTimelineCutscene parameter null.", this);
                return;
            }

            var cutscene = param.ResolveCutscene();
            if (cutscene == null)
            {
                Debug.LogWarning(
                    $"[NarrativeHubManager] PlayTimelineCutscene cutscene null (name='{param.CutsceneName}').",
                    this);
                return;
            }

            if (string.IsNullOrEmpty(param.timelineId))
            {
                Debug.LogWarning("[NarrativeHubManager] PlayTimelineCutscene timelineId kosong.", this);
                return;
            }

            PlayTimelineCutscene(cutscene, param.timelineId);
        }
    }
}
