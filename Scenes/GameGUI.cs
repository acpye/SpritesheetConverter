using _3DSpritesheetConverter.Managers;
using _3DSpritesheetConverter.ObjectLoaders;
using _3DSpritesheetConverter.SceneData;
using ImGuiNET;
using NativeFileDialogs.Net;

namespace _3DSpritesheetConverter.Scenes
{
    class GameGUI
    {
        private readonly ModelLoader _modelLoader;
        private readonly SpritesheetManager _spritesheetManager;
        private readonly PostProcessingSettings _postProcessingSettings;
        private readonly Camera _camera;
        private readonly ModelTransform _modelTransform;
        private readonly LightData _light;
        private readonly Func<nint> _gameViewTextureProvider;
        private readonly Action _takeSnapshotCallback;
        private readonly Action<int> _generateRotationSpritesheetCallback;
        private readonly Action<List<RenderMesh>>? _loadModelMeshes;
        private readonly Action<int, bool, PerspectiveOptions>? _generateAnimationSpritesheetCallback;

        private enum SelectedObjectType { None, Model, Camera, Light }
        private SelectedObjectType _selectedObject = SelectedObjectType.None;

        private bool _isAnimationPlaying = false;
        private float _animationFps = 30f;
        private int _currentAnimationFrame = 0;
        private DateTime _lastFrameTime = DateTime.Now;

        private int _animationGenerationFps = 30;
        private bool _captureMultiPerspective = false;
        private int _rotationFrames = 30;

        private bool _captureFront = true;
        private bool _captureRight = true;
        private bool _captureBack = true;
        private bool _captureLeft = true;
        private bool _captureTop = true;
        private bool _captureBelow = true;


        private bool _showErrorWindow = false;
        private bool _showExportWindow = false;
        private string _errorMessage = string.Empty;

        public GameGUI
        (
            ModelLoader modelLoader,
            SpritesheetManager spritesheetManager,
            PostProcessingSettings postProcessingSettings,
            ModelTransform modelTransform,
            Camera camera,
            LightData light,
            Func<nint> gameViewTextureProvider,
            Action takeSnapshotCallback,
            Action<int> generateRotationSpritesheetCallback
        )
        {
            _modelLoader = modelLoader;
            _spritesheetManager = spritesheetManager;
            _postProcessingSettings = postProcessingSettings;
            _modelTransform = modelTransform;
            _camera = camera;
            _light = light;
            _gameViewTextureProvider = gameViewTextureProvider;
            _takeSnapshotCallback = takeSnapshotCallback;
            _generateRotationSpritesheetCallback = generateRotationSpritesheetCallback;
            _loadModelMeshes = null;
        }

        public GameGUI
        (
            ModelLoader modelLoader,
            SpritesheetManager spritesheetManager,
            PostProcessingSettings postProcessingSettings,
            ModelTransform modelTransform,
            Camera camera,
            LightData light,
            Func<nint> gameViewTextureProvider,
            Action takeSnapshotCallback,
            Action<int> generateRotationSpritesheetCallback,
            Action<int, bool, PerspectiveOptions>? generateAnimationSpritesheetCallback = null
        )
        {
            _modelLoader = modelLoader;
            _spritesheetManager = spritesheetManager;
            _postProcessingSettings = postProcessingSettings;
            _modelTransform = modelTransform;
            _camera = camera;
            _light = light;
            _gameViewTextureProvider = gameViewTextureProvider;
            _takeSnapshotCallback = takeSnapshotCallback;
            _generateRotationSpritesheetCallback = generateRotationSpritesheetCallback;
            _generateAnimationSpritesheetCallback = generateAnimationSpritesheetCallback;
            _loadModelMeshes = null;
        }

        public GameGUI
        (
            ModelLoader modelLoader,
            SpritesheetManager spritesheetManager,
            PostProcessingSettings postProcessingSettings,
            ModelTransform modelTransform,
            Camera camera,
            LightData light,
            Func<nint> gameViewTextureProvider,
            Action takeSnapshotCallback,
            Action<int> generateRotationSpritesheetCallback,
            Action<List<RenderMesh>> loadModelMeshes,
            Action<int, bool, PerspectiveOptions>? generateAnimationSpritesheetCallback = null
        )
        {
            _modelLoader = modelLoader;
            _spritesheetManager = spritesheetManager;
            _postProcessingSettings = postProcessingSettings;
            _modelTransform = modelTransform;
            _camera = camera;
            _light = light;
            _gameViewTextureProvider = gameViewTextureProvider;
            _takeSnapshotCallback = takeSnapshotCallback;
            _generateRotationSpritesheetCallback = generateRotationSpritesheetCallback;
            _loadModelMeshes = loadModelMeshes;
            _generateAnimationSpritesheetCallback = generateAnimationSpritesheetCallback;
        }

        public void Render()
        {
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            ImGui.NewFrame();
            BuildDockSpace();
            ImGui.EndFrame();
        }

        private void BuildDockSpace()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);

            ImGui.Begin("DockSpace_Window",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.MenuBar |
                ImGuiWindowFlags.NoBackground);

            ImGui.PopStyleVar(3);

            uint dockspaceId = ImGui.GetID("MyDockspace");
            ImGui.DockSpace(dockspaceId, System.Numerics.Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);

            BuildMenuBar();
            BuildHierarchyPanel();
            BuildInspectorPanel();
            BuildSpritesheetGeneratorPanel();
            BuildAnimationPreviewPanel();
            BuildGameWindow();

            ImGui.End();

            if (!_showErrorWindow)
            {
                if (_spritesheetManager.HasWarning)
                {
                    _errorMessage = _spritesheetManager.LastErrorMessage ?? "An unknown spritesheet error occurred.";
                    _showErrorWindow = true;
                }
                else if (_modelLoader.HasError)
                {
                    _errorMessage = _modelLoader.LastErrorMessage ?? "An unknown model loading error occurred.";
                    _showErrorWindow = true;
                }
            }

            if (!string.IsNullOrEmpty(_spritesheetManager.LastExportMessage) && !_showExportWindow)
            {
                _showExportWindow = true;
            }

            BuildErrorWindow();
            BuildExportWindow();
        }

        private void BuildAnimationPreviewPanel()
        {
            ImGui.Begin("Animation Preview");

            int framesCount = _spritesheetManager.SnapshotCount;
            if (framesCount == 0 || _spritesheetManager.TextureHandle == 0)
            {
                ImGui.TextWrapped("No frames available for animation.");
                ImGui.End();
                return;
            }

            if (ImGui.Button(_isAnimationPlaying ? "Pause" : "Play"))
            {
                _isAnimationPlaying = !_isAnimationPlaying;
            }
            ImGui.SameLine();
            ImGui.SliderFloat("FPS", ref _animationFps, 1f, 120, "%.1f");

            ImGui.Separator();

            if (_isAnimationPlaying)
            {
                TimeSpan elapsed = DateTime.Now - _lastFrameTime;
                float frameDuration = 1f / _animationFps;

                if (elapsed.TotalSeconds >= frameDuration)
                {
                    _currentAnimationFrame = (_currentAnimationFrame + 1) % framesCount;
                    _lastFrameTime = DateTime.Now;
                }
            }
            if (_currentAnimationFrame >= framesCount)
            {
                _currentAnimationFrame = 0;
            }

            int columns = (int)Math.Ceiling(Math.Sqrt(framesCount));
            int rows = (int)Math.Ceiling((double)framesCount / columns);

            int columnPosition = _currentAnimationFrame % columns;
            int rowPosition = _currentAnimationFrame / columns;

            float u0 = (float)columnPosition / columns;
            float v0 = (float)rowPosition / rows;
            float u1 = (float)(columnPosition + 1) / columns;
            float v1 = (float)(rowPosition + 1) / rows;

            System.Numerics.Vector2 uv0 = new System.Numerics.Vector2(u0, v0);
            System.Numerics.Vector2 uv1 = new System.Numerics.Vector2(u1, v1);

            System.Numerics.Vector2 availableSize = ImGui.GetContentRegionAvail();
            float renderSize = Math.Min(availableSize.X, availableSize.Y);

            ImGui.Image((nint)_spritesheetManager.TextureHandle, new System.Numerics.Vector2(renderSize, renderSize), uv0, uv1);
            ImGui.End();
        }

        private void BuildSpritesheetGeneratorPanel()
        {
            ImGui.Begin("Spritesheet Generator");

            ImGui.TextWrapped($"Snapshots: {_spritesheetManager.SnapshotCount}");

            if (ImGui.Button("Take Snapshot"))
            {
                _takeSnapshotCallback?.Invoke();
            }

            ImGui.SameLine();
            if (ImGui.Button("Clear All"))
            {
                _spritesheetManager.ClearSnapshots();
            }

            ImGui.SameLine();
            if (ImGui.Button("Export"))
            {
                _spritesheetManager.Export();
            }

            bool snapshotsExist = _spritesheetManager.SnapshotCount > 0;
            if (snapshotsExist) ImGui.BeginDisabled();

            ImGui.SameLine();
            int currentTargetDimensions = _spritesheetManager.TargetDimension;
            ImGui.SetNextItemWidth(160);
            if (ImGui.InputInt("Snapshot Dimensions", ref currentTargetDimensions))
            {
                currentTargetDimensions = Math.Clamp(currentTargetDimensions, 64, 1024);
                _spritesheetManager.TargetDimension = currentTargetDimensions;
            }

            if (snapshotsExist) ImGui.EndDisabled();

            ImGui.Separator();

            ImGui.SliderInt("Spritesheet Animation FPS", ref _animationGenerationFps, 1, 120);
            ImGui.Checkbox("Capture Multi-Perspective Spritesheet", ref _captureMultiPerspective);

            if (_captureMultiPerspective)
            {
                ImGui.Indent();
                ImGui.Checkbox("Front", ref _captureFront);
                ImGui.Checkbox("Left", ref _captureLeft);
                ImGui.Checkbox("Right", ref _captureRight);
                ImGui.Checkbox("Back", ref _captureBack);
                ImGui.Checkbox("Top", ref _captureTop);
                ImGui.Checkbox("Below", ref _captureBelow);
                ImGui.Unindent();
            }

            ImGui.Separator();

            if (ImGui.Button("Generate Spritesheet From Animation Frames"))
            {
                PerspectiveOptions options = new PerspectiveOptions
                {
                    Front = _captureFront,
                    Right = _captureRight,
                    Back = _captureBack,
                    Left = _captureLeft,
                    Top = _captureTop,
                    Below = _captureBelow
                };

                _generateAnimationSpritesheetCallback?.Invoke(_animationGenerationFps, _captureMultiPerspective, options);
            }

            ImGui.Separator();

            ImGui.SliderInt("Rotation Frames", ref _rotationFrames, 4, 120);
            if (ImGui.Button("Generate Spritesheet From Model Rotation"))
            {
                _generateRotationSpritesheetCallback?.Invoke(_rotationFrames);
            }

            ImGui.Separator();

            if (_spritesheetManager.SnapshotCount == 0)
            {
                ImGui.TextWrapped("No snapshots taken yet.");
                ImGui.TextWrapped("Position your model and click 'Take Snapshot' to capture frames for the spritesheet.");
            }
            else if (_spritesheetManager.TextureHandle != 0)
            {
                System.Numerics.Vector2 windowSize = ImGui.GetContentRegionAvail();
                ImGui.Image((nint)_spritesheetManager.TextureHandle, windowSize, new System.Numerics.Vector2(1, 0), new System.Numerics.Vector2(0, 1));
            }

            ImGui.End();
        }

        private void BuildErrorWindow()
        {
            if (!_showErrorWindow) return;

            bool open = _showErrorWindow;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 0), ImGuiCond.Appearing);
            if (ImGui.Begin("Error", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TextWrapped(_errorMessage);
                ImGui.PopStyleColor();
                ImGui.Separator();

                if (ImGui.Button("Dismiss"))
                {
                    open = false;
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy"))
                {
                    ImGui.SetClipboardText(_errorMessage);
                }
            }
            ImGui.End();

            if (!open)
            {
                _showErrorWindow = false;
                _spritesheetManager.ClearError();
                _modelLoader.ClearError();
                _errorMessage = string.Empty;
            }
        }

        private void BuildExportWindow()
        {
            if (!_showExportWindow) return;

            string message = _spritesheetManager.LastExportMessage ?? "Export completed.";

            bool open = _showExportWindow;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 0), ImGuiCond.Appearing);
            if (ImGui.Begin("Export Complete", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 0.75f, 0.0f, 1f));
                ImGui.TextWrapped(message);
                ImGui.PopStyleColor();
                ImGui.Separator();

                if (ImGui.Button("Dismiss"))
                {
                    _spritesheetManager.ClearExportMessage();
                    _showExportWindow = false;
                    open = false;
                }
            }
            ImGui.End();

            if (!open)
            {
                _showExportWindow = false;
                _spritesheetManager.ClearExportMessage();
            }
        }

        private void BuildGameWindow()
        {
            ImGui.Begin("Game Window");
            nint textureHandle = _gameViewTextureProvider.Invoke();
            if (textureHandle != nint.Zero)
            {
                System.Numerics.Vector2 windowSize = ImGui.GetContentRegionAvail();
                ImGui.Image(textureHandle, windowSize, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
            }
            ImGui.End();
        }

        private void BuildMenuBar()
        {
            if (!ImGui.BeginMenuBar()) return;

            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Import .glb file"))
                {
                    string? path;
                    Dictionary<string, string> filters = new Dictionary<string, string>
                    {
                        { "glTF Files", "glb,gltf" }
                    };
                    NfdStatus status = Nfd.OpenDialog(out path, filters, null);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        _modelLoader.LoadGLB(path);
                        _loadModelMeshes?.Invoke(_modelLoader.GetRenderMeshes());
                    }
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Export Spritesheet"))
                {
                    _spritesheetManager.Export();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Shaders"))
            {
                bool pixelationEnabled = _postProcessingSettings.PixelationEnabled;
                if (ImGui.MenuItem("Apply Pixelation Shader", "", pixelationEnabled))
                {
                    _postProcessingSettings.PixelationEnabled = !pixelationEnabled;
                }

                bool celShadingEnabled = _postProcessingSettings.CelShadingEnabled;
                if (ImGui.MenuItem("Apply Cel Shader", "", celShadingEnabled))
                {
                    _postProcessingSettings.CelShadingEnabled = !celShadingEnabled;
                }

                bool isOrthographic = _camera.IsOrthographic;
                if (ImGui.MenuItem("Orthographic Projection", "", isOrthographic))
                {
                    _camera.IsOrthographic = !isOrthographic;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        private void BuildHierarchyPanel()
        {
            ImGui.Begin("Hierarchy");

            if (ImGui.Selectable("Camera", _selectedObject == SelectedObjectType.Camera))
            {
                _selectedObject = SelectedObjectType.Camera;
            }

            if (ImGui.Selectable("Lighting", _selectedObject == SelectedObjectType.Light))
            {
                _selectedObject = SelectedObjectType.Light;
            }

            if (_modelLoader.CurrentModel != null)
            {
                string modelName = string.IsNullOrWhiteSpace(_modelLoader.CurrentModel.Name) ? "Model" : _modelLoader.CurrentModel.Name;
                if (ImGui.Selectable(modelName, _selectedObject == SelectedObjectType.Model))
                {
                    _selectedObject = SelectedObjectType.Model;
                }
            }

            ImGui.End();
        }

        private void BuildInspectorPanel()
        {
            ImGui.Begin("Inspector");

            switch (_selectedObject)
            {
                case SelectedObjectType.Camera:
                    DrawCameraInspector();
                    break;
                case SelectedObjectType.Light:
                    DrawLightInspector();
                    break;
                case SelectedObjectType.Model:
                    DrawModelInspector();
                    break;
                case SelectedObjectType.None:
                    ImGui.TextWrapped("Select an object in the Hierarchy to view details.");
                    break;
            }
            DrawPostProcessingControls();

            ImGui.End();
        }

        private void DrawModelInspector()
        {
            bool isEnabled = _modelTransform.IsEnabled;

            if (ImGui.Checkbox("##ModelEnabled", ref isEnabled)) _modelTransform.IsEnabled = isEnabled;
            ImGui.SameLine();
            
            ImGui.Text("Model Transform");
            ImGui.Separator();

            if (!isEnabled) ImGui.BeginDisabled();

            System.Numerics.Vector3 position = new System.Numerics.Vector3(_modelTransform.Position.X, _modelTransform.Position.Y, _modelTransform.Position.Z);
            if (ImGui.DragFloat3("Position", ref position, 0.01f))
            {
                _modelTransform.Position = new System.Numerics.Vector3(position.X, position.Y, position.Z);
            }

            System.Numerics.Vector3 rotation = new System.Numerics.Vector3(_modelTransform.Rotation.X, _modelTransform.Rotation.Y, _modelTransform.Rotation.Z);
            if (ImGui.DragFloat3("Rotation", ref rotation, 0.5f))
            {
                _modelTransform.Rotation = new System.Numerics.Vector3(rotation.X, rotation.Y, rotation.Z);
            }

            System.Numerics.Vector3 scale = new System.Numerics.Vector3(_modelTransform.Scale.X, _modelTransform.Scale.Y, _modelTransform.Scale.Z);
            if (ImGui.DragFloat3("Scale", ref scale, 0.01f, 0.001f, 100f))
            {
                _modelTransform.Scale = new System.Numerics.Vector3(scale.X, scale.Y, scale.Z);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Model Statistics");

            ModelData? model = _modelLoader.CurrentModel;
            if (model != null)
            {
                ImGui.Text($"Vertices: {model.Vertices.Count}");
                ImGui.Text($"Normals: {model.Normals.Count}");
                ImGui.Text($"Indices: {model.Indices.Count}");
                ImGui.Text($"Animations: {model.Animations.Count}");

                if (model.Animations.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.Text("Animations");

                    List<AnimationData> animations = model.Animations;
                    string[] animationNames = new string[animations.Count];

                    for (int i = 0; i < animations.Count; i++)
                    {
                        animationNames[i] = string.IsNullOrEmpty(animations[i].Name) ? $"Animation {i}" : animations[i].Name;
                    }

                    int currentAnimationIndex = model.CurrentAnimation;

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                    if (ImGui.Combo("##Animation", ref currentAnimationIndex, animationNames, animationNames.Length))
                    {
                        model.SetCurrentAnimation(currentAnimationIndex);
                        model.CurrentAnimationTime = 0f;
                    }
                    ImGui.SameLine();
                    ImGui.Text("Animation");

                    if (ImGui.Button(model.IsPlaying ? "Pause" : "Play"))
                    {
                        model.IsPlaying = !model.IsPlaying;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Prev")) _modelLoader.PreviousAnimation();
                    ImGui.SameLine();
                    if (ImGui.Button("Next")) _modelLoader.NextAnimation();

                    float animationTime = model.CurrentAnimationTime;
                    float animationDuration = _modelLoader.AnimationDuration;

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                    if (ImGui.SliderFloat("##Time", ref animationTime, 0f, animationDuration, "%.3f"))
                    {
                        model.CurrentAnimationTime = animationTime;
                    }
                    ImGui.SameLine();
                    ImGui.Text("Time");

                    float playbackSpeed = model.PlaybackSpeed;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                    if (ImGui.DragFloat("##Speed", ref playbackSpeed, 0.01f, -5f, 5f, "%.3f"))
                    {
                        model.PlaybackSpeed = playbackSpeed;
                    }
                    ImGui.SameLine();
                    ImGui.Text("Speed");

                    ImGui.Text($"Duration: {animationDuration:F3}s  Time: {animationTime:F3}s  Speed: {playbackSpeed:F2}x");
                }
            }

            if (!isEnabled) ImGui.EndDisabled();
        }

        private void DrawCameraInspector()
        {
            ImGui.SameLine();

            ImGui.Text("Camera Settings");
            ImGui.Separator();

            System.Numerics.Vector3 cameraPosition = new System.Numerics.Vector3(_camera.Position.X, _camera.Position.Y, _camera.Position.Z);
            if (ImGui.DragFloat3("Position", ref cameraPosition, 0.01f))
            {
                _camera.Position = new OpenTK.Mathematics.Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
            }

            System.Numerics.Vector3 targetPosition = new System.Numerics.Vector3(_camera.Target.X, _camera.Target.Y, _camera.Target.Z);
            if (ImGui.DragFloat3("Target", ref targetPosition, 0.01f))
            {
                _camera.Target = new OpenTK.Mathematics.Vector3(targetPosition.X, targetPosition.Y, targetPosition.Z);
            }

            bool isOrthographic = _camera.IsOrthographic;

            ImGui.Spacing();
            ImGui.Text($"Projection: {(isOrthographic ? "Orthographic" : "Perspective")}");
            ImGui.Separator();

            if (!isOrthographic)
            {
                float fov = _camera.Fov;
                if (ImGui.DragFloat("Perspective FOV", ref fov, 0.1f, 1f, 180f))
                {
                    _camera.Fov = fov;
                }
            }
            else
            {
                float orthographicSize = _camera.OrthographicSize;
                ImGui.Text($"Orthographic Size: {orthographicSize:F2}");

                float orthographicSizeSlider = orthographicSize;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.6f);
                if (ImGui.SliderFloat("Orthographic Size##Slider", ref orthographicSizeSlider, 0.1f, 100f, "%.2f"))
                {
                    double fovRadius = _camera.Fov * Math.PI / 180.0;
                    double desiredDistance = orthographicSizeSlider / (2.0 * Math.Tan(fovRadius / 2.0));

                    _camera.Position = _camera.Target - _camera.Front * (float)desiredDistance;
                }

                ImGui.Spacing();
                ImGui.TextWrapped("Use the slider to change orthographic view. Orthographic size is camera-managed and adjusted by moving the camera along its view axis.");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Snapshot Settings");

            float orthographicSizeDisplay = _camera.OrthographicSize;
            ImGui.Text($"Orthographic Size: {orthographicSizeDisplay:F2}");

            ImGui.Spacing();
            ImGui.TextWrapped("Control camera rotation using Middle Mouse Button.");
        }

        private void DrawLightInspector()
        {
            ImGui.Text("Light Settings");
            ImGui.Separator();

            bool isDirectionalEnabled = _light.IsDirectionalEnabled;
            if (ImGui.Checkbox("##DirectionalEnabled", ref isDirectionalEnabled)) _light.IsDirectionalEnabled = isDirectionalEnabled;
            ImGui.SameLine();
            ImGui.Text("Directional Light");

            if (!isDirectionalEnabled) ImGui.BeginDisabled();

            System.Numerics.Vector3 lightPosition = new System.Numerics.Vector3(_light.Position.X, _light.Position.Y, _light.Position.Z);
            if (ImGui.DragFloat3("Position", ref lightPosition, 0.01f))
            {
                _light.Position = new OpenTK.Mathematics.Vector3(lightPosition.X, lightPosition.Y, lightPosition.Z);
            }

            System.Numerics.Vector3 colourVector = new System.Numerics.Vector3(_light.Colour.X, _light.Colour.Y, _light.Colour.Z);
            if (ImGui.ColorEdit3("Colour", ref colourVector))
            {
                _light.Colour = new OpenTK.Mathematics.Vector3(colourVector.X, colourVector.Y, colourVector.Z);
            }

            float lightIntensity = _light.Intensity;
            if (ImGui.DragFloat("Intensity", ref lightIntensity, 0.01f, 0f, 10f))
            {
                _light.Intensity = lightIntensity;
            }

            if (!isDirectionalEnabled) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();

            bool isAmbientEnabled = _light.IsAmbientEnabled;
            if (ImGui.Checkbox("##AmbientEnabled", ref isAmbientEnabled)) _light.IsAmbientEnabled = isAmbientEnabled;
            ImGui.SameLine();
            ImGui.Text("Ambient Light");

            if (!isAmbientEnabled) ImGui.BeginDisabled();

            System.Numerics.Vector3 ambientColourVector = new System.Numerics.Vector3(_light.AmbientColour.X, _light.AmbientColour.Y, _light.AmbientColour.Z);
            if (ImGui.ColorEdit3("Ambient Colour", ref ambientColourVector))
            {
                _light.AmbientColour = new OpenTK.Mathematics.Vector3(ambientColourVector.X, ambientColourVector.Y, ambientColourVector.Z);
            }

            float ambientIntensity = _light.AmbientIntensity;
            if (ImGui.DragFloat("Ambient Intensity", ref ambientIntensity, 0.01f, 0f, 10f))
            {
                _light.AmbientIntensity = ambientIntensity;
            }

            if (!isAmbientEnabled) ImGui.EndDisabled();
        }

        private void DrawPostProcessingControls()
        {
            if (!_postProcessingSettings.AnyEffectEnabled) return;

            ImGui.Separator();
            ImGui.Text("Post-Processing Settings");

            // Cel shader controls
            if (_postProcessingSettings.CelShadingEnabled)
            {
                ImGui.Spacing();
                ImGui.Text("Cel Shading");

                int levels = _postProcessingSettings.CelShadingLevels;
                if (ImGui.SliderInt("Colour Levels", ref levels, 2, 16))
                {
                    _postProcessingSettings.CelShadingLevels = levels;
                }

                float edgeThreshold = _postProcessingSettings.CelShadingEdgeThreshold;
                if (ImGui.SliderFloat("Edge Threshold", ref edgeThreshold, 0f, 1f))
                {
                    _postProcessingSettings.CelShadingEdgeThreshold = edgeThreshold;
                }
            }

            // Pixelation shader controls
            if (_postProcessingSettings.PixelationEnabled)
            {
                ImGui.Spacing();
                ImGui.Text("Pixelation");

                float pixelSize = _postProcessingSettings.PixelSize;
                if (ImGui.SliderFloat("Pixel Size", ref pixelSize, 1f, 32f))
                {
                    _postProcessingSettings.PixelSize = pixelSize;
                }
            }
        }
    }
}
