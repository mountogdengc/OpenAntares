shader_type spatial;
render_mode unshaded, blend_mix, cull_back, depth_draw_never, fog_disabled;
#define PLANET_SPATIAL
#include "res://game/theme/planets/planet_common.gdshaderinc"
varying vec3 sphere_position;
void vertex() { sphere_position = VERTEX; }
void fragment() {
    // Halo only. The on-disc limb glow lives in the surface and cloud layers (planet_glow).
    // Orthographic camera on +Z and an unrotated mesh: object-space XY is the screen offset in planet radii.
    // Alpha blending writes coverage into the transparent viewport, which additive blending does not.
    ALBEDO = planet_encode(planet_color(atmosphere_color));
    ALPHA = planet_halo(length(sphere_position.xy));
}
