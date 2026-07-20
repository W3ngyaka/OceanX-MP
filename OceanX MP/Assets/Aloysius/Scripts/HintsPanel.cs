using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tablet Hints tab: shows a single, always-current hint for the NEXT
// closest-to-unlockable species. Mirrors what Alucia says on the host, but
// stays on screen so visitors can re-read a hint they missed. Computed locally
// from EcosystemUnlockManagerGPU so it works regardless of host state.
public class HintsPanel : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Main hint line.")]
    public TMP_Text hintText;
    [Tooltip("Optional small header, e.g. 'NEXT TO DISCOVER'. Leave null to skip.")]
    public TMP_Text headerText;
    [Tooltip("Optional species name shown above/with the hint.")]
    public TMP_Text targetNameText;

    [Header("Species (drag the same SpeciesData list used elsewhere)")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();

    [Header("Copy")]
    public string header = "NEXT TO DISCOVER";
    [Tooltip("Shown when everything is already unlocked.")]
    public string allUnlockedMessage = "You've discovered every species. The reef is complete!";
    [Tooltip("Shown when no manager / no data yet.")]
    public string noDataMessage = "Add species to the reef to reveal what comes next.";

    private EcosystemUnlockManagerGPU _mgr;

    void OnEnable()
    {
        _mgr = EcosystemUnlockManagerGPU.Instance;
        if (_mgr != null) _mgr.OnUnlockStateChanged += Refresh;
        if (headerText != null) headerText.text = header;
        Refresh();
    }

    void OnDisable()
    {
        if (_mgr != null) _mgr.OnUnlockStateChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_mgr == null) _mgr = EcosystemUnlockManagerGPU.Instance;

        if (_mgr == null)
        {
            Set(null, noDataMessage);
            return;
        }

        SpeciesData best = null;
        int fewestUnmet = int.MaxValue;
        string bestHint = null;
        bool anyLocked = false;

        foreach (var sp in allSpecies)
        {
            if (sp == null || _mgr.IsUnlocked(sp)) continue;
            anyLocked = true;

            bool healthMet; int minHealth, curHealth;
            List<EcosystemUnlockManagerGPU.RequirementStatus> reqs;
            _mgr.GetLockInfo(sp, out healthMet, out minHealth, out curHealth, out reqs);

            int unmet = healthMet ? 0 : 1;
            if (reqs != null)
                foreach (var r in reqs) if (!r.Met) unmet++;

            if (unmet < fewestUnmet)
            {
                fewestUnmet = unmet;
                best = sp;
                bestHint = BuildHint(sp, healthMet, minHealth, reqs);
            }
        }

        if (!anyLocked) { Set(null, allUnlockedMessage); return; }
        Set(best, bestHint);
    }

    void Set(SpeciesData target, string hint)
    {
        if (targetNameText != null)
            targetNameText.text = target != null ? target.speciesName : "";
        if (hintText != null)
            hintText.text = hint;
    }

    // The Hints tab actively tells the player how to unlock the NEXT species, computed
    // LIVE from the current populations so it's always accurate (unlike a static sheet
    // line). Only when nothing concrete is still outstanding — a transient pre-unlock
    // state — do we fall back to a flavour line: alucia_lines.csv 'hint.flavour' first
    // (editable in the sheet, no rebuild), then the SpeciesData asset's hint1.
    string BuildHint(SpeciesData sp, bool healthMet, int minHealth,
                     List<EcosystemUnlockManagerGPU.RequirementStatus> reqs)
    {
        var parts = new List<string>();
        if (!healthMet && minHealth > 0)
            parts.Add("get eco-health to " + minHealth + "%");
        if (reqs != null)
            foreach (var r in reqs)
                if (!r.Met && r.Species != null)
                {
                    int need = Mathf.Max(0, r.Required - r.Current);
                    parts.Add("add " + need + " more " + r.Species.speciesName);
                }

        if (parts.Count > 0)
            return "To unlock " + sp.speciesName + ", " + string.Join(", and ", parts) + ".";

        var flavour = AluciaLines.GetVariants("hint.flavour", sp.speciesName);
        if (flavour.Count > 0)
            return flavour[0];
        if (!string.IsNullOrEmpty(sp.hint1))
            return sp.hint1;
        return "Something new is almost ready to appear...";
    }
}
