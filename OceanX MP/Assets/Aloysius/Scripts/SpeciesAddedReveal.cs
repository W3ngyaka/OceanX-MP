using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public Text nameText;
    public Text tierText;
    public Text msgText;

    [Header("Species data (index resolved at runtime)")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();
    [Tooltip("Optional card image per species, index-aligned with allSpecies. Leave a slot empty for text-only.")]
    public List<Sprite> cardImages = new List<Sprite>();

    [Header("Timing")]
    public float holdSeconds = 4f;
    public float fadeDuration = 0.4f;
    public float pollInterval = 0.25f;

    [Header("Behaviour")]
    [Tooltip("Show only the FIRST time each species appears. If false, shows every 0->1+ transition.")]
    public bool onlyFirstTime = true;

    private EcosystemNetworkManagerGPU _net;
    private EcosystemSimulationGPU _sim;
    private readonly Dictionary<int, SpeciesData> _indexToData = new Dictionary<int, SpeciesData>();
    private readonly Dictionary<SpeciesData, Sprite> _dataToSprite = new Dictionary<SpeciesData, Sprite>();
    private readonly Dictionary<int, int> _lastPop = new Dictionary<int, int>();
    private readonly HashSet<int> _seen = new HashSet<int>();
    private readonly Queue<SpeciesData> _pending = new Queue<SpeciesData>();
    private bool _playing;
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
                Object.FindObjectOfType<EcosystemSimulationGPU>() == null) && t < 15f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        _net = EcosystemNetworkManagerGPU.Instance;
        _sim = Object.FindObjectOfType<EcosystemSimulationGPU>();
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
                _pending.Enqueue(kv.Value);
            }
        }

        if (!_playing && _pending.Count > 0)
            StartCoroutine(PlayQueue());
    }

    IEnumerator PlayQueue()
    {
        _playing = true;
        while (_pending.Count > 0)
        {
            var species = _pending.Dequeue();
            yield return Show(species);
        }
        _playing = false;
    }

    IEnumerator Show(SpeciesData species)
    {
        if (species == null) yield break;

        if (nameText != null) nameText.text = species.speciesName;
        if (tierText != null) tierText.text = species.tier;
        if (msgText != null) msgText.text = species.addedMessage;
        if (revealImage != null)
        {
            Sprite img = _dataToSprite.TryGetValue(species, out var s) ? s : null;
            revealImage.sprite = img;
            revealImage.enabled = (img != null);
        }

        yield return Fade(1f);
        yield return new WaitForSeconds(holdSeconds);
        yield return Fade(0f);
    }

    IEnumerator Fade(float target)
    {
        if (revealGroup == null) yield break;
        float start = revealGroup.alpha;
        float e = 0f;
        while (e < fadeDuration)
        {
            e += Time.unscaledDeltaTime;
            revealGroup.alpha = Mathf.Lerp(start, target, e / fadeDuration);
            yield return null;
        }
        revealGroup.alpha = target;
    }
}
