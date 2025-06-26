using UnityEngine;

namespace Neuralizer {
    public enum NetworkArchitecture {
        [InspectorName("Perceptron (I>1)")] Perceptron = 1,
        [InspectorName("Perceptron (I>2>1)")] Two = 2,
        [InspectorName("Perceptron (I>3>1)")] Three = 3,
        [InspectorName("Perceptron (I>2>2>1)")] TwoTwo = 4,
        [InspectorName("Perceptron (I>3>3>1)")] ThreeThree = 6,
        [InspectorName("Perceptron (I>2>2>2>1)")] TwoTwoTwo = 8,
        [InspectorName("Perceptron (I>3>3>3>1)")] ThreeThreeThree = 9
    }
}