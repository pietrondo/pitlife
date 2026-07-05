with open('UI/MainMenu.cs', 'r') as f:
    content = f.read()

# Already handled in code:
# if (_showOptions && Pressed(keyboard, previousKeyboard, Keys.Escape))
# {
#     _showOptions = false;
#     _focusedIndex = 4; // Focus Options button in main menu
#     return MenuAction.None;
# }

print("Escape is already handled.")
