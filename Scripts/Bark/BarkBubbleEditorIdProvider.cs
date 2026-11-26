#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace RAXY.Narrative
{
    public static class BarkBubbleEditorIdProvider
    {
        public static Func<IEnumerable<string>> GetRegisteredIds;

        public static IEnumerable<string> GetDropdownIds()
        {
            return GetRegisteredIds?.Invoke() ?? Array.Empty<string>();
        }
    }
}
#endif
