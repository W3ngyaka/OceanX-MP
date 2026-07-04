using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shows a "New Species Discovered" reveal card on the host screen when a species
// unlocks for the first time, then asks Alucia to hint the closest-to-unlockable
// locked species. Subscribes to EcosystemUnlockManagerGPU.OnSpeciesUnlocked.
//
// SETUP (assign in Inspector):
//   revealGroup   -> CanvasGroup on the reveal card container
//   revealImage   -> Image for the fish picture (leave empty for now; text-only)
//   nameText      -> "New Species Discovered" species name
//   sciText       -> scientific name
//   tierText      -> trophic tier
//   msgText       -> addedMessage
//   alucia        -> the AluciaController (for the follow-up hint)
//   allSpecies    -> all 12 SpeciesData assets (drag them in, or auto-loaded if empty)
public class SpeciesUnlockReveal : MonoBehaviour
{
    [Header("Reveal card refs")]
    public CanvasGroup revealGroup;
    public Image revealImage;       // optional; leave null for text-only
    public Text nameText;
    public Text sciText;
    public Text tierText;
    public Text msgText;

    [Header("Links")]
    public AluciaController alucia;

    private readonly System.Collections.Generic.Dictionary<SpeciesData,int> _hintRotation = new System.Collections.Generic.Dictionary<SpeciesData,int>();
    public List<SpeciesData> allSpecies = new List<SpeciesData>();

    [Header("Timing")]
    public float revealHoldSeconds = 5.5f;
    public float fadeDuration = 0.4f;
    public float hintDelayAfterReveal = 0.4f;

    private EcosystemUnlockManagerGPU _mgr;

    void Awake()
    {
        if (revealGroup != null) revealGroup.alpha = 0f;
    }

    void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    void OnDisable()
    {
        if (_mgr != null) _mgr.OnSpeciesUnlocked -= HandleUnlock;
    }

    IEnumerator SubscribeWhenReady()
    {
        float t = 0f;
        while (EcosystemUnlockManagerGPU.Instance == null && t < 15f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        _mgr = EcosystemUnlockManagerGPU.Instance;
        if (_mgr != null)
        {
            _mgr.OnSpeciesUnlocked += HandleUnlock;
            Debug.Log("[Reveal] Subscribed to unlock manager OK.", this);
        }
        else
        {
            Debug.LogWarning("[Reveal] Unlock manager Instance never appeared - not subscribed.", this);
        }
    }

    void HandleUnlock(SpeciesData species)
    {
        Debug.Log("[Reveal] HandleUnlock fired for: " + (species != null ? species.speciesName : "NULL"), this);
        if (species == null) return;
        StopAllCoroutines();
        StartCoroutine(RevealThenHint(species));
    }

    IEnumerator RevealThenHint(SpeciesData species)
    {
        // --- Fill + show reveal card ---
        if (nameText != null) nameText.text = species.speciesName;
        if (sciText != null) sciText.text = species.sciName;
        if (tierText != null) tierText.text = species.tier;
        if (msgText != null) msgText.text = species.addedMessage;
        if (revealImage != null) revealImage.enabled = (revealImage.sprite != null);

        yield return Fade(revealGroup, 1f, fadeDuration);
        yield return new WaitForSeconds(revealHoldSeconds);
        yield return Fade(revealGroup, 0f, fadeDuration);

        // Hint moved to SpeciesAddedReveal (fires on fish ADDED, not on unlock).
    }

    public void HintNextLocked()
    {
        if (alucia == null || _mgr == null) return;

        SpeciesData best = null;
        int fewestUnmet = int.MaxValue;
        string bestHint = null;

        foreach (var sp in allSpecies)
        {
            if (sp == null || _mgr.IsUnlocked(sp)) continue;

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

        if (best != null && !string.IsNullOrEmpty(bestHint))
            alucia.Say(bestHint, AluciaController.Mood.Calm);
    }

    string BuildHint(SpeciesData sp, bool healthMet, int minHealth,
                     List<EcosystemUnlockManagerGPU.RequirementStatus> reqs)
    {
        // Build a progress-aware requirement phrase.
        var parts = new List<string>();
        if (!healthMet && minHealth > 0)
            parts.Add("get eco-health to " + minHealth + "%");
        if (reqs != null)
            foreach (var r in reqs)
                if (!r.Met && r.Species != null)
                {
                    int need = Mathf.Max(0, r.Required - r.Current);
                    string nm = r.Species.speciesName;
                    parts.Add(need == 1 ? ("one more " + nm) : (need + " more " + nm));
                }
        string reqPhrase = parts.Count > 0 ? string.Join(", and ", parts) : null;

        // Rotate through several phrasings so repeats don't feel identical.
        int rot = 0;
        if (_hintRotation.TryGetValue(sp, out int v)) rot = v;
        _hintRotation[sp] = rot + 1;

        string name = sp.speciesName;
        // Pool of variant templates. {0}=species, {1}=requirement phrase.
        string[] withReq = new string[]
        {
            "To bring in the " + name + ", you'll need to " + reqPhrase + ".",
            "Almost there! Just " + reqPhrase + " and the " + name + " will appear.",
            "The " + name + " is waiting \u2014 " + reqPhrase + ".",
            "Keep going! " + reqPhrase + " to attract the " + name + ".",
        };
        // Variants that lean on the species' own flavour hints when available.
        var flavour = new List<string>();
        if (!string.IsNullOrEmpty(sp.hint1)) flavour.Add(sp.hint1);
        if (!string.IsNullOrEmpty(sp.hint2)) flavour.Add(sp.hint2);
        if (!string.IsNullOrEmpty(sp.hint3)) flavour.Add(sp.hint3);

        // Interleave: even rotations use flavour (if any), odd use progress phrasing.
        if (reqPhrase == null)
            return flavour.Count > 0 ? flavour[rot % flavour.Count] : "Something new is almost ready to appear...";

        if (flavour.Count > 0 && rot % 2 == 0)
            return flavour[(rot / 2) % flavour.Count];
        return withReq[rot % withReq.Length];
    }



    IEnumerator Fade(CanvasGroup cg, float target, float dur)
    {
        if (cg == null) yield break;
        float start = cg.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            cg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }
        cg.alpha = target;
    }
}
