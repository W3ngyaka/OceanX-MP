using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevealQueue : MonoBehaviour
{
    public static RevealQueue Instance { get; private set; }

    [Header("Backlog handling")]
        public int backlogThreshold = 2;
        public float shortHoldSeconds = 1.5f;

        public int maxBacklog = 3;

    private class Request
    {
        public CanvasGroup group;
        public Action onShow;
        public float hold;
        public float fade;
        public Action onComplete;
        public string key;
    }

    private readonly Queue<Request> _queue = new Queue<Request>();
    private bool _playing;
    private CanvasGroup _currentGroup;

    public static RevealQueue Get()
    {
        if (Instance == null)
        {
            var existing = FindFirstObjectByType<RevealQueue>();
            Instance = existing != null ? existing : new GameObject("RevealQueue").AddComponent<RevealQueue>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Enqueue(CanvasGroup group, Action onShow, float holdSeconds, float fadeDuration, Action onComplete = null, string key = null)
    {
        if (group == null) { onComplete?.Invoke(); return; }

        if (!string.IsNullOrEmpty(key) && _queue.Count > 0)
        {
            var arr = _queue.ToArray();
            if (arr[arr.Length - 1].key == key) { onComplete?.Invoke(); return; }
        }

        group.alpha = 0f;
        _queue.Enqueue(new Request
        {
            group = group,
            onShow = onShow,
            hold = holdSeconds,
            fade = fadeDuration,
            onComplete = onComplete,
            key = key
        });

        if (maxBacklog > 0)
        {
            while (_queue.Count > maxBacklog)
            {
                var dropped = _queue.Dequeue();
                dropped.onComplete?.Invoke();
            }
        }

        if (!_playing) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _playing = true;
        while (_queue.Count > 0)
        {
            var r = _queue.Dequeue();
            _currentGroup = r.group;

            r.onShow?.Invoke();

            float hold = _queue.Count > backlogThreshold ? Mathf.Min(shortHoldSeconds, r.hold) : r.hold;

            yield return Fade(r.group, 1f, r.fade);
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
            yield return Fade(r.group, 0f, r.fade);

            _currentGroup = null;
            r.onComplete?.Invoke();
        }
        _playing = false;
    }

    public void ClearAll()
    {
        StopAllCoroutines();
        _queue.Clear();
        if (_currentGroup != null) _currentGroup.alpha = 0f;
        _currentGroup = null;
        _playing = false;
    }

    private IEnumerator Fade(CanvasGroup cg, float target, float dur)
    {
        if (cg == null) yield break;
        if (dur <= 0f) { cg.alpha = target; yield break; }

        float start = cg.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            cg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }
        cg.alpha = target;
    }
}
