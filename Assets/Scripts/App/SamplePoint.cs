using UnityEngine;
using System.Collections.Generic;

namespace Neuralizer {
    public class SamplePoint : MonoBehaviour {
        
        private static List<SamplePoint> points = new();

        public static void UpdatePoints() {
            foreach (SamplePoint point in points) {
                point.UpdateValue();
            }
        }

        [SerializeField] private Color colorOk;
        [SerializeField] private Color colorFalse;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private bool pointOfInterest;

        private float value = 0f;

        private void OnEnable() {
            points.Add(this);
            transform.position = Random.insideUnitSphere * 10f;
            UpdateValue();
        }

        private void OnDisable() {
            points.Remove(this);
        }

        private void UpdateValue() {
            value = NetworkHost.Instance.EvaluateAt(transform.position);
            target.color = (pointOfInterest ? value >= 0.5f : value < 0.5f) ? colorOk : colorFalse;
        }
    }
}