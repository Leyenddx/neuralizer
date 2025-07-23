using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Neuralizer.NetworkRendering {
    public class NeuralRenderPass : ScriptableRenderPass {
        private const string A_Name = "_A";
        private const string B_Name = "_B";
        private const string C_Name = "_C";
        private const string D_Name = "_D";
        private const string AA_Name = "_AA";
        private const string AB_Name = "_AB";
        private const string AC_Name = "_AC";
        private const string AD_Name = "_AD";
        private const string BA_Name = "_BA";
        private const string BB_Name = "_BB";
        private const string BC_Name = "_BC";
        private const string BD_Name = "_BD";
        private const string Output_Name = "_Output";
        private const string Paint_Name = "_PaintOutput";
        private const string History_Name = "_History";

        private static readonly int A_ID = Shader.PropertyToID(A_Name);
        private static readonly int B_ID = Shader.PropertyToID(B_Name);
        private static readonly int C_ID = Shader.PropertyToID(C_Name);
        private static readonly int D_ID = Shader.PropertyToID(D_Name);
        private static readonly int AA_ID = Shader.PropertyToID(AA_Name);
        private static readonly int AB_ID = Shader.PropertyToID(AB_Name);
        private static readonly int AC_ID = Shader.PropertyToID(AC_Name);
        private static readonly int AD_ID = Shader.PropertyToID(AD_Name);
        private static readonly int BA_ID = Shader.PropertyToID(BA_Name);
        private static readonly int BB_ID = Shader.PropertyToID(BB_Name);
        private static readonly int BC_ID = Shader.PropertyToID(BC_Name);
        private static readonly int BD_ID = Shader.PropertyToID(BD_Name);
        private static readonly int Output_ID = Shader.PropertyToID(Output_Name);
        private static readonly int Paint_ID = Shader.PropertyToID(Paint_Name);
        private static readonly int History_ID = Shader.PropertyToID(History_Name);

        private static readonly int Weights_ID = Shader.PropertyToID("_Weights");
        private static readonly int Bias_ID = Shader.PropertyToID("_Bias");

        private static readonly ShaderKeyword activationFunction_Linear = new("_ACTIVATIONFUNCTION_LINEAR");
        private static readonly ShaderKeyword activationFunction_Stepped = new("_ACTIVATIONFUNCTION_STEPPED");
        private static readonly ShaderKeyword activationFunction_ReLu = new("_ACTIVATIONFUNCTION_RELU");
        private static readonly ShaderKeyword activationFunction_Sigmoid = new("_ACTIVATIONFUNCTION_SIGMOID");
        private static readonly ShaderKeyword activationFunction_TanH = new("_ACTIVATIONFUNCTION_TANH");

        private static readonly ShaderKeyword inputSource_World = new("_INPUT_SOURCE_WORLD");
        private static readonly ShaderKeyword inputSource_2Textures = new("_INPUT_SOURCE_2TEXTURES");
        private static readonly ShaderKeyword inputSource_4Textures = new("_INPUT_SOURCE_4TEXTURES");

        public Material neuronMaterial;
        public Material paintMaterial;
        public Neuron AA, AB, AC, AD, BA, BB, BC, BD, Output;
        public Architecture architecture;
        public bool isPlanar = false;

        private RTHandle history;
        private RenderTextureDescriptor neuronDescriptor;
        private RenderTextureDescriptor paintDescriptor;

        private static readonly GraphicsFormat[] NeuronOutputFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.R32G32B32A32_SFloat
        };

        private static readonly GraphicsFormat[] CameraFormatList = {
            // GraphicsFormat.R16G16B16A16_SFloat,
            // GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        private class InputPassData {
            public TextureHandle output;
            public Neuron neuron;
        }

        private class LayeredPassData : InputPassData {
            public TextureHandle inputA;
            public TextureHandle inputB;
            public TextureHandle inputC;
            public TextureHandle inputD;
        }

        private class PaintPassData {
            public TextureHandle cameraTex;
            public TextureHandle networkOutput;
            public TextureHandle paintOutput;
            public TextureHandle history;
            public Material material;
        }

        private class CopyPassData {
            public TextureHandle source;
        }

        public NeuralRenderPass() {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            history = RTHandles.Alloc(History_ID, History_Name);
        }

        public void Initialize() {
            AA.PrepareForRenderPass(AA_ID, AA_Name, neuronMaterial);
            AB.PrepareForRenderPass(AB_ID, AB_Name, neuronMaterial);
            AC.PrepareForRenderPass(AC_ID, AC_Name, neuronMaterial);
            AD.PrepareForRenderPass(AD_ID, AD_Name, neuronMaterial);
            BA.PrepareForRenderPass(BA_ID, BA_Name, neuronMaterial);
            BB.PrepareForRenderPass(BB_ID, BB_Name, neuronMaterial);
            BC.PrepareForRenderPass(BC_ID, BC_Name, neuronMaterial);
            BD.PrepareForRenderPass(BD_ID, BD_Name, neuronMaterial);
            Output.PrepareForRenderPass(Output_ID, Output_Name, neuronMaterial);
        }

        public void Dispose() {
            AA.resultTexture.Release();
            AB.resultTexture.Release();
            AC.resultTexture.Release();
            AD.resultTexture.Release();
            BA.resultTexture.Release();
            BB.resultTexture.Release();
            BC.resultTexture.Release();
            BD.resultTexture.Release();
            Output.resultTexture.Release();
        }

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
            
            switch(architecture) {
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
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref history, paintDescriptor, FilterMode.Point,
                TextureWrapMode.Clamp,
                name: History_Name);
            
            var historyHandle = renderGraph.ImportTexture(history);
            
            
            switch(architecture) {
                case Architecture.Perceptron:
                    AddInputNeuronPass("Neuron Output", renderGraph, Output, OutputHandle);
                break;
                case Architecture.OneLayer:
                    TextureHandle AHandle = AA.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    TextureHandle BHandle = AB.SetupRenderTarget(B_Name, renderGraph, neuronDescriptor);
                    TextureHandle CHandle = AC.SetupRenderTarget(D_Name, renderGraph, neuronDescriptor);
                    TextureHandle DHandle = AD.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    AddInputNeuronPass("Neuron AA", renderGraph, AA, AHandle);
                    AddInputNeuronPass("Neuron AB", renderGraph, AB, BHandle);
                    AddInputNeuronPass("Neuron AC", renderGraph, AC, CHandle);
                    AddInputNeuronPass("Neuron AD", renderGraph, AD, DHandle);
                    
                    AddLayeredNeuronPass("Neuron Output", renderGraph, Output, AHandle, BHandle, CHandle, DHandle, OutputHandle);
                break;
                case Architecture.TwoLayers:
                    TextureHandle AAHandle = AA.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    TextureHandle ABHandle = AB.SetupRenderTarget(B_Name, renderGraph, neuronDescriptor);
                    TextureHandle ACHandle = AC.SetupRenderTarget(D_Name, renderGraph, neuronDescriptor);
                    TextureHandle ADHandle = AD.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    TextureHandle BAHandle = BA.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    TextureHandle BBHandle = BB.SetupRenderTarget(B_Name, renderGraph, neuronDescriptor);
                    TextureHandle BCHandle = BC.SetupRenderTarget(D_Name, renderGraph, neuronDescriptor);
                    TextureHandle BDHandle = BD.SetupRenderTarget(A_Name, renderGraph, neuronDescriptor);
                    
                    AddInputNeuronPass("Neuron AA", renderGraph, AA, AAHandle);
                    AddInputNeuronPass("Neuron AB", renderGraph, AB, ABHandle);
                    AddInputNeuronPass("Neuron AC", renderGraph, AC, ACHandle);
                    AddInputNeuronPass("Neuron AD", renderGraph, AD, ADHandle);
                    AddLayeredNeuronPass("Neuron BA", renderGraph, BA, AAHandle, ABHandle, ACHandle, ADHandle, BAHandle);
                    AddLayeredNeuronPass("Neuron BB", renderGraph, BB, AAHandle, ABHandle, ACHandle, ADHandle, BBHandle);
                    AddLayeredNeuronPass("Neuron BC", renderGraph, BC, AAHandle, ABHandle, ACHandle, ADHandle, BCHandle);
                    AddLayeredNeuronPass("Neuron BD", renderGraph, BD, AAHandle, ABHandle, ACHandle, ADHandle, BDHandle);

                    AddLayeredNeuronPass("Neuron Output", renderGraph, Output, BAHandle, BBHandle, BCHandle, BDHandle, OutputHandle);
                break;
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<PaintPassData>("Neuron Paint", out var passData)) {
                passData.material = paintMaterial;
                passData.cameraTex = resourceData.activeColorTexture;
                passData.networkOutput = OutputHandle;
                passData.history = historyHandle;
                passData.paintOutput = paintHandle;
                builder.UseTexture(passData.cameraTex);
                builder.UseTexture(passData.networkOutput);
                builder.UseTexture(passData.history);
                builder.SetRenderAttachment(passData.paintOutput, 0);
                builder.SetRenderFunc((PaintPassData data, RasterGraphContext context) =>
                    ExecutePaintPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("Neuron Copy",
                       out var passData)) {
                passData.source = paintHandle;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>("Copy History",
                       out var passData)) {
                passData.source = resourceData.activeColorTexture;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(historyHandle, 0);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    ExecuteCopyPass(data, context));
            }
        }

        private static void AddInputNeuronPass(string passName, RenderGraph renderGraph, Neuron targetNeuron, TextureHandle OutputHandle) {
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<InputPassData>(passName, out var passData);
            passData.output = OutputHandle;
            passData.neuron = targetNeuron;
            builder.SetRenderAttachment(passData.output, 0);
            builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                ExecuteInputPass(data, context));
        }

        private static void AddLayeredNeuronPass(string passName, RenderGraph renderGraph, Neuron targetNeuron, TextureHandle A,
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

        static void ExecuteInputPass(InputPassData data, RasterGraphContext context) {
            inputSource_World.SetMaterial(data.neuron.material, true);
            inputSource_2Textures.SetMaterial(data.neuron.material, false);
            inputSource_4Textures.SetMaterial(data.neuron.material, false);
            SetShaderData(data.neuron);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.neuron.material, 0);
        }

        static void ExecuteLayeredPass(LayeredPassData data, RasterGraphContext context) {
            inputSource_World.SetMaterial(data.neuron.material, false);
            inputSource_2Textures.SetMaterial(data.neuron.material, false);
            inputSource_4Textures.SetMaterial(data.neuron.material, true);
            data.neuron.material.SetTexture(A_ID, data.inputA);
            data.neuron.material.SetTexture(B_ID, data.inputB);
            data.neuron.material.SetTexture(C_ID, data.inputC);
            data.neuron.material.SetTexture(D_ID, data.inputD);
            SetShaderData(data.neuron);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.neuron.material, 0);
        }

        static void ExecutePaintPass(PaintPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(Output_ID, data.networkOutput);
            Shader.SetGlobalTexture(History_ID, data.history);
            Blitter.BlitTexture(context.cmd, data.cameraTex, Vector4.one, data.material, 0);
        }

        static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }
    }
}