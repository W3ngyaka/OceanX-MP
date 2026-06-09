using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SpeciesBubble : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite cardImage;
    public string speciesKey;

    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    private float holdDuration = 0.5f;
    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool longPressTriggered = false;

    void Update()
    {
        if (!isHolding) return;

        holdTimer += Time.unscaledDeltaTime;

        if (holdTimer >= holdDuration && !longPressTriggered)
        {
            longPressTriggered = true;

            FoodWebLines.Instance.ShowConnections(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdTimer = 0f;
        longPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;

        if (!longPressTriggered)
        {
            OnTap();
        }

        longPressTriggered = false;
    }

    void OnTap()
    {
        if (ModalController.Instance != null && cardImage != null)
        {
            ModalController.Instance.Open(cardImage);
        }
    }
}