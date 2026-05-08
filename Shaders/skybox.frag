#version 330 core

in vec2 textureCoordinate;

uniform sampler2D skybox;
uniform vec4 colour;

out vec4 FragmentColour;

void main()
{
    vec4 textureColour = texture(skybox, textureCoordinate) * colour;

    FragmentColour = vec4(textureColour.rgb, 0.99);
}