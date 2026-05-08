using System.Numerics;

namespace _3DSpritesheetConverter.SceneData
{
    class ModelTransform
    {
        public bool IsEnabled { get; set; } = true;

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;
    }
}