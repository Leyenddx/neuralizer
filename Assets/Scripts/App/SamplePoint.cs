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

        public static bool AreAllValid() {
            foreach (SamplePoint point in points) {
                if (!point.Valid) {
                    return false;
                }
            }
            return true;
        }

        [SerializeField] private Gradient colors;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private bool pointOfInterest;

        private float value = 0f;

        public ObjectPool<SamplePoint> ParentPool { get; set; }

        public bool Valid => pointOfInterest ? value > 0.5f : value <= 0.5f;

        private void OnEnable() {
            points.Add(this);
        }

        private void OnDisable() {
            points.Remove(this);
        }

        private void UpdateValue() {
            value = NetworkHost.Instance.EvaluateAt(transform.position);
            value = Mathf.Clamp01(value);
            target.color = colors.Evaluate(pointOfInterest ? value : 1f - value);
            // target.color = colors.Evaluate(Valid ? 1f : 0f);
        }
    }
}