using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Art trial (2026-11-27): the Fab/Megascans shop shutter on a game-coloured grey building.
// The content lives in the git-ignored Assets/ArtTrial/MegascansShutter (Fab licence: no
// source sharing in a public repo); copy it there from art-source/megascans-trial first.
// Batch (needs graphics, so no -nographics):
//   Unity.exe -batchmode -quit -projectPath unity/EungyeTaxi -executeMethod MegascansShutterTrial.Run -logFile <log>
// Writes art-source/megascans-trial/unity-trial-*.png. Builds an unsaved scene only.
public static class MegascansShutterTrial
{
    const string Dir = "Assets/ArtTrial/MegascansShutter/";
    const string Stem = "Urban_Japanese_Street_Shutter_Metal_Modular_02_xfcrfdhsc_Low";

    [MenuItem("Taxi/Art/Megascans Shutter Trial")]
    public static void Run()
    {
        if (!File.Exists(Dir + Stem + ".fbx")) { Debug.LogError("SHUTTER_TRIAL missing " + Dir + Stem + ".fbx"); Fail(); return; }
        // Open the scene first: a Single-mode scene switch unloads in-memory materials made before it.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Importer(Stem + "_1K_Normal.jpg", t => t.textureType = TextureImporterType.NormalMap);
        Importer(Stem + "_1K_AO.jpg", t => t.sRGBTexture = false);
        Importer(Stem + "_1K_Specular.jpg", t => t.isReadable = true);
        Importer(Stem + "_1K_Gloss.jpg", t => { t.isReadable = true; t.sRGBTexture = false; });
        Importer(Stem + "_1K_BaseColor.jpg", t => t.isReadable = true);

        // Standard (Specular setup) wants specular RGB + gloss in alpha in one texture.
        string specGlossPath = Dir + Stem + "_SpecGloss.png";
        var spec = Tex("_1K_Specular.jpg"); var gloss = Tex("_1K_Gloss.jpg");
        var sg = new Texture2D(spec.width, spec.height, TextureFormat.RGBA32, true);
        var sp = spec.GetPixels(); var gp = gloss.GetPixels();
        for (int i = 0; i < sp.Length; i++) sp[i].a = gp[i].r;
        sg.SetPixels(sp); sg.Apply();
        File.WriteAllBytes(specGlossPath, sg.EncodeToPNG());
        AssetDatabase.ImportAsset(specGlossPath);

        var scan = new Material(Shader.Find("Standard (Specular setup)")) { name = "ShutterScan" };
        scan.SetTexture("_MainTex", Tex("_1K_BaseColor.jpg"));
        scan.SetTexture("_SpecGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(specGlossPath)); scan.EnableKeyword("_SPECGLOSSMAP");
        scan.SetFloat("_GlossMapScale", 1f);
        scan.SetTexture("_BumpMap", Tex("_1K_Normal.jpg")); scan.EnableKeyword("_NORMALMAP");
        scan.SetTexture("_OcclusionMap", Tex("_1K_AO.jpg"));

        // Flat: average albedo, same gloss as WorldBuilder.Mat (0.12), no maps.
        var bc = Tex("_1K_BaseColor.jpg").GetPixels(); Color avg = Color.black;
        foreach (var c in bc) avg += c; avg /= bc.Length; avg.a = 1;
        var flat = new Material(Shader.Find("Standard")) { name = "ShutterFlat", color = avg };
        flat.SetFloat("_Glossiness", .12f);
        Debug.Log("SHUTTER_TRIAL flat colour " + avg);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.62f, .68f, .76f);
        RenderSettings.ambientEquatorColor = new Color(.50f, .50f, .48f);
        RenderSettings.ambientGroundColor = new Color(.30f, .29f, .27f);
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = .9f; sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(42f, -25f, 0f); // lights the street-facing (-Z) fronts

        Box("Road", new Vector3(0, -.05f, -9), new Vector3(60, .1f, 14), new Color(.16f, .18f, .20f));
        Box("Sidewalk", new Vector3(0, .07f, -1), new Vector3(60, .15f, 4), new Color(.65f, .63f, .58f));
        // Two shop buildings, game building colour; ground floor faces -Z at z=1.
        Box("BuildingA", new Vector3(-6.5f, 4, 4), new Vector3(12, 8, 6), new Color(.70f, .67f, .60f));
        Box("BuildingB", new Vector3(6.5f, 4, 4), new Vector3(12, 8, 6), new Color(.70f, .67f, .60f));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + Stem + ".fbx");
        Bounds size = default;
        for (int i = 0; i < 5; i++)
        {
            size = Shutter(prefab, scan, new Vector3(-10.6f + i * 2.05f, .15f, 1f));
            Shutter(prefab, flat, new Vector3(2.4f + i * 2.05f, .15f, 1f));
        }
        Debug.Log($"SHUTTER_TRIAL size {size.size.x:F2} x {size.size.y:F2} x {size.size.z:F2} m");

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../art-source/megascans-trial"));
        Shot(new Vector3(0, 1.25f, -12f), new Vector3(0, 2.2f, 1f), 50f, Path.Combine(outDir, "unity-trial-street.png"));
        Shot(new Vector3(-1.5f, 1.25f, -5f), new Vector3(-1.5f, 1.6f, 1f), 62f, Path.Combine(outDir, "unity-trial-close.png"));
        Debug.Log("SHUTTER_TRIAL_OK");
    }

    static void Importer(string file, System.Action<TextureImporter> set)
    {
        var t = (TextureImporter)AssetImporter.GetAtPath(Dir + file); set(t); t.SaveAndReimport();
    }

    static Texture2D Tex(string suffix) => AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + Stem + suffix);

    static void Box(string name, Vector3 pos, Vector3 scale, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
        g.transform.position = pos; g.transform.localScale = scale;
        var m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", .12f);
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    // Places one shutter with its bottom-centre at pos, flush against the wall plane z=pos.z.
    static Bounds Shutter(GameObject prefab, Material mat, Vector3 pos)
    {
        var g = (GameObject)Object.Instantiate(prefab);
        g.transform.rotation = Quaternion.Euler(0, 180f, 0) * g.transform.rotation; // FBX front faces +Z
        foreach (var r in g.GetComponentsInChildren<Renderer>())
        {
            var ms = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < ms.Length; i++) ms[i] = mat;
            r.sharedMaterials = ms;
        }
        var b = Bounds(g);
        g.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.max.z);
        return b;
    }

    static Bounds Bounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static void Shot(Vector3 from, Vector3 at, float fov, string path)
    {
        var cam = new GameObject("Cam").AddComponent<Camera>();
        cam.transform.position = from; cam.transform.LookAt(at); cam.fieldOfView = fov;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.72f, .80f, .88f);
        var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(cam.gameObject); rt.Release();
        Debug.Log("SHUTTER_TRIAL wrote " + path);
    }

    static void Fail() { if (Application.isBatchMode) EditorApplication.Exit(1); }
}
