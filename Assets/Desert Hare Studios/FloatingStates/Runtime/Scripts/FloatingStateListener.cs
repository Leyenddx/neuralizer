using UnityEngine;
using UnityEngine.Events;

public class FloatingStateListener : MonoBehaviour {

    [SerializeField] private FloatingStateWrapper[] listeners;
    [SerializeField] private FloatingState initialState;

    private void OnEnable() {
        foreach(FloatingStateWrapper listener in listeners) {
            listener.state.OnStateActiveChanged += listener.OnStateActiveChanged.Invoke;
            listener.state.OnStateEnter += listener.OnStateEnter.Invoke;
            listener.state.OnStateExit += listener.OnStateExit.Invoke;
        }
    }

    private void OnDisable() {
        foreach(FloatingStateWrapper listener in listeners) {
            listener.state.OnStateActiveChanged -= listener.OnStateActiveChanged.Invoke;
            listener.state.OnStateEnter -= listener.OnStateEnter.Invoke;
            listener.state.OnStateExit -= listener.OnStateExit.Invoke;
        }
    }

    private void Start() {
        if(initialState) {
            initialState.EnterState();
        }
    }

    [System.Serializable]
    public class FloatingStateWrapper {
        public FloatingState state;
        public UnityEvent<bool> OnStateActiveChanged;
        public UnityEvent OnStateEnter;
        public UnityEvent OnStateExit;
    }

}