using System.Windows;

namespace UIEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void ApplyLightTheme()
        {
            Application.Current.Resources["Color.PrimaryBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF0, 0xF0, 0xF0));
            Application.Current.Resources["Color.SecondaryBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            Application.Current.Resources["Color.HeaderBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            Application.Current.Resources["Color.Text"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x00, 0x00));
            Application.Current.Resources["Color.SubText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
            Application.Current.Resources["Color.Border"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC8, 0xC8, 0xC8));
            Application.Current.Resources["Color.GridLine"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0));
            Application.Current.Resources["Color.Selection"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD0, 0xE7, 0xFF));
            Application.Current.Resources["Color.TableHeaderBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3));
            Application.Current.Resources["Color.TableHeaderText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        public static void ApplyDarkTheme()
        {
            Application.Current.Resources["Color.PrimaryBackground"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1f1f1e"));
            Application.Current.Resources["Color.SecondaryBackground"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2a2a29"));
            Application.Current.Resources["Color.HeaderBackground"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2a2a29"));
            Application.Current.Resources["Color.Text"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gainsboro);
            Application.Current.Resources["Color.SubText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Silver);
            Application.Current.Resources["Color.Border"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22303A"));
            Application.Current.Resources["Color.GridLine"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A2530"));
            Application.Current.Resources["Color.Selection"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#264D73"));
            Application.Current.Resources["Color.TableHeaderBackground"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F141B"));
            Application.Current.Resources["Color.TableHeaderText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gainsboro);
        }
    }
}
