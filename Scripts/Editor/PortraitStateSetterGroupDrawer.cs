using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    /// <summary>
    /// Draws actor visibility toggles, then the default drawer (including TableList on states).
    /// </summary>
    public class PortraitStateSetterGroupDrawer : OdinValueDrawer<PortraitStateSetterGroup>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var group = ValueEntry.SmartValue;
            if (group == null)
            {
                CallNextDrawer(label);
                return;
            }

            group.SyncVisibleActorIds();
            DrawActorToggles(group);
            CallNextDrawer(label);
        }

        static void DrawActorToggles(PortraitStateSetterGroup group)
        {
            if (group.states == null || group.states.Count == 0)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(7));
            foreach (var setter in group.states)
            {
                if (setter == null || string.IsNullOrEmpty(setter.ActorId))
                    continue;

                string id = setter.ActorId;
                bool visible = group.IsActorVisible(id);
                bool newVisible = GUILayout.Toggle(visible, id, EditorStyles.miniButton);
                if (newVisible == visible)
                    continue;

                group.SetActorVisible(id, newVisible);
                GUIHelper.RequestRepaint();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
