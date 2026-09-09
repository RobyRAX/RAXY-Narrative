using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    public static class TimelineCutsceneCreateMenu
    {
        internal const string DefaultCutsceneName = "New Cutscene";
        const string MenuPath = "Assets/Create/RAXY/Narrative/Cutscene Timeline";
        const string DefaultFolderPath = "Assets";

        [MenuItem(MenuPath, false, 81)]
        public static void CreateCutsceneTimeline()
        {
            string targetFolder = ResolveTargetFolder();
            if (string.IsNullOrEmpty(targetFolder))
            {
                EditorUtility.DisplayDialog(
                    "Create Cutscene Timeline",
                    "Failed to resolve a target folder.",
                    "OK");
                return;
            }

            string cutsceneDisplayName = CutsceneNameDialog.Show(DefaultCutsceneName);
            if (string.IsNullOrWhiteSpace(cutsceneDisplayName))
                return;

            string baseName = ToAssetBaseName(cutsceneDisplayName);
            string prefabName = $"{baseName} Cutscene";
            string timelineName = $"{baseName} Timeline";
            string timelinePath = $"{targetFolder}/{timelineName}.playable";
            string prefabPath = $"{targetFolder}/{prefabName}.prefab";

            if (AssetExists(timelinePath) || AssetExists(prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "Create Cutscene Timeline",
                    "One or more target files already exist:\n\n" +
                    $"{timelinePath}\n{prefabPath}",
                    "OK");
                return;
            }

            var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timelineAsset, timelinePath);
            AssetDatabase.SaveAssets();
            timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);

            var go = new GameObject(prefabName);
            try
            {
                var director = go.AddComponent<PlayableDirector>();
                var cutscene = go.AddComponent<TimelineCutscene>();
                cutscene.PlayableDirector = director;
                director.playableAsset = timelineAsset;
                cutscene.EnsureDirectorTimelineRegistered();
                cutscene.ApplyEditorHelperPrefabs(
                    NarrativeEditorSettings.instance.DefaultEditorHelperPrefabs);

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var timelineReload = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            Selection.objects = new Object[] { prefabAsset, timelineReload };
            EditorGUIUtility.PingObject(prefabAsset);

            Debug.Log(
                "[TimelineCutsceneCreateMenu] Created cutscene assets.\n" +
                $"Prefab: {prefabPath}\n" +
                $"Timeline: {timelinePath}");
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateCreateCutsceneTimeline() => true;

        static string ResolveTargetFolder()
        {
            if (Selection.activeObject != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(selectedPath))
                    return selectedPath;
            }

            return DefaultFolderPath;
        }

        static bool AssetExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null;

        static string ToAssetBaseName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return DefaultCutsceneName;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string[] parts = displayName
                .Trim()
                .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            var cleanedParts = new List<string>();
            foreach (string part in parts)
            {
                var builder = new StringBuilder(part.Length);
                foreach (char c in part)
                {
                    if (!invalidChars.Contains(c))
                        builder.Append(c);
                }

                string cleaned = builder.ToString();
                if (!string.IsNullOrEmpty(cleaned))
                    cleanedParts.Add(cleaned);
            }

            return cleanedParts.Count == 0
                ? DefaultCutsceneName
                : string.Join(" ", cleanedParts);
        }
    }

    class CutsceneNameDialog : EditorWindow
    {
        string _cutsceneName = TimelineCutsceneCreateMenu.DefaultCutsceneName;
        bool _confirmed;

        public static string Show(string defaultName)
        {
            var window = CreateInstance<CutsceneNameDialog>();
            window._cutsceneName = defaultName;
            window._confirmed = false;
            window.titleContent = new GUIContent("Create Cutscene Timeline");
            window.minSize = new Vector2(360f, 96f);
            window.maxSize = new Vector2(360f, 96f);
            window.ShowModalUtility();
            return window._confirmed ? window._cutsceneName : null;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cutscene Name");
            _cutsceneName = EditorGUILayout.TextField(_cutsceneName);

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                Close();

            if (GUILayout.Button("Create", GUILayout.Width(80f)))
            {
                _confirmed = true;
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
