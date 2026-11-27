# Blender (5.x) headless: render Megascans trial props, scanned textures (top row)
# vs flat cartoon colours (bottom row). Usage:
#   blender -b --factory-startup -P tools/art/render_megascans_trial.py -- <trial_dir> <out.png>
import bpy, sys, os, glob, math
import numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
TRIAL, OUT = argv[0], argv[1]
ORDER = ["traffic_cone", "traffic_delineator_post", "concrete_barrier",
         "modular_metal_guardrail_kit", "urban_japanese_street_shutter"]

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def import_fbx(path):
    before = set(bpy.data.objects)
    try:
        bpy.ops.import_scene.fbx(filepath=path)
    except Exception:
        bpy.ops.wm.fbx_import(filepath=path)
    meshes = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in meshes:  # drop the importer's scaled parent so location moves are in metres
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    return meshes


def tex_node(nt, path, non_color):
    n = nt.nodes.new('ShaderNodeTexImage')
    n.image = bpy.data.images.load(path, check_existing=True)
    if non_color:
        n.image.colorspace_settings.name = 'Non-Color'
    return n


def scan_material(folder, stem):
    m = bpy.data.materials.new(stem + "_scan")
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes['Principled BSDF']
    f = lambda k: (glob.glob(os.path.join(folder, f"*_{k}.jpg")) or [None])[0]
    if f("BaseColor"):
        nt.links.new(tex_node(nt, f("BaseColor"), False).outputs['Color'], bsdf.inputs['Base Color'])
    if f("Roughness"):
        nt.links.new(tex_node(nt, f("Roughness"), True).outputs['Color'], bsdf.inputs['Roughness'])
    if f("Normal"):
        nm = nt.nodes.new('ShaderNodeNormalMap')
        nt.links.new(tex_node(nt, f("Normal"), True).outputs['Color'], nm.inputs['Color'])
        nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    return m, f("BaseColor")


def flat_colour(basecolor_path):
    img = bpy.data.images.load(basecolor_path, check_existing=True)
    px = np.empty(len(img.pixels), dtype=np.float32)
    img.pixels.foreach_get(px)
    rgb = px.reshape(-1, 4)[:, :3]
    # dominant colour: median of the most saturated half (keeps cone orange, concrete grey)
    sat = rgb.max(1) - rgb.min(1)
    c = np.median(rgb[sat >= np.median(sat)], axis=0)
    # brightness follows the whole texture, hue follows the dominant colour
    c = c * (rgb.mean() / max(c.mean(), 1e-3))
    # cartoon push: boost saturation a little
    mean = c.mean()
    c = np.clip(mean + (c - mean) * 1.5, 0, 1)
    # Image.pixels of a JPG are sRGB-encoded; Base Color expects linear
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return (*c.tolist(), 1.0)


def flat_material(stem, colour):
    m = bpy.data.materials.new(stem + "_flat")
    m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = colour
    b.inputs['Roughness'].default_value = 0.9
    return m


def place(objs, x, y_off, mat):
    for o in objs:
        o.data.materials.clear()
        o.data.materials.append(mat)
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (lo + hi) / 2
    for o in objs:
        o.location += Vector((x - centre.x, y_off - centre.y, -lo.z))
    return hi - lo


folders = []
for key in ORDER:
    hit = [d for d in sorted(glob.glob(os.path.join(TRIAL, key + "*"))) if os.path.isdir(d)]
    folders.append(hit[0])

x = 0.0
sizes = []
for folder in folders:
    fbx = glob.glob(os.path.join(folder, "*.fbx"))[0]
    stem = os.path.basename(folder)
    scan_m, base = scan_material(folder, stem)
    flat_m = flat_material(stem, flat_colour(base))
    a = import_fbx(fbx)
    b = import_fbx(fbx)
    size = place(a, 0, 0, scan_m)
    w = max(size.x, 0.3)
    x += w / 2
    for o in a:
        o.location.x += x
    x += w + 0.15
    place(b, x, 0, flat_m)   # flat copy right next to the scanned one
    sizes.append((stem, size))
    x += w / 2 + 0.7

# ground
bpy.ops.mesh.primitive_plane_add(size=60, location=(x / 2, -1.1, 0))
g = bpy.data.materials.new("ground"); g.use_nodes = True
g.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.55, 0.55, 0.52, 1)
bpy.context.object.data.materials.append(g)

# light + world
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(30)))
bpy.context.object.data.energy = 3.5
scene.world = bpy.data.worlds.new("w")
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.75, 0.82, 0.9, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.9

# camera: orthographic, slightly from the front-above
bpy.ops.object.camera_add(location=(x / 2, -20, 1.3 + 20 * math.tan(math.radians(10))),
                          rotation=(math.radians(80), 0, 0))
cam = bpy.context.object
cam.data.type = 'ORTHO'
cam.data.ortho_scale = x + 0.2
scene.camera = cam

scene.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
scene.render.resolution_x, scene.render.resolution_y = 2400, 640
scene.view_settings.view_transform = 'Standard'
scene.render.filepath = OUT
bpy.ops.render.render(write_still=True)
for s, v in sizes:
    print(f"SIZE {s} {v.x:.2f} x {v.y:.2f} x {v.z:.2f} m")
