using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "State", menuName = "FloatingStates/State")]
public class FloatingState : ScriptableObject {
    public static FloatingState currentState { get; protected set; }

    [SerializeField] protected FloatingState parent;
    public event UnityAction<bool> OnStateActiveChanged;
    public event UnityAction OnStateEnter;
    public event UnityAction OnStateExit;

    private static FloatingState FindCommonAncestor(FloatingState a, FloatingState b) {
        if (!a) return null;
        return IsAncestor(b, a) ? a : FindCommonAncestor(a.parent, b);
    }

    private static bool IsAncestor(FloatingState state, FloatingState potentialAncestor) {
        if (!state) return false;
        return state == potentialAncestor || IsAncestor(state.parent, potentialAncestor);
    }

    private static void ExitStates(FloatingState state, FloatingState stopAt) {
        if (!state || state == stopAt) return;
        state.OnStateActiveChanged?.Invoke(false);
        state.OnStateExit?.Invoke();
        ExitStates(state.parent, stopAt);
    }

    private static void EnterStates(FloatingState state, FloatingState stopAt) {
        if (!state || state == stopAt) return;
        EnterStates(state.parent, stopAt);
        state.OnStateActiveChanged?.Invoke(true);
        state.OnStateEnter?.Invoke();
    }

    public void EnterState() {
        if (currentState == this) return;
        FloatingState commonAncestor = FindCommonAncestor(currentState, this);
        ExitStates(currentState, commonAncestor);
        EnterStates(this, commonAncestor);
        currentState = this;
    }
}