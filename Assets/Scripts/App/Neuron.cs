using UnityEngine;

namespace Neuralizer {
    [System.Serializable]
    public class Neuron {
        public Vector4 weights;
        public float bias;
        public ActivationFunction activationFunction;

        public const float ReLuOffset = 0f;
        public const float StepThreshold = 0f;
        public const float StepMax = 1f;
        public const float StepMin = 0f;
        
        public float Sample(Vector4 value) {
            float sample = Vector3.Dot(value, weights) + bias;
            return activationFunction switch {
                ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-sample)),
                ActivationFunction.TanH => (float)System.Math.Tanh(sample),
                ActivationFunction.ReLu => Mathf.Max(0f, sample + ReLuOffset),
                ActivationFunction.Stepped => sample > StepThreshold ? StepMax : StepMin,
                _ => sample
            };
        }

        public static void Randomize(Neuron target) {
            target.weights.Set((Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f,
                (Random.value - 0.5f) * 10f);
            target.bias = Random.Range(-5f, 5f);
            target.activationFunction = (ActivationFunction)Random.Range(0, 4);
        }
    }
}