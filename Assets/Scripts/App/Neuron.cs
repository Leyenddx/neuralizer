using UnityEngine;

namespace Neuralizer {
    [System.Serializable]
    public class Neuron {

        public string activationFunctionShaderPrefix;
        
        private Vector4 info;
        private Vector4 fParams;
        private ActivationFunction activationFunction;
        
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

        public float StepMin {
            get => fParams.x;
            set => fParams.Set(value, fParams.y, fParams.z, fParams.w);
        }

        public float StepMax {
            get => fParams.y;
            set => fParams.Set(fParams.x, value, fParams.z, fParams.w);
        }
        
        public float StepThreshold {
            get => fParams.z;
            set => fParams.Set(fParams.x, fParams.y, value, fParams.w);
        }
        
        public float ReLuOffset {
            get => fParams.w;
            set => fParams.Set(fParams.x, fParams.y, fParams.z, value);
        }

        public Vector4 FunctionParams {
            get => fParams;
            set => fParams = value;
        }

        public ActivationFunction ActivationFunction {
            get => activationFunction;
            set {
                activationFunction = value;
                switch (activationFunction) {
                    case ActivationFunction.Sigmoid:
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_TANH");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_RELU");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_STEPPED");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_LINEAL");
                        Shader.EnableKeyword(activationFunctionShaderPrefix + "_SIGMOID");
                    break;
                    case ActivationFunction.TanH:
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_RELU");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_STEPPED");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_LINEAL");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_SIGMOID");
                        Shader.EnableKeyword(activationFunctionShaderPrefix + "_TANH");
                    break;
                    case ActivationFunction.ReLu:
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_STEPPED");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_LINEAL");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_SIGMOID");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_TANH");
                        Shader.EnableKeyword(activationFunctionShaderPrefix + "_RELU");
                    break;
                    case ActivationFunction.Stepped:
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_LINEAL");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_SIGMOID");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_TANH");
                        Shader.DisableKeyword(activationFunctionShaderPrefix + "_RELU");
                        Shader.EnableKeyword(activationFunctionShaderPrefix + "_STEPPED");
                    break;
                }
            }
        }

        public Neuron(string activationFunctionShaderPrefix) {
            this.activationFunctionShaderPrefix = activationFunctionShaderPrefix;
            FunctionParams = new(0f, 1f, 0f, 0f);
        }

        public float Sample(Vector3 value) {
            float sample = Vector3.Dot(value, Weights) + Bias;
            return ActivationFunction switch {
                ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-sample)),
                ActivationFunction.TanH => (float)System.Math.Tanh(sample),
                ActivationFunction.ReLu => Mathf.Max(0f, sample + ReLuOffset),
                ActivationFunction.Stepped => sample > StepThreshold ? StepMax : StepMin,
                _ => sample
            };
        }

        public static void Randomize(Neuron target) {
            target.Weights = Random.insideUnitSphere * 10f;
            target.Bias = Random.Range(-5f, 5f);
        }

    }
}