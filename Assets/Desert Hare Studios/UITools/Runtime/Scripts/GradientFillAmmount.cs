using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GradientFillAmmount : MonoBehaviour {

    [SerializeField] private Gradient gradient;
    [SerializeField] private Image img;

    private void OnGUI() {
        if(img) {
            img.color = gradient.Evaluate(img.fillAmount);
        }
    }

#if UNITY_EDITOR
    private void OnValidate() {
        img = GetComponent<Image>();
    }
#endif

}