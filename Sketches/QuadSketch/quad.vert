#version 460 core // Using GLSL version 4.6

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vOffset;

out vec3 fragPos;
out vec3 fragNormal;
out vec3 objectColor;

layout (location = 0) uniform mat4 u_model;
layout (location = 1) uniform mat4 u_view;
layout (location = 2) uniform mat4 u_projection;
layout (location = 3) uniform mat4 u_normalMatrix;

void main()
{
    mat4 instanceTranslation = mat4(
        vec4(1.0, 0.0, 0.0, 0.0),   // col 1
        vec4(0.0, 1.0, 0.0, 0.0),   // col 2
        vec4(0.0, 0.0, 1.0, 0.0),   // col 3
        vec4(vOffset      , 1.0)    // translation col 
    );

    // calc position of fragment in NDC
    gl_Position = u_projection * u_view * instanceTranslation * u_model * vec4(vPos, 1.0);
    
    // the position and normal of the fragment in "world" space
    fragPos = vec3(instanceTranslation * u_model * vec4(vPos, 1.0));
    fragNormal = mat3(u_normalMatrix) * vNormal;
    objectColor = vPos;
}
