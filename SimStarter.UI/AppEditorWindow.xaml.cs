using System.Windows;
using Microsoft.Win32;
using SimStarter.Core;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Windows.Controls;

namespace SimStarter.UI
{
    public partial class AppEditorWindow : Window
    {
        public CatalogItem Result { get; private set; } = default!;

        public AppEditorWindow(string title, CatalogItem? existing = null)
        {
            InitializeComponent();
            Title = title;
            if (existing != null)
            {
                NameBox.Text = existing.Name;
                PathBox.Text = existing.Path;
                ArgsBox.Text = existing.Arguments;
                AdminBox.IsChecked = existing.RunAsAdmin;
                WaitBox.IsChecked = existing.WaitForExit;
            }

            NameBox.Focus();
            NameBox.SelectAll();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Executable files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                PathBox.Text = dlg.FileName;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(PathBox.Text))
            {
                MessageBox.Show("Name and path are required.", "Missing data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new CatalogItem
            {
                Name = NameBox.Text.Trim(),
                Path = PathBox.Text.Trim(),
                Arguments = ArgsBox.Text ?? string.Empty,
                RunAsAdmin = AdminBox.IsChecked == true,
                WaitForExit = WaitBox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
