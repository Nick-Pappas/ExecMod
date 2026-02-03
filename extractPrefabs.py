#The python code I am using in the example.
import re

input_file = 'expand_spawns.yaml'
output_file = 'dump_all.txt'

try:
    with open(input_file, 'r') as f:
        content = f.read()
    
    # Extract unique prefabs from the yaml file using regex
    prefabs = sorted(list(set(re.findall(r'prefab:\s*(\w+)', content))))
    
    with open(output_file, 'w') as f:
        # Initial cleanup to ensure a clean slate
        f.write("killall\n")
        f.write("removedrops\n")
        f.write("echo [SCRIPT] Starting Bulk Dump using Verified Syntax...\n\n")
        
        for name in prefabs:
            # Visual feedback in console
            f.write(f"echo [PROCESSING] {name}\n")
            # Instantiate the prefab
            f.write(f"spawn {name}\n")
            # Dump the data into data.yaml (Radius 10 to ensure we catch the spawn)
            f.write(f"data dump={name}_data id={name} radius=10\n")
            # Remove the object immediately after dumping to save performance
            f.write(f"object id={name} radius=10 remove\n\n")
        
        f.write("echo [FINISHED] Check BepInEx/config/expand_world/ for the .yaml files.\n")
    
    print(f"Success! Created {output_file} with {len(prefabs)} prefabs.")

except FileNotFoundError:
    print(f"Error: Could not find {input_file}")
