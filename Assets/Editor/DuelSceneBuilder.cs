using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tools > Deadly Duel menusunden ya da komut satirindan calisir, sahneyi otomatik kurar.
public static class DuelSceneBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string HeroSheets = "Assets/Sprites/individual_sheets/";
    const string SkeletonSheets = "Assets/Sprites/Monsters Creatures Fantasy/Sprites/Skeleton/";
    const string VfxPath = "Assets/VFX/Cartoon FX Remaster/CFXR Prefabs/";
    const string HeroAnimFolder = "Assets/Animations/Hero";
    const string EnemyAnimFolder = "Assets/Animations/Enemy";
    const string WhiteSpritePath = "Assets/Sprites/UI/White.png";

    // Adim 1: TMP kaynaklari, hero sprite'larini 128x128 grid'e bol, beyaz UI sprite'i
    [MenuItem("Tools/Deadly Duel/1 - Prepare Assets")]
    public static void PrepareAssets()
    {
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            string pkg = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).First();
            AssetDatabase.ImportPackage(pkg, false);
        }

        SliceGrid(HeroSheets + "male_hero-idle.png", 128);
        SliceGrid(HeroSheets + "male_hero-combo_1.png", 128);
        SliceGrid(HeroSheets + "male_hero-combo_1_end.png", 128);

        if (!File.Exists(WhiteSpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WhiteSpritePath));
            var tex = new Texture2D(8, 8);
            tex.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray());
            File.WriteAllBytes(WhiteSpritePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(WhiteSpritePath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(WhiteSpritePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("DUEL: PrepareAssets done");
    }

    // Adim 2: animasyonlar, animator'ler ve sahne
    [MenuItem("Tools/Deadly Duel/2 - Build Scene")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(HeroAnimFolder);
        Directory.CreateDirectory(EnemyAnimFolder);
        AssetDatabase.Refresh();

        // ---------- Animasyonlar ----------
        var heroIdleSprites = LoadSprites(HeroSheets + "male_hero-idle.png");
        var heroAttackSprites = LoadSprites(HeroSheets + "male_hero-combo_1.png")
            .Concat(LoadSprites(HeroSheets + "male_hero-combo_1_end.png")).ToArray();
        var enemyIdleSprites = LoadSprites(SkeletonSheets + "Idle.png");
        var enemyHurtSprites = LoadSprites(SkeletonSheets + "Take Hit.png");
        var enemyDeathSprites = LoadSprites(SkeletonSheets + "Death.png");
        var enemyAttackSprites = LoadSprites(SkeletonSheets + "Attack1.png");

        var heroIdle = MakeClip(HeroAnimFolder + "/Hero_Idle.anim", heroIdleSprites, 10, true);
        var heroAttack = MakeClip(HeroAnimFolder + "/Hero_Attack.anim", heroAttackSprites, 14, false);
        var enemyIdle = MakeClip(EnemyAnimFolder + "/Enemy_Idle.anim", enemyIdleSprites, 8, true);
        var enemyHurt = MakeClip(EnemyAnimFolder + "/Enemy_Hurt.anim", enemyHurtSprites, 10, false);
        var enemyDeath = MakeClip(EnemyAnimFolder + "/Enemy_Death.anim", enemyDeathSprites, 8, false);
        var enemyAttack = MakeClip(EnemyAnimFolder + "/Enemy_Attack.anim", enemyAttackSprites, 14, false);

        var heroCtrl = MakeController(HeroAnimFolder + "/HeroController.controller", heroIdle, heroAttack, null, null);
        var enemyCtrl = MakeController(EnemyAnimFolder + "/EnemyController.controller", enemyIdle, enemyAttack, enemyHurt, enemyDeath);

        // ---------- Sahne ----------
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var name in new[] { "Player1", "Enemy", "Canvas", "EventSystem", "Attack1 Effect", "Attack2 Effect", "Hero Hit Effect" })
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Camera.main.orthographicSize = 5.4f;

        // Karakterler (zemin y = -3.6)
        var hero = new GameObject("Player1");
        hero.transform.position = new Vector3(-3.5f, -2.32f, 0);
        hero.transform.localScale = new Vector3(8, 8, 1);
        var heroSr = hero.AddComponent<SpriteRenderer>();
        heroSr.sprite = heroIdleSprites[0];
        heroSr.sortingOrder = 5;
        hero.AddComponent<Animator>().runtimeAnimatorController = heroCtrl;

        var enemy = new GameObject("Enemy");
        enemy.transform.position = new Vector3(3.5f, -2.1f, 0);
        enemy.transform.localScale = new Vector3(6, 6, 1);
        var enemySr = enemy.AddComponent<SpriteRenderer>();
        enemySr.sprite = enemyIdleSprites[0];
        enemySr.flipX = true;
        enemySr.sortingOrder = 5;
        enemy.AddComponent<Animator>().runtimeAnimatorController = enemyCtrl;

        // Particle efektleri (dusmanin onunde)
        var effect1 = MakeEffect(VfxPath + "Electric/CFXR3 Hit Electric C (Air).prefab", "Attack1 Effect", new Vector3(2.9f, -2.0f, -1));
        var effect2 = MakeEffect(VfxPath + "Fire/CFXR3 Hit Fire B (Air).prefab", "Attack2 Effect", new Vector3(2.9f, -2.0f, -1));

        var heroHitEffect = MakeEffect(VfxPath + "Impacts/CFXR Hit A (Red).prefab", "Hero Hit Effect", new Vector3(-3.1f, -2.0f, -1));

        // ---------- UI ----------
        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var canvas = canvasGO.transform;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        var buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 1 dunya birimi = 100 canvas pikseli (kamera size 5.4)
        var heroText = MakeText(canvas, "Player Health Text", "Health: 100", new Vector2(-350, 80), new Vector2(400, 60), 42);
        var heroBar = MakeBar(canvas, "Player Health Bar", new Vector2(-350, 30), white, new Color(0.2f, 0.85f, 0.3f));
        var enemyText = MakeText(canvas, "Enemy Health Text", "Enemy Health: 100", new Vector2(350, 80), new Vector2(460, 60), 42);
        var enemyBar = MakeBar(canvas, "Enemy Health Bar", new Vector2(350, 30), white, new Color(0.9f, 0.15f, 0.15f));

        var heroPlayer = hero.AddComponent<HeroPlayer>();
        var enemyPlayer = enemy.AddComponent<EnemyPlayer>();

        var btn1 = MakeButton(canvas, "P1 Attack 1 Button", "Attack 1", new Vector2(-460, -470), buttonSprite);
        var btn2 = MakeButton(canvas, "P1 Attack 2 Button", "Attack 2", new Vector2(-240, -470), buttonSprite);
        UnityEventTools.AddPersistentListener(btn1.onClick, heroPlayer.Attack1);
        UnityEventTools.AddPersistentListener(btn2.onClick, heroPlayer.Attack2);

        // Game Over paneli
        var panel = new GameObject("GameOver Panel", typeof(RectTransform), typeof(Image));
        panel.layer = 5;
        var prt = panel.GetComponent<RectTransform>();
        prt.SetParent(canvas, false);
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        var goText = MakeText(prt, "GameOver Text", "Game Over\nYou Win", Vector2.zero, new Vector2(1200, 400), 130);
        goText.color = new Color(1f, 0.85f, 0.2f);
        goText.fontStyle = FontStyles.Bold;
        panel.SetActive(false);

        // ---------- Baglantilar ----------
        heroPlayer.animController = hero.GetComponent<Animator>();
        heroPlayer.healthText = heroText;
        heroPlayer.healthBar = heroBar;
        heroPlayer.enemy = enemyPlayer;
        heroPlayer.attack1Effect = effect1;
        heroPlayer.attack2Effect = effect2;
        heroPlayer.hitEffect = heroHitEffect;
        heroPlayer.heroSprite = heroSr;
        heroPlayer.gameOverPanel = panel;
        heroPlayer.gameOverText = goText;

        enemyPlayer.animController = enemy.GetComponent<Animator>();
        enemyPlayer.healthText = enemyText;
        enemyPlayer.healthBar = enemyBar;
        enemyPlayer.gameOverPanel = panel;
        enemyPlayer.gameOverText = goText;
        enemyPlayer.hero = heroPlayer;

        EditorUtility.SetDirty(heroPlayer);
        EditorUtility.SetDirty(enemyPlayer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DUEL: BuildScene done");
    }

    // ---------- Yardimcilar ----------
    static void SliceGrid(string path, int cell)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int count = tex.width / cell;
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < count; i++)
        {
            rects.Add(new SpriteRect
            {
                name = baseName + "_" + i,
                rect = new Rect(i * cell, 0, cell, cell),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            });
        }
        dp.SetSpriteRects(rects.ToArray());
        var idProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idProvider != null)
            idProvider.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        importer.SaveAndReimport();
    }

    static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    static AnimationClip MakeClip(string path, Sprite[] sprites, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[sprites.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorController MakeController(string path, AnimationClip idle, AnimationClip attack, AnimationClip hurt, AnimationClip death)
    {
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Death", AnimatorControllerParameterType.Bool);
        var sm = c.layers[0].stateMachine;

        var idleState = sm.AddState("Idle", new Vector3(300, 100));
        idleState.motion = idle;
        sm.defaultState = idleState;

        AnimatorState deathState = null;
        if (death != null)
        {
            deathState = sm.AddState("Death", new Vector3(300, 250));
            deathState.motion = death;
            var t = idleState.AddTransition(deathState);
            t.AddCondition(AnimatorConditionMode.If, 0, "Death");
            t.hasExitTime = false;
            t.duration = 0;
        }
        if (attack != null)
        {
            var s = sm.AddState("Attack", new Vector3(300, -50));
            s.motion = attack;
            AddLoop(idleState, s, "Attack");
        }
        if (hurt != null)
        {
            var s = sm.AddState("Hurt", new Vector3(560, 100));
            s.motion = hurt;
            AddLoop(idleState, s, "Hurt");
            if (deathState != null)
            {
                var t = s.AddTransition(deathState);
                t.AddCondition(AnimatorConditionMode.If, 0, "Death");
                t.hasExitTime = true;
                t.exitTime = 1;
                t.duration = 0;
            }
        }
        return c;
    }

    static void AddLoop(AnimatorState idle, AnimatorState target, string trigger)
    {
        var go = idle.AddTransition(target);
        go.AddCondition(AnimatorConditionMode.If, 0, trigger);
        go.hasExitTime = false;
        go.duration = 0;
        var back = target.AddTransition(idle);
        back.hasExitTime = true;
        back.exitTime = 1;
        back.duration = 0;
    }

    static GameObject MakeEffect(string prefabPath, string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.5f;
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "CFXR_Effect")
            {
                // Destroy yerine Disable: efekt bitince kapansin, tekrar SetActive(true) ile oynasin
                var so = new SerializedObject(mb);
                so.FindProperty("clearBehavior").intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            r.sortingOrder = 20;
        go.SetActive(false);
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }

    static Image MakeBar(Transform parent, string name, Vector2 pos, Sprite white, Color fillColor)
    {
        var bg = new GameObject(name, typeof(RectTransform), typeof(Image));
        bg.layer = 5;
        var rt = bg.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(320, 28);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = white;
        bgImg.color = new Color(0, 0, 0, 0.75f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.layer = 5;
        var frt = fill.GetComponent<RectTransform>();
        frt.SetParent(rt, false);
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(4, 4);
        frt.offsetMax = new Vector2(-4, -4);
        var img = fill.GetComponent<Image>();
        img.sprite = white;
        img.color = fillColor;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1;
        return img;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 80);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.8f, 0.1f, 0.1f);
        var text = MakeText(rt, "Text", label, Vector2.zero, new Vector2(200, 80), 40);
        text.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }
}
