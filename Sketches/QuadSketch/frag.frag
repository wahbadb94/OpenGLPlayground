#version 330 core // using GLSL version 3.3


uniform vec4 u_Color;

in vec3 vertexColor;
out vec4 FragColor;

void main()
{
    FragColor = vec4(vertexColor.r, u_Color.g, vertexColor.b, 1.0);
}
