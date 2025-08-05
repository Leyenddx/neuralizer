using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {

        [Header("Scene Settings")]
        [SerializeField] private string displayName;
        [SerializeField] private string scoreboardID;
        public Vector3 origin;
        [Header("Network Settings")]
        public Architecture architecture;
        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        [Header("Scene Constraints")]
        public bool useOrbitalCamera = false;
        public bool allowEditing = true;
        public bool allowTraining = false;
        public bool allowRandomizeNeuron = true;
        public bool allowRandomizeNetwork = true;
        
        [Header("Output Layer")]
        public NeuronPrefab Output;

        [Header("First Layer")]
        public NeuronPrefab AA;
        public NeuronPrefab AB;
        public NeuronPrefab AC;
        public NeuronPrefab AD;
        [Header("Second Layer")]
        public NeuronPrefab BA;
        public NeuronPrefab BB;
        public NeuronPrefab BC;
        public NeuronPrefab BD;

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
            public float weightX = 0f;
            [Range(-10f, 10f)]
            public float weightY = 0f;
            [Range(-10f, 10f)]
            public float weightZ = 0f;
            [Range(-10f, 10f)]
            public float weightW = 0f;
            [Range(-50f, 50f)]
            public float bias = 0f;
            public ActivationFunction activationFunction;
            public bool displayNeuron;
            
            public void ApplyTo(Neuron neuron) {
                neuron.weights.Set(weightX, weightY, weightZ, weightW);
                neuron.bias = bias;
                neuron.activationFunction = activationFunction;
            }
        }

    }
}