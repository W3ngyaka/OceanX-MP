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
        int fewestBlocked = int.MaxValue;
        int fewestUnmet = int.MaxValue;
        string bestHint = null;
        string bestAudio = null;

        foreach (var sp in allSpecies)
        {
            if (sp == null || _mgr.IsUnlocked(sp)) continue;

            bool healthMet; int minHealth, curHealth;
            List<EcosystemUnlockManagerGPU.RequirementStatus> reqs;
            _mgr.GetLockInfo(sp, out healthMet, out minHealth, out curHealth, out reqs);

            int unmet = healthMet ? 0 : 1;
            int blocked = 0;
            if (reqs != null)
                foreach (var r in reqs)
                    if (!r.Met)
                    {
                        unmet++;
                        // Requirement the player cannot act on yet — the species it asks for is
                        // itself still locked, so this is not one step away, it is two or more.
                        // See HintsPanel for the worked example (moray vs ray).
                        if (r.Species != null && !_mgr.IsUnlocked(r.Species)) blocked++;
                    }

            // Reachable species first, then fewest steps.
            if (blocked < fewestBlocked || (blocked == fewestBlocked && unmet < fewestUnmet))
            {
                fewestBlocked = blocked;
                fewestUnmet = unmet;
                best = sp;
                bestHint = BuildHint(sp, out bestAudio);
            }
        }

        // Every flavour line has a recording, so bestAudio is normally set; it is null only for
        // the ScriptableObject hint1-3 fallbacks, where Say falls back to timed hiding.
        if (best != null && !string.IsNullOrEmpty(bestHint))
            alucia.Say(bestHint, AluciaController.Mood.Calm, false, bestAudio);
    }

    // Only the hint.flavour lines are used here now.
    //
    // The requirement-phrase variants were dropped from this path: they interpolate the species
    // and requirement names at runtime, so no fixed recording can voice them, and the old
    // rotation alternated INTO them every odd turn — meaning Alucia fell silent on every other
    // hint once VO existed. Most flavour lines already carry the same guidance
    // ("Try adding more X to attract Y"), so the player still learns what to do, and hears it.
    string BuildHint(SpeciesData sp, out string audioName)
    {
        audioName = null;

        int rot = 0;
        if (_hintRotation.TryGetValue(sp, out int v)) rot = v;
        _hintRotation[sp] = rot + 1;

        // GetVariantLines, not GetVariants: this method picks the variant itself by rotation
        // index, so it needs each line's Audio name to survive the lookup.
        var flavour = AluciaLines.GetVariantLines("hint.flavour", sp.speciesName);
        if (flavour.Count == 0)
        {
            // ScriptableObject fallbacks have no recordings — Audio stays null.
            if (!string.IsNullOrEmpty(sp.hint1)) flavour.Add(new AluciaLines.Line { Text = sp.hint1 });
            if (!string.IsNullOrEmpty(sp.hint2)) flavour.Add(new AluciaLines.Line { Text = sp.hint2 });
            if (!string.IsNullOrEmpty(sp.hint3)) flavour.Add(new AluciaLines.Line { Text = sp.hint3 });
        }
        if (flavour.Count == 0)
            return AluciaLines.Get("hint.fallback", "Something new is almost ready to appear...");

        AluciaLines.Line pick = flavour[rot % flavour.Count];
        audioName = pick.Audio;
        return pick.Text;
    }
}
