using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// In-editor debug panel for exercising the GPU ecosystem WITHOUT the tablet or netcode.
    ///
    /// Drop this on any GameObject in the host scene (Boids_Demo). At Play time it draws an
    /// on-screen panel (OnGUI) with:
    ///   • the live <see cref="EcosystemSimulationGPU.EcoHealth01"/> score + a coloured bar
    ///   • one row per species with its school count / cap and [-] / [+] buttons
    ///
    /// The buttons call <see cref="EcosystemSimulationGPU.AddSpecies"/> / RemoveSpecies directly,
    /// so this drives the exact same start-at-zero / ReinitializeBuffers path the tablet RPCs use —
    /// letting you test start-at-zero AND watch eco-health move in a single editor Play session.
    ///
    /// Purely a development aid: it never touches the network layer. Safe to leave in the scene
    /// (toggle it off with the hotkey) or delete before shipping.
    /// </summary>
    public class EcosystemDebugHarnessGPU : MonoBehaviour
    {
        [Tooltip("The simulation to drive. Auto-found in the scene if left empty.")]
        [SerializeField] private EcosystemSimulationGPU _simulation;

        [Tooltip("Key that shows / hides the debug panel.")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

        [Tooltip("Whether the panel is visible when the scene starts.")]
        [SerializeField] private bool _visibleOnStart = true;

        private bool _visible;
        private Vector2 _scroll;
        private GUIStyle _headerStyle;
        private Texture2D _barBg;
        private Texture2D _barFill;

        private void Awake()
        {
            _visible = _visibleOnStart;
            if (_simulation == null)
                _simulation = FindFirstObjectByType<EcosystemSimulationGPU>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            if (_simulation == null)
            {
                GUI.Box(new Rect(10, 10, 320, 40), "No EcosystemSimulationGPU in scene");
                return;
            }

            EnsureStyles();

            const float width = 360f;
            float height = Mathf.Min(Screen.height - 20f, 140f + RowCount() * 26f);
            GUILayout.BeginArea(new Rect(10, 10, width, height), GUI.skin.box);

            GUILayout.Label("ECOSYSTEM DEBUG  (toggle: " + _toggleKey + ")", _headerStyle);

            DrawHealthBar();
            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawSpeciesRows();
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ drawing

        private void DrawHealthBar()
        {
            float health = _simulation.EcoHealth01;

            GUILayout.Label($"Eco-Health: {(health * 100f):0}%");

            Rect r = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, _barBg);

            Rect fill = new Rect(r.x, r.y, r.width * Mathf.Clamp01(health), r.height);
            // low = red, mid = orange, high = cyan — matches the prototype's bar colour logic.
            GUI.color = health < 0.35f ? new Color(0.9f, 0.25f, 0.2f)
                      : health < 0.70f ? new Color(1f, 0.6f, 0.1f)
                      : new Color(0.1f, 0.8f, 0.95f);
            GUI.DrawTexture(fill, _barFill);
            GUI.color = Color.white;
        }

        private void DrawSpeciesRows()
        {
            List<SpeciesDataGPU> species = _simulation.Ecosystem != null ? _simulation.Ecosystem.Species : null;
            if (species == null || species.Count == 0)
            {
                GUILayout.Label("No species in EcosystemDefinitionGPU.");
                return;
            }

            for (int i = 0; i < species.Count; i++)
            {
                SpeciesDataGPU s = species[i];
                if (s == null) continue;

                int count = _simulation.CountGroups(s);
                int max = _simulation.GetMaxSchools(s);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{s.SpeciesName}  [{s.Role}]", GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{count}/{max}", GUILayout.Width(44));

                GUI.enabled = count > 0;
                if (GUILayout.Button("-", GUILayout.Width(26)))
                    _simulation.RemoveSpecies(s);

                GUI.enabled = count < max;
                if (GUILayout.Button("+", GUILayout.Width(26)))
                    _simulation.AddSpecies(s);

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private int RowCount()
        {
            if (_simulation.Ecosystem == null || _simulation.Ecosystem.Species == null) return 0;
            return _simulation.Ecosystem.Species.Count;
        }

        // ------------------------------------------------------------------ styling

        private void EnsureStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            }
            if (_barBg == null)
            {
                _barBg = MakeTex(new Color(0f, 0f, 0f, 0.5f));
                _barFill = MakeTex(Color.white);
            }
        }

        private static Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
