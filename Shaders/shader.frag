#version 330 core

out vec4 FragmentColour;

in vec3 Normal;
in vec3 FragPosition;
in vec2 TextureCoordinate;

uniform vec3 objectColour;
uniform vec3 lightColour;
uniform vec3 lightPosition;
uniform vec3 viewPosition;
uniform vec3 ambientLight;

uniform sampler2D diffuseTexture;
uniform bool useTexture;

void main()
{
    vec3 baseColour = useTexture ? texture(diffuseTexture, TextureCoordinate).rgb : objectColour;
    float alpha = useTexture ? texture(diffuseTexture, TextureCoordinate).a : 1.0;

    vec3 ambient = ambientLight;

    vec3 norm = normalize(Normal);
    vec3 lightDirection = normalize(lightPosition - FragPosition);
    
    float diff = max(dot(norm, lightDirection), 0.0);
    vec3 diffuse = diff * lightColour;

    float specularStrength = 0.5;
    vec3 viewDirection = normalize(viewPosition - FragPosition);
    vec3 reflectDirection = reflect(-lightDirection, norm);
    float spec = pow(max(dot(viewDirection, reflectDirection), 0.0), 32);
    vec3 specular = specularStrength * spec * lightColour;

    vec3 result = (ambient + diffuse + specular) * baseColour;
    FragmentColour = vec4(result, alpha);
}