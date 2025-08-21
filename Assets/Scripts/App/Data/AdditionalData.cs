using UnityEngine;

namespace Neuralizer.Data {
    
    /// <summary>
    /// Clase que almacena datos adicionales externos a la aplicación
    /// </summary>
    [CreateAssetMenu(fileName = "Data", menuName = "Neuralizer/Data")]
    public class AdditionalData : ScriptableObject {
        
        /// <summary>
        /// Dirección del documento de Términos y Condiciones
        /// </summary>
        [SerializeField] private string termsUrl;
        /// <summary>
        /// Dirección del documento de Política de Privacidad
        /// </summary>
        [SerializeField] private string privacyUrl;
        /// <summary>
        /// Dirección del archivo del mundo de Minecraft Bedrock
        /// </summary>
        [SerializeField] private string downloadUrl;

        /// <summary>
        /// Abre el documento de Términos y Condiciones
        /// </summary>
        public void OpenTerms() {
            Application.OpenURL(termsUrl);
        }
        
        /// <summary>
        /// Abre el documento de Política de Privacidad
        /// </summary>
        public void OpenPrivacy() {
            Application.OpenURL(privacyUrl);
        }
        
        /// <summary>
        /// Abre la dirección de descarga del mundo de Minecraft Bedrock
        /// </summary>
        public void OpenDownloadUrl() {
            Application.OpenURL(downloadUrl);
        }

    }
}