using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using _3DSpritesheetConverter.SceneData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace _3DSpritesheetConverter
{
    class SpritesheetGenerator
    {
        private readonly int _captureWidth;
        private readonly int _captureHeight;
        private readonly int _targetWidth;
        private readonly int _targetHeight;
        private readonly List<Image<Rgba32>> _snapshots = new();
        public readonly List<string> FrameNames = new();
        
        public int Width => _targetWidth;
        public int Height => _targetHeight;
        public int SnapshotCount => _snapshots.Count;

        public SpritesheetGenerator(int width, int height, int targetWidth = 512, int targetHeight = 512)
        {
            _captureWidth = width;
            _captureHeight = height;
            _targetWidth = targetWidth;
            _targetHeight = targetHeight;
        }

        public void TakeSnapshot(Action renderFrame, string framePrefix = "Manual_")
        {
            renderFrame();

            byte[] pixelData = new byte[_captureWidth * _captureHeight * 4];
            GL.ReadPixels(0, 0, _captureWidth, _captureHeight, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

            Image<Rgba32> snapshot = Image.LoadPixelData<Rgba32>(pixelData, _captureWidth, _captureHeight);
            snapshot.Mutate(x => 
            {
                x.Flip(FlipMode.Vertical);
                x.Resize(_targetWidth, _targetHeight);
            });
            _snapshots.Add(snapshot);
            FrameNames.Add(framePrefix);
        }

        public void ClearSnapshots()
        {
            foreach (Image<Rgba32> snapshot in _snapshots)
            {
                snapshot.Dispose();
            }
            _snapshots.Clear();
            FrameNames.Clear();
        }

        public Image<Rgba32>? GenerateFromSnapshots()
        {
            if (_snapshots.Count == 0) return null;

            int frames = _snapshots.Count;
            int columns = (int)Math.Ceiling(Math.Sqrt(frames));
            int rows = (int)Math.Ceiling((double)frames / columns);

            int sheetWidth = columns * _targetWidth;
            int sheetHeight = rows * _targetHeight;

            Image<Rgba32> spritesheet = new Image<Rgba32>(sheetWidth, sheetHeight);

            for (int i = 0; i < frames; i++)
            {
                int xPosition = (i % columns) * _targetWidth;
                int yPosition = (i / columns) * _targetHeight;
                spritesheet.Mutate(ctx => ctx.DrawImage(_snapshots[i], new Point(xPosition, yPosition), 1f));
            }

            return spritesheet;
        }

        public void SaveSpritesheet(string outputPath)
        {
            using Image<Rgba32>? spritesheet = GenerateFromSnapshots();
            spritesheet?.Save(outputPath);
        }

        public void Generate(ModelTransform transform, Action renderFrame, int frames, string outputPath)
        {
            int columns = (int)Math.Ceiling(Math.Sqrt(frames));
            int rows = (int)Math.Ceiling((double)frames / columns);

            int sheetWidth = columns * _targetWidth;
            int sheetHeight = rows * _targetHeight;

            using (Image<Rgba32> spritesheet = new Image<Rgba32>(sheetWidth, sheetHeight))
            {
                float angleStep = 360f / frames;
                Vector3 originalRotation = (Vector3)transform.Rotation;

                byte[] pixelData = new byte[_captureWidth * _captureHeight * 4];

                for (int i = 0; i < frames; i++)
                {
                    transform.Rotation = (System.Numerics.Vector3)new Vector3(originalRotation.X, originalRotation.Y + (i * angleStep), originalRotation.Z);

                    renderFrame();

                    GL.ReadPixels(0, 0, _captureWidth, _captureHeight, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

                    using (Image frameImage = Image.LoadPixelData<Rgba32>(pixelData, _captureWidth, _captureHeight))
                    {
                        frameImage.Mutate(x => 
                        {
                            x.Flip(FlipMode.Vertical);
                            x.Resize(_targetWidth, _targetHeight);
                        });

                        int xPosition = (i % columns) * _targetWidth;
                        int yPosition = (i / columns) * _targetHeight;

                        spritesheet.Mutate(ctx => ctx.DrawImage(frameImage, new Point(xPosition, yPosition), 1f));
                    }
                }

                transform.Rotation = (System.Numerics.Vector3)originalRotation;

                spritesheet.Save(outputPath);
            }
        }

        public void GenerateAnimation(ModelData model, Action renderFrame, int fps, string outputPath)
        {
            AnimationData? currentAnimation = model.CurrentAnimationData;
            if (currentAnimation == null || currentAnimation.Duration <= 0f) return;

            float animationDuration = currentAnimation.Duration;
            int frames = (int)(animationDuration * fps);
            if (frames == 0) return;

            int columns = (int)Math.Ceiling(Math.Sqrt(frames));
            int rows = (int)Math.Ceiling((double)frames / columns);

            int sheetWidth = columns * _targetWidth;
            int sheetHeight = rows * _targetHeight;

            using (Image<Rgba32> spritesheet = new Image<Rgba32>(sheetWidth, sheetHeight))
            {
                float timeStep = animationDuration / frames;
                float originalAnimationTime = model.CurrentAnimationTime;

                byte[] pixelData = new byte[_captureWidth * _captureHeight * 4];

                for (int i = 0; i < frames; i++)
                {
                    model.CurrentAnimationTime = i * timeStep;

                    renderFrame();

                    GL.ReadPixels(0, 0, _captureWidth, _captureHeight, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

                    using (Image<Rgba32> frameImage = Image.LoadPixelData<Rgba32>(pixelData, _captureWidth, _captureHeight))
                    {
                        frameImage.Mutate(x => 
                        {
                            x.Flip(FlipMode.Vertical);
                            x.Resize(_targetWidth, _targetHeight);
                        });

                        int xPosition = (i % columns) * _targetWidth;
                        int yPosition = (i / columns) * _targetHeight;

                        spritesheet.Mutate(ctx => ctx.DrawImage(frameImage, new Point(xPosition, yPosition), 1f));
                    }
                }

                model.CurrentAnimationTime = originalAnimationTime;

                spritesheet.Save(outputPath);
            }
        }
        
        public void TakeAnimationSnapshots(ModelData model, Action renderFrame, int fps, string framePrefix = "Animation_")
        {
            AnimationData? currentAnimation = model.CurrentAnimationData;
            if (currentAnimation == null || currentAnimation.Duration <= 0f) return;

            float animationDuration = currentAnimation.Duration;
            int frames = (int)(animationDuration * fps);
            if (frames == 0) return;

            float timeStep = animationDuration / frames;
            float originalAnimationTime = model.CurrentAnimationTime;

            List<byte[]> rawPixelDataList = new List<byte[]>(frames);
            
            for (int i = 0; i < frames; i++)
            {
                model.CurrentAnimationTime = i * timeStep;

                renderFrame();

                byte[] pixelData = new byte[_captureWidth * _captureHeight * 4];
                GL.ReadPixels(0, 0, _captureWidth, _captureHeight, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
                rawPixelDataList.Add(pixelData);
            }
            
            model.CurrentAnimationTime = originalAnimationTime;

            Image<Rgba32>[] processedSnapshots = new Image<Rgba32>[frames];

            Parallel.For(0, frames, i => 
            {
                Image<Rgba32> snapshot = Image.LoadPixelData<Rgba32>(rawPixelDataList[i], _captureWidth, _captureHeight);
                snapshot.Mutate(x => 
                {
                    x.Flip(FlipMode.Vertical);
                    x.Resize(_targetWidth, _targetHeight);
                });
                processedSnapshots[i] = snapshot;
            });

            foreach (Image<Rgba32> snapshot in processedSnapshots)
            {
                _snapshots.Add(snapshot);
                FrameNames.Add(framePrefix);
            }
        }

        public void ProcessRawSnapshotsConcurrent(List<byte[]> rawPixelDataList, int captureWidth, int captureHeight, string framePrefix)
        {
            int frames = rawPixelDataList.Count;
            Image<Rgba32>[] processedSnapshots = new Image<Rgba32>[frames];

            Parallel.For(0, frames, i => 
            {
                Image<Rgba32> snapshot = Image.LoadPixelData<Rgba32>(rawPixelDataList[i], captureWidth, captureHeight);
                snapshot.Mutate(x => 
                {
                    x.Flip(FlipMode.Vertical);
                    x.Resize(_targetWidth, _targetHeight);
                });
                processedSnapshots[i] = snapshot;
            });

            for (int i = 0; i < frames; i++)
            {
                _snapshots.Add(processedSnapshots[i]);
                FrameNames.Add(framePrefix);
            }
        }
    }
}