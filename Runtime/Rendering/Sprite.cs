using Runtime.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Rendering
{
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
        public Veldrid.ResourceSet? ResourceSet { get; private set; }
        public SpriteBoundingBox Bounds { get; set; }
        public void Dispose()
        {

        }

        private GraphicsDevice _device;
        private Veldrid.Texture? _texture;
    }
}
