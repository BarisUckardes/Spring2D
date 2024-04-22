using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Runtime.Windowing
{
    public struct WindowDesc
    {
        public Vector2 Offset;
        public Vector2 Size;
        public string Title;
        public WindowState InitialState;
    }
}
