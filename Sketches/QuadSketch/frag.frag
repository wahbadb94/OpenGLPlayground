#version 330 core // using GLSL version 3.3


uniform vec4 u_Color;
uniform sampler2D u_Texture0;

in vec2 frag_uv;
out vec4 FragColor;

void main()
{
    vec4 color = texture(u_Texture0, frag_uv);

    // set the blue stuff to be green
    if (color.b > 0.5 && color.r < color.b && color.g < color.b) {
        color = vec4(0., u_Color.g, 0., 1.);
    }

    FragColor = color;
}
