using OpenTK.Graphics.OpenGL;

namespace _3DSpritesheetConverter.Managers;

class FramebufferManager : IDisposable
{
    public int FrameBufferObject { get; private set; }
    public int GameViewTextureHandle { get; private set; }
    public int DepthBufferObject { get; private set; }
    public int PostProcessFBO { get; private set; }
    public int PostProcessTextureHandle { get; private set; }
    public int PostProcessFBO2 { get; private set; }
    public int PostProcessTextureHandle2 { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Setup(int width, int height)
    {
        Width = width;
        Height = height;

        Cleanup();

        FrameBufferObject = CreateFramebuffer(out int gameViewTextureHandle, width, height, createDepth: true, out int depthBufferObject);
        GameViewTextureHandle = gameViewTextureHandle;
        DepthBufferObject = depthBufferObject;

        PostProcessFBO = CreateFramebuffer(out int postProcessTextureHandle, width, height, createDepth: false, out _);
        PostProcessTextureHandle = postProcessTextureHandle;

        PostProcessFBO2 = CreateFramebuffer(out int postProcessTextureHandle2, width, height, createDepth: false, out _);
        PostProcessTextureHandle2 = postProcessTextureHandle2;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private int CreateFramebuffer(out int textureHandle, int width, int height, bool createDepth, out int depthBuffer)
    {
        int fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, nint.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureHandle, 0);

        depthBuffer = 0;
        if (createDepth)
        {
            depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);
        }

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer is not complete");
        }

        return fbo;
    }

    private void Cleanup()
    {
        if (FrameBufferObject != 0) GL.DeleteFramebuffer(FrameBufferObject);
        if (GameViewTextureHandle != 0) GL.DeleteTexture(GameViewTextureHandle);
        if (DepthBufferObject != 0) GL.DeleteRenderbuffer(DepthBufferObject);
        if (PostProcessFBO != 0) GL.DeleteFramebuffer(PostProcessFBO);
        if (PostProcessTextureHandle != 0) GL.DeleteTexture(PostProcessTextureHandle);
        if (PostProcessFBO2 != 0) GL.DeleteFramebuffer(PostProcessFBO2);
        if (PostProcessTextureHandle2 != 0) GL.DeleteTexture(PostProcessTextureHandle2);
    }

    public void Dispose() => Cleanup();
}