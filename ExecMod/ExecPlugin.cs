/*
 Nick Pappas Feb 2026
 */
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/*
 Simple mod to add an 'exec' command to the in-game console, allowing batch execution of commands from a text file.
 Commands are read line-by-line with timing delays to prevent engine overload, especially for spawn and data commands.
 I should probably add some kind of settings to configure the delays later, but this is good enough for now.
 */
namespace ExecMod
{
    // Define the plugin metadata: GUID, Name, and Version
    [BepInPlugin("np.execmod", "Exec Command Mod", "0.2.0")]
    public class ExecPlugin : BaseUnityPlugin
    {
        // Initialize Harmony for manual patching and store a static instance for Coroutine access
        private readonly Harmony _harmony = new Harmony("np.execmod");
        private static ExecPlugin? Instance;

        // Config entries for adjustable delays
        public static ConfigEntry<float>? SpawnDelay;
        public static ConfigEntry<float>? DataDelay;
        public static ConfigEntry<float>? DefaultDelay;

        void Awake()
        {
            // Set singleton instance and apply all Harmony patches within this assembly
            Instance = this;

            // Setup Configuration - binds to bepinex/config/np.execmod.cfg
            SpawnDelay = Config.Bind("Delays", "SpawnDelay", 0.4f, "Delay after a 'spawn' command (seconds). Original testing showed 0.28f was okay, 0.4f is safer.");
            DumpDelay = Config.Bind("Delays", "DataDelay", 0.2f, "Delay after 'data dump' or 'physdump' commands (seconds). Ensures file I/O (like YAML writing) completes.");
            DefaultDelay = Config.Bind("Delays", "DefaultDelay", 0.12f, "Delay for all other commands (seconds). 0 means one frame wait.");

            _harmony.PatchAll();
            Logger.LogInfo("Command 'exec' is registered.");
        }

        // Helper to find files: check 'config/expand_world/' first, then fall back to the base 'config/' folder
        private static string? GetFilePath(string filename)
        {
            string path = Path.Combine(Paths.ConfigPath, "expand_world", filename);
            if (File.Exists(path)) return path;

            path = Path.Combine(Paths.ConfigPath, filename);
            if (File.Exists(path)) return path;

            return null;
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
                new Terminal.ConsoleCommand("exec", "Executes commands from a script (include the .txt)", (Terminal.ConsoleEventArgs args) =>
                {
                    // Ensure a filename argument is provided
                    if (args.Length < 2) return;

                    string filename = args.Args[1];
                    string? fullPath = GetFilePath(filename);

                    if (fullPath != null)
                    {
                        // If the file exists, initiate the batch execution via a Coroutine to handle timing
                        Instance?.StartCoroutine(ExecuteBatch(File.ReadAllLines(fullPath), args.Context));
                    }
                    else
                    {
                        // Provide feedback if the file is missing
                        args.Context.AddString($"<color=red>[Exec] File not found: {filename}</color>");
                    }
                },
                optionsFetcher: () =>
                {
                    try
                    {
                        List<string> files = new List<string>();
                        string mainConfig = Paths.ConfigPath;
                        string expandWorld = Path.Combine(Paths.ConfigPath, "expand_world");

                        if (Directory.Exists(expandWorld))
                        {
                            files.AddRange(Directory.GetFiles(expandWorld, "*.txt")
                                .Select(Path.GetFileName)
                                .Where(name => !string.IsNullOrEmpty(name))!);
                        }

                        if (Directory.Exists(mainConfig))
                        {
                            files.AddRange(Directory.GetFiles(mainConfig, "*.txt")
                                .Select(Path.GetFileName)
                                .Where(name => !string.IsNullOrEmpty(name))!);
                        }

                        return files.Distinct().ToList();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ExecMod] Autocomplete failed: {ex.Message}");
                        return new List<string>();
                    }
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

                string lowerCmd = cmd.ToLower();

                // Timing Logic: Apply specific delays based on the command type to ensure stability
                if (lowerCmd.StartsWith("spawn "))
                {
                    // Longer delay for entity spawning to manage engine overhead
                    yield return new WaitForSeconds(SpawnDelay?.Value ?? 0.4f);
                }
                else if (lowerCmd.StartsWith("data dump") || lowerCmd.StartsWith("physdump "))
                {
                    // Moderate delay for data commands to ensure file I/O (like YAML writing) completes. 
                    yield return new WaitForSeconds(DumpDelay?.Value ?? 0.2f); 
                }
                else
                {
                    // Default to a configurable delay or single frame wait for all other commands
                    float d = DefaultDelay?.Value ?? 0.12f;
                    if (d > 0) yield return new WaitForSeconds(d);
                    else yield return null;
                }
            }
            // Notify the user in the console once the entire batch file has been processed
            context.AddString("<color=green>[Exec] Batch Complete. All commands processed.</color>");
        }
    }
}