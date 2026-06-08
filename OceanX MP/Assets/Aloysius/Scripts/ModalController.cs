using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ModalController : MonoBehaviour
{
    public static ModalController Instance;

    private Image img;
    private CanvasGroup cg;

    void Awake()
    {
        Instance = this;
        img = GetComponent<Image>();
        cg = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Open(Sprite card)
    {
        img.sprite = card;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.2f;
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / 0.15f;
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
