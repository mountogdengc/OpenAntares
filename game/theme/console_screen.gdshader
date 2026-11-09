shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;
uniform bool enabled = true;
uniform vec4 tint : source_color;
uniform float strength = 0.025;
void fragment() {
    vec4 screen = texture(screen_texture, SCREEN_UV);
    float scan = step(0.5, fract(FRAGCOORD.y / 3.0));
    float edge = smoothstep(0.25, 0.75, length(UV - vec2(0.5)));
    COLOR = enabled ? vec4(mix(screen.rgb, tint.rgb, strength * (scan + edge)), screen.a) : screen;
}
