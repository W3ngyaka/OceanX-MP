using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

/// <summary>
/// Watches the live simulation and asks Alucia to react when a species over-populates, under-populates,
/// goes extinct, or is added. The actual wording comes from <c>alucia_lines.csv</c> (events
/// <c>species.overpopulated / species.underpopulated / species.extinct / species.added</c>), scoped per
/// species where a checker has authored a species-specific row — so new hints are added in the sheet with
/// no code change.
///
/// SETUP (host scene): drop this on an always-active object and assign the scene's
/// <see cref="EcosystemSimulationGPU"/> and <see cref="AluciaController"/>.
/// </summary>
public class AluciaEcologyEvents : MonoBehaviour
{
    [Header("Refs")]
    public EcosystemSimulationGPU simulation;
    public AluciaController alucia;

    [Header("Which reactions to enable")]
    public bool reactOverpopulated = true;
    public bool reactUnderpopulated = true;
    public bool reactExtinct = true;
    [Tooltip("Usually handled by the 'new species' reveal card, so off by default.")]
    public bool reactAdded = false;

    [Header("Timing")]
    [Tooltip("How often (seconds) to check the ecosystem for changes.")]
    public float checkInterval = 2f;
    [Tooltip("Minimum seconds between two reactions about the SAME species (anti-nag).")]
    public float perSpeciesCooldown = 25f;
    [Tooltip("Ignore the first few seconds so the intro can play and the ocean can settle.")]
    public float startupGrace = 8f;

    private readonly Dictionary<SpeciesDataGPU, EcosystemSimulationGPU.SpeciesBalance> _lastBalance
        = new Dictionary<SpeciesDataGPU, EcosystemSimulationGPU.SpeciesBalance>();
    private readonly Dictionary<SpeciesDataGPU, int> _lastCount = new Dictionary<SpeciesDataGPU, int>();
    private readonly Dictionary<SpeciesDataGPU, float> _lastSpokeAt = new Dictionary<SpeciesDataGPU, float>();

    private float _timer;
    private float _age;
    private bool _seeded;

    void Update()
    {
        if (simulation == null || simulation.Ecosystem == null || alucia == null) return;

        _age += Time.deltaTime;
        _timer += Time.deltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;

        Evaluate();
    }

    void Evaluate()
    {
        List<SpeciesDataGPU> species = simulation.Ecosystem.Species;
        for (int i = 0; i < species.Count; i++)
        {
            SpeciesDataGPU s = species[i];
            if (s == null) continue;

            int count = simulation.CountCommittedGroups(s);
            EcosystemSimulationGPU.SpeciesBalance bal = simulation.GetBalance(s);

            // Seed the baseline on the first pass so we don't react to pre-existing state.
            if (_seeded && _age >= startupGrace)
            {
                int prevCount = _lastCount.TryGetValue(s, out int pc) ? pc : 0;
                EcosystemSimulationGPU.SpeciesBalance prevBal =
                    _lastBalance.TryGetValue(s, out var pb) ? pb : EcosystemSimulationGPU.SpeciesBalance.Absent;

                if (reactExtinct && prevCount > 0 && count == 0)
                    Speak("species.extinct", s, count);
                else if (reactAdded && prevCount == 0 && count > 0)
                    Speak("species.added", s, count);
                else if (count > 0 && bal != prevBal)
                {
                    if (reactOverpopulated && bal == EcosystemSimulationGPU.SpeciesBalance.Overpopulated)
                        Speak("species.overpopulated", s, count);
                    else if (reactUnderpopulated && bal == EcosystemSimulationGPU.SpeciesBalance.Underpopulated)
                        Speak("species.underpopulated", s, count);
                }
            }

            _lastCount[s] = count;
            _lastBalance[s] = bal;
        }
        _seeded = true;
    }

    void Speak(string evt, SpeciesDataGPU s, int count)
    {
        float now = Time.unscaledTime;
        if (_lastSpokeAt.TryGetValue(s, out float t) && now - t < perSpeciesCooldown) return;

        string name = string.IsNullOrEmpty(s.SpeciesName) ? "species" : s.SpeciesName;
        AluciaLines.Line line = AluciaLines.GetLine(evt, name);
        string text = line.Found ? line.Text : DefaultFor(evt, name);
        text = text.Replace("{species}", name).Replace("{count}", count.ToString());

        alucia.Say(text, ParseMood(line.Found ? line.Mood : "Warn"));
        _lastSpokeAt[s] = now;
    }

    // Fallbacks if the CSV/event is missing entirely (the sheet normally supplies these).
    static string DefaultFor(string evt, string name)
    {
        switch (evt)
        {
            case "species.extinct":        return "The " + name + " are gone from the reef entirely.";
            case "species.added":          return "A new group of " + name + " just swam in!";
            case "species.underpopulated": return "The " + name + " are struggling — too many predators are hunting them.";
            default:                       return "The " + name + " are booming — nothing's keeping them in check!";
        }
    }

    static AluciaController.Mood ParseMood(string mood)
    {
        if (string.Equals(mood, "Warn", System.StringComparison.OrdinalIgnoreCase)) return AluciaController.Mood.Warn;
        if (string.Equals(mood, "Win",  System.StringComparison.OrdinalIgnoreCase)) return AluciaController.Mood.Win;
        return AluciaController.Mood.Calm;
    }
}
