using Microsoft.Xna.Framework.Input;
using PitLife.UI;

namespace PitLife.Tests;

public class UiTextInputTests
{
    [Fact]
    public void TextInput_AcceptsAssetPathPunctuation()
    {
        var input = new UiTextInput { IsFocused = true, MaxLength = 20 };
        var mouse = new MouseState();
        var empty = new KeyboardState();

        Press(input, Keys.A, empty, mouse);
        Press(input, Keys.OemQuestion, empty, mouse);
        Press(input, Keys.OemPeriod, empty, mouse);
        Press(input, new[] { Keys.LeftShift, Keys.OemMinus }, empty, mouse);

        Assert.Equal("a/._", input.Text);
    }

    private static void Press(UiTextInput input, Keys key, KeyboardState empty, MouseState mouse) =>
        Press(input, [key], empty, mouse);

    private static void Press(
        UiTextInput input,
        Keys[] keys,
        KeyboardState empty,
        MouseState mouse)
    {
        input.Update(new KeyboardState(keys), empty, mouse, mouse);
        input.Update(empty, new KeyboardState(keys), mouse, mouse);
    }
}
