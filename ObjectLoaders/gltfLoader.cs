using System.Numerics;
using System.Collections.Generic;
using _3DSpritesheetConverter.SceneData;
using SharpGLTF.Schema2;

namespace _3DSpritesheetConverter.ObjectLoaders
{
    class gltfLoader
    {
        private ModelRoot? _model;

        public List<MeshData> LoadModel(string filePath)
        {
            List<MeshData> meshesData = new List<MeshData>();
            _model = ModelRoot.Load(filePath);

            if (_model.DefaultScene != null)
            {
                foreach (Node node in _model.DefaultScene.VisualChildren)
                {
                    ProcessNode(node, meshesData);
                }
            }

            return meshesData;
        }

        private void ProcessNode(Node node, List<MeshData> meshesData)
        {
            if (node.Mesh != null)
            {
                foreach (MeshPrimitive primitive in node.Mesh.Primitives)
                {
                    MeshData meshData = new MeshData();
                    meshData.NodeIndex = node.LogicalIndex;

                    if (primitive.Material != null)
                    {
                        MaterialChannel? channel = primitive.Material.FindChannel("BaseColor");
                        if (channel.HasValue && channel.Value.Texture != null)
                        {
                            Image image = channel.Value.Texture.PrimaryImage;
                            if (image != null)
                            {
                                meshData.TextureBytes = image.Content.Content.ToArray();
                            }
                        }
                    }

                    IReadOnlyList<Vector3>? positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
                    IReadOnlyList<Vector3>? normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                    IReadOnlyList<Vector2>? texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                    IReadOnlyList<Vector4>? joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                    IReadOnlyList<Vector4>? weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
                    
                    meshData.HasBones = joints != null;

                    if (positions != null)
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            meshData.Vertices.Add(new Vertex
                            {
                                Position = positions[i],
                                Normal = normals != null && normals.Count > i ? normals[i] : Vector3.Zero,
                                TextureCoordinate = texCoords != null && texCoords.Count > i 
                                    ? new Vector2(texCoords[i].X, 1.0f - texCoords[i].Y) : Vector2.Zero,
                                Joints = joints != null && joints.Count > i ? joints[i] : Vector4.Zero,
                                Weights = weights != null && weights.Count > i ? weights[i] : Vector4.Zero
                            });
                        }
                    }

                    IReadOnlyList<uint>? indices = primitive.GetIndexAccessor()?.AsIndicesArray();
                    if (indices != null)
                    {
                        for (int i = 0; i < indices.Count; i++)
                        {
                            meshData.Indices.Add(indices[i]);
                        }
                    }

                    meshesData.Add(meshData);
                }
            }

            foreach (Node child in node.VisualChildren)
            {
                ProcessNode(child, meshesData);
            }
        }

        public Matrix4x4[] GetAnimatedBoneMatrices(int animationIndex, float time)
        {
            if (_model == null || _model.LogicalAnimations.Count <= animationIndex || _model.LogicalSkins.Count == 0)
                return Array.Empty<Matrix4x4>();


            Skin skin = _model.LogicalSkins[0];
            Matrix4x4[] matrices = new Matrix4x4[skin.JointsCount];
            Animation animation = _model.LogicalAnimations[animationIndex];

            Matrix4x4 rootTransform = skin.GetJoint(0).Joint.GetWorldMatrix(animation, time);
            Vector3 rootTranslation = rootTransform.Translation;

            Vector3 offset = new Vector3(-rootTranslation.X, 0, -rootTranslation.Z);
            Matrix4x4 offsetMatrix = Matrix4x4.CreateTranslation(offset);

            for (int i = 0; i < skin.JointsCount; i++)
            {
                Node jointNode = skin.GetJoint(i).Joint;
                Matrix4x4 inverseBindMatrix = skin.GetJoint(i).InverseBindMatrix;

                Matrix4x4 worldTransform = jointNode.GetWorldMatrix(animation, time);

                matrices[i] = inverseBindMatrix * worldTransform * offsetMatrix;
            }

            return matrices;
        }

        public float GetAnimationDuration(int animationIndex)
        {
            if (_model == null || _model.LogicalAnimations.Count <= animationIndex)
                return 0f;

            return _model.LogicalAnimations[animationIndex].Duration;
        }

        public List<AnimationData> GetAnimations()
        {
            List<AnimationData> animations = new List<AnimationData>();

            if (_model == null) return animations;

            for (int i = 0; i < _model.LogicalAnimations.Count; i++)
            {
                Animation animation = _model.LogicalAnimations[i];
                animations.Add(new AnimationData
                {
                    Name = string.IsNullOrEmpty(animation.Name) ? $"Animation {i}" : animation.Name,
                    Duration = animation.Duration
                });
            }
            return animations;
        }

        public string GetModelName(string filePath)
        {
            if (_model != null)
            {
                if (_model.DefaultScene != null && !string.IsNullOrWhiteSpace(_model.DefaultScene.Name))
                {
                    return _model.DefaultScene.Name;
                }
            }
            return System.IO.Path.GetFileNameWithoutExtension(filePath);
        }

        public byte[]? GetBaseColorTextureBytes()
        {
            if (_model == null) return null;

            foreach (Material material in _model.LogicalMaterials)
            {
                MaterialChannel? channel = material.FindChannel("BaseColor");
                if (channel.HasValue && channel.Value.Texture != null)
                {
                    Image image = channel.Value.Texture.PrimaryImage;
                    if (image != null)
                    {
                        return image.Content.Content.ToArray();
                    }
                }
            }

            return null;
        }

        public Matrix4x4[] GetAnimatedNodeMatrices(int animationIndex, float time)
        {
            if (_model == null) return Array.Empty<Matrix4x4>();

            Matrix4x4[] matrices = new Matrix4x4[_model.LogicalNodes.Count];
            Animation? animation = _model.LogicalAnimations.Count > animationIndex ? _model.LogicalAnimations[animationIndex] : null;

            for (int i = 0; i < _model.LogicalNodes.Count; i++)
            {
                matrices[i] = _model.LogicalNodes[i].GetWorldMatrix(animation, time);
            }

            return matrices;
        }
    }
}