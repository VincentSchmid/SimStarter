using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SimStarter.Core;
using MessageBox = System.Windows.MessageBox;
using WpfApp = System.Windows;

namespace SimStarter.UI
{
    public partial class MainWindow : Window
    {
        private ProfilesConfig _config = null!;
        private bool _isRunning;
        public string VersionLabel { get; }

        private const string RepoOwner = "VincentSchmid";
        private const string RepoName = "SimStarter";

        public MainWindow()
        {
            InitializeComponent();
            VersionLabel = $"v{VersionProvider.GetVersionString()}";
            DataContext = this;
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = ProfilesStore.LoadOrCreate();
            RefreshSims();
            RefreshAddons();
            RefreshStarters();
        }

        // Sims tab
        private void RefreshSims()
        {
            SimsList.ItemsSource = null;
            SimsList.ItemsSource = _config.Sims;
        }

        private void AddSim_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AppEditorWindow("Add Sim");
            if (dialog.ShowDialog() == true)
            {
                var sim = ToSim(dialog.Result);
                sim.Id = Guid.NewGuid().ToString();
                _config.Sims.Add(sim);
                RefreshSims();
                SaveConfig();
                RefreshStarterSimChoices();
            }
        }

        private void EditSim_Click(object sender, RoutedEventArgs e)
        {
            if (SimsList.SelectedItem is not SimApp sim) return;
            var dialog = new AppEditorWindow("Edit Sim", ToCatalog(sim));
            if (dialog.ShowDialog() == true)
            {
                sim.Name = dialog.Result.Name;
                sim.Path = dialog.Result.Path;
                sim.Arguments = dialog.Result.Arguments;
                sim.RunAsAdmin = dialog.Result.RunAsAdmin;
                sim.WaitForExit = dialog.Result.WaitForExit;
                RefreshSims();
                SaveConfig();
                RefreshStarters();
            }
        }

        private void RemoveSim_Click(object sender, RoutedEventArgs e)
        {
            if (SimsList.SelectedItem is not SimApp sim) return;
            if (MessageBox.Show($"Remove sim '{sim.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            _config.Sims.Remove(sim);
            // Remove references from starters
            foreach (var starter in _config.Starters.Where(s => s.SimId == sim.Id))
            {
                starter.SimId = string.Empty;
            }
            RefreshSims();
            RefreshStarters();
            SaveConfig();
        }

        // Addons tab
        private void RefreshAddons()
        {
            AddonsList.ItemsSource = null;
            AddonsList.ItemsSource = _config.Addons;
            RefreshStarterAddonChoices();
        }

        private void AddAddon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AppEditorWindow("Add Addon");
            if (dialog.ShowDialog() == true)
            {
                var addon = ToAddon(dialog.Result);
                addon.Id = Guid.NewGuid().ToString();
                _config.Addons.Add(addon);
                RefreshAddons();
                SaveConfig();
            }
        }

        private void EditAddon_Click(object sender, RoutedEventArgs e)
        {
            if (AddonsList.SelectedItem is not AddonApp addon) return;
            var dialog = new AppEditorWindow("Edit Addon", ToCatalog(addon));
            if (dialog.ShowDialog() == true)
            {
                addon.Name = dialog.Result.Name;
                addon.Path = dialog.Result.Path;
                addon.Arguments = dialog.Result.Arguments;
                addon.RunAsAdmin = dialog.Result.RunAsAdmin;
                addon.WaitForExit = dialog.Result.WaitForExit;
                RefreshAddons();
                SaveConfig();
                RefreshStarters();
            }
        }

        private void RemoveAddon_Click(object sender, RoutedEventArgs e)
        {
            if (AddonsList.SelectedItem is not AddonApp addon) return;
            if (MessageBox.Show($"Remove addon '{addon.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            _config.Addons.Remove(addon);
            foreach (var starter in _config.Starters)
            {
                starter.AddonIds.Remove(addon.Id);
            }
            RefreshAddons();
            RefreshStarters();
            SaveConfig();
        }

        // Starters tab
        private void RefreshStarters(string? keepSelectedId = null)
        {
            keepSelectedId ??= (StartersList.SelectedItem as StarterProfile)?.Id;
            StartersList.ItemsSource = null;
            StartersList.ItemsSource = _config.Starters;
            RefreshStarterSimChoices();
            RefreshStarterAddonChoices();
            if (!string.IsNullOrWhiteSpace(keepSelectedId))
            {
                var match = _config.Starters.FirstOrDefault(s => s.Id == keepSelectedId);
                if (match != null)
                {
                    StartersList.SelectedItem = match;
                }
                else
                {
                    StartersList.SelectedIndex = _config.Starters.Count > 0 ? 0 : -1;
                }
            }
            else
            {
                StartersList.SelectedIndex = _config.Starters.Count > 0 ? 0 : -1;
            }
            DisplaySelectedStarter();
        }

        private void StartersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DisplaySelectedStarter();
        }

        private void StarterSimBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // No-op; selection applied on save
        }

        private void AddStarter_Click(object sender, RoutedEventArgs e)
        {
            var starter = new StarterProfile
            {
                Name = "New Starter",
                Id = Guid.NewGuid().ToString()
            };
            if (_config.Sims.Any())
            {
                starter.SimId = _config.Sims.First().Id;
            }
            _config.Starters.Add(starter);
            RefreshStarters(starter.Id);
            SaveConfig();
        }

        private void RemoveStarter_Click(object sender, RoutedEventArgs e)
        {
            if (StartersList.SelectedItem is not StarterProfile starter) return;
            if (MessageBox.Show($"Remove starter '{starter.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            _config.Starters.Remove(starter);
            RefreshStarters(starter.Id);
            SaveConfig();
        }

        private void SaveStarter_Click(object sender, RoutedEventArgs e)
        {
            if (StartersList.SelectedItem is not StarterProfile starter) return;

            starter.Name = StarterNameBox.Text?.Trim() ?? starter.Name;
            if (StarterSimBox.SelectedItem is SimApp sim)
            {
                starter.SimId = sim.Id;
            }
            starter.AddonIds = StarterAddonsBox.Items.OfType<StarterAddonChoice>()
                .Where(a => a.IsSelected)
                .Select(a => a.Addon.Id)
                .ToList();

            RefreshStarters(starter.Id);
            SaveConfig();
        }

        private void StartProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;
            if (StartersList.SelectedItem is not StarterProfile starter) return;

            _isRunning = true;
            AppendLog($"Starting '{starter.Name}'...");
            Task.Run(() => StarterRunner.RunStarter(_config, starter, AppendLogFromBackground))
                .ContinueWith(_ =>
                {
                    _isRunning = false;
                    AppendLog("Done.");
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void DesktopShortcut_Click(object sender, RoutedEventArgs e)
        {
            CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        }

        private void StartMenuShortcut_Click(object sender, RoutedEventArgs e)
        {
            var programsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "SimStarter");
            Directory.CreateDirectory(programsDir);
            CreateShortcut(programsDir);
        }

        private void CreateShortcut(string folder)
        {
            if (StartersList.SelectedItem is not StarterProfile starter)
            {
                MessageBox.Show("Select a starter first.", "No starter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
                var safeName = MakeSafeFileName(starter.Name);
                var path = Path.Combine(folder, $"SimStarter - {safeName}.lnk");
                var sim = _config.Sims.FirstOrDefault(s => s.Id == starter.SimId);
                var addons = _config.Addons.Where(a => starter.AddonIds.Contains(a.Id));
                if (sim == null)
                {
                    MessageBox.Show("Starter has no sim configured.", "No sim", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ShortcutService.CreateProfileShortcut(starter.Id, starter.Name, sim, addons, path);
                AppendLog($"Shortcut created: {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to create shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplaySelectedStarter()
        {
            if (StartersList.SelectedItem is not StarterProfile starter)
            {
                StarterNameBox.Text = string.Empty;
                StarterSimBox.ItemsSource = null;
                StarterAddonsBox.ItemsSource = null;
                return;
            }

            StarterNameBox.Text = starter.Name;
            RefreshStarterSimChoices();
            RefreshStarterAddonChoices(starter);
        }

        private void RefreshStarterSimChoices()
        {
            var current = StartersList.SelectedItem as StarterProfile;
            StarterSimBox.ItemsSource = null;
            StarterSimBox.ItemsSource = _config.Sims;
            if (current != null && !string.IsNullOrWhiteSpace(current.SimId))
            {
                var match = _config.Sims.FirstOrDefault(s => s.Id == current.SimId);
                StarterSimBox.SelectedItem = match;
            }
        }

        private void RefreshStarterAddonChoices(StarterProfile? starter = null)
        {
            starter ??= StartersList.SelectedItem as StarterProfile;
            var selections = new List<StarterAddonChoice>();
            foreach (var addon in _config.Addons)
            {
                selections.Add(new StarterAddonChoice
                {
                    Addon = addon,
                    IsSelected = starter?.AddonIds.Contains(addon.Id) == true
                });
            }
            StarterAddonsBox.ItemsSource = selections;
        }

        private static CatalogItem ToCatalog(SimApp sim) => new CatalogItem
        {
            Id = sim.Id,
            Name = sim.Name,
            Path = sim.Path,
            Arguments = sim.Arguments,
            RunAsAdmin = sim.RunAsAdmin,
            WaitForExit = sim.WaitForExit
        };

        private static CatalogItem ToCatalog(AddonApp addon) => new CatalogItem
        {
            Id = addon.Id,
            Name = addon.Name,
            Path = addon.Path,
            Arguments = addon.Arguments,
            RunAsAdmin = addon.RunAsAdmin,
            WaitForExit = addon.WaitForExit
        };

        private static SimApp ToSim(CatalogItem item) => new SimApp
        {
            Id = item.Id,
            Name = item.Name,
            Path = item.Path,
            Arguments = item.Arguments,
            RunAsAdmin = item.RunAsAdmin,
            WaitForExit = item.WaitForExit
        };

        private static AddonApp ToAddon(CatalogItem item) => new AddonApp
        {
            Id = item.Id,
            Name = item.Name,
            Path = item.Path,
            Arguments = item.Arguments,
            RunAsAdmin = item.RunAsAdmin,
            WaitForExit = item.WaitForExit
        };

        private void SaveConfig()
        {
            ProfilesStore.Save(_config);
            AppendLog("Config saved.");
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ProfilesStore.GetConfigPath(),
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to open config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Checking for updates...");
            if (!Version.TryParse(VersionProvider.GetVersionString(), out var currentVersion))
            {
                currentVersion = new Version(0, 0, 0);
            }

            var result = await UpdateService.CheckForUpdatesAsync(RepoOwner, RepoName, currentVersion, AppendLog);
            if (result == UpdateService.UpdateResult.UpdatingAndRestarting)
            {
                WpfApp.Application.Current?.Shutdown();
            }
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message)) message = string.Empty;
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        }

        private void AppendLogFromBackground(string message)
        {
            Dispatcher.Invoke(() => AppendLog(message));
        }

        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return string.IsNullOrWhiteSpace(name) ? "Profile" : name;
        }
    }

    internal sealed class StarterAddonChoice
    {
        public AddonApp Addon { get; set; } = null!;
        public bool IsSelected { get; set; }
    }
}
