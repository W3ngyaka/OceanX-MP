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

    [Header("Food Web")]
    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    private float holdDuration = 0.5f;
    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool longPressTriggered = false;
    private bool locked = false;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (data == null) return;

        bool isUnlocked = GameState.Instance != null &&
                          GameState.Instance.unlocked.ContainsKey(data.speciesName)
            ? GameState.Instance.unlocked[data.speciesName]
            : data.startUnlocked;

        locked = !isUnlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = true; // keep receiving clicks
        Debug.Log($"{data.speciesName} Locked: {locked}");
    }

    void Update()
    {
        if (locked) return;
        if (!isHolding) return;

        holdTimer += Time.unscaledDeltaTime;

        if (holdTimer >= holdDuration && !longPressTriggered)
        {
            longPressTriggered = true;

            if (FoodWebLines.Instance != null)
                FoodWebLines.Instance.ShowConnections(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked) return;

        isHolding = true;
        holdTimer = 0f;
        longPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        isHolding = false;

        if (longPressTriggered)
        {
            longPressTriggered = false;

            if (FoodWebLines.Instance != null)
                FoodWebLines.Instance.HideConnections();
        }
        else
        {
            OnTap();
        }
    }

void OnTap()
    {
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        StartCoroutine(TapPunch());

        if (ModalController.Instance != null && cardImage != null)
            ModalController.Instance.Open(cardImage);
    }

    System.Collections.IEnumerator TapPunch()
    {
        Vector3 original = transform.localScale;
        Vector3 big = original * 1.2f;
        float t = 0f;

        // scale up
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.1f;
            transform.localScale = Vector3.Lerp(original, big, Mathf.Clamp01(t));
            yield return null;
        }

        // scale back down
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            transform.localScale = Vector3.Lerp(big, original, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localScale = original;
    }

    void ShowLockedHint()
    {
        if (data == null || GameState.Instance == null) return;

        int taps = GameState.Instance.tapCounts.ContainsKey(data.speciesName)
            ? GameState.Instance.tapCounts[data.speciesName]
            : 0;

        GameState.Instance.tapCounts[data.speciesName] = taps + 1;

        string[] hints = { data.hint1, data.hint2, data.hint3 };
        int level = Mathf.Min(taps, hints.Length - 1);

        string[] labels = { "Hint", "Clearer hint", "Almost there" };
        string label = labels[Mathf.Min(taps, labels.Length - 1)];

        if (LockedHintPanel.Instance != null)
            LockedHintPanel.Instance.Show(label, hints[level]);
    }
}