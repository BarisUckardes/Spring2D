using StbiSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Runtime.Graphics
{
    /// <summary>
    /// Simple graphics utils
    /// </summary>
    public static class GraphicsUtils
    {
        /// <summary>
        /// Loads the texture from the path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public unsafe static Texture LoadTexture(string path,GraphicsDevice device)
        {
            //Check
            if(!File.Exists(path))
                throw new FileNotFoundException("Texture file failed to found");

            //Load
            byte[] bytes = File.ReadAllBytes(path);

            //Create image from memory
            StbiImage image = Stbi.LoadFromMemory(bytes, 4);

            //Allocate texture
            TextureDescription desc = new TextureDescription()
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                Depth = 1,
                ArrayLayers = 1,
                MipLevels = 1,
                Format = PixelFormat.R8_G8_B8_A8_UNorm,
                SampleCount = TextureSampleCount.Count1,
                Type = TextureType.Texture2D,
                Usage = TextureUsage.Sampled
            };
            Texture texture = device.AllocateTexture(desc);

            //Update texture JIT
            fixed(byte* pByte = image.Data)
            {
                device.UpdateTextureJIT(texture, (nuint)pByte, (uint)(image.Width*image.Height*4), 0,0,0, (uint)image.Width, (uint)image.Height,1,0,0);
            }

            return texture;
        }
    }
}
