using BepInEx;
using HarmonyLib;
using System.Collections;
using System.IO;
using UnityEngine;

/*
 Simple mod to add an 'exec' command to the in-game console, allowing batch execution of commands from a text file.
 Commands are read line-by-line with timing delays to prevent engine overload, especially for spawn and data commands.
 I should probably add some kind of settings to configure the delays later, but this is good enough for now.
 */
namespace ExecMod
{
    // Define the plugin metadata: GUID, Name, and Version
    [BepInPlugin("com.nickpappas.execmod", "Exec Command Mod", "0.1.1")]
    public class ExecPlugin : BaseUnityPlugin
    {
        // Initialize Harmony for manual patching and store a static instance for Coroutine access
        private readonly Harmony _harmony = new Harmony("com.nickpappas.execmod");
        private static ExecPlugin? Instance;

        void Awake()
        {
            // Set singleton instance and apply all Harmony patches within this assembly
            Instance = this;
            _harmony.PatchAll();
            Logger.LogInfo("Command 'exec' is registered.");
        }

        // Patch the Terminal's initialization method to register the custom 'exec' command
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        public static class Terminal_InitTerminal_Patch
        {
            static void Postfix()
            {
                // Prevent duplicate command registration if InitTerminal runs multiple times
                if (Terminal.commands.ContainsKey("exec")) return;

                // Register 'exec' as a new terminal command
                new Terminal.ConsoleCommand("exec", "Executes commands from a script in config/expand_world (include the .txt)", (Terminal.ConsoleEventArgs args) =>
                {
                    // Ensure a filename argument is provided
                    if (args.Length < 2) return;

                    // Resolve path: check 'config/expand_world/' first, then fall back to the base 'config/' folder
                    string path = Path.Combine(Paths.ConfigPath, "expand_world", args.Args[1]);
                    if (!File.Exists(path)) path = Path.Combine(Paths.ConfigPath, args.Args[1]);

                    // If the file exists, initiate the batch execution via a Coroutine to handle timing
                    if (File.Exists(path)) Instance?.StartCoroutine(ExecuteBatch(File.ReadAllLines(path), args.Context));
                });
            }
        }

        // Coroutine to process the file line-by-line without freezing the game engine
        private static IEnumerator ExecuteBatch(string[] lines, Terminal context)
        {
            foreach (string line in lines)
            {
                string cmd = line.Trim();

                // Ignore empty lines and comment lines starting with '#'
                if (string.IsNullOrEmpty(cmd) || cmd.StartsWith("#")) continue;

                // Handle 'echo' locally to print messages to the console.
                if (cmd.ToLower().StartsWith("echo "))
                {
                    context.AddString(cmd.Substring(5));
                    continue;
                }

                // Execute the command through the game's internal console system
                Console.instance.TryRunCommand(cmd);

                // Timing Logic: Apply specific delays based on the command type to ensure stability
                if (cmd.ToLower().StartsWith("spawn "))
                {
                    // Longer delay for entity spawning to manage engine overhead
                    yield return new WaitForSeconds(0.4f); // Slightly more buffer for safety. For use with data dump this was working fine with 0.28f. I 'm just too cautious here.
                }
                else if (cmd.ToLower().StartsWith("data "))
                {
                    // Moderate delay for data commands to ensure file I/O (like YAML writing) completes. For people who are not on super fast SSDs this might be necessary and actually a bit increased?
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    // Default to a single frame wait for all other commands
                    yield return null;
                }
            }
            // Notify the user in the console once the entire batch file has been processed
            context.AddString("<color=green>[Exec] Batch Complete. All YAMLs generated.</color>");
        }
    }
}