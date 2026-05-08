using _3DSpritesheetConverter.Managers;
using _3DSpritesheetConverter.ObjectLoaders;
using _3DSpritesheetConverter.Shaders;
using _3DSpritesheetConverter.Scenes;

namespace _3DSpritesheetConverter.SceneData
{
    class SceneContext
    {
        public Camera Camera { get; }
        public LightData Light { get; }
        public ModelTransform ModelTransform { get; }
        public ModelLoader ModelLoader { get; }
        public List<RenderMesh> RenderMeshes { get; set; } = new List<RenderMesh>();

        public SceneContext(Camera camera, LightData light, ModelTransform modelTransform, ModelLoader modelLoader)
        {
            Camera = camera;
            Light = light;
            ModelTransform = modelTransform;
            ModelLoader = modelLoader;
        }
    }
}