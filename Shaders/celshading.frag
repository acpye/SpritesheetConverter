#version 330 core

out vec4 FragmentColour;

in vec2 TextureCoordinate;

uniform sampler2D screenTexture;
uniform int levels;
uniform float edgeThreshold;
uniform vec2 resolution;

void main()
{
    vec4 textureColour = texture(screenTexture, TextureCoordinate);
    
    if (textureColour.a < 0.01)
    {
        FragmentColour = textureColour;
        return;
    }
    
    if (textureColour.a > 0.985 && textureColour.a < 0.995)
    {
        FragmentColour = vec4(textureColour.rgb, 1.0);
        return;
    }
    
    vec3 colour = textureColour.rgb;
    float levelFloat = float(levels);
    colour = floor(colour * levelFloat) / (levelFloat - 1.0);
    
    vec2 texelSize = 1.0 / resolution;
    
    float topLeft = dot(texture(screenTexture, TextureCoordinate + vec2(-texelSize.x, texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    float top = dot(texture(screenTexture, TextureCoordinate + vec2(0.0, texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    float topRight = dot(texture(screenTexture, TextureCoordinate + vec2(texelSize.x, texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    float left = dot(texture(screenTexture, TextureCoordinate + vec2(-texelSize.x, 0.0)).rgb, vec3(0.299, 0.587, 0.114));
    float right = dot(texture(screenTexture, TextureCoordinate + vec2(texelSize.x, 0.0)).rgb, vec3(0.299, 0.587, 0.114));
    float bottomLeft = dot(texture(screenTexture, TextureCoordinate + vec2(-texelSize.x, -texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    float bottom = dot(texture(screenTexture, TextureCoordinate + vec2(0.0, -texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    float bottomRight = dot(texture(screenTexture, TextureCoordinate + vec2(texelSize.x, -texelSize.y)).rgb, vec3(0.299, 0.587, 0.114));
    
    float sobelX = topLeft + 2.0 * left + bottomLeft - topRight - 2.0 * right - bottomRight;
    float sobelY = topLeft + 2.0 * top + topRight - bottomLeft - 2.0 * bottom - bottomRight;
    float edge = sqrt(sobelX * sobelX + sobelY * sobelY);
    
    if (edge > edgeThreshold)
    {
        colour = vec3(0.0);
    }
    
    FragmentColour = vec4(colour, textureColour.a);
}