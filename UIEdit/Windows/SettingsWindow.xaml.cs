using Ookii.Dialogs.Wpf;
using System.Windows;

namespace UIEdit.Windows
{
    public partial class SettingsWindow : Window
    {
        #region Injection Properties
        public string InterfacesPath { get; private set; }
        public string SurfacesPath { get; private set; }
        #endregion

        #region Constructor
        public SettingsWindow(string currentInterfaces, string currentSurfaces)
        {
            InitializeComponent();
            InterfacesPath = currentInterfaces;
            SurfacesPath = currentSurfaces;
            TxtInterfaces.Text = InterfacesPath;
            TxtSurfaces.Text = SurfacesPath;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Handles the Browse button click for selecting the Interfaces directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBrowseInterfaces(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog();
            var ok = dlg.ShowDialog();
            if (ok == true) TxtInterfaces.Text = dlg.SelectedPath;
        }

        /// <summary>
        /// Handles the Browse button click for selecting the Surfaces directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBrowseSurfaces(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog();
            var ok = dlg.ShowDialog();
            if (ok == true) TxtSurfaces.Text = dlg.SelectedPath;
        }

        /// <summary>
        /// Handles the OK button click to save the selected paths and close the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOk(object sender, RoutedEventArgs e)
        {
            InterfacesPath = TxtInterfaces.Text;
            SurfacesPath = TxtSurfaces.Text;
            DialogResult = true;
            Close();
        }
        #endregion
    }
}

