using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public static class SimpleSpriteShader
    {
        public const string VertexShader = @"
        #version 450

        layout(location = 0) in vec2 Position;
        layout(location = 1) in vec2 Uv;
        layout(location = 0) out vec2 UvOut;
        
        layout(set = 0,binding = 0) uniform CameraBuffer
        {
            mat4 ProjectionMatrix;
            mat4 ViewMatrix;
        };
        struct InstanceData
        {
            mat4 ModelMatrix;
            vec2 MinUv;
            vec2 MaxUv;
        };
        layout(std140,set = 1,binding = 0) readonly buffer InstanceBuffer
        {
            InstanceData Instances[];
        };
        void main()
        {
            mat4 mvp = Instances[gl_InstanceIndex].ModelMatrix*ViewMatrix*ProjectionMatrix;
            gl_Position = mvp*vec4(Position,0,1);
            UvOut = vec2(
                    mix(Instances[gl_InstanceIndex].MinUv.x,Instances[gl_InstanceIndex].MaxUv.x,Uv.x),
                    mix(Instances[gl_InstanceIndex].MinUv.y,Instances[gl_InstanceIndex].MaxUv.y,Uv.y)
                    );
        }
    ";

        public const string FragmentShader = @"
        #version 450

        layout(location = 0) in vec2 Uv;
        layout(location = 0) out vec4 ColorOut;


        layout(set = 2,binding = 0) uniform sampler SpriteSampler;
        layout(set = 3,binding = 0) uniform texture2D SpriteTexture;

        void main()
        {
            ColorOut = texture(sampler2D(SpriteTexture,SpriteSampler),Uv);
        }
    ";


    }
}
