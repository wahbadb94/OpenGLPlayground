#version 460 core // using GLSL version 4.6 

in vec3 fragPos;
in vec3 fragNormal;
in vec3 objectColor;

out vec4 FragColor;

layout (location = 4) uniform vec3 u_lightAmbient;
layout (location = 5) uniform vec3 u_lightDiffusePos;
layout (location = 6) uniform vec3 u_lightDiffuseColor;
layout (location = 7) uniform vec3 u_cameraPos;
layout (location = 9) uniform float u_fogNear;
layout (location = 10) uniform float u_fogFar;

vec3 fogColor = vec3(0.0, 0.0, 0.0);
float getFogFactor();

void main()
{
    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(u_lightDiffusePos - fragPos);
    float diffuseStrength = max((dot(norm, lightDir)), 0.0);
    vec3 lightDiffuse = diffuseStrength * u_lightDiffuseColor;

    vec3 lightingColor = (u_lightAmbient + lightDiffuse) * objectColor;
    float fogFactor = getFogFactor();
    vec3 foggedColor = mix(lightingColor, fogColor, fogFactor);

    FragColor = vec4(foggedColor, 1.0);
}

float getFogFactor() {
    float dist = distance(u_cameraPos, fragPos);
    return smoothstep(u_fogNear, u_fogFar, dist);
}