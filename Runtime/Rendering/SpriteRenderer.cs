using Runtime.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Vulkan;
using static System.Formats.Asn1.AsnWriter;

namespace Runtime.Rendering
{
    /// <summary>
    /// A renderer where you can submit draw calls adn render them in a performant way
    /// </summary>
    public sealed class SpriteRenderer : IDisposable
    {
        private struct DrawCall
        {
            public Vector2 Position;
            public Vector2 Scale;
            public float Rotation;
            public SpriteBoundingBox BoundingBox;
        }

        private struct BatchCall
        {
            public Sprite? Sprite;
            public DrawCall[] Calls;
            public uint CurrentCallIndex;
        }
        private struct CameraData
        {
            public Vector2 Position;
            public float OrthoSize;
            public float NPlane;
            public float FPlane;
            public float AspectRatio;
        }

        private struct VertexData
        {
            public Vector2 Position;
            public Vector2 Uv;
        }

        private struct InstanceData
        {
            public Matrix4x4 Matrix;
            public SpriteBoundingBox BoundingBox;
        }

        private const int InstanceDataSize = 64 + 16;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SpriteRenderer(GraphicsDevice device,int batchCacheSize = 1000,int drawCallCacheSize = 10000,int instanceMaxCount = 10000)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            //Allocate resources
            _batches = new BatchCall[batchCacheSize];
            for(int i = 0;i< _batches.Length;i++)
            {
                BatchCall batchCall = new BatchCall()
                {
                    Calls = new DrawCall[drawCallCacheSize],
                    Sprite = null
                };
                _batches[i] = batchCall;
            }
            _instanceCpuData = new InstanceData[instanceMaxCount];
            _currentTasks = new List<Task>(100);

            //Set properties
            _device = device;
            _drawCallCacheSize = drawCallCacheSize;
            _batchCallCacheSize = batchCacheSize;

            //Create resources
            CreateInternalResources();

            //Set default camera state
            _cameraData = new CameraData()
            {
                Position = new Vector2(0, 0),
                OrthoSize = 5.0f,
                NPlane = 0.1f,
                FPlane = 10.0f
            };
        }

        /// <summary>
        /// Set camera data, it will recreate the graphics pipeline if the Veldrid.Framebuffer is different from the before
        /// </summary>
        /// <param name="targetFramebuffer"></param>
        /// <param name="position"></param>
        /// <param name="orthoSize"></param>
        /// <param name="nPlane"></param>
        /// <param name="fPlane"></param>
        public void SetCameraData(Veldrid.Framebuffer targetFramebuffer,in Vector2 position,float orthoSize,float nPlane,float fPlane)
        {
            //Check framebuffer
            if (targetFramebuffer == null)
                return;

            //Create camera data
            _cameraData = new CameraData()
            {
                Position = position,
                OrthoSize = orthoSize,
                AspectRatio = (float)targetFramebuffer.Width / (float)targetFramebuffer.Height,
                NPlane = nPlane,
                FPlane = fPlane
            };

            //Recreate pipeline
            if(_targetFramebuffer != targetFramebuffer)
                RecreateGraphicsPipeline(targetFramebuffer);

            _targetFramebuffer = targetFramebuffer;

        }

        /// <summary>
        /// Sets the sampler for the shader texture sampling
        /// </summary>
        /// <param name="sampler"></param>
        public void SetSampler(Veldrid.Sampler sampler)
        {
            //Clear the former
            if(_sampler != null)
                _samplerResourceSet.Dispose();

            //Create new one and set
            Veldrid.ResourceSetDescription setDesc = new Veldrid.ResourceSetDescription()
            {
                BoundResources = new[] { sampler },
                Layout = _samplerResourceLayout
            };

            _sampler = sampler;
            _samplerResourceSet = _device.CreateResourceSet(setDesc);
        }

        /// <summary>
        /// Registers a draw call
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="position"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <param name="boundingBox"></param>
        /// <exception cref="Exception"></exception>
        public void Draw(Sprite sprite,in Vector2 position,in Vector2 scale,float rotation,in SpriteBoundingBox boundingBox)
        {
            //Check new draw call range
            if (_currentDrawCallCount > _drawCallCacheSize)
                throw new Exception("Draw calls limit exceeded");

            //Check underlying texture
            if (sprite.Texture == null)
                return;

            //Check if this sprite is existing inside the draw calls
            for(int i = 0;i<_currentBatchCount;i++)
            { 
                BatchCall batchCall = _batches[i];
                if(batchCall.Sprite == sprite)
                {
                    batchCall.Calls[batchCall.CurrentCallIndex] = new DrawCall() { Position = position, Scale = scale, Rotation = rotation, BoundingBox = boundingBox };
                    batchCall.CurrentCallIndex++;
                    _batches[i] = batchCall;
                    _currentDrawCallCount++;
                    return;
                }
            }

            //Check new batch call range
            if (_currentBatchCount >= _batchCallCacheSize)
                throw new Exception("Batch call limit exceeded");

            //Setup new batch
            BatchCall newBatchCall = _batches[_currentBatchCount];
            newBatchCall.CurrentCallIndex =1;
            newBatchCall.Sprite = sprite;
            newBatchCall.Calls[0] = new DrawCall() { Position = position, Scale = scale, Rotation = rotation, BoundingBox = boundingBox };
            _batches[_currentBatchCount] = newBatchCall;

            //Increment
            _currentDrawCallCount++;
            _currentBatchCount++;
        }

        /// <summary>
        /// Renders the registered draw calls
        /// </summary>
        /// <param name="bMultithreaded"></param>
        /// <param name="clearColor"></param>
        /// <param name="drawCallPerThread"></param>
        /// <exception cref="Exception"></exception>
        public void Render(bool bMultithreaded,in Veldrid.RgbaFloat clearColor,int drawCallPerThread = 1000)
        {
            if (_currentDrawCallCount > _instanceCpuData.Length)
                throw new Exception("Instance render count exceed the limit");

            if (bMultithreaded)
                RenderMultithreaded(clearColor,drawCallPerThread);
            else
                RenderSingleThreaded(clearColor);

            //Clear up per session data
            _currentDrawCallCount = 0;
            _currentBatchCount = 0;
        }

        public void Dispose()
        {
            //CLear command lists
            _cmdList.Dispose();

            //Clear fence
            _fence.Dispose();

            //Clear resource layouts
            _cameraBufferResourceLayout.Dispose();
            _instanceBufferResourceLayout.Dispose();
            _samplerResourceLayout.Dispose();
            _spriteResourceLayout.Dispose();
        }

        private unsafe void RenderSingleThreaded(in Veldrid.RgbaFloat clearColor)
        {
            //Calculate camera data
            Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-_cameraData.OrthoSize * _cameraData.AspectRatio, _cameraData.OrthoSize * _cameraData.AspectRatio, -_cameraData.OrthoSize, _cameraData.OrthoSize, _cameraData.NPlane, _cameraData.FPlane);
            Matrix4x4 viewMatrix = Matrix4x4.CreateTranslation(new Vector3(-_cameraData.Position.X, -_cameraData.Position.Y, 0));

            //Update camera host data
            ReadOnlySpan<Matrix4x4> projViewMatrices = stackalloc Matrix4x4[] { projectionMatrix, viewMatrix };
            fixed(Matrix4x4* pData = projViewMatrices)
            {
                _device.UpdateHostBuffer(_cameraStageBuffer, (nuint)pData, 128, 0);
            }

            //Iterate batches and collect instance data
            int instanceOffset = 0;
            for(int batchIndex = 0;batchIndex<_currentBatchCount;batchIndex++)
            {
                //Get batch
                BatchCall batch = _batches[batchIndex];
                if (batch.Sprite == null || batch.Sprite.ResourceSet == null)
                    continue;

                for (int drawIndex = 0;drawIndex < batch.CurrentCallIndex;drawIndex++)
                {
                    //Get draw call
                    DrawCall drawCall = batch.Calls[drawIndex];

                    //Update the instance stage buffer
                    Matrix4x4 modelMatrix =
                        Matrix4x4.CreateRotationZ(drawCall.Rotation) *
                        Matrix4x4.CreateScale(new Vector3(drawCall.Scale.X, drawCall.Scale.Y, 1)) *
                        Matrix4x4.CreateTranslation(new Vector3(drawCall.Position.X, drawCall.Position.Y, 0));

                    _instanceCpuData[instanceOffset] = new InstanceData()
                    {
                        Matrix = modelMatrix,
                        BoundingBox = drawCall.BoundingBox
                    };
                    instanceOffset++;
                }
            }

            //Update instance host data
            fixed(InstanceData * pData = _instanceCpuData)
            {
                _device.UpdateHostBuffer(_instanceStageBuffer, (nuint)pData, (uint)(instanceOffset * (InstanceDataSize)),0);
            }

            //Start rendering
            _cmdList.Begin();

            //Copy camera data
            _cmdList.CopyBuffer(_cameraStageBuffer, 0, _cameraDeviceBuffer, 0, 128);

            //Copy instance data
            _cmdList.CopyBuffer(_instanceStageBuffer, 0, _instanceDeviceBuffer, 0, (uint)(instanceOffset * (InstanceDataSize)));

            //Set pipeline
            _cmdList.SetPipeline(_pipeline);

            //Set vertex&index
            _cmdList.SetVertexBuffer(0, _vertexBuffer);
            _cmdList.SetIndexBuffer(_indexBuffer, Veldrid.IndexFormat.UInt16);

            //Set static resource sets
            _cmdList.SetGraphicsResourceSet(0,_cameraBufferResourceSet);
            _cmdList.SetGraphicsResourceSet(1, _instanceBufferResourceSet);
            _cmdList.SetGraphicsResourceSet(2, _samplerResourceSet);

            //Set framebuffer and clear
            _cmdList.SetFramebuffer(_targetFramebuffer);
            _cmdList.ClearColorTarget(0, clearColor);
            _cmdList.ClearDepthStencil(0, 0);

            //Draw
            instanceOffset = 0;
            for(int i = 0;i<_batches.Length;i++)
            {
                BatchCall batch = _batches[i];
                if (batch.Sprite == null || batch.Sprite.ResourceSet == null)
                    continue;

                _cmdList.SetGraphicsResourceSet(3, batch.Sprite.ResourceSet);

                _cmdList.DrawIndexed(6,batch.CurrentCallIndex,0,0, (uint)instanceOffset);

                instanceOffset += (int)batch.CurrentCallIndex;
            }

             _cmdList.End();
            _device.SubmitCommandList(_cmdList, _fence);
            _device.WaitFence(_fence);
            _device.ResetFence(_fence);
        }

        Matrix4x4[] multithreaded_projection_matrices = new Matrix4x4[2];
        private unsafe void RenderMultithreaded(in Veldrid.RgbaFloat clearColor,int drawCallPerThread)
        {
            //Calculate camera data
            Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-_cameraData.OrthoSize * _cameraData.AspectRatio, _cameraData.OrthoSize * _cameraData.AspectRatio, -_cameraData.OrthoSize, _cameraData.OrthoSize, _cameraData.NPlane, _cameraData.FPlane);
            Matrix4x4 viewMatrix = Matrix4x4.CreateTranslation(new Vector3(-_cameraData.Position.X, -_cameraData.Position.Y, 0));

            //Update camera host data
            multithreaded_projection_matrices[0] = projectionMatrix;
            multithreaded_projection_matrices[1] = viewMatrix;
            fixed (Matrix4x4* pData = multithreaded_projection_matrices)
            {
                _device.UpdateHostBuffer(_cameraStageBuffer, (nuint)pData, 128, 0);
            }

            //Setup cpu data per draw batch
            int instanceOffset = 0;
            for(int i = 0;i<_currentBatchCount;i++)
            {
                BatchCall batchCall = _batches[i];

                int drawCallCount = (int)batchCall.CurrentCallIndex;
                while(drawCallCount > 0)
                {
                    if(drawCallCount > drawCallPerThread) // split
                    {
                        Task task = UpdateCPUInstanceDataAsync(batchCall.Calls, (int)(batchCall.CurrentCallIndex - drawCallCount), drawCallPerThread,instanceOffset);
                        _currentTasks.Add(task);
                    }
                    else
                    {
                        Task task = UpdateCPUInstanceDataAsync(batchCall.Calls, (int)(batchCall.CurrentCallIndex - drawCallCount), drawCallCount,instanceOffset);
                        _currentTasks.Add(task);
                    }

                    drawCallCount -= drawCallPerThread;
                }

                instanceOffset += (int)batchCall.CurrentCallIndex;
            }

            //Wait for the cpu tasks
            Task.WhenAll(_currentTasks).Wait();

            //Start rendering
            _cmdList.Begin();

            //Copy camera data
            _cmdList.CopyBuffer(_cameraStageBuffer, 0, _cameraDeviceBuffer, 0, 128);

            //Copy instance data
            _cmdList.CopyBuffer(_instanceStageBuffer, 0, _instanceDeviceBuffer, 0, (uint)(instanceOffset * (InstanceDataSize)));

            //Set pipeline
            _cmdList.SetPipeline(_pipeline);

            //Set vertex&index
            _cmdList.SetVertexBuffer(0, _vertexBuffer);
            _cmdList.SetIndexBuffer(_indexBuffer, Veldrid.IndexFormat.UInt16);

            //Set static resource sets
            _cmdList.SetGraphicsResourceSet(0, _cameraBufferResourceSet);
            _cmdList.SetGraphicsResourceSet(1, _instanceBufferResourceSet);
            _cmdList.SetGraphicsResourceSet(2, _samplerResourceSet);

            //Set framebuffer and clear
            _cmdList.SetFramebuffer(_targetFramebuffer);
            _cmdList.ClearColorTarget(0, clearColor);
            _cmdList.ClearDepthStencil(0, 0);

            //Draw
            instanceOffset = 0;
            for (int i = 0; i < _batches.Length; i++)
            {
                BatchCall batch = _batches[i];
                if (batch.Sprite == null || batch.Sprite.ResourceSet == null)
                    continue;

                _cmdList.SetGraphicsResourceSet(3, batch.Sprite.ResourceSet);

                _cmdList.DrawIndexed(6, batch.CurrentCallIndex, 0, 0, (uint)instanceOffset);

                instanceOffset += (int)batch.CurrentCallIndex;
            }

            _cmdList.End();
            _device.SubmitCommandList(_cmdList, _fence);
            _device.WaitFence(_fence);
            _device.ResetFence(_fence);
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private unsafe async Task UpdateCPUInstanceDataAsync(DrawCall[] drawCalls,int drawCallOffset,int drawCallCount,int baseInstanceOffset)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //Iterate batches and collect instance data
            int instanceIndex = baseInstanceOffset;
            for (int drawIndex = drawCallOffset; drawIndex < drawCallCount; drawIndex++)
            {
                //Get draw call
                DrawCall drawCall = drawCalls[drawIndex];

                //Update the instance stage buffer
                Matrix4x4 modelMatrix =
                    Matrix4x4.CreateRotationZ(drawCall.Rotation) *
                    Matrix4x4.CreateScale(new Vector3(drawCall.Scale.X, drawCall.Scale.Y, 1)) *
                    Matrix4x4.CreateTranslation(new Vector3(drawCall.Position.X, drawCall.Position.Y, 0));

                _instanceCpuData[instanceIndex] = new InstanceData()
                {
                    Matrix = modelMatrix,
                    BoundingBox = drawCall.BoundingBox
                };
                instanceIndex++;
            }

            //Update instance host data
            fixed (InstanceData* pData = _instanceCpuData)
            {
                _device.UpdateHostBuffer(_instanceStageBuffer, (nuint)(pData + baseInstanceOffset), (uint)(drawCallCount * (InstanceDataSize)), (uint)(baseInstanceOffset * InstanceDataSize));
            }
        }
        private unsafe void CreateInternalResources()
        {
            //Create cmdlists
            _cmdList = _device.AllocateCommandList();

            //Create fence
            _fence = _device.CreateFence();

            //Create cameraq buffer resource layout
            Veldrid.ResourceLayoutDescription cameraBufferLayoutDesc = new Veldrid.ResourceLayoutDescription()
            {
                Elements = new Veldrid.ResourceLayoutElementDescription[]
                {
                    new Veldrid.ResourceLayoutElementDescription()
                    {
                        Kind = Veldrid.ResourceKind.UniformBuffer,
                        Name = "CameraBuffer",
                        Options = Veldrid.ResourceLayoutElementOptions.None,
                        Stages = Veldrid.ShaderStages.Vertex
                    }
                }

            };
            _cameraBufferResourceLayout = _device.CreateResourceLayout(cameraBufferLayoutDesc);

            //Create instace buffer resource layout
            Veldrid.ResourceLayoutDescription instanceBufferLayoutDesc = new Veldrid.ResourceLayoutDescription()
            {
                Elements = new Veldrid.ResourceLayoutElementDescription[]
              {
                    new Veldrid.ResourceLayoutElementDescription()
                    {
                        Kind = Veldrid.ResourceKind.StructuredBufferReadOnly,
                        Name = "InstanceBuffer",
                        Options = Veldrid.ResourceLayoutElementOptions.None,
                        Stages = Veldrid.ShaderStages.Vertex
                    }
              }

            };
            _instanceBufferResourceLayout = _device.CreateResourceLayout(instanceBufferLayoutDesc);

            //Create sampler resource layout
            Veldrid.ResourceLayoutDescription samplerLayoutDesc = new Veldrid.ResourceLayoutDescription()
            {
                Elements = new Veldrid.ResourceLayoutElementDescription[]
              {
                    new Veldrid.ResourceLayoutElementDescription()
                    {
                        Kind = Veldrid.ResourceKind.Sampler,
                        Name = "SpriteSampler",
                        Options = Veldrid.ResourceLayoutElementOptions.None,
                        Stages = Veldrid.ShaderStages.Fragment
                    }
              }

            };
            _samplerResourceLayout = _device.CreateResourceLayout(samplerLayoutDesc);

            //Create sprite resource layout
            Veldrid.ResourceLayoutDescription spriteResourceLayoutDesc = new Veldrid.ResourceLayoutDescription()
            {
                Elements = new Veldrid.ResourceLayoutElementDescription[]
                {
                    new Veldrid.ResourceLayoutElementDescription()
                    {
                        Kind = Veldrid.ResourceKind.TextureReadOnly,
                        Name = "SpriteTexture",
                        Options = Veldrid.ResourceLayoutElementOptions.None,
                        Stages = Veldrid.ShaderStages.Fragment
                    }
                }
            };
            _spriteResourceLayout = _device.CreateResourceLayout(spriteResourceLayoutDesc);

            //Create camera buffer
            Veldrid.BufferDescription cameraBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = false,
                SizeInBytes = 128, // 2x mat4
                StructureByteStride = 0,
                Usage = Veldrid.BufferUsage.UniformBuffer
            };
            _cameraDeviceBuffer = _device.AllocateBuffer(cameraBufferDesc);

            //Create instance buffer
            Veldrid.BufferDescription instanceBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = true,
                SizeInBytes = (uint)(_drawCallCacheSize * (64 + 16)),
                StructureByteStride = InstanceDataSize,
                Usage = Veldrid.BufferUsage.StructuredBufferReadOnly
            };
            _instanceDeviceBuffer = _device.AllocateBuffer(instanceBufferDesc);

            //Create camera buffer resource set
            Veldrid.ResourceSetDescription cameraBufferResourceSetDesc = new Veldrid.ResourceSetDescription()
            {
                BoundResources = new[]
                {
                    _cameraDeviceBuffer
                },
                Layout = _cameraBufferResourceLayout
            };
            _cameraBufferResourceSet = _device.CreateResourceSet(cameraBufferResourceSetDesc);

            //Create instance buffer resource set
            Veldrid.ResourceSetDescription instanceBufferResourceSetDesc = new Veldrid.ResourceSetDescription()
            {
                BoundResources = new[]
                {
                    _instanceDeviceBuffer
                },
                Layout = _instanceBufferResourceLayout
            };
            _instanceBufferResourceSet = _device.CreateResourceSet(instanceBufferResourceSetDesc);

            //Create camera stage buffer
            Veldrid.BufferDescription cameraStageBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = false,
                SizeInBytes = cameraBufferDesc.SizeInBytes,
                StructureByteStride = 0,
                Usage = Veldrid.BufferUsage.Staging
            };
            _cameraStageBuffer = _device.AllocateBuffer(cameraStageBufferDesc);

            //Create instance stage buffer
            Veldrid.BufferDescription instanceStageBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = false,
                SizeInBytes = instanceBufferDesc.SizeInBytes,
                StructureByteStride = 0,
                Usage = Veldrid.BufferUsage.Staging
            };
            _instanceStageBuffer = _device.AllocateBuffer(instanceStageBufferDesc);

            //Create shaders
            Veldrid.ShaderDescription vertexShaderDesc = new Veldrid.ShaderDescription()
            {
                Debug = true,
                EntryPoint = "main",
                ShaderBytes = Encoding.UTF8.GetBytes(SimpleSpriteShader.VertexShader),
                Stage = Veldrid.ShaderStages.Vertex
            };
            Veldrid.ShaderDescription fragmentShaderDesc = new Veldrid.ShaderDescription()
            {
                Debug = true,
                EntryPoint = "main",
                ShaderBytes = Encoding.UTF8.GetBytes(SimpleSpriteShader.FragmentShader),
                Stage = Veldrid.ShaderStages.Fragment
            };
            _shaderSet = _device.CompileVertexFragmentShader(vertexShaderDesc,fragmentShaderDesc);

            //Create vertex buffer
            Span<VertexData> vertexes = stackalloc VertexData[4]
            {
                new VertexData()
                {
                    Position = new Vector2(-1.0f,1.0f),
                    Uv = new Vector2(0,0)
                },
                new VertexData()
                {
                    Position = new Vector2(1.0f,1.0f),
                    Uv = new Vector2(1,0)
                },
                new VertexData()
                {
                    Position = new Vector2(1.0f,-1.0f),
                    Uv = new Vector2(1,1)
                },
                new VertexData()
                {
                    Position = new Vector2(-1.0f,-1.0f),
                    Uv = new Vector2(0,1)
                }
            };
            Veldrid.BufferDescription vertexBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = false,
                SizeInBytes = 16 * 4,
                StructureByteStride = 0,
                Usage = Veldrid.BufferUsage.VertexBuffer
            };
            _vertexBuffer = _device.AllocateBuffer(vertexBufferDesc);

            //Create index buffer
            Span<ushort> indexes = stackalloc ushort[6]
            {
                0,1,2,0,2,3
            };

            Veldrid.BufferDescription indexBufferDesc = new Veldrid.BufferDescription()
            {
                RawBuffer = false,
                SizeInBytes = (uint)(sizeof(ushort)*indexes.Length),
                StructureByteStride = 0,
                Usage = Veldrid.BufferUsage.IndexBuffer
            };
            _indexBuffer = _device.AllocateBuffer(indexBufferDesc);

            fixed(VertexData* pData = vertexes)
            {
                _device.UpdateBufferJIT(_vertexBuffer,(nuint)pData,0,vertexBufferDesc.SizeInBytes);
            }
            fixed(ushort* pData = indexes)
            {
                _device.UpdateBufferJIT(_indexBuffer,(nuint)pData,0,indexBufferDesc.SizeInBytes);
            }
        }

        private void RecreateGraphicsPipeline(Veldrid.Framebuffer targetFrambuffer)
        {
            //Create graphics pipeline
            Veldrid.GraphicsPipelineDescription pipelineDesc = new Veldrid.GraphicsPipelineDescription()
            {
                BlendState = Veldrid.BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new Veldrid.DepthStencilStateDescription()
                {
                    DepthComparison = Veldrid.ComparisonKind.Never,
                    DepthTestEnabled = false,
                    DepthWriteEnabled = false,
                    StencilBack = new Veldrid.StencilBehaviorDescription(),
                    StencilFront = new Veldrid.StencilBehaviorDescription(),
                    StencilReadMask = 0,
                    StencilReference = 0,
                    StencilTestEnabled = false,
                    StencilWriteMask = 0
                },
                Outputs = targetFrambuffer.OutputDescription,
                PrimitiveTopology = Veldrid.PrimitiveTopology.TriangleList,
                RasterizerState = new Veldrid.RasterizerStateDescription()
                {
                    CullMode = Veldrid.FaceCullMode.None,
                    DepthClipEnabled = false,
                    FillMode = Veldrid.PolygonFillMode.Solid,
                    FrontFace = Veldrid.FrontFace.CounterClockwise,
                    ScissorTestEnabled = false
                },
                ResourceBindingModel = Veldrid.ResourceBindingModel.Default,
                ResourceLayouts = new Veldrid.ResourceLayout[]
                {
                    _cameraBufferResourceLayout,
                    _instanceBufferResourceLayout,
                    _samplerResourceLayout,
                    _spriteResourceLayout
                },
                ShaderSet = new Veldrid.ShaderSetDescription()
                {
                    Shaders = _shaderSet,
                    VertexLayouts = new[]
                    {
                        new Veldrid.VertexLayoutDescription()
                        {
                            Elements = new Veldrid.VertexElementDescription[]
                            {
                                new Veldrid.VertexElementDescription()
                                {
                                    Format = Veldrid.VertexElementFormat.Float2,
                                    Name = "Position",
                                    Offset = 0,
                                    Semantic = Veldrid.VertexElementSemantic.Position
                                },
                                new Veldrid.VertexElementDescription()
                                {
                                    Format = Veldrid.VertexElementFormat.Float2,
                                    Name = "Uv",
                                    Offset = 8,
                                    Semantic = Veldrid.VertexElementSemantic.TextureCoordinate
                                }
                            },
                            InstanceStepRate = 0,
                            Stride = 16
                        }
                    }
                }
            };

            _pipeline = _device.CreateGraphicsPipeline(pipelineDesc);
        }

        private Veldrid.Fence _fence;
        private Veldrid.CommandList _cmdList;
        private Veldrid.Framebuffer _targetFramebuffer;
        private Veldrid.ResourceLayout _cameraBufferResourceLayout;
        private Veldrid.ResourceLayout _instanceBufferResourceLayout;
        private Veldrid.ResourceLayout _samplerResourceLayout;
        private Veldrid.ResourceLayout _spriteResourceLayout;
        private GraphicsDevice _device;

        private Veldrid.Sampler _sampler;
        private Veldrid.ResourceSet _samplerResourceSet;

        private Veldrid.DeviceBuffer _vertexBuffer;
        private Veldrid.DeviceBuffer _indexBuffer;

        private Veldrid.DeviceBuffer _cameraDeviceBuffer;
        private Veldrid.DeviceBuffer _instanceDeviceBuffer;
        private Veldrid.DeviceBuffer _cameraStageBuffer;
        private Veldrid.DeviceBuffer _instanceStageBuffer;

        private Veldrid.ResourceSet _cameraBufferResourceSet;
        private Veldrid.ResourceSet _instanceBufferResourceSet;
        private Veldrid.Pipeline _pipeline;
        private Veldrid.Shader[] _shaderSet;

        private CameraData _cameraData;
        private BatchCall[] _batches;
        private InstanceData[] _instanceCpuData;

        private int _drawCallCacheSize;
        private int _batchCallCacheSize;
        private int _currentDrawCallCount;
        private int _currentBatchCount;
        private List<Task> _currentTasks;
    }
}
