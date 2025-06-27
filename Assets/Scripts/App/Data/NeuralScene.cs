using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {

        [SerializeField] private string displayName;
        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        public NetworkArchitecture networkArchitecture = NetworkArchitecture.Perceptron;
        public bool useOrbitalCamera = false;

        public NeuronPrefab A, B, C, D, E, F, Output;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        [System.Serializable]
        public class NeuronPrefab {
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthX = 0f;
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthY = 0f;
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthZ = 0f;
            [Range(-10f, 10f)]
            public float bias = 0f;
            public ActivationFunction activationFunction;

            public void ApplyTo(Neuron neuron) {
                neuron.Info = new Vector4(weigthX, weigthY, weigthZ, bias);
                neuron.ActivationFunction = activationFunction;
            }
        }

    }
}