using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {
        
        [Header("Scene Settings")] [SerializeField]
        private string displayName;
        [SerializeField] private string storageName;
        [SerializeField] private string scoreboardID;
        public float minTime = 15f;
        public Vector2 minMaxScores = new(1f, 10f);
        public Vector3 origin;
        [Range(0f, 1f)]
        public float manualAutoPositionRatio = 1f / 3f;
        public float zoomFactor = 1f;
        [Header("Network Settings")] public Architecture architecture;
        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        [Header("Scene Constraints")] public Vector2 weightRange = new(-15f, 15f);
        public Vector2 biasRange = new(-30f, 30f);
        public bool useOrbitalCamera = false;
        public WorldInputs inputs = WorldInputs.XY;
        public bool allowEditing = true;
        public bool allowTraining = false;
        public bool allowRandomizeNeuron = true;
        public bool allowRandomizeNetwork = true;
        [Range(0.5f, 1f)]
        public float successThreshold = 0.95f;

        [Header("Output Layer")] public NeuronPrefab Output;

        [Header("First Layer")] public NeuronPrefab AA;
        public NeuronPrefab AB;
        public NeuronPrefab AC;
        public NeuronPrefab AD;
        [Header("Second Layer")] public NeuronPrefab BA;
        public NeuronPrefab BB;
        public NeuronPrefab BC;
        public NeuronPrefab BD;
        
        public event UnityAction OnUpdated;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public bool AllowScoring => !string.IsNullOrEmpty(scoreboardID);

        private string StorageName => string.IsNullOrEmpty(storageName) ? name : storageName;

        private string ScoreboardID => string.IsNullOrEmpty(scoreboardID) ? "" : scoreboardID;

        public bool ZInputNeeded => useOrbitalCamera && inputs == WorldInputs.XYZ;
        

        private string IsCompletedString => StorageName + ".completed";
        
        private string TimeString => ScoreboardID + ".time";
        private string ScoreString => ScoreboardID + ".score";
        private string RatioString => ScoreboardID + ".ratio";

        public bool SubmitResult(float time, float ratio = 1f) {
            if (IsCompleted) return false;
            Time = time;
            Ratio = ratio;
            Score = Mathf.FloorToInt(Mathf.Lerp(minMaxScores.x, minMaxScores.y, Mathf.Clamp01(minTime / Mathf.Max(time, Mathf.Epsilon))) * Mathf.Lerp(successThreshold, 1f, ratio));
            OnUpdated?.Invoke();
            return true;
        }

        public bool IsCompleted {
            get => PlayerPrefs.GetInt(IsCompletedString, 0) > 0;
            set {
                PlayerPrefs.SetInt(IsCompletedString, value ? 1 : 0);
                OnUpdated?.Invoke();
            }
        }

        public float Time {
            get => PlayerPrefs.GetFloat(TimeString, 0);
            private set => PlayerPrefs.SetFloat(TimeString, value);
        }

        public int Score {
            get => PlayerPrefs.GetInt(ScoreString, 0);
            private set => PlayerPrefs.SetInt(ScoreString, value);
        }

        public float Ratio {
            get => PlayerPrefs.GetFloat(RatioString, 0f);
            private set => PlayerPrefs.SetFloat(RatioString, value);
        }

        public static string FormatTime(float time) {
            return System.TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
        }

        public string FormattedTime => FormatTime(Time);

        public static string FormatRatio(float ratio) {
            return Mathf.RoundToInt(ratio * 100f) + "%";
        }
        
        public string FormattedRatio => FormatRatio((Ratio));

        public float NormalizedScore => Mathf.InverseLerp(minMaxScores.x, minMaxScores.y, (float)Score);

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
            [Range(-50f, 50f)] public float weightX = 0f;
            [Range(-50f, 50f)] public float weightY = 0f;
            [Range(-50f, 50f)] public float weightZ = 0f;
            [Range(-50f, 50f)] public float weightW = 0f;
            [Range(-50f, 50f)] public float bias = 0f;
            public ActivationFunction activationFunction;
            public bool displayNeuron;

            public void ApplyTo(Neuron neuron) {
                neuron.weights.Set(weightX, weightY, weightZ, weightW);
                neuron.bias = bias;
                neuron.activationFunction = activationFunction;
                neuron.enabled = displayNeuron;
            }
        }

        public enum WorldInputs 
        {
            [InspectorName("2D XY")]
            XY = 2,
            [InspectorName("3D XYZ")]
            XYZ = 3
        }
    }
}