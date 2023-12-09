shader_type canvas_item;

uniform sampler2D NOISE;
uniform vec2 u_center = vec2(0, 0);
uniform float u_offset = 0;

uniform float u_inner_radius = 0;

vec2 polar(vec2 uv, vec2 center, float repeat, float scale) {
    vec2 delta = uv - center;
    float radius = length(delta) * 2.0;
    float angle = atan(delta.x, delta.y) / TAU + 0.5;
	return vec2(angle * repeat, radius / scale);
}

void fragment() {
	//vec2 uv = UV * vec2(16.0 / 9.0, 1.0) - u_center;
	vec2 uv = (UV * 2.0 - 1.0) * vec2(16.0 / 9.0, 1.0) - u_center;
	vec2 polar_uv = polar(uv, vec2(0), 5, 1);
	
	vec2 spiral_uv = vec2(polar_uv.x + polar_uv.y, polar_uv.y + TIME * u_offset);
	float d = length(uv);
	
	vec3 col_dist = d < u_inner_radius ? vec3(0.0) : vec3(1.0);
	float col_spiral = texture(NOISE, fract(spiral_uv)).x;
	vec3 col = col_dist * vec3(step(0.2, col_spiral));
	
	
	
	COLOR = vec4(col, 1.0);
}