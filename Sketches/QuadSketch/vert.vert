#version 330 core //Using GLSL version 3.3

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vColor;
layout (location = 2) in vec2 uv;

out vec2 frag_uv;

uniform mat4 u_MVP;

void main()
{
    // apply the mvp matrix
    gl_Position = u_MVP * vec4(vPos.x, vPos.y, vPos.z, 1.0);
    
    // setting uv in the vert makes sure the coords get interpolated for each fragment
    frag_uv = uv;
}
