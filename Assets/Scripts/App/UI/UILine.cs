using UnityEngine;

namespace Neuralizer.UI {
    
    [ExecuteInEditMode]
    public class UILine : MonoBehaviour {
        
        [SerializeField] private float thickness = 5f;
        [SerializeField] private float lengthOffset = 0f;
        [SerializeField] private RectTransform from;
        [SerializeField] private RectTransform to;
        [SerializeField] private RectTransform rectTransform;

        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnGUI() {
            if (!from) return;
            if (!to) return;
            if (!rectTransform) return;

            if (from.position.x > to.position.x) {
                (from, to) = (to, from);
            }

            if (!from.gameObject.activeSelf || !to.gameObject.activeSelf) return;
            
            rectTransform.position = (from.position + to.position) / 2f;
            Vector3 dif = to.position - from.position;
            rectTransform.sizeDelta = new Vector3(dif.magnitude + lengthOffset, thickness);
            rectTransform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 180f * Mathf.Atan(dif.y / dif.x) / Mathf.PI));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            rectTransform ??= GetComponent<RectTransform>();
        }
#endif
    }
}