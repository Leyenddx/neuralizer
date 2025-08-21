using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    /// <summary>
    /// Objeto contenedor de la información de una escena/ejercicio
    /// </summary>
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer/Scene")]
    public class NeuralScene : ScriptableObject {
        
        /// <summary>
        /// Nombre que se mostrara en la interfaz
        /// </summary>
        [Header("Scene Settings")] [SerializeField]
        private string displayName;
        /// <summary>
        /// Nombre que se utilizará en el almacenamiento de datos
        /// </summary>
        [SerializeField] private string storageName;
        /// <summary>
        /// ID de la tabla de puntuación
        /// </summary>
        [SerializeField] private string scoreboardID;
        /// <summary>
        /// Tiempo mínimo para obtener la máxima puntuación
        /// </summary>
        public float minTime = 15f;
        /// <summary>
        /// Rango de puntuación que se puede obtener [min, max]
        /// </summary>
        public Vector2 minMaxScores = new(1f, 10f);
        /// <summary>
        /// Punto donde la cámara orbitara alrededor
        /// </summary>
        public Vector3 origin;
        /// <summary>
        /// Relación entre el origen manual y el origen que se calcula con el promedio de todos los puntos
        /// </summary>
        /// <seealso cref="origin"/>
        /// <seealso cref="targetPoints"/>
        /// <seealso cref="unwantedPoints"/>
        [Range(0f, 1f)]
        public float manualAutoPositionRatio = 1f / 3f;
        /// <summary>
        /// Factor de zoom y escala de puntos
        /// </summary>
        public float zoomFactor = 1f;
        /// <summary>
        /// Arquitectura de la red neural
        /// </summary>
        /// <seealso cref="Neuralizer.Architecture"/>
        [Header("Network Settings")] public Architecture architecture;
        /// <summary>
        /// Lista de los puntos que queremos clasificar
        /// </summary>
        public Vector3[] targetPoints;
        /// <summary>
        /// Lista de los puntos que queremos evitar
        /// </summary>
        public Vector3[] unwantedPoints;
        /// <summary>
        /// Rango de pesos de las neuronas que se podrán editar en la interfaz
        /// </summary>
        [Header("Scene Constraints")] public Vector2 weightRange = new(-15f, 15f);
        /// <summary>
        /// Rango del sesgo que se podrá editar en la interfaz
        /// </summary>
        public Vector2 biasRange = new(-30f, 30f);
        /// <summary>
        /// Define si la cámara tendrá comportamiento orbital o plano
        /// </summary>
        public bool useOrbitalCamera = false;
        /// <summary>
        /// Las entradas permitidas que tendrá la red neural
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.WorldInputs"/>
        public WorldInputs inputs = WorldInputs.XY;
        /// <summary>
        /// Permite la edición de la red por parte del usuario
        /// </summary>
        public bool allowEditing = true;
        /// <summary>
        /// Permite al usuario aleatorizar una neurona 
        /// </summary>
        public bool allowRandomizeNeuron = true;
        /// <summary>
        /// Permite al usuario aleatorizar la red neural 
        /// </summary>
        public bool allowRandomizeNetwork = true;
        /// <summary>
        /// Porcentaje de exactitud mínimo que se requiere para considerar la red como válida
        /// </summary>
        [Range(0.5f, 1f)]
        public float successThreshold = 0.95f;

        /// <summary>
        /// Plantilla de la neurona de salida
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        [Header("Output Layer")] public NeuronPrefab Output;

        /// <summary>
        /// Plantilla de la primer neurona de la primer capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        [Header("First Layer")] public NeuronPrefab AA;
        /// <summary>
        /// Plantilla de la segunda neurona de la primer capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab AB;
        /// <summary>
        /// Plantilla de la tercer neurona de la primer capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab AC;
        /// <summary>
        /// Plantilla de la cuarta neurona de la primer capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab AD;
        /// <summary>
        /// Plantilla de la primer neurona de la segunda capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        [Header("Second Layer")] public NeuronPrefab BA;
        /// <summary>
        /// Plantilla de la segunda neurona de la segunda capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab BB;
        /// <summary>
        /// Plantilla de la tercer neurona de la segunda capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab BC;
        /// <summary>
        /// Plantilla de la cuarta neurona de la segunda capa
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
        public NeuronPrefab BD;
        
        /// <summary>
        /// Evento que se dispara cuando hay una actualización en la información almacenada en la escena
        /// </summary>
        public event UnityAction OnUpdated;

        /// <summary>
        /// Nombre que se mostrara en la interfaz
        /// </summary>
        /// <seealso cref="displayName"/>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>
        /// Describe si la escena debe calcular puntuación y almacenarla
        /// </summary>
        /// <seealso cref="scoreboardID"/>
        public bool AllowScoring => !string.IsNullOrEmpty(scoreboardID);

        /// <summary>
        /// Nombre que se utilizará en el almacenamiento de datos
        /// </summary>
        /// <seealso cref="storageName"/>
        private string StorageName => string.IsNullOrEmpty(storageName) ? name : storageName;

        /// <summary>
        /// ID de la tabla de puntuación
        /// </summary>
        /// <seealso cref="scoreboardID"/>
        private string ScoreboardID => string.IsNullOrEmpty(scoreboardID) ? "" : scoreboardID;

        /// <summary>
        /// Indíca si la escena requiere el eje Z en la entrada de la red
        /// </summary>
        /// <seealso cref="useOrbitalCamera"/>
        /// <seealso cref="Neuralizer.Data.NeuralScene.WorldInputs"/>
        public bool ZInputNeeded => useOrbitalCamera && inputs == WorldInputs.XYZ;
        

        /// <summary>
        /// ID de almacenamiento de <see cref="IsCompleted"/>
        /// </summary>
        private string IsCompletedString => StorageName + ".completed";
        
        /// <summary>
        /// ID de almacenamiento de <see cref="Time"/>
        /// </summary>
        private string TimeString => ScoreboardID + ".time";
        
        /// <summary>
        /// ID de almacenamiento de <see cref="Score"/>
        /// </summary>
        private string ScoreString => ScoreboardID + ".score";
        
        /// <summary>
        /// ID de almacenamiento de <see cref="NormalizedScore"/>
        /// </summary>
        private string NormalizedScoreString => ScoreboardID + ".normalizedScore";
        
        /// <summary>
        /// ID de almacenamiento de <see cref="Ratio"/>
        /// </summary>
        private string RatioString => ScoreboardID + ".ratio";

        /// <summary>
        /// Calcula la puntuación a partir del tiempo y exactitud y la almacena si <see cref="IsCompleted"/> es <b>false</b>
        /// </summary>
        /// <param name="time">Tiempo en el que se resolvió la escena</param>
        /// <param name="ratio">Exactitud de la red neural</param>
        /// <returns><b>true</b> si la puntuación fue almacenada</returns>
        /// <seealso cref="IsCompleted"/>
        public bool SubmitResult(float time, float ratio = 1f) {
            if (IsCompleted) return false;
            float normalizedScore = Mathf.Clamp01(minTime / Mathf.Max(time, Mathf.Epsilon)) * Mathf.Lerp(
                successThreshold, 1f, ratio);
            NormalizedScore = normalizedScore;
            Time = time;
            Ratio = ratio;
            Score = Mathf.FloorToInt(Mathf.Lerp(minMaxScores.x, minMaxScores.y, normalizedScore));
            OnUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Devuelve <b>true</b> si la escena ha sido completada
        /// </summary>
        public bool IsCompleted {
            get => PlayerPrefs.GetInt(IsCompletedString, 0) > 0;
            set {
                PlayerPrefs.SetInt(IsCompletedString, value ? 1 : 0);
                OnUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Tiempo en el que fue completada la escena por primera vez
        /// </summary>
        /// <remarks>Devuelve 0 si la escena no ha sido completada</remarks>
        public float Time {
            get => PlayerPrefs.GetFloat(TimeString, 0f);
            private set => PlayerPrefs.SetFloat(TimeString, value);
        }

        /// <summary>
        /// Puntuación calculada de la escena
        /// </summary>
        /// <remarks>Devuelve 0 si la escena no ha sido completada</remarks>
        public int Score {
            get => PlayerPrefs.GetInt(ScoreString, 0);
            private set => PlayerPrefs.SetInt(ScoreString, value);
        }

        /// <summary>
        /// Exactitud de la red con la que se completó la escena 
        /// </summary>
        /// <remarks>Devuelve 0 si la escena no ha sido completada</remarks>
        public float Ratio {
            get => PlayerPrefs.GetFloat(RatioString, 0f);
            private set => PlayerPrefs.SetFloat(RatioString, value);
        }

        /// <summary>
        /// Devuelve los segundos en formato de reloj (mm:ss) 
        /// </summary>
        /// <param name="time">Tiempo en segundos que se desea formatear</param>
        /// <returns>Segundos formateados en (mm:ss)</returns>
        public static string FormatTime(float time) {
            return System.TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
        }

        /// <summary>
        /// Devuelve el tiempo en el que se completó la escena en formato de reloj (mm:ss)
        /// </summary>
        /// <seealso cref="Time"/>
        public string FormattedTime => FormatTime(Time);

        
        /// <summary>
        /// Formatea una exactitud a porcentaje
        /// </summary>
        /// <param name="ratio">La exactitud que se desea formatear</param>
        /// <returns>Exactitud en formato de porcentaje</returns>
        public static string FormatRatio(float ratio) {
            return Mathf.RoundToInt(ratio * 100f) + "%";
        }
        
        /// <summary>
        /// Devuelve la exactitud en de la red con la se que completó la escena en formato porcentaje
        /// </summary>
        /// <seealso cref="Ratio"/>
        public string FormattedRatio => FormatRatio((Ratio));

        /// <summary>
        /// La puntuación normalizada [0, 1] con la que se completó la escena
        /// </summary>
        /// <seealso cref="Score"/>
        public float NormalizedScore {
            get => PlayerPrefs.GetFloat(NormalizedScoreString, 0f);
            private set => PlayerPrefs.SetFloat(NormalizedScoreString, value);
        }
        
        /// <summary>
        /// Función auxiliar para obtener la plantilla de neurona deseada
        /// </summary>
        /// <param name="target">ID de la neurona deseada</param>
        /// <returns>La plantilla de la neurona deseada</returns>
        /// <seealso cref="Neuralizer.TargetNeuron"/>
        /// <seealso cref="Neuralizer.Data.NeuralScene.NeuronPrefab"/>
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

        /// <summary>
        /// Plantilla de una neurona
        /// </summary>
        /// <seealso cref="Neuron"/>
        [System.Serializable]
        public class NeuronPrefab {
            /// <summary>
            /// Peso de la primer entrada
            /// </summary>
            [Range(-50f, 50f)] public float weightX = 0f;
            /// <summary>
            /// Peso de la segunda entrada
            /// </summary>
            [Range(-50f, 50f)] public float weightY = 0f;
            /// <summary>
            /// Peso de la tercer entrada
            /// </summary>
            [Range(-50f, 50f)] public float weightZ = 0f;
            /// <summary>
            /// Peso de la cuarta entrada
            /// </summary>
            [Range(-50f, 50f)] public float weightW = 0f;
            /// <summary>
            /// Sesgo de la neurona 
            /// </summary>
            [Range(-50f, 50f)] public float bias = 0f;
            /// <summary>
            /// ID de la función de activación
            /// </summary>
            public ActivationFunction activationFunction;
            /// <summary>
            /// Define si esta neurona será utilizada y mostrada en la red
            /// </summary>
            public bool displayNeuron;

            /// <summary>
            /// Aplica esta plantilla a la neurona especificada
            /// </summary>
            /// <param name="neuron">La neurona a la que deseamos aplicar la plantilla</param>
            public void ApplyTo(Neuron neuron) {
                neuron.weights.Set(weightX, weightY, weightZ, weightW);
                neuron.bias = bias;
                neuron.activationFunction = activationFunction;
                neuron.enabled = displayNeuron;
            }
        }

        /// <summary>
        /// Definicion de las entradas que permitirá la red
        /// </summary>
        public enum WorldInputs 
        {
            [InspectorName("2D XY")]
            XY = 2,
            [InspectorName("3D XYZ")]
            XYZ = 3
        }
    }
}