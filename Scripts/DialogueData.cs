using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using RAXY.Core.Addressable;
using RAXY.Utility.Localization;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace RAXY.Narrative
{
    public interface IDialogueCollection
    {
        public string DialogueCollectionId { get; }
        public List<DialogueLine> DialogueLines { get; }
    }

    public interface IDialogueCollectionWithSpeaker
    {
        public string DialogueCollectionId { get; }
        public List<DialogueLineWithSpeaker> DialogueLineWithSpeakers { get; }
    }

    public interface IDialogueCollectionWithPortrait
    {
        public string DialogueCollectionId { get; }
        public List<DialogueLineWithPortrait> DialogueLineWithPortraits { get; }
    }

    public interface IDialogueCollectionWithBanter
    {
        public string DialogueCollectionId { get; }
        public List<DialogueLineWithBanter> DialogueLineWithBanters { get; }
    }

    [Serializable]
    public class DialogueCollection : IDialogueCollection
    {
        [SerializeField]
        string dialogueCollectionId;

        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label", OnTitleBarGUI = "DrawRefreshButton", Expanded = true)]
        List<DialogueLine> dialogueLines;

        public string DialogueCollectionId => dialogueCollectionId;
        public List<DialogueLine> DialogueLines => dialogueLines;

#if UNITY_EDITOR
        private void DrawRefreshButton()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                foreach (var line in dialogueLines)
                {
                    line.lineProvider.RefreshCacheAsync().Forget();
                }
            }
        }
#endif
    }

    [Serializable]
    public class DialogueCollectionWithSpeaker : IDialogueCollection, IDialogueCollectionWithSpeaker
    {
        [SerializeField]
        string dialogueCollectionId;

        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label", OnTitleBarGUI = "DrawRefreshButton", Expanded = true)]
        List<DialogueLineWithSpeaker> dialogueLines;

        public string DialogueCollectionId => dialogueCollectionId;
        public List<DialogueLine> DialogueLines
        {
            get
            {
                var temp = new List<DialogueLine>();
                foreach (var line in dialogueLines)
                {
                    temp.Add(line);
                }

                return temp;
            }
        }
        public List<DialogueLineWithSpeaker> DialogueLineWithSpeakers => dialogueLines;

#if UNITY_EDITOR
        private void DrawRefreshButton()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                foreach (var line in dialogueLines)
                {
                    line.lineProvider.RefreshCacheAsync().Forget();
                }
            }
        }
#endif
    }

    [Serializable]
    public class DialogueCollectionWithPortrait : IDialogueCollection, IDialogueCollectionWithSpeaker, IDialogueCollectionWithPortrait
    {
        [SerializeField]
        string dialogueCollectionId;

        [TitleGroup("On Start - Portrait Setting")]
        [HideLabel]
        [FormerlySerializedAs("initialPortraitSetting")]
        public PortraitStateSetterGroup portraitSetting_OnStart = new();

        [TitleGroup("On Start - Narrative Action")]
        [ListDrawerSettings(Expanded = true, ListElementLabelName = "action")]
        public List<NarrativeAction> narrativeActions_OnStart;

        [TitleGroup("Dialogue Lines")]
        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label", OnTitleBarGUI = "DrawRefreshButton", Expanded = true)]
        [OnCollectionChanged(After = nameof(OnDialogueLinesChanged))]
        List<DialogueLineWithPortrait> dialogueLines;

        [TitleGroup("On Complete - Narrative Action")]
        [OnCollectionChanged(After = nameof(OnNarrativeActionsOnCompleteChanged))]
        [ListDrawerSettings(Expanded = true, ListElementLabelName = "action")]
        public List<NarrativeAction> narrativeActions_OnComplete;

        public string DialogueCollectionId => dialogueCollectionId;
        public List<DialogueLine> DialogueLines
        {
            get
            {
                var temp = new List<DialogueLine>();
                foreach (var line in dialogueLines)
                {
                    temp.Add(line);
                }

                return temp;
            }
        }
        public List<DialogueLineWithSpeaker> DialogueLineWithSpeakers
        {
            get
            {
                var temp = new List<DialogueLineWithSpeaker>();
                foreach (var line in dialogueLines)
                {
                    temp.Add(line);
                }

                return temp;
            }
        }
        public List<DialogueLineWithPortrait> DialogueLineWithPortraits => dialogueLines;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        List<DialogueActorSO> cachedActors;

        [SerializeField]
        [HideInInspector]
        FullscreenDialogueDataSO cachedParentSO;

        private void DrawRefreshButton()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                foreach (var line in dialogueLines)
                {
                    line.lineProvider.RefreshCacheAsync().Forget();
                }
            }
        }

        public void SetupEditor(FullscreenDialogueDataSO parentSO, List<DialogueActorSO> actors)
        {
            cachedParentSO = parentSO;
            cachedActors = actors;
            portraitSetting_OnStart.SetupActors(actors, true);
            NarrativeAction.BindPlayDialogueToParent(narrativeActions_OnStart, parentSO);
            NarrativeAction.BindPlayDialogueToParent(narrativeActions_OnComplete, parentSO);

            if (dialogueLines != null)
            {
                foreach (var line in dialogueLines)
                    line.SetupEditor(parentSO, actors);
            }
        }

        void OnDialogueLinesChanged(CollectionChangeInfo info)
        {
            if (info.ChangeType != CollectionChangeType.Add && info.ChangeType != CollectionChangeType.Insert)
                return;

            if (cachedActors == null || dialogueLines == null)
                return;

            foreach (var line in dialogueLines)
                line.SetupEditor(cachedParentSO, cachedActors);
        }

        void OnNarrativeActionsOnCompleteChanged(CollectionChangeInfo info)
        {
            if (info.ChangeType != CollectionChangeType.Add && info.ChangeType != CollectionChangeType.Insert)
                return;

            NarrativeAction.BindPlayDialogueToParent(narrativeActions_OnComplete, cachedParentSO);
        }
#endif
    }

    [Serializable]
    public class DialogueCollectionWithBanter : IDialogueCollection, IDialogueCollectionWithSpeaker, IDialogueCollectionWithBanter
    {
        [SerializeField]
        string dialogueCollectionId;
        public string DialogueCollectionId => dialogueCollectionId;

        //[TitleGroup("Dialogue Lines")]
        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Label", OnTitleBarGUI = "DrawRefreshButton", Expanded = true)]
        [OnCollectionChanged(After = nameof(OnDialogueLinesChanged))]
        List<DialogueLineWithBanter> dialogueLines;

        public List<DialogueLine> DialogueLines
        {
            get
            {
                var temp = new List<DialogueLine>();
                foreach (var line in dialogueLines)
                {
                    temp.Add(line);
                }

                return temp;
            }
        }
        public List<DialogueLineWithSpeaker> DialogueLineWithSpeakers
        {
            get
            {
                var temp = new List<DialogueLineWithSpeaker>();
                foreach (var line in dialogueLines)
                {
                    temp.Add(line);
                }

                return temp;
            }
        }
        public List<DialogueLineWithBanter> DialogueLineWithBanters => dialogueLines;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        List<DialogueActorSO> cachedActors;

        private void DrawRefreshButton()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                foreach (var line in dialogueLines)
                {
                    line.lineProvider.RefreshCacheAsync().Forget();
                }
            }
        }

        public void SetupActors(List<DialogueActorSO> actors)
        {
            cachedActors = actors;

            if (dialogueLines != null)
                foreach (var line in dialogueLines)
                    line.SetupEditor(actors);
        }

        void OnDialogueLinesChanged(CollectionChangeInfo info)
        {
            if (info.ChangeType != CollectionChangeType.Add && info.ChangeType != CollectionChangeType.Insert)
                return;

            if (cachedActors == null || dialogueLines == null)
                return;

            foreach (var line in dialogueLines)
                line.SetupEditor(cachedActors);
        }
#endif
    }

    [Serializable]
    public class DialogueLine
    {
        const int LabelMaxLength = 40;

        [TitleGroup("Dialogue Line")]
        [HideLabel]
        public StringProvider lineProvider;

        protected virtual string Label => TruncateForLabel(lineProvider?.String);

        protected static string TruncateForLabel(string text, int maxLength = LabelMaxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text.Substring(0, maxLength - 3) + "...";
        }
    }

    [Serializable]
    public class DialogueLineWithSpeaker : DialogueLine
    {
        [TitleGroup("Setting")]
        [SuffixLabel("seconds")]
        [ShowIf("@UseAutoNext")]
        public float autoNextDuration = 5;

        [SerializeField, HideInInspector]
        protected List<DialogueActorSO> actors;
        public DialogueActorSO ActorSO => actors?.Find(a => a != null && a.ActorId == speakerActorId);

        [TitleGroup("Speaker")]
        [SerializeField]
        protected bool useCustomSpeakerName;

        [TitleGroup("Speaker")]
        [ValueDropdown("ActorIds")]
        [SerializeField]
        [HideIf("@useCustomSpeakerName")]
        [FormerlySerializedAs("actorId")]
        protected string speakerActorId;

        [TitleGroup("Speaker")]
        [SerializeField]
        [ShowIf("@useCustomSpeakerName")]
        StringProvider customSpeakerNameProvider;

        public virtual bool UseAutoNext => true;

        protected override string Label
        {
            get
            {
                string speaker = "";

                if (useCustomSpeakerName)
                    speaker = customSpeakerNameProvider.String;
                else
                    speaker = string.IsNullOrEmpty(speakerActorId) ? string.Empty : speakerActorId;

                return $"{speaker} > {TruncateForLabel(lineProvider?.String)}";
            }
        }

        public string SpeakerName
        {
            get
            {
                if (useCustomSpeakerName)
                    return customSpeakerNameProvider.String;
                else
                {
                    var actor = ActorSO;
                    return actor == null ? string.Empty : actor.actorNameProvider.String;
                }
            }
        }

#if UNITY_EDITOR
        IEnumerable<string> ActorIds => actors == null ? null : actors.Where(a => a != null).Select(a => a.ActorId);

        public void SetupEditor(List<DialogueActorSO> actors)
        {
            this.actors = actors;

            if (!string.IsNullOrEmpty(speakerActorId) && (actors == null || !actors.Exists(a => a != null && a.ActorId == speakerActorId)))
                speakerActorId = null;
        }
#endif
    }

    [Serializable]
    public class DialogueLineWithPortrait : DialogueLineWithSpeaker
    {
        [TitleGroup("On Enter - Portrait Setting")]
        [HideLabel]
        [FormerlySerializedAs("portraitStates")]
        [PropertyOrder(-1)]
        public PortraitStateSetterGroup portraitSetting_OnEnter = new();

        [TitleGroup("On Enter - Narrative Action")]
        [PropertyOrder(-1)]
        [ListDrawerSettings(Expanded = true, ListElementLabelName = "action")]
        public List<NarrativeAction> narrativeActions_OnEnter;

        [TitleGroup("Setting")]
        [SerializeField]
        [PropertyOrder(-1)]
        bool useAutoNext = true;

        [TitleGroup("Setting")]
        [SuffixLabel("seconds")]
        public float blockNextDuration = 0.3f;

        public override bool UseAutoNext => useAutoNext;

#if UNITY_EDITOR
        public void SetupEditor(FullscreenDialogueDataSO parentSO, List<DialogueActorSO> actors)
        {
            SetupEditor(actors);
            portraitSetting_OnEnter.SetupActors(actors);
            NarrativeAction.BindPlayDialogueToParent(narrativeActions_OnEnter, parentSO);
        }
#endif
    }

    [Serializable]
    public class DialogueLineWithBanter : DialogueLineWithSpeaker
    {
        [TitleGroup("Banter Portrait")]
        public bool useCustomBanterPortrait;

        [TitleGroup("Banter Portrait")]
        [ShowIf("@useCustomBanterPortrait")]
        public AddressableAssetProviderSprite banterPortraitProvider;

        [TitleGroup("Banter Portrait")]
        [ShowIf("@ShowSameAsSpeakerField")]
        public bool sameAsSpeaker;

        [TitleGroup("Banter Portrait")]
        [ShowIf("@ShowBanterActorIdField")]
        [ValueDropdown("ActorIds")]
        [SerializeField]
        string banterActorId;

        [TitleGroup("Banter Portrait")]
        [ShowIf("@ShowBanterPortraitIdField")]
        [ValueDropdown("BanterPortraitIds")]
        public string banterPortraitId;

        public string BanterActorId
        {
            get
            {
                if (useCustomSpeakerName)
                {
                    return banterActorId;
                }
                else
                {
                    if (sameAsSpeaker)
                        return speakerActorId;
                    else
                        return banterActorId;
                }
            }
        }

#if UNITY_EDITOR
        bool ShowSameAsSpeakerField
        {
            get
            {
                if (useCustomBanterPortrait)
                    return false;

                return !useCustomSpeakerName;
            }
        }

        bool ShowBanterPortraitIdField
        {
            get
            {
                if (useCustomBanterPortrait)
                    return false;

                return true;
            }
        }

        bool ShowBanterActorIdField
        {
            get
            {
                if (useCustomBanterPortrait)
                    return false;

                if (useCustomSpeakerName)
                {
                    return true;
                }
                else
                {
                    return !sameAsSpeaker;
                }
            }
        }

        IEnumerable<string> BanterPortraitIds
        {
            get
            {
                var actorSO = actors.Find(x => x.ActorId == BanterActorId);
                if (actorSO == null)
                    return null;

                return actorSO.banterPortraits.Select(x => x.portraitId);
            }
        }
#endif
    }

    [Serializable]
    public class ActorPortraitStateSetter
    {
#if UNITY_EDITOR
        [NonSerialized, HideInInspector]
        public bool editorVisible = true;
#endif

        [PropertyOrder(-1)]
        [ShowInInspector]
        [ReadOnly]
        [TableColumnWidth(75, false)]
#if UNITY_EDITOR
        [ShowIf("@editorVisible")]
#endif
        public string ActorId => actorSO?.ActorId;

        [SerializeField]
        [HideInInspector]
        DialogueActorSO actorSO;

        [TableColumnWidth(30, false)]
#if UNITY_EDITOR
        [ShowIf("@editorVisible")]
#endif
        public bool set;

#if UNITY_EDITOR
        [ShowIf("@set && editorVisible")]
#else
    [ShowIf("@set")]
#endif
        public PortraitStateSetter portraitStateSetter;

#if UNITY_EDITOR
        public void SetupEditor(DialogueActorSO actorSO, bool hideUseTween = false)
        {
            this.actorSO = actorSO;

            portraitStateSetter ??= new PortraitStateSetter();
            portraitStateSetter.SetupEditor(actorSO, hideUseTween);
        }
#endif
    }

    [Serializable]
    public class PortraitStateSetterGroup
    {
        [TableList(AlwaysExpanded = true, IsReadOnly = true, HideToolbar = true, DrawScrollView = false)]
        public List<ActorPortraitStateSetter> states = new();

#if UNITY_EDITOR
        [NonSerialized]
        HashSet<string> visibleActorIds;

        [NonSerialized]
        HashSet<string> knownActorIds;

        public bool IsActorVisible(string actorId)
        {
            SyncVisibleActorIds();
            return !string.IsNullOrEmpty(actorId) && visibleActorIds.Contains(actorId);
        }

        public void SyncVisibleActorIds()
        {
            visibleActorIds ??= new HashSet<string>();
            knownActorIds ??= new HashSet<string>();

            var currentIds = new HashSet<string>();
            if (states != null)
            {
                foreach (var setter in states)
                {
                    if (setter == null || string.IsNullOrEmpty(setter.ActorId))
                        continue;

                    currentIds.Add(setter.ActorId);
                    if (knownActorIds.Add(setter.ActorId))
                        visibleActorIds.Add(setter.ActorId);

                    setter.editorVisible = visibleActorIds.Contains(setter.ActorId);
                }
            }

            visibleActorIds.RemoveWhere(id => !currentIds.Contains(id));
            knownActorIds.RemoveWhere(id => !currentIds.Contains(id));
        }

        public bool SetActorVisible(string actorId, bool visible)
        {
            SyncVisibleActorIds();
            if (string.IsNullOrEmpty(actorId))
                return false;

            bool changed = visible ? visibleActorIds.Add(actorId) : visibleActorIds.Remove(actorId);
            if (states != null)
            {
                foreach (var setter in states)
                {
                    if (setter == null || setter.ActorId != actorId)
                        continue;
                    setter.editorVisible = visible;
                }
            }

            return changed;
        }

        [FoldoutGroup("Set Highlight")]
        [SerializeField]
        [ValueDropdown("ActorIds")]
        [LabelText("Actor Id")]
        string helper_HighlightActorId;

        IEnumerable<string> ActorIds => states?.Where(x => x != null).Select(x => x.ActorId);

        [HorizontalGroup("Set Highlight/Btn", 0.2f)]
        [Button("Edit Preset")]
        void EditHighlightPreset() => PortraitHighlightPreset.OpenWindow();

        [HorizontalGroup("Set Highlight/Btn")]
        [Button]
        void SetHighlight()
        {
            Color highlightColor = PortraitHighlightPreset.HighlightColor;
            Color noHighlightColor = PortraitHighlightPreset.NoHighlightColor;

            foreach (var setter in states)
            {
                setter.set = true;

                foreach (var part in setter.portraitStateSetter.parts)
                {
                    part.colorSetter.setColor = true;
                }

                if (setter.ActorId == helper_HighlightActorId)
                    setter.portraitStateSetter.SetAllPartColor(highlightColor);
                else
                    setter.portraitStateSetter.SetAllPartColor(noHighlightColor);
            }
        }

        public void SetupActors(List<DialogueActorSO> actors, bool hideUseTween = false)
        {
            states = DialogueUtility.Rebuild(states, actors, hideUseTween);
            SyncVisibleActorIds();
        }
#endif
    }

#if UNITY_EDITOR
    public static class PortraitHighlightPreset
    {
        const string HighlightKey = "RAXY_Portrait_HighlightColor";
        const string NoHighlightKey = "RAXY_Portrait_NoHighlightColor";

        public static readonly Color DefaultHighlight = Color.white;
        public static readonly Color DefaultNoHighlight = Color.grey;

        public static Action OpenWindowHandler;

        public static Color HighlightColor
        {
            get => Get(HighlightKey, DefaultHighlight);
            set => Set(HighlightKey, value);
        }

        public static Color NoHighlightColor
        {
            get => Get(NoHighlightKey, DefaultNoHighlight);
            set => Set(NoHighlightKey, value);
        }

        public static void OpenWindow() => OpenWindowHandler?.Invoke();

        public static void ResetToDefault()
        {
            HighlightColor = DefaultHighlight;
            NoHighlightColor = DefaultNoHighlight;
        }

        static Color Get(string key, Color fallback)
            => ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(key, ""), out var c) ? c : fallback;

        static void Set(string key, Color c)
            => EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(c));
    }

    public static class DialogueUtility
    {
        public static List<ActorPortraitStateSetter> Rebuild(List<ActorPortraitStateSetter> old,
                                                                List<DialogueActorSO> actors,
                                                                bool hideUseTween = false)
        {
            var result = new List<ActorPortraitStateSetter>();
            if (actors == null)
                return result;

            foreach (var actor in actors)
            {
                if (actor == null)
                    continue;

                var existing = old?.Find(s => s.ActorId == actor.ActorId);
                var setter = existing ?? new ActorPortraitStateSetter();
                setter.SetupEditor(actor, hideUseTween);
                result.Add(setter);
            }

            return result;
        }
    }
#endif
}