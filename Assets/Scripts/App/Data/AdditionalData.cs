using UnityEngine;

namespace Neuralizer.Data {
    [CreateAssetMenu(fileName = "Data", menuName = "Neuralizer/Data")]
    public class AdditionalData : ScriptableObject {
        
        [SerializeField] private string termsUrl;
        [SerializeField] private string privacyUrl;
        [SerializeField] private string downloadUrl;

        public void OpenTerms() {
            Application.OpenURL(termsUrl);
        }
        
        public void OpenPrivacy() {
            Application.OpenURL(privacyUrl);
        }
        
        public void OpenDownloadUrl() {
            Application.OpenURL(downloadUrl);
        }

    }
}