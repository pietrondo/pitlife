import re

with open('Simulation/Systems/CataclysmSystem.cs', 'r') as f:
    content = f.read()

# Fix switch case
old_switch = """<<<<<<< HEAD
            DrawAsteroid(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Supervolcano")
        {
            DrawSupervolcano(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Firestorm")
        {
            DrawFirestorm(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent is "IceAge" or "Ice Age")
        {
            DrawIceAge(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Earthquake")
        {
            DrawEarthquake(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Drought")
        {
            DrawDrought(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Flood")
        {
            DrawFlood(sb, pixel, pos, maxR, progress);
        }
        else if (ActiveEvent == "Bloom")
        {
            DrawBloom(sb, pixel, pos, maxR, progress);
        }
=======
            case "Asteroid":
            case "Asteroid Impact":
                DrawAsteroidEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Supervolcano":
                DrawSupervolcanoEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Firestorm":
                DrawFirestormEvent(sb, pixel, pos, maxR, progress);
                break;
            case "IceAge":
            case "Ice Age":
                DrawIceAgeEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Earthquake":
                DrawEarthquakeEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Drought":
                DrawDroughtEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Flood":
                DrawFloodEvent(sb, pixel, pos, maxR, progress);
                break;
            case "Bloom":
                DrawBloomEvent(sb, pixel, pos, maxR, progress);
                break;
>>>>>>> origin/master"""

new_switch = """            case "Asteroid":
            case "Asteroid Impact":
                DrawAsteroid(sb, pixel, pos, maxR, progress);
                break;
            case "Supervolcano":
                DrawSupervolcano(sb, pixel, pos, maxR, progress);
                break;
            case "Firestorm":
                DrawFirestorm(sb, pixel, pos, maxR, progress);
                break;
            case "IceAge":
            case "Ice Age":
                DrawIceAge(sb, pixel, pos, maxR, progress);
                break;
            case "Earthquake":
                DrawEarthquake(sb, pixel, pos, maxR, progress);
                break;
            case "Drought":
                DrawDrought(sb, pixel, pos, maxR, progress);
                break;
            case "Flood":
                DrawFlood(sb, pixel, pos, maxR, progress);
                break;
            case "Bloom":
                DrawBloom(sb, pixel, pos, maxR, progress);
                break;"""
content = content.replace(old_switch, new_switch)


old_asteroid = """<<<<<<< HEAD
    private void DrawAsteroid(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawAsteroidEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_asteroid, "    private void DrawAsteroid(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_supervolcano = """<<<<<<< HEAD
    private void DrawSupervolcano(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawSupervolcanoEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_supervolcano, "    private void DrawSupervolcano(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_firestorm = """<<<<<<< HEAD
    private void DrawFirestorm(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawFirestormEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_firestorm, "    private void DrawFirestorm(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_iceage = """<<<<<<< HEAD
    private void DrawIceAge(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawIceAgeEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_iceage, "    private void DrawIceAge(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_earthquake = """<<<<<<< HEAD
    private void DrawEarthquake(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawEarthquakeEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_earthquake, "    private void DrawEarthquake(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_earthquake_inner = """<<<<<<< HEAD

            // Draw 2 smaller branching lines at the midpoint
            float midX = (pos.X + endX) / 2f;
            float midY = (pos.Y + endY) / 2f;
            float perpAngle1 = angle + 1.57f;
            float perpAngle2 = angle - 1.57f;
            float branchLen = crackR * 0.3f;
            float branch1X = midX + MathF.Cos(perpAngle1) * branchLen;
            float branch1Y = midY + MathF.Sin(perpAngle1) * branchLen;
            float branch2X = midX + MathF.Cos(perpAngle2) * branchLen;
            float branch2Y = midY + MathF.Sin(perpAngle2) * branchLen;
            DrawLine(sb, pixel, new Vector2(midX, midY), new Vector2(branch1X, branch1Y), new Color(100, 80, 60, (int)120), 1);
            DrawLine(sb, pixel, new Vector2(midX, midY), new Vector2(branch2X, branch2Y), new Color(100, 80, 60, (int)120), 1);
        }
    }

    private void DrawDrought(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
        }
    }

    private void DrawDroughtEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""

new_earthquake_inner = """
            // Draw 2 smaller branching lines at the midpoint
            float midX = (pos.X + endX) / 2f;
            float midY = (pos.Y + endY) / 2f;
            float perpAngle1 = angle + 1.57f;
            float perpAngle2 = angle - 1.57f;
            float branchLen = crackR * 0.3f;
            float branch1X = midX + MathF.Cos(perpAngle1) * branchLen;
            float branch1Y = midY + MathF.Sin(perpAngle1) * branchLen;
            float branch2X = midX + MathF.Cos(perpAngle2) * branchLen;
            float branch2Y = midY + MathF.Sin(perpAngle2) * branchLen;
            DrawLine(sb, pixel, new Vector2(midX, midY), new Vector2(branch1X, branch1Y), new Color(100, 80, 60, (int)120), 1);
            DrawLine(sb, pixel, new Vector2(midX, midY), new Vector2(branch2X, branch2Y), new Color(100, 80, 60, (int)120), 1);
        }
    }

    private void DrawDrought(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)"""
content = content.replace(old_earthquake_inner, new_earthquake_inner)


old_flood = """<<<<<<< HEAD
    private void DrawFlood(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawFloodEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_flood, "    private void DrawFlood(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_flood_inner = """<<<<<<< HEAD
            rr += MathF.Sin(progress * 10f + r) * (maxR * 0.05f); // create rippling waves
=======
>>>>>>> origin/master"""
content = content.replace(old_flood_inner, "            rr += MathF.Sin(progress * 10f + r) * (maxR * 0.05f); // create rippling waves")

old_bloom = """<<<<<<< HEAD
    private void DrawBloom(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
=======
    private void DrawBloomEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)
>>>>>>> origin/master"""
content = content.replace(old_bloom, "    private void DrawBloom(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress)")

old_end = """<<<<<<< HEAD
=======

>>>>>>> origin/master"""
content = content.replace(old_end, "")

with open('Simulation/Systems/CataclysmSystem.cs', 'w') as f:
    f.write(content)
print("Resolved conflicts")
