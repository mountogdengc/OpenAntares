shader_type spatial;
render_mode unshaded, cull_back, depth_draw_opaque, fog_disabled;
#define PLANET_SPATIAL
#include "res://game/theme/planets/planet_common.gdshaderinc"
varying vec3 sphere_position;
void vertex() { sphere_position = VERTEX; }
void fragment() {
    vec3 normal = normalize(sphere_position);
    ALBEDO = planet_encode(planet_surface(planet_local(normal,spin_cycles),normal) + planet_glow(normal));
}
