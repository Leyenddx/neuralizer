using System.Collections.Generic;
using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "MedalsDB", menuName = "Neuralizer/Medals DB")]
    public class MedalsAsset : ScriptableObject {

        [SerializeField] private List<Medal> medals;

        public Medal GetFromRatio(float ratio) {
            try {
                return medals[Mathf.FloorToInt(ratio * (float)medals.Count) - 1];
            }
            catch {
                return null;
            }
        }

    }

    [System.Serializable]
    public class Medal 
    {
        public string name;
        public Sprite sprite;
        public string description;
    }
}