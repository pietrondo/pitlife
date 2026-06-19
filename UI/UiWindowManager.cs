using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI;

public sealed class UiWindowManager
{
    private readonly List<UiWindow> _windows = new();
    private UiWindow? _draggedWindow;
    private Point _dragOffset;

    public IReadOnlyList<UiWindow> Windows => _windows;

    public void Add(UiWindow window) => _windows.Add(window);

    public void Toggle(string id)
    {
        UiWindow? window = Find(id);
        if (window == null)
            return;

        window.IsOpen = !window.IsOpen;
        if (window.IsOpen)
            BringToFront(window);
    }

    public void Open(string id)
    {
        UiWindow? window = Find(id);
        if (window == null)
            return;

        window.IsOpen = true;
        BringToFront(window);
    }

    public bool CloseTopWindow()
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (!_windows[i].IsOpen)
                continue;

            _windows[i].IsOpen = false;
            return true;
        }

        return false;
    }

    public bool Update(MouseState mouse, MouseState previousMouse, int viewportWidth, int viewportHeight)
    {
        ClampWindows(viewportWidth, viewportHeight);

        if (_draggedWindow != null)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                _draggedWindow.Bounds = Clamp(
                    new Rectangle(
                        mouse.X - _dragOffset.X,
                        mouse.Y - _dragOffset.Y,
                        _draggedWindow.Bounds.Width,
                        _draggedWindow.Bounds.Height),
                    viewportWidth,
                    viewportHeight);
                return true;
            }

            _draggedWindow = null;
        }

        bool pressed = mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        if (pressed)
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                UiWindow window = _windows[i];
                if (!window.IsOpen || !window.Bounds.Contains(mouse.Position))
                    continue;

                BringToFront(window);
                if (window.ShowCloseButton && window.CloseButtonBounds.Contains(mouse.Position))
                {
                    window.IsOpen = false;
                    return true;
                }

                if (window.IsDraggable && window.TitleBarBounds.Contains(mouse.Position))
                {
                    _draggedWindow = window;
                    _dragOffset = new Point(mouse.X - window.Bounds.X, mouse.Y - window.Bounds.Y);
                }

                return true;
            }
        }

        return IsPointerOverWindow(mouse.Position);
    }

    public bool IsPointerOverWindow(Point point)
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].IsOpen && _windows[i].Bounds.Contains(point))
                return true;
        }

        return false;
    }

    private UiWindow? Find(string id) => _windows.Find(window => window.Id == id);

    private void BringToFront(UiWindow window)
    {
        _windows.Remove(window);
        _windows.Add(window);
    }

    private void ClampWindows(int viewportWidth, int viewportHeight)
    {
        foreach (UiWindow window in _windows)
            window.Bounds = Clamp(window.Bounds, viewportWidth, viewportHeight);
    }

    private static Rectangle Clamp(Rectangle bounds, int viewportWidth, int viewportHeight)
    {
        int maxX = System.Math.Max(8, viewportWidth - bounds.Width - 8);
        int maxY = System.Math.Max(56, viewportHeight - bounds.Height - 64);
        return new Rectangle(
            System.Math.Clamp(bounds.X, 8, maxX),
            System.Math.Clamp(bounds.Y, 56, maxY),
            bounds.Width,
            bounds.Height);
    }
}
