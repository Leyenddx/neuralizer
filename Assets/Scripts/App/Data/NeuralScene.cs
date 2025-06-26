using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Neural Scene", menuName = "Neuralizer")]
    public class NeuralScene : ScriptableObject {

        public Vector3[] targetPoints;
        public Vector3[] unwantedPoints;
        public NetworkArchitecture networkArchitecture;

    }
}