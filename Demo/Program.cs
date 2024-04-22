using Runtime.Graphics;
using Runtime.Rendering;
using Runtime.Windowing;

namespace Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Create window
            Console.WriteLine("Creating window...");
            WindowDesc windowDesc = new WindowDesc()
            {
                Offset = new System.Numerics.Vector2(100, 100),
                Size = new System.Numerics.Vector2(1024, 1024),
                InitialState = Veldrid.WindowState.Normal,
                Title = "Spring2D Demo"
            };
            Window window = new Window(windowDesc);

            //Create graphics device
            Console.WriteLine("Creating graphics device...");
            GraphicsDevice device = new GraphicsDevice(window, true, true);

            //Load texture
            Console.WriteLine("Loading textures...");
            Veldrid.Texture texture0 = GraphicsUtils.LoadTexture("D:\\Resources\\Textures\\SceneAspectIcon.png", device);
            Veldrid.Texture texture1 = GraphicsUtils.LoadTexture("D:\\Resources\\Textures\\SceneEntityIcon.png", device);
            Veldrid.Texture texture2 = GraphicsUtils.LoadTexture("D:\\Resources\\Textures\\SceneIcon.png", device);
            Veldrid.Texture texture3 = GraphicsUtils.LoadTexture("D:\\Resources\\Textures\\ShaderIcon.png", device);
            Veldrid.Texture texture4 = GraphicsUtils.LoadTexture("D:\\Resources\\Textures\\smiley.png", device);

            //Create sprites
            Console.Write("Loading sprites...");
            Sprite sprite0 = new Sprite(device);
            sprite0.Texture = texture0;
            Sprite sprite1 = new Sprite(device);
            sprite1.Texture = texture1;
            Sprite sprite2 = new Sprite(device);
            sprite2.Texture = texture2;
            Sprite sprite3 = new Sprite(device);
            sprite3.Texture = texture3;
            Sprite sprite4 = new Sprite(device);
            sprite4.Texture = texture4;

            //Create renderer
            SpriteRenderer renderer = new SpriteRenderer(device);

            //Set sampler
            renderer.SetSampler(device.LinearSampler);

            //Set camera data
            renderer.SetCameraData(device.MainSwapchain.Framebuffer, new System.Numerics.Vector2(0, 0), 5.0f, 0.1f, 10.0f);

            //Loop
            while (window.IsAlive)
            {
                //Poll events
                window.PollEvents();
                if (!window.IsAlive)
                    break;

                //Populate draw call
                renderer.Draw(sprite0, new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(1, 1), 0,SpriteBoundingBox.Identity);
                renderer.Draw(sprite0, new System.Numerics.Vector2(1, 0), new System.Numerics.Vector2(1, 1), 0,SpriteBoundingBox.Identity);
                renderer.Draw(sprite0, new System.Numerics.Vector2(-1, 0), new System.Numerics.Vector2(1, 1), 0,SpriteBoundingBox.Identity);
                renderer.Draw(sprite0, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 1), 0,SpriteBoundingBox.Identity);
                renderer.Draw(sprite0, new System.Numerics.Vector2(0, -1), new System.Numerics.Vector2(1, 1), 0,SpriteBoundingBox.Identity);

                renderer.Draw(sprite1, new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(1, 1), 0, SpriteBoundingBox.Identity);
                renderer.Draw(sprite1, new System.Numerics.Vector2(0.5f, 0), new System.Numerics.Vector2(1, 1), 0, SpriteBoundingBox.Identity);
                renderer.Draw(sprite1, new System.Numerics.Vector2(-0.5f, 0), new System.Numerics.Vector2(1, 1), 0, SpriteBoundingBox.Identity);
                renderer.Draw(sprite1, new System.Numerics.Vector2(0, 0.5f), new System.Numerics.Vector2(1, 1), 0, SpriteBoundingBox.Identity);
                renderer.Draw(sprite1, new System.Numerics.Vector2(0, -0.5f), new System.Numerics.Vector2(1, 1), 0, SpriteBoundingBox.Identity);

                //Render
                renderer.Render(true,Veldrid.RgbaFloat.CornflowerBlue,1000);

                //Present
                device.Present();
            }
        }
    }
}
