using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AdaptiveMusicSetup
{
    static readonly string[] MoodNames    = { "Somber", "Unstable", "Hopeful", "Thriving" };
    static readonly string[] ExposedParams = { "BedVol", "SorrowVol", "HopeVol", "FlourishVol" };
    static readonly float[]  DefaultEdges  = { 0.30f, 0.60f, 0.90f };

    [MenuItem("OceanX/Setup Adaptive Music (full)")]
    static void SetupFull()
    {
        var go = FindTarget();
        if (go == null) return;

        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        var amp = go.GetComponent<AdaptiveMusicSystem>() ?? Undo.AddComponent<AdaptiveMusicSystem>(go);
        Undo.RecordObject(amp, "Setup Adaptive Music");

        if (amp.layers == null) amp.layers = new List<AdaptiveMusicSystem.Layer>();
        while (amp.layers.Count < 4) amp.layers.Add(new AdaptiveMusicSystem.Layer());

        var missingClips = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var l = amp.layers[i] ?? (amp.layers[i] = new AdaptiveMusicSystem.Layer());
            l.name = MoodNames[i];
            if (string.IsNullOrEmpty(l.exposedParam)) l.exposedParam = ExposedParams[i];
            if (l.clip == null) missingClips.Add(MoodNames[i]);
        }

        ApplyBandsAndFeel(amp);

        EditorUtility.SetDirty(amp);
        EditorSceneManager.MarkSceneDirty(go.scene);

        string msg = $"Full setup done on '{go.name}'. Removed {removed} missing script(s).\n" +
                     "4 mood layers ready (Somber / Unstable / Hopeful / Thriving) with band edges " +
                     "0.30 / 0.60 / 0.90.\n\nAssign each mood's SONG (and its mixer group) in the Inspector — " +
                     "existing clips were left untouched.";
        if (missingClips.Count > 0) msg += "\n⚠ Still need a clip: " + string.Join(", ", missingClips);
        msg += "\n\nThen Ctrl+S. Play + tick Override Health + drag Debug Health 0→1 to hear the switches.";
        Log(msg);
    }

    [MenuItem("OceanX/Set Music Bands (keep clips)")]
    static void SetBandsOnly()
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

        Undo.RecordObject(amp, "Set Music Bands");
        int n = Mathf.Min(MoodNames.Length, amp.layers.Count);
        for (int i = 0; i < n; i++)
            if (amp.layers[i] != null) amp.layers[i].name = MoodNames[i];
        ApplyBandsAndFeel(amp);

        EditorUtility.SetDirty(amp);
        EditorSceneManager.MarkSceneDirty(go.scene);

        string extra = amp.layers.Count != 4
            ? $"\n(You have {amp.layers.Count} layers; edges were set for that count.)" : "";
        Log($"Bands + transition defaults applied on '{go.name}', clips untouched." + extra +
            "\n\nCtrl+S to save. Play + Override Health + drag Debug Health 0→1 to test.");
    }

    static void ApplyBandsAndFeel(AdaptiveMusicSystem amp)
    {

        int need = Mathf.Max(0, amp.layers.Count - 1);
        if (need == DefaultEdges.Length)
            amp.bandEdges = (float[])DefaultEdges.Clone();
        else
        {
            amp.bandEdges = new float[need];
            for (int i = 0; i < need; i++) amp.bandEdges[i] = (i + 1f) / (need + 1f);
        }

        amp.masterVolume     = 0.6f;
        amp.crossfadeSeconds = 3f;
        amp.healthSmoothing  = 2f;
        amp.hysteresis       = 0.05f;
        amp.minMoodSeconds   = 8f;
        amp.silenceDb        = -80f;
        amp.swellVolume      = 0.8f;
        amp.showDebugReadout = true;
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

    static void Log(string msg)
    {
        Debug.Log("[AdaptiveMusicSetup] " + msg.Replace("\n", "  "));
        EditorUtility.DisplayDialog("Adaptive Music", msg, "OK");
    }
}
