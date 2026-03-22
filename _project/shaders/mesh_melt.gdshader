shader_type spatial;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float squash_strength : hint_range(0.0, 5.0) = 2.5;
uniform float spread_strength : hint_range(0.0, 3.0) = 1.8;
uniform float noise_strength : hint_range(0.0, 1.0) = 0.3;
uniform float spread_end : hint_range(0.1, 1.0) = 0.7;
uniform sampler2D noise_tex : hint_default_white;
uniform vec4 albedo_color : source_color = vec4(0.2, 0.6, 0.3, 1.0);
uniform float mesh_height : hint_range(0.1, 10.0) = 2.0;

void vertex() {
    float noise = texture(noise_tex, VERTEX.xz * 0.5 + 0.5).r;
    float local_progress = clamp((progress - noise * noise_strength) / (1.0 - noise_strength), 0.0, 1.0);

    float half_height = mesh_height * 0.5;
    float height_factor = clamp((VERTEX.y + half_height) / mesh_height, 0.0, 1.0);

    // Vertical: same noisy melt as before, runs to full progress
    VERTEX.y -= height_factor * local_progress * squash_strength;
    VERTEX.y = max(VERTEX.y, -half_height);

    // Horizontal: same noisy melt, but capped at spread_end
    float capped_progress = clamp((min(progress, spread_end) - noise * noise_strength) / (1.0 - noise_strength), 0.0, 1.0);
    float spread = capped_progress * spread_strength * height_factor;
    VERTEX.x *= 1.0 + spread;
    VERTEX.z *= 1.0 + spread;
}

void fragment() {
    ALBEDO = albedo_color.rgb;
}