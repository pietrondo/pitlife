import re

content = open('Simulation/Entities/BuiltinSpecies.cs', 'r').read()

plants = len(re.findall(r'RegisterPlant\([^;]+;', content))
aquatic_plants = len(re.findall(r'RegisterAquaticPlant\([^;]+;', content))
animals = len(re.findall(r'RegisterAnimal\([^;]+;', content))

# Check lines per method
lines = content.split('\n')
methods = {}
current_method = None
count = 0
for line in lines:
    m = re.match(r'\s*private static void (Register[a-zA-Z]+)\(\)', line)
    if m:
        current_method = m.group(1)
        count = 0
    elif current_method and line.strip() == '}':
        methods[current_method] = count
        current_method = None
    elif current_method:
        count += 1

print(f"Total plants: {plants}")
print(f"Total aquatic plants: {aquatic_plants}")
print(f"Total animals: {animals}")
print("Method sizes:")
for method, size in methods.items():
    print(f"  {method}: {size} lines")
