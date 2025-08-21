using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SliderText : MonoBehaviour {

    [SerializeField] private Slider slider;

    [SerializeField] private UnityEvent<string> OnTextUpdate;

    private void OnGUI() {
        if(slider) {
            
            OnTextUpdate?.Invoke((Mathf.Round((slider.value / 4f) * 100f) / 100f).ToString());
            
            //OnTextUpdate?.Invoke(slider.wholeNumbers
            //    ? Mathf.RoundToInt(slider.value).ToString()
            //    : (Mathf.Round(slider.value * 100f) / 100f).ToString());
        }
    }

#if UNITY_EDITOR
    private void OnValidate() {
        slider ??= GetComponent<Slider>();
    }
#endif

}
