using _3DSpritesheetConverter.Managers;
using _3DSpritesheetConverter.ObjectLoaders;
using _3DSpritesheetConverter.SceneData;
using _3DSpritesheetConverter.Shaders;
using ImGuiNET;
using OpenTK.DearImGui;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace _3DSpritesheetConverter.Scenes
{
    public class Game : GameWindow
    {
        private SceneContext _sceneContext;
        private Shader _shader;
        private InputHandler _inputHandler;
        private GameGUI _gameGUI;
        private ImGuiController _controller;

        private FramebufferManager _framebufferManager;
        private ModelBufferManager _modelBufferManager;
        private PostProcessingManager _postProcessingManager;
        private TextureManager _textureManager;
        private SpritesheetManager _spritesheetManager;
        private SceneRenderer _sceneRenderer;
        private ModelLoader _modelLoader;
        private PostProcessingSettings _postProcessingSettings;
        private SkyboxRenderer _skyboxRenderer;

        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Title = "3D Spritesheet Converter";
            Size = new Vector2i(1400, 800);
            CenterWindow();
            WindowState = WindowState.Maximized;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.SteelBlue);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _controller = new ImGuiController(this);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            Camera camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);
            _inputHandler = new InputHandler(this, camera);
            CursorState = CursorState.Normal;

            ModelTransform modelTransform = new ModelTransform { Position = new System.Numerics.Vector3(0f, -0.9f, 0f), Rotation = System.Numerics.Vector3.Zero, Scale = System.Numerics.Vector3.One };
            LightData light = new LightData { Position = new Vector3(1.5f, 1.0f, 2.0f), Colour = OpenTK.Mathematics.Vector3.One, Intensity = 1.0f };

            _framebufferManager = new FramebufferManager();
            _modelBufferManager = new ModelBufferManager();
            _postProcessingManager = new PostProcessingManager();
            _spritesheetManager = new SpritesheetManager();
            _textureManager = new TextureManager();
            _sceneRenderer = new SceneRenderer(_shader, _textureManager);
            _modelLoader = new ModelLoader(_modelBufferManager, _textureManager, _shader);
            _postProcessingSettings = new PostProcessingSettings();

            _sceneContext = new SceneContext(camera, light, modelTransform, _modelLoader);

            _gameGUI = new GameGUI
            (
                _sceneContext.ModelLoader,
                _spritesheetManager,
                _postProcessingSettings,
                _sceneContext.ModelTransform,
                _sceneContext.Camera,
                _sceneContext.Light,
                GetDisplayTextureHandle,
                TakeSnapshot,
                GenerateRotationSpritesheet,
                LoadModelMeshes,
                GenerateAnimationSpritesheet
            );

            _skyboxRenderer = new SkyboxRenderer("Scenes/skybox.jpg", segments: 64);

            _framebufferManager.Setup(Size.X, Size.Y);
            _postProcessingManager.Initialize();
            _spritesheetManager.Initialize(Size.X, Size.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            
            _inputHandler.HandleInput(args);
            _sceneContext.ModelLoader.Update((float)args.Time);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _inputHandler.HandleMouseWheel(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Keys.Space)
            {
                _sceneContext.ModelLoader.ToggleAnimation();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            _postProcessingManager.CelShadingEnabled = _postProcessingSettings.CelShadingEnabled;
            _postProcessingManager.CelShadingLevels = _postProcessingSettings.CelShadingLevels;
            _postProcessingManager.CelShadingEdgeThreshold = _postProcessingSettings.CelShadingEdgeThreshold;
            _postProcessingManager.PixelationEnabled = _postProcessingSettings.PixelationEnabled;
            _postProcessingManager.PixelSize = _postProcessingSettings.PixelSize;

            _sceneRenderer.RenderWithPostProcess
            (
                _framebufferManager,
                _postProcessingManager,
                () => 
                {
                    _skyboxRenderer.Render(_sceneContext.Camera.GetViewMatrix(), _sceneContext.Camera.GetPerspectiveProjectionMatrix());
                    
                    _sceneRenderer.RenderScene(_sceneContext);
                },
                Size.X,
                Size.Y,
                useTransparentBackground: false
            );

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.ClearColor(new Color4(45, 55, 60, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _controller.Update((float)args.Time);
            _gameGUI.Render();

            _controller.Render();

            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            int width = Math.Max(1, e.Size.X);
            int height = Math.Max(1, e.Size.Y);

            if (_sceneContext?.Camera != null)
            {
                _sceneContext.Camera.AspectRatio = width / (float)height;
            }

            if (_framebufferManager != null)
            {
                _framebufferManager.Setup(width, height);
            }
            GL.Viewport(0, 0, width, height);
        }

        private nint GetDisplayTextureHandle() => (nint)_postProcessingManager.GetDisplayTexture(_framebufferManager);

        private void CaptureSpritesheetAction(Action<Action> captureLogic, bool updateModel = false)
        {
            bool wasOrthographic = _sceneContext.Camera.IsOrthographic;
            _sceneContext.Camera.IsOrthographic = true;

            Action renderAction = () =>
            {
                if (updateModel) _sceneContext.ModelLoader.Update(0); 

                _sceneRenderer.RenderWithPostProcess
                (
                    _framebufferManager, _postProcessingManager, () => _sceneRenderer.RenderScene(_sceneContext),
                    _framebufferManager.Width, _framebufferManager.Height, useTransparentBackground: true
                );

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _postProcessingManager.GetFinalFBO(_framebufferManager));
            };

            try
            {
                captureLogic(renderAction);
            }
            finally
            {
                _sceneContext.Camera.IsOrthographic = wasOrthographic;
                GL.Viewport(0, 0, Size.X, Size.Y);
            }
        }

        private void TakeSnapshot()
        {
            CaptureSpritesheetAction(renderAction => 
                _spritesheetManager.TakeSnapshot(renderAction, Size.X, Size.Y));
        }

        private void GenerateRotationSpritesheet(int frames)
        {
            CaptureSpritesheetAction(renderAction => 
                _spritesheetManager.GenerateRotationSpritesheet(_sceneContext.ModelTransform, renderAction, Size.X, Size.Y, frames));
        }

        private void GenerateAnimationSpritesheet(int fps, bool multiPerspective, PerspectiveOptions options)
        {
            if (_sceneContext.ModelLoader.CurrentModel == null) return;

            CaptureSpritesheetAction(renderAction => 
                _spritesheetManager.GenerateAnimationSpritesheet
                (
                    _sceneContext.ModelLoader.CurrentModel, 
                    _sceneContext.ModelTransform, 
                    renderAction, 
                    Size.X, 
                    Size.Y, 
                    fps, 
                    multiPerspective,
                    options
                ),  updateModel: true);
        }

        private void LoadModelMeshes(List<RenderMesh> meshes) => _sceneContext.RenderMeshes = meshes;

        protected override void OnUnload()
        {
            _controller?.Dispose();

            if (_sceneContext?.RenderMeshes != null)
            {
                foreach (RenderMesh mesh in _sceneContext.RenderMeshes)
                {
                    mesh.Dispose();
                }
                _sceneContext.RenderMeshes.Clear();
            }

            _modelBufferManager?.Dispose();
            _framebufferManager?.Dispose();
            _spritesheetManager?.Dispose();
            _textureManager?.Dispose();
            _shader?.Dispose();

            base.OnUnload();
        }
    }
}