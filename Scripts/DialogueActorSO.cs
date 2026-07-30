using System;
using System.Collections.Generic;
using System.Linq;
using RAXY.Core.Addressable;
using RAXY.Utility.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [CreateAssetMenu(fileName = "DialogueActorSO", menuName = "RAXY/Narrative/Dialogue Actor")]
    public class DialogueActorSO : ScriptableObject
    {
        [HideIf("@useSoNameAsId")]
        [SerializeField]
        string actorId;

        [SerializeField]
        bool useSoNameAsId;

        [ShowIf("@useSoNameAsId")]
        [ShowInInspector]
        [PropertyOrder(-1)]
        [DisplayAsString]
        public string ActorId
        {
            get
            {
                if (useSoNameAsId)
                    return name;
                else
                    return actorId;
            }
        }

        public StringProvider actorNameProvider;
        public AddressableAssetProviderGameObject portraitPrefabProvider;

        [TitleGroup("Fullscreen Portrait")]
        [TableList(AlwaysExpanded = true)]
        public List<PortraitPartEntry> fullScreenPortraitParts = new List<PortraitPartEntry>() { new PortraitPartEntry() { partId = "base" } };

        [TitleGroup("Fullscreen Portrait")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        [OnValueChanged(nameof(NotifyPortraitSizesChanged), IncludeChildren = true)]
        public List<PortraitSizeEntry> fullScreenPortraitSizes = new List<PortraitSizeEntry>() { new PortraitSizeEntry() };

        [TitleGroup("Banter Portrait")]
        [ListDrawerSettings(Expanded = true, ListElementLabelName = "portraitId", ShowIndexLabels = true)]
        public List<PortraitEntry> banterPortraits;

#if UNITY_EDITOR
        void OnValidate()
        {
            NotifyPortraitSizesChanged();
        }

        void NotifyPortraitSizesChanged()
        {
            var portraits = UnityEngine.Object.FindObjectsByType<DialoguePortrait>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var portrait in portraits)
            {
                if (portrait != null && portrait.actorSO == this)
                    portrait.SyncPortraitSizesWithSO();
            }
        }
#endif
    }

    [Serializable]
    public class PortraitPartEntry
    {
        [TableColumnWidth(100, false)]
        public string partId;

        [ListDrawerSettings(Expanded = true, ListElementLabelName = "portraitId", ShowIndexLabels = true)]
        public List<PortraitEntry> portraitEntries;

        public List<string> AllPortraitIds => portraitEntries.Select(x => x.portraitId).ToList();
    }

    [Serializable]
    public class PortraitSizeEntry
    {
        public float yPos = 0;
        public float portraitScale = 1;
    }

    [Serializable]
    public class PortraitEntry
    {
        public string portraitId;

        [TitleGroup("Sprite")]
        [HorizontalGroup("Sprite/Row")]
        [HideLabel]
        public AddressableAssetProviderSprite spriteProvider;

#if UNITY_EDITOR
        [HorizontalGroup("Sprite/Row", Width = 0.25f)]
        [Button(ButtonHeight = 42)]
        void SetAsId()
        {
            if (spriteProvider.Asset == null)
                return;

            portraitId = spriteProvider.Asset.name;
        }
#endif
    }
}
