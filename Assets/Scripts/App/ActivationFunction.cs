namespace Neuralizer {

    /// <summary>
    /// Función de activación de una neurona
    /// </summary>
    public enum ActivationFunction {
        /// <summary>
        /// Función lineal, no modifica la salida de la neurona
        /// </summary>
        Linear = -1,
        /// <summary>
        /// FUnción escalonada, devuelve 1 i el valor es positivo, 0 si no lo es
        /// </summary>
        Stepped = 0,
        /// <summary>
        /// Función lineal rectificada, Devuelve 0 si el valor es negativo, devuelve el valor lineal si es positivo
        /// </summary>
        ReLu = 1,
        /// <summary>
        /// Función sigmoide, devuelve un valor entre [0, 1]
        /// </summary>
        /// <seealso href="https://es.wikipedia.org/wiki/Funci%C3%B3n_sigmoide"/>
        Sigmoid = 2,
        /// <summary>
        /// Función tangente hiperbólica, devuelve un valor entre [-1, 1]
        /// </summary>
        /// <seealso href="https://es.wikipedia.org/wiki/Tangente_hiperb%C3%B3lica"/>
        TanH = 3,
        /// <summary>
        /// Función de pertenencia trapezoidal/triangular (ver <see cref="Neuralizer.Fuzzy.MembershipFunction"/>
        /// para la misma fórmula del lado CPU) — usa <see cref="Neuron.shape"/> como (a, b, c, d) en vez
        /// de <see cref="Neuron.weights"/>/<see cref="Neuron.bias"/> para la forma.
        /// </summary>
        Trapezoidal = 4,
        /// <summary>
        /// Campana gaussiana — usa <see cref="Neuron.shape"/> como (centro, ancho, _, _).
        /// </summary>
        Gaussiana = 5
    }

}