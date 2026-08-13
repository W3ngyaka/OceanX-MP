using UnityEngine;
using UnityEngine.UI;
public class Health : MonoBehaviour
{

    public Image fillImage;
    public float ecoHealth = 1f;

        public bool readFromSimulation = true;

    void Update()
    {

        if (readFromSimulation && EcosystemNetworkManagerGPU.Instance != null)
            ecoHealth = EcosystemNetworkManagerGPU.Instance.GetEcoHealth();

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, ecoHealth, Time.deltaTime * 3f);
    }

    public void SetHealth(float value)
    {
        ecoHealth = Mathf.Clamp01(value);
    }
}
