using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeciesUnlockReveal : MonoBehaviour
{
    [Header("Reveal card refs")]
    public CanvasGroup revealGroup;
    public Image revealImage;
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

        RevealQueue.Get().Enqueue(revealGroup, () => FillCard(species), revealHoldSeconds, fadeDuration, key: species.speciesName);

    }

    void FillCard(SpeciesData species)
    {

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

        int rot = 0;
        if (_hintRotation.TryGetValue(sp, out int v)) rot = v;
        _hintRotation[sp] = rot + 1;

        string name = sp.speciesName;
        string rp = reqPhrase ?? "";

        string[] withReq = new string[]
        {
            AluciaLines.Get("hint.withReq.1", "To bring in the {species}, you'll need to {req}.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.2", "Almost there! Just {req} and the {species} will appear.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.3", "The {species} is waiting \u2014 {req}.").Replace("{species}", name).Replace("{req}", rp),
            AluciaLines.Get("hint.withReq.4", "Keep going! {req} to attract the {species}.").Replace("{species}", name).Replace("{req}", rp),
        };

        var flavour = AluciaLines.GetVariants("hint.flavour", name);
        if (flavour.Count == 0)
        {
            if (!string.IsNullOrEmpty(sp.hint1)) flavour.Add(sp.hint1);
            if (!string.IsNullOrEmpty(sp.hint2)) flavour.Add(sp.hint2);
            if (!string.IsNullOrEmpty(sp.hint3)) flavour.Add(sp.hint3);
        }

        if (reqPhrase == null)
            return flavour.Count > 0 ? flavour[rot % flavour.Count] : AluciaLines.Get("hint.fallback", "Something new is almost ready to appear...");

        if (flavour.Count > 0 && rot % 2 == 0)
            return flavour[(rot / 2) % flavour.Count];
        return withReq[rot % withReq.Length];
    }
}
