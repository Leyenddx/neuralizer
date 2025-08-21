using System.Collections.Generic;
using UnityEngine;

namespace Neuralizer.Data {
    
    /// <summary>
    /// Contenedor de información de las medallas
    /// </summary>
    [CreateAssetMenu(fileName = "MedalsDB", menuName = "Neuralizer/Medals DB")]
    public class MedalsAsset : ScriptableObject {

        /// <summary>
        /// Lista de medallas disponibles, ordenadas de menor a mayor
        /// </summary>
        [SerializeField] private List<Medal> medals;

        /// <summary>
        /// Obtiene la medalla correspondiente a partir de un valor normalizado [0, 1]
        /// </summary>
        /// <param name="ratio">Puntuación normalizada [0, 1]</param>
        /// <returns>La medalla correspondiente</returns>
        public Medal GetFromRatio(float ratio) {
            try {
                return medals[Mathf.RoundToInt(ratio * ((float)medals.Count - 1f))];
            }
            catch {
                return null;
            }
        }

    }

    /// <summary>
    /// Descripción de la medalla
    /// </summary>
    [System.Serializable]
    public class Medal 
    {
        /// <summary>
        /// Nombre de la medalla
        /// </summary>
        public string name;
        /// <summary>
        /// Representación visual de la medalla
        /// </summary>
        public Sprite sprite;
        /// <summary>
        /// Descripción de la medalla
        /// </summary>
        public string description;
    }
}