using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrganismCardData : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text nameText; // optional, leave unassigned if not used

    [Header("Overpopulation badge")]
    [Tooltip("Shown when this species is at/over its carrying capacity (pop >= MaxSchools). " +
             "Auto-found (child named 'OverpopBadge') if left empty. Matches the food-web bubble's overpop logic.")]
    public GameObject overpopBadge;

    [Header("Live update")]
    [Tooltip("Seconds between population polls. 0 = every frame.")]
    public float pollInterval = 0.2f;

    private int speciesIndex = -1;
    private int lastShown = -1;
    private float pollTimer;
    private bool overpopShown;

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;

        if (overpopBadge == null)
        {
            var b = transform.Find("OverpopBadge");
            if (b != null) overpopBadge = b.gameObject;
        }
        if (overpopBadge != null) overpopBadge.SetActive(false);
        overpopShown = false;

        // Show the optimistic count so a card recreated after a panel switch reflects a pending
        // removal instead of snapping back to the host's not-yet-lowered count.
        SetShown(OptimisticPopulationStore.Display(index));
        UpdateOverpop();
    }

    void Update()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = pollInterval;

        int display = OptimisticPopulationStore.Display(speciesIndex);
        if (display <= 0)
        {
            // Species no longer in the ecosystem: remove this card from the list.
            gameObject.SetActive(false);
            return;
        }
        if (display != lastShown) SetShown(display);
        UpdateOverpop();
    }

    void SetShown(int count)
    {
        lastShown = count;
        if (countText != null) countText.text = count.ToString();
    }

    // Toggle the overpopulation badge using the SAME check the food-web bubbles use:
    // pop >= MaxSchools (both values arrive already-synced from the host). Only AT/over cap.
    void UpdateOverpop()
    {
        if (overpopBadge == null) return;
        bool over = false;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (speciesIndex >= 0 && net != null)
        {
            int pop = net.GetPopulation(speciesIndex);
            int max = net.GetMaxSchools(speciesIndex);
            over = max > 0 && pop >= max;
        }
        if (over != overpopShown)
        {
            overpopShown = over;
            overpopBadge.SetActive(over);
        }
    }

    // Hook this to the card's "-1" Button.onClick (prototype spec: -1 or All).
    public void OnTapRemoveOne()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;
        if (OptimisticPopulationStore.Display(speciesIndex) <= 0) return; // already at zero optimistically

        // Record the intent in the shared store (persists across panel switches), then fire the RPC.
        OptimisticPopulationStore.RegisterDelta(speciesIndex, -1);
        EcosystemNetworkManagerGPU.Instance.RequestRemoveSpeciesRpc(speciesIndex);

        // Update the number the instant the player taps — don't wait for the next poll.
        int display = OptimisticPopulationStore.Display(speciesIndex);
        if (display <= 0) { SetShown(0); gameObject.SetActive(false); }
        else SetShown(display);
    }
}
