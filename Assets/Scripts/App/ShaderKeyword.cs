using UnityEngine;
using UnityEngine.Rendering;

namespace Neuralizer {
    public class ShaderKeyword {

        private string name;
        
        public ShaderKeyword(string name) {
            this.name = name;
        }

        public bool Enabled {
            get => Shader.IsKeywordEnabled(new(name));
            set {
                if (value) {
                    Shader.EnableKeyword(name);
                }
                else {
                    Shader.DisableKeyword(name);
                }
            }
        }

        public void SetMaterial(Material target, bool value) {
            if (value) {
                target.EnableKeyword(name);
            }
            else {
                target.DisableKeyword(name);
            }
        }

        public string Name => name;
    }
}