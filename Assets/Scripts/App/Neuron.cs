using UnityEngine;

namespace Neuralizer {
    [System.Serializable]
    public class Neuron {

        private Vector4 info;
        
        public Vector3 Weights {
            get => new(info.x, info.y, info.z);
            set => info.Set(value.x, value.y, value.z, info.w);
        }

        public float Bias {
            get => info.w;
            set => info.Set(info.x, info.y, info.z, value);
        }

        public Vector4 Info {
            get => info;
            set => info = value;
        }

        public float Sample(Vector3 value, ActivationFunction function = ActivationFunction.Sigmoid) {
            float sample = Vector3.Dot(value, Weights) + Bias;
            return function switch {
                ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-sample)),
                ActivationFunction.TanH => (float)System.Math.Tanh(sample),
                ActivationFunction.ReLu => Mathf.Max(0f, sample),
                ActivationFunction.Stepped => sample > 0f ? 1f : 0f,
                _ => sample
            };
        }

        public static void Randomize(Neuron target) {
            target.Weights = Random.insideUnitSphere * 10f;
            target.Bias = Random.Range(-5f, 5f);
        }

        public static Neuron CreateRandom() {
            Neuron output = new();
            Randomize(output);
            return output;
        }

    }
}