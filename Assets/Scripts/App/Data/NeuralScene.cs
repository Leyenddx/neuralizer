using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {

        [SerializeField] private string displayName;
        public Architecture architecture;
        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        public bool useOrbitalCamera = false;

        public NeuronPrefab AA, AB, AC, AD, BA, BB, BC, BD, Output;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        [System.Serializable]
        public class NeuronPrefab {
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthX = 0f;
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthY = 0f;
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthZ = 0f;
            [Range(-Mathf.PI, Mathf.PI)]
            public float weigthW = 0f;
            [Range(-10f, 10f)]
            public float bias = 0f;
            public ActivationFunction activationFunction;
            public bool displayNeuron;
            
            public void ApplyTo(Neuron neuron) {
                neuron.weights.Set(weigthX, weigthY, weigthZ, weigthW);
                neuron.bias = bias;
                neuron.activationFunction = activationFunction;
            }
        }

    }
}