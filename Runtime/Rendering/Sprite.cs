using Runtime.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Rendering
{
    /// <summary>
    /// Sprite class it holds the target data and the target regions which to enable support for sprite atlases
    /// </summary>
    public sealed class Sprite : IDisposable
    {
        public Sprite(GraphicsDevice device)
        {
            //Set default bounds
            Bounds = new SpriteBoundingBox()
            {
                Min = new System.Numerics.Vector2()
                {
                    X = 0,
                    Y = 0,
                },
                Max = new System.Numerics.Vector2()
                {
                    X = 0,
                    Y = 0,
                }
            };

            _device = device;
        }

        /// <summary>
        /// The target texture
        /// </summary>
        public Veldrid.Texture? Texture
        {
            get
            {
                return _texture;
            }
            set
            {
                _texture = value;

                Veldrid.ResourceSetDescription setDesc = new Veldrid.ResourceSetDescription()
                {
                    BoundResources = new[]
                {
                        value
                },
                    Layout = _device.SpriteLayout 
                };
                ResourceSet = _device.CreateResourceSet(setDesc);
            }
        }

        /// <summary>
        /// The resource set for binding to the graphics pipeline
        /// </summary>
        public Veldrid.ResourceSet? ResourceSet { get; private set; }

        /// <summary>
        /// The bounding box of the sprite
        /// </summary>
        public SpriteBoundingBox Bounds { get; set; }

        public void Dispose()
        {
            ResourceSet?.Dispose();
        }

        private GraphicsDevice _device;
        private Veldrid.Texture? _texture;
    }
}
