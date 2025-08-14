using System;
using System.Collections.Generic;
using Neuralizer.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    public class ScoreboardController : MonoBehaviour {

        [SerializeField] private List<NeuralScene> scenes;

        [SerializeField] private UnityEvent<string> OnTotalScoreString;
        
        private void OnEnable() {
            foreach (NeuralScene scene in scenes) {
                scene.OnUpdated += OnSceneUpdated;
            }
            OnSceneUpdated();
        }
        private void OnDisable() {
            foreach (NeuralScene scene in scenes) {
                scene.OnUpdated -= OnSceneUpdated;
            }
        }

        private void OnSceneUpdated() {
            int score = 0;
            foreach (NeuralScene scene in scenes) {
                score += scene.Score;
            }
            OnTotalScoreString?.Invoke(score.ToString());
        }
    }
}