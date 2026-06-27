import re

with open('UI/InGameUi.cs', 'rb') as f:
    content = f.read().decode('utf-8')


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

# Remove static from DrawTerrain and replace format strings
drawterrain_orig = r'    private static void DrawTerrain\(\r?\n        SpriteBatch spriteBatch,\r?\n        Texture2D pixel,\r?\n        SpriteFont font,\r?\n        Rectangle content,\r?\n        World\? World,\r?\n        Point\? SelectedTile\)'
drawterrain_repl = r'    private void DrawTerrain(\n        SpriteBatch spriteBatch,\n        Texture2D pixel,\n        SpriteFont font,\n        Rectangle content,\n        World? World,\n        Point? SelectedTile)'
content = re.sub(drawterrain_orig, drawterrain_repl, content)

# Replace DrawString for species in DrawCreature
species2_orig = r'        spriteBatch\.DrawString\(font, creatureInfo, new Vector2\(content\.X, content\.Y \+ 40\), UiTheme\.WarmParchment\);'
content = re.sub(species2_orig, r'        spriteBatch.DrawString(font, creatureInfo, new Vector2(content.X, content.Y + 40), UiTheme.WarmParchment);', content)

with open('UI/InGameUi.cs', 'wb') as f:
    f.write(content.encode('utf-8'))
