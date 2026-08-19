using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SonarPulse : MonoBehaviour
{
    [Header("Timing")]
        public float interval = 3.5f;
        public float lifetime = 4f;

    [Header("Size")]
        public float startDiameter = 40f;
        public float maxDiameter = 760f;

    [Header("Look")]
        public Color ringColor = new Color(0.6f, 0.9f, 1f, 0.5f);
        public float thickness = 3f;

    private Sprite _ringSprite;
    private float _timer;
    private readonly List<Pulse> _pulses = new List<Pulse>();

    private class Pulse
    {
        public RectTransform rt;
        public Image img;
        public float age;
    }

    void Awake()
    {
        _ringSprite = BuildRingSprite(128, thickness);
    }

    void OnEnable()
    {

        _timer = interval;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= interval)
        {
            _timer = 0f;
            Emit();
        }

        for (int i = _pulses.Count - 1; i >= 0; i--)
        {
            var p = _pulses[i];
            p.age += Time.deltaTime;
            float t = p.age / lifetime;
            if (t >= 1f)
            {
                Destroy(p.rt.gameObject);
                _pulses.RemoveAt(i);
                continue;
            }
            float dia = Mathf.Lerp(startDiameter, maxDiameter, t);
            p.rt.sizeDelta = new Vector2(dia, dia);

            float alpha = ringColor.a * Mathf.Sin(t * Mathf.PI);
            var c = ringColor; c.a = alpha;
            p.img.color = c;
        }
    }

    void Emit()
    {
        var go = new GameObject("PulseRing", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(startDiameter, startDiameter);
        rt.SetAsFirstSibling();

        var img = go.GetComponent<Image>();
        img.sprite = _ringSprite;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        img.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0f);

        _pulses.Add(new Pulse { rt = rt, img = img, age = 0f });
    }

    Sprite BuildRingSprite(int size, float thicknessPx)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float outer = center - 1f;

        float ringHalf = Mathf.Max(1.5f, thicknessPx);
        var clear = new Color(1, 1, 1, 0);
        var solid = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = Mathf.Abs(d - (outer - ringHalf));

                float a = Mathf.Clamp01(1f - (edge / ringHalf));
                tex.SetPixel(x, y, a > 0f ? new Color(1, 1, 1, a) : clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
