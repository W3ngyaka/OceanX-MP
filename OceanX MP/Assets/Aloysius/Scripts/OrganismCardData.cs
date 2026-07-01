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
    private int lastCount = -1;
    private float pollTimer;

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;
        SetCount(count);
    }

    void Update()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = pollInterval;

        int pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex);

        if (pop <= 0)
        {
            // Species no longer in the ecosystem: remove this card from the list.
            gameObject.SetActive(false);
            return;
        }

        if (pop != lastCount) SetCount(pop);
    }

    void SetCount(int count)
    {
        lastCount = count;
        if (countText != null) countText.text = count.ToString();
    }

    // Hook this to a Button.onClick if you want tap-to-remove (prototype spec: -1 or All)
    public void OnTapRemoveOne()
    {
        if (speciesIndex < 0) return;
        EcosystemNetworkManagerGPU.Instance?.RequestRemoveSpeciesRpc(speciesIndex);
        // The count will refresh on the next poll; no manual decrement needed.
    }
}
