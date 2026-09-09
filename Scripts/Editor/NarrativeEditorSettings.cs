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
        [SerializeField]
        List<GameObject> defaultEditorHelperPrefabs = new();

        public IReadOnlyList<GameObject> DefaultEditorHelperPrefabs => defaultEditorHelperPrefabs;

        public void SaveDefaults() => Save(true);
    }
}
