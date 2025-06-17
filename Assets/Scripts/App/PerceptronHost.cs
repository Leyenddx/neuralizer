using UnityEngine;
using UnityEngine.Events;

public class PerceptronHost : MonoBehaviour {
    private static readonly int WeightsID = Shader.PropertyToID("_Weights");
    private static readonly int FunctionParamsID = Shader.PropertyToID("_FunctionParams");

    public enum ActivationFunction {
        Stepped = 0,
        ReLu = 1,
        Sigmoid = 2,
        TanH = 3
    }

    public float EvaluateActivationFunction(ActivationFunction function, float value) {
        return function switch {
            ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-value)),
            ActivationFunction.TanH => (float)System.Math.Tanh(value),
            ActivationFunction.ReLu => Mathf.Max(0f, value + ReLuOffset),
            ActivationFunction.Stepped => value >= StepThreshold ? StepMax : StepMin,
            _ => value
        };
    }

    public static PerceptronHost Instance { get; private set; }

    [SerializeField] private UnityEvent<bool> OnStepped;
    [SerializeField] private UnityEvent<bool> OnReLu;
    [SerializeField] private UnityEvent<bool> OnSigmoid;
    [SerializeField] private UnityEvent<bool> OnTanH;

    public float WeightX { get; set; } = 0f;

    public float WeightY { get; set; } = 0f;

    public float WeightZ { get; set; } = 0f;

    public float Bias { get; set; } = 0f;
    
    public Vector4 Weights => new(WeightX, WeightY, WeightZ, Bias);

    public ActivationFunction CurrentActivationFunction { get; private set; }

    public float StepMin { get; set; } = 0f;

    public float StepMax { get; set; } = 1f;

    public float StepThreshold { get; set; } = 0f;

    public float ReLuOffset { get; set; } = 0f;
    
    public Vector4 FunctionParams => new(StepMin, StepMax, StepThreshold, ReLuOffset);

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetStepped(true);
        NotifyChange();
    }

    public void SetStepped(bool value) {
        if (!value) return;
        SetFunctionIndex((int)ActivationFunction.Stepped);
    }

    public void SetReLu(bool value) {
        if (!value) return;
        SetFunctionIndex((int)ActivationFunction.ReLu);
    }

    public void SetSigmoid(bool value) {
        if (!value) return;
        SetFunctionIndex((int)ActivationFunction.Sigmoid);
    }

    public void SetTanH(bool value) {
        if (!value) return;
        SetFunctionIndex((int)ActivationFunction.TanH);
    }

    public void SetFunctionIndex(int index) {
        CurrentActivationFunction = (ActivationFunction)index;
        OnStepped?.Invoke(CurrentActivationFunction == ActivationFunction.Stepped);
        OnReLu?.Invoke(CurrentActivationFunction == ActivationFunction.ReLu);
        OnSigmoid?.Invoke(CurrentActivationFunction == ActivationFunction.Sigmoid);
        OnTanH?.Invoke(CurrentActivationFunction == ActivationFunction.TanH);
        NotifyChange();
    }

    public void NotifyChange() {
        Shader.SetGlobalVector(WeightsID, Weights);
        Shader.SetGlobalVector(FunctionParamsID, FunctionParams);
        switch (CurrentActivationFunction) {
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
        SamplePoint.UpdatePoints();
    }

    public float EvaluateAt(Vector3 point) {
        return EvaluateActivationFunction(CurrentActivationFunction, Vector3.Dot(point, Weights) + Bias);
    }
}