using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Rendering
{
    public struct SpriteBoundingBox
    {
        public static SpriteBoundingBox Identity = new SpriteBoundingBox()
        {
            Min = new Vector2(0, 0),
            Max = new Vector2(1, 1)
        };

        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }
    }
}
