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

        private static readonly int Weights_ID = Shader.PropertyToID("_Weights");
        private static readonly int Bias_ID = Shader.PropertyToID("_Bias");

        private static readonly ShaderKeyword activationFunction_Linear = new("_ACTIVATIONFUNCTION_LINEAR");
        private static readonly ShaderKeyword activationFunction_Stepped = new("_ACTIVATIONFUNCTION_STEPPED");
        private static readonly ShaderKeyword activationFunction_ReLu = new("_ACTIVATIONFUNCTION_RELU");
        private static readonly ShaderKeyword activationFunction_Sigmoid = new("_ACTIVATIONFUNCTION_SIGMOID");
        private static readonly ShaderKeyword activationFunction_TanH = new("_ACTIVATIONFUNCTION_TANH");

        public Material inputMaterial;
        public Material layerMaterial;
        public Neuron AA, AB, AC, AD, BA, BB, BC, BD, Output;
        public Architecture architecture;

        private RTHandle AATex, ABTex, ACTex, ADTex, BATex, BBTex, BCTex, BDTex, OutputTex;
        private RenderTextureDescriptor descriptor;

        private static readonly GraphicsFormat[] AccumulationFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.R32G32B32A32_SFloat
        };

        public class InputPassData {
            public TextureHandle output;
            public Material material;
            public Neuron target;
        }

        public class LayeredPassData {
            public TextureHandle inputA;
            public TextureHandle inputB;
            public TextureHandle inputC;
            public TextureHandle inputD;
            public TextureHandle output;
            public Material material;
            public Neuron target;
        }

        public NeuralRenderPass() {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            AATex = RTHandles.Alloc(AA_ID, AA_Name);
            ABTex = RTHandles.Alloc(AB_ID, AB_Name);
            ACTex = RTHandles.Alloc(AC_ID, AC_Name);
            ADTex = RTHandles.Alloc(AD_ID, AD_Name);
            BATex = RTHandles.Alloc(BA_ID, BA_Name);
            BBTex = RTHandles.Alloc(BB_ID, BB_Name);
            BCTex = RTHandles.Alloc(BC_ID, BC_Name);
            BDTex = RTHandles.Alloc(BD_ID, BD_Name);
            OutputTex = RTHandles.Alloc(Output_ID, Output_Name);

            const int size = 36;
            const int sizeSqr = size * size;

            descriptor = new(sizeSqr, size, RenderTextureFormat.ARGBFloat, 0);
            descriptor.msaaSamples = 1;
            descriptor.mipCount = 0;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.bindMS = false;
            descriptor.useDynamicScale = false;
            descriptor.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;

            if (!SystemInfo.IsFormatSupported(descriptor.graphicsFormat, GraphicsFormatUsage.Render)) {
                descriptor.graphicsFormat = GraphicsFormat.None;
                for (int i = 0; i < AccumulationFormatList.Length; i++)
                    if (SystemInfo.IsFormatSupported(AccumulationFormatList[i], GraphicsFormatUsage.Render)) {
                        descriptor.graphicsFormat = AccumulationFormatList[i];
                        break;
                    }
            }
        }

        public void Dispose() {
            AATex.Release();
            ABTex.Release();
            ACTex.Release();
            ADTex.Release();
            BATex.Release();
            BBTex.Release();
            BCTex.Release();
            BDTex.Release();
            OutputTex.Release();
        }

        private void ReAllocateIfNeeded(ref RTHandle handle, string name) {
            RenderingUtils.ReAllocateHandleIfNeeded(ref handle, descriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: name);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            // UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            ReAllocateIfNeeded(ref AATex, AA_Name);
            ReAllocateIfNeeded(ref ABTex, AB_Name);
            ReAllocateIfNeeded(ref ACTex, AC_Name);
            ReAllocateIfNeeded(ref ADTex, AD_Name);

            TextureHandle AAHandle = renderGraph.ImportTexture(AATex);
            TextureHandle ABHandle = renderGraph.ImportTexture(ABTex);
            TextureHandle ACHandle = renderGraph.ImportTexture(ACTex);
            TextureHandle ADHandle = renderGraph.ImportTexture(ADTex);

            ReAllocateIfNeeded(ref OutputTex, Output_Name);
            TextureHandle OutputHandle = renderGraph.ImportTexture(OutputTex);

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AA", out var passData)) {
                passData.output = AAHandle;
                passData.material = inputMaterial;
                passData.target = AA;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AB", out var passData)) {
                passData.output = ABHandle;
                passData.material = inputMaterial;
                passData.target = AB;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AC", out var passData)) {
                passData.output = ACHandle;
                passData.material = inputMaterial;
                passData.target = AC;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AD", out var passData)) {
                passData.output = ADHandle;
                passData.material = inputMaterial;
                passData.target = AD;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            if (architecture == Architecture.TwoLayers) {
                ReAllocateIfNeeded(ref BATex, BA_Name);
                ReAllocateIfNeeded(ref BBTex, BB_Name);
                ReAllocateIfNeeded(ref BCTex, BC_Name);
                ReAllocateIfNeeded(ref BDTex, BD_Name);

                TextureHandle BAHandle = renderGraph.ImportTexture(BATex);
                TextureHandle BBHandle = renderGraph.ImportTexture(BBTex);
                TextureHandle BCHandle = renderGraph.ImportTexture(BCTex);
                TextureHandle BDHandle = renderGraph.ImportTexture(BDTex);

                using (var builder = renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron BA", out var passData)) {
                    passData.inputA = AAHandle;
                    passData.inputB = ABHandle;
                    passData.inputC = ACHandle;
                    passData.inputD = ADHandle;
                    passData.output = BAHandle;
                    passData.target = BA;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteHiddenPass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron BB", out var passData)) {
                    passData.inputA = AAHandle;
                    passData.inputB = ABHandle;
                    passData.inputC = ACHandle;
                    passData.inputD = ADHandle;
                    passData.output = BBHandle;
                    passData.target = BB;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteHiddenPass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron BC", out var passData)) {
                    passData.inputA = AAHandle;
                    passData.inputB = ABHandle;
                    passData.inputC = ACHandle;
                    passData.inputD = ADHandle;
                    passData.output = BCHandle;
                    passData.target = BC;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteHiddenPass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron BD", out var passData)) {
                    passData.inputA = AAHandle;
                    passData.inputB = ABHandle;
                    passData.inputC = ACHandle;
                    passData.inputD = ADHandle;
                    passData.output = BDHandle;
                    passData.target = BD;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteHiddenPass(data, context));
                }

                using (var builder =
                       renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron Output", out var passData)) {
                    passData.inputA = BAHandle;
                    passData.inputB = BBHandle;
                    passData.inputC = BCHandle;
                    passData.inputD = BDHandle;
                    passData.output = OutputHandle;
                    passData.target = Output;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteOutputPass(data, context));
                }
            }
            else {
                using (var builder =
                       renderGraph.AddRasterRenderPass<LayeredPassData>("Neuron Output", out var passData)) {
                    passData.inputA = AAHandle;
                    passData.inputB = ABHandle;
                    passData.inputC = ACHandle;
                    passData.inputD = ADHandle;
                    passData.output = OutputHandle;
                    passData.target = Output;
                    passData.material = layerMaterial;
                    builder.UseTexture(passData.inputA);
                    builder.UseTexture(passData.inputB);
                    builder.UseTexture(passData.inputC);
                    builder.UseTexture(passData.inputD);
                    builder.SetRenderAttachment(passData.output, 0);
                    builder.SetRenderFunc((LayeredPassData data, RasterGraphContext context) =>
                        ExecuteOutputPass(data, context));
                }
            }
        }

        public static void SetShaderData(Neuron target) {
            Shader.SetGlobalVector(Weights_ID, target.weights);
            Shader.SetGlobalFloat(Bias_ID, target.bias);
            activationFunction_Linear.Enabled = target.activationFunction == ActivationFunction.Linear;
            activationFunction_Stepped.Enabled = target.activationFunction == ActivationFunction.Stepped;
            activationFunction_ReLu.Enabled = target.activationFunction == ActivationFunction.ReLu;
            activationFunction_Sigmoid.Enabled = target.activationFunction == ActivationFunction.Sigmoid;
            activationFunction_TanH.Enabled = target.activationFunction == ActivationFunction.TanH;
        }

        static void ExecuteInputPass(InputPassData data, RasterGraphContext context) {
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
        }

        static void ExecuteHiddenPass(LayeredPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(A_ID, data.inputA);
            Shader.SetGlobalTexture(B_ID, data.inputB);
            Shader.SetGlobalTexture(C_ID, data.inputC);
            Shader.SetGlobalTexture(D_ID, data.inputD);
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
        }

        static void ExecuteOutputPass(LayeredPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(A_ID, data.inputA);
            Shader.SetGlobalTexture(B_ID, data.inputB);
            Shader.SetGlobalTexture(C_ID, data.inputC);
            Shader.SetGlobalTexture(D_ID, data.inputD);
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
            Shader.SetGlobalTexture(Output_ID, data.output);
        }
    }
}