using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
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
        private const string Intermediate_Name = "_IntermediateOutput";
        private const string Weights_Name = "_Weights";
        private const string Bias_Name = "_Bias";

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
        private static readonly int Intermediate_ID = Shader.PropertyToID(Intermediate_Name);

        private static readonly int Weights_ID = Shader.PropertyToID(Weights_Name);
        private static readonly int Bias_ID = Shader.PropertyToID(Bias_Name);

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
        public Material blurMaterial;
        public Neuron AA, AB, AC, AD, BA, BB, BC, BD, Output;
        public Architecture architecture;
        public bool isPlanar = false;

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
            public Material material;
        }

        private class BlitPass {
            public TextureHandle source;
            public Material material;
        }

        private class CopyPassData {
            public TextureHandle source;
        }

        public NeuralRenderPass() {
            ConfigureInput(ScriptableRenderPassInput.Color);
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

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
            Blitter.BlitTexture(context.cmd, Vector2.one, data.neuron.material, 0);
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
            Blitter.BlitTexture(context.cmd, Vector2.one, data.neuron.material, 0);
        }

        static void ExecutePaintPass(PaintPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(Output_ID, data.networkOutput);
            Blitter.BlitTexture(context.cmd, data.cameraTex, Vector2.one, data.material, 0);
        }

        static void ExecuteBlurPass(BlitPass data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, 0);
        }

        static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context) {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0f, true);
        }

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
        
        private static void AddCopyPass(string passName, RenderGraph renderGraph, TextureHandle from, TextureHandle to) {
            using var builder =
                renderGraph.AddRasterRenderPass<CopyPassData>(passName, out var passData);
            passData.source = from;
            builder.UseTexture(passData.source);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                ExecuteCopyPass(data, context));
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