using System;
using System.Diagnostics;
using System.Linq;
using SimStarter.Core;

#nullable enable

namespace SimStarter
{
    internal static class Program
    {
        private static ProfilesConfig _config = null!;

        private static void Main()
        {
            Console.Title = "Sim Starter (CLI)";
            _config = ProfilesStore.LoadOrCreate();

            while (true)
            {
                ShowMainMenu();
                var input = ReadNonEmpty("Choice");

                switch (input.ToLowerInvariant())
                {
                    case "q":
                        return;
                    case "s":
                        StartStarter();
                        break;
                    case "r":
                        _config = ProfilesStore.LoadOrCreate();
                        break;
                    case "o":
                        OpenConfigInEditor();
                        break;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }

        private static void ShowMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=== Sim Starter ===");
            Console.WriteLine("Profiles file: " + ProfilesStore.GetConfigPath());
            Console.WriteLine();

            if (_config.Starters.Count == 0)
            {
                Console.WriteLine("No starters configured yet. Use the WPF app to set them up.");
            }
            else
            {
                Console.WriteLine("Starters:");
                for (var i = 0; i < _config.Starters.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {_config.Starters[i].Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  s - Start a starter profile");
            Console.WriteLine("  r - Reload profiles.json");
            Console.WriteLine("  o - Open profiles.json in editor");
            Console.WriteLine("  q - Quit");
        }

        // --- Starter operations ------------------------------------------------

        private static void StartStarter()
        {
            if (_config.Starters.Count == 0)
            {
                Console.WriteLine("No starters to start. Use the WPF app to configure.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--- Start starter ---");
            for (var i = 0; i < _config.Starters.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_config.Starters[i].Name}");
            }

            var str = ReadNonEmpty("Starter number (or 'b' to cancel)");
            if (str.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!int.TryParse(str, out var index))
            {
                Console.WriteLine("Invalid number.");
                return;
            }

            index -= 1;
            if (index < 0 || index >= _config.Starters.Count)
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            var starter = _config.Starters[index];
            if (string.IsNullOrWhiteSpace(starter.SimId))
            {
                Console.WriteLine("Starter has no sim configured.");
                return;
            }

            StarterRunner.RunStarter(_config, starter);
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey(intercept: true);
        }

        // --- Utilities ---------------------------------------------------------

        private static string ReadNonEmpty(string label)
        {
            while (true)
            {
                Console.Write(label + ": ");
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s))
                    return s.Trim();

                Console.WriteLine("Value cannot be empty.");
            }
        }

        private static void OpenConfigInEditor()
        {
            var path = ProfilesStore.GetConfigPath();

            try
            {
                Console.WriteLine("Opening config: " + path);
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open config: " + ex.Message);
            }
        }
    }
}
