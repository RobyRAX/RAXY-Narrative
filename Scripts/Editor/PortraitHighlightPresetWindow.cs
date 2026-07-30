using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    [InitializeOnLoad]
    static class PortraitHighlightPresetWindowBootstrap
    {
        static PortraitHighlightPresetWindowBootstrap()
        {
            PortraitHighlightPreset.OpenWindowHandler = PortraitHighlightPresetWindow.Open;
        }
    }

    public class PortraitHighlightPresetWindow : OdinEditorWindow
    {
        public static void Open()
        {
            var window = GetWindow<PortraitHighlightPresetWindow>("Highlight Preset");
            window.minSize = new Vector2(300, 120);
            window.Show();
        }

        [ShowInInspector]
        [LabelText("Highlight Color")]
        Color Highlight
        {
            get => PortraitHighlightPreset.HighlightColor;
            set => PortraitHighlightPreset.HighlightColor = value;
        }

        [ShowInInspector]
        [LabelText("No Highlight Color")]
        Color NoHighlight
        {
            get => PortraitHighlightPreset.NoHighlightColor;
            set => PortraitHighlightPreset.NoHighlightColor = value;
        }

        [Button("Reset to Default")]
        void ResetToDefault() => PortraitHighlightPreset.ResetToDefault();
    }
}
