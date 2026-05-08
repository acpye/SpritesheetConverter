using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace _3DSpritesheetConverter.Shaders
{
    public class SkyboxRenderer : IDisposable
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _ebo;
        private readonly int _indexCount;
        private readonly int _texture;
        private readonly Shader _shader;

        public SkyboxRenderer(string imagePath, int segments = 64, float radius = 1f)
        {
            _shader = new Shader("Shaders/skybox.vert", "Shaders/skybox.frag");

            GenerateSphere(segments, radius, out float[] vertices, out uint[] indices);
            _indexCount = indices.Length;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            _texture = LoadTexture(imagePath);
            
            _shader.Use();
            _shader.SetInt("skybox", 0);
            _shader.SetVector4("colour", new Vector4(1f, 1f, 1f, 1f));
        }

        private void GenerateSphere(int segments, float radius, out float[] vertices, out uint[] indices)
        {
            List<float> vertexList = new List<float>();
            List<uint> indexList = new List<uint>();

            for (int y = 0; y <= segments; y++)
            {
                for (int x = 0; x <= segments; x++)
                {
                    float xSegment = (float)x / segments;
                    float ySegment = (float)y / segments;

                    float xPosition = (float)(Math.Cos(xSegment * 2.0 * Math.PI) * Math.Sin(ySegment * Math.PI));
                    float yPosition = (float)(Math.Cos(ySegment * Math.PI));
                    float zPosition = (float)(Math.Sin(xSegment * 2.0 * Math.PI) * Math.Sin(ySegment * Math.PI));

                    vertexList.Add(xPosition * radius);
                    vertexList.Add(yPosition * radius);
                    vertexList.Add(zPosition * radius);

                    vertexList.Add(xSegment);
                    vertexList.Add(1.0f - ySegment);
                }
            }

            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    uint i0 = (uint)((y + 1) * (segments + 1) + x);
                    uint i1 = (uint)(y * (segments + 1) + x);
                    uint i2 = (uint)(y * (segments + 1) + x + 1);
                    uint i3 = (uint)((y + 1) * (segments + 1) + x + 1);

                    indexList.Add(i0);
                    indexList.Add(i1);
                    indexList.Add(i2);
                    indexList.Add(i0);
                    indexList.Add(i2);
                    indexList.Add(i3);
                }
            }

            vertices = vertexList.ToArray();
            indices = indexList.ToArray();
        }

        private int LoadTexture(string path)
        {
            int textureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            if (File.Exists(path))
            {
                using (Image<Rgba32> image = Image.Load<Rgba32>(path))
                {
                    image.Mutate(x => x.Flip(FlipMode.Vertical));

                    byte[] pixelData = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(pixelData);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
                }
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return textureID;
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            bool wasCullEnabled = GL.IsEnabled(EnableCap.CullFace);
            bool wasBlendEnabled = GL.IsEnabled(EnableCap.Blend);
            int previousCullFace = GL.GetInteger(GetPName.CullFaceMode);

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(false);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Front);
            
            if (wasBlendEnabled) GL.Disable(EnableCap.Blend);

            _shader.Use();
            Matrix3 view3 = new Matrix3(view);
            Matrix4 viewNoTranslation = new Matrix4(view3);
            _shader.SetMatrix4("view", viewNoTranslation);
            _shader.SetMatrix4("projection", projection);

            GL.BindVertexArray(_vao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            if (!wasCullEnabled)
            {
                GL.Disable(EnableCap.CullFace);
            }
            else
            {
                GL.CullFace((TriangleFace)previousCullFace);
            }

            if (wasBlendEnabled) GL.Enable(EnableCap.Blend);

            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Less);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteTexture(_texture);
            _shader.Dispose();
        }
    }
}