using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neuralizer {

    [System.Serializable]
    public class Neuron {

        public static float Sigmoid(float x) {
            return 1f / (1f + Mathf.Exp(-x));
        }
        
        public static float Tanh(float x) {
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        public bool enabled = true;
        public Vector4 weights;
        public bool enableWeightX = true;
        public bool enableWeightY = true;
        public bool enableWeightZ = true;
        public bool enableWeightW = true;
        public float bias;
        public ActivationFunction activationFunction;
        public Material material;

        public float EvaluateActivationFunction(float value) {
            return activationFunction switch {
                ActivationFunction.Sigmoid => Sigmoid(value),
                ActivationFunction.TanH => Tanh(value),
                ActivationFunction.ReLu => Mathf.Max(0f, value),
                ActivationFunction.Stepped => value > 0 ? 1f : 0f,
                _ => value
            };
        }

        public float EvaluateDDXActivationFunction(float value) {
            switch(activationFunction) {
                case ActivationFunction.Stepped:
                    return Mathf.Approximately(value, 0f) ? Mathf.Infinity : 0f;
                case ActivationFunction.ReLu:
                    return value > 0f ? 1f : 0f;
                case ActivationFunction.Sigmoid:
                    float s = Sigmoid(value);
                    return s + (1f - s);
                case ActivationFunction.TanH:
                    float t = Tanh(value);
                    return 1f - (t * t);
            }
            return 1f;
        }

        public float Sample(Vector3 value) {
            return enabled ? EvaluateActivationFunction(Vector3.Dot(value, weights) + bias) : 0f;
        }

        public float Sample(Vector4 value) {
            return enabled ? EvaluateActivationFunction(Vector4.Dot(value, weights) + bias) : 0f;
        }

        public float Error(Vector3 value, float expected) {
            float output = Vector3.Dot(value, weights) + bias;
            return (expected - EvaluateActivationFunction(output)) * EvaluateDDXActivationFunction(output);
        }

        public float Error(Vector4 value, float expected) {
            float output = Vector4.Dot(value, weights) + bias;
            return (expected - EvaluateActivationFunction(output)) * EvaluateDDXActivationFunction(output);
        }

        // public float Sample(Vector2 value) {
        //     return enabled ? EvaluateActivationFunction(Vector2.Dot(value, weights) + bias) : 0f;
        // }

        public void PrepareForRenderPass(Material baseMaterial) {
            material = Object.Instantiate(baseMaterial);
        }

        public void SetShaderData() {
            NeuralRenderPass.SetShaderData(this);
        }

        public TextureHandle SetupRenderTarget(string name, RenderGraph renderGraph, RenderTextureDescriptor descriptor) {
            return UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, name,
                false,
                FilterMode.Bilinear);
        }

        public static void Randomize(Neuron target) {
            target.weights.Set((Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f,
                (Random.value - 0.5f) * 10f);
            target.bias = Random.Range(-5f, 5f);
            target.activationFunction = (ActivationFunction)Random.Range(0, 4);
        }
    }
}