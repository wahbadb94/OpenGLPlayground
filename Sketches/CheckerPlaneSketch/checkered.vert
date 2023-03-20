#version 460 core //Using GLSL version 4.6 

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 uv;

out vec2 fragUV;
// out vec3 fragNormal;

layout (location = 0) uniform mat4 u_model;
layout (location = 1) uniform mat4 u_view;
layout (location = 2) uniform mat4 u_projection;
layout (location = 3) uniform mat4 u_normalMatrix;

void main()
{
    // calc position of fragment in NDC
    gl_Position = u_projection * u_view * u_model * vec4(vPos, 1.0);
    
    // the position and normal of the fragment in "world" space
    // fragPos = vec3(u_model * vec4(vPos, 1.0));
    fragUV = uv;

    // fragNormal = mat3(u_normalMatrix) * vNormal;
}
