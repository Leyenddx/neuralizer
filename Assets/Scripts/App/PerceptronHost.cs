using UnityEngine;
using UnityEngine.Events;

public class PerceptronHost : MonoBehaviour
{
    private static readonly int WeightsID = Shader.PropertyToID("_Weights");

    public enum ActivationFunction {
        Lineal = 0,
        Sigmoid = 1,
        TanH = 2,
        ReLu = 3,
        Stepped = 4
    }

    public static float EvaluateActivationFunction(ActivationFunction function, float value) {
        return function switch {
            ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-value)),
            ActivationFunction.TanH => (float)System.Math.Tanh(value),
            ActivationFunction.ReLu => Mathf.Max(0f, value),
            ActivationFunction.Stepped => value > 0f ? 1f : 0f,
            _ => value
        };
    }

    public static PerceptronHost Instance { get; private set; }

    public float WeightX { get; set; } = 0f;
    
    public float WeightY { get; set; } = 0f;
    
    public float WeightZ { get; set; } = 0f;

    public Vector3 Weights => new(WeightX, WeightY, WeightZ);

    public ActivationFunction CurrentActivationFunction { get; private set; }

    public UnityAction OnPerceptronChanged;
    
    private void Awake() {
        Instance = this;
    }

    private void Start() {
        OnPerceptronChanged?.Invoke();
    }

    public void SetLineal(bool value) {
        if (!value) return;
        CurrentActivationFunction = ActivationFunction.Lineal;
        NotifyChange();
    }

    public void SetSigmoid(bool value) {
        if (!value) return;
        CurrentActivationFunction = ActivationFunction.Sigmoid;
        NotifyChange();
    }

    public void SetTanH(bool value) {
        if (!value) return;
        CurrentActivationFunction = ActivationFunction.TanH;
        NotifyChange();
    }

    public void SetReLu(bool value) {
        if (!value) return;
        CurrentActivationFunction = ActivationFunction.ReLu;
        NotifyChange();
    }

    public void SetStepped(bool value) {
        if (!value) return;
        CurrentActivationFunction = ActivationFunction.Stepped;
        NotifyChange();
    }

    public void NotifyChange() {
        OnPerceptronChanged?.Invoke();
        Shader.SetGlobalVector(WeightsID, Weights);
        switch(CurrentActivationFunction) {
            default:
            case ActivationFunction.Lineal:
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                Shader.EnableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                break;
            case ActivationFunction.Sigmoid:
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                Shader.EnableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                break;
            case ActivationFunction.TanH:
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                Shader.EnableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
            case ActivationFunction.ReLu:
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                Shader.EnableKeyword("_ACTIVATIONFUNCTION_RELU");
                break;
            case ActivationFunction.Stepped:
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                Shader.EnableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                break;
        }
    }

    public float EvaluateAt(Vector3 point) {
        return EvaluateActivationFunction(CurrentActivationFunction, Vector3.Dot(point, Weights));
    }

}
