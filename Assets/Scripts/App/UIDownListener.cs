using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neuralizer {
    
    /// <summary>
    /// Componente auxiliar para interceptar los eventos de Unity, IPointerDownHandler y IPointerUpHandler, desde el editor
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.IPointerDownHandler.html"/>
    /// <seealso href="https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.IPointerUpHandler.html"/>
    public class UIDownListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        
        /// <summary>
        /// Evento que índica si el apuntador está actualmente sobre el objeto o no
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnPointer;

        /// <inheritdoc cref="IPointerDownHandler.OnPointerDown"/>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.IPointerDownHandler.html"/>
        public void OnPointerDown(PointerEventData eventData) {
            OnPointer?.Invoke(true);
        }

        /// <inheritdoc cref="IPointerUpHandler.OnPointerUp"/>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.IPointerUpHandler.html"/>
        public void OnPointerUp(PointerEventData eventData) {
            OnPointer?.Invoke(false);
        }
    }
}