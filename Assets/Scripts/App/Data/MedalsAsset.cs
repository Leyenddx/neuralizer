using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "MedalsDB", menuName = "Neuralizer/Medals DB")]
    public class MedalsAsset : ScriptableObject {

        [SerializeField] private List<Sprite> medals;

        public Sprite GetFromRatio(float ratio) {
            try {
                return medals[Mathf.FloorToInt(ratio * (float)medals.Count) - 1];
            }
            catch (Exception e) {
                return null;
            }
        }

    }
}