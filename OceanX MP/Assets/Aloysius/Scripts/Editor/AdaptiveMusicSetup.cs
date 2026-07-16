using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// One-click configuration for AdaptiveMusicSystem.
//
// Two menus under  OceanX :
//   • "Setup Adaptive Music (full)"        — first-time setup. Adds the component, assigns the
//                                            PLACEHOLDER clips (Ambient/Somber/Warm/Thrive), and sets
//                                            the band curves. ⚠ OVERWRITES clip assignments.
//   • "Set Music Band Curves (keep clips)" — only rewrites the health->volume curves on the existing
//                                            layers, IN ORDER. Leaves your clips, groups, params, names
//                                            untouched. Use this after you've assigned your own audio.
//
// BANDS (exclusive, with a crossfade at each edge so transitions are smooth, not hard cuts):
//   layer 0  Bed       0%  – 30%
//   layer 1  Sorrow    30% – 60%
//   layer 2  Hope      60% – 90%
//   layer 3  Flourish  90% – 100%
// At each boundary the two neighbours cross at ~0.5 over a 10%-wide window, so exactly one track plays
// in the middle of a band and two briefly blend while crossing. Nothing else plays on top.
//
// After running either: SAVE THE SCENE (Ctrl+S).
public static class AdaptiveMusicSetup
{
    const string SoundsDir = "Assets/Sounds/";

    [MenuItem("OceanX/Setup Adaptive Music (full)")]
    static void SetupFull()
    {
        var go = FindTarget();
        if (go == null) return;

        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        var amp = go.GetComponent<AdaptiveMusicSystem>() ?? Undo.AddComponent<AdaptiveMusicSystem>(go);
        Undo.RecordObject(amp, "Setup Adaptive Music");

        var missing = new List<string>();
        AudioClip bed      = LoadClip("Ambient.mp3", missing);
        AudioClip sorrow   = LoadClip("Somber.mp3",  missing);
        AudioClip hope     = LoadClip("Warm.mp3",    missing);
        AudioClip flourish = LoadClip("Thrive.mp3",  missing);

        var c = BandCurves();
        amp.layers = new List<AdaptiveMusicSystem.Layer>
        {
            new AdaptiveMusicSystem.Layer { name = "Bed",      clip = bed,      exposedParam = "BedVol",      healthToVolume = c[0] },
            new AdaptiveMusicSystem.Layer { name = "Sorrow",   clip = sorrow,   exposedParam = "SorrowVol",   healthToVolume = c[1] },
            new AdaptiveMusicSystem.Layer { name = "Hope",     clip = hope,     exposedParam = "HopeVol",     healthToVolume = c[2] },
            new AdaptiveMusicSystem.Layer { name = "Flourish", clip = flourish, exposedParam = "FlourishVol", healthToVolume = c[3] },
        };
        amp.masterVolume     = 0.6f;
        amp.transitionSeconds = 2f;
        amp.silenceDb        = -80f;
        amp.swellVolume      = 0.8f;
        amp.showDebugReadout = true;

        EditorUtility.SetDirty(amp);
        EditorSceneManager.MarkSceneDirty(go.scene);

        string msg = $"Full setup done on '{go.name}'. Removed {removed} missing script(s).\n" +
                     "4 layers wired with PLACEHOLDER clips + exclusive band curves (0-30 / 30-60 / 60-90 / 90-100).\n";
        if (missing.Count > 0) msg += "⚠ Missing clips: " + string.Join(", ", missing) + "\n";
        msg += "\nUsing your own audio? Assign clips to the layers, then run 'Set Music Band Curves (keep clips)'.\n" +
               "Then Ctrl+S. Play + tick Override Health + drag Debug Health 0→1 to test.";
        Log(msg);
    }

    [MenuItem("OceanX/Set Music Band Curves (keep clips)")]
    static void SetCurvesOnly()
    {
        var go = FindTarget();
        if (go == null) return;

        var amp = go.GetComponent<AdaptiveMusicSystem>();
        if (amp == null || amp.layers == null || amp.layers.Count == 0)
        {
            EditorUtility.DisplayDialog("Adaptive Music",
                "No AdaptiveMusicSystem with layers found on '" + go.name + "'.\n\nRun 'Setup Adaptive " +
                "Music (full)' first, or add the component and its layers.", "OK");
            return;
        }

        Undo.RecordObject(amp, "Set Music Band Curves");
        var c = BandCurves();
        int n = Mathf.Min(4, amp.layers.Count);
        for (int i = 0; i < n; i++)
            if (amp.layers[i] != null) amp.layers[i].healthToVolume = c[i];

        EditorUtility.SetDirty(amp);
        EditorSceneManager.MarkSceneDirty(go.scene);

        string extra = amp.layers.Count > 4
            ? $"\n(You have {amp.layers.Count} layers; only the first 4 got band curves.)" : "";
        Log($"Band curves applied to the first {n} layer(s) on '{go.name}', clips untouched.\n" +
            "Order used: 0=Bed(0-30), 1=Sorrow(30-60), 2=Hope(60-90), 3=Flourish(90-100)." + extra +
            "\n\nCtrl+S to save. Play + Override Health + drag Debug Health 0→1 to test.");
    }

    // ---- shared -------------------------------------------------------------------------------

    // Four exclusive-band curves with a 10%-wide crossfade centred on each boundary (0.30/0.60/0.90).
    // Health is on X (0-1), volume on Y (0-1). Curves clamp outside their key range.
    static AnimationCurve[] BandCurves()
    {
        return new[]
        {
            // Bed: full to 0.25, cross down to 0 by 0.35
            Curve((0f,1f),(0.25f,1f),(0.35f,0f),(1f,0f)),
            // Sorrow: up 0.25→0.35, hold, down 0.55→0.65
            Curve((0f,0f),(0.25f,0f),(0.35f,1f),(0.55f,1f),(0.65f,0f),(1f,0f)),
            // Hope: up 0.55→0.65, hold, down 0.85→0.95
            Curve((0f,0f),(0.55f,0f),(0.65f,1f),(0.85f,1f),(0.95f,0f),(1f,0f)),
            // Flourish: up 0.85→0.95, hold to 1
            Curve((0f,0f),(0.85f,0f),(0.95f,1f),(1f,1f)),
        };
    }

    static GameObject FindTarget()
    {
        GameObject go = null;
        if (Selection.activeGameObject != null &&
            (Selection.activeGameObject.GetComponent<AdaptiveMusicSystem>() != null ||
             Selection.activeGameObject.name == "AudioManager"))
            go = Selection.activeGameObject;
        if (go == null) go = GameObject.Find("AudioManager");

        if (go == null)
            EditorUtility.DisplayDialog("Adaptive Music",
                "Couldn't find the AudioManager object.\n\nOpen the host scene (the one with " +
                "EcosystemSimulationGPU), select the AudioManager (or ensure one named exactly " +
                "\"AudioManager\" exists), and run this again.", "OK");
        return go;
    }

    static AudioClip LoadClip(string file, List<string> missing)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundsDir + file);
        if (clip == null) missing.Add(file);
        return clip;
    }

    static AnimationCurve Curve(params (float t, float v)[] keys)
    {
        var frames = new Keyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++) frames[i] = new Keyframe(keys[i].t, keys[i].v);
        return new AnimationCurve(frames);
    }

    static void Log(string msg)
    {
        Debug.Log("[AdaptiveMusicSetup] " + msg.Replace("\n", "  "));
        EditorUtility.DisplayDialog("Adaptive Music", msg, "OK");
    }
}
