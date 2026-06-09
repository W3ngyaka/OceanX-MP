using UnityEngine;
using UnityEngine.UI;

public class ModalController : MonoBehaviour
{
    public static ModalController Instance;

    private Image img;

    void Awake()
    {
        Instance = this;
        img = GetComponent<Image>();
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Open(Sprite card)
    {
        img.sprite = card;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
