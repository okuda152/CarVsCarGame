// ============================================================
//  CarVsCarSetup.cs  —  Unity Editor  (Assets/Editor/)
//  Menu: Tools > Setup CarVsCar Demo
//  Unity 6 (6000.3.14f1) / URP
// ============================================================
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

public static class CarVsCarSetup
{
    const string P_PREFABS   = "Assets/Prefabs";
    const string P_MATERIALS = "Assets/Materials";
    const string P_SCENES    = "Assets/Scenes";
    const string P_EDITOR    = "Assets/Editor";

    // 日本語対応フォント（Run() 冒頭で生成、AddLabel/MakeButton で使用）
    static TMP_FontAsset s_jaFont;

    // ── Entry point ────────────────────────────────────────────
    [MenuItem("Tools/Setup CarVsCar Demo")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        s_jaFont = null; // 毎回リセット

        try
        {
            Prog(0.02f, "Creating folders...");
            EnsureFolders();

            Prog(0.06f, "Registering tags...");
            EnsureTag("Player");
            EnsureTag("Enemy");

            Prog(0.10f, "Configuring Input System & URP...");
            SetInputBoth();
            SetupURP();

            Prog(0.12f, "TMP Essential Resources インポート中…");
            ImportTMPEssentials();

            Prog(0.13f, "日本語フォント生成中…");
            s_jaFont = CreateJapaneseFont();

            // ── Materials ────────────────────────────────────
            Prog(0.14f, "Creating materials...");
            var mGhost  = MakeMat("Ghost",       transparent: true,  new Color(0.30f, 0.65f, 1.00f, 0.35f));
            var mPlayer = MakeMat("PlayerCar",  transparent: false, new Color(0.20f, 0.40f, 0.90f, 1.00f));
            var mAI     = MakeMat("AICar",      transparent: false, new Color(0.90f, 0.20f, 0.20f, 1.00f));
            var mProj   = MakeMat("Projectile", transparent: false, new Color(1.00f, 0.90f, 0.10f, 1.00f));
            var mGround = MakeMat("Ground",     transparent: false, new Color(0.27f, 0.42f, 0.27f, 1.00f));
            var mWeapon = MakeMat("Weapon",     transparent: false, new Color(0.28f, 0.28f, 0.30f, 1.00f));
            var mWheel  = MakeMat("Wheel",      transparent: false, new Color(0.10f, 0.10f, 0.10f, 1.00f));

            // ── Prefabs ──────────────────────────────────────
            Prog(0.22f, "Creating Projectile prefab...");
            var projPrefab = MakeProjectile(mProj);

            Prog(0.32f, "Creating weapon prefabs...");
            var turretBattle = MakeTurretBattle(mWeapon, projPrefab);
            var mgBattle     = MakeMGBattle(mWeapon, projPrefab);
            var turretPrev   = MakeTurretPreview(mWeapon);
            var mgPrev       = MakeMGPreview(mWeapon);

            // ※ PlayerCar / AICar / GridCell プレハブは不要
            //   CarBuilder が実行時に組み立て、BuildManager が直接 GameObject を生成する。

            // ── Scenes ───────────────────────────────────────
            Prog(0.66f, "Creating BuildScene...");
            BuildScene(turretPrev, mgPrev, mPlayer, mWheel, mWeapon, mGhost, mGround);

            Prog(0.82f, "Creating BattleScene...");
            BattleScene(turretBattle, mgBattle, mPlayer, mAI, mWheel, mGround);

            Prog(0.96f, "Updating build settings...");
            SetBuildSettings();

            AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh() は呼ばない:
            // Refresh() → reimport → TMP_FontAsset.OnDestroy() → DestroyAtlasTextures()
            // の流れでサブアセットのアトラスが破壊される可能性があるため。

            EditorSceneManager.OpenScene($"{P_SCENES}/BuildScene.unity");

            EditorUtility.DisplayDialog("セットアップ完了！",
                "CarVsCar Demo の準備ができました。\n\n" +
                "BuildScene が開かれました。\n▶ Play でゲーム開始！",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[CarVsCarSetup] " + ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ─────────────────────────────────────────────────────────
    //  URP Setup
    // ─────────────────────────────────────────────────────────
    static void SetupURP()
    {
#if UNITY_6000_0_OR_NEWER
        try
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                return; // Already configured

            // Create renderer data
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/URP_Renderer.asset");

            // Create URP pipeline asset
            var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
            urpAsset.shadowDistance       = 50f;
            urpAsset.msaaSampleCount      = 4;
            AssetDatabase.CreateAsset(urpAsset, "Assets/URP_Pipeline.asset");

            // Set as default render pipeline in all quality levels
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            for (int i = 0; i < QualitySettings.count; i++)
                QualitySettings.SetQualityLevel(i, true);
            QualitySettings.renderPipeline = urpAsset;

            AssetDatabase.SaveAssets();
            Debug.Log("[CarVsCarSetup] URP configured.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CarVsCarSetup] URP setup skipped: {ex.Message}");
        }
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  TMP Essential Resources auto-import
    // ─────────────────────────────────────────────────────────
    static void ImportTMPEssentials()
    {
        // Already imported if the default TMP settings asset exists
        if (AssetDatabase.FindAssets("TMP Settings").Length > 0)
        {
            Debug.Log("[CarVsCarSetup] TMP Essential Resources already imported. Skipping.");
            return;
        }

        // Search Library/PackageCache for the TMP unitypackage
        string cacheDir = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));

        string packageFile = null;
        if (Directory.Exists(cacheDir))
        {
            foreach (var dir in Directory.GetDirectories(cacheDir, "com.unity.textmeshpro@*"))
            {
                string candidate = Path.Combine(dir, "Package Resources",
                                                "TMP Essential Resources.unitypackage");
                if (File.Exists(candidate)) { packageFile = candidate; break; }
            }
        }

        if (packageFile == null)
        {
            Debug.LogWarning("[CarVsCarSetup] TMP Essential Resources.unitypackage not found. " +
                             "Run Window > TextMeshPro > Import TMP Essential Resources manually.");
            return;
        }

        AssetDatabase.ImportPackage(packageFile, false); // false = no dialog
        AssetDatabase.Refresh();
        Debug.Log("[CarVsCarSetup] TMP Essential Resources imported from: " + packageFile);
    }

    // ─────────────────────────────────────────────────────────
    //  Japanese dynamic font asset
    // ─────────────────────────────────────────────────────────
    // UI で使うすべての日本語文字 + よく使う記号
    const string k_JaChars =
        "武器選択砲台高ダメ低速機関銃なし中開始もう一度バトルビルドに戻る配置削除未" +
        "グリッドをクリックで／▶　" +
        "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめも" +
        "やゆよらりるれろわをんアイウエオカキクケコサシスセソタチツテトナニヌネノ" +
        "ハヒフヘホマミムメモヤユヨラリルレロワヲン" +
        "一二三四五六七八九十百千万円年月日時分秒" +
        "勝負開始終了結果攻撃防御体力残機";

    // ─────────────────────────────────────────────────────────
    //  Windows フォントファイルをプロジェクトにコピーしてインポート
    //  （Font.CreateDynamicFontFromOSFont はファイル実体を持たないため
    //    TMP のアトラス生成が失敗する。ファイルを直接インポートすることで解決）
    // ─────────────────────────────────────────────────────────
    static Font ImportWindowsFontFile()
    {
        string winFonts;
        try { winFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts); }
        catch { return null; }

        if (!Directory.Exists(winFonts)) return null;

        if (!AssetDatabase.IsValidFolder("Assets/Fonts"))
            AssetDatabase.CreateFolder("Assets", "Fonts");

        // 優先度順：Yu Gothic UI → Meiryo → MS Gothic
        string[] fontFiles = { "YuGothR.ttc", "YuGothM.ttc", "meiryo.ttc", "msgothic.ttc" };

        foreach (var file in fontFiles)
        {
            string src = Path.Combine(winFonts, file);
            if (!File.Exists(src)) continue;

            string assetRel = "Assets/Fonts/" + file;
            string assetAbs = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", assetRel));

            try { if (!File.Exists(assetAbs)) File.Copy(src, assetAbs); }
            catch (Exception ex) { Debug.LogWarning("[CarVsCarSetup] コピー失敗: " + ex.Message); continue; }

            AssetDatabase.ImportAsset(assetRel, ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<Font>(assetRel);
            if (font != null)
            {
                Debug.Log("[CarVsCarSetup] フォントをインポートしました: " + assetRel);
                return font;
            }
        }
        return null;
    }

    static TMP_FontAsset CreateJapaneseFont()
    {
        const string assetPath = "Assets/Materials/JapaneseFont.asset";

        // 毎回クリーンに再作成（古いサブアセット込みで削除）
        AssetDatabase.DeleteAsset(assetPath);
        // 旧実装が作っていた独立アセットも念のため削除
        AssetDatabase.DeleteAsset("Assets/Materials/JapaneseFont_Atlas.asset");
        AssetDatabase.DeleteAsset("Assets/Materials/JapaneseFont_Mat.mat");

        // ① フォントファイル取得
        Font srcFont = ImportWindowsFontFile();
        if (srcFont == null)
        {
            string[] names = { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo",
                                "MS UI Gothic", "MS Gothic" };
            srcFont = Font.CreateDynamicFontFromOSFont(names, 32);
            if (srcFont != null)
                Debug.Log("[CarVsCarSetup] OS フォント (フォールバック): " + srcFont.name);
        }
        if (srcFont == null)
        {
            Debug.LogWarning("[CarVsCarSetup] 日本語フォントが見つかりません。UI は英語表示になります。");
            return null;
        }

        // ② TMP 公式ファクトリで生成
        //    内部で FontEngine.LoadFontFace → GetFaceInfo → ReadFontAssetDefinition を正しく実行。
        //    デフォルト: samplingPointSize=90, padding=9, SDFAA, 1024×1024, Dynamic, multiAtlas=true
        var fa = TMP_FontAsset.CreateFontAsset(srcFont);
        if (fa == null)
        {
            Debug.LogError("[CarVsCarSetup] TMP_FontAsset.CreateFontAsset に失敗しました。" +
                           "フォントファイルが正しい TrueType/OpenType 形式か確認してください。");
            return null;
        }
        fa.name = "JapaneseFont";

        // ③ フォントアセットをディスクに保存（ScriptableObject 本体）
        AssetDatabase.CreateAsset(fa, assetPath);

        // ④ アトラステクスチャ・マテリアルをサブアセットとして登録
        //    CreateFontAsset() が生成した Texture2D / Material はまだ非保存。
        //    AddObjectToAsset で親アセットに紐づけることで永続化される。
        if (fa.atlasTextures != null)
            foreach (var tex in fa.atlasTextures)
                if (tex != null) AssetDatabase.AddObjectToAsset(tex, assetPath);

        if (fa.material != null)
            AssetDatabase.AddObjectToAsset(fa.material, assetPath);

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();

        var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        Debug.Log("[CarVsCarSetup] 日本語フォント生成完了: " + assetPath +
                  (result != null ? " OK" : " ← LoadAssetAtPath 失敗"));
        return result;
    }

    // ─────────────────────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────────────────────
    static void Prog(float t, string msg) =>
        EditorUtility.DisplayProgressBar("CarVsCar Setup", msg, t);

    static void EnsureFolders()
    {
        foreach (var p in new[] { P_PREFABS, P_MATERIALS, P_SCENES, P_EDITOR })
        {
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(p)!.Replace('\\', '/'),
                    Path.GetFileName(p));
        }
    }

    static void EnsureTag(string tag)
    {
        var so   = new SerializedObject(
                       AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    static void SetInputBoth()
    {
        try
        {
            var so   = new SerializedObject(
                           AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = so.FindProperty("activeInputHandling");
            if (prop != null) { prop.intValue = 2; so.ApplyModifiedProperties(); }
        }
        catch { /* プロジェクトによっては非対応 */ }
    }

    // ─────────────────────────────────────────────────────────
    //  Materials
    // ─────────────────────────────────────────────────────────
    static Material MakeMat(string name, bool transparent, Color color)
    {
        string path   = $"{P_MATERIALS}/{name}.mat";
        Shader shader = transparent
            ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
            : (Shader.Find("Universal Render Pipeline/Lit")   ?? Shader.Find("Standard"));

        var mat = new Material(shader ?? Shader.Find("Standard")) { name = name };

        // Color (URP uses _BaseColor; Standard uses _Color)
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);

        if (transparent)
        {
            // URP transparent
            if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface",   1f);
            if (mat.HasProperty("_Blend"))     mat.SetFloat("_Blend",     0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // Standard shader transparent fallback
            if (mat.HasProperty("_Mode"))      mat.SetFloat("_Mode",      3f);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // Common blend settings
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    // ─────────────────────────────────────────────────────────
    //  Prefab helpers
    // ─────────────────────────────────────────────────────────
    static GameObject SavePrefab(GameObject go, string prefabName)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{P_PREFABS}/{prefabName}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static void NoCollider<T>(GameObject go) where T : Collider =>
        UnityEngine.Object.DestroyImmediate(go.GetComponent<T>());

    static GameObject Prim(PrimitiveType t, Transform parent,
                           Vector3 scale, Vector3 localPos, Material mat, bool removeCol = true)
    {
        var go = GameObject.CreatePrimitive(t);
        go.transform.SetParent(parent, false);
        go.transform.localScale    = scale;
        go.transform.localPosition = localPos;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (removeCol)
        {
            var c = go.GetComponent<Collider>();
            if (c) UnityEngine.Object.DestroyImmediate(c);
        }
        return go;
    }

    // ── Projectile ────────────────────────────────────────────
    static GameObject MakeProjectile(Material mat)
    {
        var root = new GameObject("Projectile");
        var rb   = root.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;
        var sc   = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius    = 0.18f;
        root.AddComponent<Projectile>();

        var vis = Prim(PrimitiveType.Sphere, root.transform,
                       Vector3.one * 0.28f, Vector3.zero, mat);
        vis.name = "Visual";

        return SavePrefab(root, "Projectile");
    }

    // ── Turret (shared geometry) ──────────────────────────────
    static (GameObject root, Transform head, Transform fp) TurretGeometry(Material mat)
    {
        var root = new GameObject("Turret");

        var baseObj = Prim(PrimitiveType.Cylinder, root.transform,
                           new Vector3(0.65f, 0.10f, 0.65f), Vector3.zero, mat);
        baseObj.name = "Base";

        var headGO = new GameObject("TurretHead");
        headGO.transform.SetParent(root.transform, false);
        headGO.transform.localPosition = new Vector3(0f, 0.18f, 0f);

        Prim(PrimitiveType.Cube, headGO.transform,
             new Vector3(0.55f, 0.30f, 0.55f), Vector3.zero, mat).name = "Mesh";

        Prim(PrimitiveType.Cube, headGO.transform,
             new Vector3(0.13f, 0.13f, 0.55f), new Vector3(0f, 0f, 0.38f), mat).name = "Barrel";

        var fp       = new GameObject("FirePoint");
        fp.transform.SetParent(headGO.transform, false);
        fp.transform.localPosition = new Vector3(0f, 0f, 0.67f);

        return (root, headGO.transform, fp.transform);
    }

    static GameObject MakeTurretBattle(Material mat, GameObject projPrefab)
    {
        var (root, head, fp) = TurretGeometry(mat);
        var w  = root.AddComponent<TurretWeapon>();
        var so = new SerializedObject(w);
        so.FindProperty("turretHead").objectReferenceValue        = head;
        so.FindProperty("firePoint").objectReferenceValue         = fp;
        so.FindProperty("projectilePrefab").objectReferenceValue  = projPrefab;
        so.ApplyModifiedProperties();
        return SavePrefab(root, "TurretBattle");
    }

    static GameObject MakeTurretPreview(Material mat)
    {
        var (root, _, _) = TurretGeometry(mat);
        return SavePrefab(root, "TurretPreview");
    }

    // ── MachineGun (shared geometry) ──────────────────────────
    static (GameObject root, Transform fp) MGGeometry(Material mat)
    {
        var root = new GameObject("MachineGun");
        Prim(PrimitiveType.Cube, root.transform,
             new Vector3(0.42f, 0.28f, 0.85f), Vector3.zero, mat).name = "Body";
        Prim(PrimitiveType.Cube, root.transform,
             new Vector3(0.12f, 0.12f, 0.40f), new Vector3(0f, 0.05f, 0.62f), mat).name = "Barrel";

        var fp = new GameObject("FirePoint");
        fp.transform.SetParent(root.transform, false);
        fp.transform.localPosition = new Vector3(0f, 0.05f, 0.84f);

        return (root, fp.transform);
    }

    static GameObject MakeMGBattle(Material mat, GameObject projPrefab)
    {
        var (root, fp) = MGGeometry(mat);
        var w  = root.AddComponent<MachineGunWeapon>();
        var so = new SerializedObject(w);
        so.FindProperty("firePoint").objectReferenceValue        = fp;
        so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        so.ApplyModifiedProperties();
        return SavePrefab(root, "MachineGunBattle");
    }

    static GameObject MakeMGPreview(Material mat)
    {
        var (root, _) = MGGeometry(mat);
        return SavePrefab(root, "MachineGunPreview");
    }

    // ─────────────────────────────────────────────────────────
    //  Build Scene  (3D LEGO 式ビルダー)
    // ─────────────────────────────────────────────────────────
    static void BuildScene(GameObject turretPrev, GameObject mgPrev,
                           Material blockMat, Material tireMat, Material weaponMat,
                           Material ghostMat, Material groundMat)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── カメラ（パース + OrbitCamera）────────────────────
        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam   = camGO.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.clearFlags  = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.16f);
        // OrbitCamera が実行時に位置を制御。エディタ用の初期位置を設定。
        camGO.transform.SetPositionAndRotation(new Vector3(-6f, 9f, -9f),
                                               Quaternion.Euler(38f, 34f, 0f));
        camGO.AddComponent<AudioListener>();

        var orb = camGO.AddComponent<OrbitCamera>();
        var orbSO = new SerializedObject(orb);
        // pivotPoint, yaw, pitch, distance はデフォルト値を使用
        orbSO.ApplyModifiedProperties();

        MakeDirectionalLight();

        // ── 地面（レイキャスト可能なコライダー付き）─────────
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position   = new Vector3(0f, 0f, 0f);
        ground.transform.localScale = new Vector3(2.0f, 1f, 2.0f);   // 20×20 ユニット
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
        // MeshCollider はそのまま（Plane のデフォルト）

        // ── グリッド原点（世界 (-3, 0, -3)）────────────────
        //    7×7 グリッド、セルサイズ 1.0 → グリッド中心が原点付近になる
        var gridOriginGO = new GameObject("GridOrigin");
        gridOriginGO.transform.position = new Vector3(-3f, 0f, -3f);

        // ── Canvas + UI ───────────────────────────────────────
        var canvas  = MakeCanvas("BuildCanvas");
        var buildUI = MakeBuildUI(canvas.transform);
        MakeEventSystem();

        // ── BuildManager ──────────────────────────────────────
        var bmGO = new GameObject("BuildManager");
        var bm   = bmGO.AddComponent<BuildManager>();
        var bmSO = new SerializedObject(bm);
        bmSO.FindProperty("blockMat").objectReferenceValue               = blockMat;
        bmSO.FindProperty("tireMat").objectReferenceValue                = tireMat;
        bmSO.FindProperty("weaponMat").objectReferenceValue              = weaponMat;
        bmSO.FindProperty("ghostMat").objectReferenceValue               = ghostMat;
        bmSO.FindProperty("turretPreviewPrefab").objectReferenceValue    = turretPrev;
        bmSO.FindProperty("machineGunPreviewPrefab").objectReferenceValue = mgPrev;
        bmSO.FindProperty("gridOrigin").objectReferenceValue             = gridOriginGO.transform;
        bmSO.FindProperty("buildUI").objectReferenceValue                = buildUI;
        bmSO.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, $"{P_SCENES}/BuildScene.unity");
    }

    // ─────────────────────────────────────────────────────────
    //  Battle Scene
    // ─────────────────────────────────────────────────────────
    static void BattleScene(GameObject turretBattle, GameObject mgBattle,
                            Material playerBodyMat, Material aiBodyMat,
                            Material tireMat, Material groundMat)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // カメラ — サードパーソン（スクリプトが実行時にターゲットを設定）
        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam   = camGO.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.clearFlags  = CameraClearFlags.Skybox;
        camGO.transform.SetPositionAndRotation(new Vector3(0f, 8f, -16f),
                                               Quaternion.Euler(20f, 0f, 0f));
        camGO.AddComponent<AudioListener>();
        var tpCam = camGO.AddComponent<ThirdPersonCamera>();

        MakeDirectionalLight();

        // 地面
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // 高低差のあるテレイン
        MakeBattleTerrain(groundMat);

        // 境界壁（不可視）
        MakeWall(new Vector3(  0f, 1f,  52f), new Vector3(104f, 3f, 1f));
        MakeWall(new Vector3(  0f, 1f, -52f), new Vector3(104f, 3f, 1f));
        MakeWall(new Vector3( 52f, 1f,   0f), new Vector3(1f, 3f, 104f));
        MakeWall(new Vector3(-52f, 1f,   0f), new Vector3(1f, 3f, 104f));

        // スポーン地点
        var psGO = new GameObject("PlayerSpawn");
        psGO.transform.SetPositionAndRotation(new Vector3(-18f, 0f, 0f),
                                              Quaternion.Euler(0f, 90f, 0f));
        var asGO = new GameObject("AISpawn");
        asGO.transform.SetPositionAndRotation(new Vector3(18f, 0f, 0f),
                                              Quaternion.Euler(0f, -90f, 0f));

        // Canvas + UI
        var canvas   = MakeCanvas("BattleCanvas");
        var battleUI = MakeBattleUI(canvas.transform);
        MakeEventSystem();

        // BattleManager + CarBuilder（同じ GameObject に追加）
        var bmGO = new GameObject("BattleManager");
        var bm   = bmGO.AddComponent<BattleManager>();
        var cb   = bmGO.AddComponent<CarBuilder>();

        var bmSO = new SerializedObject(bm);
        bmSO.FindProperty("carBuilder").objectReferenceValue         = cb;
        bmSO.FindProperty("playerSpawn").objectReferenceValue        = psGO.transform;
        bmSO.FindProperty("aiSpawn").objectReferenceValue            = asGO.transform;
        bmSO.FindProperty("battleUI").objectReferenceValue           = battleUI;
        bmSO.FindProperty("thirdPersonCamera").objectReferenceValue  = tpCam;
        bmSO.ApplyModifiedProperties();

        var cbSO = new SerializedObject(cb);
        cbSO.FindProperty("playerBodyMat").objectReferenceValue          = playerBodyMat;
        cbSO.FindProperty("aiBodyMat").objectReferenceValue              = aiBodyMat;
        cbSO.FindProperty("tireMat").objectReferenceValue                = tireMat;
        cbSO.FindProperty("turretBattlePrefab").objectReferenceValue     = turretBattle;
        cbSO.FindProperty("machineGunBattlePrefab").objectReferenceValue = mgBattle;
        cbSO.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, $"{P_SCENES}/BattleScene.unity");
    }

    // ─────────────────────────────────────────────────────────
    //  Scene helpers
    // ─────────────────────────────────────────────────────────
    static void MakeDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var l = go.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.intensity = 1.4f;
    }

    static void MakeWall(Vector3 pos, Vector3 scale)
    {
        var w = new GameObject("Wall");
        w.transform.position   = pos;
        w.transform.localScale = scale;
        w.AddComponent<BoxCollider>();   // invisible — collider only
    }

    // ─────────────────────────────────────────────────────────
    //  Battle terrain helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 中央高台・スロープ・カバーを作る。
    /// 高台: 16×16、高さ 0.5 m（y=0.5 が上面）
    /// スロープ: 4 方向、傾斜角 ~8.1°
    /// 四隅の小丘: y=0.25 の低い盛り上がり
    /// 高台上カバー: 4 本の遮蔽物
    /// </summary>
    static void MakeBattleTerrain(Material mat)
    {
        // ── 中央高台 ───────────────────────────────────────────
        // scale(16,1,16)、center y=0 → 上面 y=0.5
        MakeTerrainBox("CentralPlatform", Vector3.zero,
                        new Vector3(16f, 1.0f, 16f), mat);

        // ── 4 方向スロープ ────────────────────────────────────
        // 高台端 (±8) から 3.5 m で地面まで下がる → 傾斜角 arctan(0.5/3.5) ≈ 8.1°
        // 斜辺長 = sqrt(3.5² + 0.5²) ≈ 3.54
        const float sLen  = 3.54f;
        const float angle = 8.1f;
        MakeTerrainBox("RampN", new Vector3( 0f,  0.25f, -9.75f), new Vector3(12f, 0.4f, sLen), mat,  angle,  0f,  0f);
        MakeTerrainBox("RampS", new Vector3( 0f,  0.25f,  9.75f), new Vector3(12f, 0.4f, sLen), mat, -angle,  0f,  0f);
        MakeTerrainBox("RampE", new Vector3( 9.75f, 0.25f, 0f),   new Vector3(sLen, 0.4f, 12f), mat,  0f,  0f, -angle);
        MakeTerrainBox("RampW", new Vector3(-9.75f, 0.25f, 0f),   new Vector3(sLen, 0.4f, 12f), mat,  0f,  0f,  angle);

        // ── 四隅の小丘（高さ 0.25 m）────────────────────────
        float[] cs = { 28f, -28f };
        foreach (var cx in cs)
            foreach (var cz in cs)
                MakeTerrainBox("CornerBump", new Vector3(cx, 0f, cz),
                                new Vector3(10f, 0.5f, 10f), mat);

        // ── 左右の中間段差 ─────────────────────────────────
        // スポーン（x=±18）と高台端（x=±8）の間に緩やかな盛り上がり
        MakeTerrainBox("BumpL", new Vector3(-15f, 0f,  0f), new Vector3(6f, 0.35f, 14f), mat);
        MakeTerrainBox("BumpR", new Vector3( 15f, 0f,  0f), new Vector3(6f, 0.35f, 14f), mat);

        // ── 高台上のカバー遮蔽物 ───────────────────────────
        // 上面 y=0.5 の高台に乗せるため center.y = 0.5 + 0.6 = 1.1
        float[] offs = { 4.5f, -4.5f };
        foreach (var ox in offs)
            foreach (var oz in offs)
                MakeTerrainBox("Cover", new Vector3(ox, 1.1f, oz),
                                new Vector3(1.4f, 1.2f, 1.4f), mat);
    }

    /// <summary>コライダー付きのテレインボックスを生成する。</summary>
    static void MakeTerrainBox(string name, Vector3 pos, Vector3 scale, Material mat,
                                float ex = 0f, float ey = 0f, float ez = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(ex, ey, ez));
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // ─────────────────────────────────────────────────────────
    //  UI factories
    // ─────────────────────────────────────────────────────────
    static Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name);
        var c  = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280f, 720f);
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static void MakeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        // Support both old & new Input System
        var type = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (type != null) go.AddComponent(type);
        else              go.AddComponent<StandaloneInputModule>();
    }

    // ── Build UI ──────────────────────────────────────────────
    static BuildUI MakeBuildUI(Transform canvasT)
    {
        // ── 左パネル ─────────────────────────────────────────────
        var panel = MakePanel(canvasT, "BuildPanel",
            anchor: new Rect(0f, 0f, 0f, 1f),
            pivot:  new Vector2(0f, 0.5f),
            size:   new Vector2(380f, 0f),
            pos:    Vector2.zero,
            color:  new Color(0.05f, 0.05f, 0.10f, 0.88f));

        // タイトル
        AddLabel(panel.transform, "Title", "カスタムカー設定", 34f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -68f, -8f, -8f));

        // ── パレットボタン（上から順に配置）─────────────────────
        // オフセット形式: Vector4(left, bottom-from-top, right-from-right, top-from-top)
        //   anchorRect(0,1,1,1) = 上端揃えのフルワイド
        //   off.w = -上辺距離(px)  off.y = -下辺距離(px)

        var blockBtn = MakeButton(panel.transform, "BlockButton",
            "■  ブロック\n車体を自由に形成", 26f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -168f, -8f, -76f));

        var tireBtn = MakeButton(panel.transform, "TireButton",
            "◯  タイヤ\n2個以上で走行可能", 26f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -268f, -8f, -176f));

        var turretBtn = MakeButton(panel.transform, "TurretButton",
            "▲  砲台\n高ダメ・低連射", 26f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -368f, -8f, -276f));

        var mgBtn = MakeButton(panel.transform, "MachineGunButton",
            "⊕  機関銃\n低ダメ・高連射", 26f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -468f, -8f, -376f));

        // 操作ヒント
        var hintLbl = AddLabel(panel.transform, "Hint",
            "左クリック: 配置　右クリック: 削除\n中ドラッグ or Alt+左: 視点回転　スクロール: ズーム", 16f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -524f, -8f, -474f));
        hintLbl.color = new Color(0.6f, 0.6f, 0.6f);

        // ステータスラベル（要件表示）
        var statusLbl = AddLabel(panel.transform, "StatusLabel",
            "ブロック: 0   タイヤ: 0/2   武器: 0/1\nタイヤが足りません", 20f,
            AnchorRect(0f, 1f, 1f, 1f), off: new Vector4(8f, -600f, -8f, -512f));
        statusLbl.alignment = TextAlignmentOptions.Left;
        statusLbl.color     = new Color(0.85f, 0.85f, 0.85f);

        // バトル開始ボタン（下揃え）
        var battleBtn = MakeButton(panel.transform, "BattleStartButton",
            "▶  バトル開始", 34f,
            AnchorRect(0f, 0f, 1f, 0f), off: new Vector4(8f, 10f, -8f, 100f));
        battleBtn.GetComponent<Button>().interactable = false;
        battleBtn.GetComponent<Image>().color         = new Color(0.15f, 0.50f, 0.15f);

        // 画面下部の操作説明
        var instrGO = new GameObject("Instructions");
        instrGO.transform.SetParent(canvasT, false);
        SetRT(instrGO, AnchorRect(0.22f, 0f, 1f, 0f), new Vector4(0f, 5f, 0f, 50f));
        var instr = instrGO.AddComponent<TextMeshProUGUI>();
        if (s_jaFont != null) instr.font = s_jaFont;
        instr.text      = "タイルを選んでクリック配置！　タイヤ×2 ＋ 武器×1 以上でバトル開始可能";
        instr.fontSize  = 22f;
        instr.alignment = TextAlignmentOptions.Center;
        instr.color     = new Color(0.75f, 0.75f, 0.75f);

        // ── BuildUI コンポーネントに各ボタンを登録 ────────────────
        var uiGO    = new GameObject("BuildUI");
        uiGO.transform.SetParent(canvasT, false);
        var buildUI = uiGO.AddComponent<BuildUI>();
        var so      = new SerializedObject(buildUI);
        so.FindProperty("blockButton").objectReferenceValue      = blockBtn.GetComponent<Button>();
        so.FindProperty("tireButton").objectReferenceValue       = tireBtn.GetComponent<Button>();
        so.FindProperty("turretButton").objectReferenceValue     = turretBtn.GetComponent<Button>();
        so.FindProperty("machineGunButton").objectReferenceValue = mgBtn.GetComponent<Button>();
        so.FindProperty("statusLabel").objectReferenceValue      = statusLbl;
        so.FindProperty("battleStartButton").objectReferenceValue = battleBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();
        return buildUI;
    }

    // ── Battle UI ─────────────────────────────────────────────
    static BattleUI MakeBattleUI(Transform canvasT)
    {
        // Player HP — top-left
        var (pPanel, pSlider, pLabel) = MakeHPPanel(canvasT, "PlayerHP", "PLAYER",
            new Color(0.3f, 0.5f, 1.0f),
            AnchorRect(0f, 1f, 0f, 1f),
            new Vector4(10f, -150f, 520f, -10f),
            new Vector2(0f, 1f));

        // Enemy HP — top-right
        var (ePanel, eSlider, eLabel) = MakeHPPanel(canvasT, "EnemyHP", "ENEMY",
            new Color(1.0f, 0.3f, 0.3f),
            AnchorRect(1f, 1f, 1f, 1f),
            new Vector4(-520f, -150f, -10f, -10f),
            new Vector2(1f, 1f));

        // Result panel — center, initially inactive
        var resultPanel = MakePanel(canvasT, "ResultPanel",
            anchor: new Rect(0.25f, 0.28f, 0.75f, 0.72f),
            pivot: new Vector2(0.5f, 0.5f),
            size: Vector2.zero, pos: Vector2.zero,
            color: new Color(0f, 0f, 0f, 0.88f));
        resultPanel.SetActive(false);

        var resultLabel = AddLabel(resultPanel.transform, "ResultLabel", "YOU WIN!", 90f,
            AnchorRect(0f, 0.45f, 1f, 1f), off: Vector4.zero);
        resultLabel.fontStyle = FontStyles.Bold;

        var retryBtn = MakeButton(resultPanel.transform, "RetryButton",
            "もう一度", 32f,
            AnchorRect(0.05f, 0.05f, 0.48f, 0.05f),
            off: new Vector4(0f, 5f, 0f, 80f));

        var buildBtn = MakeButton(resultPanel.transform, "BuildButton",
            "ビルドに戻る", 32f,
            AnchorRect(0.52f, 0.05f, 0.95f, 0.05f),
            off: new Vector4(0f, 5f, 0f, 80f));

        var uiGO   = new GameObject("BattleUI");
        uiGO.transform.SetParent(canvasT, false);
        var battleUI = uiGO.AddComponent<BattleUI>();
        var so       = new SerializedObject(battleUI);
        so.FindProperty("playerHPBar").objectReferenceValue   = pSlider;
        so.FindProperty("enemyHPBar").objectReferenceValue    = eSlider;
        so.FindProperty("playerHPLabel").objectReferenceValue = pLabel;
        so.FindProperty("enemyHPLabel").objectReferenceValue  = eLabel;
        so.FindProperty("resultPanel").objectReferenceValue   = resultPanel;
        so.FindProperty("resultLabel").objectReferenceValue   = resultLabel;
        so.FindProperty("retryButton").objectReferenceValue   = retryBtn.GetComponent<Button>();
        so.FindProperty("buildButton").objectReferenceValue   = buildBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();
        return battleUI;
    }

    // ─────────────────────────────────────────────────────────
    //  UI component helpers
    // ─────────────────────────────────────────────────────────

    // anchorRect: (anchorMinX, anchorMinY, anchorMaxX, anchorMaxY)
    static Rect AnchorRect(float x0, float y0, float x1, float y1) =>
        new Rect(x0, y0, x1, y1);

    // off: (offsetMinX, offsetMinY, offsetMaxX, offsetMaxY)
    static void SetRT(GameObject go, Rect anchor, Vector4 off, Vector2 pivot = default)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y);
        rt.anchorMax = new Vector2(anchor.width, anchor.height);
        rt.offsetMin = new Vector2(off.x, off.y);
        rt.offsetMax = new Vector2(off.z, off.w);
        if (pivot != default) rt.pivot = pivot;
    }

    static GameObject MakePanel(Transform parent, string name,
                                 Rect anchor, Vector2 pivot, Vector2 size, Vector2 pos,
                                 Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(anchor.x,     anchor.y);
        rt.anchorMax        = new Vector2(anchor.width,  anchor.height);
        rt.pivot            = pivot;
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float size,
                                    Rect anchor, Vector4 off)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRT(go, anchor, off);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (s_jaFont != null) tmp.font = s_jaFont;
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string name, string label, float fontSize,
                                  Rect anchor, Vector4 off)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRT(go, anchor, off);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.32f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = new Color(0.38f, 0.38f, 0.55f);
        cols.pressedColor     = new Color(0.12f, 0.12f, 0.20f);
        btn.colors = cols;

        // Button text child
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        SetRT(txtGO, AnchorRect(0f, 0f, 1f, 1f), new Vector4(6f, 4f, -6f, -4f));
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        if (s_jaFont != null) tmp.font = s_jaFont;
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static (GameObject panel, Slider slider, TextMeshProUGUI hpLabel)
        MakeHPPanel(Transform parent, string name, string labelText, Color fillColor,
                    Rect anchor, Vector4 off, Vector2 pivot)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x,    anchor.y);
        rt.anchorMax = new Vector2(anchor.width, anchor.height);
        rt.offsetMin = new Vector2(off.x, off.y);
        rt.offsetMax = new Vector2(off.z, off.w);
        rt.pivot     = pivot;
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);

        // Name label
        var nameLbl = AddLabel(panel.transform, "Label", labelText, 26f,
            AnchorRect(0f, 0.55f, 1f, 1f), new Vector4(8f, 0f, -8f, 0f));
        nameLbl.alignment = TextAlignmentOptions.Left;

        // HP slider
        var slider = MakeSlider(panel.transform, "HPBar", fillColor,
            AnchorRect(0f, 0.1f, 1f, 0.55f), new Vector4(8f, 0f, -8f, 0f));

        // HP text
        var hpLabel = AddLabel(panel.transform, "HPText", "100 / 100", 24f,
            AnchorRect(0f, 0.55f, 1f, 1f), new Vector4(8f, 0f, -8f, 0f));
        hpLabel.alignment = TextAlignmentOptions.Right;

        return (panel, slider, hpLabel);
    }

    static Slider MakeSlider(Transform parent, string name, Color fillColor,
                              Rect anchor, Vector4 off)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRT(go, anchor, off);

        var slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue  = 0f;
        slider.maxValue  = 1f;
        slider.value     = 1f;

        // Background
        var bg  = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        SetRT(bg, AnchorRect(0f, 0f, 1f, 1f), Vector4.zero);
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Fill area
        var fa = new GameObject("Fill Area");
        fa.transform.SetParent(go.transform, false);
        SetRT(fa, AnchorRect(0f, 0.05f, 1f, 0.95f), new Vector4(4f, 0f, -4f, 0f));

        // Fill
        var fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = new Vector2(5f, 0f);
        fill.AddComponent<Image>().color = fillColor;
        slider.fillRect = fillRT;

        return slider;
    }

    // ─────────────────────────────────────────────────────────
    //  Build Settings
    // ─────────────────────────────────────────────────────────
    static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{P_SCENES}/BuildScene.unity",  enabled: true),
            new EditorBuildSettingsScene($"{P_SCENES}/BattleScene.unity", enabled: true)
        };
    }
}
