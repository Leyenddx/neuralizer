using UnityEngine;

namespace Neuralizer {
    
    /// <summary>
    /// Dato a evaluar en la red neuronal
    /// </summary>
    [System.Serializable]
    public class TargetPoint {
        /// <summary>
        /// La posición o input a evaluar
        /// </summary>
        public Vector3 input;
        /// <summary>
        /// ¿Es este dato algo que queremos clasificar o descartar?
        /// </summary>
        public bool pointOfInterest;
    }
}