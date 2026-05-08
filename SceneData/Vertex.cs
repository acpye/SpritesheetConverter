using System.Numerics;

namespace _3DSpritesheetConverter.SceneData
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector4 Joints;
        public Vector4 Weights;
    }
}