using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace _3DSpritesheetConverter.Managers;

class TextureManager : IDisposable
{
    private readonly List<int> _loadedTextures = new();

    public int LoadTexture(string path)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(path);
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        return CreateTextureFromImage(image);
    }

    public int LoadTextureFromBytes(byte[] data)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(data);
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        return CreateTextureFromImage(image);
    }

    private int CreateTextureFromImage(Image<Rgba32> image)
    {
        byte[] pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        int textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        GL.TexImage2D
        (
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            image.Width,
            image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixels
        );

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        _loadedTextures.Add(textureHandle);
        return textureHandle;
    }

    public void BindTexture(int textureHandle, TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
    }

    public void Dispose()
    {
        foreach (int texture in _loadedTextures)
        {
            GL.DeleteTexture(texture);
        }
        _loadedTextures.Clear();
    }
}