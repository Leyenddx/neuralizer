using UnityEngine;

[System.Serializable]
public class Perceptron {

    public float weightX = 0f;
    public float weightY = 0f;
    public float weightZ = 0f;
    public float bias = 0f;

    public Vector3 Weights => new Vector3(weightX, weightY, weightZ);
    
    public Vector4 Info => new Vector4(weightX, weightY, weightZ, bias);

    public float Sample(Vector3 value) {
        return Vector3.Dot(value, Weights) + bias;
    }

}