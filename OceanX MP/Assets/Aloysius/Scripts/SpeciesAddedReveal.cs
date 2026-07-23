using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

// Shows a center-stage info card on the HOST/large screen the FIRST time each
// species is added to the ecosystem (population goes 0 -> 1+). Fades in, holds,
// fades out. Separate from SpeciesUnlockReveal (which fires on UNLOCK, not add).
//
// No population-change event exists on EcosystemNetworkManagerGPU, so this polls
// GetPopulation(index) for each known species and detects the 0 -> >0 edge.
//
// SETUP (Inspector):
//   revealGroup -> CanvasGroup on the card container (center stage)
//   revealImage -> Image for the fish picture (uses SpeciesData.cardImage)
//   nameText / tierText / msgText -> the card texts
//   allSpecies  -> the SpeciesData assets (same list you drag into SpeciesUnlockReveal)
public class SpeciesAddedReveal : MonoBehaviour
{
    [Header("Card refs")]
    public CanvasGroup revealGroup;
    public Image revealImage;      // optional
    // The card's texts are TextMeshPro (TMP_Text).
    public TMP_Text nameText;
    public TMP_Text tierText;
    public TMP_Text msgText;

    [Header("Species data (index resolved at runtime)")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();
    [Tooltip("Optional card image per species, index-aligned with allSpecies. Leave a slot empty for text-only.")]
    public List<Sprite> cardImages = new List<Sprite>();

    [Header("Timing")]
    public float holdSeconds = 4f;
    public float fadeDuration = 0.4f;

    [Header("Hint after add")]
    [Tooltip("Source of the 'next fish' hint logic (the SpeciesUnlockReveal on AluciaCanvas). Auto-found if null.")]
    public SpeciesUnlockReveal hintSource;
    [Tooltip("Delay after the added card fades before Alucia hints the next species.")]
    public float hintDelayAfterAdd = 0.4f;

    [Header("Behaviour")]
    [Tooltip("Show only the FIRST time each species appears. If false, shows every 0->1+ transition.")]
    public bool onlyFirstTime = true;

    [Header("Card image")]
    [Tooltip("OFF = text-only card (no photo — the current design). ON = show the fish photo from " +
             "SpeciesContent.csv (the 'imageFile' column). Turn this ON only after the RevealImage slot " +
             "is laid out as a proper sized image area with Preserve Aspect, so the photo doesn't overlap " +
             "the text.")]
    public bool useCsvImage = false;

    private EcosystemNetworkManagerGPU _net;
    private EcosystemSimulationGPU _sim;
    private readonly Dictionary<int, SpeciesData> _indexToData = new Dictionary<int, SpeciesData>();
    private readonly Dictionary<SpeciesDataGPU, SpeciesData> _gpuToData = new Dictionary<SpeciesDataGPU, SpeciesData>();
    private readonly Dictionary<SpeciesData, Sprite> _dataToSprite = new Dictionary<SpeciesData, Sprite>();

    void Awake()
    {
        if (revealGroup != null) revealGroup.alpha = 0f;
    }

    void OnEnable() { StartCoroutine(SetupWhenReady()); }

    IEnumerator SetupWhenReady()
    {
        float t = 0f;
        while ((EcosystemNetworkManagerGPU.Instance == null ||
                Object.FindFirstObjectByType<EcosystemSimulationGPU>() == null) && t < 15f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        _net = EcosystemNetworkManagerGPU.Instance;
        _sim = Object.FindFirstObjectByType<EcosystemSimulationGPU>();
        if (hintSource == null) hintSource = Object.FindFirstObjectByType<SpeciesUnlockReveal>();
        BuildIndexMap();

        // Drive the card off the SAME event the intro camera uses, so the card and the
        // zoom can never desync during rapid adds (both fire on first introduction only).
        if (_sim != null)
        {
            _sim.OnSpeciesFirstIntroduced -= HandleFirstIntroduced; // avoid double
            _sim.OnSpeciesFirstIntroduced += HandleFirstIntroduced;
        }

    }

    void BuildIndexMap()
    {
        _indexToData.Clear();
        _dataToSprite.Clear();
        _gpuToData.Clear();

        // Sprite lookup (index-aligned with allSpecies).
        for (int i = 0; i < allSpecies.Count; i++)
        {
            var sd = allSpecies[i];
            if (sd == null) continue;
            if (i < cardImages.Count && cardImages[i] != null) _dataToSprite[sd] = cardImages[i];
        }

        // Map sim index -> SpeciesData by matching gpuSpecies against the
        // ecosystem's ordered Species list (index == position in that list).
        if (_sim == null || _sim.Ecosystem == null || _sim.Ecosystem.Species == null) return;
        var order = _sim.Ecosystem.Species;
        for (int idx = 0; idx < order.Count; idx++)
        {
            var gpu = order[idx];
            if (gpu == null) continue;
            foreach (var sd in allSpecies)
            {
                if (sd != null && sd.gpuSpecies == gpu) { _indexToData[idx] = sd; _gpuToData[gpu] = sd; break; }
            }
        }
    }

    void OnDisable()
    {
        if (_sim != null) _sim.OnSpeciesFirstIntroduced -= HandleFirstIntroduced;
    }

    // Fired by the sim the instant a species is first introduced — the very same signal
    // the intro camera zooms on. Map the GPU species back to our UI SpeciesData and show
    // its card. No polling, so no ordering drift under spam.
    void HandleFirstIntroduced(SpeciesDataGPU gpu)
    {
        if (gpu == null) return;
        if (_gpuToData.TryGetValue(gpu, out var sd) && sd != null)
            SubmitReveal(sd);
    }

    // Hand the card to the shared queue so it can never overlap an unlock reveal.
    // Content is filled by FillCard the instant before the card fades in.
    void SubmitReveal(SpeciesData species)
    {
        if (species == null) return;
        RevealQueue.Get().Enqueue(
            revealGroup,
            () =>
            {
                FillCard(species);
                // Positive beat — lift the music the moment the card appears.
                if (AdaptiveMusicSystem.Instance != null) AdaptiveMusicSystem.Instance.PlaySwell();
            },
            holdSeconds,
            fadeDuration,
            () => OnCardShown(),
            key: species.speciesName);
    }

    void FillCard(SpeciesData species)
    {
        if (species == null) return;

        // The big screen shows the SHORT "new arrival" blurb from RevealContent.csv — its OWN sheet,
        // separate from the tablet's SpeciesContent.csv (which holds the long, detailed description).
        // Matched by the species' stable contentId, falling back to its display name (RevealContentDB is
        // indexed by both). Every field falls back to the SpeciesData asset if the CSV row/value is
        // missing, so the card never goes blank offline.
        string key = !string.IsNullOrEmpty(species.contentId) ? species.contentId : species.speciesName;
        RevealContentDB.Entry e = RevealContentDB.Get(key);

        string name  = (e != null && !string.IsNullOrWhiteSpace(e.speciesName)) ? e.speciesName : species.speciesName;
        string role  = (e != null && !string.IsNullOrWhiteSpace(e.role))        ? e.role        : species.tier;
        string blurb = (e != null && !string.IsNullOrWhiteSpace(e.firstAddedMessage)) ? e.firstAddedMessage : species.addedMessage;

        if (nameText != null) nameText.text = name;
        if (tierText != null) tierText.text = role;
        if (msgText  != null) msgText.text  = blurb;

        if (revealImage != null)
        {
            // When useCsvImage is on, the big-screen photo comes from RevealContent.csv's 'imageFile',
            // loaded from StreamingAssets/Trifold -- the big screen's OWN images, SEPARATE from the
            // tablet's Tablet folder. Falls back to the inspector cardImages list if the CSV has none.
            Sprite img = null;
            if (useCsvImage)
            {
                img = (e != null) ? RevealContentDB.GetImage(e.imageFile) : null;
                if (img == null) _dataToSprite.TryGetValue(species, out img);
            }
            revealImage.sprite = img;
            revealImage.enabled = (img != null);
        }
    }

    // After the added card fades out, hint the next closest-to-unlockable species.
    void OnCardShown()
    {
        if (hintSource != null && isActiveAndEnabled)
            StartCoroutine(HintAfterDelay());
    }

    IEnumerator HintAfterDelay()
    {
        if (hintDelayAfterAdd > 0f) yield return new WaitForSeconds(hintDelayAfterAdd);
        if (hintSource != null) hintSource.HintNextLocked();
    }

    // Fresh-start reset for a new visitor. The "first added" gating is NOT stored in this
    // component: the sim fires OnSpeciesFirstIntroduced on a species' 0 -> 1 edge, so the
    // coordinator emptying the ecosystem (EcosystemSimulationGPU.ResetToEmpty) re-arms
    // every card automatically. Here we clear this component's own pending reveal state:
    // hide its card slot back to idle.
    public void ResetShownHistory()
    {
        if (revealGroup != null) revealGroup.alpha = 0f;
    }
}
