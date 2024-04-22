using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;
using Veldrid;
using Veldrid.StartupUtilities;
using System.Numerics;

namespace Runtime.Windowing
{
    public sealed class Window : IDisposable
    {
        public Window(in WindowDesc desc)
        {
            //Allocate event data
            _bufferedEvents = new List<WindowEvent>(100);

            //Create window options
            WindowCreateInfo info = new WindowCreateInfo()
            {
                WindowTitle = desc.Title,
                WindowWidth = (int)desc.Size.X,
                WindowHeight = (int)desc.Size.Y,
                X = (int)desc.Offset.X,
                Y = (int)desc.Offset.Y
            };

            //Create window
            UnderlyingWindow = VeldridStartup.CreateWindow(info);

            //Setup events

        }

        public bool IsAlive => UnderlyingWindow.Exists;
        public IReadOnlyCollection<WindowEvent> BufferedEvents => _bufferedEvents;
        public Vector2 Size
        {
            get
            {
                return new Vector2(UnderlyingWindow.Width, UnderlyingWindow.Height);
            }
            set
            {
                UnderlyingWindow.Width = (int)value.X;
                UnderlyingWindow.Height = (int)value.Y;
            }
        }
        public Vector2 Position
        {
            get
            {
                return new Vector2(UnderlyingWindow.X, UnderlyingWindow.Y);
            }
            set
            {
                UnderlyingWindow.X = (int)value.X;
                UnderlyingWindow.Y = (int)value.Y;
            }
        }
        public WindowState State
        {
            get
            {
                return UnderlyingWindow.WindowState;
            }
            set
            {
                UnderlyingWindow.WindowState = value;
            }
        }

        internal Veldrid.Sdl2.Sdl2Window UnderlyingWindow { get; private init; }

        public void PollEvents()
        {
            _bufferedEvents.Clear();
            UnderlyingWindow.PumpEvents();
        }

        public void Dispose()
        {
            //Reset events
            UnderlyingWindow.Closed -= OnWindowClosed;
            UnderlyingWindow.Resized -= OnWindowResized;
            UnderlyingWindow.Moved -= OnWindowMoved;
            UnderlyingWindow.MouseMove -= OnMouseMoved;
            UnderlyingWindow.MouseDown -= OnMouseDown;
            UnderlyingWindow.MouseUp -= OnMouseUp;
            UnderlyingWindow.MouseWheel -= OnMouseWheel;
            UnderlyingWindow.KeyDown -= OnKeyDown;
            UnderlyingWindow.KeyUp -= OnKeyUp;
            UnderlyingWindow.DragDrop -= OnDragDrop;
        }
        private void OnWindowClosed()
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.WindowClosed
            };
            DispatchEvent(instance);
        }
        private void OnWindowMoved(Point pos)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.WindowMoved,
                WindowPosition = new System.Numerics.Vector2(pos.X, pos.Y)
            };
            DispatchEvent(instance);

        }
        private void OnWindowResized()
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.WindowResized,
                WindowSize = new System.Numerics.Vector2(UnderlyingWindow.Width, UnderlyingWindow.Height)
            };
            DispatchEvent(instance);
        }
        private void OnMouseMoved(MouseMoveEventArgs e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.MouseMoved,
                MousePosition = e.MousePosition
            };
            DispatchEvent(instance);
        }
        private void OnMouseDown(MouseEvent e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.MouseButtonDown,
                MouseButton = e.MouseButton
            };
            DispatchEvent(instance);
        }
        private void OnMouseUp(MouseEvent e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.MouseButtonDown,
                MouseButton = e.MouseButton
            };
            DispatchEvent(instance);
        }
        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.MouseScrolled,
                MouseWheelDelta = e.WheelDelta
            };
            DispatchEvent(instance);
        }
        private void OnKeyDown(KeyEvent e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.KeyboardDown,
                KeyboardKey = e.Key
            };
            DispatchEvent(instance);
        }
        private void OnKeyUp(KeyEvent e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.KeyboardDown,
                KeyboardKey = e.Key
            };
            DispatchEvent(instance);
        }
        private void OnDragDrop(DragDropEvent e)
        {
            WindowEvent instance = new WindowEvent()
            {
                Type = WindowEventType.DragDrop,
                DropItem = e.File
            };
            DispatchEvent(instance);
        }

        private void DispatchEvent(in WindowEvent e)
        {
            _bufferedEvents.Add(e);
        }

        private List<WindowEvent> _bufferedEvents;
    }
}
