import re

with open('UI/InGameUi.cs', 'rb') as f:
    content = f.read().decode('utf-8')

# Add using System.Text;
content = re.sub(r'using System;\r?\n', 'using System;\nusing System.Text;\n', content)

# Add StringBuilders
stringbuilders = """
    private readonly StringBuilder _speedSb = new StringBuilder(16);
    private readonly StringBuilder _valueSb = new StringBuilder(16);
    private readonly StringBuilder _speciesSb = new StringBuilder(64);
    private readonly StringBuilder _creatureGenSb = new StringBuilder(128);
    private readonly StringBuilder _creatureLineageSb = new StringBuilder(128);
    private readonly StringBuilder _tempSb = new StringBuilder(32);
"""
content = re.sub(r'public string\? SelectedCataclysm \{ get; set; \}\r?\n', 'public string? SelectedCataclysm { get; set; }\n' + stringbuilders, content)

# Replace DrawString for speed label
speed_orig = r'        var speedLabel = paused \? I18n\.T\("hud\.paused"\) : \$\"\{speed:0\.#\}x\";\r?\n        var slSize = font\.MeasureString\(speedLabel\);\r?\n        var sx = _speedDownButton\.Bounds\.Right \+ \(_speedUpButton\.Bounds\.X - _speedDownButton\.Bounds\.Right\) / 2f - slSize\.X / 2f;\r?\n        var sy = _speedDownButton\.Bounds\.Center\.Y - slSize\.Y / 2;\r?\n        spriteBatch\.DrawString\(font, speedLabel, new Vector2\(sx, sy\), Color\.White\);'

speed_repl = """        _speedSb.Clear();
        if (paused) _speedSb.Append(I18n.T("hud.paused"));
        else { _speedSb.Append(Math.Round(speed, 1)); _speedSb.Append('x'); }
        var slSize = font.MeasureString(_speedSb);
        var sx = _speedDownButton.Bounds.Right + (_speedUpButton.Bounds.X - _speedDownButton.Bounds.Right) / 2f - slSize.X / 2f;
        var sy = _speedDownButton.Bounds.Center.Y - slSize.Y / 2;
        spriteBatch.DrawString(font, _speedSb, new Vector2(sx, sy), Color.White);"""
content = re.sub(speed_orig, speed_repl, content)

# Replace DrawInlineBar ToString
inline_orig = r'        sb\.DrawString\(font, value\.ToString\(\), new Vector2\(bg\.Right \+ 4, y - 2\), UiTheme\.WarmParchment\);'
inline_repl = """        _valueSb.Clear();
        _valueSb.Append(value);
        sb.DrawString(font, _valueSb, new Vector2(bg.Right + 4, y - 2), UiTheme.WarmParchment);"""
content = re.sub(inline_orig, inline_repl, content)

# Replace SpeciesList DrawString
species_orig = r'                spriteBatch\.DrawString\(font, \$\"\{kvp\.Value\} \{I18n\.Species\(kvp\.Key\)\}\",\r?\n                    new Vector2\(content\.X, y\), col\);'
species_repl = """                _speciesSb.Clear();
                _speciesSb.Append(kvp.Value).Append(' ').Append(I18n.Species(kvp.Key));
                spriteBatch.DrawString(font, _speciesSb, new Vector2(content.X, y), col);"""
content = re.sub(species_orig, species_repl, content)

with open('UI/InGameUi.cs', 'wb') as f:
    f.write(content.encode('utf-8'))
