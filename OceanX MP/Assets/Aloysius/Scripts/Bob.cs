using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Bob : MonoBehaviour
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

}
