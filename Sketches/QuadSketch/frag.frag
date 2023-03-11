#version 330 core // using GLSL version 3.3

in vec3 fragPos;
in vec3 fragNormal;

out vec4 FragColor;

uniform vec3 u_lightAmbient;
uniform vec3 u_lightDiffusePos;
uniform vec3 u_lightDiffuseColor;

void main()
{
    vec3 objectColor = fragPos;
    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(u_lightDiffusePos - fragPos);
    float diffuseStrength = max((dot(norm, lightDir)), 0.0);
    vec3 lightDiffuse = diffuseStrength * u_lightDiffuseColor;

    FragColor = vec4((u_lightAmbient + lightDiffuse) * objectColor, 1.0);
}
