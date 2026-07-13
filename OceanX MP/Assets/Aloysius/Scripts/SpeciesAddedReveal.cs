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
    public float pollInterval = 0.25f;

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
    private readonly Dictionary<SpeciesData, Sprite> _dataToSprite = new Dictionary<SpeciesData, Sprite>();
    private readonly Dictionary<int, int> _lastPop = new Dictionary<int, int>();
    private readonly HashSet<int> _seen = new HashSet<int>();
    private float _pollTimer;

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

        // Seed baseline so species already present at startup don't all pop cards.
        foreach (var kv in _indexToData)
        {
            int pop = SafePop(kv.Key);
            _lastPop[kv.Key] = pop;
            if (pop > 0) _seen.Add(kv.Key); // treat pre-existing as already seen
        }
    }

    void BuildIndexMap()
    {
        _indexToData.Clear();
        _dataToSprite.Clear();

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
                if (sd != null && sd.gpuSpecies == gpu) { _indexToData[idx] = sd; break; }
            }
        }
    }

    int SafePop(int index)
    {
        if (_net == null) return 0;
        return _net.GetPopulation(index);
    }

    void Update()
    {
        if (_net == null || _indexToData.Count == 0) return;

        _pollTimer -= Time.unscaledDeltaTime;
        if (_pollTimer > 0f) return;
        _pollTimer = pollInterval;

        foreach (var kv in _indexToData)
        {
            int idx = kv.Key;
            int pop = SafePop(idx);
            int prev = _lastPop.TryGetValue(idx, out var p) ? p : 0;
            _lastPop[idx] = pop;

            // rising edge 0 -> >0
            if (prev <= 0 && pop > 0)
            {
                if (onlyFirstTime && _seen.Contains(idx)) continue;
                _seen.Add(idx);
                SubmitReveal(kv.Value);
            }
        }
    }

    // Hand the card to the shared queue so it can never overlap an unlock reveal.
    // Content is filled by FillCard the instant before the card fades in.
    void SubmitReveal(SpeciesData species)
    {
        if (species == null) return;
        RevealQueue.Get().Enqueue(
            revealGroup,
            () => FillCard(species),
            holdSeconds,
            fadeDuration,
            () => OnCardShown());
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
        string blurb = (e != null && !string.IsNullOrWhiteSpace(e.blurb))       ? e.blurb       : species.addedMessage;

        if (nameText != null) nameText.text = name;
        if (tierText != null) tierText.text = role;
        if (msgText  != null) msgText.text  = blurb;

        if (revealImage != null)
        {
            // When useCsvImage is on, the big-screen photo comes from RevealContent.csv's 'imageFile',
            // loaded from StreamingAssets/RevealImages -- the big screen's OWN images, SEPARATE from the
            // tablet's SpeciesImages. Falls back to the inspector cardImages list if the CSV has none.
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
}
