import re

with open('UI/InGameUi.cs', 'rb') as f:
    content = f.read().decode('utf-8')


# Replace string interpolation in DrawStatistics
time_orig = r'        DrawLine\(spriteBatch, font, content\.X, y, I18n\.Format\("stats\.time", time\), UiTheme\.WarmParchment\);'
# InGameUi.cs DrawLine expects a string, so we'd better make a DrawLine that takes a StringBuilder.
