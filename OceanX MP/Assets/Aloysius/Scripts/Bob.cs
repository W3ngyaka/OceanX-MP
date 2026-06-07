using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Bob : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float speed = 1.2f;
    public float height = 6f;
    private Vector3 startPos;
    private float offset;

    void Start()
    {
        startPos = transform.localPosition;
        offset = Random.Range(0f, Mathf.PI * 2f); 
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed + offset) * height;
        transform.localPosition = startPos + new Vector3(0, y, 0);

    }

  public void OnPointerEnter(PointerEventData e) {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(1.1f, 0.15f));
    }

    public void OnPointerExit(PointerEventData e) {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(1f, 0.15f));
    }

    IEnumerator ScaleTo(float target, float duration) {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.one * target;
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            float e = t - 1f;
            float eased = e * e * ((2.5f + 1) * e + 2.5f) + 1f;
            transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }

        transform.localScale = endScale;
    }
}
