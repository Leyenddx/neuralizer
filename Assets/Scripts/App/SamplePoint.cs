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
    [SerializeField] private Renderer target;
    [SerializeField] private bool pointOfInterest;

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
        target.sharedMaterial = (pointOfInterest ? value >= 0.333f : value < 0.333f) ? okMaterial : falseMaterial;
    }
}
