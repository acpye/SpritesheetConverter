using System;
using System.Collections.Generic;

namespace _3DSpritesheetConverter.SceneData
{
    class MeshData
    {
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<uint> Indices { get; set; } = new List<uint>();
        public byte[]? TextureBytes { get; set; }
        public int TextureHandle { get; set; }
        public int NodeIndex { get; set; }
        public bool HasBones { get; set; }
    }
}