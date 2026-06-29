using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PitLife.Rendering;

namespace PitLife.Core;

public class InputManager
{
    public KeyboardState CurrentKbd { get; private set; }
    public KeyboardState PrevKbd { get; private set; }
    public MouseState CurrentMouse { get; private set; }
    public MouseState PrevMouse { get; private set; }

    public void Update()
    {
        PrevKbd = CurrentKbd;
        PrevMouse = CurrentMouse;
        CurrentKbd = Keyboard.GetState();
        CurrentMouse = Mouse.GetState();
    }

    public bool IsKeyJustPressed(Keys key)
    {
        return CurrentKbd.IsKeyDown(key) && PrevKbd.IsKeyUp(key);
    }

    public bool IsLeftClickJustPressed()
    {
        return CurrentMouse.LeftButton == ButtonState.Pressed && PrevMouse.LeftButton == ButtonState.Released;
    }

    public bool IsGamepadBackPressed()
    {
        return GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
    }

    public void UpdateCamera(Camera camera, float dt)
    {
        camera.HandleInput(dt);
    }
}
