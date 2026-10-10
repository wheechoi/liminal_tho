using System.Collections.Generic;
using System.IO;
using System.Linq;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Liminal.Audio;
using Liminal.Camera;
using Liminal.Core;
using Liminal.Interaction;
using Liminal.Player;
using Liminal.Rendering;
using Liminal.Story;
using Liminal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Liminal.Editor
{
    /// <summary>
    /// Surplus Person 씬 1-1 "소희가 잠에서 깨어남" 목업 씬을 처음부터 생성한다.
    /// 메뉴: Surplus Person > Build Scene 1-1. 다시 실행하면 씬을 새로 만든다 (씬에서 손으로 고친 건 사라짐).
    ///
    /// 방 좌표 (미터): x -4~4, z 0~6, 높이 3.2. 북쪽 벽(z=6)에 창문, 서쪽 벽(x=-4)에 침대 머리.
    /// 돌하우스 시점은 남동쪽 위에서 내려다보므로 남쪽·동쪽 벽과 천장을 숨긴다.
    /// </summary>
    public static class SPScene11Builder
    {
        public const string ScenePath = "Assets/_Project/Scenes/SP_1-1_Wake.unity";
        const string MatFolder = "Assets/_Project/Materials/SP";
        const string RenderFolder = "Assets/_Project/Rendering";
        const float H = 3.2f;   // 천장 높이
        const float T = 0.15f;  // 벽 두께

        static Shader ps1;
        static readonly Dictionary<string, Material> MatCache = new();
        static Transform root;
        static Texture2D tWall, tChecker, tWood, tRug, tCurtain, tDuvet, tPlaster, tGrass, tLandscape, tPortrait;

        static Color C(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        [MenuItem("Surplus Person/Build Scene 1-1")]
        public static void Build()
        {
            MatCache.Clear();
            foreach (var f in new[] { "Assets/_Project/Scenes", MatFolder, SPTextures.Folder, RenderFolder })
                Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
            PlayerSettings.productName = "Surplus Person";

            ps1 = Shader.Find("Liminal/PS1Lit");
            tWall = SPTextures.Wallpaper();
            tChecker = SPTextures.Checker();
            tWood = SPTextures.Wood();
            tRug = SPTextures.OvalRug();
            tCurtain = SPTextures.Curtain();
            tDuvet = SPTextures.Duvet();
            tPlaster = SPTextures.Plaster();
            tGrass = SPTextures.Grass();
            tLandscape = SPTextures.PaintingLandscape();
            tPortrait = SPTextures.PaintingPortrait();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var settings = new GameObject("SurplusPerson");
            settings.AddComponent<PS1Settings>().ambient = new Color(0.56f, 0.5f, 0.46f);

            root = new GameObject("World").transform;
            var cutaway = new List<GameObject>();
            BuildRoom(cutaway);
            var bed = BuildBed();
            var clock = BuildNightstand();
            BuildFurniture();
            BuildExterior(cutaway);
            BuildLights();

            var girl = BuildSohee(out var parts);
            var rt = MakeRenderTexture();
            var shots = BuildShots();
            var rig = BuildCamera(girl.transform, rt, shots, cutaway);
            var ui = BuildUI(girl, rig, rt);
            girl.dialogue = ui.dialogue;

            var audioGo = new GameObject("Audio");
            var sound = audioGo.AddComponent<MorningAudio>();

            BuildInspectPoints(rig, ui.dialogue, clock, shots["ClockCloseUp"]);

            var dir = new GameObject("WakeUpDirector").AddComponent<WakeUpDirector>();
            dir.rig = rig;
            dir.anchorShot = shots["Anchor_WindowOutside"];
            dir.player = girl;
            dir.animator = girl.GetComponent<GirlAnimator>();
            dir.sound = sound;
            dir.clock = clock;
            dir.hud = ui.hud;
            dir.dialogue = ui.dialogue;
            dir.skip = Action("Interact");
            dir.pose = parts.pose;
            dir.hipL = parts.hipL; dir.hipR = parts.hipR;
            dir.shoulderL = parts.shoulderL; dir.shoulderR = parts.shoulderR;
            dir.duvet = bed;
            dir.duvetFoldedScale = new Vector3(0.6f, 0.2f, 1.6f);
            dir.duvetFoldedOffset = new Vector3(0.45f, 0.03f, 0f);

            // 시작 자세: 침대에 누운 상태 (재생 전 에디터에서도 그렇게 보이게)
            girl.transform.SetPositionAndRotation(dir.bedPos, Quaternion.Euler(0, dir.bedYaw, 0));
            parts.pose.localRotation = Quaternion.Euler(-90f, 0, 0);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Surplus Person] 씬 1-1 생성 완료: " + ScenePath);
        }

        // ---------------- 머티리얼 ----------------

        static Material Tex(Texture2D t, float rx = 1, float ry = 1, float cutoff = 0f)
        {
            string key = $"M_{t.name.Replace("T_SP_", "")}_{rx:0.##}x{ry:0.##}";
            return GetMat(key, m =>
            {
                m.SetTexture("_BaseMap", t);
                m.SetTextureScale("_BaseMap", new Vector2(rx, ry));
                m.SetFloat("_Cutoff", cutoff);
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
            m.SetVector("_SnapRes", new Vector4(480, 360, 0, 0)); // 화면 픽셀에 맞춰 스냅 (흔들림 없음)
            setup(m);
            EditorUtility.SetDirty(m);
            MatCache[key] = m;
            return m;
        }

        // ---------------- 도형 ----------------

        static GameObject Prim(PrimitiveType type, string name, Material m, Vector3 pos, Vector3 scale, Transform parent, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent ? parent : root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>최소/최대 좌표로 상자 만들기.</summary>
        static GameObject Box(string name, Material m, Vector3 min, Vector3 max, Transform p = null, bool collider = false)
            => Prim(PrimitiveType.Cube, name, m, (min + max) / 2, max - min, p, collider);

        static GameObject Cyl(string name, Material m, Vector3 center, float radius, float height, Transform p = null, bool collider = false)
            => Prim(PrimitiveType.Cylinder, name, m, center, new Vector3(radius * 2, height / 2, radius * 2), p, collider);

        static GameObject Ball(string name, Material m, Vector3 center, Vector3 size, Transform p = null)
            => Prim(PrimitiveType.Sphere, name, m, center, size, p, false);

        static GameObject Quad(string name, Material m, Vector3 center, Vector2 size, Quaternion rot, Transform p = null)
        {
            var q = Prim(PrimitiveType.Quad, name, m, center, new Vector3(size.x, size.y, 1), p, false);
            q.transform.localRotation = rot;
            return q;
        }

        static Transform Group(string name, Transform parent = null, Vector3 pos = default)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent ? parent : root, false);
            g.localPosition = pos;
            return g;
        }

        /// <summary>벽지 벽 (텍스처 반복은 크기에 맞춤: 가로 0.8m, 세로 1.6m).</summary>
        static GameObject WallBox(string name, Vector3 min, Vector3 max, Transform p)
        {
            Vector3 s = max - min;
            return Box(name, Tex(tWall, Mathf.Max(s.x, s.z) / 0.8f, s.y / 1.6f), min, max, p, true);
        }

        // ---------------- 방 ----------------

        static void BuildRoom(List<GameObject> cutaway)
        {
            var room = Group("Bedroom");
            var hide = Group("Cutaway_HiddenInDollhouse", room);
            cutaway.Add(hide.gameObject);

            // 바닥 (흑백 체크) + 돌하우스 받침
            Quad("Floor", Tex(tChecker, 8, 6), new Vector3(0, 0, 3), new Vector2(8, 6), Quaternion.Euler(90, 0, 0), room);
            Box("FloorSlab", Col(0x5a3a2c), new Vector3(-4 - T, -0.4f, -T), new Vector3(4 + T, -0.03f, 6 + T), room); // 바닥과 겹치면 깜빡이므로 3cm 아래
            var ground = new GameObject("GroundCollider").AddComponent<BoxCollider>();
            ground.transform.SetParent(room, false);
            ground.center = new Vector3(0, -0.25f, 3);
            ground.size = new Vector3(8, 0.5f, 6);

            // 북쪽 벽 (창문 x -3.45~-1.95, y 0.85~2.35)
            float wx0 = -3.45f, wx1 = -1.95f, wy0 = 0.85f, wy1 = 2.35f;
            var north = Group("Wall_North", room);
            WallBox("Left", new Vector3(-4 - T, 0, 6), new Vector3(wx0, H, 6 + T), north);
            WallBox("Right", new Vector3(wx1, 0, 6), new Vector3(4 + T, H, 6 + T), north);
            WallBox("BelowWindow", new Vector3(wx0, 0, 6), new Vector3(wx1, wy0, 6 + T), north);
            WallBox("AboveWindow", new Vector3(wx0, wy1, 6), new Vector3(wx1, H, 6 + T), north);
            // 바깥면 회벽
            var plaster = Tex(tPlaster, 4, 2);
            Box("Ext_Left", plaster, new Vector3(-4 - T, 0, 6 + T), new Vector3(wx0, H, 6 + T + 0.02f), north);
            Box("Ext_Right", plaster, new Vector3(wx1, 0, 6 + T), new Vector3(4 + T, H, 6 + T + 0.02f), north);
            Box("Ext_Below", plaster, new Vector3(wx0, 0, 6 + T), new Vector3(wx1, wy0, 6 + T + 0.02f), north);
            Box("Ext_Above", plaster, new Vector3(wx0, wy1, 6 + T), new Vector3(wx1, H, 6 + T + 0.02f), north);
            Box("Ext_Sill", Col(0xe7e0d2), new Vector3(wx0 - 0.08f, wy0 - 0.06f, 6 + T), new Vector3(wx1 + 0.08f, wy0, 6 + T + 0.12f), north);

            // 서쪽 벽
            var west = Group("Wall_West", room);
            WallBox("Wall", new Vector3(-4 - T, 0, -T), new Vector3(-4, H, 6 + T), west);

            // 남쪽·동쪽 벽, 천장 (돌하우스 시점에서 숨김)
            WallBox("Wall_South", new Vector3(-4 - T, 0, -T), new Vector3(4 + T, H, 0), hide);
            WallBox("Wall_East", new Vector3(4, 0, -T), new Vector3(4 + T, H, 6 + T), hide);
            Box("Door_East", Tex(tWood, 1, 2), new Vector3(3.97f, 0, 0.6f), new Vector3(4f, 2.2f, 1.6f), hide);
            Ball("DoorKnob", Col(0xc9a24a), new Vector3(3.94f, 1.0f, 1.45f), Vector3.one * 0.07f, hide);
            Quad("Ceiling", Col(0xf1e9dc), new Vector3(0, H, 3), new Vector2(8, 6), Quaternion.Euler(-90, 0, 0), hide);

            // 징두리 판벽 (짙은 목재, 높이 0.9) + 몰딩
            var wood = Tex(tWood, 8, 1);
            var rail = Col(0x4a2f25);
            Box("Wainscot_N", wood, new Vector3(-4, 0, 5.96f), new Vector3(4, 0.9f, 6), room);
            Box("Rail_N", rail, new Vector3(-4, 0.9f, 5.94f), new Vector3(4, 0.96f, 6), room);
            Box("Crown_N", rail, new Vector3(-4, H - 0.1f, 5.94f), new Vector3(4, H, 6), room);
            Box("Wainscot_W", wood, new Vector3(-4, 0, 0), new Vector3(-3.96f, 0.9f, 6), room);
            Box("Rail_W", rail, new Vector3(-4, 0.9f, 0), new Vector3(-3.94f, 0.96f, 6), room);
            Box("Crown_W", rail, new Vector3(-4, H - 0.1f, 0), new Vector3(-3.94f, H, 6), room);
            Box("Wainscot_S", wood, new Vector3(-4, 0, 0), new Vector3(4, 0.9f, 0.04f), hide);
            Box("Wainscot_E", wood, new Vector3(3.96f, 0, 0), new Vector3(4, 0.9f, 6), hide);

            // 창틀 + 커튼 (커튼 틈 x -3.03~-2.67: 앵커 컷이 이 틈으로 소희를 본다. 누운 자세~기지개까지 틈 안에 들어오게 맞춤)
            var win = Group("Window", room);
            var frame = Col(0xf1ece2);
            Box("Sill", frame, new Vector3(wx0 - 0.1f, wy0 - 0.05f, 5.75f), new Vector3(wx1 + 0.1f, wy0, 6.02f), win);
            Box("FrameL", frame, new Vector3(wx0, wy0, 5.97f), new Vector3(wx0 + 0.05f, wy1, 6.05f), win);
            Box("FrameR", frame, new Vector3(wx1 - 0.05f, wy0, 5.97f), new Vector3(wx1, wy1, 6.05f), win);
            Box("FrameTop", frame, new Vector3(wx0, wy1 - 0.05f, 5.97f), new Vector3(wx1, wy1, 6.05f), win);
            Box("MullionV", frame, new Vector3(-2.47f, wy0, 6.0f), new Vector3(-2.43f, wy1, 6.04f), win); // 오른쪽 커튼 뒤
            var curtain = Tex(tCurtain, 3, 1);
            Box("Curtain_L", curtain, new Vector3(-3.7f, 0.12f, 5.78f), new Vector3(-3.03f, 2.52f, 5.84f), win);
            Box("Curtain_R", curtain, new Vector3(-2.67f, 0.12f, 5.78f), new Vector3(-1.7f, 2.52f, 5.84f), win);
            Box("Valance", Col(0x7e464b), new Vector3(-3.8f, 2.48f, 5.74f), new Vector3(-1.6f, 2.75f, 5.86f), win);
            Box("Rod", Col(0xc9a24a), new Vector3(-3.85f, 2.55f, 5.8f), new Vector3(-1.55f, 2.58f, 5.83f), win);
        }

        /// <summary>침대 (머리는 서쪽 벽). 반환: 이불 (일어날 때 접힘).</summary>
        static Transform BuildBed()
        {
            var g = Group("Bed");
            var wood = Tex(tWood, 1, 1);
            Box("Frame", wood, new Vector3(-3.95f, 0.15f, 2.85f), new Vector3(-1.75f, 0.45f, 4.45f), g, true);
            foreach (var x in new[] { -3.9f, -1.8f })
            foreach (var z in new[] { 2.9f, 4.4f })
                Box("Leg", Col(0x3e2820), new Vector3(x - 0.04f, 0, z - 0.04f), new Vector3(x + 0.04f, 0.15f, z + 0.04f), g);
            Box("Mattress", Col(0xf2eee6), new Vector3(-3.9f, 0.45f, 2.9f), new Vector3(-1.8f, 0.62f, 4.4f), g);
            Box("Headboard", wood, new Vector3(-3.97f, 0, 2.85f), new Vector3(-3.89f, 1.35f, 4.45f), g, true);
            for (int i = 0; i < 6; i++)
                Box("Spindle", Col(0x4a2f25), new Vector3(-3.95f, 0.75f, 3.05f + i * 0.24f), new Vector3(-3.87f, 1.25f, 3.1f + i * 0.24f), g);
            foreach (var z in new[] { 2.85f, 4.45f })
            {
                Cyl("Post", Col(0x4a2f25), new Vector3(-3.93f, 0.8f, z), 0.05f, 1.6f, g);
                Ball("Finial", Col(0xc9a24a), new Vector3(-3.93f, 1.65f, z), Vector3.one * 0.12f, g);
            }
            Box("Footboard", wood, new Vector3(-1.8f, 0, 2.85f), new Vector3(-1.72f, 0.8f, 4.45f), g, true);
            Box("Pillow", Col(0xf6efe4), new Vector3(-3.85f, 0.62f, 3.3f), new Vector3(-3.45f, 0.74f, 4.0f), g);
            var duvet = Box("Duvet", Tex(tDuvet, 2, 2), new Vector3(-3.25f, 0.62f, 2.87f), new Vector3(-1.8f, 0.76f, 4.43f), g);
            return duvet.transform;
        }

        /// <summary>협탁 + 스탠드 + 알람시계.</summary>
        static AlarmClock BuildNightstand()
        {
            var g = Group("Nightstand");
            Box("Body", Tex(tWood, 1, 0.5f), new Vector3(-3.9f, 0, 4.6f), new Vector3(-3.3f, 0.6f, 5.1f), g, true);
            Box("Drawer", Col(0x8a5f48), new Vector3(-3.3f, 0.32f, 4.65f), new Vector3(-3.28f, 0.5f, 5.05f), g);
            Ball("Knob", Col(0xc9a24a), new Vector3(-3.27f, 0.41f, 4.85f), Vector3.one * 0.04f, g);
            Cyl("LampBase", Col(0xc9a24a), new Vector3(-3.75f, 0.75f, 5.0f), 0.04f, 0.3f, g);
            Cyl("LampShade", Glow(0xf3dfb8), new Vector3(-3.75f, 0.98f, 5.0f), 0.13f, 0.2f, g);

            var clockGo = Group("AlarmClock", g, new Vector3(-3.55f, 0.66f, 4.75f));
            Box("Case", Col(0xefe6d6), new Vector3(-0.045f, -0.06f, -0.11f), new Vector3(0.045f, 0.06f, 0.11f), clockGo);
            Box("Face", Col(0x1b1414), new Vector3(0.045f, -0.035f, -0.085f), new Vector3(0.05f, 0.04f, 0.085f), clockGo);
            Box("Bell", Col(0xc9a24a), new Vector3(-0.02f, 0.06f, -0.03f), new Vector3(0.02f, 0.08f, 0.03f), clockGo);
            var textGo = new GameObject("Display", typeof(RectTransform));
            textGo.transform.SetParent(clockGo, false);
            textGo.transform.localPosition = new Vector3(0.052f, 0.003f, 0);
            textGo.transform.localRotation = Quaternion.Euler(0, -90, 0);
            var tmp = textGo.AddComponent<TextMeshPro>();
            ((RectTransform)textGo.transform).sizeDelta = new Vector2(0.16f, 0.07f);
            tmp.text = "10:34";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = C(0xff5a40);
            tmp.fontStyle = FontStyles.Bold;
            var clock = clockGo.gameObject.AddComponent<AlarmClock>();
            clock.display = tmp;
            return clock;
        }

        static void BuildFurniture()
        {
            // 옷장 (북쪽 벽 오른쪽)
            var wr = Group("Wardrobe");
            Box("Body", Tex(tWood, 2, 2), new Vector3(1.2f, 0, 5.3f), new Vector3(3.2f, 2.35f, 5.9f), wr, true);
            Box("Crown", Col(0x3e2820), new Vector3(1.15f, 2.35f, 5.25f), new Vector3(3.25f, 2.48f, 5.92f), wr);
            foreach (var x in new[] { 1.27f, 2.23f })
            {
                Box("Door", Col(0x8a5f48), new Vector3(x, 0.15f, 5.27f), new Vector3(x + 0.9f, 2.25f, 5.3f), wr);
                Box("Panel", Col(0x6a4636), new Vector3(x + 0.12f, 0.35f, 5.26f), new Vector3(x + 0.78f, 2.05f, 5.27f), wr);
            }
            Ball("Knob", Col(0xc9a24a), new Vector3(2.12f, 1.2f, 5.24f), Vector3.one * 0.05f, wr);
            Ball("Knob", Col(0xc9a24a), new Vector3(2.28f, 1.2f, 5.24f), Vector3.one * 0.05f, wr);

            // 화장대 + 거울 (서쪽 벽 앞쪽)
            var va = Group("Vanity");
            Box("Top", Tex(tWood, 1, 0.5f), new Vector3(-3.95f, 0.7f, 0.5f), new Vector3(-3.4f, 0.76f, 1.7f), va, true);
            foreach (var z in new[] { 0.55f, 1.65f })
                Box("Leg", Col(0x4a2f25), new Vector3(-3.47f, 0, z - 0.03f), new Vector3(-3.42f, 0.7f, z + 0.03f), va);
            Box("Drawers", Col(0x8a5f48), new Vector3(-3.95f, 0.45f, 0.55f), new Vector3(-3.42f, 0.7f, 1.65f), va);
            Box("MirrorFrame", Col(0xb8964f), new Vector3(-3.98f, 0.95f, 0.72f), new Vector3(-3.93f, 2.05f, 1.48f), va);
            Box("Mirror", Col(0xc8d5d9), new Vector3(-3.94f, 1.02f, 0.79f), new Vector3(-3.91f, 1.98f, 1.41f), va);
            Ball("Perfume", Col(0xe7b8c0), new Vector3(-3.7f, 0.82f, 0.7f), new Vector3(0.06f, 0.1f, 0.06f), va);
            Cyl("Stool", Col(0x9b4f57), new Vector3(-3.0f, 0.22f, 1.1f), 0.2f, 0.44f, va, true);

            // 서랍장 + 꽃병 (북쪽 벽 가운데)
            var dr = Group("Dresser");
            Box("Body", Tex(tWood, 1, 1), new Vector3(-0.9f, 0, 5.5f), new Vector3(0.5f, 0.95f, 5.92f), dr, true);
            for (int i = 0; i < 3; i++)
                Box("Drawer", Col(0x8a5f48), new Vector3(-0.85f, 0.08f + i * 0.29f, 5.48f), new Vector3(0.45f, 0.32f + i * 0.29f, 5.5f), dr);
            Cyl("Vase", Col(0xe9e3d8), new Vector3(-0.5f, 1.07f, 5.7f), 0.07f, 0.24f, dr);
            foreach (var (dx, c) in new[] { (-0.06f, 0xe7a3b0), (0.05f, 0xf2e2d6), (0f, 0xd16f80) })
                Ball("Flower", Col(c), new Vector3(-0.5f + dx, 1.27f + Mathf.Abs(dx), 5.7f), Vector3.one * 0.09f, dr);
            Ball("Leaves", Col(0x6f9356), new Vector3(-0.5f, 1.2f, 5.7f), new Vector3(0.22f, 0.12f, 0.16f), dr);

            // 액자
            void Painting(string name, Texture2D t, Vector3 center, Vector2 size, Quaternion rot)
            {
                var p = Group(name);
                p.localPosition = center;
                p.localRotation = rot;
                Box("Frame", Col(0xb8964f), new Vector3(-size.x / 2 - 0.06f, -size.y / 2 - 0.06f, 0), new Vector3(size.x / 2 + 0.06f, size.y / 2 + 0.06f, 0.04f), p);
                Quad("Canvas", Tex(t), new Vector3(0, 0, -0.005f), size, Quaternion.identity, p);
            }
            Painting("Painting_Landscape", tLandscape, new Vector3(-0.2f, 1.9f, 5.94f), new Vector2(0.9f, 0.68f), Quaternion.identity);
            Painting("Painting_Portrait", tPortrait, new Vector3(-3.94f, 1.95f, 5.3f), new Vector2(0.45f, 0.6f), Quaternion.Euler(0, -90, 0));

            // 안락의자 + 둥근 탁자 + 스탠드 (동쪽)
            var ch = Group("Armchair", null, new Vector3(2.8f, 0, 2.0f));
            ch.localRotation = Quaternion.Euler(0, -120, 0);
            var velvet = Col(0x9b4f57);
            Box("Seat", velvet, new Vector3(-0.4f, 0.2f, -0.4f), new Vector3(0.4f, 0.45f, 0.4f), ch, true);
            Box("Back", velvet, new Vector3(-0.4f, 0.45f, -0.45f), new Vector3(0.4f, 1.05f, -0.28f), ch);
            Box("ArmL", velvet, new Vector3(-0.48f, 0.2f, -0.4f), new Vector3(-0.36f, 0.65f, 0.4f), ch);
            Box("ArmR", velvet, new Vector3(0.36f, 0.2f, -0.4f), new Vector3(0.48f, 0.65f, 0.4f), ch);
            foreach (var (x, z) in new[] { (-0.38f, -0.38f), (0.38f, -0.38f), (-0.38f, 0.38f), (0.38f, 0.38f) })
                Box("Leg", Col(0x3e2820), new Vector3(x - 0.03f, 0, z - 0.03f), new Vector3(x + 0.03f, 0.2f, z + 0.03f), ch);
            Cyl("SideTable", Tex(tWood), new Vector3(3.45f, 0.55f, 3.1f), 0.28f, 0.04f, null, true);
            Cyl("SideTableLeg", Col(0x3e2820), new Vector3(3.45f, 0.27f, 3.1f), 0.04f, 0.54f);
            Cyl("Teacup", Col(0xf6efe4), new Vector3(3.4f, 0.61f, 3.05f), 0.05f, 0.07f);
            Cyl("FloorLampPole", Col(0xc9a24a), new Vector3(3.55f, 0.8f, 0.6f), 0.025f, 1.6f, null, true);
            Cyl("FloorLampShade", Glow(0xf3dfb8), new Vector3(3.55f, 1.65f, 0.6f), 0.2f, 0.3f);

            // 화분 (창 오른쪽)
            Cyl("Pot", Col(0xb5694a), new Vector3(-1.35f, 0.2f, 5.55f), 0.18f, 0.4f, null, true);
            Ball("Plant", Col(0x5f8a4a), new Vector3(-1.35f, 0.7f, 5.55f), new Vector3(0.5f, 0.7f, 0.5f));
            Ball("Plant", Col(0x739e57), new Vector3(-1.25f, 0.95f, 5.5f), new Vector3(0.3f, 0.4f, 0.3f));

            // 러그 (타원) + 샹들리에
            Quad("Rug", Tex(tRug, 1, 1, 0.5f), new Vector3(0.2f, 0.012f, 2.6f), new Vector2(3.4f, 2.5f), Quaternion.Euler(90, 0, 0));
            var cd = Group("Chandelier", null, new Vector3(0.2f, 2.65f, 2.6f));
            Box("Cord", Col(0x3a2a20), new Vector3(-0.01f, 0.1f, -0.01f), new Vector3(0.01f, H - 2.65f, 0.01f), cd);
            Cyl("Ring", Col(0xc9a24a), Vector3.zero, 0.38f, 0.04f, cd);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var p = new Vector3(Mathf.Cos(a) * 0.34f, 0.08f, Mathf.Sin(a) * 0.34f);
                Cyl("Candle", Col(0xf6efe4), p, 0.025f, 0.14f, cd);
                Ball("Bulb", Glow(0xffe7b0), p + Vector3.up * 0.1f, Vector3.one * 0.06f, cd);
            }
        }

        /// <summary>창밖: 잔디, 산울타리, 나무 (돌하우스 시점에서는 숨김).</summary>
        static void BuildExterior(List<GameObject> cutaway)
        {
            var ext = Group("Exterior_Garden");
            cutaway.Add(ext.gameObject);
            Quad("Lawn", Tex(tGrass, 12, 7), new Vector3(0, -0.02f, 13), new Vector2(30, 14), Quaternion.Euler(90, 0, 0), ext);
            Box("Hedge", Col(0x55794a), new Vector3(-12, 0, 11), new Vector3(12, 1.1f, 11.8f), ext);
            void Tree(float x, float z, float s)
            {
                Cyl("Trunk", Col(0x6a4636), new Vector3(x, 1.2f * s, z), 0.15f * s, 2.4f * s, ext);
                Ball("Canopy", Col(0x6f9356), new Vector3(x, 3.0f * s, z), new Vector3(2.4f, 2.2f, 2.4f) * s, ext);
            }
            Tree(-6.5f, 9.5f, 1.1f);
            Tree(3.5f, 13.5f, 1.3f);
            Tree(-1.5f, 15f, 0.9f);
        }

        static void BuildLights()
        {
            var g = Group("Lights");
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(g, false);
            sun.type = LightType.Directional;
            sun.color = C(0xfff1d6);
            sun.intensity = 0.75f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(48f, 157f, 0f); // 북쪽 창에서 들어오는 아침 햇살
            void Point(string name, int hex, float intensity, float range, Vector3 pos)
            {
                var l = new GameObject(name).AddComponent<Light>();
                l.transform.SetParent(g, false);
                l.transform.position = pos;
                l.type = LightType.Point;
                l.color = C(hex);
                l.intensity = intensity;
                l.range = range;
                l.shadows = LightShadows.None;
            }
            Point("WindowGlow", 0xe6f1ff, 1.2f, 5f, new Vector3(-2.7f, 1.9f, 5.2f));
            Point("Chandelier", 0xffe2b0, 1.0f, 7f, new Vector3(0.2f, 2.5f, 2.6f));
            Point("BedsideLamp", 0xffd59a, 0.8f, 3f, new Vector3(-3.6f, 1.1f, 4.9f));
            Point("FloorLamp", 0xffd59a, 0.7f, 3.5f, new Vector3(3.4f, 1.6f, 0.8f));
        }

        // ---------------- 소희 ----------------

        public struct SoheeParts { public Transform pose, hipL, hipR, shoulderL, shoulderR; }

        static InputActionReference Action(string name)
        {
            var acts = AssetDatabase.LoadAllAssetsAtPath("Assets/InputSystem_Actions.inputactions").OfType<InputActionReference>();
            var a = acts.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "Tank" && r.action.name == name);
            if (a == null) Debug.LogWarning("[Surplus Person] 입력 액션 없음: Tank/" + name);
            return a;
        }

        /// <summary>
        /// 목업용 소희: 기본 도형 조합 (잠옷 차림, 긴 흑발).
        /// 계층: Sohee(조작) → Pose(엉덩이 높이 회전축, 눕기/앉기) → Body → 부위.
        /// </summary>
        static TankController BuildSohee(out SoheeParts parts)
        {
            var girl = new GameObject("Sohee");
            girl.tag = "Player";
            var cc = girl.AddComponent<CharacterController>();
            cc.height = 1.4f; cc.radius = 0.25f; cc.center = new Vector3(0, 0.72f, 0); cc.skinWidth = 0.02f; cc.stepOffset = 0.2f;
            cc.enabled = false; // 연출이 끝나면 켠다
            var rb = girl.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var tank = girl.AddComponent<TankController>();
            tank.move = Action("Move"); tank.run = Action("Run"); tank.interact = Action("Interact"); tank.quickTurn = Action("QuickTurn");
            tank.walkSpeed = 1.5f; // 집 안이라 조금 느긋하게
            tank.enabled = false;

            var pose = Group("Pose", girl.transform, new Vector3(0, 0.56f, 0));
            var body = Group("Body", pose, new Vector3(0, -0.56f, 0));
            Material skin = Col(0xead6c8), hair = Col(0x1d1716), pj = Col(0xe6c0bf), pjDark = Col(0xc78f93), eye = Col(0x1b1414);
            var hips = new Transform[2];
            var shoulders = new Transform[2];
            int k = 0;
            foreach (var s in new[] { -1, 1 })
            {
                var hip = Group(s < 0 ? "HipL" : "HipR", body, new Vector3(s * 0.075f, 0.56f, 0));
                Box("Leg", pj, new Vector3(-0.045f, -0.48f, -0.045f), new Vector3(0.045f, 0f, 0.045f), hip);
                Box("Cuff", pjDark, new Vector3(-0.047f, -0.5f, -0.047f), new Vector3(0.047f, -0.46f, 0.047f), hip);
                Box("Foot", skin, new Vector3(-0.04f, -0.56f, -0.04f), new Vector3(0.04f, -0.5f, 0.1f), hip);
                var sh = Group(s < 0 ? "ShoulderL" : "ShoulderR", body, new Vector3(s * 0.19f, 1.12f, 0));
                Box("Sleeve", pj, new Vector3(-0.037f, -0.25f, -0.037f), new Vector3(0.037f, 0.01f, 0.037f), sh);
                Box("Hand", skin, new Vector3(-0.03f, -0.42f, -0.03f), new Vector3(0.03f, -0.25f, 0.03f), sh);
                sh.localRotation = Quaternion.Euler(0, 0, s * 0.08f * Mathf.Rad2Deg);
                hips[k] = hip; shoulders[k] = sh; k++;
            }
            Cyl("Pants", pj, new Vector3(0, 0.64f, 0), 0.17f, 0.22f, body);
            Cyl("Top", pj, new Vector3(0, 0.95f, 0), 0.155f, 0.4f, body);
            Box("Collar", pjDark, new Vector3(-0.08f, 1.12f, 0.1f), new Vector3(0.08f, 1.16f, 0.15f), body);
            Box("Buttons", pjDark, new Vector3(-0.012f, 0.8f, 0.152f), new Vector3(0.012f, 1.1f, 0.158f), body);
            Box("Neck", skin, new Vector3(-0.03f, 1.15f, -0.03f), new Vector3(0.03f, 1.22f, 0.03f), body);
            Ball("Head", skin, new Vector3(0, 1.32f, 0), Vector3.one * 0.25f, body);
            Ball("Hair", hair, new Vector3(0, 1.35f, -0.025f), Vector3.one * 0.28f, body);
            Box("HairBack", hair, new Vector3(-0.145f, 0.92f, -0.13f), new Vector3(0.145f, 1.36f, 0f), body); // 허리까지 오는 긴 머리
            Box("Bangs", hair, new Vector3(-0.125f, 1.39f, 0.07f), new Vector3(0.125f, 1.45f, 0.12f), body);
            foreach (var s in new[] { -1, 1 })
            {
                Box("HairSide", hair, new Vector3(s * 0.125f - 0.025f, 1.0f, -0.0f), new Vector3(s * 0.125f + 0.025f, 1.36f, 0.08f), body);
                Box("Eye", eye, new Vector3(s * 0.045f - 0.0125f, 1.32f, 0.118f), new Vector3(s * 0.045f + 0.0125f, 1.34f, 0.128f), body);
            }

            var anim = girl.AddComponent<GirlAnimator>();
            anim.controller = tank;
            anim.body = body;
            anim.leftHip = hips[0]; anim.rightHip = hips[1];
            anim.leftShoulder = shoulders[0]; anim.rightShoulder = shoulders[1];
            anim.enabled = false;

            parts = new SoheeParts { pose = pose, hipL = hips[0], hipR = hips[1], shoulderL = shoulders[0], shoulderR = shoulders[1] };
            return tank;
        }

        // ---------------- 카메라 ----------------

        static RenderTexture MakeRenderTexture()
        {
            string path = RenderFolder + "/SP_GameView.renderTexture";
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (!rt)
            {
                rt = new RenderTexture(480, 360, 24, RenderTextureFormat.ARGB32) { name = "SP_GameView" };
                AssetDatabase.CreateAsset(rt, path);
            }
            rt.filterMode = FilterMode.Point;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            EditorUtility.SetDirty(rt);
            return rt;
        }

        static Dictionary<string, Transform> BuildShots()
        {
            var g = Group("Shots");
            var d = new Dictionary<string, Transform>();
            Transform Shot(string name, Vector3 pos, Vector3 look)
            {
                var t = Group(name, g, pos);
                t.rotation = Quaternion.LookRotation(look - pos);
                d[name] = t;
                return t;
            }
            // ② 앵커 컷: 창밖에서 커튼 틈으로 침대의 소희를 들여다본다. 엔딩에서 그대로 재사용.
            Shot("Anchor_WindowOutside", new Vector3(-2.62f, 2.05f, 7.0f), new Vector3(-3.2f, 0.75f, 3.4f));
            // 돌하우스: 남동쪽 위에서 비스듬히 (직교)
            var center = new Vector3(0.2f, 1.5f, 3.0f);
            Shot("Dollhouse", center + new Vector3(1f, 1.05f, -1f).normalized * 25f, center);
            // 구석 고정 카메라: 남동쪽 천장 모서리
            Shot("Corner_SE", new Vector3(3.65f, 2.75f, 0.35f), new Vector3(-1.2f, 0.5f, 3.8f));
            // 알람시계 클로즈업
            Shot("ClockCloseUp", new Vector3(-3.0f, 0.8f, 4.74f), new Vector3(-3.55f, 0.55f, 4.76f));
            return d;
        }

        static ShotRig BuildCamera(Transform player, RenderTexture rt, Dictionary<string, Transform> shots, List<GameObject> cutaway)
        {
            var camGo = new GameObject("GameCamera");
            var c = camGo.AddComponent<UnityEngine.Camera>();
            c.nearClipPlane = 0.05f;
            c.farClipPlane = 80f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            c.targetTexture = rt;
            c.allowMSAA = false;
            c.allowHDR = false;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;

            var rig = camGo.AddComponent<ShotRig>();
            rig.cam = c;
            rig.player = player;
            rig.toggleView = Action("ToggleView");
            rig.dollhouseShot = shots["Dollhouse"];
            rig.cornerShot = shots["Corner_SE"];
            rig.cutawayHide = cutaway.ToArray();
            // 첫 프레임 구도 = 앵커 컷
            camGo.transform.SetPositionAndRotation(shots["Anchor_WindowOutside"].position, shots["Anchor_WindowOutside"].rotation);
            c.fieldOfView = 38f;

            // 화면 출력용 카메라 (아무것도 안 찍고 UI만)
            var disp = new GameObject("DisplayCamera");
            disp.transform.position = new Vector3(0, -50, 0);
            var dc = disp.AddComponent<UnityEngine.Camera>();
            dc.cullingMask = 0;
            dc.clearFlags = CameraClearFlags.SolidColor;
            dc.backgroundColor = Color.black;
            dc.depth = 10;
            disp.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            disp.AddComponent<AudioListener>();
            return rig;
        }

        // ---------------- 조사 지점 ----------------

        static void BuildInspectPoints(ShotRig rig, DialogueBox dlg, AlarmClock clock, Transform clockCloseUp)
        {
            var parent = Group("InspectPoints");
            StoryInspectPoint Spot(string id, Vector3 pos, float radius, params string[] lines)
            {
                var g = Group("Inspect_" + id, parent, pos);
                var p = g.gameObject.AddComponent<StoryInspectPoint>();
                p.id = id;
                p.radius = radius;
                p.lines = lines;
                p.line = lines[0];
                p.group = "bedroom";
                p.rig = rig;
                p.dialogue = dlg;
                return p;
            }

            // 1) 알람시계 — 침실을 다 둘러본 뒤 다시 보면 6시 56분 (첫 번째 진엔딩 조건)
            var c = Spot("01_AlarmClock", new Vector3(-3.55f, 0, 4.75f), 1.0f, "“10시 34분. 오늘은 늦잠 좀 잤네.”");
            c.hasSecret = true;
            c.secretLines = new[] { "<speed=0.6>“…<waitfor=0.6>아직도 6시 56분이네.”" };
            c.secretFlag = StoryFlags.TrueEndClock;
            c.closeUp = clockCloseUp;
            c.closeUpFov = 40f;
            UnityEventTools.AddPersistentListener(c.onSecretShown, clock.ShowRealTime);
            UnityEventTools.AddPersistentListener(c.onSecretClosed, clock.ShowNormalTime);

            // 2) 창문
            Spot("02_Window", new Vector3(-2.2f, 0, 5.55f), 0.8f, "“날씨 좋다.”", "하늘이 구름 한 점 없이 푸르다.");
            // 3) 옷장
            Spot("03_Wardrobe", new Vector3(2.2f, 0, 4.85f), 0.9f, "“입을 옷이 너무 많아서 고민이야.<waitfor=0.4> 내일도 쇼핑을 나가기로 했어.”");
            // 4) 거울
            Spot("04_Mirror", new Vector3(-3.15f, 0, 1.1f), 0.9f, "“…어라.<waitfor=0.5> 오늘따라 얼굴이 창백하네.<waitfor=0.4> 아프진 않은데…”");
        }

        // ---------------- UI ----------------

        struct UiRefs { public DialogueBox dialogue; public SurplusHud hud; }

        static GameObject UiNode(string name, Transform parent, params System.Type[] comps)
        {
            var g = new GameObject(name, new[] { typeof(RectTransform) }.Concat(comps).ToArray());
            g.transform.SetParent(parent, false);
            return g;
        }

        static RectTransform Rect(GameObject g, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var r = (RectTransform)g.transform;
            r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        static void Stretch(GameObject g, float inset = 0) => Rect(g, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-inset * 2, -inset * 2));

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
            g.AddComponent<UseRuntimeFont>().kind = font;
            return t;
        }

        static UiRefs BuildUI(TankController player, ShotRig rig, RenderTexture rt)
        {
            Color cream = B58ProcTextures.Hex("#efe3cf"), dim = B58ProcTextures.Hex("#b7a58f"), rose = B58ProcTextures.Hex("#b98f8a");

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 720);
            scaler.matchWidthOrHeight = 1f;

            // 4:3 화면
            var screen = UiNode("Screen", canvasGo.transform, typeof(AspectRatioFitter));
            Stretch(screen);
            var fit = screen.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 4f / 3f;

            var view = UiNode("GameView_480x360", screen.transform, typeof(RawImage));
            Stretch(view);
            var vi = view.GetComponent<RawImage>();
            vi.texture = rt;
            vi.raycastTarget = false;
            vi.material = IllustratedMaterial();

            // HUD (컷신 중 숨김)
            var hud = UiNode("HUD", screen.transform, typeof(CanvasGroup));
            Stretch(hud);
            var hudGroup = hud.GetComponent<CanvasGroup>();
            hudGroup.alpha = 0f;
            var title = Label("Title", hud.transform, "SURPLUS PERSON", 18, dim, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Serif);
            Rect(title.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -22), new Vector2(500, 30));
            title.characterSpacing = 30;
            var place = Label("Place", hud.transform, "1-1  ·  소희의 방, 아침", 26, cream, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Serif);
            Rect(place.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -46), new Vector2(500, 40));
            var viewLabel = Label("ViewMode", hud.transform, "[V] 시점", 20, dim, TextAlignmentOptions.TopRight, UseRuntimeFont.Kind.Korean);
            Rect(viewLabel.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -24), new Vector2(400, 30));
            var dev = Label("DevStatus", hud.transform, "", 17, dim, TextAlignmentOptions.TopRight, UseRuntimeFont.Kind.Korean);
            Rect(dev.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -52), new Vector2(500, 30));
            dev.alpha = 0.75f;

            var prompt = UiNode("Prompt", hud.transform, typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            Rect(prompt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(160, 40));
            prompt.GetComponent<Image>().color = new Color(0.1f, 0.07f, 0.07f, 0.75f);
            var hl = prompt.GetComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(18, 18, 4, 4);
            hl.childControlWidth = hl.childControlHeight = true;
            var csf = prompt.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var pt = Label("Text", prompt.transform, "[E] 조사", 26, cream, TextAlignmentOptions.Center, UseRuntimeFont.Kind.Korean);
            pt.textWrappingMode = TextWrappingModes.NoWrap;
            prompt.SetActive(false);

            // 대사창 (레퍼런스 1처럼 어두운 판에 가는 크림색 테두리 두 줄)
            var dlgRoot = UiNode("Dialogue", screen.transform);
            Stretch(dlgRoot);
            var panel = UiNode("Panel", dlgRoot.transform, typeof(Image));
            Rect(panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 30), new Vector2(-140, 150));
            panel.GetComponent<Image>().color = new Color(0.94f, 0.89f, 0.81f, 0.9f);
            var gap = UiNode("Gap", panel.transform, typeof(Image));
            Stretch(gap, 2);
            gap.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.06f, 1f);
            var line2 = UiNode("InnerLine", panel.transform, typeof(Image));
            Stretch(line2, 6);
            line2.GetComponent<Image>().color = new Color(0.94f, 0.89f, 0.81f, 0.55f);
            var inner = UiNode("Inner", panel.transform, typeof(Image));
            Stretch(inner, 7);
            inner.GetComponent<Image>().color = new Color(0.1f, 0.075f, 0.075f, 0.94f);
            var dt = Label("Text", inner.transform, "", 30, cream, TextAlignmentOptions.TopLeft, UseRuntimeFont.Kind.Serif);
            Stretch(dt.gameObject, 22);
            dt.lineSpacing = 18;
            dt.gameObject.AddComponent<TextAnimator_TMP>();
            var tw = dt.gameObject.AddComponent<TypewriterComponent>();
            var actions = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Plugins/Febucci/Text Animator for Unity/Actions/Actions Database.asset");
            if (actions)
            {
                var so = new SerializedObject(tw);
                so.FindProperty("actionsDatabase").objectReferenceValue = actions;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var more = Label("More", inner.transform, "▼", 26, rose, TextAlignmentOptions.BottomRight, UseRuntimeFont.Kind.Korean);
            Rect(more.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 8), new Vector2(40, 40));
            var dlg = dlgRoot.AddComponent<DialogueBox>();
            dlg.panel = panel; dlg.text = dt; dlg.typewriter = tw; dlg.moreIndicator = more.gameObject;
            dlg.charsPerSecond = 26f; // 아침이라 천천히

            // 암전용 검은 화면 (최상단)
            var fade = UiNode("Fade", screen.transform, typeof(Image), typeof(CanvasGroup));
            Stretch(fade);
            fade.GetComponent<Image>().color = Color.black;
            fade.GetComponent<Image>().raycastTarget = false;
            fade.GetComponent<CanvasGroup>().alpha = 1f;

            var h = canvasGo.AddComponent<SurplusHud>();
            h.player = player; h.dialogue = dlg; h.rig = rig;
            h.hudGroup = hudGroup; h.prompt = prompt; h.viewLabel = viewLabel; h.devStatus = dev;
            h.fade = fade.GetComponent<CanvasGroup>();
            return new UiRefs { dialogue = dlg, hud = h };
        }

        static Material IllustratedMaterial()
        {
            string path = MatFolder + "/M_SP_IllustratedScreen.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Liminal/IllustratedScreen"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }
    }
}
