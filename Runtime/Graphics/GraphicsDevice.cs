using Runtime.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace Runtime.Graphics
{
    public sealed class GraphicsDevice : IDisposable
    {
        public GraphicsDevice(Window window,bool bSrgb,bool bVsync)
        {
#if DEBUG
            bool bDebug = false;
#else
            bool bDebug = false;
#endif
            GraphicsDeviceOptions options = new GraphicsDeviceOptions()
            {
                Debug = bDebug,
                HasMainSwapchain = true,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Default,
                SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt,
                SwapchainSrgbFormat = bSrgb,
                SyncToVerticalBlank = bVsync
            };

            UnderlyingDevice = VeldridStartup.CreateGraphicsDevice(window.UnderlyingWindow, options, GetOptimalBackend());

            //Create sprite resource layout
            Veldrid.ResourceLayoutDescription layoutDesc = new ResourceLayoutDescription()
            {
                Elements = new[]
                {
                    new Veldrid.ResourceLayoutElementDescription()
                    {
                        Kind = ResourceKind.TextureReadOnly,
                        Name = "SpriteTexture",
                        Options = ResourceLayoutElementOptions.None,
                        Stages = ShaderStages.Fragment
                    }
                }
            };
            SpriteLayout = CreateResourceLayout(layoutDesc);
        }

        public Veldrid.Swapchain MainSwapchain => UnderlyingDevice.MainSwapchain;
        public Veldrid.Sampler PointSampler => UnderlyingDevice.PointSampler;
        public Veldrid.Sampler LinearSampler => UnderlyingDevice.LinearSampler;
        public Veldrid.ResourceLayout SpriteLayout { get; private init; }

        internal Veldrid.GraphicsDevice UnderlyingDevice { get; private init; }

        public DeviceBuffer AllocateBuffer(in BufferDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateBuffer(desc);
        }
        public Texture AllocateTexture(in TextureDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateTexture(desc);
        }
        public TextureView CreateTextureView(in TextureViewDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateTextureView(desc);
        }
        public Sampler CreateSampler(in SamplerDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateSampler(desc);
        }
        public CommandList AllocateCommandList()
        {
            return UnderlyingDevice.ResourceFactory.CreateCommandList();
        }
        public Fence CreateFence()
        {
            return UnderlyingDevice.ResourceFactory.CreateFence(true);
        }
        public Framebuffer CreateFramebuffer(in FramebufferDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateFramebuffer(desc);
        }
        public ResourceLayout CreateResourceLayout(in ResourceLayoutDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateResourceLayout(desc);
        }
        public ResourceSet CreateResourceSet(in ResourceSetDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateResourceSet(desc);
        }
        public Pipeline CreateGraphicsPipeline(in GraphicsPipelineDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateGraphicsPipeline(desc);
        }
        public Pipeline CreateComputePipeline(in ComputePipelineDescription desc)
        {
            return UnderlyingDevice.ResourceFactory.CreateComputePipeline(desc);
        }
        public Shader[] CompileVertexFragmentShader(in ShaderDescription vertexDesc,in ShaderDescription fragmentDesc)
        {
            return UnderlyingDevice.ResourceFactory.CreateFromSpirv(vertexDesc, fragmentDesc);
        }
        public void UpdateHostBuffer(DeviceBuffer buffer, nuint data, uint sizeInBytes,uint offset)
        {
            UnderlyingDevice.UpdateBuffer(buffer, offset, (nint)data,sizeInBytes);
        }
        public void UpdateHostBufferGeneric<T>(DeviceBuffer buffer, T[] data,uint offset) where T : unmanaged
        {
            UnderlyingDevice.UpdateBuffer<T>(buffer, offset, data);
        }
        public void UpdateHostBufferGeneric<T>(DeviceBuffer buffer, ReadOnlySpan<T> data, uint offset) where T : unmanaged
        {
            UnderlyingDevice.UpdateBuffer<T>(buffer, offset, data);
        }
        public void UpdateHostTexture(Texture texture, nuint data, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint arrayIndex, uint mipIndex)
        {
            UnderlyingDevice.UpdateTexture(texture, (nint)data, sizeInBytes, x, y, z, width, height, depth, arrayIndex, mipIndex);
        }
        public void UpdateBufferJIT(DeviceBuffer buffer, nuint data, uint offsetInBytes, uint sizeInBytes)
        {
            UnderlyingDevice.UpdateBuffer(buffer, offsetInBytes, (nint)data, sizeInBytes);
        }
        public void UpdateTextureJIT(Texture texture, nuint data, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint arrayIndex, uint mipIndex)
        {
            UnderlyingDevice.UpdateTexture(texture, (nint)data, sizeInBytes, x, y, z, width, height, depth, arrayIndex, mipIndex);
        }
        public void SubmitCommandList(CommandList cmdList, Fence fence)
        {
            UnderlyingDevice.SubmitCommands(cmdList, fence);
        }
        public void SubmitCommandList(CommandList cmdList)
        {
            UnderlyingDevice.SubmitCommands(cmdList);
        }
        public void ResetFence(Fence fence)
        {
            UnderlyingDevice.ResetFence(fence);
        }
        public void WaitFence(Fence fence)
        {
            UnderlyingDevice.WaitForFence(fence);
        }
        public void WaitFences(Fence[] fences)
        {
            UnderlyingDevice.WaitForFences(fences, true);
        }
        public void WaitIdle()
        {
            UnderlyingDevice.WaitForIdle();
        }
        public void Present()
        {
            UnderlyingDevice.SwapBuffers(UnderlyingDevice.MainSwapchain);
        }

        public void Dispose()
        {
            UnderlyingDevice.Dispose();
        }

        private Veldrid.GraphicsBackend GetOptimalBackend()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Veldrid.GraphicsBackend.Metal : Veldrid.GraphicsBackend.Vulkan;
        }
    }
}
