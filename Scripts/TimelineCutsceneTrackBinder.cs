using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace RAXY.Narrative
{
    public class TimelineCutsceneTrackBinder : MonoBehaviour
    {
        [TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        public List<TrackBindEntry> trackBinds;

        public bool IsAnimationTrackBind
        {
            get
            {
                bool containAnimation = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.Animation)
                    {
                        containAnimation = true;
                        break;
                    }
                }

                return containAnimation;
            }
        }

        public bool IsCinemachineTrackBind
        {
            get
            {
                bool containCinemachine = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.Cinemachine)
                    {
                        containCinemachine = true;
                        break;
                    }
                }

                return containCinemachine;
            }
        }

        [TitleGroup("Track Ref")]
        [ShowIf("@IsAnimationTrackBind")]
        public Animator animator;

        [TitleGroup("Track Ref")]
        [ShowIf("@IsCinemachineTrackBind")]
        public CinemachineBrain cinemachineBrain;
    }

    [Serializable]
    public class TrackBindEntry
    {
        public string trackName;
        public TrackBindType trackBindType;
    }

    public enum TrackBindType
    {
        Animation,
        Cinemachine
    }
}
