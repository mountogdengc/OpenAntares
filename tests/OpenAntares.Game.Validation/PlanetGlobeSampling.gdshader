shader_type canvas_item;
render_mode unshaded;
#include "res://game/theme/planets/planet_common.gdshaderinc"
void fragment() {
    float longitude = UV.x * PLANET_TAU;
    float latitude = (UV.y - 0.5) * 3.14159265359;
    vec3 p = vec3(sin(longitude)*cos(latitude),sin(latitude),cos(longitude)*cos(latitude));
    float height = planet_terrain(p);
    COLOR = vec4(planet_land(height),planet_sites(p,height,city_level),height,1.0);
}
