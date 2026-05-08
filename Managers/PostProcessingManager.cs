using _3DSpritesheetConverter.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DSpritesheetConverter.Managers;

class PostProcessingManager
{
    private Shader _pixelationShader;
    private Shader _celShadingShader;
    private int _quadVAO;
    private int _quadVBO;

    public bool PixelationEnabled { get; set; }
    public float PixelSize { get; set; } = 4f;
    public bool CelShadingEnabled { get; set; }
    public int CelShadingLevels { get; set; } = 4;
    public float CelShadingEdgeThreshold { get; set; } = 0.2f;

    public bool AnyEffectEnabled => PixelationEnabled || CelShadingEnabled;

    public void Initialize()
    {
        _pixelationShader = new Shader("Shaders/pixelation.vert", "Shaders/pixelation.frag");
        _celShadingShader = new Shader("Shaders/celshading.vert", "Shaders/celshading.frag");
        SetupScreenQuad();
    }

    public void ApplyEffects(FramebufferManager fbManager, int width, int height, float r, float g, float b, float a)
    {
        int currentSourceTexture = fbManager.GameViewTextureHandle;
        int currentTargetFBO = fbManager.PostProcessFBO;

        GL.Disable(EnableCap.DepthTest);

        if (CelShadingEnabled)
        {
            ApplyShader(_celShadingShader, currentTargetFBO, currentSourceTexture, width, height, r, g, b, a, shader =>
            {
                shader.SetInt("levels", CelShadingLevels);
                shader.SetFloat("edgeThreshold", CelShadingEdgeThreshold);
            });
            currentSourceTexture = fbManager.PostProcessTextureHandle;
            currentTargetFBO = fbManager.PostProcessFBO2;
        }

        if (PixelationEnabled)
        {
            ApplyShader(_pixelationShader, currentTargetFBO, currentSourceTexture, width, height, r, g, b, a, shader =>
            {
                shader.SetFloat("pixelSize", PixelSize);
            });
        }

        GL.Enable(EnableCap.DepthTest);
    }

    private void ApplyShader(Shader shader, int targetFBO, int sourceTexture, int width, int height, float r, float g, float b, float a, Action<Shader> configureShader)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFBO);
        GL.Viewport(0, 0, width, height);
        GL.ClearColor(r, g, b, a);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shader.Use();
        shader.SetInt("screenTexture", 0);
        shader.SetVector2("resolution", new Vector2(width, height));
        configureShader(shader);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, sourceTexture);
        GL.BindVertexArray(_quadVAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
    }

    private void SetupScreenQuad()
    {
        float[] quadVertices =
        {
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        };

        _quadVAO = GL.GenVertexArray();
        _quadVBO = GL.GenBuffer();
        GL.BindVertexArray(_quadVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public int GetFinalFBO(FramebufferManager fb) =>
        (CelShadingEnabled, PixelationEnabled) switch
        {
            (true, true) => fb.PostProcessFBO2,
            (true, false) or (false, true) => fb.PostProcessFBO,
            _ => fb.FrameBufferObject
        };

    public int GetDisplayTexture(FramebufferManager fb) =>
        (CelShadingEnabled, PixelationEnabled) switch
        {
            (true, true) => fb.PostProcessTextureHandle2,
            (true, false) or (false, true) => fb.PostProcessTextureHandle,
            _ => fb.GameViewTextureHandle
        };
}