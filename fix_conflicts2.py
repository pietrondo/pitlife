import re

with open('Simulation/Systems/CataclysmSystem.cs', 'r') as f:
    content = f.read()

# Let's fix this properly. The file contains a bunch of HEAD / master stuff that wasn't properly replaced.
# Let's clean the entire `switch (ActiveEvent)` block down to `DrawRing`

start_idx = content.find("        switch (ActiveEvent)")
end_idx = content.find("        // Generic impact ring for all types")

if start_idx != -1 and end_idx != -1:
    new_switch = """        switch (ActiveEvent)
        {
            case "Asteroid":
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
                break;
        }

"""
    content = content[:start_idx] + new_switch + content[end_idx:]

# Now let's remove any remaining conflict markers.
content = re.sub(r'<<<<<<< HEAD\n', '', content)
content = re.sub(r'=======\n', '', content)
content = re.sub(r'>>>>>>> origin/master\n', '', content)

with open('Simulation/Systems/CataclysmSystem.cs', 'w') as f:
    f.write(content)
print("Resolved conflicts")
