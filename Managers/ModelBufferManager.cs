using System.IO;
using _3DSpritesheetConverter.SceneData;
using _3DSpritesheetConverter.Shaders;
using _3DSpritesheetConverter.Managers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DSpritesheetConverter.Managers
{
    class ModelBufferManager : IDisposable
    {
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;
        private int _normalBufferObject;
        private int _textureCoordinateBufferObject;
        private int _jointBufferObject;
        private int _weightBufferObject;

        public int VertexArrayObject => _vertexArrayObject;

        public void SetupBuffers(ModelData model, Shader shader)
        {
            Cleanup();

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, model.Vertices.Count * Vector3.SizeInBytes, model.Vertices.ToArray(), BufferUsageHint.StaticDraw);

            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, model.Indices.Count * sizeof(int), model.Indices.ToArray(), BufferUsageHint.StaticDraw);

            _normalBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _normalBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, model.Normals.Count * Vector3.SizeInBytes, model.Normals.ToArray(), BufferUsageHint.StaticDraw);

            _textureCoordinateBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _textureCoordinateBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, model.TextureCoordinates.Count * Vector2.SizeInBytes, model.TextureCoordinates.ToArray(), BufferUsageHint.StaticDraw);

            if (model.Joints.Count > 0)
            {
                _jointBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, _jointBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, model.Joints.Count * Vector4.SizeInBytes, model.Joints.ToArray(), BufferUsageHint.StaticDraw);
            }

            if (model.Weights.Count > 0)
            {
                _weightBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, _weightBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, model.Weights.Count * Vector4.SizeInBytes, model.Weights.ToArray(), BufferUsageHint.StaticDraw);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            int vertexLocation = shader.GetAttribLocation("aPosition");
            if (vertexLocation >= 0)
            {
                GL.EnableVertexAttribArray(vertexLocation);
                GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _normalBufferObject);
            int normalLocation = shader.GetAttribLocation("aNormal");
            if (normalLocation >= 0)
            {
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _textureCoordinateBufferObject);
            int texCoordLocation = shader.GetAttribLocation("aTextureCoordinate");
            if (texCoordLocation >= 0)
            {
                GL.EnableVertexAttribArray(texCoordLocation);
                GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
            }

            if (_jointBufferObject != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _jointBufferObject);
                int jointLocation = shader.GetAttribLocation("aJoints");
                if (jointLocation >= 0)
                {
                    GL.EnableVertexAttribArray(jointLocation);
                    GL.VertexAttribPointer(jointLocation, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, 0);
                }
            }

            if (_weightBufferObject != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _weightBufferObject);
                int weightLocation = shader.GetAttribLocation("aWeights");
                if (weightLocation >= 0)
                {
                    GL.EnableVertexAttribArray(weightLocation);
                    GL.VertexAttribPointer(weightLocation, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, 0);
                }
            }

            GL.BindVertexArray(0);
            shader.Use();
        }

        private void Cleanup()
        {
            if (_vertexBufferObject != 0) GL.DeleteBuffer(_vertexBufferObject);
            if (_elementBufferObject != 0) GL.DeleteBuffer(_elementBufferObject);
            if (_normalBufferObject != 0) GL.DeleteBuffer(_normalBufferObject);
            if (_textureCoordinateBufferObject != 0) GL.DeleteBuffer(_textureCoordinateBufferObject);
            if (_jointBufferObject != 0) GL.DeleteBuffer(_jointBufferObject);
            if (_weightBufferObject != 0) GL.DeleteBuffer(_weightBufferObject);
            if (_vertexArrayObject != 0) GL.DeleteVertexArray(_vertexArrayObject);

            _vertexBufferObject = 0;
            _elementBufferObject = 0;
            _normalBufferObject = 0;
            _textureCoordinateBufferObject = 0;
            _jointBufferObject = 0;
            _weightBufferObject = 0;
            _vertexArrayObject = 0;
        }

        public void Dispose() => Cleanup();
    }
}
