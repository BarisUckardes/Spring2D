using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Runtime.Windowing
{
    /// <summary>
    /// Descriptor for to create a new window
    /// </summary>
    public struct WindowDesc
    {
        public Vector2 Offset;
        public Vector2 Size;
        public string Title;
        public WindowState InitialState;
    }
}
