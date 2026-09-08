using Neuralizer.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Neuralizer.NetworkRendering {
    
    /// <summary>
    /// Pase de renderizado que permite visualizar la red neural actual de forma deferida
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.2/api/UnityEngine.Rendering.Universal.ScriptableRenderPass.html"/>
    public class NeuralRenderPass : ScriptableRenderPass {
        /// <summary>
        /// Nombre del parametro de shader "_A"
        /// </summary>
        private const string A_Name = "_A";
        /// <summary>
        /// Nombre del parametro de shader"_B"
        /// </summary>
        private const string B_Name = "_B";
        /// <summary>
        /// Nombre del parametro de shader "_C"
        /// </summary>
        private const string C_Name = "_C";
        /// <summary>
        /// Nombre del parametro de shader "_D"
        /// </summary>
        private const string D_Name = "_D";
        /// <summary>
        /// Nombre del parametro de shader "_AA"
        /// </summary>
        private const string AA_Name = "_AA";
        /// <summary>
        /// Nombre del parametro de shader "_AB"
        /// </summary>
        private const string AB_Name = "_AB";
        /// <summary>
        /// Nombre del parametro de shader "_AC"
        /// </summary>
        private const string AC_Name = "_AC";
        /// <summary>
        /// Nombre del parametro de shader "_AD"
        /// </summary>
        private const string AD_Name = "_AD";
        /// <summary>
        /// Nombre del parametro de shader "_BA"
        /// </summary>
        private const string BA_Name = "_BA";
        /// <summary>
        /// Nombre del parametro de shader "_BB"
        /// </summary>
        private const string BB_Name = "_BB";
        /// <summary>
        /// Nombre del parametro de shader "_BC"
        /// </summary>
        private const string BC_Name = "_BC";
        /// <summary>
        /// Nombre del parametro de shader "_BD"
        /// </summary>
        private const string BD_Name = "_BD";
        /// <summary>
        /// Nombre del parametro de shader "_Output"
        /// </summary>
        private const string Output_Name = "_Output";
        /// <summary>
        /// Nombre del parametro de shader "_PaintOutput"
        /// </summary>
        private const string Paint_Name = "_PaintOutput";
        /// <summary>
        /// Nombre del parametro de shader "_IntermediateOutput"
        /// </summary>
        private const string Intermediate_Name = "_IntermediateOutput";
        /// <summary>
        /// Nombre del parametro de shader "_Weights"
        /// </summary>
        private const string Weights_Name = "_Weights";
        /// <summary>
        /// Nombre del parametro de shader "_Bias"
        /// </summary>
        private const string Bias_Name = "_Bias";

        /// <summary>
        /// ID único del parametro de shader <see cref="A_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int A_ID = Shader.PropertyToID(A_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="B_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int B_ID = Shader.PropertyToID(B_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="C_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int C_ID = Shader.PropertyToID(C_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="D_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int D_ID = Shader.PropertyToID(D_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="AA_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int AA_ID = Shader.PropertyToID(AA_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="AB_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int AB_ID = Shader.PropertyToID(AB_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="AC_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int AC_ID = Shader.PropertyToID(AC_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="AD_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int AD_ID = Shader.PropertyToID(AD_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="BA_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int BA_ID = Shader.PropertyToID(BA_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="BB_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int BB_ID = Shader.PropertyToID(BB_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="BC_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int BC_ID = Shader.PropertyToID(BC_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="BD_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int BD_ID = Shader.PropertyToID(BD_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="Output_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int Output_ID = Shader.PropertyToID(Output_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="Paint_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int Paint_ID = Shader.PropertyToID(Paint_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="Intermediate_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int Intermediate_ID = Shader.PropertyToID(Intermediate_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="Weights_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int Weights_ID = Shader.PropertyToID(Weights_Name);
        /// <summary>
        /// ID único del parametro de shader <see cref="Bias_Name"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html"/>
        private static readonly int Bias_ID = Shader.PropertyToID(Bias_Name);

        /// <summary>
        /// Keyword de shader que índica si se utilizará la función de activación linear
        /// </summary>
        private static readonly ShaderKeyword activationFunction_Linear = new("_ACTIVATIONFUNCTION_LINEAR");
        /// <summary>
        /// Keyword de shader que índica si se utilizará la función de activación escalonada
        /// </summary>
        private static readonly ShaderKeyword activationFunction_Stepped = new("_ACTIVATIONFUNCTION_STEPPED");
        /// <summary>
        /// Keyword de shader que índica si se utilizará la función de activación ReLu
        /// </summary>
        private static readonly ShaderKeyword activationFunction_ReLu = new("_ACTIVATIONFUNCTION_RELU");
        /// <summary>
        /// Keyword de shader que índica si se utilizará la función de activación sigmoide
        /// </summary>
        /// <seealso href="https://es.wikipedia.org/wiki/Funci%C3%B3n_sigmoide"/>
        private static readonly ShaderKeyword activationFunction_Sigmoid = new("_ACTIVATIONFUNCTION_SIGMOID");
        /// <summary>
        /// Keyword de shader que índica si se utilizará la función de activación TanH
        /// </summary>
        private static readonly ShaderKeyword activationFunction_TanH = new("_ACTIVATIONFUNCTION_TANH");

        /// <summary>
        /// Keyword de shader que índica si las entradas de la neurona son la posición del punto
        /// </summary>
        private static readonly ShaderKeyword inputSource_World = new("_INPUT_SOURCE_WORLD");
        /// <summary>
        /// Keyword de shader que índica si las entradas de la neurona dos neuronas de la capa anterior
        /// </summary>
        private static readonly ShaderKeyword inputSource_2Textures = new("_INPUT_SOURCE_2TEXTURES");
        /// <summary>
        /// Keyword de shader que índica si las entradas de la neurona cuatro neuronas de la capa anterior
        /// </summary>
        private static readonly ShaderKeyword inputSource_4Textures = new("_INPUT_SOURCE_4TEXTURES");

        /// <summary>
        /// Material para evaluar la neurona
        /// </summary>
        public Material neuronMaterial;
        /// <summary>
        /// Material que pinta el resultado de la  evaluación de la red en la pantalla del dispositivo
        /// </summary>
        public Material paintMaterial;
        /// <summary>
        /// Material que aplica un pequeño desenfoque para reducir el ruido en entrada
        /// </summary>
        public Material blurMaterial;

        /// <summary>
        /// Referencia a la primera neurona de la primera capa
        /// </summary>
        public Neuron AA;
        /// <summary>
        /// Referencia a la segunda neurona de la primera capa
        /// </summary>
        public Neuron AB;
        /// <summary>
        /// Referencia a la tercera neurona de la primera capa
        /// </summary>
        public Neuron AC;
        /// <summary>
        /// Referencia a la cuarta neurona de la primera capa
        /// </summary>
        public Neuron AD;
        /// <summary>
        /// Referencia a la primera neurona de la segunda capa
        /// </summary>
        public Neuron BA;
        /// <summary>
        /// Referencia a la segunda neurona de la segunda capa
        /// </summary>
        public Neuron BB;
        /// <summary>
        /// Referencia a la tercera neurona de la segunda capa
        /// </summary>
        public Neuron BC;
        /// <summary>
        /// Referencia a la cuarta neurona de la segunda capa
        /// </summary>
        public Neuron BD;
        /// <summary>
        /// Referencia a la neurona de salida
        /// </summary>
        public Neuron Output;
        /// <summary>
        /// Arquitectura de la red a evaluar
        /// </summary>
        public Architecture architecture;
        /// <summary>
        /// Índica si la red se evalua de forma plana u orbital
        /// </summary>
        /// <seealso cref="NeuralScene.useOrbitalCamera"/>
        public bool isPlanar = false;

        /// <summary>
        /// Definición de la textura que almacena el resultado de las neuronas
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor.html"/>
        private RenderTextureDescriptor neuronDescriptor;
        /// <summary>
        /// Definición de la textura que almacena el resultado de la evaluación completa
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor.html"/>
        private RenderTextureDescriptor paintDescriptor;

        /// <summary>
        /// Formatos de imágen permitidos en <see cref="neuronDescriptor"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Experimental.Rendering.GraphicsFormat.html"/>
        private static readonly GraphicsFormat[] NeuronOutputFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.R32G32B32A32_SFloat
        };

        /// <summary>
        /// Formatos de imágen permitidos en <see cref="paintDescriptor"/>
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Experimental.Rendering.GraphicsFormat.html"/>
        private static readonly GraphicsFormat[] CameraFormatList = {
            // GraphicsFormat.R16G16B16A16_SFloat,
            // GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        /// <summary>
        /// Información que necesita el pase de entrada
        /// </summary>
        /// <seealso cref="NeuralRenderPass.ExecuteInputPass"/>
        private class InputPassData {
            public TextureHandle output;
            public Neuron neuron;
        }

        /// <summary>
        /// Información que necesita el pase de capa
        /// </summary>
        /// <seealso cref="NeuralRenderPass.ExecuteLayeredPass"/>
        private class LayeredPassData : InputPassData {
            public TextureHandle inputA;
            public TextureHandle inputB;
            public TextureHandle inputC;
            public TextureHandle inputD;
        }

        /// <summary>
        /// Información que necesita el pase de dibujado final
        /// </summary>
        /// <seealso cref="NeuralRenderPass.ExecutePaintPass"/>
        private class PaintPassData {
            public TextureHandle cameraTex;
            public TextureHandle networkOutput;
            public TextureHandle paintOutput;
            public Material material;
        }

        /// <summary>
        /// Información que necesita el pase de desenfoque
        /// </summary>
        /// <seealso cref="NeuralRenderPass.ExecuteBlurPass"/>
        private class BlitPass {
            public TextureHandle source;
            public Material material;
        }

        /// <summary>
        /// Información que necesita el pase de copiado de textura
        /// </summary>
        /// <seealso cref="NeuralRenderPass.ExecuteCopyPass"/>
        private class CopyPassData {
            public TextureHandle source;
        }

        /// <summary>
        /// Inicializa el pase de renderizado
        /// </summary>
        public NeuralRenderPass() {
            ConfigureInput(ScriptableRenderPassInput.Color);
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        /// <summary>
        /// Inicializa los materiales y texturas que se utilizarán en el pase
        /// </summary>
        public void Initialize() {
            AA.PrepareForRenderPass(neuronMaterial);
            AB.PrepareForRenderPass(neuronMaterial);
            AC.PrepareForRenderPass(neuronMaterial);
            AD.PrepareForRenderPass(neuronMaterial);
            BA.PrepareForRenderPass(neuronMaterial);
            BB.PrepareForRenderPass(neuronMaterial);
            BC.PrepareForRenderPass(neuronMaterial);
            BD.PrepareForRenderPass(neuronMaterial);
            Output.PrepareForRenderPass(neuronMaterial);
        }

        /// <summary>
        /// Configura el material y shader con la información de la neurona especificada
        /// </summary>
        /// <param name="target">La neurona que queremos evaluar después</param>
        public static void SetShaderData(Neuron target) {
            target.material.SetVector(Weights_ID, target.weights);
            target.material.SetFloat(Bias_ID, target.bias);
            activationFunction_Linear.SetMaterial(target.material,
                target.activationFunction == ActivationFunction.Linear);
            activationFunction_Stepped.SetMaterial(target.material,
                target.activationFunction == ActivationFunction.Stepped);
            activationFunction_ReLu.SetMaterial(target.material, target.activationFunction == ActivationFunction.ReLu);
            activationFunction_Sigmoid.SetMaterial(target.material,
                target.activationFunction == ActivationFunction.Sigmoid);
            activationFunction_TanH.SetMaterial(target.material, target.activationFunction == ActivationFunction.TanH);
        }

        /// <summary>
        /// Evalua la neurona usando la posición del mundo y almacena los datos en <see cref="InputPassData.output"/>
        /// </summary>
        /// <param name="data">La información de entrada</param>
        /// <param name="context">El contexto del renderizador</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RasterGraphContext.html"/>
        static void ExecuteInputPass(InputPassData data, RasterGraphContext context) {
            inputSource_World.SetMaterial(data.neuron.material, true);
            inputSource_2Textures.SetMaterial(data.neuron.material, false);
            inputSource_4Textures.SetMaterial(data.neuron.material, false);
            SetShaderData(data.neuron);
            Blitter.BlitTexture(context.cmd, Vector2.one, data.neuron.material, 0);
        }

        /// <summary>
        /// Evalua la neurona usando la información de neuronas anteriores y almacena los datos en <see cref="InputPassData.output"/>
        /// </summary>
        /// <param name="data">La información de entrada</param>
        /// <param name="context">El contexto del renderizador</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RasterGraphContext.html"/>
        static void ExecuteLayeredPass(LayeredPassData data, RasterGraphContext context) {
            inputSource_World.SetMaterial(data.neuron.material, false);
            inputSource_2Textures.SetMaterial(data.neuron.material, false);
            inputSource_4Textures.SetMaterial(data.neuron.material, true);
            data.neuron.material.SetTexture(A_ID, data.inputA);
            data.neuron.material.SetTexture(B_ID, data.inputB);
            data.neuron.material.SetTexture(C_ID, data.inputC);
            data.neuron.material.SetTexture(D_ID, data.inputD);
            SetShaderData(data.neuron);
            Blitter.BlitTexture(context.cmd, Vector2.one, data.neuron.material, 0);
        }

        /// <summary>
        /// Pinta el resultado de la evaluación de la red en la pantalla del dispoitivo
        /// </summary>
        /// <param name="data">La información de entrada</param>
        /// <param name="context">El contexto del renderizador</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RasterGraphContext.html"/>
        static void ExecutePaintPass(PaintPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(Output_ID, data.networkOutput);
            Blitter.BlitTexture(context.cmd, data.cameraTex, Vector2.one, data.material, 0);
        }

        /// <summary>
        /// Aplica un pequeño desenfoque a la textura especificada en <see cref="BlitPass.source"/>
        /// </summary>
        /// <param name="data">La información de entrada</param>
        /// <param name="context">El contexto del renderizador</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RasterGraphContext.html"/>
        static void ExecuteBlurPass(BlitPass data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, 0);
        }

        /// <summary>
        /// Copia una textura
        /// </summary>
        /// <param name="data">La información de entrada</param>
        /// <param name="context">El contexto del renderizador</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RasterGraphContext.html"/>
        static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }

        /// <summary>
        /// Agrega un pase de evaluación de neurona de entrada a la gráfica de renderizado actual
        /// </summary>
        /// <param name="passName">Nombre del pase</param>
        /// <param name="renderGraph">Gráfica de renderizado actual</param>
        /// <param name="targetNeuron">Neurona a evaluar</param>
        /// <param name="OutputHandle">Textura que almacenará el resultado</param>
        /// <seealso cref="ExecuteInputPass"/>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html"/>
        private static void AddInputNeuronPass(string passName, RenderGraph renderGraph, Neuron targetNeuron,
            TextureHandle OutputHandle) {
            using IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<InputPassData>(passName, out var passData);
            passData.output = OutputHandle;
            passData.neuron = targetNeuron;
            builder.SetRenderAttachment(passData.output, 0);
            builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                ExecuteInputPass(data, context));
        }

        /// <summary>
        /// Agrega un pase de evaluación de neurona a la gráfica de renderizado actual
        /// </summary>
        /// <param name="passName">Nombre del pase</param>
        /// <param name="renderGraph">Gráfica de renderizado actual</param>
        /// <param name="targetNeuron">Neurona a evaluar</param>
        /// <param name="A">Textura con la información de la evaluación anterior que se ligará al primer peso de <see cref="targetNeuron"/></param>
        /// <param name="B">Textura con la información de la evaluación anterior que se ligará al segundo peso de <see cref="targetNeuron"/></param>
        /// <param name="C">Textura con la información de la evaluación anterior que se ligará al tercer peso de <see cref="targetNeuron"/></param>
        /// <param name="D">Textura con la información de la evaluación anterior que se ligará al cuarto peso de <see cref="targetNeuron"/></param>
        /// <param name="OutputHandle">Textura que almacenará el resultado</param>
        /// <seealso cref="ExecuteLayeredPass"/>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html"/>
        private static void AddLayeredNeuronPass(string passName, RenderGraph renderGraph, Neuron targetNeuron,
            TextureHandle A,
            TextureHandle B, TextureHandle C, TextureHandle D, TextureHandle OutputHandle) {
            using IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<LayeredPassData>(passName, out var passData);
            passData.inputA = A;
            passData.inputB = B;
            passData.inputC = C;
            passData.inputD = D;
            passData.neuron = targetNeuron;
            passData.output = OutputHandle;
            builder.UseTexture(passData.inputA);
            builder.UseTexture(passData.inputB);
            builder.UseTexture(passData.inputC);
            builder.UseTexture(passData.inputD);
            builder.SetRenderAttachment(passData.output, 0);
            builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                ExecuteLayeredPass(data, context));
        }

        /// <summary>
        /// Agrega un pase genérico a la gráfica de renderizado actual
        /// </summary>
        /// <param name="passName">Nombre del pase</param>
        /// <param name="renderGraph">Gráfica de renderizado actual</param>
        /// <param name="material">Material que se utilizará</param>
        /// <param name="from">Textura de entrada</param>
        /// <param name="to">Textura de salida</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html"/>
        private static void AddBlitPass(string passName, RenderGraph renderGraph, Material material, TextureHandle from, TextureHandle to) {
            using var builder =
                renderGraph.AddRasterRenderPass<BlitPass>(passName, out var passData);
            passData.material = material;
            passData.source = from;
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((BlitPass data, RasterGraphContext context) =>
                ExecuteBlurPass(data, context));
        }
        
        /// <summary>
        /// Agrega un pase a la gráfica de renderizado que copia una textura a otra
        /// </summary>
        /// <param name="passName">Nombre del pase</param>
        /// <param name="renderGraph">Gráfica de renderizado actual</param>
        /// <param name="from">Textura a copiar</param>
        /// <param name="to">Textura destino</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html"/>
        private static void AddCopyPass(string passName, RenderGraph renderGraph, TextureHandle from, TextureHandle to) {
            using var builder =
                renderGraph.AddRasterRenderPass<CopyPassData>(passName, out var passData);
            passData.source = from;
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                ExecuteCopyPass(data, context));
        }

        /// <summary>
        /// Comienza a agregar los pases necesarios a la gráfica de renderizado actual para evaluar la red neural actual
        /// </summary>
        /// <param name="renderGraph">Gráfica de renderizado actual</param>
        /// <param name="frameData">Información de renderizado proveniente de pases anteriores</param>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html"/>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.ContextContainer.html"/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            neuronDescriptor = cameraData.cameraTargetDescriptor;
            neuronDescriptor.msaaSamples = 1;
            neuronDescriptor.mipCount = 0;
            neuronDescriptor.sRGB = false;
            neuronDescriptor.depthStencilFormat = GraphicsFormat.None;
            neuronDescriptor.memoryless = RenderTextureMemoryless.None;
            neuronDescriptor.useMipMap = false;
            neuronDescriptor.autoGenerateMips = false;
            neuronDescriptor.bindMS = false;
            neuronDescriptor.useDynamicScale = false;
            neuronDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;

            switch (architecture) {
                case Architecture.Perceptron:
                    neuronDescriptor.width /= 2;
                    neuronDescriptor.height /= 2;
                break;
                case Architecture.OneLayer:
                    neuronDescriptor.width /= 3;
                    neuronDescriptor.height /= 3;
                break;
                case Architecture.TwoLayers:
                    neuronDescriptor.width /= 4;
                    neuronDescriptor.height /= 4;
                break;
            }

            if (!SystemInfo.IsFormatSupported(neuronDescriptor.graphicsFormat, GraphicsFormatUsage.Render)) {
                neuronDescriptor.graphicsFormat = GraphicsFormat.None;
                foreach (GraphicsFormat t in NeuronOutputFormatList) {
                    if (!SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) continue;
                    neuronDescriptor.graphicsFormat = t;
                    break;
                }
            }

            paintDescriptor = cameraData.cameraTargetDescriptor;
            paintDescriptor.msaaSamples = 1;
            paintDescriptor.mipCount = 0;
            // desc.sRGB = false;
            paintDescriptor.depthStencilFormat = GraphicsFormat.None;
            paintDescriptor.memoryless = RenderTextureMemoryless.None;
            paintDescriptor.useMipMap = false;
            paintDescriptor.autoGenerateMips = false;
            paintDescriptor.enableRandomWrite = false;
            paintDescriptor.bindMS = false;
            paintDescriptor.useDynamicScale = false;

            if (!SystemInfo.IsFormatSupported(neuronDescriptor.graphicsFormat, GraphicsFormatUsage.Render)) {
                neuronDescriptor.graphicsFormat = GraphicsFormat.None;

                foreach (GraphicsFormat t in CameraFormatList) {
                    if (!SystemInfo.IsFormatSupported(t, GraphicsFormatUsage.Render)) continue;
                    neuronDescriptor.graphicsFormat = t;
                    break;
                }
            }

            TextureHandle OutputHandle = Output.SetupRenderTarget(Output_Name, renderGraph, neuronDescriptor);

            var paintHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, paintDescriptor, Paint_Name,
                false,
                FilterMode.Bilinear);

            var intermediateHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, paintDescriptor,
                Intermediate_Name,
                false,
                FilterMode.Bilinear);

            switch (architecture) {
                case Architecture.Perceptron:
                    AddInputNeuronPass("Neuron Output", renderGraph, Output, OutputHandle);
                break;
                case Architecture.OneLayer:
                    TextureHandle AHandle = AA.SetupRenderTarget(AA_Name, renderGraph, neuronDescriptor);
                    TextureHandle BHandle = AB.SetupRenderTarget(AB_Name, renderGraph, neuronDescriptor);
                    TextureHandle CHandle = AC.SetupRenderTarget(AD_Name, renderGraph, neuronDescriptor);
                    TextureHandle DHandle = AD.SetupRenderTarget(AA_Name, renderGraph, neuronDescriptor);
                    AddInputNeuronPass("Neuron AA", renderGraph, AA, AHandle);
                    AddInputNeuronPass("Neuron AB", renderGraph, AB, BHandle);
                    AddInputNeuronPass("Neuron AC", renderGraph, AC, CHandle);
                    AddInputNeuronPass("Neuron AD", renderGraph, AD, DHandle);

                    AddLayeredNeuronPass("Neuron Output", renderGraph, Output, AHandle, BHandle, CHandle, DHandle,
                        OutputHandle);
                break;
                case Architecture.TwoLayers:
                    TextureHandle AAHandle = AA.SetupRenderTarget(AA_Name, renderGraph, neuronDescriptor);
                    TextureHandle ABHandle = AB.SetupRenderTarget(AB_Name, renderGraph, neuronDescriptor);
                    TextureHandle ACHandle = AC.SetupRenderTarget(AD_Name, renderGraph, neuronDescriptor);
                    TextureHandle ADHandle = AD.SetupRenderTarget(AA_Name, renderGraph, neuronDescriptor);
                    TextureHandle BAHandle = BA.SetupRenderTarget(BA_Name, renderGraph, neuronDescriptor);
                    TextureHandle BBHandle = BB.SetupRenderTarget(BB_Name, renderGraph, neuronDescriptor);
                    TextureHandle BCHandle = BC.SetupRenderTarget(BD_Name, renderGraph, neuronDescriptor);
                    TextureHandle BDHandle = BD.SetupRenderTarget(BA_Name, renderGraph, neuronDescriptor);

                    AddInputNeuronPass("Neuron AA", renderGraph, AA, AAHandle);
                    AddInputNeuronPass("Neuron AB", renderGraph, AB, ABHandle);
                    AddInputNeuronPass("Neuron AC", renderGraph, AC, ACHandle);
                    AddInputNeuronPass("Neuron AD", renderGraph, AD, ADHandle);
                    AddLayeredNeuronPass("Neuron BA", renderGraph, BA, AAHandle, ABHandle, ACHandle, ADHandle,
                        BAHandle);
                    AddLayeredNeuronPass("Neuron BB", renderGraph, BB, AAHandle, ABHandle, ACHandle, ADHandle,
                        BBHandle);
                    AddLayeredNeuronPass("Neuron BC", renderGraph, BC, AAHandle, ABHandle, ACHandle, ADHandle,
                        BCHandle);
                    AddLayeredNeuronPass("Neuron BD", renderGraph, BD, AAHandle, ABHandle, ACHandle, ADHandle,
                        BDHandle);

                    AddLayeredNeuronPass("Neuron Output", renderGraph, Output, BAHandle, BBHandle, BCHandle, BDHandle,
                        OutputHandle);
                break;
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<PaintPassData>("Neuron Paint", out var passData)) {
                passData.material = paintMaterial;
                passData.cameraTex = resourceData.cameraColor;
                passData.networkOutput = OutputHandle;
                passData.paintOutput = paintHandle;
                builder.UseTexture(passData.cameraTex);
                builder.UseTexture(passData.networkOutput);
                builder.SetRenderAttachment(passData.paintOutput, 0);
                builder.SetRenderFunc((PaintPassData data, RasterGraphContext context) =>
                    ExecutePaintPass(data, context));
            }
            
            AddBlitPass("Blur", renderGraph, blurMaterial, paintHandle, intermediateHandle);
            
            AddCopyPass("Copy Camera", renderGraph, intermediateHandle, resourceData.cameraColor);
            
            AddCopyPass("Copy Active", renderGraph, intermediateHandle, resourceData.activeColorTexture);
        }
    }
}