#version 330 core //Using GLSL version 3.3

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;

out vec3 fragPos;
out vec3 fragNormal;

uniform mat4 u_model;
uniform mat4 u_view;
uniform mat4 u_projection;
uniform mat4 u_normalMatrix;

void main()
{
    // calc position of fragment in NDC
    gl_Position = u_projection * u_view * u_model * vec4(vPos, 1.0);
    
    // the position and normal of the fragment in "world" space
    fragPos = vec3(u_model * vec4(vPos, 1.0));
    fragNormal = mat3(u_normalMatrix) * vNormal;
}
