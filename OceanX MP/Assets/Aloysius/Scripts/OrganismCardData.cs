using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrganismCardData : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text nameText; // optional, leave unassigned if not used

    private int speciesIndex = -1;

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;
        if (countText != null) countText.text = count.ToString();
    }

    // Hook this to a Button.onClick if you want tap-to-remove (prototype spec: -1 or All)
    public void OnTapRemoveOne()
    {
        if (speciesIndex < 0) return;
        EcosystemNetworkManagerGPU.Instance?.RequestRemoveSpeciesRpc(speciesIndex);
    }
}
