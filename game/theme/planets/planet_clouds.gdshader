shader_type spatial;
render_mode unshaded, blend_mix, cull_back, depth_draw_never, fog_disabled;
#define PLANET_SPATIAL
#include "res://game/theme/planets/planet_common.gdshaderinc"
varying vec3 sphere_position;
void vertex() { sphere_position = VERTEX; }
void fragment() {
    vec3 normal = normalize(sphere_position);
    ALBEDO = planet_encode(planet_illumination(planet_color(cloud_color),normal) + planet_glow(normal));
    ALPHA = planet_cloud_mask(planet_local(normal,cloud_cycles));
}
