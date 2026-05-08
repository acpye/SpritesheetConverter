#version 330 core

out vec4 FragmentColour;

in vec2 TextureCoordinate;

uniform sampler2D screenTexture;
uniform float pixelSize;
uniform vec2 resolution;

void main()
{
    if (pixelSize <= 1.0)
    {
        FragmentColour = texture(screenTexture, TextureCoordinate);
        return;
    }
    
    vec2 pixelatedUV = floor(TextureCoordinate * resolution / pixelSize) * pixelSize / resolution;
    FragmentColour = texture(screenTexture, pixelatedUV);
}