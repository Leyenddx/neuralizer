using UnityEngine;
using UnityEngine.Rendering;

namespace Neuralizer {
    
    /// <summary>
    /// Clase auxiliar para la configuración de shaders y materiales
    /// </summary>
    public class ShaderKeyword {

        /// <summary>
        /// Nombre del keyword que se desea manipular
        /// </summary>
        private string name;
        
        /// <summary>
        /// Inicializa la clase y <see cref="name"/>
        /// </summary>
        /// <param name="name">Nombre del keyword que se desea manipular</param>
        public ShaderKeyword(string name) {
            this.name = name;
        }

        /// <summary>
        /// Activa o desactiva la keyword global de shader
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.IsKeywordEnabled.html"/>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.EnableKeyword.html"/>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.DisableKeyword.html"/>
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

        /// <summary>
        /// Activa o desactiva la keyword de shader del material especificado
        /// </summary>
        /// <param name="target">El material que deseamos manipular</param>
        /// <param name="value">¿Activar o desactivar la keyword?</param>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Material.EnableKeyword.html"/>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Material.DisableKeyword.html"/>
        public void SetMaterial(Material target, bool value) {
            if (value) {
                target.EnableKeyword(name);
            }
            else {
                target.DisableKeyword(name);
            }
        }

        /// <summary>
        /// Permite el acceso de lectura de <see cref="name"/>
        /// </summary>
        public string Name => name;
    }
}