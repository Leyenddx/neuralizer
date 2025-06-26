using System.Collections.Generic;
using UnityEngine;

namespace Neuralizer {
    [CreateAssetMenu(fileName = "Ejercicio", menuName = "Neuralizer/Ejercicio")]
    public class PuzzleDefinition : ScriptableObject {
        public List<TargetPoint> points;
        public bool is3D;
    }
}