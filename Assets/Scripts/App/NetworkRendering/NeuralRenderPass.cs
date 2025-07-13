using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Neuralizer.NetworkRendering {
    public class NeuralRenderPass : ScriptableRenderPass {
        private const string AA_Name = "_AA";
        private const string AB_Name = "_AB";
        private const string AC_Name = "_AC";
        private const string AD_Name = "_AD";
        private const string BA_Name = "_BA";
        private const string BB_Name = "_BB";
        private const string BC_Name = "_BC";
        private const string BD_Name = "_BD";
        private const string Output_Name = "_Output";
        
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

        public Material inputLayerMaterial;
        public Material hiddenLayerMaterial;
        public Material outputLayerMaterial;
        public Neuron AA, AB, AC, AD, BA, BB, BC, BD, Output;

        private RTHandle AATex, ABTex, ACTex, ADTex, BATex, BBTex, BCTex, BDTex, OutputTex;
        private RenderTextureDescriptor descriptor;

        private static readonly GraphicsFormat[] AccumulationFormatList = {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm
        };

        public class InputPassData {
            public TextureHandle output;
            public Material material;
            public Neuron target;
        }

        public class HiddenPassData {
            public TextureHandle inputAA;
            public TextureHandle inputAB;
            public TextureHandle inputAC;
            public TextureHandle inputAD;
            public TextureHandle output;
            public Material material;
            public Neuron target;
        }

        public class OutputPassData {
            public TextureHandle inputBA;
            public TextureHandle inputBB;
            public TextureHandle inputBC;
            public TextureHandle inputBD;
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

            descriptor = new() {
                width = 216,
                height = 216,
                // width = 36,
                // height = 36,
                // volumeDepth = 36,
                dimension = TextureDimension.Tex2D,
                msaaSamples = 1,
                mipCount = 0,
                sRGB = false,
                depthStencilFormat = GraphicsFormat.None,
                memoryless = RenderTextureMemoryless.None,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false,
                bindMS = false,
                useDynamicScale = false,
                graphicsFormat = GraphicsFormat.R16G16B16_SFloat
            };

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
            RenderingUtils.ReAllocateHandleIfNeeded(ref handle, descriptor, FilterMode.Point, TextureWrapMode.Mirror,
                name: name);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            ReAllocateIfNeeded(ref AATex, AA_Name);
            ReAllocateIfNeeded(ref ABTex, AB_Name);
            ReAllocateIfNeeded(ref ACTex, AC_Name);
            ReAllocateIfNeeded(ref ADTex, AD_Name);
            ReAllocateIfNeeded(ref BATex, BA_Name);
            ReAllocateIfNeeded(ref BBTex, BB_Name);
            ReAllocateIfNeeded(ref BCTex, BC_Name);
            ReAllocateIfNeeded(ref BDTex, BD_Name);
            ReAllocateIfNeeded(ref OutputTex, Output_Name);

            TextureHandle AAHandle = renderGraph.ImportTexture(AATex);
            TextureHandle ABHandle = renderGraph.ImportTexture(ABTex);
            TextureHandle ACHandle = renderGraph.ImportTexture(ACTex);
            TextureHandle ADHandle = renderGraph.ImportTexture(ADTex);
            TextureHandle BAHandle = renderGraph.ImportTexture(BATex);
            TextureHandle BBHandle = renderGraph.ImportTexture(BBTex);
            TextureHandle BCHandle = renderGraph.ImportTexture(BCTex);
            TextureHandle BDHandle = renderGraph.ImportTexture(BDTex);
            TextureHandle OutputHandle = renderGraph.ImportTexture(OutputTex);

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AA", out var passData)) {
                passData.output = AAHandle;
                passData.material = inputLayerMaterial;
                passData.target = AA;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AB", out var passData)) {
                passData.output = ABHandle;
                passData.material = inputLayerMaterial;
                passData.target = AB;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AC", out var passData)) {
                passData.output = ACHandle;
                passData.material = inputLayerMaterial;
                passData.target = AC;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<InputPassData>("Neuron AD", out var passData)) {
                passData.output = ADHandle;
                passData.material = inputLayerMaterial;
                passData.target = AD;
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((InputPassData data, RasterGraphContext context) =>
                    ExecuteInputPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<HiddenPassData>("Neuron BA", out var passData)) {
                passData.inputAA = AAHandle;
                passData.inputAB = ABHandle;
                passData.inputAC = ACHandle;
                passData.inputAD = ADHandle;
                passData.output = BAHandle;
                passData.target = BA;
                passData.material = hiddenLayerMaterial;
                builder.UseTexture(passData.inputAA);
                builder.UseTexture(passData.inputAB);
                builder.UseTexture(passData.inputAC);
                builder.UseTexture(passData.inputAD);
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((HiddenPassData data, RasterGraphContext context) =>
                    ExecuteHiddenPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<HiddenPassData>("Neuron BB", out var passData)) {
                passData.inputAA = AAHandle;
                passData.inputAB = ABHandle;
                passData.inputAC = ACHandle;
                passData.inputAD = ADHandle;
                passData.output = BBHandle;
                passData.target = BB;
                passData.material = hiddenLayerMaterial;
                builder.UseTexture(passData.inputAA);
                builder.UseTexture(passData.inputAB);
                builder.UseTexture(passData.inputAC);
                builder.UseTexture(passData.inputAD);
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((HiddenPassData data, RasterGraphContext context) =>
                    ExecuteHiddenPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<HiddenPassData>("Neuron BC", out var passData)) {
                passData.inputAA = AAHandle;
                passData.inputAB = ABHandle;
                passData.inputAC = ACHandle;
                passData.inputAD = ADHandle;
                passData.output = BCHandle;
                passData.target = BC;
                passData.material = hiddenLayerMaterial;
                builder.UseTexture(passData.inputAA);
                builder.UseTexture(passData.inputAB);
                builder.UseTexture(passData.inputAC);
                builder.UseTexture(passData.inputAD);
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((HiddenPassData data, RasterGraphContext context) =>
                    ExecuteHiddenPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<HiddenPassData>("Neuron BD", out var passData)) {
                passData.inputAA = AAHandle;
                passData.inputAB = ABHandle;
                passData.inputAC = ACHandle;
                passData.inputAD = ADHandle;
                passData.output = BDHandle;
                passData.target = BD;
                passData.material = hiddenLayerMaterial;
                builder.UseTexture(passData.inputAA);
                builder.UseTexture(passData.inputAB);
                builder.UseTexture(passData.inputAC);
                builder.UseTexture(passData.inputAD);
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((HiddenPassData data, RasterGraphContext context) =>
                    ExecuteHiddenPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<OutputPassData>("Neuron Output", out var passData)) {
                passData.inputBA = BAHandle;
                passData.inputBB = BBHandle;
                passData.inputBC = BCHandle;
                passData.inputBD = BDHandle;
                passData.output = OutputHandle;
                passData.target = Output;
                passData.material = outputLayerMaterial;
                builder.UseTexture(passData.inputBA);
                builder.UseTexture(passData.inputBB);
                builder.UseTexture(passData.inputBC);
                builder.UseTexture(passData.inputBD);
                builder.SetRenderAttachment(passData.output, 0);
                builder.SetRenderFunc((OutputPassData data, RasterGraphContext context) =>
                    ExecuteOutputPass(data, context));
            }
        }

        static void SetShaderData(Neuron target) {
            Shader.SetGlobalVector(Weights_ID, target.weights);
            Shader.SetGlobalFloat(Bias_ID, target.bias);
            switch (target.activationFunction) {
                default:
                case ActivationFunction.Linear:
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_LINEAR");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
                case ActivationFunction.Stepped:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAR");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
                case ActivationFunction.ReLu:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAR");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
                case ActivationFunction.Sigmoid:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAR");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
                case ActivationFunction.TanH:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAR");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
            }
        }

        static void ExecuteInputPass(InputPassData data, RasterGraphContext context) {
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
        }

        static void ExecuteHiddenPass(HiddenPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(AA_ID, data.inputAA);
            Shader.SetGlobalTexture(AB_ID, data.inputAB);
            Shader.SetGlobalTexture(AC_ID, data.inputAC);
            Shader.SetGlobalTexture(AD_ID, data.inputAD);
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
        }

        static void ExecuteOutputPass(OutputPassData data, RasterGraphContext context) {
            Shader.SetGlobalTexture(BA_ID, data.inputBA);
            Shader.SetGlobalTexture(BB_ID, data.inputBB);
            Shader.SetGlobalTexture(BC_ID, data.inputBC);
            Shader.SetGlobalTexture(BD_ID, data.inputBD);
            SetShaderData(data.target);
            Blitter.BlitTexture(context.cmd, Vector4.one, data.material, 0);
            Shader.SetGlobalTexture(Output_ID, data.output);
        }
    }
}