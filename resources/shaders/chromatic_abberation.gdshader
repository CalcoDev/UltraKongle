shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

uniform float strength : hint_range(-1.0, 1.0) = -0.5;

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    vec2 position = SCREEN_UV;
    float d = length(position - center);

	vec2 off = vec2(d * strength / 5.0, 0);
	
    vec3 r = texture(SCREEN_TEXTURE, SCREEN_UV + off).rgb;
    vec3 g = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
    vec3 b = texture(SCREEN_TEXTURE, SCREEN_UV - off).rgb;

	COLOR = vec4(r.r, g.g, b.b, 1);
}