shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never;

uniform sampler2D slash_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float arc_degrees : hint_range(0.0, 360.0) = 200.0;

void fragment() {
    vec2 dir = vec2(UV.x - 0.5, 0.5 - UV.y);

    if (dot(dir, dir) < 0.0001) discard;

    float angle = degrees(atan(dir.y, dir.x));
    if (angle < 0.0) angle += 360.0;

    if (angle > progress * arc_degrees) discard;

    vec4 tex = texture(slash_texture, UV);
    ALBEDO = tex.rgb;
    ALPHA = tex.a;
}