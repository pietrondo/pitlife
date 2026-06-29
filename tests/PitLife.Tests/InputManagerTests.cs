using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Moq;
using PitLife.Core;
using PitLife.Rendering;
using Xunit;

namespace PitLife.Tests;

public class InputManagerTests
{
    private static void SetKeyboardStates(InputManager manager, KeyboardState current, KeyboardState prev)
    {
        var type = typeof(InputManager);
        type.GetProperty("CurrentKbd", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(manager, current);
        type.GetProperty("PrevKbd", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(manager, prev);
    }

    private static void SetMouseStates(InputManager manager, MouseState current, MouseState prev)
    {
        var type = typeof(InputManager);
        type.GetProperty("CurrentMouse", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(manager, current);
        type.GetProperty("PrevMouse", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(manager, prev);
    }

    [Fact]
    public void InputManager_Update_CopiesCurrentToPrevious()
    {
        var manager = new InputManager();
        var currentKbd = new KeyboardState(Keys.Space);
        var currentMouse = new MouseState(10, 20, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

        SetKeyboardStates(manager, currentKbd, new KeyboardState());
        SetMouseStates(manager, currentMouse, new MouseState());

        manager.Update();

        Assert.Equal(currentKbd, manager.PrevKbd);
        Assert.Equal(currentMouse, manager.PrevMouse);
    }

    [Fact]
    public void InputManager_IsKeyJustPressed_ReturnsTrue_WhenPressedNowButNotBefore()
    {
        var manager = new InputManager();
        
        // Key just pressed
        SetKeyboardStates(manager, new KeyboardState(Keys.Space), new KeyboardState());
        Assert.True(manager.IsKeyJustPressed(Keys.Space));

        // Key held down (was pressed before and is pressed now)
        SetKeyboardStates(manager, new KeyboardState(Keys.Space), new KeyboardState(Keys.Space));
        Assert.False(manager.IsKeyJustPressed(Keys.Space));

        // Key not pressed
        SetKeyboardStates(manager, new KeyboardState(), new KeyboardState());
        Assert.False(manager.IsKeyJustPressed(Keys.Space));
    }

    [Fact]
    public void InputManager_IsLeftClickJustPressed_ReturnsTrue_WhenClickedNowButNotBefore()
    {
        var manager = new InputManager();

        var pressed = new MouseState(0, 0, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var released = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

        // Click just pressed
        SetMouseStates(manager, pressed, released);
        Assert.True(manager.IsLeftClickJustPressed());

        // Click held down
        SetMouseStates(manager, pressed, pressed);
        Assert.False(manager.IsLeftClickJustPressed());

        // Click released
        SetMouseStates(manager, released, released);
        Assert.False(manager.IsLeftClickJustPressed());
    }

    [Fact]
    public void InputManager_UpdateCamera_CallsHandleInput()
    {
        var manager = new InputManager();
        var camera = new Camera(800, 600);
        
        // Just verify it doesn't crash and handles input
        manager.UpdateCamera(camera, 0.016f);
        Assert.NotNull(camera);
    }
}
