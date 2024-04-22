using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Windowing
{
    /// <summary>
    /// Supported event types
    /// </summary>
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
