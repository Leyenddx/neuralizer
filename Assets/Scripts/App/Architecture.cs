namespace Neuralizer {
    
    /// <summary>
    /// Arquitectura base de la red
    /// </summary>
    public enum Architecture {
        /// <summary>
        /// Arquitectura de perceptrón; solamente una neurona será evaluada 
        /// </summary>
        Perceptron = 0,
        /// <summary>
        /// Arquitectura de una capa; evalua una capa de máximo 4 neuronas antes de la neurona de salida
        /// </summary>
        OneLayer = 1,
        /// <summary>
        /// Arquitectura de dos capas; evalua dos capas de máximo 4 neuroans cada una antes de la neurona de salida
        /// </summary>
        TwoLayers = 2
    }
    
}