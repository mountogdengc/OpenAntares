// Planet generator v1. Shared sampling and lighting; only the colour adapter checks the spatial output convention.
global uniform float planet_clock_turn;
uniform ivec2 terrain_seed = ivec2(0);
uniform ivec2 cloud_seed = ivec2(0);
uniform ivec2 city_seed = ivec2(0);
uniform int surface_mode = 0;
uniform int structural_variant = 0;
uniform vec4 deep_water : source_color;
uniform vec4 shallow_water : source_color;
uniform vec4 lowland : source_color;
uniform vec4 highland : source_color;
uniform vec4 polar_color : source_color;
uniform vec4 cloud_color : source_color;
uniform vec4 atmosphere_color : source_color;
uniform vec4 city_color : source_color;
uniform bool water_enabled = true;
uniform bool all_water = false;
uniform float sea_threshold = 0.48;
uniform float ice_cap = 0.15;
uniform float cloud_cover = 0.3;
uniform float atmosphere_density = 0.3;
uniform int spin_cycles = 48;
uniform int cloud_cycles = 72;
uniform float axial_tilt = 0.0;
uniform float rotation_phase = 0.0;
uniform float city_level = 0.0;
uniform float city_threshold_sparse = 0.87;
uniform float city_threshold_dense = 0.57;
uniform float sampling_grid = 192.0;
uniform bool diagnostic = false;
const float PLANET_TAU = 6.28318530718;
const vec3 PLANET_LIGHT = vec3(-0.696526, 0.398015, 0.597022);

// Light in linear space in both views. Compatibility spatial shaders receive sRGB colours
// and expect sRGB ALBEDO, whereas Forward+/Mobile use linear uniforms and ALBEDO.
// The non-HDR canvas path also needs explicit decoding/encoding.
bool planet_srgb_path() {
#ifdef PLANET_CANVAS_SRGB
    return true;
#else
#ifdef PLANET_SPATIAL
    return OUTPUT_IS_SRGB;
#else
    return false;
#endif
#endif
}
vec3 planet_color(vec4 value) {
    if (!planet_srgb_path()) return value.rgb;
    vec3 c = value.rgb;
    return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), c));
}
vec3 planet_encode(vec3 value) {
    if (!planet_srgb_path()) return value;
    vec3 c = clamp(value, vec3(0.0), vec3(1.0));
    return mix(c * 12.92, 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055, step(vec3(0.0031308), c));
}

uint planet_hash(ivec3 cell, ivec2 seed) {
    uvec3 p = uvec3(cell);
    uint h = p.x * 1597334677u ^ p.y * 3812015801u ^ p.z * 2798796415u ^ uint(seed.x);
    h = (h ^ (h >> 16u) ^ uint(seed.y)) * 2246822519u;
    h = (h ^ (h >> 13u)) * 3266489917u;
    return h ^ (h >> 16u);
}
float planet_random(ivec3 cell, ivec2 seed) {
    return float(planet_hash(cell, seed) & 16777215u) / 16777215.0;
}
float planet_noise(vec3 p, ivec2 seed) {
    ivec3 cell = ivec3(floor(p));
    vec3 f = fract(p); f = f * f * (3.0 - 2.0 * f);
    float a = mix(planet_random(cell, seed), planet_random(cell + ivec3(1,0,0), seed), f.x);
    float b = mix(planet_random(cell + ivec3(0,1,0), seed), planet_random(cell + ivec3(1,1,0), seed), f.x);
    float c = mix(planet_random(cell + ivec3(0,0,1), seed), planet_random(cell + ivec3(1,0,1), seed), f.x);
    float d = mix(planet_random(cell + ivec3(0,1,1), seed), planet_random(cell + ivec3(1,1,1), seed), f.x);
    return mix(mix(a,b,f.y), mix(c,d,f.y), f.z);
}
float planet_terrain(vec3 p) {
    float total = 0.0; float amplitude = 0.5333333333;
    vec3 q = p * 3.4;
    for (int octave = 0; octave < 4; octave++) {
        total += amplitude * planet_noise(q, terrain_seed);
        q = q * 2.03 + vec3(3.7,1.9,5.3); amplitude *= 0.5;
    }
    float structure = 0.5 + 0.5 * sin(p.x * 3.0 + p.y * float(2 + structural_variant) + p.z * 4.0);
    if (structural_variant == 1) structure = planet_noise(p * 9.0, terrain_seed);
    if (structural_variant == 5) structure = abs(planet_noise(p * 13.0, terrain_seed) * 2.0 - 1.0);
    if (structural_variant == 4) structure = abs(p.y);
    if (structural_variant == 6) structure = 1.0 - abs(p.y);
    return clamp(total * 0.8 + structure * 0.2, 0.0, 1.0);
}
// Inverse of R = tilt-Z * spin-Y. Fixed-frame light never rotates with the terrain.
vec3 planet_local(vec3 normal, int cycles) {
    float angle = PLANET_TAU * fract(rotation_phase + planet_clock_turn * float(cycles));
    float ct = cos(axial_tilt); float st = sin(axial_tilt);
    vec3 untilted = vec3(ct * normal.x + st * normal.y, -st * normal.x + ct * normal.y, normal.z);
    float c = cos(angle); float s = sin(angle);
    return vec3(c * untilted.x - s * untilted.z, untilted.y, s * untilted.x + c * untilted.z);
}
vec3 planet_sample_point(vec3 p) {
    // Quantize the unit-sphere sample, not normals, silhouette, light or shared mask rules.
    return normalize(floor(p * sampling_grid + 0.5) / sampling_grid);
}
float planet_land(float height) {
    if (surface_mode == 1 || !water_enabled) return 1.0;
    if (all_water) return 0.0;
    return step(sea_threshold, height);
}
float planet_sites(vec3 p, float height, float level) {
    if (level <= 0.0 || surface_mode == 2 || (!all_water && planet_land(height) < 0.5)) return 0.0;
    float district = smoothstep(0.48,0.66,planet_noise(p * 17.0, city_seed));
    float threshold = mix(city_threshold_sparse,city_threshold_dense,level);
    float candidate = planet_noise(p * 92.0, city_seed);
    return district * smoothstep(threshold, min(1.0,threshold + 0.06),candidate);
}
float planet_day(vec3 normal) { return dot(normal, PLANET_LIGHT); }
vec3 planet_illumination(vec3 color, vec3 normal) {
    return color * (0.055 + 0.945 * smoothstep(-0.1,0.85,planet_day(normal)));
}
vec3 planet_surface(vec3 local_point, vec3 normal) {
    vec3 p = planet_sample_point(local_point);
    if (diagnostic) {
        vec3 result = vec3(0.1);
        if (p.y > 0.65) result = vec3(0.0,1.0,0.0);
        if (dot(p,normalize(vec3(0.35,0.0,1.0))) > 0.97) result = vec3(1.0,0.0,0.0);
        if (dot(p,normalize(vec3(-0.55,-0.3,1.0))) > 0.94) result = vec3(0.0,0.2,1.0);
        if (dot(p,normalize(vec3(0.1,-0.65,1.0))) > 0.985) result = vec3(1.0,1.0,0.0);
        return result;
    }
    float height = planet_terrain(p);
    float land = planet_land(height);
    float land_height = water_enabled ? clamp((height-sea_threshold)/max(0.1,1.0-sea_threshold),0.0,1.0) : height;
    vec3 ground = mix(planet_color(lowland),planet_color(highland),smoothstep(0.12,0.85,land_height));
    vec3 water = mix(planet_color(deep_water),planet_color(shallow_water),smoothstep(sea_threshold-0.18,sea_threshold,height));
    vec3 color = mix(water,ground,land);
    if (surface_mode == 2) color = mix(planet_color(lowland),planet_color(highland),0.5+0.5*sin(p.y*32.0+height*4.0));
    if (ice_cap > 0.0) color = mix(color,planet_color(polar_color),smoothstep(1.0-ice_cap,1.0-ice_cap+0.05,abs(p.y)));
    color = planet_illumination(color,normal);
    float night = 1.0-smoothstep(-0.3,0.12,planet_day(normal));
    color += planet_color(city_color) * planet_sites(p,height,city_level) * night;
    return color;
}
float planet_cloud_mask(vec3 local_point) {
    if (cloud_cover <= 0.0 || diagnostic) return 0.0;
    float field = planet_noise(planet_sample_point(local_point) * vec3(8.0,11.0,8.0),cloud_seed);
    return smoothstep(1.0-cloud_cover, min(1.0,1.08-cloud_cover),field)*0.66;
}
float planet_rim(vec3 normal) { return pow(clamp(1.0-normal.z,0.0,1.0),3.0)*atmosphere_density; }
// On-disc limb glow, added to the surface and cloud layers alike so cloud blending leaves it unchanged.
// The normal is the unit-sphere surface normal in the fixed camera frame.
vec3 planet_glow(vec3 normal) { return diagnostic ? vec3(0.0) : planet_color(atmosphere_color) * planet_rim(normal); }
// Off-disc halo coverage. Radius is the screen distance from the planet centre in planet radii.
float planet_halo(float radius) {
    if (diagnostic || radius < 1.0) return 0.0;
    float fade = clamp((1.10 - radius) / 0.10, 0.0, 1.0);
    return atmosphere_density * 0.22 * fade * fade;
}
