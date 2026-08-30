using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HintsPanel : MonoBehaviour
{
    [Header("UI")]
        public TMP_Text hintText;
        public TMP_Text headerText;
        public TMP_Text targetNameText;

    [Header("Species (drag the same SpeciesData list used elsewhere)")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();

    [Header("Copy")]
    public string header = "NEXT TO DISCOVER";
        public string allUnlockedMessage = "You've discovered every species. The reef is complete!";
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

    string BuildHint(SpeciesData sp, bool healthMet, int minHealth,
                     List<EcosystemUnlockManagerGPU.RequirementStatus> reqs)
    {

        if (!healthMet && minHealth > 0)
            return "Raise the ecosystem's health to " + minHealth + "% to unlock the " + sp.speciesName + ".";

        // The prey-count variants (hint.needs / hint.close / hint.one) were removed: they
        // interpolate the species and prey names at runtime, so they can never be voiced, and
        // they read in a different register from the rest of Alucia's writing. The flavour lines
        // cover the same ground ("Try adding more X to attract Y") and are the set with recordings.
        //
        // The which-requirement-is-closest scan that used to sit here went with them: it existed
        // only to choose between those three wordings. The health gate above still short-circuits,
        // and 'reqs' is kept in the signature for callers.
        var fl = AluciaLines.GetVariants("hint.flavour", sp.speciesName);
        if (fl.Count > 0) return fl[0];
        if (!string.IsNullOrEmpty(sp.hint1)) return sp.hint1;
        return "Something new is almost ready to appear...";
    }
}
