using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIDownListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    [SerializeField] private UnityEvent<bool> OnPointer;
    
    public void OnPointerDown(PointerEventData eventData) {
        OnPointer?.Invoke(true);
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        OnPointer?.Invoke(false);
    }
}
