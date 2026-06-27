import re

with open('UI/InGameUi.cs', 'rb') as f:
    content = f.read().decode('utf-8')

# Add namespace
content = re.sub(r'using System;\r?\n', 'using System;\nusing System.Text;\n', content)

# Add StringBuilders
stringbuilders = """
    private readonly StringBuilder _speedSb = new StringBuilder(16);
    private readonly StringBuilder _valueSb = new StringBuilder(16);
    private readonly StringBuilder _speciesSb = new StringBuilder(64);
    private readonly StringBuilder _creatureGenSb = new StringBuilder(128);
    private readonly StringBuilder _creatureLineageSb = new StringBuilder(128);
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

# Replace DrawInlineBar to not be static, and remove ToString
inline_orig = r'    private static void DrawInlineBar\(SpriteBatch sb, Texture2D pixel, SpriteFont font,\r?\n        int x, int y, string label, int value, int total, Color color\)\r?\n    \{\r?\n        int barW = \(int\)\(120f \* \(total > 0 \? value / \(float\)total : 0\)\);\r?\n        int barH = 10;\r?\n        sb\.DrawString\(font, label, new Vector2\(x, y - 2\), color\);\r?\n        var bg = new Rectangle\(x \+ 14, y \+ 1, 122, barH\);\r?\n        UiPrimitives\.Fill\(sb, pixel, bg, UiTheme\.DeepGrove\);\r?\n        if \(barW > 0\)\r?\n            UiPrimitives\.Fill\(sb, pixel, new Rectangle\(bg\.X, bg\.Y, barW, barH\), color\);\r?\n        UiPrimitives\.Border\(sb, pixel, bg, 1, UiTheme\.BarkEdge\);\r?\n        sb\.DrawString\(font, value\.ToString\(\), new Vector2\(bg\.Right \+ 4, y - 2\), UiTheme\.WarmParchment\);\r?\n    \}'

inline_repl = """    private void DrawInlineBar(SpriteBatch sb, Texture2D pixel, SpriteFont font,
        int x, int y, string label, int value, int total, Color color)
    {
        int barW = (int)(120f * (total > 0 ? value / (float)total : 0));
        int barH = 10;
        sb.DrawString(font, label, new Vector2(x, y - 2), color);
        var bg = new Rectangle(x + 14, y + 1, 122, barH);
        UiPrimitives.Fill(sb, pixel, bg, UiTheme.DeepGrove);
        if (barW > 0)
            UiPrimitives.Fill(sb, pixel, new Rectangle(bg.X, bg.Y, barW, barH), color);
        UiPrimitives.Border(sb, pixel, bg, 1, UiTheme.BarkEdge);

        _valueSb.Clear();
        _valueSb.Append(value);
        sb.DrawString(font, _valueSb, new Vector2(bg.Right + 4, y - 2), UiTheme.WarmParchment);
    }"""
content = re.sub(inline_orig, inline_repl, content)

# Remove static from DrawStatistics
stats_orig = r'    private static int DrawStatistics\('
stats_repl = r'    private int DrawStatistics('
content = re.sub(stats_orig, stats_repl, content)

# Replace SpeciesList DrawString
species_orig = r'                spriteBatch\.DrawString\(font, \$\"\{kvp\.Value\} \{I18n\.Species\(kvp\.Key\)\}\",\r?\n                    new Vector2\(content\.X, y\), col\);'
species_repl = """                _speciesSb.Clear();
                _speciesSb.Append(kvp.Value).Append(' ').Append(I18n.Species(kvp.Key));
                spriteBatch.DrawString(font, _speciesSb, new Vector2(content.X, y), col);"""
content = re.sub(species_orig, species_repl, content)


# Add a DrawLine overload that takes a StringBuilder
drawline_sb = """    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, StringBuilder text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

"""
content = re.sub(r'    private static void DrawLine\(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color\)\r?\n    \{\r?\n        spriteBatch\.DrawString\(font, text, new Vector2\(x, y\), color\);\r?\n    \}',
    r'    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)\n    {\n        spriteBatch.DrawString(font, text, new Vector2(x, y), color);\n    }\n\n' + drawline_sb, content)

# Remove static from DrawCreature and replace lineage strings
drawcreature_orig = r'    private static void DrawCreature\(\r?\n        SpriteBatch spriteBatch,\r?\n        Texture2D pixel,\r?\n        SpriteFont font,\r?\n        Rectangle content,\r?\n        Creature\? creature\)'
drawcreature_repl = r'    private void DrawCreature(\n        SpriteBatch spriteBatch,\n        Texture2D pixel,\n        SpriteFont font,\n        Rectangle content,\n        Creature? creature)'
content = re.sub(drawcreature_orig, drawcreature_repl, content)

lineage_orig = r'        string lineageText = lineage\.ParentAId > 0\r?\n            \? \$\"Parents: \[\{lineage\.ParentAId\}, \{lineage\.ParentBId\}\]  \|  ID: \{lineage\.IndividualId\}\"\r?\n            : \$\"ID: \{lineage\.IndividualId\}  \|  Founder\";\r?\n        DrawLine\(spriteBatch, font, content\.X, content\.Y \+ 362, lineageText, UiTheme\.WarmParchment\);\r?\n        DrawLine\(spriteBatch, font, content\.X, content\.Y \+ 382,\r?\n            \$\"Ancestors: \{lineage\.AncestorDepths\.Count\}  \|  MaxGen: \{genDepth\}  \|  Inbreeding: \{creature\.InbreedingCoefficient:F3\}  \|  Fitness: \{creature\.GeneticFitness:F2\}\",\r?\n            new Color\(200, 180, 140\)\);'

lineage_repl = """        _creatureLineageSb.Clear();
        if (lineage.ParentAId > 0)
        {
            _creatureLineageSb.Append("Parents: [").Append(lineage.ParentAId).Append(", ").Append(lineage.ParentBId).Append("]  |  ID: ").Append(lineage.IndividualId);
        }
        else
        {
            _creatureLineageSb.Append("ID: ").Append(lineage.IndividualId).Append("  |  Founder");
        }
        DrawLine(spriteBatch, font, content.X, content.Y + 362, _creatureLineageSb, UiTheme.WarmParchment);

        _creatureGenSb.Clear();
        _creatureGenSb.Append("Ancestors: ").Append(lineage.AncestorDepths.Count).Append("  |  MaxGen: ").Append(genDepth)
            .Append("  |  Inbreeding: ").Append(Math.Round(creature.InbreedingCoefficient, 3))
            .Append("  |  Fitness: ").Append(Math.Round(creature.GeneticFitness, 2));
        DrawLine(spriteBatch, font, content.X, content.Y + 382, _creatureGenSb, new Color(200, 180, 140));"""
content = re.sub(lineage_orig, lineage_repl, content)

with open('UI/InGameUi.cs', 'wb') as f:
    f.write(content.encode('utf-8'))
