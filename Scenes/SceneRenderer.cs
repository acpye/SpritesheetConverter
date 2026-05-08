using _3DSpritesheetConverter.Managers;
using _3DSpritesheetConverter.SceneData;
using _3DSpritesheetConverter.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DSpritesheetConverter.Scenes
{
    class SceneRenderer
    {
        private readonly Shader _shader;
        private readonly TextureManager _textureManager;

        public SceneRenderer(Shader shader, TextureManager textureManager)
        {
            _shader = shader;
            _textureManager = textureManager;
        }

        public void RenderScene(SceneContext context)
        {
            if (!context.ModelTransform.IsEnabled) return;

            _shader.Use();

            Matrix4 globalModelMatrix =
                            Matrix4.CreateScale(context.ModelTransform.Scale.X, context.ModelTransform.Scale.Y, context.ModelTransform.Scale.Z) *
                            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(context.ModelTransform.Rotation.X)) *
                            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(context.ModelTransform.Rotation.Y)) *
                            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(context.ModelTransform.Rotation.Z)) *
                            Matrix4.CreateTranslation(context.ModelTransform.Position.X, context.ModelTransform.Position.Y, context.ModelTransform.Position.Z);

            Matrix4 view = context.Camera.GetViewMatrix();
            Matrix4 projection = context.Camera.GetProjectionMatrix();

            OpenTK.Mathematics.Vector3 cameraPosition = context.Camera.Position;
            
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);
            
            _shader.SetVector3("viewPosition", cameraPosition);
            _shader.SetVector3("objectColour", new OpenTK.Mathematics.Vector3(0.8f, 0.8f, 0.8f));

            OpenTK.Mathematics.Vector3 finalLightColour = context.Light.IsDirectionalEnabled ? (context.Light.Colour * context.Light.Intensity) : OpenTK.Mathematics.Vector3.Zero;
            _shader.SetVector3("lightPosition", context.Light.Position);
            _shader.SetVector3("lightColour", finalLightColour);

            OpenTK.Mathematics.Vector3 finalAmbientColour = context.Light.IsAmbientEnabled ? (context.Light.AmbientColour * context.Light.AmbientIntensity) : OpenTK.Mathematics.Vector3.Zero;
            _shader.SetVector3("ambientLight", finalAmbientColour);

            System.Numerics.Matrix4x4[] boneMatrices = context.ModelLoader.BoneMatrices;
            if (boneMatrices != null && boneMatrices.Length > 0)
            {
                int boneUniformLoc = GL.GetUniformLocation(_shader.Handle, "boneMatrices");
                GL.UniformMatrix4(boneUniformLoc, boneMatrices.Length, false, ref boneMatrices[0].M11);
            }

            System.Numerics.Matrix4x4[] nodeMatrices = context.ModelLoader.NodeMatrices;

            foreach (RenderMesh renderMesh in context.RenderMeshes)
            {
                Matrix4 finalModelMatrix = globalModelMatrix;

                if (!renderMesh.HasBones && renderMesh.NodeIndex >= 0 && nodeMatrices != null && renderMesh.NodeIndex < nodeMatrices.Length)
                {
                    System.Numerics.Matrix4x4 nm = nodeMatrices[renderMesh.NodeIndex];

                    Matrix4 nodeMatrixTK = new Matrix4(
                        nm.M11, nm.M12, nm.M13, nm.M14,
                        nm.M21, nm.M22, nm.M23, nm.M24,
                        nm.M31, nm.M32, nm.M33, nm.M34,
                        nm.M41, nm.M42, nm.M43, nm.M44
                    );

                    finalModelMatrix = nodeMatrixTK * globalModelMatrix;
                }

                _shader.SetMatrix4("model", finalModelMatrix);

                if (renderMesh.TextureHandle > 0)
                {
                    _shader.SetInt("useTexture", 1);
                    _shader.SetInt("diffuseTexture", 0);
                    _textureManager.BindTexture(renderMesh.TextureHandle, TextureUnit.Texture0);
                }
                else
                {
                    _shader.SetInt("useTexture", 0);
                }

                GL.BindVertexArray(renderMesh.VAO);
                GL.DrawElements(PrimitiveType.Triangles, renderMesh.IndexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        public void RenderWithPostProcess
        (
            FramebufferManager framebufferManager,
            PostProcessingManager postProcessingManager,
            Action renderSceneAction,
            int width,
            int height,
            bool useTransparentBackground = false
        )
        {
            float r = useTransparentBackground ? 0f : Color4.SteelBlue.R;
            float g = useTransparentBackground ? 0f : Color4.SteelBlue.G;
            float b = useTransparentBackground ? 0f : Color4.SteelBlue.B;
            float a = useTransparentBackground ? 0f : 1f;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferManager.FrameBufferObject);
            GL.Viewport(0, 0, framebufferManager.Width, framebufferManager.Height);
            GL.DepthMask(true);
            GL.ClearColor(r, g, b, a);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            renderSceneAction();

            if (postProcessingManager.AnyEffectEnabled)
            {
                postProcessingManager.ApplyEffects(framebufferManager, width, height, r, g, b, a);
            }

            GL.Finish();
        }
    }
}