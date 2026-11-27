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

        Street();
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

    // Cartoon pass: no textures, the mesh split into a slat body and a housing+guide-rail
    // trim, five bright two-colour schemes on the right building, scan reference on the left.
    // Writes art-source/megascans-trial/unity-trial-cartoon-*.png.
    [MenuItem("Taxi/Art/Megascans Shutter Cartoon Trial")]
    public static void RunCartoon()
    {
        if (!File.Exists(Dir + Stem + ".fbx")) { Debug.LogError("SHUTTER_TRIAL missing " + Dir + Stem + ".fbx"); Fail(); return; }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var scan = new Material(Shader.Find("Standard")) { name = "ShutterScanRef" };
        scan.SetTexture("_MainTex", Tex("_1K_BaseColor.jpg"));
        scan.SetTexture("_BumpMap", Tex("_1K_Normal.jpg")); scan.EnableKeyword("_NORMALMAP");
        scan.SetFloat("_Glossiness", .3f);
        Street();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + Stem + ".fbx");
        // body (slats), trim (housing + rails)
        var schemes = new[] {
            ("navy",    new Color(.76f, .78f, .78f), new Color(.20f, .33f, .55f)),
            ("green",   new Color(.76f, .78f, .78f), new Color(.24f, .52f, .38f)),
            ("brick",   new Color(.79f, .77f, .73f), new Color(.74f, .31f, .24f)),
            ("skyblue", new Color(.62f, .72f, .80f), new Color(.93f, .93f, .91f)),
            ("mustard", new Color(.84f, .80f, .70f), new Color(.86f, .64f, .22f)),
        };
        int trimTris = 0, allTris = 0;
        for (int i = 0; i < 5; i++)
        {
            Shutter(prefab, scan, new Vector3(-10.6f + i * 2.05f, .15f, 1f));
            var g = (GameObject)Object.Instantiate(prefab);
            g.transform.rotation = Quaternion.Euler(0, 180f, 0) * g.transform.rotation;
            var b = Bounds(g);
            g.transform.position += new Vector3(2.4f + i * 2.05f - b.center.x, .15f - b.min.y, 1f - b.max.z);
            b = Bounds(g);
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                var (t, a) = SplitTrim(mf, b);
                trimTris += t; allTris += a;
                mf.GetComponent<Renderer>().sharedMaterials = new[] { Flat(schemes[i].Item2), Flat(schemes[i].Item3) };
            }
        }
        Debug.Log($"SHUTTER_TRIAL cartoon trim {trimTris}/{allTris} triangles");
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../art-source/megascans-trial"));
        Shot(new Vector3(0, 1.25f, -12f), new Vector3(0, 2.2f, 1f), 50f, Path.Combine(outDir, "unity-trial-cartoon-street.png"));
        Shot(new Vector3(6.5f, 1.25f, -6.5f), new Vector3(6.5f, 1.7f, 1f), 62f, Path.Combine(outDir, "unity-trial-cartoon-close.png"));
        Debug.Log("SHUTTER_TRIAL_CARTOON_OK");
    }

    // Bakes the navy cartoon shutter into git-ignored Assets/ArtTrial/Resources/MegascansShutter/
    // (mesh, two materials, prefab) for ShopShutterTrial at runtime. Prefab root = bottom-centre of
    // the back face (the wall side); the shutter front faces local -Z.
    const string BakeDir = "Assets/ArtTrial/Resources/MegascansShutter/";
    [MenuItem("Taxi/Art/Bake Megascans Navy Shutter")]
    public static void BakeNavy()
    {
        if (!File.Exists(Dir + Stem + ".fbx")) { Debug.LogError("SHUTTER_TRIAL missing " + Dir + Stem + ".fbx"); Fail(); return; }
        Directory.CreateDirectory(BakeDir);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + Stem + ".fbx");
        var g = (GameObject)Object.Instantiate(prefab);
        g.transform.rotation = Quaternion.Euler(0, 180f, 0) * g.transform.rotation;
        var b = Bounds(g);
        var anchor = new Vector3(b.center.x, b.min.y, b.max.z);
        var verts = new System.Collections.Generic.List<Vector3>(); var normals = new System.Collections.Generic.List<Vector3>();
        var body = new System.Collections.Generic.List<int>(); var trim = new System.Collections.Generic.List<int>();
        float hTop = b.min.y + .84f * b.size.y, rail = .445f * b.size.x;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            var m = mf.sharedMesh; var v = m.vertices; var n = m.normals; var tris = m.triangles; int baseIndex = verts.Count;
            for (int i = 0; i < v.Length; i++) { verts.Add(mf.transform.TransformPoint(v[i]) - anchor); normals.Add(mf.transform.TransformDirection(n[i]).normalized); }
            for (int i = 0; i < tris.Length; i += 3)
            {
                float minY = float.MaxValue, minX = float.MaxValue;
                for (int k = 0; k < 3; k++) { var p = verts[baseIndex + tris[i + k]] + anchor; minY = Mathf.Min(minY, p.y); minX = Mathf.Min(minX, Mathf.Abs(p.x - b.center.x)); }
                var list = minY > hTop || minX > rail ? trim : body;
                for (int k = 0; k < 3; k++) list.Add(baseIndex + tris[i + k]);
            }
        }
        Object.DestroyImmediate(g);
        var mesh = new Mesh { name = "ShutterNavyMesh" };
        mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.subMeshCount = 2;
        mesh.SetTriangles(body, 0); mesh.SetTriangles(trim, 1); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, BakeDir + "ShutterNavyMesh.asset");
        var bodyMat = Flat(new Color(.76f, .78f, .78f)); bodyMat.name = "ShutterNavyBody";
        var trimMat = Flat(new Color(.20f, .33f, .55f)); trimMat.name = "ShutterNavyTrim";
        AssetDatabase.CreateAsset(bodyMat, BakeDir + "ShutterNavyBody.mat");
        AssetDatabase.CreateAsset(trimMat, BakeDir + "ShutterNavyTrim.mat");
        var root = new GameObject("ShutterNavy");
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = root.AddComponent<MeshRenderer>(); r.sharedMaterials = new[] { bodyMat, trimMat };
        PrefabUtility.SaveAsPrefabAsset(root, BakeDir + "ShutterNavy.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"SHUTTER_BAKE_OK verts={verts.Count} body={body.Count / 3} trim={trim.Count / 3} size={b.size}");
    }

    static Material Flat(Color c)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Glossiness", .12f); return m;
    }

    // Two submeshes: 0 = slat body, 1 = trim. Trim is the top housing band and the side
    // guide rails, picked by triangle centre position inside the placed shutter bounds.
    static (int trim, int all) SplitTrim(MeshFilter mf, Bounds b)
    {
        var mesh = Object.Instantiate(mf.sharedMesh);
        var v = mesh.vertices; var tris = mesh.triangles;
        var body = new System.Collections.Generic.List<int>(); var trim = new System.Collections.Generic.List<int>();
        float hTop = b.min.y + .84f * b.size.y, rail = .445f * b.size.x;
        for (int i = 0; i < tris.Length; i += 3)
        {
            // A triangle is trim only when all three corners are in the trim zone, so long
            // slat triangles reaching into a rail stay body colour (no saw-tooth edge).
            float minY = float.MaxValue, minX = float.MaxValue;
            for (int k = 0; k < 3; k++)
            {
                var p = mf.transform.TransformPoint(v[tris[i + k]]);
                minY = Mathf.Min(minY, p.y); minX = Mathf.Min(minX, Mathf.Abs(p.x - b.center.x));
            }
            var list = minY > hTop || minX > rail ? trim : body;
            list.Add(tris[i]); list.Add(tris[i + 1]); list.Add(tris[i + 2]);
        }
        mesh.subMeshCount = 2;
        mesh.SetTriangles(body, 0); mesh.SetTriangles(trim, 1);
        mf.sharedMesh = mesh;
        return (trim.Count / 3, tris.Length / 3);
    }

    static void Street()
    {
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
