using UnityEngine;
using UnityEngine.Playables;

namespace RAXY.Narrative
{
    public class ColorOverlayMixerBehaviour : PlayableBehaviour
    {
        ColorOverlayManager _boundManager;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var manager = playerData as ColorOverlayManager;
            if (manager == null)
                return;

            _boundManager = manager;

            var inputCount = playable.GetInputCount();
            var blendedAlpha = 0f;
            var blendedRgb = Color.clear;
            var totalWeight = 0f;

            for (var i = 0; i < inputCount; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f)
                    continue;

                var inputPlayable = (ScriptPlayable<ColorOverlayBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                var time = (float)inputPlayable.GetTime();
                var duration = (float)inputPlayable.GetDuration();

                var alpha = ColorOverlayBehaviour.EvaluateHoldAlpha(
                    time,
                    duration,
                    behaviour.fadeIn,
                    behaviour.fadeOut);

                blendedAlpha += alpha * weight;
                blendedRgb += behaviour.color * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                var clearColor = manager.CurrentColor;
                clearColor.a = 0f;
                manager.SetColorOverlay(clearColor, 0f);
                return;
            }

            blendedRgb /= totalWeight;
            blendedRgb.a = blendedAlpha;
            manager.SetColorOverlay(blendedRgb, 0f);
        }

        // public override void OnGraphStop(Playable playable)
        // {
        //     ClearOverlay();
        // }

        // public override void OnPlayableDestroy(Playable playable)
        // {
        //     ClearOverlay();
        // }

        // void ClearOverlay()
        // {
        //     var manager = _boundManager != null ? _boundManager : ColorOverlayManager.Instance;
        //     if (manager != null)
        //     {
        //         var clearColor = manager.CurrentColor;
        //         clearColor.a = 0f;
        //         manager.SetColorOverlay(clearColor, 0f);
        //     }
        //     _boundManager = null;
        // }
    }
} 
