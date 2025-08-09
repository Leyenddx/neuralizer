using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace Neuralizer {
    public class SamplePoint : MonoBehaviour {
        
        private static List<SamplePoint> points = new();

        public static float CountRcp => 1f / (float)points.Count;

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

        public static bool AreAllValid => points.All(point => point.Valid);

        public static float CorrectnessRatio => (float)points.Count(point => point.Valid) / (float)points.Count;

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