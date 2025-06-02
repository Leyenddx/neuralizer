using UnityEngine;
using System.Collections.Generic;

public class SamplePoint : MonoBehaviour {

    private static List<SamplePoint> points = new();

    public static void UpdatePoints() {
        foreach (SamplePoint point in points) {
            point.UpdateValue();
        }
    }

    [SerializeField] private Material okMaterial;
    [SerializeField] private Material falseMaterial;
    [SerializeField] private float okThreshold = 0f;
    [SerializeField] private bool invert = false;
    [SerializeField] private Renderer target;

    private float value = 0f;

    private void Start() {
        UpdateValue();
    }

    private void OnEnable() {
        points.Add(this);
    }
    
    private void OnDisable() {
        points.Remove(this);
    }

    private void UpdateValue() {
        value = PerceptronHost.Instance.EvaluateAt(transform.position);
        target.sharedMaterial = (invert ? value < okThreshold : value >= okThreshold) ? okMaterial : falseMaterial;
    }
}
