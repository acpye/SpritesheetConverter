using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace _3DSpritesheetConverter.SceneData
{
    public class AnimationData
    {
        public string Name { get; set; } = string.Empty;
        public float Duration { get; set; }
        public List<AnimationChannel> Channels { get; set; } = new List<AnimationChannel>();
    }

    public class AnimationChannel
    {
        public string TargetNode { get; set; } = string.Empty;
        public AnimationPath Path { get; set; }
        public List<Keyframe> Keyframes { get; set; } = new List<Keyframe>();
    }

    public enum AnimationPath
    {
        Translation,
        Rotation,
        Scale
    }

    public class Keyframe
    {
        public float Time { get; set; }
        public object Value { get; set; } = Vector3.Zero;
    }
}
