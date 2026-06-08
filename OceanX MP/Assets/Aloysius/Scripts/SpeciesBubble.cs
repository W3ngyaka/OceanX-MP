using UnityEngine;
using UnityEngine.UI;

public class SpeciesBubble : MonoBehaviour
{
    public Sprite cardImage;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnTap);
    }

    void OnTap()
    {
        Debug.Log("Bubble tapped: " + gameObject.name);
        Debug.Log("ModalController.Instance: " + ModalController.Instance);
        Debug.Log("cardImage: " + cardImage);
        if (ModalController.Instance != null && cardImage != null)
            ModalController.Instance.Open(cardImage);
    }
}
