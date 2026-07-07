using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrganismCardData : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text nameText; // optional, leave unassigned if not used

    [Header("Live update")]
    [Tooltip("Seconds between population polls. 0 = every frame.")]
    public float pollInterval = 0.2f;

    private int speciesIndex = -1;
    private int lastShown = -1;
    private float pollTimer;

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;

        // Show the optimistic count so a card recreated after a panel switch reflects a pending
        // removal instead of snapping back to the host's not-yet-lowered count.
        SetShown(OptimisticPopulationStore.Display(index));
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
    }

    void SetShown(int count)
    {
        lastShown = count;
        if (countText != null) countText.text = count.ToString();
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
