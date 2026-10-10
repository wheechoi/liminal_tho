using System.Collections.Generic;
using System.IO;
using System.Linq;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Liminal.Audio;
using Liminal.Camera;
using Liminal.Core;
using Liminal.Enemy;
using Liminal.Environment;
using Liminal.Interaction;
using Liminal.Player;
using Liminal.Rendering;
using Liminal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Liminal.Editor
{
    /// <summary>
    /// B58 버티컬 슬라이스 씬을 목업(Docs/B58/B58_mockup.html) 좌표 그대로 생성한다.
    /// 메뉴: Liminal > Build B58 Scene. 다시 실행하면 씬을 처음부터 새로 만든다.
    /// 좌표는 목업과 같은 (x, y, z) 값. three.js(오른손)와 Unity(왼손) 차이로 화면은 좌우 반전되어 보이며,
    /// 기획서 9장의 "왼쪽/오른쪽" 표기는 Unity 쪽과 일치한다.
    /// </summary>
    public static class B58SceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/B58.unity";
        const string MatFolder = "Assets/_Project/Materials/B58";
        const string RenderFolder = "Assets/_Project/Rendering";
        const string MeshFolder = "Assets/_Project/Meshes/B58";

        static Shader ps1;
        static readonly Dictionary<string, Material> MatCache = new();
        static System.Random rng;
        static Transform root;

        // 텍스처
        static Texture2D tPlank, tCeil, tMoss, tKWall, tKFloor, tCream, tWhite, tDark, tRed, tRedPlank, tDoor, tRug, tKRug, tSign, tHang;

        static float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Color C(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        [MenuItem("Liminal/Build B58 Scene")]
        public static void Build()
        {
            rng = new System.Random(58);
            MatCache.Clear();
            foreach (var f in new[] { "Assets/_Project/Scenes", MatFolder, B58ProcTextures.Folder, RenderFolder, MeshFolder })
                Directory.CreateDirectory(f);
            AssetDatabase.Refresh();

            ps1 = Shader.Find("Liminal/PS1Lit");
            MakeTextures();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupRenderSettings();

            root = new GameObject("World").transform;
            BuildKitchen();
            var corLights = BuildCorridor();
            var eyeWall = BuildDollRoom();

            var game = new GameObject("B58Game");
            game.AddComponent<B58Game>();
            game.AddComponent<PS1Settings>();

            var girl = BuildGirl();
            var stalker = BuildStalker(girl);
            eyeWall.target = girl.transform;

            BuildInspectPoints();
            var rt = MakeRenderTexture();
            BuildCameras(girl.transform, rt);
            var dlg = BuildUI(girl, rt);
            girl.dialogue = dlg;

            var audioGo = new GameObject("Audio");
            audioGo.AddComponent<B58Audio>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[B58] 씬 생성 완료: " + ScenePath);
        }

        // ---------------- 렌더 설정 ----------------

        static void SetupRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = C(0x020302);
            RenderSettings.fogStartDistance = 2.6f;
            RenderSettings.fogEndDistance = 15f;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
        }

        static RenderTexture MakeRenderTexture()
        {
            string path = RenderFolder + "/B58_LowRes.renderTexture";
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (!rt)
            {
                rt = new RenderTexture(320, 240, 24, RenderTextureFormat.ARGB32) { name = "B58_LowRes" };
                AssetDatabase.CreateAsset(rt, path);
            }
            rt.filterMode = FilterMode.Point;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            EditorUtility.SetDirty(rt);
            return rt;
        }

        // ---------------- 텍스처 / 머티리얼 ----------------

        static void MakeTextures()
        {
            tPlank = B58ProcTextures.Plank();
            tCeil = B58ProcTextures.Ceil();
            tMoss = B58ProcTextures.Moss();
            tKWall = B58ProcTextures.KitchenWall();
            tKFloor = B58ProcTextures.KitchenFloor();
            tCream = B58ProcTextures.Cream();
            tWhite = B58ProcTextures.White();
            tDark = B58ProcTextures.Dark();
            tRed = B58ProcTextures.Red();
            tRedPlank = B58ProcTextures.RedPlank();
            tDoor = B58ProcTextures.Door();
            tRug = B58ProcTextures.EyeRug();
            tKRug = B58ProcTextures.KitchenRug();
            tSign = B58ProcTextures.Sign58();
            tHang = B58ProcTextures.HangingSign();
        }

        /// <summary>텍스처 + 반복 횟수 조합마다 머티리얼 하나 (목업 tf(rx, ry)).</summary>
        static Material Tex(Texture2D t, float rx = 1, float ry = 1)
        {
            string key = $"M_{t.name.Replace("T_", "")}_{rx:0.##}x{ry:0.##}";
            return GetMat(key, m =>
            {
                m.SetTexture("_BaseMap", t);
                m.SetTextureScale("_BaseMap", new Vector2(rx, ry));
            });
        }

        static Material Col(int hex) => GetMat($"M_Color_{hex:x6}", m => m.SetColor("_BaseColor", C(hex)));

        static Material Glow(int hex) => GetMat($"M_Glow_{hex:x6}", m =>
        {
            m.SetColor("_BaseColor", C(hex));
            m.SetFloat("_Unlit", 1f);
            m.SetFloat("_Cull", 0f);
        });

        static Material GetMat(string key, System.Action<Material> setup)
        {
            if (MatCache.TryGetValue(key, out var m)) return m;
            string path = $"{MatFolder}/{key}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(ps1);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = ps1;
            setup(m);
            EditorUtility.SetDirty(m);
            MatCache[key] = m;
            return m;
        }

        // ---------------- 빌더 (목업 box/wall/plane/endWall/sides) ----------------

        static GameObject Prim(PrimitiveType type, string name, Material m, Vector3 pos, Vector3 scale, Transform parent, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent ? parent : root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = parent == null || parent == root || parent.gameObject.isStatic;
            return go;
        }

        static GameObject Box(float sx, float sy, float sz, Material m, float x, float y, float z, Transform p = null, bool collider = false, string name = "Box")
            => Prim(PrimitiveType.Cube, name, m, new Vector3(x, y, z), new Vector3(sx, sy, sz), p, collider);

        static GameObject Wall(float sx, float sy, float sz, float x, float y, float z, Texture2D t, float tile)
            => Box(sx, sy, sz, Tex(t, Mathf.Max(sx, sz) / tile, sy / tile), x, y, z, null, true, "Wall");

        static GameObject Plane(float x0, float x1, float z0, float z1, float y, Material m, bool up = true, string name = "Floor")
        {
            var go = Prim(PrimitiveType.Quad, name, m, new Vector3((x0 + x1) / 2, y, (z0 + z1) / 2), new Vector3(x1 - x0, z1 - z0, 1), null, false);
            go.transform.localRotation = Quaternion.Euler(up ? 90f : -90f, 0f, 0f);
            return go;
        }

        static GameObject Plane(float x0, float x1, float z0, float z1, float y, Texture2D t, float tile, bool up = true, string name = "Floor")
            => Plane(x0, x1, z0, z1, y, Tex(t, (x1 - x0) / tile, (z1 - z0) / tile), up, name);

        static void EndWall(float z, float x0, float x1, float h, Texture2D t, float tile, float door)
        {
            if (door <= 0f) { Wall(x1 - x0, h, 0.1f, (x0 + x1) / 2, h / 2, z, t, tile); return; }
            float a = -door / 2, b = door / 2, dh = 2.1f;
            Wall(a - x0, h, 0.1f, (x0 + a) / 2, h / 2, z, t, tile);
            Wall(x1 - b, h, 0.1f, (b + x1) / 2, h / 2, z, t, tile);
            Wall(door, h - dh, 0.1f, 0, (h + dh) / 2, z, t, tile);
        }

        static void Sides(float x0, float x1, float z0, float z1, float h, Texture2D t, float tile)
        {
            Wall(0.1f, h, z1 - z0, x0, h / 2, (z0 + z1) / 2, t, tile);
            Wall(0.1f, h, z1 - z0, x1, h / 2, (z0 + z1) / 2, t, tile);
        }

        static void Ground(float x0, float x1, float z0, float z1)
        {
            var g = new GameObject("GroundCollider");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3((x0 + x1) / 2, -0.25f, (z0 + z1) / 2);
            var c = g.AddComponent<BoxCollider>();
            c.size = new Vector3(x1 - x0, 0.5f, z1 - z0);
            g.isStatic = true;
        }

        /// <summary>목업 PointLight(color, intensity, distance, decay=1). Unity 밝기는 화면 보고 맞춘 배율.</summary>
        public const float LightScale = 2.5f;

        static Light PointLight(string name, int hex, float intensity, float range, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = C(hex);
            l.intensity = intensity * LightScale;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static GameObject Group(string name, Vector3 pos, Transform parent = null)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent ? parent : root, false);
            g.transform.localPosition = pos;
            return g;
        }

        // ---------------- 부엌 x[-4,4] z[0,8] ----------------

        static void BuildKitchen()
        {
            var prev = root;
            root = Group("Kitchen", Vector3.zero, prev).transform;
            const float h = 2.8f;
            Plane(-4, 4, 0, 8, 0, tKFloor, 0.8f);
            Plane(-4, 4, 0, 8, h, tKWall, 2, false, "Ceiling");
            Ground(-4, 4, 0, 8.6f);
            EndWall(0, -4, 4, h, tKWall, 2, 0);
            EndWall(8, -4, 4, h, tKWall, 2, 1.2f);
            Sides(-4, 4, 0, 8, h, tKWall, 2);

            Material cream = Tex(tCream), white = Tex(tWhite), dark = Tex(tDark), blk = Col(0x151510);
            // 식탁 + 의자
            Box(1.7f, 0.08f, 1.1f, white, 0.4f, 0.78f, 3.6f, null, true, "Table");
            foreach (var (dx, dz) in new[] { (-.75f, -.45f), (.75f, -.45f), (-.75f, .45f), (.75f, .45f) })
                Box(0.07f, 0.76f, 0.07f, dark, 0.4f + dx, 0.38f, 3.6f + dz, null, true, "TableLeg");
            var ch = Col(0x9ccf3a);
            Box(0.5f, 0.08f, 0.5f, ch, -0.9f, 0.46f, 3.6f, null, true, "Chair");
            Box(0.08f, 0.55f, 0.5f, ch, -1.15f, 0.75f, 3.6f, null, true, "ChairBack");
            foreach (var (dx, dz) in new[] { (-.2f, -.2f), (.2f, -.2f), (-.2f, .2f), (.2f, .2f) })
                Box(0.05f, 0.44f, 0.05f, dark, -0.9f + dx, 0.22f, 3.6f + dz, null, true, "ChairLeg");
            Box(0.08f, 0.16f, 0.08f, white, 0.7f, 0.9f, 3.5f, null, false, "Milk");
            Box(0.12f, 0.02f, 0.12f, blk, 0.05f, 0.83f, 3.8f);
            Box(0.18f, 0.02f, 0.1f, blk, 0.25f, 0.83f, 3.4f);
            // 조리대 + 상부장
            Box(3.4f, 0.9f, 0.7f, cream, 2.2f, 0.45f, 0.4f, null, true, "Counter");
            Box(3.4f, 0.05f, 0.74f, Col(0x5e7a62), 2.2f, 0.92f, 0.4f);
            Box(3.4f, 0.7f, 0.4f, cream, 2.2f, 2.05f, 0.25f, null, false, "UpperCabinet");
            for (int i = 0; i < 6; i++)
                Box(0.07f, Rnd(.18f, .32f), 0.07f, Col(i % 2 == 1 ? 0x1d3a24 : 0x3a2a14), 1.0f + i * 0.28f, 1.06f, 0.35f, null, false, "Bottle");
            Box(0.42f, 0.32f, 0.34f, white, 3.3f, 1.12f, 0.4f);
            // 냉장고 + 전자레인지
            Box(0.85f, 1.8f, 0.75f, white, -3.4f, 0.9f, 0.5f, null, true, "Fridge");
            Box(0.04f, 0.5f, 0.04f, blk, -3.0f, 1.2f, 0.88f);
            Box(0.6f, 0.34f, 0.42f, white, -3.4f, 1.97f, 0.5f, null, false, "Microwave");
            Box(0.36f, 0.22f, 0.02f, blk, -3.48f, 1.97f, 0.72f);
            // 찬장
            Box(0.55f, 1.9f, 1.1f, dark, -3.68f, 0.95f, 3.0f, null, true, "Cupboard");
            for (int i = 0; i < 4; i++)
                Box(0.3f, 0.12f, 0.18f, white, -3.62f, 1.4f + (i % 2) * 0.35f, 2.75f + (i >> 1) * 0.45f, null, false, "Bowl");
            // 러그 + 전등
            Plane(-1.6f, 0, 5.4f, 6.8f, 0.01f, tKRug, 1.4f, true, "Rug");
            var shade = new GameObject("LampShade");
            shade.transform.SetParent(root, false);
            shade.transform.localPosition = new Vector3(0.4f, 2.35f, 3.6f);
            shade.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
            var smr = shade.AddComponent<MeshRenderer>();
            smr.sharedMaterial = Glow(0xc9a43c);
            smr.shadowCastingMode = ShadowCastingMode.Off;
            Box(0.02f, 0.3f, 0.02f, blk, 0.4f, 2.65f, 3.6f, null, false, "LampCord");
            PointLight("KitchenLamp", 0xe8d27c, 1.7f, 8, new Vector3(0.4f, 2.1f, 3.6f));
            PointLight("KitchenFill", 0x7f9a60, 0.7f, 7, new Vector3(-2.4f, 2.2f, 6.4f));
            root = prev;
        }

        /// <summary>목업 ConeGeometry(0.42, 0.28, 8, 1, open).</summary>
        static Mesh ConeMesh()
        {
            string path = MeshFolder + "/LampShadeCone.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;
            const int seg = 8;
            const float r = 0.42f, hh = 0.14f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                int b = v.Count;
                v.Add(new Vector3(0, hh, 0));
                v.Add(new Vector3(Mathf.Sin(a0) * r, -hh, Mathf.Cos(a0) * r));
                v.Add(new Vector3(Mathf.Sin(a1) * r, -hh, Mathf.Cos(a1) * r));
                var nn = Vector3.Cross(v[b + 2] - v[b], v[b + 1] - v[b]).normalized;
                n.Add(nn); n.Add(nn); n.Add(nn);
                tri.AddRange(new[] { b, b + 2, b + 1 });
            }
            var m = new Mesh { name = "LampShadeCone" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.SetUVs(0, v.Select(p => new Vector2(p.x, p.z)).ToList());
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------------- 서쪽 통로 x[-1.2,1.2] z[8,26] ----------------

        static List<Light> BuildCorridor()
        {
            var prev = root;
            root = Group("Corridor", Vector3.zero, prev).transform;
            const float h = 2.6f;
            Plane(-1.2f, 1.2f, 8, 26, 0, tMoss, 1.2f);
            Plane(-1.2f, 1.2f, 8, 26, h, tCeil, 1.2f, false, "Ceiling");
            Ground(-1.2f, 1.2f, 8, 26);
            Sides(-1.2f, 1.2f, 8, 26, h, tPlank, 1.3f);
            var pm = Tex(tPlank, 1, 3);
            for (float z = 8.6f; z < 26f; z += 1.45f)
            {
                Box(2.4f, 0.18f, 0.22f, pm, 0, 2.5f, z, null, false, "Beam");
                Box(0.14f, 2.6f, 0.2f, pm, -1.12f, 1.3f, z, null, true, "Post");
                Box(0.14f, 2.6f, 0.2f, pm, 1.12f, 1.3f, z, null, true, "Post");
            }
            // 왼쪽 가짜 문틀
            foreach (var z in new[] { 13.8f, 18.2f })
                Box(0.08f, 2.0f, 0.8f, Col(0x020202), -1.14f, 1.0f, z, null, false, "FakeDoorway");
            // 오른쪽 -58 표지판
            var s = Prim(PrimitiveType.Quad, "Sign_-58", Tex(tSign), new Vector3(1.13f, 1.75f, 11.2f), new Vector3(0.42f, 0.56f, 1), null, false);
            s.transform.localRotation = Quaternion.Euler(0, 90f, 0); // 방 안쪽(-x)을 향함
            // 매달린 안내판
            Box(1.1f, 0.28f, 0.04f, Tex(tHang), 0, 2.15f, 21.0f, null, false, "HangingSign");
            foreach (var dx in new[] { -.45f, .45f })
                Box(0.015f, 0.3f, 0.015f, Col(0x111111), dx, 2.42f, 21.0f, null, false, "Wire");
            var a = PointLight("CorridorLightA", 0x6fa7a0, 1.1f, 8, new Vector3(0, 2.2f, 10.5f));
            var b = PointLight("CorridorLightB_Orange", 0xc98b4a, 0.9f, 7, new Vector3(0, 2.2f, 19.5f));
            b.gameObject.AddComponent<LightFlicker>();
            PointLight("CorridorLightC", 0x4f7a74, 0.6f, 7, new Vector3(0, 2.2f, 24.5f));
            root = prev;
            return new List<Light> { a, b };
        }

        // ---------------- 인형의 방 x[-4,4] z[26,34] ----------------

        static EyeWall BuildDollRoom()
        {
            var prev = root;
            root = Group("DollRoom", Vector3.zero, prev).transform;
            const float h = 3.4f;
            Plane(-4, 4, 26, 34, 0, tRedPlank, 1.2f);
            Plane(-4, 4, 26, 34, h, tCeil, 1.4f, false, "Ceiling");
            Ground(-4, 4, 25.4f, 34);
            EndWall(26, -4, 4, h, tRed, 1.8f, 1.2f);
            EndWall(34, -4, 4, h, tRed, 1.8f, 0);
            Sides(-4, 4, 26, 34, h, tRed, 1.8f);
            var wain = Tex(tRedPlank, 4, 1);
            Box(7.9f, 0.9f, 0.06f, wain, 0, 0.45f, 33.92f, null, false, "Wainscot");
            Box(0.06f, 0.9f, 7.9f, wain, -3.92f, 0.45f, 30, null, false, "Wainscot");
            Box(0.06f, 0.9f, 7.9f, wain, 3.92f, 0.45f, 30, null, false, "Wainscot");
            foreach (var x in new[] { -2.35f, 2.35f }) Box(3.3f, 0.9f, 0.06f, wain, x, 0.45f, 26.08f, null, false, "Wainscot");
            // 잠긴 문
            Box(1.15f, 2.2f, 0.08f, Tex(tDoor), 0.2f, 1.1f, 33.92f, null, true, "LockedDoor");
            Box(1.35f, 0.12f, 0.1f, Col(0x3a2620), 0.2f, 2.26f, 33.9f, null, false, "DoorFrame");
            // 기둥
            var pil = Prim(PrimitiveType.Cylinder, "Pillar", Tex(tRed, 1, 2), new Vector3(3.35f, h / 2, 33.35f), new Vector3(0.6f, h / 2, 0.6f), null, true);
            // 눈 카펫
            Plane(-1.9f, 1.9f, 27.0f, 29.8f, 0.012f, Tex(tRug), true, "EyeRug");

            // 눈알 벽 (오른쪽 벽)
            var wall = Group("EyeWall", Vector3.zero);
            var eyeWall = wall.AddComponent<EyeWall>();
            Material lid = Col(0xb7aca8), iris = Col(0x231314);
            var irises = new List<Transform>();
            var sizes = new List<float>();
            for (int r = 0; r < 6; r++)
            for (int c = 0; c < 7; c++)
            {
                if (rng.NextDouble() < .18) continue;
                float y = 0.45f + r * 0.36f + Rnd(-.06f, .06f), z = 28.6f + c * 0.68f + (r % 2) * 0.3f + Rnd(-.08f, .08f);
                if (z > 33.2f) continue;
                float sz = Rnd(.13f, .19f);
                var g = Group("Eye", new Vector3(3.95f, y, z), wall.transform);
                Prim(PrimitiveType.Sphere, "Lid", lid, Vector3.zero, new Vector3(sz * 0.55f * 2, sz * 0.62f * 2, sz * 2), g.transform, false);
                var ir = Prim(PrimitiveType.Sphere, "Iris", iris, new Vector3(-sz * 0.5f, 0, 0), Vector3.one * sz * 0.32f * 2, g.transform, false);
                g.isStatic = ir.isStatic = false;
                irises.Add(ir.transform);
                sizes.Add(sz);
            }
            eyeWall.irises = irises.ToArray();
            eyeWall.sizes = sizes.ToArray();

            // 매달린 마네킹
            var wood = Col(0x8d8a86);
            void Manneq(float x, float y, float z, float s)
            {
                var g = Group("Mannequin", new Vector3(x, y, z));
                g.transform.localScale = Vector3.one * s;
                var t = g.transform;
                Box(0.012f, h - y, 0.012f, Col(0x0d0d0d), 0, (h - y) / 2 / s, 0, t, false, "String");
                Prim(PrimitiveType.Sphere, "Head", wood, new Vector3(0, -0.1f, 0), new Vector3(0.24f * 0.85f, 0.24f * 1.1f, 0.24f * 0.9f), t, false);
                Box(0.24f, 0.36f, 0.14f, wood, 0, -0.42f, 0, t, false, "Torso");
                Box(0.2f, 0.14f, 0.13f, wood, 0, -0.67f, 0, t, false, "Pelvis");
                foreach (var sd in new[] { -1, 1 })
                {
                    var ar = Box(0.05f, 0.42f, 0.05f, wood, sd * 0.17f, -0.5f, 0, t, false, "Arm");
                    ar.transform.localRotation = Quaternion.Euler(Rnd(-.4f, .4f) * Mathf.Rad2Deg, 0, sd * Rnd(.1f, .9f) * Mathf.Rad2Deg);
                    var lg = Box(0.06f, 0.55f, 0.06f, wood, sd * 0.07f, -1.0f, 0, t, false, "Leg");
                    lg.transform.localRotation = Quaternion.Euler(Rnd(-.3f, .3f) * Mathf.Rad2Deg, 0, sd * Rnd(-.1f, .3f) * Mathf.Rad2Deg);
                }
                g.transform.localRotation = Quaternion.Euler(0, Rnd(-.6f, .6f) * Mathf.Rad2Deg, 0);
                var sw = g.AddComponent<DollSway>();
                sw.phase = Rnd(0, 6);
                sw.amplitude = Rnd(.02f, .06f);
                SetStaticRecursive(g, false);
            }
            Manneq(-3.55f, 2.7f, 27.2f, 1.0f); Manneq(-3.6f, 2.3f, 28.6f, 0.9f); Manneq(-3.5f, 3.0f, 30.1f, 1.05f);
            Manneq(-3.6f, 2.2f, 31.6f, 0.85f); Manneq(-3.4f, 2.9f, 33.0f, 1f);
            Manneq(-1.2f, 2.9f, 32.6f, 1f); Manneq(1.7f, 2.95f, 33.2f, 0.9f); Manneq(-2.3f, 2.5f, 33.5f, 0.8f);

            // 매달린 테디베어
            {
                var tb = Col(0x8a5b3f);
                var g = Group("TeddyBear", new Vector3(-1.6f, 1.9f, 33.75f));
                Prim(PrimitiveType.Sphere, "Body", tb, Vector3.zero, Vector3.one * 0.32f, g.transform, false);
                Prim(PrimitiveType.Sphere, "Head", tb, new Vector3(0, 0.23f, 0), Vector3.one * 0.24f, g.transform, false);
                foreach (var s in new[] { -1, 1 }) Prim(PrimitiveType.Sphere, "Ear", tb, new Vector3(s * 0.09f, 0.33f, 0), Vector3.one * 0.09f, g.transform, false);
                Box(0.012f, 1.3f, 0.012f, Col(0x0d0d0d), 0, 0.95f, 0, g.transform, false, "String");
                var sw = g.AddComponent<DollSway>();
                sw.phase = 1; sw.amplitude = .03f;
                SetStaticRecursive(g, false);
            }
            // 랜턴
            Box(0.16f, 0.26f, 0.16f, Glow(0xff5040), -3.75f, 1.75f, 27.6f, null, false, "Lantern");
            PointLight("RedLantern", 0xff3a30, 1.9f, 10, new Vector3(-3.3f, 1.8f, 27.8f));
            PointLight("DollFill", 0xa53a4a, 0.9f, 9, new Vector3(1.2f, 3.0f, 31f));
            root = prev;
            return eyeWall;
        }

        static void SetStaticRecursive(GameObject g, bool s)
        {
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = s;
        }

        // ---------------- 소녀 ----------------

        static TankController BuildGirl()
        {
            var girl = new GameObject("Girl");
            girl.tag = "Player";
            girl.transform.position = new Vector3(-1.4f, 0.02f, 5.2f);
            girl.transform.rotation = Quaternion.Euler(0, 0.35f * Mathf.Rad2Deg, 0);
            var cc = girl.AddComponent<CharacterController>();
            cc.height = 1.4f; cc.radius = 0.25f; cc.center = new Vector3(0, 0.72f, 0); cc.skinWidth = 0.02f; cc.stepOffset = 0.2f;
            var rb = girl.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var tank = girl.AddComponent<TankController>();
            var acts = AssetDatabase.LoadAllAssetsAtPath("Assets/InputSystem_Actions.inputactions").OfType<InputActionReference>().ToList();
            InputActionReference A(string n) => acts.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "Tank" && r.action.name == n);
            tank.move = A("Move"); tank.run = A("Run"); tank.interact = A("Interact"); tank.quickTurn = A("QuickTurn");

            var body = Group("Body", Vector3.zero, girl.transform).transform;
            Material skin = Col(0xd8c0a4), hair = Col(0x261a15), top = Col(0x8c8d86), skirt = Col(0x2b2e36), sock = Col(0xb9b6ad), shoe = Col(0x17120f);
            var hips = new Transform[2];
            var shoulders = new Transform[2];
            int k = 0;
            foreach (var s in new[] { -1, 1 })
            {
                var hip = Group(s < 0 ? "HipL" : "HipR", new Vector3(s * 0.075f, 0.56f, 0), body).transform;
                Box(0.085f, 0.3f, 0.085f, skin, 0, -0.15f, 0, hip, false, "Leg");
                Box(0.08f, 0.2f, 0.08f, sock, 0, -0.4f, 0, hip, false, "Sock");
                Box(0.09f, 0.06f, 0.15f, shoe, 0, -0.53f, 0.025f, hip, false, "Shoe");
                var sh = Group(s < 0 ? "ShoulderL" : "ShoulderR", new Vector3(s * 0.19f, 1.12f, 0), body).transform;
                Box(0.07f, 0.24f, 0.07f, top, 0, -0.11f, 0, sh, false, "Sleeve");
                Box(0.06f, 0.2f, 0.06f, skin, 0, -0.33f, 0, sh, false, "Hand");
                sh.localRotation = Quaternion.Euler(0, 0, s * 0.08f * Mathf.Rad2Deg);
                hips[k] = hip; shoulders[k] = sh; k++;
            }
            Prim(PrimitiveType.Cylinder, "Skirt", skirt, new Vector3(0, 0.66f, 0), new Vector3(0.42f, 0.15f, 0.42f), body, false);
            Prim(PrimitiveType.Cylinder, "Top", top, new Vector3(0, 0.98f, 0), new Vector3(0.3f, 0.2f, 0.3f), body, false);
            Box(0.06f, 0.06f, 0.06f, skin, 0, 1.2f, 0, body, false, "Neck");
            Prim(PrimitiveType.Sphere, "Head", skin, new Vector3(0, 1.32f, 0), Vector3.one * 0.25f, body, false);
            Prim(PrimitiveType.Sphere, "Hair", hair, new Vector3(0, 1.35f, -0.025f), Vector3.one * 0.28f, body, false);
            Box(0.29f, 0.22f, 0.13f, hair, 0, 1.24f, -0.06f, body, false, "HairBack");
            Box(0.25f, 0.06f, 0.05f, hair, 0, 1.415f, 0.095f, body, false, "Bangs");
            foreach (var s in new[] { -1, 1 })
            {
                Box(0.05f, 0.2f, 0.08f, hair, s * 0.125f, 1.25f, 0.04f, body, false, "HairSide");
                Box(0.025f, 0.02f, 0.01f, shoe, s * 0.045f, 1.33f, 0.123f, body, false, "Eye");
            }
            SetStaticRecursive(girl, false);

            var anim = girl.AddComponent<GirlAnimator>();
            anim.controller = tank;
            anim.body = body;
            anim.leftHip = hips[0]; anim.rightHip = hips[1];
            anim.leftShoulder = shoulders[0]; anim.rightShoulder = shoulders[1];
            return tank;
        }

        // ---------------- 그것 ----------------

        static WeepingStalker BuildStalker(TankController player)
        {
            var beast = new GameObject("WeepingStalker");
            beast.transform.position = new Vector3(-0.4f, 0, 21.6f);
            var pale = Col(0xcdbd9c);
            var hip = Group("Hip", new Vector3(0, 0.62f, 0), beast.transform).transform;
            var torso = Group("Torso", Vector3.zero, hip).transform;
            torso.localRotation = Quaternion.Euler(1.05f * Mathf.Rad2Deg, 0, 0);
            Box(0.3f, 0.62f, 0.2f, pale, 0, 0.3f, 0, torso, false, "Chest");
            for (int i = 0; i < 5; i++) Box(0.05f, 0.05f, 0.05f, pale, 0, 0.08f + i * 0.12f, -0.11f, torso, false, "Spine");
            var head = Group("Head", new Vector3(0, 0.66f, 0), torso).transform;
            Prim(PrimitiveType.Sphere, "Skull", pale, new Vector3(0, 0.06f, 0.06f), new Vector3(0.26f * 0.9f, 0.26f * 1.2f, 0.26f), head, false);
            Box(0.12f, 0.03f, 0.02f, Col(0x1a0d0b), 0, 0, 0.17f, head, false, "Mouth");
            var arms = new Transform[2];
            int k = 0;
            foreach (var s in new[] { -1, 1 })
            {
                var sh = Group(s < 0 ? "ArmL" : "ArmR", new Vector3(s * 0.17f, 0.55f, 0), torso).transform;
                Box(0.06f, 0.85f, 0.06f, pale, 0, -0.4f, 0, sh, false, "Arm");
                sh.localRotation = Quaternion.Euler(-1.0f * Mathf.Rad2Deg, 0, s * 0.12f * Mathf.Rad2Deg);
                arms[k++] = sh;
                var th = Box(0.08f, 0.42f, 0.08f, pale, s * 0.12f, -0.1f, 0.12f, hip, false, "Thigh");
                th.transform.localRotation = Quaternion.Euler(-1.2f * Mathf.Rad2Deg, 0, 0);
                var sn = Box(0.07f, 0.48f, 0.07f, pale, s * 0.14f, -0.38f, 0.18f, hip, false, "Shin");
                sn.transform.localRotation = Quaternion.Euler(0.35f * Mathf.Rad2Deg, 0, 0);
            }
            SetStaticRecursive(beast, false);
            var st = beast.AddComponent<WeepingStalker>();
            st.player = player;
            st.torso = torso; st.head = head; st.leftArm = arms[0]; st.rightArm = arms[1];
            return st;
        }

        // ---------------- 조사 지점 11곳 (목업 spots) ----------------

        static void BuildInspectPoints()
        {
            var parent = Group("InspectPoints", Vector3.zero);
            (float x, float z, float r, string name, string t)[] spots =
            {
                // 대사 연출 (Text Animator 태그): <shake> 떨림, <wiggle> 꿈틀, <wave> 물결, <dangle> 매달림,
                // <waitfor=초> 멈칫, <speed=배율> 타이핑 속도. 모든 글자는 기본으로 {fade} 등장.
                (0.4f, 3.0f, 1.0f, "01_Milk", "식탁 위에 우유 한 잔. 아직 미지근하다.<waitfor=0.5> 나는 이걸 <wiggle>따른 기억이 없다</wiggle>."),
                (-3.4f, 1.2f, 0.9f, "02_Fridge", "냉장고는 비어 있다. 문 안쪽에 손톱으로 긁어 새긴 숫자 —<waitfor=0.6> <shake>58</shake>."),
                (-3.0f, 3.0f, 0.8f, "03_Cupboard", "찬장의 그릇은 전부 <wave>두 개씩</wave>이다.<waitfor=0.5> 이 집에 사는 사람은 <shake>나 하나</shake>인데."),
                (0f, 7.4f, 0.8f, "04_CorridorDoor", "문 너머로 젖은 나무 냄새가 올라온다.<waitfor=0.4> <speed=0.6><size=90%><color=#8f8b7c>여기서부터는 소리를 내지 않는 게 좋겠다.</color></size></speed>"),
                (0.7f, 11.2f, 1.0f, "05_Sign58", "「<shake>-58</shake>」.<waitfor=0.4> 엘리베이터 버튼은 B3까지밖에 없었다."),
                (-0.7f, 13.8f, 0.8f, "06_FakeDoorway", "문틀 안쪽은 그냥 벽이다.<waitfor=0.4> 누군가 문을 <wiggle>그려 넣은</wiggle> 것뿐이었다."),
                (0f, 20.6f, 1.1f, "07_HangingSign", "「비상 통로」. 화살표는 <dangle>아래를</dangle> 가리키고 있다.<waitfor=0.5> 여기보다 더 아래가 있다는 뜻일까."),
                (0f, 28.4f, 1.2f, "08_EyeRug", "카펫의 눈이 <shake>깜빡였다</shake>.<waitfor=0.8> …아니, <speed=0.7>그렇게 보였을 뿐이다.</speed>"),
                (3.1f, 30.8f, 1.1f, "09_EyeWall", "벽의 눈들이 <wave>일제히</wave> 나를 따라 굴러간다.<waitfor=0.5> <speed=0.5>하나도 빠짐없이.</speed>"),
                (-3.0f, 29.4f, 1.0f, "10_Mannequins", "인형의 관절마다 이름표가 붙어 있다.<waitfor=0.7> 전부 <shake><color=#c4453c>내 이름</color></shake>이다."),
                (0.2f, 33.3f, 0.9f, "11_LockedDoor", "잠겨 있다.<waitfor=0.8> <speed=0.6>문 너머에서 누군가 <wiggle><color=#c4453c>내 목소리로</color></wiggle> 숨을 쉰다.</speed>"),
            };
            foreach (var s in spots)
            {
                var g = Group("Inspect_" + s.name, new Vector3(s.x, 0, s.z), parent.transform);
                var ip = g.AddComponent<InspectPoint>();
                ip.radius = s.r;
                ip.line = s.t;
            }
        }

        // ---------------- 카메라 ----------------

        static void BuildCameras(Transform player, RenderTexture rt)
        {
            var parent = Group("Cameras", Vector3.zero);
            UnityEngine.Camera Cam(string name, Vector3 pos, float fov, bool on)
            {
                var g = Group(name, pos, parent.transform);
                var c = g.AddComponent<UnityEngine.Camera>();
                c.fieldOfView = fov;
                c.nearClipPlane = 0.05f;
                c.farClipPlane = 60f;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = Color.black;
                c.targetTexture = rt;
                c.allowMSAA = false;
                c.allowHDR = false;
                var data = g.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
                c.enabled = on;
                return c;
            }
            var kitchen = Cam("Cam_Kitchen", new Vector3(3.5f, 2.5f, 0.5f), 78, true);
            var corridor = Cam("Cam_Corridor_Rail", new Vector3(0, 1.72f, 8.25f), 64, false);
            corridor.gameObject.AddComponent<RailCamera>().player = player;
            var doll = Cam("Cam_DollRoom", new Vector3(-3.35f, 3.15f, 26.6f), 72, false);
            kitchen.transform.LookAt(new Vector3(-0.4f, 0.1f, 4.6f));
            doll.transform.LookAt(new Vector3(0.3f, 0.5f, 31f));

            void Zone(string name, B58Room room, UnityEngine.Camera cam, Vector3 min, Vector3 max, Vector3 center, float blend)
            {
                var g = Group(name, (min + max) / 2, parent.transform);
                var box = g.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = max - min;
                var z = g.AddComponent<CameraZone>();
                z.room = room; z.shotCamera = cam; z.player = player;
                z.lookCenter = center; z.lookBlend = blend;
            }
            // 경계는 목업 roomOf: z < 7.95 부엌, < 26.05 통로, 그 외 인형의 방
            Zone("Zone_Kitchen", B58Room.Kitchen, kitchen, new Vector3(-4, 0, -0.5f), new Vector3(4, 3, 7.95f), new Vector3(-0.4f, 0.1f, 4.6f), 0.35f);
            Zone("Zone_Corridor", B58Room.Corridor, corridor, new Vector3(-1.3f, 0, 7.95f), new Vector3(1.3f, 3, 26.05f), Vector3.zero, 0f);
            Zone("Zone_DollRoom", B58Room.DollRoom, doll, new Vector3(-4, 0, 26.05f), new Vector3(4, 3.5f, 34.5f), new Vector3(0.3f, 0.5f, 31f), 0.3f);

            // 화면 출력용 카메라 (아무것도 안 찍고 검정으로 지운 뒤 UI만 올라감)
            var disp = Group("DisplayCamera", new Vector3(0, -50, 0), parent.transform);
            var dc = disp.AddComponent<UnityEngine.Camera>();
            dc.cullingMask = 0;
            dc.clearFlags = CameraClearFlags.SolidColor;
            dc.backgroundColor = Color.black;
            dc.depth = 10;
            var dd = disp.AddComponent<UniversalAdditionalCameraData>();
            dd.renderPostProcessing = false;
            disp.AddComponent<AudioListener>();
        }

        // ---------------- UI ----------------

        static TMP_Text Label(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, UseRuntimeFont.Kind font)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            var shadow = g.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);
            g.AddComponent<UseRuntimeFont>().kind = font;
            return t;
        }

        static RectTransform Rect(GameObject g, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var r = (RectTransform)g.transform;
            r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        static GameObject UiNode(string name, Transform parent, params System.Type[] comps)
        {
            var g = new GameObject(name, new[] { typeof(RectTransform) }.Concat(comps).ToArray());
            g.transform.SetParent(parent, false);
            return g;
        }

        static void Stretch(GameObject g) => Rect(g, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);

        static DialogueBox BuildUI(TankController player, RenderTexture rt)
        {
            Color ink = B58ProcTextures.Hex("#cdc8b6"), dim = B58ProcTextures.Hex("#6f6c5f"), amber = B58ProcTextures.Hex("#c98b2c");

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 720);
            scaler.matchWidthOrHeight = 1f;

            // 4:3 화면 영역 (목업 .screen) — 1cqw = 9.6, 1cqh = 7.2
            var screen = UiNode("Screen", canvasGo.transform, typeof(AspectRatioFitter));
            Stretch(screen);
            var fit = screen.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 4f / 3f;

            var game = UiNode("GameView_320x240", screen.transform, typeof(RawImage));
            Stretch(game);
            var gi = game.GetComponent<RawImage>();
            gi.texture = rt;
            gi.raycastTarget = false;

            // HUD
            var hud = UiNode("HUD", screen.transform);
            Stretch(hud);
            var room = UiNode("Room", hud.transform, typeof(VerticalLayoutGroup));
            Rect(room, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(33.6f, -21.6f), new Vector2(400, 80));
            room.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
            var small = Label("Floor", room.transform, "B58 · UNDERLEVEL", 26, dim, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Mono);
            small.characterSpacing = 18;
            var roomName = Label("RoomName", room.transform, "부엌", 32, ink, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Korean);
            roomName.fontStyle = FontStyles.Bold;

            var ecg = UiNode("ECG", hud.transform);
            Rect(ecg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-33.6f, -21.6f), new Vector2(144, 90));
            var graphBg = UiNode("GraphFrame", ecg.transform, typeof(Image));
            Rect(graphBg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(144, 43));
            graphBg.GetComponent<Image>().color = new Color(0.8f, 0.78f, 0.71f, 0.25f);
            var graphInner = UiNode("GraphBg", graphBg.transform, typeof(Image));
            Rect(graphInner, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-2, -2));
            graphInner.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
            var graph = UiNode("Graph", graphInner.transform, typeof(RawImage));
            Stretch(graph);
            var cond = Label("Condition", ecg.transform, "FINE", 33, B58ProcTextures.Hex("#8fa37a"), TextAlignmentOptions.TopRight, UseRuntimeFont.Kind.Mono);
            Rect(cond.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -47), new Vector2(200, 40));
            cond.characterSpacing = 12;
            var ecgHud = ecg.AddComponent<EcgHud>();
            ecgHud.graph = graph.GetComponent<RawImage>();
            ecgHud.label = cond;

            var prompt = UiNode("Prompt", hud.transform, typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            Rect(prompt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 43), new Vector2(160, 46));
            prompt.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            var hl = prompt.GetComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(19, 19, 3, 3);
            hl.childControlWidth = hl.childControlHeight = true;
            var csf = prompt.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var pt = Label("Text", prompt.transform, "[E] 조사", 34, ink, TextAlignmentOptions.Center, UseRuntimeFont.Kind.Mono);
            pt.textWrappingMode = TextWrappingModes.NoWrap;
            prompt.SetActive(false);

            var hint = Label("Hint", hud.transform, "그것은 시선이 닿지 않을 때만 움직인다.", 22, dim, TextAlignmentOptions.BottomLeft, UseRuntimeFont.Kind.Korean);
            Rect(hint.gameObject, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(33.6f, 21.6f), new Vector2(576, 40));
            hint.gameObject.SetActive(false);

            // 대사창
            var dlgRoot = UiNode("Dialogue", hud.transform);
            Stretch(dlgRoot);
            var panel = UiNode("Panel", dlgRoot.transform, typeof(Image));
            Rect(panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(-96, 140));
            panel.GetComponent<Image>().color = B58ProcTextures.Hex("#4c4a40");
            var inner = UiNode("Inner", panel.transform, typeof(Image));
            Rect(inner, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-4, -4));
            inner.GetComponent<Image>().color = new Color(4 / 255f, 5 / 255f, 4 / 255f, 0.86f);
            var dt = Label("Text", inner.transform, "", 33, ink, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Korean);
            Rect(dt.gameObject, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-58, -38));
            dt.lineSpacing = 25;
            dt.gameObject.AddComponent<TextAnimator_TMP>();
            var tw = dt.gameObject.AddComponent<TypewriterComponent>();
            // <waitfor>, <speed> 같은 타이핑 동작용 데이터베이스 (Text Animator 기본 묶음)
            var actions = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Plugins/Febucci/Text Animator for Unity/Actions/Actions Database.asset");
            if (actions)
            {
                var so = new SerializedObject(tw);
                so.FindProperty("actionsDatabase").objectReferenceValue = actions;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var more = Label("More", inner.transform, "▼", 33, amber, TextAlignmentOptions.BottomRight, UseRuntimeFont.Kind.Korean);
            Rect(more.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-23, 10), new Vector2(40, 40));
            var dlg = dlgRoot.AddComponent<DialogueBox>();
            dlg.panel = panel; dlg.text = dt; dlg.typewriter = tw; dlg.moreIndicator = more.gameObject;

            var h = hud.AddComponent<B58Hud>();
            h.player = player; h.roomName = roomName; h.prompt = prompt; h.hint = hint.gameObject;

            // 스캔라인 · 비네트 · 플리커 풀스크린 덮개 (HUD 위)
            var scan = UiNode("ScanlineVignette", screen.transform, typeof(RawImage));
            Stretch(scan);
            var si = scan.GetComponent<RawImage>();
            si.raycastTarget = false;
            si.material = OverlayMaterial();

            // 컷 노이즈, 잡힘 화면 (최상단)
            var cut = UiNode("CutNoise", screen.transform, typeof(RawImage), typeof(CanvasGroup));
            Stretch(cut);
            var ci = cut.GetComponent<RawImage>();
            ci.texture = B58ProcTextures.CutStripes();
            ci.uvRect = new Rect(0, 0, 1, 180);
            ci.raycastTarget = false;
            cut.GetComponent<CanvasGroup>().alpha = 0;

            var hurt = UiNode("Caught", screen.transform, typeof(RawImage), typeof(CanvasGroup));
            Stretch(hurt);
            var hi = hurt.GetComponent<RawImage>();
            hi.texture = B58ProcTextures.HurtGradient();
            hi.raycastTarget = false;
            hurt.GetComponent<CanvasGroup>().alpha = 0;
            var ht = Label("Text", hurt.transform, "들켰다", 86, B58ProcTextures.Hex("#f1d6cf"), TextAlignmentOptions.Center, UseRuntimeFont.Kind.Korean);
            Stretch(ht.gameObject);
            ht.characterSpacing = 20;

            var fx = canvasGo.AddComponent<ScreenFx>();
            fx.cut = cut.GetComponent<CanvasGroup>();
            fx.hurt = hurt.GetComponent<CanvasGroup>();
            return dlg;
        }

        static Material OverlayMaterial()
        {
            string path = MatFolder + "/M_ScreenOverlay.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Liminal/PS1ScreenOverlay"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static void AddToBuildSettings()
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
