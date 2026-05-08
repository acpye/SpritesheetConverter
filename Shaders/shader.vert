#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTextureCoordinate;
layout(location = 3) in vec4 aJoints;
layout(location = 4) in vec4 aWeights;

const int MAX_BONES = 100;
uniform mat4 boneMatrices[MAX_BONES];   

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 TextureCoordinate;
out vec3 Normal;
out vec3 FragPosition;

void main()
{
    mat4 skinMatrix;
    float totalWeight = aWeights.x + aWeights.y + aWeights.z + aWeights.w;
    
    if (totalWeight > 0.0) 
    {
        skinMatrix = 
            aWeights.x * boneMatrices[int(aJoints.x)] +
            aWeights.y * boneMatrices[int(aJoints.y)] +
            aWeights.z * boneMatrices[int(aJoints.z)] +
            aWeights.w * boneMatrices[int(aJoints.w)];
    }
    else 
    {
        skinMatrix = mat4(1.0);
    }

    vec4 localSkinPosition = skinMatrix * vec4(aPosition, 1.0);
    gl_Position = projection * view * model * localSkinPosition;

    Normal = mat3(model) * mat3(skinMatrix) * aNormal; 
    TextureCoordinate = aTextureCoordinate;
    FragPosition = vec3(model * localSkinPosition);
}