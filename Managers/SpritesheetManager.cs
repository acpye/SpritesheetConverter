using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using _3DSpritesheetConverter.SceneData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _3DSpritesheetConverter.Managers;

public class PerspectiveOptions
{
    public bool Front = true;
    public bool Left = true;
    public bool Right = true;
    public bool Back = true;
    public bool Top = true;
    public bool Below = true;
}

class SpritesheetManager : IDisposable
{
    private SpritesheetGenerator? _generator;
    private int _textureHandle;

    private string? _lastErrorMessage;
    public string? LastErrorMessage => _lastErrorMessage;
    public bool HasWarning => !string.IsNullOrEmpty(_lastErrorMessage);
    public void ClearError() => _lastErrorMessage = null;

    private string? _lastExportMessage;
    public string? LastExportMessage => _lastExportMessage;
    public void ClearExportMessage() => _lastExportMessage = null;

    public int SnapshotCount => _generator?.SnapshotCount ?? 0;
    public int TextureHandle => _textureHandle;
    public int TargetDimension { get; set; } = 512;

    public void Initialize(int width, int height)
    {
        _generator = new SpritesheetGenerator(width, height, TargetDimension, TargetDimension);
    }

    private void PrepareGenerator(int width, int height, bool clearExisting = true)
    {
        if (clearExisting || _generator == null || _generator.SnapshotCount == 0)
        {
            _generator = new SpritesheetGenerator(width, height, TargetDimension, TargetDimension);
        }
    }

    public void TakeSnapshot(Action renderAction, int width, int height)
    {
        PrepareGenerator(width, height, clearExisting: false);
        _generator?.TakeSnapshot(renderAction);
        UpdateTextureFromGenerator();
    }

    public void GenerateRotationSpritesheet
    (
        ModelTransform modelTransform,
        Action renderAction,
        int width,
        int height,
        int frames = 36
    )
    {
        PrepareGenerator(width, height);
        Vector3 originalRotation = (Vector3)modelTransform.Rotation;

        float angleStep = 360f / frames;

        for (int i = 0; i < frames; i++)
        {
            modelTransform.Rotation = (System.Numerics.Vector3)new Vector3
            (
                originalRotation.X,
                originalRotation.Y + i * angleStep,
                originalRotation.Z
            );
            _generator.TakeSnapshot(renderAction);
        }

        modelTransform.Rotation = (System.Numerics.Vector3)originalRotation;
        UpdateTextureFromGenerator();
    }

    public void GenerateAnimationSpritesheet
    (
        ModelData model,
        ModelTransform modelTransform,
        Action renderAction,
        int width,
        int height,
        int fps,
        bool multiPerspective,
        PerspectiveOptions options
    )
    {
        PrepareGenerator(width, height);
        
        if (multiPerspective)
        {
            Vector3 originalRotation = (Vector3)modelTransform.Rotation;
            Vector3 originalPosition = (Vector3)modelTransform.Position;

            List<(string Name, Vector3 Rotation, Vector3 Position)> perspectives = new List<(string Name, Vector3 Rotation, Vector3 Position)>();
            
            if (options.Front) perspectives.Add(("Front", new Vector3(originalRotation.X, originalRotation.Y, originalRotation.Z), originalPosition));
            if (options.Left)  perspectives.Add(("Left", new Vector3(originalRotation.X, originalRotation.Y + 270f, originalRotation.Z), originalPosition));
            if (options.Right) perspectives.Add(("Right", new Vector3(originalRotation.X, originalRotation.Y + 90f, originalRotation.Z), originalPosition));
            if (options.Back)  perspectives.Add(("Back", new Vector3(originalRotation.X, originalRotation.Y + 180f, originalRotation.Z), originalPosition));
            if (options.Top)   perspectives.Add(("Top", new Vector3(originalRotation.X + 60f, originalRotation.Y, originalRotation.Z), new Vector3(originalPosition.X, originalPosition.Y, originalPosition.Z - 1.0f)));
            if (options.Below) perspectives.Add(("Below", new Vector3(originalRotation.X - 60f, originalRotation.Y, originalRotation.Z), new Vector3(originalPosition.X, originalPosition.Y + 0.50f, originalPosition.Z + 1f)));

            foreach ((string Name, Vector3 Rotation, Vector3 Position) perspective in perspectives)
            {
                modelTransform.Rotation = (System.Numerics.Vector3)perspective.Rotation;
                modelTransform.Position = (System.Numerics.Vector3)perspective.Position;
                _generator.TakeAnimationSnapshots(model, renderAction, fps, $"{perspective.Name}_");
            }

            modelTransform.Rotation = (System.Numerics.Vector3)originalRotation;
            modelTransform.Position = (System.Numerics.Vector3)originalPosition;
        }
        else
        {
            _generator.TakeAnimationSnapshots(model, renderAction, fps, "Animation_");
        }
        
        UpdateTextureFromGenerator();
    }

    public void ClearSnapshots()
    {
        _generator?.ClearSnapshots();
        if (_textureHandle != 0)
        {
            GL.DeleteTexture(_textureHandle);
            _textureHandle = 0;
        }
    }

    public void Export()
    {
        if (_generator == null || _generator.SnapshotCount == 0)
        {
            return;
        }

        Directory.CreateDirectory("Output");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string imagePath = Path.Combine("Output", $"spritesheet_{timestamp}.png");
        string jsonPath = Path.Combine("Output", $"spritesheet_{timestamp}.json");

        try
        {
            _generator.SaveSpritesheet(imagePath);
        }
        catch (SixLabors.ImageSharp.Memory.InvalidMemoryOperationException ex)
        {
            _lastErrorMessage = $"Unable to allocate memory for the spritesheet image buffer. Reduce the amount of snapshot frames or dimension size. Detailed Error: {ex.Message}";
            return;
        }

        int framesCount = _generator.SnapshotCount;
        int tileWidth = _generator.Width;
        int tileHeight = _generator.Height;
        int columns = (int)Math.Ceiling(Math.Sqrt(framesCount));

        SpritesheetData spritesheetInfo = new SpritesheetData
        {
            frames = new Dictionary<string, object>()
        };

        for (int i = 0; i < framesCount; i++)
        {
            int xPosition = (i % columns) * tileWidth;
            int yPosition = (i / columns) * tileHeight;
            
            string objectName = _generator.FrameNames.Count > i ? 
                $"{_generator.FrameNames[i]}{i:D3}" : $"tile{i:D3}";

            spritesheetInfo.frames[objectName] = new
            {
                frame = new { x = xPosition, y = yPosition, w = tileWidth, h = tileHeight },
                sourceSize = new { w = tileWidth, h = tileHeight }
            };
        }

        string jsonString = JsonSerializer.Serialize(spritesheetInfo, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, jsonString);

        _lastExportMessage = $"Spritesheet exported to: '{imagePath}'\n Metadata exported to '{jsonPath}'.";
    }

    private void UpdateTextureFromGenerator()
    {
        Image<Rgba32>? spritesheet = null;
        
        try
        {
            spritesheet = _generator?.GenerateFromSnapshots();
        }
        catch (SixLabors.ImageSharp.Memory.InvalidMemoryOperationException ex)
        {
            _lastErrorMessage = $"Unable to allocate memory for the generated texture. Reduce the amount of snapshot frames or dimension size. Detailed Error: {ex.Message}";
            _generator?.ClearSnapshots();
            
            if (_textureHandle != 0)
            {
                GL.DeleteTexture(_textureHandle);
                _textureHandle = 0;
            }
            return;
        }

        if (spritesheet == null)
        {
            return;
        }

        int maxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
        
        using Image<Rgba32> previewImage = spritesheet.Clone(ctx =>
        {
            if (spritesheet.Width > maxTextureSize || spritesheet.Height > maxTextureSize)
            {
                float ratio = Math.Min((float)maxTextureSize / spritesheet.Width, (float)maxTextureSize / spritesheet.Height);
                int newWidth = (int)(spritesheet.Width * ratio);
                int newHeight = (int)(spritesheet.Height * ratio);
                ctx.Resize(newWidth, newHeight);
            }
        });
        
        spritesheet.Dispose();

        long pixelCount = (long)previewImage.Width * previewImage.Height;
        const int bytesPerPixel = 4;
        long totalBytes = pixelCount * bytesPerPixel;

        if (totalBytes > int.MaxValue)
        {
            _lastErrorMessage = $"Unable to allocate pixel buffer: required {totalBytes:N0} bytes exceeds maximum array size ({int.MaxValue:N0}). Reduce amount of snapshot frames or dimension size.";

            if (_textureHandle != 0)
            {
                GL.DeleteTexture(_textureHandle);
                _textureHandle = 0;
            }
            return;
        }

        _lastErrorMessage = null;

        int size = (int)totalBytes;

        GL.BindTexture(TextureTarget.Texture2D, 0);
        if (_textureHandle != 0)
        {
            GL.DeleteTexture(_textureHandle);
        }
        _textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _textureHandle);

        byte[] pixelData = new byte[size];
        previewImage.CopyPixelDataTo(pixelData);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, previewImage.Width, previewImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Dispose()
    {
        if (_textureHandle != 0)
        {
            GL.DeleteTexture(_textureHandle);
            _textureHandle = 0;
        }
    }
}