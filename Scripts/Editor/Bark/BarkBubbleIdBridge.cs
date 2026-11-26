using UnityEditor;

namespace RAXY.Narrative
{
    [InitializeOnLoad]
    public static class BarkBubbleIdBridge
    {
        static BarkBubbleIdBridge()
        {
            BarkBubbleEditorIdProvider.GetRegisteredIds = () =>
            {
                var settings = NarrativeEditorSettings.instance;
                settings.EnsureDefaultBarkBubbleIds();
                return settings.BarkBubbleIds;
            };
        }
    }
}
