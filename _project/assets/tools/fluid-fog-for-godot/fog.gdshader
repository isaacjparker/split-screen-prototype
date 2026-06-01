shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_mix;

// Same density texture the FogManager already uploads - no manager changes needed.
uniform sampler2D density_tex : filter_linear, repeat_disable;

// Two colors: dense fog tends toward _low, thin wisps toward _high. The density
// swirling across this ramp is what reads as "colors mixing".
uniform vec4 fog_color_low : source_color = vec4(0.40, 0.44, 0.52, 1.0);
uniform vec4 fog_color_high : source_color = vec4(0.78, 0.82, 0.90, 1.0);

uniform float max_alpha = 0.9;
uniform float contrast = 2.0;          // sharpens density into the fog
uniform float detail_scale = 6.0;      // noise frequency: higher = finer wisps
uniform float warp_amount = 0.35;      // how much the noise folds on itself (swirl)
uniform float flow_speed = 0.04;       // ambient drift speed of the wisps
uniform float wisp_amount = 0.8;       // how strongly noise breaks up the fog
uniform float shading_strength = 0.7;  // fake volume from density gradients
uniform vec2 light_dir = vec2(0.6, 0.8);

float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float fbm(vec2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int k = 0; k < 5; k++) {   // drop to 3 if Steam Deck framerate dips
        v += amp * vnoise(p);
        p *= 2.0;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    vec2 uv = UV;
    float t = TIME * flow_speed;

    // Domain warp: sample noise, then use that to distort a second noise lookup.
    // This is what turns flat noise into folding, swirling structure.
    vec2 q = vec2(
        fbm(uv * detail_scale + vec2(0.0, t)),
        fbm(uv * detail_scale + vec2(5.2, -t))
    );
    float n = fbm(uv * detail_scale + warp_amount * q + t);

    // Sim density, broken up by the noise into wisps.
    float d = texture(density_tex, uv).r;
    float fog = clamp(d * (1.0 - wisp_amount + wisp_amount * 2.0 * n) * contrast, 0.0, 1.0);

    // Fake volume: shade by the density gradient (denser slopes catch the light).
    vec2 texel = 1.0 / vec2(textureSize(density_tex, 0));
    float dx = texture(density_tex, uv + vec2(texel.x, 0.0)).r - texture(density_tex, uv - vec2(texel.x, 0.0)).r;
    float dy = texture(density_tex, uv + vec2(0.0, texel.y)).r - texture(density_tex, uv - vec2(0.0, texel.y)).r;
    vec2 g = vec2(dx, dy);
    float gl = length(g);
    vec2 nrm = gl > 1e-4 ? g / gl : vec2(0.0);
    float shade = 1.0 + shading_strength * dot(nrm, normalize(light_dir));

    // Color ramp driven by density + noise, then shaded.
    vec3 col = mix(fog_color_high.rgb, fog_color_low.rgb, clamp(d * 1.2 + (n - 0.5) * 0.4, 0.0, 1.0));
    col *= clamp(shade, 0.4, 1.6);

    ALBEDO = col;
    ALPHA = fog * max_alpha;
}
