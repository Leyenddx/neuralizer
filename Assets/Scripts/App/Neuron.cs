using Neuralizer.NetworkRendering;
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

        public float EvaluateActivationFunction(float value) {
            return activationFunction switch {
                ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-value)),
                ActivationFunction.TanH => (float)System.Math.Tanh(value),
                ActivationFunction.ReLu => Mathf.Max(0f, value + ReLuOffset),
                ActivationFunction.Stepped => value > StepThreshold ? StepMax : StepMin,
                _ => value
            };
        }

        public float Sample(Vector4 value) {
            return EvaluateActivationFunction(Vector4.Dot(value, weights) + bias);
        }

        public float Sample(Vector3 value) {
            return EvaluateActivationFunction(Vector3.Dot(value, weights) + bias);
        }
        
        // public float Sample(Vector2 value) {
        //     return EvaluateActivationFunction(Vector2.Dot(value, weights) + bias);
        // }

        public void SetShaderData() {
            NeuralRenderPass.SetShaderData(this);
        }

        public static void Randomize(Neuron target) {
            target.weights.Set((Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f,
                (Random.value - 0.5f) * 10f);
            target.bias = Random.Range(-5f, 5f);
            target.activationFunction = (ActivationFunction)Random.Range(0, 4);
        }
    }
}