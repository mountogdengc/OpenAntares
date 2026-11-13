shader_type canvas_item;
render_mode unshaded;
#define PLANET_CANVAS_SRGB
#include "res://game/theme/planets/planet_common.gdshaderinc"
uniform bool show_clouds = true;
void fragment() {
    vec2 xy = (UV * 2.0 - 1.0) / 0.85;
    xy.y = -xy.y;
    float radius = length(xy);
    float edge = max(fwidth(radius),0.0001);
    vec3 normal = normalize(vec3(xy,sqrt(max(0.0,1.0-dot(xy,xy)))));
    vec3 glow = planet_glow(normal);
    // Same order as the spatial layers: each layer is encoded on its own, then clouds blend over the surface.
    vec3 color = planet_encode(planet_surface(planet_local(normal,spin_cycles),normal) + glow);
    float cloud = show_clouds ? planet_cloud_mask(planet_local(normal,cloud_cycles)) : 0.0;
    color = mix(color,planet_encode(planet_illumination(planet_color(cloud_color),normal) + glow),cloud);
    float disc = 1.0-smoothstep(1.0-edge,1.0+edge,radius);
    float halo = planet_halo(radius);
    float alpha = disc + (1.0-disc)*halo;
    COLOR = vec4(mix(planet_encode(planet_color(atmosphere_color)),color,disc/max(alpha,0.0001)),alpha);
}
