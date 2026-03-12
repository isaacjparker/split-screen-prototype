shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never;

uniform sampler2D slash_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float arc_degrees : hint_range(0.0, 360.0) = 200.0;

void fragment() {
    vec2 dir = vec2(UV.x - 0.5, 0.5 - UV.y);
    if (dot(dir, dir) < 0.0001) discard;

    float angle = degrees(atan(dir.y, dir.x));
    float half_arc = arc_degrees * 0.5;

    // Signed distance from center, wraps correctly across 360/0
    float diff = mod(angle - 90.0 + 180.0, 360.0) - 180.0;

    // Outside the arc entirely
    if (abs(diff) > half_arc) discard;

    // Remap diff from [-half_arc, +half_arc] to [0, arc_degrees]
    float position_in_arc = diff + half_arc;

    // Directional sweep: reveal from 0 to arc_degrees
    if (position_in_arc > progress * arc_degrees) discard;

    vec4 tex = texture(slash_texture, UV);
    ALBEDO = tex.rgb;
    ALPHA = tex.a;
}