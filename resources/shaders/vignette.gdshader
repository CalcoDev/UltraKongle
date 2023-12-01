shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

uniform float inner : hint_range(0.0, 1.0) = 0.4;
uniform float outer : hint_range(0.0, 1.0) = 0.95;
uniform vec4 color : source_color;

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    vec2 position = SCREEN_UV;
    float d = length(position - center);

    // Smoothstep to create a smooth transition
    float vignette = smoothstep(inner, outer, d);

    // Mix the original color with the vignette color
	vec4 colour = texture(SCREEN_TEXTURE, SCREEN_UV);
    COLOR = vec4(mix(colour.rgb, color.rgb, vignette), 1);
}