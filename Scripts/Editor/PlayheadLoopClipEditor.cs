using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    [CustomEditor(typeof(PlayheadLoopClip))]
    public class PlayheadLoopClipEditor : UnityEditor.Editor
    {
        static readonly string[] LoopModeLabels = { "Ping Pong", "Repeat" };
        static readonly CutsceneDialogueMode[] LoopModes =
        {
            CutsceneDialogueMode.PingPong,
            CutsceneDialogueMode.Repeat
        };

        SerializedProperty modeProp;

        void OnEnable()
        {
            modeProp = serializedObject.FindProperty("mode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int currentIndex = 0;
            for (int i = 0; i < LoopModes.Length; i++)
            {
                if ((CutsceneDialogueMode)modeProp.enumValueIndex == LoopModes[i])
                {
                    currentIndex = i;
                    break;
                }
            }

            if ((CutsceneDialogueMode)modeProp.enumValueIndex == CutsceneDialogueMode.Pause)
                modeProp.enumValueIndex = (int)CutsceneDialogueMode.Repeat;

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Mode", currentIndex, LoopModeLabels);
            if (EditorGUI.EndChangeCheck())
                modeProp.enumValueIndex = (int)LoopModes[selected];

            EditorGUILayout.HelpBox(
                "Loops the cutscene playhead within this clip (Repeat or Ping Pong). "
                + "Call TimelineCutscene.DisablePlayheadWrap() when the player should proceed.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
