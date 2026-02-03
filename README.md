
# ExecMod

A utility mod for **Valheim** built with BepInEx. This mod provides an in-game console command:

`exec`

It allows running batch console commands from local text files.

## Installation

1.  Ensure you have [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
    
2.  Compile this and place `ExecMod.dll` into your `BepInEx/plugins` folder.
    
3.  Place your `.txt` files inside the `BepInEx/config/expand_world` folder OR the `BepInEx/config` folder.
    
    -   _Note: If a file with the same name exists in both locations, the version in **expand_world** takes priority._
        

## Usage

### The `exec` Command

Open the game console (**F5**) and use the following syntax:

`exec [filename.txt]`

**You can press Tab to autocomplete filenames for any .txt files found in the supported config folders.**

-   **File Format:** Your `.txt` files support anything that you could manually type in your Valheim console.
    
-   **Comments:** Lines starting with `#` are ignored, allowing you to document your scripts.
    
-   **Error Handling:** If the file is not found, the console will display a red error message: `[Exec] File not found: [filename]`.
    

#### Example:

Suppose that you want to do the following task (which is what I wanted to do and why I wrote the mod): Spawn a particular prefab, and use Jere Kuusela’s _World Edit Commands_ to do a data dump of this prefab's various fields.

You would do:

-   `spawn Blob` <— spawn a prefab, in this case a _Blob_
    
-   `data dump=Blob_data id=Blob radius=10` <— save the prefab’s data in the default Valheim’s location in a file called `data.yaml`.
    
-   `object id=Blob radius=10 remove` <— now that you are done with this spawn just remove it.
    

You would have in your `data.yaml` something that looks like:

> -   name: Blob_data_
>     
> 
> _ints:_
> 
> _- $onGround, 1_
> 
> _- CL&LC infusion, 0_
> 
> _- $statei, 1_
> 
> _- seed, 1063079624_
> 
> _- seAttrib, 0_
> 
> _- CL&LC effect, 6_
> 
> _- haveTarget, 0_
> 
> _- Humanoid.m_boss, 0_
> 
> _- Humanoid.m_dontHideBossHud, 0_
> 
> _- Humanoid.m_aiSkipTarget, 0_
> 
> _- Humanoid.m_canSwim, 0_
> 
> _- Humanoid.m_flying, 0_
> 
> …..
> 
> (a gazillion other stuff, for ints, then for floats etc.)
> 
> ….
> 
> _vecs:_
> 
> _- vel, 0,0,-7.6_
> 
> _- BodyVelocity, 0,0,-8.4_
> 
> _- spawnpoint, 284.514,-50.78159,32.39852_
> 
> _- CharacterDrop.m_spawnOffset, 0,0,0_
> 
> _persistent: true_
> 
> _distant: false_
> 
> _priority: Default

Now, what if you want to also do this for every other prefab in your game, including the modded ones? You would have to do it manually for each one. Each subsequent dump would append to the `data.yaml`.

If you have _Expand World Spawns_, then you have an `expand_spawns.yaml` in your `BepInEx/config`. In there you can find the prefab names of all your spawns - vanilla and those added by your mods. You could write a simple python script like so:

Python

```
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

```

You save this `.py` file in your `BepInEx/config/expand_world` folder as `extractPrefabs.py` for example.

You open a cmd prompt there and you do `python extractPrefabs.py`.

This will generate a text file called `dump_all.txt` that will have entries like so:

```
killall
removedrops
echo [SCRIPT] Starting Bulk Dump using Verified Syntax...

echo [PROCESS] Blob
spawn Blob
data dump=Blob_data id=Blob radius=10
object id=Blob radius=10 remove

echo [PROCESS] BlobElite
spawn BlobElite
data dump=BlobElite_data id=BlobElite radius=10
object id=BlobElite radius=10 remove
...

```

Now if you go in-game and you get into `debugmode`, press **Z** to fly, and fly over open sea, you can do:

`exec dump_all.txt`

This is equivalent to you typing manually in order:

-   `killall` <— kills everything around you
    
-   `removedrops` <— removes everything so you have a clean area
    
-   `echo [SCRIPT] Starting Bulk Dump using Verified Syntax...` <— prints this on the CLI locally
    
-   `echo [PROCESS] Blob` <— print this too
    
-   `spawn Blob` <— spawn a blob
    
-   `data dump=Blob_data id=Blob radius=10` <— do the data dump
    
-   `object id=Blob radius=10 remove` <— remove that blob… its gone now.
    

Rinse and repeat for each entry. Once complete, a green notification `[Exec] Batch Complete` will appear.

## Configuration

On first run, the mod generates a config file at `BepInEx/config/np.execmod.cfg`. You can adjust the following timing delays (in seconds) to suit your hardware:

-   **SpawnDelay (Default: 0.4):** Delay after a `spawn` command to manage engine overhead.
    
-   **DumpDelay (Default: 0.2):** Delay after `data dump` (World Edit Commands) or `physdump` (Physics Dump mod) commands to ensure file I/O completes.
    
-   **DefaultDelay (Default: 0.12):** Delay for all other commands (0 = wait 1 frame).
    

## License

MIT