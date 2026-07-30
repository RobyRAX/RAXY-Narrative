using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    [CustomEditor(typeof(CutsceneDialogueClip))]
    public class CutsceneDialogueClipEditor : Editor
    {
        SerializedProperty dialogueSOProp;
        SerializedProperty dialogueCollectionIdProp;
        SerializedProperty modeProp;
        SerializedProperty triggerTimeProp;

        void OnEnable()
        {
            dialogueSOProp = serializedObject.FindProperty("dialogueSO");
            dialogueCollectionIdProp = serializedObject.FindProperty("dialogueCollectionId");
            modeProp = serializedObject.FindProperty("mode");
            triggerTimeProp = serializedObject.FindProperty("triggerTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(triggerTimeProp);
            EditorGUILayout.PropertyField(modeProp);
            EditorGUILayout.PropertyField(dialogueSOProp);

            var dialogueSO = dialogueSOProp.objectReferenceValue as FullscreenDialogueDataSO;

            if (dialogueSO == null)
            {
                EditorGUILayout.HelpBox("Assign Dialogue SO !!!", MessageType.Info);
            }
            else
            {
                var options = dialogueSO.CollectionIds;

                if (options == null || options.Count == 0)
                {
                    EditorGUILayout.HelpBox("CollectionIds kosong di Dialogue SO ini.", MessageType.Warning);
                }
                else
                {
                    if (string.IsNullOrEmpty(dialogueCollectionIdProp.stringValue))
                        dialogueCollectionIdProp.stringValue = options[0];

                    string currentValue = dialogueCollectionIdProp.stringValue;
                    int currentIndex = options.IndexOf(currentValue);

                    bool isInvalid = currentIndex < 0;
                    string[] displayOptions = isInvalid
                        ? new[] { $"[Invalid] {currentValue}" }.Concat(options).ToArray()
                        : options.ToArray();

                    int selectedIndex = isInvalid ? 0 : currentIndex;

                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUILayout.Popup("Dialogue Collection Id", selectedIndex, displayOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!(isInvalid && newIndex == 0))
                        {
                            int listIndex = isInvalid ? newIndex - 1 : newIndex;
                            dialogueCollectionIdProp.stringValue = options[listIndex];
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
