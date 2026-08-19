using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

public class SpeciesAddedReveal : MonoBehaviour
{
    [Header("Card refs")]
    public CanvasGroup revealGroup;
    public Image revealImage;

    public TMP_Text nameText;
    public TMP_Text tierText;
    public TMP_Text msgText;

    [Header("Species data (index resolved at runtime)")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();
        public List<Sprite> cardImages = new List<Sprite>();

    [Header("Timing")]
    public float holdSeconds = 4f;
    public float fadeDuration = 0.4f;

    [Header("Hint after add")]
        public SpeciesUnlockReveal hintSource;
        public float hintDelayAfterAdd = 0.4f;

    [Header("Behaviour")]
        public bool onlyFirstTime = true;

    [Header("Card image")]
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

        if (_sim != null)
        {
            _sim.OnSpeciesFirstIntroduced -= HandleFirstIntroduced;
            _sim.OnSpeciesFirstIntroduced += HandleFirstIntroduced;
        }

    }

    void BuildIndexMap()
    {
        _indexToData.Clear();
        _dataToSprite.Clear();
        _gpuToData.Clear();

        for (int i = 0; i < allSpecies.Count; i++)
        {
            var sd = allSpecies[i];
            if (sd == null) continue;
            if (i < cardImages.Count && cardImages[i] != null) _dataToSprite[sd] = cardImages[i];
        }

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

    void HandleFirstIntroduced(SpeciesDataGPU gpu)
    {
        if (gpu == null) return;
        if (_gpuToData.TryGetValue(gpu, out var sd) && sd != null)
            SubmitReveal(sd);
    }

    void SubmitReveal(SpeciesData species)
    {
        if (species == null) return;
        RevealQueue.Get().Enqueue(
            revealGroup,
            () =>
            {
                FillCard(species);

                if (AdaptiveMusicSystem.Instance != null)
                    AdaptiveMusicSystem.Instance.PlayIntro(species.introSound, species.introVolume);
            },
            holdSeconds,
            fadeDuration,
            () => OnCardShown(),
            key: species.speciesName);
    }

    void FillCard(SpeciesData species)
    {
        if (species == null) return;

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

    public void ResetShownHistory()
    {
        if (revealGroup != null) revealGroup.alpha = 0f;
    }
}
