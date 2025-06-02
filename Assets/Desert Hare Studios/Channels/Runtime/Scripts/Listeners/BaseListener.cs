using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class BaseChannelListener<T, C> : MonoBehaviour where C : BaseChannel<T> {

    [SerializeField] private C channel;

    [SerializeField] private UnityEvent<T> OnTrigger;

    private void OnEnable() {
        if(!channel) {
            return;
        }
        channel.AddListener(OnTrigger.Invoke);
    }

    private void OnDisable() {
        if(!channel) {
            return;
        }
        channel.RemoveListener(OnTrigger.Invoke);
    }

}