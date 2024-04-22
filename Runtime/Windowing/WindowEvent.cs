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
    /// The data which holds the per event information
    /// </summary>
    public struct WindowEvent
    {
        public WindowEventType Type;

        public Vector2 WindowSize;
        public Vector2 WindowPosition;

        public MouseButton MouseButton;
        public Vector2 MousePosition;
        public float MouseWheelDelta;

        public Key KeyboardKey;

        public string DropItem;
    }
}
