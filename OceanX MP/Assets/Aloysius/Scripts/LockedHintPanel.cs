using UnityEngine;
using TMPro;
using System.Collections;

public class LockedHintPanel : MonoBehaviour
{
    public static LockedHintPanel Instance;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI hintText;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show(string label, string hint)
    {
        labelText.text = label;
        hintText.text = hint;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(4f);
        gameObject.SetActive(false);
    }
}
