using UnityEngine;
using UnityEngine.UI;
public class Health : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image fillImage;
    public float ecoHealth = 1f; // 0 to 1

    [Tooltip("When true, the bar reads the live ecosystem health synced from the host instead of the " +
             "manual ecoHealth value above. Leave on for the real game; turn off to drive it by hand.")]
    public bool readFromSimulation = true;

    void Update()
    {
        // Pull the live health score from the netcode layer when it's available.
        if (readFromSimulation && EcosystemNetworkManagerGPU.Instance != null)
            ecoHealth = EcosystemNetworkManagerGPU.Instance.GetEcoHealth();

        // Smoothly animate the bar
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, ecoHealth, Time.deltaTime * 3f);
    }

    public void SetHealth(float value)
    {
        ecoHealth = Mathf.Clamp01(value);
    }
}
