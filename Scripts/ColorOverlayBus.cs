using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    public class ColorOverlayBus : MonoBehaviour
    {
        [SerializeField]
        Color color = Color.black;

        [SerializeField]
        float fadeDuration = 0.33f;

        [SerializeField]
        bool force;

        [Button]
        public void Execute()
        {
            ColorOverlayManager.Instance?.SetColorOverlay(color, fadeDuration, force);
        }
    }
}
