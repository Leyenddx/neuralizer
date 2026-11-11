using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {

    /// <summary>
    /// Base compartida para cualquier ejercicio evaluable de la app (redes neuronales, lógica difusa,
    /// futuros módulos). Contiene únicamente lo que es común a todos: nombre, puntuación, tiempo y medallas.
    /// </summary>
    /// <remarks>
    /// Esta clase se extrajo de <see cref="NeuralScene"/> para poder reutilizar la lógica de puntuación
    /// en <see cref="FuzzyScene"/> sin duplicarla. <see cref="NeuralScene"/> todavía NO hereda de esta
    /// clase (se dejó intacta a propósito para no arriesgar al Módulo 1 con un cambio sin poder probarlo
    /// en el momento); es un refactor pendiente y seguro de hacer más adelante, cuando se pueda validar
    /// en el Editor con calma.
    /// </remarks>
    public abstract class ScorableExercise : ScriptableObject {

        /// <summary>
        /// Nombre que se mostrará en la interfaz
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
        /// Porcentaje de exactitud mínimo que se requiere para considerar el ejercicio como resuelto
        /// </summary>
        [Range(0.5f, 1f)]
        public float successThreshold = 0.95f;

        /// <summary>
        /// Evento que se dispara cuando hay una actualización en la información almacenada
        /// </summary>
        public event UnityAction OnUpdated;

        /// <summary>
        /// Nombre que se mostrará en la interfaz
        /// </summary>
        /// <seealso cref="displayName"/>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>
        /// Describe si el ejercicio debe calcular puntuación y almacenarla
        /// </summary>
        /// <seealso cref="scoreboardID"/>
        public bool AllowScoring => !string.IsNullOrEmpty(scoreboardID);

        private string StorageName => string.IsNullOrEmpty(storageName) ? name : storageName;

        private string ScoreboardID => string.IsNullOrEmpty(scoreboardID) ? "" : scoreboardID;

        private string IsCompletedString => StorageName + ".completed";
        private string TimeString => ScoreboardID + ".time";
        private string ScoreString => ScoreboardID + ".score";
        private string NormalizedScoreString => ScoreboardID + ".normalizedScore";
        private string RatioString => ScoreboardID + ".ratio";

        /// <summary>
        /// Calcula la puntuación a partir del tiempo y exactitud y la almacena si <see cref="IsCompleted"/> es <b>false</b>
        /// </summary>
        /// <param name="time">Tiempo en el que se resolvió el ejercicio</param>
        /// <param name="ratio">Exactitud con la que se resolvió [0, 1]</param>
        /// <returns><b>true</b> si la puntuación fue almacenada</returns>
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
        /// Devuelve <b>true</b> si el ejercicio ha sido completado
        /// </summary>
        public bool IsCompleted {
            get => PlayerPrefs.GetInt(IsCompletedString, 0) > 0;
            set {
                PlayerPrefs.SetInt(IsCompletedString, value ? 1 : 0);
                OnUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Tiempo en el que fue completado el ejercicio por primera vez
        /// </summary>
        public float Time {
            get => PlayerPrefs.GetFloat(TimeString, 0f);
            private set => PlayerPrefs.SetFloat(TimeString, value);
        }

        /// <summary>
        /// Puntuación calculada del ejercicio
        /// </summary>
        public int Score {
            get => PlayerPrefs.GetInt(ScoreString, 0);
            private set => PlayerPrefs.SetInt(ScoreString, value);
        }

        /// <summary>
        /// Exactitud con la que se completó el ejercicio
        /// </summary>
        public float Ratio {
            get => PlayerPrefs.GetFloat(RatioString, 0f);
            private set => PlayerPrefs.SetFloat(RatioString, value);
        }

        /// <summary>
        /// La puntuación normalizada [0, 1] con la que se completó el ejercicio
        /// </summary>
        public float NormalizedScore {
            get => PlayerPrefs.GetFloat(NormalizedScoreString, 0f);
            private set => PlayerPrefs.SetFloat(NormalizedScoreString, value);
        }

        /// <summary>
        /// Devuelve los segundos en formato de reloj (mm:ss)
        /// </summary>
        public static string FormatTime(float time) {
            return System.TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
        }

        /// <summary>
        /// Devuelve el tiempo en el que se completó el ejercicio en formato de reloj (mm:ss)
        /// </summary>
        public string FormattedTime => FormatTime(Time);

        /// <summary>
        /// Formatea una exactitud a porcentaje
        /// </summary>
        public static string FormatRatio(float ratio) {
            return Mathf.RoundToInt(ratio * 100f) + "%";
        }

        /// <summary>
        /// Devuelve la exactitud con la que se completó el ejercicio en formato porcentaje
        /// </summary>
        public string FormattedRatio => FormatRatio(Ratio);
    }
}
