// Planet generator v2. Shared sampling and lighting; only the colour adapter checks the spatial output convention.
// v2 keeps the v1 uniform contract and entry points and replaces the look: smooth (unquantized) sampling,
// domain-warped multi-octave terrain with footprint-based detail fading, relief shading, moisture/height biomes,
// noisy ice caps and snow lines, ocean depth and sun glint, layered clouds with shadows, craters on airless
// worlds, turbulent bands on banded worlds and a day-weighted atmosphere rim.
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
// Retained for the v1 binder contract. v2 samples the sphere continuously and no longer snaps to this grid.
uniform float sampling_grid = 192.0;
// Small canvas globes hide the cloud layer; the surface must then also skip cloud shadows.
uniform bool show_clouds = true;
uniform bool diagnostic = false;
const float PLANET_TAU = 6.28318530718;
const vec3 PLANET_LIGHT = vec3(-0.696526, 0.398015, 0.597022);
// Fixed rotation between octaves hides the lattice of the value noise.
const mat3 PLANET_OCTAVE = mat3(vec3(0.00, 1.60, 1.20), vec3(-1.60, 0.72, -0.96), vec3(-1.20, -0.96, 1.28));

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
    vec3 f = fract(p); f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float a = mix(planet_random(cell, seed), planet_random(cell + ivec3(1,0,0), seed), f.x);
    float b = mix(planet_random(cell + ivec3(0,1,0), seed), planet_random(cell + ivec3(1,1,0), seed), f.x);
    float c = mix(planet_random(cell + ivec3(0,0,1), seed), planet_random(cell + ivec3(1,0,1), seed), f.x);
    float d = mix(planet_random(cell + ivec3(0,1,1), seed), planet_random(cell + ivec3(1,1,1), seed), f.x);
    return mix(mix(a,b,f.y), mix(c,d,f.y), f.z);
}
// Octave weight for a feature frequency (cells per sphere unit) at a pixel footprint (sphere units per pixel).
// Detail finer than about one cell per pixel fades to its mean instead of shimmering.
float planet_detail(float frequency, float footprint) {
    return 1.0 - smoothstep(0.30, 0.85, frequency * footprint);
}
// Zero-mean fractal sum in roughly -0.5..0.5 with footprint fading.
float planet_fbm(vec3 p, ivec2 seed, int octaves, float frequency, float footprint) {
    float total = 0.0; float amplitude = 0.5; float norm = 0.0;
    for (int octave = 0; octave < octaves; octave++) {
        total += amplitude * (planet_noise(p, seed) - 0.5) * planet_detail(frequency, footprint);
        norm += amplitude;
        p = PLANET_OCTAVE * p + vec3(3.7,1.9,5.3); frequency *= 2.0; amplitude *= 0.5;
    }
    return total / norm;
}
vec3 planet_warp(vec3 p, ivec2 seed) {
    return vec3(planet_noise(p + vec3(11.3,0.0,0.0), seed), planet_noise(p + vec3(0.0,7.7,0.0), seed),
        planet_noise(p + vec3(0.0,0.0,5.1), seed)) - 0.5;
}
// Height field. The first four octaves keep the v1 amplitudes so sea thresholds keep their land share;
// the warp and the finer zero-mean octaves add coastline and mountain detail.
float planet_height(vec3 p, float footprint) {
    vec3 q = p * 3.4 + planet_warp(p * 2.3, terrain_seed) * 0.85;
    float total = 0.5; float amplitude = 0.5333333333; float frequency = 3.4;
    for (int octave = 0; octave < 4; octave++) {
        total += amplitude * (planet_noise(q, terrain_seed) - 0.5) * planet_detail(frequency, footprint);
        q = PLANET_OCTAVE * q + vec3(3.7,1.9,5.3); amplitude *= 0.5; frequency *= 2.0;
    }
    float fine = planet_fbm(q, terrain_seed, 3, frequency, footprint);
    // Ridged detail rises with elevation: plains stay calm, highlands break into ranges.
    float ridge = 1.0 - abs(planet_noise(q * 0.5 + vec3(9.1,4.3,2.7), terrain_seed) * 2.0 - 1.0);
    ridge = (ridge * ridge - 0.33) * planet_detail(frequency * 0.5, footprint);
    total += fine * 0.085 + ridge * (surface_mode == 1 ? 0.02 : 0.07) * smoothstep(0.42, 0.72, total);
    float structure = 0.5 + 0.5 * sin(p.x * 3.0 + p.y * float(2 + structural_variant) + p.z * 4.0);
    // Variants 1 and 5 sample a rotated, warped lattice so their features do not line up with the noise grid.
    if (structural_variant == 1) structure = planet_noise(PLANET_OCTAVE * p * 4.5 + planet_warp(p * 4.0, terrain_seed) * 0.9, terrain_seed);
    if (structural_variant == 5) structure = abs(planet_noise(PLANET_OCTAVE * p * 6.5 + planet_warp(p * 5.0, terrain_seed) * 1.2, terrain_seed) * 2.0 - 1.0);
    if (structural_variant == 4) structure = abs(p.y);
    if (structural_variant == 6) structure = 1.0 - abs(p.y);
    return clamp(total * 0.8 + structure * 0.2, 0.0, 1.0);
}
float planet_footprint(vec3 p) { return max(length(fwidth(p)), 0.00001); }
float planet_terrain(vec3 p) { return planet_height(p, planet_footprint(p)); }
// Impact craters for airless worlds: bowls with raised rims from a jittered cell lattice.
float planet_craters(vec3 p, float scale, float footprint) {
    float weight = planet_detail(scale * 2.5, footprint);
    if (weight <= 0.0) return 0.0;
    vec3 q = p * scale;
    ivec3 base = ivec3(floor(q - 0.5));
    float relief = 0.0;
    for (int index = 0; index < 8; index++) {
        ivec3 cell = base + ivec3(index & 1, (index >> 1) & 1, (index >> 2) & 1);
        if (planet_random(cell, terrain_seed + ivec2(17, 3)) > 0.62) continue;
        vec3 centre = vec3(cell) + 0.2 + 0.6 * vec3(planet_random(cell, terrain_seed + ivec2(1, 0)),
            planet_random(cell, terrain_seed + ivec2(0, 1)), planet_random(cell, terrain_seed + ivec2(1, 1)));
        float size = mix(0.12, 0.42, pow(planet_random(cell, terrain_seed + ivec2(5, 9)), 2.2));
        float d = length(q - centre) / size;
        float bowl = -(1.0 - smoothstep(0.0, 1.0, d)) * 0.9;
        float rim = exp(-(d - 1.0) * (d - 1.0) * 28.0) * 0.55;
        relief += (bowl + rim) * size * 2.4;
    }
    return relief * weight;
}
// Inverse of R = tilt-Z * spin-Y. Fixed-frame light never rotates with the terrain.
vec3 planet_local(vec3 normal, int cycles) {
    float angle = PLANET_TAU * fract(rotation_phase + planet_clock_turn * float(cycles));
    float ct = cos(axial_tilt); float st = sin(axial_tilt);
    vec3 untilted = vec3(ct * normal.x + st * normal.y, -st * normal.x + ct * normal.y, normal.z);
    float c = cos(angle); float s = sin(angle);
    return vec3(c * untilted.x - s * untilted.z, untilted.y, s * untilted.x + c * untilted.z);
}
vec3 planet_sample_point(vec3 p) { return normalize(p); }
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
    float sites = district * smoothstep(threshold, min(1.0,threshold + 0.06),candidate);
    // On large globes each site breaks into individual lights; small globes keep the plain site value.
    float fine = planet_detail(230.0, planet_footprint(p));
    return sites * mix(1.0, 1.7 * smoothstep(0.42, 0.78, planet_noise(p * 230.0, city_seed + ivec2(2, 5))), fine);
}
float planet_day(vec3 normal) { return dot(normal, PLANET_LIGHT); }
vec3 planet_illumination(vec3 color, vec3 normal) {
    return color * (0.055 + 0.945 * smoothstep(-0.1,0.85,planet_day(normal)));
}
float planet_cloud_field(vec3 p, float footprint) {
    // Stretched along latitude and warped so systems read as streaks and swirls, not round blobs.
    vec3 q = p * vec3(3.0,5.4,3.0) + planet_warp(p * 2.1, cloud_seed) * 1.1;
    float field = 0.5 + planet_fbm(q, cloud_seed, 5, 5.4, footprint);
    float threshold = 0.71 - 0.32 * cloud_cover;
    float body = smoothstep(threshold, threshold + 0.075, field);
    // Thin veil around each system so edges feather instead of cutting off.
    return max(body, 0.35 * smoothstep(threshold - 0.06, threshold + 0.02, field));
}
float planet_cloud_mask(vec3 local_point) {
    if (cloud_cover <= 0.0 || diagnostic) return 0.0;
    vec3 p = planet_sample_point(local_point);
    return planet_cloud_field(p, planet_footprint(p)) * 0.82;
}
vec3 planet_banded(vec3 p, float footprint) {
    float sway = planet_fbm(p * vec3(1.6,5.0,1.6) + vec3(2.0,0.0,7.0), terrain_seed, 3, 5.0, footprint);
    float latitude = p.y + sway * 0.11;
    float belts = 0.5 + 0.5 * sin(latitude * 26.0 + 2.2 * sin(latitude * 8.0 + float(structural_variant)));
    float flow = planet_fbm(vec3(p.x * 2.4, latitude * 19.0, p.z * 2.4), terrain_seed, 4, 19.0, footprint);
    float tone = clamp(belts * 0.72 + flow * 0.9 + 0.12, 0.0, 1.0);
    vec3 color = mix(planet_color(lowland), planet_color(highland), smoothstep(0.15, 0.85, tone));
    // A few oval storms that sit inside the belts.
    vec3 q = vec3(p.x, latitude * 2.2, p.z) * 2.1;
    ivec3 cell = ivec3(floor(q));
    vec3 centre = vec3(cell) + 0.3 + 0.4 * vec3(planet_random(cell, terrain_seed + ivec2(3, 1)), 0.5, planet_random(cell, terrain_seed + ivec2(1, 3)));
    float storm = planet_random(cell, terrain_seed + ivec2(7, 7)) > 0.72 ? 1.0 - smoothstep(0.10, 0.20, length((q - centre) * vec3(1.0,1.9,1.0))) : 0.0;
    return mix(color, mix(planet_color(highland), planet_color(polar_color), 0.45), storm * 0.75 * planet_detail(8.0, footprint));
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
    float footprint = planet_footprint(p);
    float height = planet_height(p, footprint);
    float day = planet_day(normal);
    vec3 color;
    float relief = 1.0;
    float sheen = 0.0;
    if (surface_mode == 2) {
        color = planet_banded(p, footprint);
    } else {
        bool airless = surface_mode == 1;
        bool sea = !airless && water_enabled;
        // Antialiased coast; planet_land stays the exact mask shared with city sites and the checks.
        float coast = max(0.0035, footprint * 0.35);
        float land = !sea ? 1.0 : all_water ? 0.0 : smoothstep(sea_threshold - coast, sea_threshold + coast, height);
        float land_height = sea ? clamp((height-sea_threshold)/max(0.1,1.0-sea_threshold),0.0,1.0) : height;
        float crater = airless ? planet_craters(p, 5.0, footprint) + planet_craters(p + vec3(4.1), 13.0, footprint) * 0.6 : 0.0;
        // Tone variation that is independent of height: moisture on living worlds, dark plains on airless ones.
        float tone = 0.5 + planet_fbm(p * 2.2 + vec3(31.0,17.0,5.0), terrain_seed, 3, 2.2, footprint);
        float grain = planet_fbm(p * 21.0, terrain_seed, 2, 21.0, footprint);
        vec3 low = planet_color(lowland); vec3 high = planet_color(highland); vec3 polar = planet_color(polar_color);
        vec3 ground;
        if (airless) {
            ground = mix(low, high, smoothstep(0.25, 0.80, height + (tone - 0.5) * 0.5));
            ground *= mix(0.72, 1.06, smoothstep(0.35, 0.65, tone));
            ground = mix(ground, high * 1.12, clamp(crater * 5.0, 0.0, 0.5));
            ground *= 1.0 + clamp(crater * 2.2, -0.22, 0.0);
        } else {
            // Wet lowlands deepen, dry ones drift toward the highland tone, peaks carry the highland colour.
            vec3 wet = low * vec3(0.70, 0.80, 0.72);
            vec3 base = mix(mix(high, low, 0.55), wet, smoothstep(0.30, 0.72, tone));
            ground = mix(base, high, smoothstep(0.30, 0.82, land_height + (0.5 - tone) * 0.22));
            ground = mix(ground, high * 0.62, smoothstep(0.70, 1.0, land_height) * 0.5);
            // Pale shore band.
            ground = mix(ground, mix(high, vec3(1.0), 0.25), sea ? (1.0 - smoothstep(0.0, 0.035, land_height)) * 0.45 : 0.0);
        }
        ground *= 1.0 + grain * 0.22;
        if (land > 0.0) {
            // Relief: tilt the local normal by the height gradient and compare against the smooth sphere.
            vec3 east = abs(p.y) > 0.995 ? vec3(1.0,0.0,0.0) : normalize(cross(vec3(0.0,1.0,0.0), p));
            vec3 north = cross(p, east);
            float step_size = max(0.006, footprint * 1.5);
            vec3 pe = normalize(p + east * step_size); vec3 pn = normalize(p + north * step_size);
            float he = planet_height(pe, footprint); float hn = planet_height(pn, footprint);
            float h0 = height;
            if (airless) {
                h0 += crater * 0.16;
                he += (planet_craters(pe, 5.0, footprint) + planet_craters(pe + vec3(4.1), 13.0, footprint) * 0.6) * 0.16;
                hn += (planet_craters(pn, 5.0, footprint) + planet_craters(pn + vec3(4.1), 13.0, footprint) * 0.6) * 0.16;
            }
            float strength = airless ? 0.17 : 0.20 * smoothstep(0.0, 0.10, land_height);
            vec3 bumped = normalize(p - (east * (he - h0) + north * (hn - h0)) * (strength / step_size));
            vec3 light = planet_local(PLANET_LIGHT, spin_cycles);
            relief = clamp(1.0 + 1.5 * (dot(bumped, light) - dot(p, light)), 0.50, 1.45);
        }
        float depth = sea_threshold - height;
        vec3 water = mix(planet_color(shallow_water), planet_color(deep_water), smoothstep(0.0, 0.20, depth));
        water = mix(water, planet_color(shallow_water) * 1.25, (1.0 - smoothstep(0.0, 0.03, depth)) * 0.45);
        water *= 1.0 + grain * 0.06;
        color = mix(water, ground * relief, land);
        // Snow line falls toward the poles; only worlds with ice caps carry snow.
        if (ice_cap > 0.0) {
            float snow_line = 0.86 - abs(p.y) * 0.45;
            color = mix(color, polar * relief, land * smoothstep(snow_line, snow_line + 0.07, land_height) * 0.9);
            float edge = 1.0 - ice_cap + (tone - 0.5) * 0.10 + grain * 0.10;
            float ice = smoothstep(edge, edge + 0.035, abs(p.y));
            color = mix(color, polar * (0.94 + grain * 0.25) * mix(1.0, relief, 0.5 * land), ice);
            land = max(land, ice);
        }
        // Sun glint on open water: a broad sheen and a tight highlight around the mirror direction.
        float mirror = max(dot(normal, normalize(PLANET_LIGHT + vec3(0.0,0.0,1.0))), 0.0);
        sheen = (1.0 - land) * (0.07 * pow(mirror, 14.0) + 0.20 * pow(mirror, 70.0));
    }
    if (show_clouds && cloud_cover > 0.0) {
        // Shadow from the cloud deck, displaced toward the light.
        vec3 shadow_point = planet_sample_point(planet_local(normalize(normal + PLANET_LIGHT * 0.035), cloud_cycles));
        color *= 1.0 - 0.30 * planet_cloud_field(shadow_point, footprint);
    }
    color = planet_illumination(color, normal) + vec3(sheen) * smoothstep(0.0, 0.3, day);
    float night = 1.0-smoothstep(-0.3,0.12,day);
    color += planet_color(city_color) * planet_sites(p,height,city_level) * night;
    return color;
}
// On-disc limb glow, added to the surface and cloud layers alike so cloud blending leaves it unchanged.
// The normal is the unit-sphere surface normal in the fixed camera frame. The glow is stronger on the lit
// limb and adds a thin bright edge line.
float planet_rim(vec3 normal) {
    float grazing = clamp(1.0-normal.z,0.0,1.0);
    float lit = 0.30 + 0.70 * smoothstep(-0.35, 0.45, planet_day(normal));
    return (pow(grazing,3.0) + 0.55 * pow(grazing,9.0)) * atmosphere_density * lit;
}
vec3 planet_glow(vec3 normal) { return diagnostic ? vec3(0.0) : planet_color(atmosphere_color) * planet_rim(normal); }
// Off-disc halo coverage. Radius is the screen distance from the planet centre in planet radii.
float planet_halo(float radius) {
    if (diagnostic || radius < 1.0) return 0.0;
    float fade = clamp((1.10 - radius) / 0.10, 0.0, 1.0);
    return atmosphere_density * 0.22 * fade * fade;
}
