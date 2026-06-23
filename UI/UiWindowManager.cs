using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI;

public sealed class UiWindowManager
{
    private readonly List<UiWindow> _windows = new();
    private UiWindow? _draggedWindow;
    private Point _dragOffset;

    private System.DateTime _lastClickTime = System.DateTime.MinValue;
    private string? _lastClickedWindowId;

    public IReadOnlyList<UiWindow> Windows => _windows;

    public void Add(UiWindow window) => _windows.Add(window);

    public bool IsActive(UiWindow window)
    {
        if (!window.IsOpen) return false;
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].IsOpen)
                return _windows[i] == window;
        }
        return false;
    }

    public void TileWindows(int viewportWidth, int viewportHeight)
    {
        int startX = 32;
        int startY = 88;
        int gap = 16;
        int currentX = startX;
        int currentY = startY;
        int maxRowHeight = 0;

        foreach (UiWindow window in _windows)
        {
            if (!window.IsOpen)
                continue;

            // If the window exceeds viewport width, wrap to next row
            if (currentX + window.Bounds.Width > viewportWidth - 32 && currentX > startX)
            {
                currentX = startX;
                currentY += maxRowHeight + gap;
                maxRowHeight = 0;
            }

            window.Bounds = new Rectangle(currentX, currentY, window.Bounds.Width, window.Bounds.Height);
            currentX += window.Bounds.Width + gap;
            if (window.Bounds.Height > maxRowHeight)
                maxRowHeight = window.Bounds.Height;
        }

        // Clamp them after tiling to ensure they are inside viewport bounds
        ClampWindows(viewportWidth, viewportHeight);
    }

    public void Toggle(string id, int viewportWidth = 0, int viewportHeight = 0)
    {
        UiWindow? window = Find(id);
        if (window == null)
            return;

        if (window.IsOpen && IsActive(window))
        {
            window.IsOpen = false;
        }
        else
        {
            CloseAllWindows();
            window.IsOpen = true;
            BringToFront(window);
            if (viewportWidth > 0 && viewportHeight > 0)
                TileWindows(viewportWidth, viewportHeight);
        }
    }

    public void Open(string id, int viewportWidth = 0, int viewportHeight = 0)
    {
        UiWindow? window = Find(id);
        if (window == null)
            return;

        CloseAllWindows();
        window.IsOpen = true;
        BringToFront(window);

        if (viewportWidth > 0 && viewportHeight > 0)
            TileWindows(viewportWidth, viewportHeight);
    }

    public void CloseAllWindows()
    {
        foreach (UiWindow w in _windows)
            w.IsOpen = false;
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

                // Double click detection on title bar to collapse/expand
                var now = System.DateTime.UtcNow;
                var elapsed = now - _lastClickTime;
                bool isDoubleClick = _lastClickedWindowId == window.Id && elapsed.TotalMilliseconds < 350;
                _lastClickTime = now;
                _lastClickedWindowId = window.Id;

                if (window.ShowCloseButton && window.CloseButtonBounds.Contains(mouse.Position))
                {
                    window.IsOpen = false;
                    return true;
                }

                if (window.IsDraggable && window.TitleBarBounds.Contains(mouse.Position))
                {
                    if (isDoubleClick)
                    {
                        window.ToggleCollapse();
                        _draggedWindow = null;
                        return true;
                    }

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
        // Allow the window to be dragged partially off-screen,
        // but keep the title bar clickable. Title bar height is 40px.
        int minX = -bounds.Width + 40;
        int maxX = viewportWidth - 40;
        int minY = 56; // Keep below top HUD
        int maxY = viewportHeight - 56 - 40; // Keep above toolbar

        return new Rectangle(
            System.Math.Clamp(bounds.X, minX, maxX),
            System.Math.Clamp(bounds.Y, minY, System.Math.Max(minY, maxY)),
            bounds.Width,
            bounds.Height);
    }
}
