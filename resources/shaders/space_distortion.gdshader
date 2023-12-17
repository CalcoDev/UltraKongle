shader_type canvas_item;

uniform vec2 offset;
uniform vec2 strength;
uniform float speed;
uniform float rot_range_count;
uniform float rot_speed;

// piecewise function f
// let x be abs((asin(sin(TIME)) + (PI / 2.0)) * 2.0)
// { x if df > 0
// -x if df < 0 }

vec2 rotate(vec2 og, float theta)
{
	return vec2(
		cos(theta) * og.x - sin(theta) * og.y,
		sin(theta) * og.x + cos(theta) * og.y
	);
}

void fragment() {
	vec2 uv = UV;
	vec2 center = vec2(0.5, 0.5) + (offset * 0.005);
	float d = length(UV - center) * 2.0;
	
	//d += abs(sin(TIME * 0.1));
	
	vec2 a = vec2(cos(TIME * speed), sin(TIME * speed)) * strength;
	uv += (d * a);
	
	float map_d = floor(d * rot_range_count) / rot_range_count;
	if (map_d == 0.0)
		map_d += 1.0 / rot_range_count / 2.0;
	uv = rotate(uv - center, TIME * rot_speed * map_d) + center;

	vec4 col = texture(TEXTURE, uv);
	COLOR = col;
	//COLOR = vec4(map_d, 0, 0, 1);
}