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

    // --- Optimistic UI ---------------------------------------------------------
    // Adding a school bumps the host count immediately, so Add already feels instant.
    // Removing DOESN'T: the host keeps the old count until the school has finished
    // swimming out, so a raw read would sit still for a few seconds after a -1 tap.
    // To make Remove feel instant too we store the player's not-yet-confirmed change
    // here and show (authoritative + pending) right away. As the host's real count
    // catches up in the same direction, we consume that much pending so the number
    // stays put and finally lands exactly on the host's value (no double-counting).
    private int pendingDelta = 0;
    private int lastAuth = 0;

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;
        pendingDelta = 0;
        lastAuth = count;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;
        SetShown(count);
    }

    void Update()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = pollInterval;

        Reconcile();

        int display = DisplayCount();
        if (display <= 0)
        {
            // Species no longer in the ecosystem: remove this card from the list.
            gameObject.SetActive(false);
            return;
        }
        if (display != lastShown) SetShown(display);
    }

    // The number to show: the host's synced count plus the player's not-yet-confirmed taps.
    private int DisplayCount()
    {
        int auth = EcosystemNetworkManagerGPU.Instance != null
            ? EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex)
            : 0;
        return Mathf.Max(0, auth + pendingDelta);
    }

    // As the host's authoritative count moves the way the player asked, consume that much
    // pending intent so the displayed target holds steady and settles on the real count.
    // Movement the other way (e.g. an auto population tick) is just tracked, not cancelled.
    private void Reconcile()
    {
        int auth = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex);
        int move = auth - lastAuth;
        if (move != 0)
        {
            if (pendingDelta < 0 && move < 0)      pendingDelta -= Mathf.Max(pendingDelta, move);
            else if (pendingDelta > 0 && move > 0) pendingDelta -= Mathf.Min(pendingDelta, move);
            lastAuth = auth;
        }
        // Never show below zero if the host can't go any lower.
        if (auth + pendingDelta < 0) pendingDelta = -auth;
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
        if (DisplayCount() <= 0) return; // already at zero optimistically

        // Seed the reconcile baseline the first time we start tracking a pending change.
        if (pendingDelta == 0)
            lastAuth = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex);
        pendingDelta -= 1;

        EcosystemNetworkManagerGPU.Instance.RequestRemoveSpeciesRpc(speciesIndex);

        // Update the number the instant the player taps — don't wait for the next poll.
        int display = DisplayCount();
        if (display <= 0) { SetShown(0); gameObject.SetActive(false); }
        else SetShown(display);
    }
}
