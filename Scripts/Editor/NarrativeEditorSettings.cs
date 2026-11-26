using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    /// <summary>
    /// Project-scoped Narrative editor defaults (Project Hub).
    /// Saved under ProjectSettings/RaxyNarrativeEditorSettings.asset.
    /// </summary>
    [FilePath("ProjectSettings/RaxyNarrativeEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NarrativeEditorSettings : ScriptableSingleton<NarrativeEditorSettings>
    {
        public const string DefaultHeroBubbleId = "Hero";

        [SerializeField]
        List<GameObject> defaultEditorHelperPrefabs = new();

        [SerializeField]
        List<string> barkBubbleIds = new() { DefaultHeroBubbleId };

        public IReadOnlyList<GameObject> DefaultEditorHelperPrefabs => defaultEditorHelperPrefabs;
        public IReadOnlyList<string> BarkBubbleIds => barkBubbleIds;

        public void EnsureDefaultBarkBubbleIds()
        {
            barkBubbleIds ??= new List<string>();
            if (barkBubbleIds.Count == 0)
            {
                barkBubbleIds.Add(DefaultHeroBubbleId);
                SaveDefaults();
            }
        }

        public bool AddBarkBubbleId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var trimmed = id.Trim();
            barkBubbleIds ??= new List<string>();
            foreach (var existing in barkBubbleIds)
            {
                if (string.Equals(existing, trimmed, StringComparison.Ordinal))
                    return false;
            }

            barkBubbleIds.Add(trimmed);
            SaveDefaults();
            return true;
        }

        public bool RemoveBarkBubbleIdAt(int index)
        {
            if (barkBubbleIds == null || index < 0 || index >= barkBubbleIds.Count)
                return false;

            barkBubbleIds.RemoveAt(index);
            SaveDefaults();
            return true;
        }

        public bool SetBarkBubbleIdAt(int index, string id)
        {
            if (barkBubbleIds == null || index < 0 || index >= barkBubbleIds.Count || string.IsNullOrWhiteSpace(id))
                return false;

            var trimmed = id.Trim();
            for (int i = 0; i < barkBubbleIds.Count; i++)
            {
                if (i != index && string.Equals(barkBubbleIds[i], trimmed, StringComparison.Ordinal))
                    return false;
            }

            if (string.Equals(barkBubbleIds[index], trimmed, StringComparison.Ordinal))
                return false;

            barkBubbleIds[index] = trimmed;
            SaveDefaults();
            return true;
        }

        public void SaveDefaults() => Save(true);
    }
}
