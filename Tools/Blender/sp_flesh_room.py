# 살덩이 방(내장 방)을 블렌더로 만들어 목업용 모델 파일로 내보낸다.
#   좌우로 좁고 앞뒤로 긴 내장 굴 (폭 약 7m, 길이 35m). 한쪽 끝 = 들어오는 나무 문(식당 문이 살에 먹힌 모습),
#   반대쪽 끝 = 살 벽을 밀고 나온 사람 얼굴 문 (비명 지르듯 벌린 입·이빨, 위에 따라 구르는 눈알 두 개, 입 안은 목구멍 → 연구소로 가는 길)
#
# 사용 (프로젝트 폴더에서):
#   C:\Users\ADmiN\Tools\Blender-5.2.4\blender.exe -b --factory-startup --python Tools\Blender\sp_flesh_room.py -- [--preview <폴더>] [--blend <파일.blend>]
#   → Docs/SurplusPerson/models/sp_flesh_room.js 를 새로 씀 (목업이 이 파일을 읽음)
#
# 좌표: 목업(three.js) 기준으로 짠다. y가 위, 얼굴 벽 = z 0, 입구 = z -35, 바닥 = y 0, 방 가운데 = x 0.
#       목업에서는 이 모델 전체를 (67, 0, -1)에 놓는다. 블렌더 안에서는 (x, -z, y)로 바꿔 넣는다.
import bpy, bmesh, math, random, sys, os, base64, json, struct
from mathutils import Vector, noise, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT_JS = os.path.join(ROOT, 'Docs', 'SurplusPerson', 'models', 'sp_flesh_room.js')
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
opt = lambda k: argv[argv.index(k) + 1] if k in argv else None
PREVIEW, BLEND = opt('--preview'), opt('--blend')

L = 35.0          # 방 길이 (z -35 ~ 0)
HW0 = 3.5         # 바닥 폭의 절반
WALK = 2.5        # 걸어 다니는 폭의 절반 (벽·창자가 이 안으로 안 들어옴)
MOUTH_C = (0.0, 1.43)                       # 입 가운데 — 사람이 "아" 하고 크게 벌린 입. 아랫입술이 바닥 위로 보이게 띄움
MW, MHU, MHD = 1.25, .92, 1.0               # 입 구멍: 입꼬리까지 반폭 / 위·아래 높이
MRX, MRY = MW, (MHU + MHD) / 2              # (어림값 — 입 안 깊이 계산용)
MP, MQ = 2.4, .66                          # 입 구멍 윤곽: 입꼬리에서 위·아래 가장자리가 뾰족하게 만남 (MQ가 클수록 뾰족)
DEPTH = 2.4                                 # 입 안 깊이 (입술 → 목구멍 벽)
EYES = [(-1.5, 3.95), (1.5, 3.95)]
EYE_R, EYE_D = 0.55, 0.78                   # 눈알 반지름 / 눈알 가운데가 벽에서 튀어나온 거리

random.seed(7)
B = lambda x, y, z: Vector((x, -z, y))                       # 목업 좌표 → 블렌더 좌표
def n3(x, y, z, s=1.0): return noise.noise(Vector((x * s + 13.1, y * s + 7.7, z * s + 3.3)))
def fbm(x, y, z, s, oct=3):
    v, a, f = 0.0, 1.0, s
    for i in range(oct): v += a * n3(x + i * 31.7, y, z, f); a *= .5; f *= 2.03
    return v
def mix(a, b, t): t = max(0.0, min(1.0, t)); return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
def sstep(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
def gauss(x, y, cx, cy, sx, sy): return math.exp(-(((x - cx) / sx) ** 2 + ((y - cy) / sy) ** 2))

for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
SC = bpy.context.scene
EXPORT = []   # (오브젝트, 재질 키, 각진 모양?, 기준점(목업 좌표) 또는 None)

def mesh_obj(name, verts, faces, colors, key, flat=False, pivot=None, smooth=True):
    """verts: 목업 좌표 목록, colors: 꼭짓점마다 (r,g,b) 0~1"""
    me = bpy.data.meshes.new(name)
    me.from_pydata([B(*v) for v in verts], [], faces)
    me.update()
    set_colors(me, colors)
    if smooth and not flat:
        try: me.shade_smooth()
        except Exception: me.polygons.foreach_set('use_smooth', [True] * len(me.polygons))
    ob = bpy.data.objects.new(name, me); SC.collection.objects.link(ob)
    EXPORT.append((ob, key, flat, pivot))
    return ob

def set_colors(me, colors):
    at = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT')
    flat = []
    for c in colors: flat += [c[0], c[1], c[2], 1.0]
    at.data.foreach_set('color', flat)
    me.color_attributes.active_color = at
    try: me.color_attributes.render_color_index = 0
    except Exception: pass

def mesh_from(ob):
    """메타볼 오브젝트를 메시로 바꾼 새 오브젝트"""
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    me.transform(ob.matrix_world)
    nob = bpy.data.objects.new(ob.name + '_m', me); SC.collection.objects.link(nob)
    bpy.data.objects.remove(ob, do_unlink=True)
    try: me.shade_smooth()
    except Exception: pass
    return nob

def color_mesh(ob, fn):
    """fn(목업 좌표 x,y,z) → (r,g,b)"""
    set_colors(ob.data, [fn(v.co.x, v.co.z, -v.co.y) for v in ob.data.vertices])

def tv(ob):  # 오브젝트 꼭짓점을 목업 좌표로
    return [(v.co.x, v.co.z, -v.co.y) for v in ob.data.vertices]

def grid_faces(nu, nv, flip=False, skip=None):
    W = nu + 1; out = []
    for i in range(nv):
        for j in range(nu):
            if skip and skip(i, j): continue
            a = i * W + j
            out.append((a, a + W, a + W + 1, a + 1) if flip else (a, a + 1, a + W + 1, a + W))
    return out

# ---------------- 색 ----------------
C_DEEP = (0.36, 0.06, 0.08); C_FLESH = (0.66, 0.26, 0.27); C_PALE = (0.86, 0.58, 0.52)
C_VEIN = (0.28, 0.05, 0.14); C_WET = (0.30, 0.05, 0.06); C_BLOOD = (0.16, 0.01, 0.02)
C_SKIN = (0.74, 0.52, 0.47); C_SKIN_D = (0.52, 0.30, 0.30); C_LIP = (0.72, 0.32, 0.34); C_GUM = (0.80, 0.44, 0.44)
C_MUC = (0.60, 0.22, 0.24); C_MUC_D = (0.36, 0.07, 0.09); C_PALATE = (0.52, 0.17, 0.20); C_TONGUE = (0.78, 0.40, 0.40)

def vein(x, y, z, s=1.3):
    r = 1.0 - abs(n3(x, y, z, s))
    return sstep(.90, .975, r)

def flesh_col(x, y, z, cav):
    """cav: 튀어나옴(+) / 움푹(-)"""
    t = .45 + cav * 1.0 + fbm(x, y, z, .9, 2) * .25
    c = mix(C_DEEP, C_FLESH, t * 1.6) if t < .62 else mix(C_FLESH, C_PALE, (t - .62) * 1.3)
    c = mix(c, C_VEIN, vein(x, y, z) * .85)
    return c

# ---------------- 굴 (벽·천장) ----------------
def H(z):  # 천장 높이 — 얼굴 쪽 끝에서 높아짐
    return 4.4 + .3 * n3(0, 0, z, .2) + 1.2 * sstep(-8, -1.5, z)
def HW(z):
    return HW0 + .25 * n3(5, 0, z, .15)

def build_shell():
    NZ, NT, EXP = 170, 96, 3.0
    z0, z1 = -L - .6, .6
    verts, cols = [], []
    for i in range(NZ + 1):
        z = z0 + (z1 - z0) * i / NZ
        hw, h = HW(z), H(z)
        for j in range(NT + 1):
            th = -.14 + (math.pi + .28) * j / NT
            c, s = math.cos(th), math.sin(th)
            px = hw * math.copysign(abs(c) ** (2 / EXP), c)
            py = h * math.copysign(abs(s) ** (2 / EXP), s)
            gx = math.copysign((abs(px) / hw) ** (EXP - 1), px) / hw
            gy = math.copysign((abs(py) / h) ** (EXP - 1), py) / h
            gl = math.hypot(gx, gy) or 1
            nx, ny = -gx / gl, -gy / gl                       # 안쪽 방향
            blob = fbm(px, py, z, .42, 3)
            rug = (1 - abs(math.sin(math.pi * (z / 1.35 + .35 * n3(px, py, z, .5))))) ** 4   # 둘레를 감는 주름 (창자 안쪽처럼)
            bump = n3(px, py, z, 2.2)
            d = .62 * max(blob, -.35) + .16 * rug + .06 * bump
            if py < 2.1:                                      # 걸어 다니는 높이에서는 안으로 너무 안 들어오게
                lim = abs(px) - (WALK + .25)
                if nx * (-math.copysign(1, px)) > .2: d = min(d, lim / max(abs(nx), .3))
            verts.append((px + nx * d, py + ny * d, z))
            cols.append(flesh_col(px, py, z, d * .9 + rug * .1))
    return mesh_obj('shell_tunnel', verts, grid_faces(NT, NZ), cols, 'flesh')

def build_floor():
    NX, NZ = 44, 200
    x0, x1, z0, z1 = -3.8, 3.8, -L - .5, .2
    verts, cols = [], []
    for i in range(NZ + 1):
        z = z0 + (z1 - z0) * i / NZ
        for j in range(NX + 1):
            x = x0 + (x1 - x0) * j / NX
            e = max(0.0, abs(x) - (WALK + .1))
            y = .015 * n3(x, 0, z, 3) + e * e * .9 + e * .25 * (1 + n3(x, 1, z, 1.2))
            verts.append((x, y, z))
            wet = fbm(x, 0, z, .35, 2)
            c = mix(C_WET, C_FLESH, .35 + n3(x, 2, z, 1.4) * .3 + e * .6)
            c = mix(c, C_BLOOD, sstep(.25, .45, wet) * .9)           # 고인 피
            c = mix(c, C_PALE, sstep(.55, .7, n3(x * 3, 5, z * .6, 1.0)) * .35)   # 끈적한 점액 줄
            c = mix(c, C_VEIN, vein(x, 0, z, 1.0) * .6)
            cols.append(c)
    return mesh_obj('shell_floor', verts, grid_faces(NX, NZ, flip=True), cols, 'floor')

# ---------------- 관 모양 (갈비뼈·창자·힘줄) ----------------
def tube(name, pts, radius, key, colfn, ring=10, cap=True, pivot=None, flat=False):
    """pts: 목업 좌표 경로, radius: 숫자 또는 점마다 반지름 목록"""
    verts, faces, cols = [], [], []
    n = len(pts)
    rs = radius if isinstance(radius, (list, tuple)) else [radius] * n
    up = Vector((0, 1, 0))
    for i, p in enumerate(pts):
        p = Vector(p)
        t = (Vector(pts[min(i + 1, n - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        a = t.cross(up) if abs(t.dot(up)) < .95 else t.cross(Vector((1, 0, 0)))
        a.normalize(); b = t.cross(a).normalized()
        for k in range(ring):
            ang = 2 * math.pi * k / ring
            q = p + (a * math.cos(ang) + b * math.sin(ang)) * rs[i]
            verts.append(tuple(q)); cols.append(colfn(q.x, q.y, q.z, i / (n - 1)))
    for i in range(n - 1):
        for k in range(ring):
            a0 = i * ring + k; a1 = i * ring + (k + 1) % ring
            faces.append((a0, a1, a1 + ring, a0 + ring))
    if cap:
        for end in (0, n - 1):
            verts.append(tuple(pts[end])); cols.append(colfn(*pts[end], end / (n - 1)))
            c = len(verts) - 1
            for k in range(ring):
                a0 = end * ring + k; a1 = end * ring + (k + 1) % ring
                faces.append((a0, a1, c) if end == n - 1 else (a1, a0, c))
    return mesh_obj(name, verts, faces, cols, key, flat=flat, pivot=pivot)

C_BONE = (0.86, 0.80, 0.68); C_BONE_D = (0.55, 0.44, 0.34)
def bone_col(x, y, z, t):
    c = mix(C_BONE, C_BONE_D, .3 + n3(x, y, z, 2.5) * .5)
    return mix(c, C_DEEP, sstep(.2, .5, n3(x, y, z, .8)) * .5 + (1 - sstep(0, .6, y)) * .6)

def build_ribs():
    for k, z in enumerate([-31.5, -27.0, -22.5, -18.0, -13.5, -9.0]):
        hw, h = HW(z) - .28, H(z) - .3
        pts, rad = [], []
        for j in range(41):
            th = -.05 + (math.pi + .1) * j / 40
            c, s = math.cos(th), math.sin(th)
            x = hw * math.copysign(abs(c) ** (2 / 3.0), c); y = h * math.copysign(abs(s) ** (2 / 3.0), s)
            if y < 2.1: x = math.copysign(max(abs(x), WALK + .45), x)
            pts.append((x, y, z + .12 * math.sin(j * .7 + k)))
            knob = .05 * max(0, math.cos(j * 1.3)) ** 6                   # 마디 혹
            rad.append(.13 + knob + .03 * (1 - math.sin(math.pi * j / 40)))
        tube('shell_rib%d' % k, pts, rad, 'bone', bone_col, ring=9)

# ---------------- 창자 ----------------
C_GUT = (0.62, 0.30, 0.36); C_GUT_D = (0.40, 0.12, 0.20)
def gut_col(x, y, z, t):
    c = mix(C_GUT, C_GUT_D, .5 + n3(x, y, z, 1.5) * .6)
    return mix(c, C_VEIN, vein(x, y, z, 2.0) * .7)

def build_guts():
    for k, (side, za, zb, yb) in enumerate([(-1, -33.5, -21, .55), (-1, -19, -6, .7), (1, -34, -23, .65), (1, -21, -7.5, .5)]):
        pts, rad = [], []
        N = int((zb - za) * 4)
        for i in range(N + 1):
            z = za + (zb - za) * i / N
            r = .24 + .05 * n3(k, 0, z, .8)
            y = yb + .35 * math.sin(z * 2.3 + k) + .25 * n3(k, 1, z, .5)
            x = side * (WALK + .3 + r + .12 * (1 + math.sin(z * 1.7 + k * 2)))
            if i < 3 or i > N - 3: x = side * (abs(x) + .25)              # 끝은 벽 속으로 파고듦
            pts.append((x, max(y, r * .6), z)); rad.append(r)
        tube('shell_gut%d' % k, pts, rad, 'gut', gut_col, ring=8)

# ---------------- 천장에 늘어진 힘줄·방울 ----------------
def build_strands():
    verts, faces, cols = [], [], []
    def add(vv, ff, cc):
        o = len(verts); verts.extend(vv); cols.extend(cc); faces.extend([tuple(i + o for i in f) for f in ff])
    C_STR = (0.30, 0.07, 0.10)
    for i in range(22):
        x = random.uniform(-2.3, 2.3); z = random.uniform(-33.5, -3.5)
        top = H(z) - .1; ln = random.uniform(.5, 1.6); bot = max(top - ln, 2.5)
        r = random.uniform(.012, .028)
        ph = random.uniform(0, 6)
        pts = [(x + .08 * math.sin(t * 5 + ph) * t, top - (top - bot) * t, z + .06 * math.cos(t * 4 + ph) * t) for t in [j / 7 for j in range(8)]]
        ob = tube('tmp', pts, [r * (1.8 - .9 * j / 7) for j in range(8)], 'gut', lambda *a: C_STR, ring=5, cap=False)
        add(tv(ob), [tuple(p.vertices) for p in ob.data.polygons], [C_STR] * len(ob.data.vertices))
        EXPORT.pop(); bpy.data.objects.remove(ob, do_unlink=True)
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=7, v_segments=5, radius=1)
        dr = r * random.uniform(1.6, 2.4)
        vv = [(pts[-1][0] + v.co.x * dr, bot - dr * .8 + v.co.z * dr * 1.7, pts[-1][2] + v.co.y * dr) for v in bm.verts]
        ff = [tuple(v.index for v in f.verts) for f in bm.faces]; bm.free()
        add(vv, ff, [(0.34, 0.03, 0.05)] * len(vv))
    mesh_obj('shell_strands', verts, faces, cols, 'gut')

# ---------------- 숨 쉬는 혹 (목업에서 부풀었다 줄어듦) ----------------
def build_pulses():
    for k in range(8):
        side = -1 if k % 2 else 1
        z = -32 + k * 3.9 + random.uniform(-.8, .8); y = random.uniform(2.3, 3.4)
        x = side * (HW(z) - .15); s = random.uniform(.45, .8)
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=18, v_segments=12, radius=1)
        vv, cc = [], []
        for v in bm.verts:
            d = 1 + .18 * n3(v.co.x, v.co.y, v.co.z + k, 1.6)
            lx, ly, lz = v.co.x * s * .7 * d, v.co.z * s * d, v.co.y * s * 1.1 * d
            vv.append((lx, ly, lz))
            cc.append(mix(flesh_col(lx + x, ly + y, lz + z, .1 + v.co.x * -side * .2), C_VEIN, vein(lx, ly, lz + k, 3) * .8))
        ff = [tuple(v.index for v in f.verts) for f in bm.faces]; bm.free()
        ob = mesh_obj('pulse%d' % k, vv, ff, cc, 'flesh', pivot=(x, y, z))
        ob.location = B(x, y, z)

# ---------------- 메타볼 도우미 (혀·문틀 살덩이) ----------------
def mball(name, res=.07, thr=.6):
    mb = bpy.data.metaballs.new(name); mb.resolution = res; mb.render_resolution = res; mb.threshold = thr
    ob = bpy.data.objects.new(name, mb); SC.collection.objects.link(ob)
    return ob, mb
def el(mb, x, y, z, r, sx=1, sy=1, sz=1, neg=False, stiff=2.0):
    e = mb.elements.new(type='ELLIPSOID'); e.co = B(x, y, z); e.radius = r
    e.size_x, e.size_y, e.size_z = sx, sz, sy          # 블렌더 y = 목업 -z
    e.stiffness = stiff; e.use_negative = neg
    return e

# ---------------- 얼굴 문 (북쪽 끝) — 살 벽을 밀고 나온 사람 얼굴 (부조) ----------------
# 얼굴 = 높이 지도: D(x,y) = 벽에서 방 쪽으로 튀어나온 거리. 표면은 z = -D.
def mouth_h(dx, up):  # 입 가운데에서 위(또는 아래) 가장자리까지 높이. 입꼬리(|dx| = MW)에서 0
    u = min(1.0, abs(dx) / MW)
    return (MHU if up else MHD) * (1 - u ** MP) ** MQ
_RT = []                                    # 각도별 입 구멍 반지름 표
for _i in range(1440):
    _a = 2 * math.pi * _i / 1440; _c, _s = math.cos(_a), math.sin(_a); _lo, _hi = 0.0, 3.0
    for _ in range(28):
        _m = (_lo + _hi) / 2
        if abs(_c * _m) < MW and abs(_s * _m) < mouth_h(_c * _m, _s > 0): _lo = _m
        else: _hi = _m
    _RT.append(_lo)
def rim_r(a):
    f = (a % (2 * math.pi)) / (2 * math.pi) * 1440; i = int(f) % 1440; t = f - int(f)
    return _RT[i] * (1 - t) + _RT[(i + 1) % 1440] * t
def mouth_e(x, y):  # 입 구멍 테두리 = 1 (안쪽 < 1)
    dx, dy = x - MOUTH_C[0], y - MOUTH_C[1]; d = math.hypot(dx, dy)
    return 0.0 if d < 1e-6 else d / max(rim_r(math.atan2(dy, dx)), 1e-3)
def lip_v(x, y):
    """입술 띠 안이면 (0 = 입 안쪽 가장자리 ~ 1 = 입술 바깥 경계, 윗입술?, 두께 비율) — 입술은 위·아래 두 띠, 가운데가 두껍고 입꼬리에서 0"""
    dx = x - MOUTH_C[0]; u = abs(dx) / MW
    if u >= 1: return None
    up = y > MOUTH_C[1]
    k = max(0.0, 1 - u ** 2.2) ** .8
    T = (.3 if up else .4) * k
    if up: T += .045 * math.cos(u * math.pi * 3.2) * sstep(.45, .1, u) * k   # 윗입술 테두리의 활 모양 (가운데 살짝 패이고 양쪽 봉긋)
    d = abs(y - MOUTH_C[1]) - mouth_h(dx, up)
    if d < 0 or T < .01 or d > T: return None
    return d / T, up, k
def seg_d(x, y, ax, ay, bx, by):
    vx, vy = bx - ax, by - ay; t = max(0, min(1, ((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy)))
    return math.hypot(x - ax - vx * t, y - ay - vy * t)
FC, FRX, FRY = (0.0, 3.0), 3.0, 3.4
def face_mask(x, y): return sstep(1.08, .78, math.hypot((x - FC[0]) / FRX, (y - FC[1]) / FRY))

def face_D(x, y):
    r2 = ((x - FC[0]) / FRX) ** 2 + ((y - FC[1]) / FRY) ** 2
    m = face_mask(x, y)
    wall = .12 + .28 * max(fbm(x, y, 0, .55, 3), -.3)
    D = wall + m * (.95 * math.sqrt(max(0, 1 - r2)))
    # 얼굴 가장자리: 살 벽에 당겨 늘어난 힘줄
    ang = math.atan2(y - FC[1], x - FC[0])
    D += .07 * (1 - abs(math.sin(ang * 11 + n3(x, y, 0, .7) * 2))) ** 3 * sstep(.6, .95, math.sqrt(r2)) * sstep(1.25, .95, math.sqrt(r2))
    for s in (-1, 1):
        D += m * .2 * gauss(x, y, s * 1.45, 4.68, .95, .2)                  # 눈썹뼈
        D -= m * .05 * gauss(x, y, s * .2, 4.55, .05, .28)                 # 미간 주름
        D += m * .3 * gauss(x, y, s * 2.0, 2.55, .85, .75)                 # 볼
        D += m * .18 * gauss(x, y, s * 2.45, 3.5, .6, .4)                  # 광대
        D -= m * .07 * math.exp(-(seg_d(x, y, s * .5, 2.85, s * 1.42, 1.45) / .11) ** 2)   # 팔자 주름
        D += m * .05 * math.exp(-((seg_d(x, y, s * .5, 2.85, s * 1.42, 1.45) - .18) / .12) ** 2)
        D += m * .26 * gauss(x, y, s * .33, 2.9, .17, .15)                 # 콧방울
        D -= m * .22 * gauss(x, y, s * .17, 2.79, .075, .05)               # 콧구멍
        D += m * .05 * gauss(x, y, s * .11, 2.5, .05, .14)                 # 인중
    D += m * .03 * math.sin(y * 15) * gauss(x, y, 0, 5.3, 1.7, .45)        # 이마 주름
    D += m * .35 * gauss(x, y, 0, MOUTH_C[1] + .05, 1.5, 1.2)              # 입 둘레가 앞으로 쏠림
    if 2.7 < y < 4.3:                                                      # 콧대
        t = max(0, min(1, (4.15 - y) / 1.25)); w = .15 + .13 * t
        D += m * (.12 + .55 * t ** 1.5) * math.exp(-(x / w) ** 2) * sstep(4.3, 4.0, y) * sstep(2.72, 2.95, y)
    D += m * .3 * gauss(x, y, 0, 2.98, .25, .2)                            # 코끝
    D -= m * .1 * gauss(x, y, 0, 2.68, .3, .07)                            # 코 밑 그늘
    # 입술: 입 구멍 둘레의 두툼한 고리 (윗입술 가운데 활 모양, 아랫입술이 더 두꺼움)
    lv = lip_v(x, y)                                                       # 입술: 가운데 두툼, 입꼬리로 갈수록 얇아져 사라짐
    if lv:
        v, up, k = lv
        D += (.2 if up else .27) * k ** .7 * math.sin(math.pi * (.25 + .75 * v)) ** .7
    for s in (-1, 1): D -= m * .05 * gauss(x, y, s * (MW + .1), MOUTH_C[1], .12, .18)   # 입꼬리 옆 오목한 자리
    # 눈: 눈알이 보이는 아몬드 모양 구멍 + 눈알을 덮는 눈꺼풀
    for (ex, ey) in EYES:
        dx, dy = x - ex, y - ey; rr = math.hypot(dx, dy)
        sph = EYE_D + math.sqrt(max(0, EYE_R ** 2 - rr ** 2))
        alm = (dx / .56) ** 2 + (dy / (.27 if dy > 0 else .2)) ** 2
        w = sstep(EYE_R + .38, EYE_R - .02, rr)
        lid = sph + .07 + .05 * math.exp(-((alm - 1) / .25) ** 2)
        D = D * (1 - w) + lid * w
        if alm < 1: D = min(D, sph - .2)                                   # 구멍 (눈알이 보임)
        D -= .07 * math.exp(-((dy - .5) / .06) ** 2) * sstep(.8, .3, abs(dx))   # 쌍꺼풀 주름
        D += .08 * gauss(dx, dy, 0, -.62, .5, .14)                          # 눈 밑 처진 살
    return D

def face_col(x, y, z):
    m = face_mask(x, y); e = mouth_e(x, y)
    c = mix(C_SKIN_D, C_SKIN, .5 + n3(x, y, 0, 1.4) * .4 + (-z - .7) * .5)
    c = mix(flesh_col(x, y, z, -.1), c, m)                                 # 얼굴 바깥은 살 벽
    lv = lip_v(x, y)
    if lv: c = mix(c, mix((0.62, 0.20, 0.24), (0.76, 0.30, 0.33), sstep(0, .5, lv[0])), sstep(1.0, .88, lv[0]) * sstep(0, .2, lv[2]))   # 입술 (안쪽일수록 붉고 촉촉, 입꼬리에선 피부색)
    elif e < 1.02: c = mix(c, (0.62, 0.22, 0.25), .8)
    for (ex, ey) in EYES:                                                  # 눈 둘레는 검붉게 짓무름
        de = math.hypot(x - ex, (y - ey) * 1.3)
        c = mix(c, (0.42, 0.16, 0.20), sstep(1.0, .55, de) * .75)
    c = mix(c, (0.62, 0.30, 0.30), gauss(x, y, 0, 2.95, .3, .25) * .5)     # 빨간 코끝
    return mix(c, C_VEIN, vein(x, y, z, 1.8) * (.35 + .5 * (1 - m)))

def build_face():
    NX, NY = 150, 118
    x0, x1, y0, y1 = -4.6, 4.6, -.6, 6.5
    verts, cols, inside = [], [], []
    for i in range(NY + 1):
        yr = y0 + (y1 - y0) * i / NY
        for j in range(NX + 1):
            x, y = x0 + (x1 - x0) * j / NX, yr
            e = mouth_e(x, y); ins = e < 1
            if ins:                                                        # 입 구멍 안 꼭짓점은 입술 테두리로 붙임 (틈 없이 목구멍과 이어짐)
                k = 1 / max(e, 1e-3); x = MOUTH_C[0] + (x - MOUTH_C[0]) * k; y = MOUTH_C[1] + (y - MOUTH_C[1]) * k
            D = face_D(x, y)
            edge = max(0, (abs(x) - 3.1) / 1.5) ** 2 * 1.2 + max(0, (y - 5.4) / 1.1) ** 2 * 1.2   # 가장자리는 굴 벽 쪽으로 휨
            z = -D - edge
            verts.append((x, y, z)); cols.append(face_col(x, y, z)); inside.append(ins)
    W = NX + 1
    skip = lambda i, j: all(inside[a] for a in (i * W + j, i * W + j + 1, (i + 1) * W + j, (i + 1) * W + j + 1))
    mesh_obj('face_main', verts, grid_faces(NX, NY, skip=skip), cols, 'skin')

def rim(a, e=1.0):  # 입 구멍 테두리 위의 점 (각도 a)
    r = rim_r(a) * e
    return MOUTH_C[0] + math.cos(a) * r, MOUTH_C[1] + math.sin(a) * r
def lerp(a, b, t): return a + (b - a) * t
Y_TOP, Y_BOT = MOUTH_C[1] + MHU, MOUTH_C[1] - MHD
THROAT_Y = MOUTH_C[1] - .4                                                  # 목구멍 구멍 높이

# ---- 입 안: 입술 안쪽 → 볼 안쪽·입천장 → 목젖 쪽으로 처진 입천장 → 목구멍 벽 (가운데 아래가 검은 목구멍 구멍) ----
def build_mouth_inside():
    NT = 72
    ts = [0, .03, .07, .12, .2, .3, .42, .55, .68, .8, .9, 1.0, 1.12, 1.26, 1.4, 1.55, 1.7, 1.85, 2.0]
    verts, cols, faces = [], [], []
    for t in ts:
        for j in range(NT):
            a = 2 * math.pi * j / NT
            rx, ry = rim(a); Dr = face_D(rx, ry)
            top = max(0.0, math.sin(a)) ** 1.5
            if t <= 1:
                s = 1 - .03 * sstep(0, .07, t) + .3 * sstep(.07, .3, t) - .1 * sstep(.75, 1, t)   # 입술이 안으로 말렸다가 바로 볼 안쪽으로 넓어짐 (입술 뒤가 관처럼 안 보이게)
                cx, cy = MOUTH_C[0], MOUTH_C[1] + .1 * sstep(.1, .6, t)
                z = -Dr + .3 * sstep(0, .2, t) + (DEPTH + Dr - .3) * sstep(.2, 1, t)
                drop = .4 * top * sstep(.72, 1, t)                             # 목젖 쪽으로 처지는 여린 입천장
            else:
                u = t - 1
                s = 1.12 * (1 - u) ** .85
                cx, cy = MOUTH_C[0], lerp(MOUTH_C[1] + .1, THROAT_Y, u)
                z = DEPTH + .3 * math.sin(u * math.pi / 2) + 1.1 * sstep(.5, 1, u)   # 가운데로 갈수록 깊은 목구멍
                drop = .4 * top * (1 - u)
            x = cx + (rx - MOUTH_C[0]) * s; y = cy + (ry - MOUTH_C[1]) * s - drop
            if .1 < t <= 1.2: y = max(y, Y_BOT - .05)                          # 입 안 바닥 (혀 밑)
            roof = sstep(MOUTH_C[1] + .35, MOUTH_C[1] + .9, y) * sstep(.12, .3, t) * (1 - sstep(.95, 1.15, t))
            rug = (1 - abs(math.sin(z * 8 + abs(x) * 2.5))) ** 4 * roof * sstep(.75, .3, abs(x)) * sstep(.75, .4, t)  # 입천장 주름
            y += .03 * rug
            verts.append((x, y, z))
            if t < .1: c = mix(C_LIP, C_MUC, t / .1)
            elif t <= 1: c = mix(mix(C_MUC, C_MUC_D, sstep(.3, 1, t) * .6 + n3(x, y, z, 2.5) * .2), C_PALATE, roof)
            else: c = mix(mix(C_MUC, C_MUC_D, .35 + n3(x, y, z, 3) * .3), (0.0, 0.0, 0.0), sstep(.35, .75, t - 1))
            cols.append(mix(c, (0.85, 0.50, 0.48), rug * .5))
    for i in range(len(ts) - 1):
        for j in range(NT):
            a = i * NT + j; b = i * NT + (j + 1) % NT
            faces.append((a, b, b + NT, a + NT))
    last = (len(ts) - 1) * NT
    verts.append((0, THROAT_Y, DEPTH + 1.5)); cols.append((0, 0, 0)); c = len(verts) - 1
    for j in range(NT): faces.append((last + j, last + (j + 1) % NT, c))
    mesh_obj('face_mouth', verts, faces, cols, 'flesh')

    # 혀: 넓적하게 깔려 있고 가운데 홈, 끝은 아랫니 바로 뒤
    zb = -face_D(0, Y_BOT)
    ob, mb = mball('tongue', res=.045)
    for i in range(10):
        t = i / 9; z = zb + .85 + t * (DEPTH - .95 - zb)
        el(mb, 0, Y_BOT + .12 + .14 * t, z, .62, 1.85 - .3 * t, .55, 1.0)
    for s in (-1, 1): el(mb, s * .24, Y_BOT + .14, zb + .72, .3, 1.2, .65, 1)  # 혀끝 양쪽 볼록 (아랫니 바로 뒤)
    for i in range(16): el(mb, 0, Y_BOT + .4 + .01 * i, zb + 1.0 + i * .12, .12, .7, 1, 1.6, neg=True, stiff=1.3)   # 가운데 홈
    tg = mesh_from(ob)
    color_mesh(tg, lambda x, y, z: mix(mix(C_TONGUE, (0.58, 0.24, 0.26), sstep(Y_BOT + .25, Y_BOT + .0, y) + n3(x * 3, y * 3, z * 3, 3) * .15), C_MUC_D, sstep(1.0, DEPTH - .3, z)))
    EXPORT.append((tg, 'gut', False, None)); tg.name = 'face_tongue'

    # 여린 입천장 커튼: 목구멍 벽 앞에 늘어져 아치 모양 구멍을 만듦 (앞·뒤 아치 주름, 가운데 목젖, 양옆 편도)
    yr = MOUTH_C[1] + .1 + MRY * 1.12 - .4                                     # 처진 입천장 높이
    y0, aw = Y_BOT + .15, .6; h = yr - .1 - y0
    xb = lambda y: aw if y < y0 else aw * max(0.0, 1 - ((y - y0) / h) ** 2.2) ** .6   # 아치 구멍의 반폭
    ob, mb = mball('softpalate', res=.05)
    yy = Y_BOT - .1
    while yy < yr + .4:
        xx = -1.5
        while xx < 1.55:
            if abs(xx) > xb(yy) + .16 or yy > y0 + h + .14: el(mb, xx, yy, DEPTH - .28, .36, 1, 1, .6)
            xx += .2
        yy += .2
    for i in range(23):
        y = y0 + h * i / 22
        for side in (-1, 1):
            el(mb, side * (xb(y) + .05), y, DEPTH - .36, .19, .8, 1, .8)                 # 앞 아치 주름
            el(mb, side * xb(y) * .78, y - .05, DEPTH - .06, .14, .8, 1, .8)             # 뒤 아치 주름
    for side in (-1, 1): el(mb, side * .44, y0 + .3, DEPTH - .2, .26, .8, 1.3, .8)       # 편도
    for i in range(8):                                                                   # 목젖
        t = i / 7
        el(mb, 0, yr - .02 - .58 * t, DEPTH - .3 - .04 * t, .13 + .1 * t ** 2, 1, 1.2, 1)
    sp = mesh_from(ob)
    def spc(x, y, z):
        d = abs(x) - xb(y)
        c = mix((0.58, 0.20, 0.23), (0.76, 0.36, 0.38), sstep(.3, .02, abs(d)) * sstep(DEPTH - .1, DEPTH - .4, z))
        c = mix(c, (0.40, 0.10, 0.13), sstep(DEPTH - .25, DEPTH, z))                     # 뒤쪽은 어둡게
        c = mix(c, (0.80, 0.42, 0.42), sstep(.25, .05, abs(x)) * sstep(yr - .1, yr - .4, y) * sstep(y0 + .5, y0 + .8, y))   # 목젖
        return mix(c, (0.66, 0.16, 0.18), gauss(abs(x), y, .44, y0 + .3, .2, .3) * .6)   # 편도
    color_mesh(sp, spc)
    EXPORT.append((sp, 'gut', False, None)); sp.name = 'face_softpalate'

def tooth(kind, w, h, d):
    """뿌리 = (0,0,0), 끝 = -y 방향 (x = 폭, z = 두께, +z = 바깥 면). 반환: 꼭짓점, 면, 0~1 (뿌리→끝)"""
    R, K = 12, 10
    verts, faces, ts = [], [], []
    for k in range(K):
        t = k / (K - 1)
        if kind == 'incisor': sw, sd = .8 + .2 * sstep(0, .45, t), 1 - .55 * t                       # 앞니: 넓적, 끝이 얇은 날
        elif kind == 'canine': sw, sd = 1 - .75 * sstep(.45, 1, t), 1 - .3 * t                         # 송곳니: 뾰족
        else: sw, sd = .88 + .12 * sstep(0, .4, t), 1                                                # 작은어금니·어금니: 뭉툭
        if t > .8 and kind != 'canine': sw *= 1 - .3 * ((t - .8) / .2) ** 2                           # 모서리 둥글게
        for r in range(R):
            a = 2 * math.pi * r / R
            ca, sa = math.cos(a), math.sin(a)
            ex = math.copysign(abs(ca) ** .45, ca) * w / 2 * sw
            ez = math.copysign(abs(sa) ** .7, sa) * d / 2 * sd
            yy = -h * t + (.05 * h * (1 - abs(ca)) if kind == 'molar' and t > .8 else 0)
            verts.append((ex, yy, ez)); ts.append(t)
    for k in range(K - 1):
        for r in range(R):
            a = k * R + r; b = k * R + (r + 1) % R
            faces.append((a, b, b + R, a + R))
    verts.append((0, -h * (1.0 if kind == 'canine' else .97), 0)); ts.append(1)
    c = len(verts) - 1
    for r in range(R): faces.append(((K - 1) * R + r, (K - 1) * R + (r + 1) % R, c))
    return verts, faces, ts

def arch(aw, z0, dz, n=1.8):
    """치열(U자): s(가운데에서 잰 길이, 왼쪽은 -) → (x, z, 바깥 방향 x, z)"""
    pts, L = [(0.0, z0, 0.0)], 0.0
    for i in range(1, 400):
        x = aw * i / 399; z = z0 + dz * (x / aw) ** n
        px, pz, _ = pts[-1]; L += math.hypot(x - px, z - pz); pts.append((x, z, L))
    def at(s):
        side = 1 if s >= 0 else -1; s = abs(s)
        for i in range(1, len(pts)):
            if pts[i][2] >= s or i == len(pts) - 1:
                x0_, z0_, l0 = pts[i - 1]; x1_, z1_, l1 = pts[i]
                f = 0 if l1 == l0 else min(1, (s - l0) / (l1 - l0))
                tx, tz = x1_ - x0_, z1_ - z0_; tl = math.hypot(tx, tz) or 1
                return side * (x0_ + tx * f), z0_ + tz * f, side * tz / tl, -tx / tl
    return at

def build_teeth():
    verts, faces, cols = [], [], []
    def row(at, y_root, upper, kinds):
        s = -.012                                                                         # 앞니 둘은 딱 붙게
        for kind, w, h in kinds:
            for side in (-1, 1):
                x, z, nx_, nz_ = at(side * (s + w / 2))
                hh = h * random.uniform(.95, 1.05)
                vv, ff, ts = tooth(kind, w * random.uniform(.96, 1.04), hh, w * (.5 if kind == 'incisor' else .8))
                tx, tz = -nz_, nx_                                                         # 치열을 따라가는 방향
                o = len(verts)
                for (vx, vy, vz), t in zip(vv, ts):
                    vy2 = vy if upper else -vy
                    lean = (.05 if upper else .03) * abs(vy2) / hh                        # 이 끝이 살짝 바깥으로 기움
                    px_ = x + tx * vx + nx_ * (vz + lean); pz_ = z + tz * vx + nz_ * (vz + lean)
                    verts.append((px_, y_root + vy2, pz_))
                    c = mix((0.93, 0.89, 0.78), (0.80, 0.70, 0.52), .2 + n3(px_ * 4, vy2 * 4, pz_ * 4, 5) * .3 + (.25 if t < .2 else 0))
                    cols.append(mix(c, (0.82, 0.84, 0.86), sstep(.8, 1, t) * .35))    # 끝은 살짝 반투명한 회색
                faces.extend(tuple(i + o for i in (f if upper else f[::-1])) for f in ff)
            s += w + .004
    zt, zb = -face_D(0, Y_TOP), -face_D(0, Y_BOT)
    up = arch(1.1, zt + .24, 1.6)
    row(up, Y_TOP - .08, True, [('incisor', .3, .34), ('incisor', .23, .29), ('canine', .22, .32), ('molar', .2, .25), ('molar', .2, .24), ('molar', .27, .23), ('molar', .27, .2)])
    lo = arch(.98, zb + .26, 1.45)
    row(lo, Y_BOT + .07, False, [('incisor', .18, .2), ('incisor', .19, .21), ('canine', .2, .24), ('molar', .2, .2), ('molar', .21, .19), ('molar', .25, .18), ('molar', .25, .17)])
    mesh_obj('face_teeth', verts, faces, cols, 'tooth')
    # 잇몸: 치열을 따라가는 분홍 띠 (윗니 위, 아랫니 아래)
    for k, (at, y, r, ext) in enumerate([(up, Y_TOP - .03, .12, 1.75), (lo, Y_BOT + .05, .11, 1.6)]):
        pts = []
        for i in range(31):
            p = at(-ext + 2 * ext * i / 30)
            pts.append((p[0] - p[2] * .03, y, p[1] - p[3] * .03))
        tube('face_gum%d' % k, pts, r, 'gut', lambda x, y, z, t: mix(C_GUM, (0.66, 0.28, 0.30), n3(x * 3, y, z * 3, 4) * .5 + .2), ring=10)

def build_eyes():
    for i, (ex, ey) in enumerate(EYES):
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=40, v_segments=30, radius=EYE_R)
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 3, 'X'))   # 극점을 눈동자 쪽으로 (눈동자가 동그랗게)
        vv, cc = [], []
        for v in bm.verts:
            p = Vector((v.co.x, v.co.z, -v.co.y))                           # 목업 좌표로 (눈동자 = +z)
            vv.append(tuple(p))
            ang = math.acos(max(-1, min(1, p.z / EYE_R)))
            if ang < .17: c = (0.02, 0.01, 0.01)
            elif ang < .42:
                c = mix((0.30, 0.12, 0.07), (0.58, 0.40, 0.20), (ang - .17) * 3 + n3(p.x, p.y, i, 9) * .3)
                if ang > .39: c = (0.14, 0.05, 0.03)
            else:
                c = mix((0.94, 0.89, 0.80), (0.82, 0.58, 0.52), sstep(.9, 2.0, ang))
                c = mix(c, (0.72, 0.08, 0.10), vein(p.x, p.y, p.z + i * 5, 4.0) * sstep(.55, 1.2, ang))
            cc.append(c)
        ff = [tuple(v.index for v in f.verts) for f in bm.faces]; bm.free()
        ob = mesh_obj('eye%d' % i, vv, ff, cc, 'eye', pivot=(ex, ey, -EYE_D))
        ob.location = B(ex, ey, -EYE_D)
        ob.rotation_euler = (0, 0, math.pi)                                   # 미리보기: 방 쪽을 봄

# ---------------- 입구 문 (남쪽 끝) — 식당 문이 살에 먹힌 모습 ----------------
DOOR_W, DOOR_H, DZ = 1.1, 2.15, -L
def build_end_wall():
    NX, NY = 64, 48
    x0, x1, y0, y1 = -4.6, 4.6, -.6, 5.4
    verts, cols = [], []
    for i in range(NY + 1):
        y = y0 + (y1 - y0) * i / NY
        for j in range(NX + 1):
            x = x0 + (x1 - x0) * j / NX
            near = max(0, 1 - max(abs(x) - DOOR_W / 2 - .14, y - DOOR_H - .14, 0) / .7)
            d = .5 * max(fbm(x, y, DZ, .5, 3), -.3) + .1 * n3(x, y, 0, 2.5) - .2 * near
            edge = max(0, (abs(x) - 2.6) / 2) ** 2 * 1.6 + max(0, (y - 3.6) / 1.8) ** 2 * 1.6   # 가장자리는 굴 벽 쪽으로 휨
            verts.append((x, y, DZ - .2 + d + edge))
            cols.append(flesh_col(x, y, DZ, d * .9 - .3))
    def skip(i, j):
        cx = x0 + (x1 - x0) * (j + .5) / NX; cy = y0 + (y1 - y0) * (i + .5) / NY
        return abs(cx) < DOOR_W / 2 + .1 and 0 < cy < DOOR_H + .1
    mesh_obj('door_wall', verts, grid_faces(NX, NY, flip=True, skip=skip), cols, 'flesh')

def box(verts, faces, cols, x0, y0, z0, x1, y1, z1, colfn):
    o = len(verts)
    P = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    verts.extend(P); cols.extend([colfn(*p) for p in P])
    for f in [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (0, 4, 7, 3)]:
        faces.append(tuple(i + o for i in f))

def build_door():
    verts, faces, cols = [], [], []
    wood = lambda x, y, z: mix((0.42, 0.27, 0.19), (0.30, 0.18, 0.13), .4 + n3(x * 4, y * .3, z, 3) * .5)
    stain = lambda x, y, z: mix(wood(x, y, z), (0.30, 0.05, 0.06), sstep(.1, .5, n3(x, y, z, 1.6)) * .8)   # 피·살이 번진 얼룩
    dark = lambda x, y, z: (0.24, 0.14, 0.10)
    hw, z = DOOR_W / 2, DZ
    box(verts, faces, cols, -hw - .12, 0, z - .1, -hw, DOOR_H + .12, z + .12, stain)          # 문틀
    box(verts, faces, cols, hw, 0, z - .1, hw + .12, DOOR_H + .12, z + .12, stain)
    box(verts, faces, cols, -hw - .12, DOOR_H, z - .1, hw + .12, DOOR_H + .14, z + .12, stain)
    box(verts, faces, cols, -hw - .2, DOOR_H + .14, z + .06, hw + .2, DOOR_H + .24, z + .16, stain)
    box(verts, faces, cols, -hw, 0, z - .02, hw, DOOR_H, z + .03, stain)                       # 닫힌 문짝
    for (y0, y1) in ((.25, 1.0), (1.2, 1.95)):                                               # 문짝 판넬
        box(verts, faces, cols, -hw + .14, y0, z + .03, hw - .14, y1, z + .05, dark)
        box(verts, faces, cols, -hw + .2, y0 + .06, z + .05, hw - .2, y1 - .06, z + .065, stain)
    box(verts, faces, cols, hw - .2, 1.0, z + .03, hw - .12, 1.08, z + .12, lambda *a: (0.70, 0.55, 0.28))   # 손잡이
    mesh_obj('door_wood', verts, faces, cols, 'wood', flat=True)
    # 문틀을 덮으며 기어오른 살덩이 + 문짝에 늘어진 힘줄
    ob, mb = mball('doorflesh', res=.055)
    for i in range(30):
        t = i / 29
        if t < .4: x, y = -hw - .14 + random.uniform(-.12, .1), t / .4 * (DOOR_H + .1)
        elif t < .6: x, y = -hw + (t - .4) / .2 * DOOR_W, DOOR_H + .16 + random.uniform(-.05, .1)
        else: x, y = hw + .14 + random.uniform(-.1, .12), (1 - (t - .6) / .4) * (DOOR_H + .1)
        el(mb, x, y, z + .1, random.uniform(.2, .4), 1, random.uniform(.8, 1.5), .6)
    for (x, y, r) in [(-.22, 1.75, .2), (.28, .6, .16), (-.38, .3, .22), (.4, 1.45, .14)]:    # 문짝에 붙은 혹
        el(mb, x, y, z + .05, r, 1, 1.2, .5)
    df = mesh_from(ob)
    color_mesh(df, lambda x, y, z: flesh_col(x, y, z, -.05 + (z - DZ) * .5))
    EXPORT.append((df, 'flesh', False, None)); df.name = 'door_flesh'
    for k in range(5):                                                       # 문짝을 가로지르는 힘줄
        ya, yb = random.uniform(.3, 2.0), random.uniform(.3, 2.0)
        pts = [(-hw - .1 + (DOOR_W + .2) * t, ya + (yb - ya) * t - .12 * math.sin(math.pi * t), z + .07 + .02 * math.sin(math.pi * t)) for t in [j / 10 for j in range(11)]]
        tube('door_vein%d' % k, pts, [.025 + .015 * math.sin(math.pi * j / 10) for j in range(11)], 'gut', lambda *a: (0.38, 0.08, 0.14), ring=6)

# ---------------- 만들기 ----------------
build_shell(); build_floor(); build_ribs(); build_guts(); build_strands(); build_pulses()
build_face(); build_mouth_inside(); build_teeth(); build_eyes()
build_end_wall(); build_door()

# ---------------- 목업용 파일 쓰기 ----------------
def b64(b): return base64.b64encode(b).decode('ascii')
def export_js():
    objs, tris = [], 0
    dg = bpy.context.evaluated_depsgraph_get()
    for ob, key, flat, pivot in EXPORT:
        me = ob.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        mw = ob.matrix_world.copy() if pivot is None else Matrix.Identity(4)   # 기준점이 있는 것(눈알·혹)은 기준점 기준 좌표로
        col = me.color_attributes.get('Col')
        cv = [tuple(c.color[:3]) for c in col.data] if col else [(1, 1, 1)] * len(me.vertices)
        P = [(p.x, p.z, -p.y) for p in (mw @ v.co for v in me.vertices)]
        I = []
        if flat:
            V, N, C = [], [], []
            for t in me.loop_triangles:
                n = mw.to_3x3() @ t.normal
                for vi in t.vertices:
                    V.append(P[vi]); N.append((n.x, n.z, -n.y)); C.append(cv[vi])
        else:
            V = P; C = cv
            N = [(n.x, n.z, -n.y) for n in ((mw.to_3x3() @ vn.vector).normalized() for vn in me.vertex_normals)]
            I = [i for t in me.loop_triangles for i in t.vertices]
        tris += len(me.loop_triangles)
        print('  %-14s %7d tris' % (ob.name, len(me.loop_triangles)))
        vb = struct.pack('<%df' % (len(V) * 3), *[round(c, 4) for p in V for c in p])
        nb = struct.pack('<%db' % (len(N) * 3), *[max(-127, min(127, round(c * 127))) for p in N for c in p])
        cb = bytes([max(0, min(255, round(c * 255))) for p in C for c in p])
        o = {'n': ob.name, 'm': key, 'p': [round(c, 4) for c in (pivot or (0, 0, 0))], 'v': b64(vb), 'nr': b64(nb), 'c': b64(cb)}
        if not flat:
            big = len(V) > 65535
            o['i'] = b64(struct.pack('<%d%s' % (len(I), 'I' if big else 'H'), *I)); o['i32'] = 1 if big else 0
        objs.append(o)
        ob.evaluated_get(dg).to_mesh_clear()
    os.makedirs(os.path.dirname(OUT_JS), exist_ok=True)
    with open(OUT_JS, 'w', encoding='utf-8') as f:
        f.write('// 자동 생성 파일 — 손으로 고치지 말 것. 만든 곳: Tools/Blender/sp_flesh_room.py (블렌더)\n')
        f.write('// 살덩이 방: 길이 35m 내장 굴 + 얼굴 문(입·이빨·눈알 두 개) + 입구 문. 목업에서 (67,0,-1)에 놓음\n')
        f.write('window.SP_MODELS=window.SP_MODELS||{};SP_MODELS.fleshRoom=' + json.dumps({'objs': objs}, separators=(',', ':')) + ';\n')
    print('EXPORT', OUT_JS, 'objects', len(objs), 'tris', tris, 'bytes', os.path.getsize(OUT_JS))
export_js()

# ---------------- 미리보기 그림 ----------------
def preview(folder):
    os.makedirs(folder, exist_ok=True)
    SC.render.engine = 'BLENDER_WORKBENCH'
    sh = SC.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'VERTEX'; sh.show_cavity = True; sh.show_shadows = False
    SC.render.resolution_x, SC.render.resolution_y = 960, 600
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); SC.collection.objects.link(cam); SC.camera = cam
    shots = {'1_entrance_view': ((2.3, 3.2, -34.2), (-.3, 1.4, -24), 70),
             '2_face': ((1.6, 2.2, -10.5), (0, 2.6, 0), 60),
             '3_face_close': ((.9, 1.9, -5.0), (0, 2.4, 0), 62),
             '6_mouth_front': ((0, 1.25, -4.6), (0, 1.5, 0), 40),
             '4_face_side': ((-2.6, 1.6, -3.2), (.4, 2.6, 0), 62),
             '5_door': ((-1.2, 2.0, -27.5), (0, 1.3, -35), 62)}
    for name, (p, t, fov) in shots.items():
        cam.location = B(*p)
        cam.rotation_euler = (B(*t) - B(*p)).to_track_quat('-Z', 'Y').to_euler()
        cam.data.angle = math.radians(fov) * 1.25
        SC.render.filepath = os.path.join(folder, name + '.png')
        bpy.ops.render.render(write_still=True)
    print('PREVIEW', folder)
if PREVIEW: preview(PREVIEW)
if BLEND: bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(BLEND)); print('BLEND', BLEND)
