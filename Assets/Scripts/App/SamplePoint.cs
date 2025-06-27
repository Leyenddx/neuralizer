using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Neuralizer {
    public class SamplePoint : MonoBehaviour {
        
        private static List<SamplePoint> points = new();

        public static void UpdatePoints() {
            foreach (SamplePoint point in points) {
                point.UpdateValue();
            }
        }

        public static void ClearPoints() {
            while (points.Count > 0) {
                var point = points[0];
                point.ParentPool.Release(point);
            }
        }

        [SerializeField] private Gradient color;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private bool pointOfInterest;

        private float value = 0f;

        public ObjectPool<SamplePoint> ParentPool { get; set; }

        private void OnEnable() {
            points.Add(this);
        }

        private void OnDisable() {
            points.Remove(this);
        }

        private void UpdateValue() {
            value = NetworkHost.Instance.EvaluateAt(transform.position);
            value = Mathf.Clamp(value, -1f, 1f);
            value = (value + 1f) / 2f;
            target.color = color.Evaluate(value);
        }
    }
}