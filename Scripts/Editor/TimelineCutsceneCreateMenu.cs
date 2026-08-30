using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    public static class TimelineCutsceneCreateMenu
    {
        const string DefaultObjectName = "Cutscene Timeline";
        const string MenuPath = "GameObject/RAXY/Narrative/Cutscene Timeline";

        [MenuItem(MenuPath, false, 10)]
        static void CreateCutsceneTimeline(MenuCommand menuCommand)
        {
            var go = new GameObject(DefaultObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create Cutscene Timeline");

            go.transform.SetParent(null);
            go.transform.position = Vector3.zero;

            var director = go.AddComponent<PlayableDirector>();
            var cutscene = go.AddComponent<TimelineCutscene>();
            cutscene.PlayableDirector = director;

            var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"Assets/{DefaultObjectName}.playable");
            AssetDatabase.CreateAsset(timelineAsset, assetPath);
            Undo.RegisterCreatedObjectUndo(timelineAsset, "Create Cutscene Timeline");

            Undo.RecordObject(director, "Create Cutscene Timeline");
            director.playableAsset = timelineAsset;

            Undo.RecordObject(cutscene, "Create Cutscene Timeline");
            cutscene.EnsureDirectorTimelineRegistered();

            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(cutscene);
            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = go;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(timelineAsset);
        }
    }
}
