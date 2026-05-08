using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3DSpritesheetConverter.SceneData
{
    class ModelData
    {
        public string Name { get; set; } = string.Empty;
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<Vector2> TextureCoordinates { get; } = new List<Vector2>();
        public List<Vector3> Normals { get; } = new List<Vector3>();
        public List<int> Indices { get; } = new List<int>();
        public List<Vector4> Joints { get; } = new List<Vector4>();
        public List<Vector4> Weights { get; } = new List<Vector4>();
        public List<AnimationData> Animations { get; } = new List<AnimationData>();
        public int TextureHandle { get; set; } = 0;
        public bool HasTexture => TextureHandle != 0;
        public bool IsPlaying { get; set; } = false;
        public int CurrentAnimation { get; private set; } = 0;
        public float CurrentAnimationTime { get; set; } = 0f;
        public float PlaybackSpeed { get; set; } = 1.0f;

        public AnimationData? CurrentAnimationData =>
            (Animations.Count > 0 && CurrentAnimation >= 0 && CurrentAnimation < Animations.Count)
                ? Animations[CurrentAnimation]
                : null;

        public void SetCurrentAnimation(int index)
        {
            if (Animations.Count > 0 && index >= 0 && index < Animations.Count)
            {
                CurrentAnimation = index;
                CurrentAnimationTime = 0f;
            }
            else
            {
                CurrentAnimation = 0;
                CurrentAnimationTime = 0f;
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsPlaying || CurrentAnimationData == null) return;

            float duration = CurrentAnimationData.Duration;
            if (duration > 0)
            {
                CurrentAnimationTime += deltaTime * PlaybackSpeed;

                if (PlaybackSpeed > 0 && CurrentAnimationTime > duration)
                {
                    CurrentAnimationTime %= duration;
                }
                else if (PlaybackSpeed < 0 && CurrentAnimationTime < 0)
                {
                    CurrentAnimationTime = duration + (CurrentAnimationTime % duration);
                }
            }
        }
    }
}