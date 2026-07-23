using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TMP_Text nameText;
    public TMP_Text sciText;
    public TMP_Text tierText;
    public TMP_Text msgText;

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

        // Route through the shared queue so an unlock card waits its turn instead of
        // slamming on top of an added card (they share the same center-stage slot).
        RevealQueue.Get().Enqueue(revealGroup, () => FillCard(species), revealHoldSeconds, fadeDuration, key: species.speciesName);
        // Hint moved to SpeciesAddedReveal (fires on fish ADDED, not on unlock).
    }

    void FillCard(SpeciesData species)
    {
        // Read the unlock card from RevealContent.csv (the SAME sheet as the arrival card), matched by the
        // species' stable contentId (falling back to its display name). Uses the 'unlockMessage' column so the
        // unlock line can differ from the arrival 'blurb'. Every field falls back to the SpeciesData asset if
        // the CSV row/value is missing, so the card never goes blank offline.
        string key = !string.IsNullOrEmpty(species.contentId) ? species.contentId : species.speciesName;
        RevealContentDB.Entry e = RevealContentDB.Get(key);

        string name = (e != null && !string.IsNullOrWhiteSpace(e.speciesName))   ? e.speciesName   : species.speciesName;
        string sci  = (e != null && !string.IsNullOrWhiteSpace(e.sciName))       ? e.sciName       : species.sciName;
        string role = (e != null && !string.IsNullOrWhiteSpace(e.role))          ? e.role          : species.tier;
        string msg  = (e != null && !string.IsNullOrWhiteSpace(e.unlockMessage)) ? e.unlockMessage : species.addedMessage;

        if (nameText != null) nameText.text = name;
        if (sciText  != null) sciText.text  = sci;
        if (tierText != null) tierText.text = role;
        if (msgText  != null) msgText.text  = msg;

        if (revealImage != null)
        {
            // The unlock card has its OWN photo column (unlockImageFile), loaded from the SAME folder as the
            // arrival card (StreamingAssets/Trifold), just a different filename. If unlockImageFile is blank
            // it falls back to the arrival photo (imageFile), so the card is never image-less while the friend
            // hasn't dropped the unlock-specific pics yet. Then to any sprite pre-assigned in the scene.
            string imgFile = (e != null && !string.IsNullOrWhiteSpace(e.unlockImageFile)) ? e.unlockImageFile
                            : (e != null ? e.imageFile : null);
            Sprite img = RevealContentDB.GetImage(imgFile);
            if (img != null) revealImage.sprite = img;
            revealImage.enabled = (revealImage.sprite != null);
        }
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
        string rp = reqPhrase ?? "";
        // Templates are checker-editable in StreamingAssets/alucia_lines.csv; the
        // string here is the fallback if the CSV/key is missing. {species}=name, {req}=requirements.
        string[] withReq = new string[]
        {
            AluciaLines.Get("hint.withReq.1", "To bring in the {species}, you'll need to {req}.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.2", "Almost there! Just {req} and the {species} will appear.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.3", "The {species} is waiting \u2014 {req}.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.4", "Keep going! {req} to attract the {species}.").Replace("{species}", name).Replace("{req}", rp),
        };
        // Variants that lean on the species' own flavour hints. These come from the CSV first (event
        // 'hint.flavour', scoped to this species) so fact-checkers can edit them in the sheet; if the CSV
        // has no rows for this fish, fall back to the SpeciesData asset's hint1/2/3 so nothing breaks.
        var flavour = AluciaLines.GetVariants("hint.flavour", name);
        if (flavour.Count == 0)
        {
            if (!string.IsNullOrEmpty(sp.hint1)) flavour.Add(sp.hint1);
            if (!string.IsNullOrEmpty(sp.hint2)) flavour.Add(sp.hint2);
            if (!string.IsNullOrEmpty(sp.hint3)) flavour.Add(sp.hint3);
        }

        // Interleave: even rotations use flavour (if any), odd use progress phrasing.
        if (reqPhrase == null)
            return flavour.Count > 0 ? flavour[rot % flavour.Count] : AluciaLines.Get("hint.fallback", "Something new is almost ready to appear...");

        if (flavour.Count > 0 && rot % 2 == 0)
            return flavour[(rot / 2) % flavour.Count];
        return withReq[rot % withReq.Length];
    }
}
