using _3DSpritesheetConverter.Managers;
using _3DSpritesheetConverter.Shaders;
using _3DSpritesheetConverter.SceneData;
using System.Numerics;

namespace _3DSpritesheetConverter.ObjectLoaders
{
    class ModelLoader
    {
        private readonly TextureManager _textureManager;
        private readonly gltfLoader _gltfLoader;

        public List<MeshData> LoadedMeshes { get; private set; }
        public Matrix4x4[] BoneMatrices { get; private set; }
        public Matrix4x4[] NodeMatrices { get; private set; } = Array.Empty<Matrix4x4>();
        public ModelData? CurrentModel { get; private set; }
        public string? LastErrorMessage { get; private set; }
        public bool HasError => !string.IsNullOrEmpty(LastErrorMessage);
        public void ClearError() => LastErrorMessage = null;

        public ModelLoader(ModelBufferManager bufferManager, TextureManager textureManager, Shader shader)
        {
            _textureManager = textureManager;
            _gltfLoader = new gltfLoader();
        }
        
        public float AnimationDuration
        {
            get
            {
                if (CurrentModel?.CurrentAnimationData == null) return 0f;
                return CurrentModel.CurrentAnimationData.Duration;
            }
        }

        public ModelLoader(TextureManager textureManager, Shader shader)
        {
            _textureManager = textureManager;
            _gltfLoader = new gltfLoader();
            LoadedMeshes = new List<MeshData>();
            BoneMatrices = Array.Empty<Matrix4x4>();
        }

        public void LoadGLB(string path)
        {
            ClearError();
            try
            {
                LoadedMeshes = _gltfLoader.LoadModel(path);
                if (LoadedMeshes == null || LoadedMeshes.Count == 0)
                {
                    LastErrorMessage = "The selected model file could not be loaded or contains no valid mesh data.";
                    CurrentModel = null;
                    LoadedMeshes = new List<MeshData>();
                    return;
                }

                CurrentModel = new ModelData();
                CurrentModel.Name = _gltfLoader.GetModelName(path);

                Dictionary<byte[], int> loadedTextures = new Dictionary<byte[], int>();

                foreach (MeshData mesh in LoadedMeshes)
                {
                    if (mesh.TextureBytes != null && mesh.TextureBytes.Length > 0)
                    {
                        if (!loadedTextures.TryGetValue(mesh.TextureBytes, out int handle))
                        {
                            handle = _textureManager.LoadTextureFromBytes(mesh.TextureBytes);
                            loadedTextures[mesh.TextureBytes] = handle;
                        }
                        mesh.TextureHandle = handle;
                    }

                    int vertexOffset = CurrentModel.Vertices.Count;
                    CurrentModel.Vertices.AddRange(mesh.Vertices.Select(v => v.Position));
                    CurrentModel.Normals.AddRange(mesh.Vertices.Select(v => v.Normal));
                    CurrentModel.TextureCoordinates.AddRange(mesh.Vertices.Select(v => v.TextureCoordinate));
                    CurrentModel.Joints.AddRange(mesh.Vertices.Select(v => v.Joints));
                    CurrentModel.Weights.AddRange(mesh.Vertices.Select(v => v.Weights));
                    CurrentModel.Indices.AddRange(mesh.Indices.Select(i => (int)(i + vertexOffset)));
                }

                List<AnimationData> animations = _gltfLoader.GetAnimations();
                CurrentModel.Animations.AddRange(animations);

                if (CurrentModel != null)
                {
                    CurrentModel.SetCurrentAnimation(0);
                }

                UpdateBoneMatrices();
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Failed to load the model. It may be malformed or corrupted.\n\nDetailed Error:\n{ex.Message}";
                CurrentModel = null;
                LoadedMeshes = new List<MeshData>();
            }
        }

        public List<RenderMesh> GetRenderMeshes()
        {
            List<RenderMesh> renderMeshes = new List<RenderMesh>();
            foreach (MeshData mesh in LoadedMeshes)
            {
                renderMeshes.Add(RenderMesh.Create(mesh));
            }
            return renderMeshes;
        }

        public void Update(float deltaTime)
        {
            if (CurrentModel == null) return;

            CurrentModel.Update(deltaTime);
            UpdateBoneMatrices();
        }

        public void NextAnimation()
        {
            if (CurrentModel == null) return;

            int nextIndex = (CurrentModel.CurrentAnimation + 1) % CurrentModel.Animations.Count;
            SetAnimation(nextIndex);
        }

        public void PreviousAnimation()
        {
            if (CurrentModel == null) return;

            int prevIndex = CurrentModel.CurrentAnimation - 1;
            if (prevIndex < 0) prevIndex = CurrentModel.Animations.Count - 1;
            SetAnimation(prevIndex);
        }

        private void SetAnimation(int index)
        {
            CurrentModel?.SetCurrentAnimation(index);
            UpdateBoneMatrices();
        }

        public void ToggleAnimation()
        {
            if (CurrentModel != null)
            {
                CurrentModel.IsPlaying = !CurrentModel.IsPlaying;
            }
        }


        private void UpdateBoneMatrices()
        {
            if (CurrentModel == null) return;

            float duration = AnimationDuration;
            if (duration > 0f)
            {
                BoneMatrices = _gltfLoader.GetAnimatedBoneMatrices(CurrentModel.CurrentAnimation, CurrentModel.CurrentAnimationTime);
                NodeMatrices = _gltfLoader.GetAnimatedNodeMatrices(CurrentModel.CurrentAnimation, CurrentModel.CurrentAnimationTime);
            }
            else
            {
                BoneMatrices = Array.Empty<Matrix4x4>();
                NodeMatrices = _gltfLoader.GetAnimatedNodeMatrices(0, 0);
            }
        }
    }
}
