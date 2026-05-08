using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace _3DSpritesheetConverter.SceneData
{
    class LightData
    {
        // Directional light
        public bool IsDirectionalEnabled { get; set; } = true;
        public Vector3 Position { get; set; } = new Vector3(1.5f, 1.0f, 2.0f);
        public Vector3 Colour { get; set; } = Vector3.One;
        public float Intensity { get; set; } = 1.0f;


        // Ambient light
        public bool IsAmbientEnabled { get; set; } = true;
        public Vector3 AmbientColour { get; set; } = Vector3.One;
        public float AmbientIntensity { get; set; } = 0.5f;
    }
}
