using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

/// <summary>
/// Watches the live simulation and asks Alucia to react when a species' situation genuinely changes:
/// it goes extinct, is added, starts starving (its food ran out), gets over-predated (too many predators),
/// or overpopulates (too few predators left to keep it in check). The wording comes from
/// <c>alucia_lines.csv</c> — events <c>species.starving / species.overpredated / species.overpopulated /
/// species.extinct / species.added</c> — scoped per species where a checker authored a species-specific row.
///
/// It reads <see cref="EcosystemSimulationGPU.GetSpeciesStatus"/> (the cause-aware status, NOT the raw tick
/// pressure), reports each concerning state at most once per entry, waits a "settle" window after a species'
/// count changes so a freshly-added school isn't instantly labelled, and cools down per species to avoid nagging.
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
    public bool reactStarving = true;
    public bool reactOverpredated = true;
    public bool reactOverpopulated = true;
    public bool reactExtinct = true;
    [Tooltip("Fires ONLY the first time a species is ever added (not on later re-adds). Safe to tick on. " +
             "Species already present when the experience starts don't count as a first-time add.")]
    public bool reactAdded = false;

    [Header("Timing")]
    [Tooltip("How often (seconds) to check the ecosystem for changes.")]
    public float checkInterval = 2f;
    [Tooltip("Minimum seconds between two reactions about the SAME species (anti-nag).")]
    public float perSpeciesCooldown = 25f;
    [Tooltip("Ignore the first few seconds so the intro can play and the ocean can settle.")]
    public float startupGrace = 8f;
    [Tooltip("After a species' count changes (e.g. the player just added or removed one), wait this long " +
             "before reacting to its balance — so a freshly-added species isn't instantly labelled.")]
    public float settleSeconds = 5f;

    [Header("Debug")]
    [Tooltip("Log every check to the Console: each live species' count + status, and why it did/didn't speak. Turn OFF for the exhibit.")]
    public bool debugLog = false;

    private readonly Dictionary<SpeciesDataGPU, int> _lastCount = new Dictionary<SpeciesDataGPU, int>();
    private readonly Dictionary<SpeciesDataGPU, EcosystemSimulationGPU.SpeciesStatus> _lastReported
        = new Dictionary<SpeciesDataGPU, EcosystemSimulationGPU.SpeciesStatus>();
    private readonly Dictionary<SpeciesDataGPU, float> _changedAt = new Dictionary<SpeciesDataGPU, float>();
    private readonly Dictionary<SpeciesDataGPU, float> _lastSpokeAt = new Dictionary<SpeciesDataGPU, float>();
    // Species that have been added at least once (so 'species.added' only fires on the FIRST-ever add).
    private readonly HashSet<SpeciesDataGPU> _everAdded = new HashSet<SpeciesDataGPU>();

    private float _timer;
    private float _age;
    private bool _seeded;

    void Update()
    {
        if (simulation == null || simulation.Ecosystem == null || alucia == null)
        {
            if (debugLog) Debug.Log($"[AluciaEcology] NOT running: simulation={simulation != null}, " +
                $"ecosystem={simulation != null && simulation.Ecosystem != null}, alucia={alucia != null}");
            return;
        }

        _age += Time.deltaTime;
        _timer += Time.deltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;

        Evaluate();
    }

    void Evaluate()
    {
        float now = Time.unscaledTime;
        List<SpeciesDataGPU> species = simulation.Ecosystem.Species;

        for (int i = 0; i < species.Count; i++)
        {
            SpeciesDataGPU s = species[i];
            if (s == null) continue;

            int count = simulation.CountCommittedGroups(s);
            EcosystemSimulationGPU.SpeciesStatus status = simulation.GetSpeciesStatus(s);

            if (debugLog && count > 0)
                Debug.Log($"[AluciaEcology] {(string.IsNullOrEmpty(s.SpeciesName) ? "?" : s.SpeciesName)}: " +
                    $"count={count} status={status} concerning={IsConcerning(status)} seeded={_seeded} age={_age:F0}/{startupGrace:F0}");

            int prevCount = _lastCount.TryGetValue(s, out int pc) ? pc : 0;
            if (count != prevCount) _changedAt[s] = now;

            // Seed a baseline on the first pass / during startup grace, so we don't react to pre-existing state.
            if (_seeded && _age >= startupGrace)
            {
                if (prevCount > 0 && count == 0)
                {
                    if (reactExtinct) Speak("species.extinct", s, count);
                    _lastReported[s] = EcosystemSimulationGPU.SpeciesStatus.Absent;
                }
                else if (prevCount == 0 && count > 0)
                {
                    // Only the FIRST time this species is ever added — not on a later re-add.
                    if (reactAdded && !_everAdded.Contains(s)) Speak("species.added", s, count);
                    _everAdded.Add(s);
                    _lastReported[s] = EcosystemSimulationGPU.SpeciesStatus.Balanced; // fresh — nothing concerning yet
                }
                else if (count > 0)
                {
                    float changedAt = _changedAt.TryGetValue(s, out float c) ? c : 0f;
                    bool settled = now - changedAt >= settleSeconds;
                    var lastReported = _lastReported.TryGetValue(s, out var lr) ? lr : EcosystemSimulationGPU.SpeciesStatus.Balanced;

                    if (IsConcerning(status))
                    {
                        if (debugLog) Debug.Log($"[AluciaEcology]   -> {s.SpeciesName} CONCERNING={status}: " +
                            $"settled={settled}, lastReported={lastReported}, " +
                            $"cooldownOK={!(_lastSpokeAt.TryGetValue(s, out var dlt) && now - dlt < perSpeciesCooldown)}");
                        // Fire once per fresh entry into a concerning state, only after it has settled.
                        if (settled && status != lastReported)
                        {
                            if (SpeakFor(status, s, count))
                                _lastReported[s] = status;
                        }
                    }
                    else
                    {
                        // Balanced again → reset so re-entering a concerning state later fires afresh.
                        _lastReported[s] = EcosystemSimulationGPU.SpeciesStatus.Balanced;
                    }
                }
            }
            else if (count > 0)
            {
                // Startup / seeding phase: anything already present counts as "already added", so it
                // is never mistaken for a first-time add once we go live.
                _everAdded.Add(s);
            }

            _lastCount[s] = count;
        }
        _seeded = true;
    }

    static bool IsConcerning(EcosystemSimulationGPU.SpeciesStatus s)
        => s == EcosystemSimulationGPU.SpeciesStatus.Starving
        || s == EcosystemSimulationGPU.SpeciesStatus.OverPredated
        || s == EcosystemSimulationGPU.SpeciesStatus.Overpopulated;

    // Maps a concerning status to its CSV event + enable toggle, then speaks. Returns true if it fired.
    bool SpeakFor(EcosystemSimulationGPU.SpeciesStatus status, SpeciesDataGPU s, int count)
    {
        switch (status)
        {
            case EcosystemSimulationGPU.SpeciesStatus.Starving:
                if (!reactStarving) return false;
                return Speak("species.starving", s, count);
            case EcosystemSimulationGPU.SpeciesStatus.OverPredated:
                if (!reactOverpredated) return false;
                return Speak("species.overpredated", s, count);
            case EcosystemSimulationGPU.SpeciesStatus.Overpopulated:
                if (!reactOverpopulated) return false;
                return Speak("species.overpopulated", s, count);
            default:
                return false;
        }
    }

    // Speaks a line for an event, scoped to the species. Respects the per-species cooldown. Returns true if it spoke.
    bool Speak(string evt, SpeciesDataGPU s, int count)
    {
        float now = Time.unscaledTime;
        if (_lastSpokeAt.TryGetValue(s, out float t) && now - t < perSpeciesCooldown) return false;

        string name = string.IsNullOrEmpty(s.SpeciesName) ? "species" : s.SpeciesName;
        AluciaLines.Line line = AluciaLines.GetLine(evt, name);
        string text = line.Found ? line.Text : DefaultFor(evt, name);
        text = text.Replace("{species}", name).Replace("{count}", count.ToString());

        // line.Audio carries the picked variant's clip name, so the bubble is held for the
        // length of THAT recording. Rows here are authored per species, so a clip that names
        // the fish out loud still matches the text after the {species} substitution above.
        //
        // Extinction cuts in; everything else waits its turn. Alucia drops a Normal line rather than
        // talking over herself, and a species vanishing is both irreversible and the point of the
        // exhibit, so it is the one ecology event worth interrupting for.
        AluciaController.Priority priority = evt == "species.extinct"
            ? AluciaController.Priority.High
            : AluciaController.Priority.Normal;

        // Only spend the cooldown if she ACTUALLY spoke. Say can decline (she is mid-line, muted, or
        // the intro has not finished), and stamping regardless meant a dropped line also silenced this
        // species for the whole perSpeciesCooldown - the message was lost rather than delayed. Now a
        // declined line simply gets retried on the next tick, which is what makes dropping safe.
        bool spoke = alucia.Say(text, ParseMood(line.Found ? line.Mood : "Warn"), false, line.Audio, priority);
        if (!spoke) return false;

        _lastSpokeAt[s] = now;
        return true;
    }

    // Fallbacks if the CSV/event is missing entirely (the sheet normally supplies these).
    static string DefaultFor(string evt, string name)
    {
        switch (evt)
        {
            case "species.extinct":      return "The " + name + " are gone from the reef entirely.";
            case "species.added":        return "A new group of " + name + " just swam in!";
            case "species.starving":     return "The " + name + " have run out of food — there's nothing left for them to eat.";
            case "species.overpredated": return "The " + name + " are being hunted faster than they can recover.";
            default:                     return "The " + name + " are overpopulating — nothing's keeping them in check!";
        }
    }

    static AluciaController.Mood ParseMood(string mood)
    {
        if (string.Equals(mood, "Warn", System.StringComparison.OrdinalIgnoreCase)) return AluciaController.Mood.Warn;
        if (string.Equals(mood, "Win",  System.StringComparison.OrdinalIgnoreCase)) return AluciaController.Mood.Win;
        return AluciaController.Mood.Calm;
    }
}
