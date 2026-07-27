using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;   // SpeciesDataGPU / SpeciesRoleGPU / EcosystemSimulationGPU

// Bottom-left "how do I balance this?" panel.
//
// Every line is derived from the SIMULATION's own verdict (GetSpeciesStatus, synced per species)
// rather than a second opinion computed up here. Eco-health is a weighted mix of diversity,
// per-species balance and apex presence, so a panel that recomputed any of that itself would
// eventually disagree with the bar and send visitors chasing fixes that don't move it.
//
// Two rules keep it from becoming an answer key:
//
//   1. GROUPED, NOT ENUMERATED. Problems collapse by kind, so eight missing species is one line
//      about a sparse reef, not eight lines naming each fish.
//   2. ESCALATING DETAIL. It opens vague and only gets specific when the visitor is genuinely
//      stuck — measured as eco-health failing to reach a new high for a while. Any improvement
//      drops it back to vague, so a visitor who is making progress is never handed the answer.
public class BalanceAdvisor : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup group;
    public TMP_Text headerLabel;
    public TMP_Text bodyLabel;

    [Header("Behaviour")]
    [Tooltip("How many problem categories to show at once. Two is usually plenty — more reads as a checklist.")]
    [Range(1, 4)] public int maxLines = 2;

    [Tooltip("Seconds between recalculations.")]
    public float refreshInterval = 0.5f;

    [Tooltip("Hide the panel until the experience has started.")]
    public bool hideBeforeStart = true;

    [Tooltip("Hide while the HOW TO PLAY panel is open, so the two don't compete for attention.")]
    public bool hideWhileTutorialOpen = true;

    [Header("Escalation")]
    [Tooltip("Seconds of no NEW eco-health high before the panel gets more specific. Each step of " +
             "this long moves it one level: vague -> names the species -> names the fix.")]
    public float escalateAfter = 25f;

    [Tooltip("How much eco-health must rise above its previous best to count as progress and reset " +
             "the panel to vague. Small, so genuine progress is recognised quickly.")]
    public float progressEpsilon = 0.01f;

    [Header("Copy")]
    public string headerText = "HOW TO BALANCE";
    public string balancedMessage = "The reef is balanced. Nice work!";

    private float _timer;
    private float _bestHealth = -1f;
    private float _stuckTime;
    private readonly List<Issue> _issues = new List<Issue>();
    private readonly StringBuilder _sb = new StringBuilder();

    private enum Kind { MissingApex, Missing, Overpopulated, Starving, OverPredated }

    private struct Issue
    {
        public Kind Kind;
        public int Count;              // how many species share this problem
        public SpeciesDataGPU First;   // exemplar, used once the panel is allowed to name names
        public string Fix;             // species to add/remove, null if all candidates are locked
    }

    // 0 = vague, 1 = names the species, 2 = names the fix.
    private int Level => Mathf.Clamp(Mathf.FloorToInt(_stuckTime / Mathf.Max(1f, escalateAfter)), 0, 2);

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (headerLabel != null) headerLabel.text = headerText;
    }

    void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        float elapsed = Mathf.Max(0.1f, refreshInterval);
        _timer = elapsed;
        Rebuild(elapsed);
    }

    void Rebuild(float elapsed)
    {
        var ui  = TabletEcosystemUIGPU.Instance;
        var net = EcosystemNetworkManagerGPU.Instance;

        if (ui == null || ui.Ecosystem == null || net == null) { SetShown(false); return; }
        if (hideBeforeStart && !net.HasStarted)
        {
            // Attract screen: forget the previous visitor's progress so the next one starts vague.
            _bestHealth = -1f; _stuckTime = 0f;
            SetShown(false);
            return;
        }

        // The onboarding panel owns the screen while it is up, and two sets of instructions at once
        // is just noise. Returning here also freezes the stuck clock, which is deliberate: time spent
        // reading HOW TO PLAY is not the visitor failing to balance the reef, so it must not count
        // toward escalating the hint.
        if (hideWhileTutorialOpen && TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpenOrPending)
        {
            SetShown(false);
            return;
        }

        // Progress tracking. A new high resets the ladder; otherwise the stuck clock runs.
        float health = net.GetEcoHealth();
        if (health > _bestHealth + progressEpsilon) { _bestHealth = health; _stuckTime = 0f; }
        else _stuckTime += elapsed;

        var unlock = EcosystemUnlockManagerGPU.Instance;
        _issues.Clear();

        foreach (var sp in ui.Ecosystem.Species)
        {
            if (sp == null) continue;
            int idx = ui.GetSpeciesIndex(sp);
            if (idx < 0) continue;

            bool canAdd = unlock == null || unlock.IsUnlocked(sp);
            switch (net.GetSpeciesStatus(idx))
            {
                case EcosystemSimulationGPU.SpeciesStatus.Absent:
                    if (!canAdd) break;   // locked: the visitor can't act on it, so never mention it
                    Add(sp.Role == SpeciesRoleGPU.Apex ? Kind.MissingApex : Kind.Missing, sp, null);
                    break;
                case EcosystemSimulationGPU.SpeciesStatus.Overpopulated:
                    Add(Kind.Overpopulated, sp, FirstActionable(sp.PredatorSpecies, unlock, ui));
                    break;
                case EcosystemSimulationGPU.SpeciesStatus.Starving:
                    Add(Kind.Starving, sp, FirstActionable(sp.PreySpecies, unlock, ui));
                    break;
                case EcosystemSimulationGPU.SpeciesStatus.OverPredated:
                    Add(Kind.OverPredated, sp, FirstActionable(sp.PredatorSpecies, unlock, ui));
                    break;
            }
        }

        // A collapsing reef matters more than a sparse one, so problems outrank absences.
        _issues.Sort((a, b) => Rank(a.Kind).CompareTo(Rank(b.Kind)));

        _sb.Clear();
        if (_issues.Count == 0) _sb.Append(balancedMessage);
        else
        {
            int n = Mathf.Min(maxLines, _issues.Count);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) _sb.Append('\n');
                _sb.Append("\u2022 ").Append(Line(_issues[i], Level));
            }
        }

        if (bodyLabel != null) bodyLabel.text = _sb.ToString();
        SetShown(true);
    }

    // Merge into an existing issue of the same kind rather than adding a second line for it.
    private void Add(Kind kind, SpeciesDataGPU sp, string fix)
    {
        for (int i = 0; i < _issues.Count; i++)
        {
            if (_issues[i].Kind != kind) continue;
            var e = _issues[i];
            e.Count++;
            if (e.Fix == null) e.Fix = fix;
            _issues[i] = e;
            return;
        }
        _issues.Add(new Issue { Kind = kind, Count = 1, First = sp, Fix = fix });
    }

    private static int Rank(Kind k)
    {
        switch (k)
        {
            case Kind.Starving:      return 0;   // an active collapse
            case Kind.OverPredated:  return 1;
            case Kind.Overpopulated: return 2;
            case Kind.MissingApex:   return 3;
            default:                 return 4;   // merely sparse
        }
    }

    private static string Line(Issue e, int level)
    {
        string who = e.First != null && !string.IsNullOrEmpty(e.First.SpeciesName)
                   ? e.First.SpeciesName : "One species";

        switch (e.Kind)
        {
            case Kind.Starving:
                if (level == 0) return "Something can't find enough to eat";
                if (level == 1) return $"{who} are going hungry";
                return e.Fix != null ? $"{who} are going hungry \u2014 add {e.Fix}"
                                     : $"{who} are going hungry \u2014 there are too many of them";

            case Kind.OverPredated:
                if (level == 0) return "Something is being eaten faster than it can recover";
                if (level == 1) return $"{who} are being hunted out";
                return e.Fix != null ? $"{who} are being hunted out \u2014 add more, or remove {e.Fix}"
                                     : $"{who} are being hunted out \u2014 add more of them";

            case Kind.Overpopulated:
                if (level == 0) return "Something has grown out of control";
                if (level == 1) return $"There are too many {who}";
                return e.Fix != null ? $"Too many {who} \u2014 add {e.Fix} to hunt them"
                                     : $"Too many {who} \u2014 remove a few";

            case Kind.MissingApex:
                if (level == 0) return "Nothing at the top of the food chain yet";
                if (level == 1) return "The reef has no top predator";
                return $"Add {who} \u2014 the reef has no top predator";

            default:
                if (level == 0) return "The reef still looks empty";
                if (level == 1) return e.Count > 1 ? $"{e.Count} species haven't been introduced yet"
                                                   : "One species hasn't been introduced yet";
                return $"Add {who}";
        }
    }

    // Name of the first species in the list the visitor could act on right now, or null if they
    // are all locked — so the caller can fall back to wording that doesn't name one.
    private static string FirstActionable(List<SpeciesDataGPU> list,
                                          EcosystemUnlockManagerGPU unlock,
                                          TabletEcosystemUIGPU ui)
    {
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (s == null) continue;
            if (unlock != null && !unlock.IsUnlocked(s)) continue;
            if (ui.GetSpeciesIndex(s) < 0) continue;
            return string.IsNullOrEmpty(s.SpeciesName) ? null : s.SpeciesName;
        }
        return null;
    }

    private void SetShown(bool shown)
    {
        if (group == null) return;
        group.alpha = shown ? 1f : 0f;
        group.blocksRaycasts = false;   // advisory only; never intercept taps meant for the reef
        group.interactable = false;
    }
}
