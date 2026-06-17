using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;
    public TextMeshProUGUI messageText;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void ShowUnlocked(SpeciesData s)
    {
        messageText.text = $"You've unlocked the {s.speciesName}!";
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
