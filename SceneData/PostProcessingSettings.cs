namespace _3DSpritesheetConverter.SceneData
{
    class PostProcessingSettings
    {
        public bool PixelationEnabled { get; set; }
        public float PixelSize { get; set; } = 4f;
        public bool CelShadingEnabled { get; set; }
        public int CelShadingLevels { get; set; } = 4;
        public float CelShadingEdgeThreshold { get; set; } = 0.2f;

        public bool AnyEffectEnabled => PixelationEnabled || CelShadingEnabled;
    }
}