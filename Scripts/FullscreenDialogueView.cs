using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using RAXY.UI;
using RAXY.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RAXY.Narrative
{
    public class FullscreenDialogueView : MonoBehaviour
    {
        [TitleGroup("UI Ref")]
        [SerializeField]
        Transform dialogueBarRoot;

        [TitleGroup("UI Ref")]
        [SerializeField]
        Canvas canvas;

        [TitleGroup("UI Ref")]
        [SerializeField]
        Button nextBtn;

        [TitleGroup("UI Ref")]
        [SerializeField]
        Transform portraitContainer;

        [TitleGroup("UI Ref")]
        [SerializeField]
        TextMeshProUGUI speakerNameTmp;

        [TitleGroup("UI Ref")]
        [SerializeField]
        TextMeshProUGUI dialogueLineTmp;

        [TitleGroup("Settings")]
        [SerializeField]
        [Range(0.001f, 0.1f)]
        float charInterval = 0.03f;

        [TitleGroup("Settings")]
        [SerializeField]
        float positionTweenDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        float mirrorTweenDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        float colorTweenDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        float sizeTweenDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        float mainFadeDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        float dialogueBarFadeDuration = 0.2f;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        TextTyper _textTyper;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        CanvasGroup _mainCanvasGroup;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        CanvasGroup _dialogueBarCanvasGroup;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        public FullscreenDialogueDataSO CurrentDialogueSO { get; set; }

        [TitleGroup("Runtime")]
        [ShowInInspector]
        bool _advanceRequested;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        Dictionary<string, DialoguePortrait> _portraitByActorId;

        CancellationTokenSource _playCts;

        public event Action<FullscreenDialogueDataSO, string> OnDialogueStart;
        public event Action<FullscreenDialogueDataSO, string> OnDialogueEnd;

        void Awake()
        {
            _textTyper = dialogueLineTmp.GetOrAddComponent<TextTyper>();
            _textTyper.charInterval = charInterval;

            _mainCanvasGroup = canvas.GetOrAddComponent<CanvasGroup>();
            _mainCanvasGroup.alpha = 0;

            _dialogueBarCanvasGroup = dialogueBarRoot.GetOrAddComponent<CanvasGroup>();
            _dialogueBarCanvasGroup.alpha = 0;

            if (nextBtn != null)
                nextBtn.onClick.AddListener(Advance);
        }

        void OnDestroy()
        {
            if (nextBtn != null)
                nextBtn.onClick.RemoveListener(Advance);

            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void Play(FullscreenDialogueDataSO data, string collectionId = null)
        {
            if (data == null)
                return;

            if (collectionId == null && data.dialogueCollections != null && data.dialogueCollections.Count > 0)
                collectionId = data.dialogueCollections[0].DialogueCollectionId;

            PlayAsync(data, collectionId).Forget();
        }

        public async UniTask PlayAsync(FullscreenDialogueDataSO data, string collectionId, CancellationToken ct = default)
        {
            if (data == null)
                return;

            // Batalkan playthrough sebelumnya biar loop lama berhenti menyentuh state bersama
            // (_advanceRequested), yang bikin auto next/advance terasa "gk ke reset".
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _playCts.Token;

            _advanceRequested = false;
            CurrentDialogueSO = data;

            EnsurePortraits(data);

            var collection = data.dialogueCollections?.Find(c => c != null && c.DialogueCollectionId == collectionId);
            if (collection == null)
            {
                Debug.LogWarning($"[FullscreenDialogueView] Collection '{collectionId}' tidak ditemukan.");
                return;
            }

            bool started = false;
            try
            {
                started = true;
                OnDialogueStart?.Invoke(data, collectionId);

                ApplyPortraitStates(collection.portraitSetting_OnStart.states, forceInstant: true);

                await FadeMainCanvasGroupAsync(1f);
                ShowDialogueBar();

                await ProcessNarrativeActionsAsync(collection.narrativeActions_OnStart, token);

                var lines = collection.DialogueLineWithPortraits;
                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (line == null)
                            continue;

                        ApplyPortraitStates(line.portraitSetting_OnEnter.states);

                        await ProcessNarrativeActionsAsync(line.narrativeActions_OnEnter, token);

                        if (speakerNameTmp != null)
                            speakerNameTmp.text = line.SpeakerName;

                        string text = await GetLineTextAsync(line);

                        if (_textTyper != null)
                            _textTyper.StartTyping(text);
                        else if (dialogueLineTmp != null)
                            dialogueLineTmp.text = text;

                        await WaitLineAsync(line, token);
                    }
                }

                await ProcessNarrativeActionsAsync(collection.narrativeActions_OnComplete, token);

                // PlayDialogue di OnComplete sudah handoff ke playthrough baru (_playCts lama di-cancel).
                // Jangan EndDialogue lagi — itu milik play baru.
                if (token.IsCancellationRequested)
                    return;

                await EndDialogueAsync(token);
            }
            catch (System.OperationCanceledException)
            {
                // Handoff / external cancel — biarkan finally fire OnDialogueEnd untuk collection ini.
            }
            finally
            {
                if (started)
                    OnDialogueEnd?.Invoke(data, collectionId);
            }
        }

        async UniTask ProcessNarrativeActionsAsync(List<NarrativeAction> actions, CancellationToken ct)
        {
            if (actions == null || actions.Count == 0)
                return;

            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[FullscreenDialogueView] NarrativeHubManager.Instance null — narrative actions di-skip.", this);
                return;
            }

            await hub.Process_NarrativeActionsAsync(actions, ct);
        }

        public void Advance()
        {
            _advanceRequested = true;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void EndDialogue()
        {
            EndDialogueAsync().Forget();
        }

        public async UniTask EndDialogueAsync(CancellationToken ct = default)
        {
            HideDialogueBar();
            await FadeMainCanvasGroupAsync(0f);
            Hide();
        }

        public async UniTask FadeMainCanvasGroupAsync(float targetAlpha)
        {
            if (_mainCanvasGroup == null)
                return;

            DOTween.Kill(_mainCanvasGroup);

            if (mainFadeDuration <= 0f)
            {
                _mainCanvasGroup.alpha = targetAlpha;
                return;
            }

            var tween = FadeCanvasGroup(_mainCanvasGroup, targetAlpha, mainFadeDuration);
            await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        public void ShowDialogueBar()
        {
            FadeDialogueBar(1f);
        }

        public void HideDialogueBar()
        {
            FadeDialogueBar(0f);
        }

        void FadeDialogueBar(float targetAlpha)
        {
            if (_dialogueBarCanvasGroup == null)
                return;

            DOTween.Kill(_dialogueBarCanvasGroup);

            if (dialogueBarFadeDuration <= 0f)
                _dialogueBarCanvasGroup.alpha = targetAlpha;
            else
                FadeCanvasGroup(_dialogueBarCanvasGroup, targetAlpha, dialogueBarFadeDuration);
        }

        static Tween FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
        {
            return DOTween.To(() => cg.alpha, x => cg.alpha = x, targetAlpha, duration)
                          .SetTarget(cg);
        }

        void EnsurePortraits(FullscreenDialogueDataSO data)
        {
            if (CurrentDialogueSO == data && _portraitByActorId != null && _portraitByActorId.Count > 0)
                return;

            foreach (Transform child in portraitContainer)
            {
                Destroy(child.gameObject);
            }

            if (_portraitByActorId != null)
            {
                foreach (var portrait in _portraitByActorId.Values)
                    if (portrait != null)
                        Destroy(portrait.gameObject);
            }

            _portraitByActorId = new Dictionary<string, DialoguePortrait>();

            if (portraitContainer == null || data.actors == null)
                return;

            foreach (var actor in data.actors)
            {
                if (actor == null || actor.portraitPrefabProvider.Asset == null)
                    continue;

                var portrait = Instantiate(actor.portraitPrefabProvider.Asset, portraitContainer).GetComponent<DialoguePortrait>();
                if (portrait == null)
                    continue;

                portrait.actorSO = actor;
                portrait.ConfigureTweenDurations(positionTweenDuration, mirrorTweenDuration, colorTweenDuration, sizeTweenDuration);

                var id = actor.ActorId;
                if (!string.IsNullOrEmpty(id))
                    _portraitByActorId[id] = portrait;
            }
        }

        void ApplyPortraitStates(List<ActorPortraitStateSetter> states, bool forceInstant = false)
        {
            if (states == null || _portraitByActorId == null)
                return;

            foreach (var state in states)
            {
                if (state == null || !state.set)
                    continue;

                var id = state.ActorId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (_portraitByActorId.TryGetValue(id, out var portrait) && portrait != null)
                    portrait.ProcessPortraitStateSetter(state.portraitStateSetter, forceInstant);
            }
        }

        async UniTask<string> GetLineTextAsync(DialogueLine line)
        {
            if (line?.lineProvider == null)
                return string.Empty;

            return await line.lineProvider.GetStringAsync();
        }

        async UniTask WaitLineAsync(DialogueLineWithPortrait line, CancellationToken ct)
        {
            _advanceRequested = false;

            // Block window: input diblok penuh selama blockNextDuration.
            if (nextBtn != null)
                nextBtn.interactable = false;

            if (line.blockNextDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(line.blockNextDuration), cancellationToken: ct);

            _advanceRequested = false;

            // Fase advance: klik pertama saat masih mengetik -> skip, klik berikutnya -> lanjut.
            if (nextBtn != null)
                nextBtn.interactable = true;

            float autoTimer = 0f;
            while (true)
            {
                if (_advanceRequested)
                {
                    _advanceRequested = false;

                    if (_textTyper != null && _textTyper.IsTyping)
                    {
                        // Klik pertama saat masih mengetik: skip typing, lalu mulai ulang hitungan auto next.
                        _textTyper.ShowAllInstant();
                        autoTimer = 0f;
                    }
                    else
                    {
                        break;
                    }
                }

                bool doneTyping = _textTyper == null || !_textTyper.IsTyping;
                if (doneTyping)
                {
                    if (line.UseAutoNext)
                    {
                        autoTimer += Time.deltaTime;
                        if (autoTimer >= line.autoNextDuration)
                            break;
                    }
                }
                else
                {
                    // Selama masih mengetik, tahan hitungan di 0 supaya auto next baru mulai
                    // setelah teks benar-benar selesai (tahan terhadap flicker IsTyping).
                    autoTimer = 0f;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _advanceRequested = false;

            if (nextBtn != null)
                nextBtn.interactable = false;
        }

        void Hide()
        {
            if (nextBtn != null)
                nextBtn.interactable = false;

            if (_textTyper == null && dialogueLineTmp != null)
                dialogueLineTmp.text = string.Empty;
        }
    }
}
