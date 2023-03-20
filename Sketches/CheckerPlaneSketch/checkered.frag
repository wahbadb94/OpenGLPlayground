#version 460 core // using GLSL version 4.6

in vec2 fragUV;

out vec4 FragColor;

vec3 lightColor = vec3(0.9, 0.9, 1.0);
vec3 darkColor = vec3(0.0, 0.1, 0.0);
float divisions = 10.0;

void main()
{
    vec2 vUV = fragUV;
    float thickness = 0.05;
    float delta = 0.05 / 2.0;

    float x = fract(vUV.x * divisions);
    x = min(x, 1.0 - x); // when 0 -> 0, when 0.49 -> 0.49, when 0.51 -> 0.49

    float xdelta = fwidth(x);
    x = smoothstep(x - xdelta, x + xdelta, thickness);

    float y = fract(vUV.y * divisions);
    y = min(y, 1.0 - y);

    float ydelta = fwidth(y);
    y = smoothstep(y - ydelta, y + ydelta, thickness);

    float c = clamp(x + y, 0.0, 1.0);

    FragColor = vec4(vec3(mix(darkColor, lightColor, c)), 1.0);
}
