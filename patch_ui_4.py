import re

with open('UI/InGameUi.cs', 'rb') as f:
    content = f.read().decode('utf-8')

# Add StringBuilders
stringbuilders = """
    private readonly StringBuilder _tempSb = new StringBuilder(32);
"""
content = re.sub(r'    private readonly StringBuilder _creatureLineageSb = new StringBuilder\(128\);\r?\n', r'    private readonly StringBuilder _creatureLineageSb = new StringBuilder(128);\n' + stringbuilders, content)

# Fix I18n format calls to not allocate using StringBuilder where simple interpolation was used
# Let's fix the tempLabel interpolation:
temp_orig = r'        string tempLabel = \$\"\{\(int\)\(20f \+ climate\.TemperatureModifier \* 20f\)\}°C\";\r?\n        float tempNorm = Math\.Clamp\(\(climate\.TemperatureModifier \+ 0\.15f\) / 0\.3f, 0f, 1f\);\r?\n        DrawLine\(sb, font, content\.X, y, I18n\.Format\("climate\.temperature", tempLabel\),\r?\n            Color\.Lerp\(new Color\(100, 150, 255\), new Color\(255, 120, 40\), tempNorm\)\);'

temp_repl = """        _tempSb.Clear();
        _tempSb.Append((int)(20f + climate.TemperatureModifier * 20f)).Append("°C");
        float tempNorm = Math.Clamp((climate.TemperatureModifier + 0.15f) / 0.3f, 0f, 1f);
        DrawLine(sb, font, content.X, y, I18n.Format("climate.temperature", _tempSb.ToString()),
            Color.Lerp(new Color(100, 150, 255), new Color(255, 120, 40), tempNorm));"""
content = re.sub(temp_orig, temp_repl, content)


with open('UI/InGameUi.cs', 'wb') as f:
    f.write(content.encode('utf-8'))
