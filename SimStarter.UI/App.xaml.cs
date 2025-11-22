using System;
using System.Linq;
using System.Windows;
using SimStarter.Core;

namespace SimStarter.UI
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var runArg = e.Args.FirstOrDefault(a => a.StartsWith("--run-profile-id=", StringComparison.OrdinalIgnoreCase));
            if (runArg != null)
            {
                var id = runArg.Substring("--run-profile-id=".Length);
                RunProfileHeadless(id);
                Shutdown();
                return;
            }

            var window = new MainWindow();
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }

        private static void RunProfileHeadless(string id)
        {
            var config = ProfilesStore.LoadOrCreate();
            var starter = config.Starters.FirstOrDefault(p =>
                string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, id, StringComparison.OrdinalIgnoreCase));

            if (starter == null)
            {
                return;
            }

            StarterRunner.RunStarter(config, starter, Console.WriteLine);
        }
    }
}
