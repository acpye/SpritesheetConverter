using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace _3DSpritesheetConverter.SceneData
{
    class RenderMesh : IDisposable
    {
        public int VAO;
        public int VBO;
        public int EBO;
        public int IndexCount;
        public int TextureHandle { get; private set; }
        public int NodeIndex { get; private set; }
        public bool HasBones { get; private set; }

        public static RenderMesh Create(MeshData mesh)
        {
            RenderMesh renderMesh = new RenderMesh();

            renderMesh.VAO = GL.GenVertexArray();
            renderMesh.VBO = GL.GenBuffer();
            renderMesh.EBO = GL.GenBuffer();
            renderMesh.IndexCount = mesh.Indices.Count;

            GL.BindVertexArray(renderMesh.VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, renderMesh.VBO);
            Vertex[] vertexData = mesh.Vertices.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * Marshal.SizeOf<Vertex>(), vertexData, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderMesh.EBO);
            uint[] indexData = mesh.Indices.ToArray();
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(uint), indexData, BufferUsageHint.StaticDraw);

            int stride = Marshal.SizeOf<Vertex>();

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, OpenTK.Mathematics.Vector3.SizeInBytes);

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, OpenTK.Mathematics.Vector3.SizeInBytes * 2);

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, OpenTK.Mathematics.Vector3.SizeInBytes * 2 + OpenTK.Mathematics.Vector2.SizeInBytes);

            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, stride, OpenTK.Mathematics.Vector3.SizeInBytes * 2 + OpenTK.Mathematics.Vector2.SizeInBytes + OpenTK.Mathematics.Vector4.SizeInBytes);

            GL.BindVertexArray(0);

            return new RenderMesh
            {
                VAO = renderMesh.VAO,
                VBO = renderMesh.VBO,
                EBO = renderMesh.EBO,
                IndexCount = renderMesh.IndexCount,
                TextureHandle = mesh.TextureHandle,
                NodeIndex = mesh.NodeIndex,
                HasBones = mesh.HasBones
            };
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteVertexArray(VAO);
        }
    }
}