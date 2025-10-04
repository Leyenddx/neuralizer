using System.Collections.Generic;
using Neuralizer.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    
    /// <summary>
    /// Componente para mostrar la puntuación final del usuario
    /// </summary>
    public class ScoreboardController : MonoBehaviour {

        /// <summary>
        /// Todas las escenas a calificar
        /// </summary>
        [SerializeField] private List<NeuralScene> scenes;

        /// <summary>
        /// Evento que índica en formato string la puntuación final del usuario
        /// </summary>
        [SerializeField] private UnityEvent<string> OnTotalScoreString;
        
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
        private void OnEnable() {
            foreach (NeuralScene scene in scenes) {
                scene.OnUpdated += OnSceneUpdated;
            }
            OnSceneUpdated();
        }
        
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        private void OnDisable() {
            foreach (NeuralScene scene in scenes) {
                scene.OnUpdated -= OnSceneUpdated;
            }
        }

        /// <summary>
        /// Función que se llama cada vez que una escena que se encuentra en la lista <see cref="scenes"/> ha sufrido un cambio 
        /// </summary>
        private void OnSceneUpdated() {
            int score = 0;
            foreach (NeuralScene scene in scenes) {
                score += scene.Score;
            }
            OnTotalScoreString?.Invoke(score.ToString());
        }
    }
}