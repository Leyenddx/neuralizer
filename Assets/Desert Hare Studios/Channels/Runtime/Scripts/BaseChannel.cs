using UnityEngine;
using UnityEngine.Events;

public class BaseChannel<T> : ScriptableObject {

    private event UnityAction<T> OnTrigger;
    private T lastValue;

    public void FireEvent(T value) {
        lastValue = value;
        OnTrigger?.Invoke(value);
    }

    public void AddListener(UnityAction<T> action) {
        if(action != null) {
            OnTrigger += action;
            if(lastValue != null) {
                action.Invoke(lastValue);
            }
        }
    }

    public void RemoveListener(UnityAction<T> action) {
        if(action != null) {
            OnTrigger -= action;
        }
    }
}
