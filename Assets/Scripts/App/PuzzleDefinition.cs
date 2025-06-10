using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ejercicio", menuName = "Neuralizer/Ejercicio")]
public class PuzzleDefinition : ScriptableObject {

    public List<TargetPoint> points;
    public bool is3D;

}