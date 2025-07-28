using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {

        [SerializeField] private string displayName;
        [SerializeField] private string scoreboardID;
        public Architecture architecture;
        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        public bool useOrbitalCamera = false;
        public bool allowTraining = false;

        public NeuronPrefab AA, AB, AC, AD, BA, BB, BC, BD, Output;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        
        public string ScoreboardID => string.IsNullOrEmpty(scoreboardID) ? "" : scoreboardID;

        public bool AllowScoring => !string.IsNullOrEmpty(scoreboardID);
        
        public NeuronPrefab GetTargetNeuron(TargetNeuron target) {
            switch (target) {
                default:
                case TargetNeuron.None:
                    return null;
                case TargetNeuron.AA:
                    return AA;
                case TargetNeuron.AB:
                    return AB;
                case TargetNeuron.AC:
                    return AC;
                case TargetNeuron.AD:
                    return AD;
                case TargetNeuron.BA:
                    return BA;
                case TargetNeuron.BB:
                    return BB;
                case TargetNeuron.BC:
                    return BC;
                case TargetNeuron.BD:
                    return BD;
                case TargetNeuron.Output:
                    return Output;
            }
        }

        [System.Serializable]
        public class NeuronPrefab {
            [Range(-10f, 10f)]
            public float weigthX = 0f;
            [Range(-10f, 10f)]
            public float weigthY = 0f;
            [Range(-10f, 10f)]
            public float weigthZ = 0f;
            [Range(-10f, 10f)]
            public float weigthW = 0f;
            [Range(-32f, 32f)]
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