using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SpeciesBubble : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Species")]
    public SpeciesData data;

    [Header("Visuals")]
    public Sprite cardImage;
    public GameObject lockOverlay;
    public GameObject glowRing;
    [Tooltip("Name label under the bubble. Auto-found if left empty. Shows '???' when locked.")]
    public TMPro.TMP_Text nameLabel;
    [Tooltip("Text shown in place of the species name while locked.")]
    public string lockedNameText = "???";
    [Tooltip("The fish image. Auto-found (child whose name contains 'IMAGE') if empty. Greyed when locked.")]
    public UnityEngine.UI.Image fishImage;
    [Tooltip("Tint applied to the fish image while locked.")]
    public Color lockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Overpopulation")]
    [Tooltip("Status overlay shown when the simulation reports this species as overpopulated " +
             "(too few predators left to keep it in check). Auto-found (child named " +
             "'Overpopulated') if left empty.")]
    public GameObject overpopulatedOverlay;
    [Tooltip("How often (seconds) to re-check the species' live status from the host.")]
    public float overpopCheckInterval = 0.4f;

    [Header("Food Web")]
    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    private Vector3 baseScale = Vector3.one;
    private Coroutine punchRoutine;
    private bool locked = false;

    private SpeciesBubbleHoldRing _holdRing;

  
    private int _speciesIndex = -2;
    private float _overpopCheckTimer;
    private bool _overpopShown;

   
    void OnEnable()
    {
        if (Application.isPlaying) Refresh();
    }

    void Start()
    {
        baseScale = transform.localScale;
        Refresh();
        _holdRing = GetComponentInChildren<SpeciesBubbleHoldRing>(true);

        if (overpopulatedOverlay != null) overpopulatedOverlay.SetActive(false);
        _overpopShown = false;
    }

    public void Refresh()
    {
        if (data == null) return;

        bool isUnlocked;
        if (EcosystemUnlockManagerGPU.Instance != null)
            isUnlocked = EcosystemUnlockManagerGPU.Instance.IsUnlocked(data);
        else if (GameState.Instance != null && GameState.Instance.unlocked.ContainsKey(data.speciesName))
            isUnlocked = GameState.Instance.unlocked[data.speciesName];
        else
            isUnlocked = data.startUnlocked;

        locked = !isUnlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);

        if (overpopulatedOverlay == null)
        {
            var op = transform.Find("Overpopulated");
            if (op != null) overpopulatedOverlay = op.gameObject;
        }
        if (locked) UpdateOverpopulation();

        if (nameLabel == null)
            nameLabel = GetComponentInChildren<TMPro.TMP_Text>(true);

        if (nameLabel != null)
            nameLabel.text = (locked && Application.isPlaying) ? lockedNameText : data.speciesName;

   
        if (fishImage == null)
        {
            foreach (Transform c in transform)
            {
                if (c.name == "Overpopulated") continue;
                if (lockOverlay != null && c == lockOverlay.transform) continue;
                var im = c.GetComponent<UnityEngine.UI.Image>();
                if (im != null) { fishImage = im; break; }
            }
        }
        if (fishImage != null)
            fishImage.color = (locked && Application.isPlaying) ? lockedTint : Color.white;

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = true;
    }

    void Update()
    {
        _overpopCheckTimer += Time.unscaledDeltaTime;
        if (_overpopCheckTimer >= overpopCheckInterval)
        {
            _overpopCheckTimer = 0f;
            UpdateOverpopulation();
        }

        if (locked) { if (_holdRing != null) _holdRing.CancelHold(); return; }
        if (_holdRing != null) _holdRing.Tick();
    }

    void ResolveSpeciesIndex()
    {
        if (_speciesIndex != -2) return;
        _speciesIndex = (data != null && data.gpuSpecies != null && TabletEcosystemUIGPU.Instance != null)
            ? TabletEcosystemUIGPU.Instance.GetSpeciesIndex(data.gpuSpecies)
            : -1;
    }

  
    void UpdateOverpopulation()
    {
        if (overpopulatedOverlay == null) return;

        bool over = false;
        if (!locked && Application.isPlaying)
        {
            ResolveSpeciesIndex();
            var net = EcosystemNetworkManagerGPU.Instance;
            if (_speciesIndex >= 0 && net != null)
                over = net.IsOverpopulated(_speciesIndex);
        }

        if (over != _overpopShown)
        {
            _overpopShown = over;
            overpopulatedOverlay.SetActive(over);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked) return;
        if (_holdRing != null) _holdRing.BeginHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (locked)
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.Play(UISound.Locked);
            ShowLockedHint();
            return;
        }

        
        bool wasHold = _holdRing != null && _holdRing.EndHold();
        if (wasHold) ContextNudge.DismissId("hold");   // learned the hold gesture
        if (!wasHold)
            OnTap();
    }

    void OnTap()
    {
        ContextNudge.DismissId("tap");   // learned to tap — this unlocks the 'hold' nudge
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayTap();

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());   // tap-punch animation (Aloysius)

        // Resolve this species' netcode index from its sim link so the modal's Add/Remove
        // buttons drive the real simulation (and the population number shows). -1 = cosmetic only.
        int speciesIndex = -1;
        if (data != null && data.gpuSpecies != null && TabletEcosystemUIGPU.Instance != null)
            speciesIndex = TabletEcosystemUIGPU.Instance.GetSpeciesIndex(data.gpuSpecies);

        // Fill the right-side info panel (summary). Its 'View Details' button opens the full modal.
        if (SpeciesInfoPanel.Instance != null)
            SpeciesInfoPanel.Instance.Show(data, cardImage, speciesIndex);
        else if (ModalController.Instance != null)
            ModalController.Instance.Open(data); // fallback: open modal directly (data-driven)
    }

    void PlayPunch()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());
    }

    System.Collections.IEnumerator TapPunch()
    {
        Vector3 original = baseScale;
        Vector3 big = original * 1.2f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.1f;
            transform.localScale = Vector3.Lerp(original, big, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            transform.localScale = Vector3.Lerp(big, original, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localScale = original;
        punchRoutine = null;
    }

    void ShowLockedHint()
    {
        if (data == null) return;

        PlayPunch();
        if (SpeciesInfoPanel.Instance != null)
            SpeciesInfoPanel.Instance.ShowLocked(data);

        int taps;
        if (EcosystemUnlockManagerGPU.Instance != null)
        {
            taps = EcosystemUnlockManagerGPU.Instance.RegisterLockedTap(data);
        }
        else if (GameState.Instance != null)
        {
            taps = GameState.Instance.tapCounts.ContainsKey(data.speciesName)
                ? GameState.Instance.tapCounts[data.speciesName]
                : 0;
            GameState.Instance.tapCounts[data.speciesName] = taps + 1;
        }
        else
        {
            taps = 0;
        }

        // Progressive hints come from alucia_lines.csv ('hint.flavour', scoped to this
        // species) so they're fact-checkable in the sheet with no rebuild — one source of
        // truth with the host and the Hints tab. Variants are ordered vague -> specific ->
        // almost there, matching the old asset hint1/2/3, which is the fallback if the
        // sheet has no rows for this fish.
        var flavour = AluciaLines.GetVariants("hint.flavour", data.speciesName);
        string[] hints = flavour.Count > 0 ? flavour.ToArray()
                                           : new[] { data.hint1, data.hint2, data.hint3 };
        int level = Mathf.Min(taps, hints.Length - 1);

        string[] labels = { "Hint", "Clearer hint", "Almost there" };
        string label = labels[Mathf.Min(taps, labels.Length - 1)];

        if (LockedHintPanel.Instance != null)
            LockedHintPanel.Instance.Show(label, hints[level]);
    }
}
