using UnityEngine;
using UnityEngine.UI;
public class Health : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image fillImage;
    public float ecoHealth = 1f; // 0 to 1

    void Update()
    {
        // Smoothly animate the bar
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, ecoHealth, Time.deltaTime * 3f);
    }

    public void SetHealth(float value)
    {
        ecoHealth = Mathf.Clamp01(value);
    }
}
