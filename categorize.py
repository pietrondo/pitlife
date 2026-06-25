import re

content = open('Simulation/Entities/BuiltinSpecies.cs', 'r').read()

plants = re.findall(r'RegisterPlant\([^;]+;', content)
aquatic_plants = re.findall(r'RegisterAquaticPlant\([^;]+;', content)
animals = re.findall(r'RegisterAnimal\([^;]+;', content)

print(f"Found {len(plants)} land plants, {len(aquatic_plants)} aquatic plants, {len(animals)} animals.")
