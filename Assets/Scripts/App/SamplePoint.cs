using UnityEngine;

public class SamplePoint : MonoBehaviour
{
    
    [SerializeField] private Material okMaterial;
    [SerializeField] private Material falseMaterial;
    [SerializeField] private float okThreshold = 0f;
    [SerializeField] private bool invert = false;
    [SerializeField] private Renderer target;

    private float value = 0f;

    private void Start() {
        OnPerceptronChanged();
    }

    private void OnEnable() {
        PerceptronHost.Instance.OnPerceptronChanged += OnPerceptronChanged;
    }
    
    private void OnDisable() {
        PerceptronHost.Instance.OnPerceptronChanged -= OnPerceptronChanged;
    }

    private void OnPerceptronChanged() {
        value = PerceptronHost.Instance.EvaluateAt(transform.position);
        target.sharedMaterial = (invert ? value <= okThreshold : value >= okThreshold) ? okMaterial : falseMaterial;
    }
}
