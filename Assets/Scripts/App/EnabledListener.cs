using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    /// <summary>
    /// Componente auxiliar para vincular los eventos OnEnable y OnDisable de forma arbitraria desd el editor Unity
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
    /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
    [ExecuteInEditMode]
    public class EnabledListener : MonoBehaviour {
        /// <summary>
        /// Evento que indica si el objeto está habilitado cada vez que esa propiedad ha cambiado
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnEnabledChanged;

        /// <summary>
        /// Evento que indica si el objeto está deshabilitado cada vez que esa propiedad ha cambiado
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnDisabledChanged;

        /// <summary>
        /// Evento a llamar cuando este objeto se habilite
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
        [SerializeField] private UnityEvent OnEnabled;

        /// <summary>
        /// Evento a llamar cuando este objeto se deshabilite
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        [SerializeField] private UnityEvent OnDisabled;

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
        private void OnEnable() {
            OnEnabledChanged?.Invoke(true);
            OnDisabledChanged?.Invoke(false);
            OnEnabled?.Invoke();
        }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        private void OnDisable() {
            OnEnabledChanged?.Invoke(false);
            OnDisabledChanged?.Invoke(true);
            OnDisabled?.Invoke();
        }
    }
}