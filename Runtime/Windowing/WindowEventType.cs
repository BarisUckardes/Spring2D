using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Windowing
{
    public enum WindowEventType
    {
        None,

        WindowClosed,
        WindowMoved,
        WindowResized,

        DragDrop,

        KeyboardDown,
        KeyboardUp,
        Char,

        MouseButtonDown,
        MouseButtonUp,
        MouseMoved,
        MouseScrolled,

        GamepadButtonDown,
        GamepadButtonUp,
        GamepadTriggerMove,
        GamepadThumbMove
    }
}
