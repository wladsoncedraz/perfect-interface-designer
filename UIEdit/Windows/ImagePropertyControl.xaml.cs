using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UIEdit.Utils;

// By: SpinxDev 2025 xD
namespace UIEdit.Windows
{
    public class PropRow
    {
        public string Property { get; set; }
        public string Value { get; set; }
        public bool IsImageProperty { get; set; }
    }
}

namespace UIEdit.Windows
{
    public partial class ImagePropertyControl : UserControl, INotifyPropertyChanged
    {
        #region Injection properties
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(ImagePropertyControl),
                new PropertyMetadata("", OnValueChanged));

        public static readonly DependencyProperty SurfacesPathProperty =
            DependencyProperty.Register("SurfacesPath", typeof(string), typeof(ImagePropertyControl),
                new PropertyMetadata("", OnSurfacesPathChanged));
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public string SurfacesPath
        {
            get => (string)GetValue(SurfacesPathProperty);
            set => SetValue(SurfacesPathProperty, value);
        }
        public string ImageTooltip
        {
            get => _imageTooltip;
            set
            {
                if (_imageTooltip != value)
                {
                    _imageTooltip = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _imageTooltip;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor
        public ImagePropertyControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Called when the Value property changes to update the image preview.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImagePropertyControl control)
            {
                control.UpdateImagePreview();
            }
        }

        /// <summary>
        /// Called when the SurfacesPath property changes to update the image preview.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSurfacesPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImagePropertyControl control)
            {
                control.UpdateImagePreview();
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the TxtImagePath TextBox to update the image preview and notify property changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtImagePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateImagePreview();
            OnPropertyChanged(nameof(Value));

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var dataGridRow = FindParent<DataGridRow>(this);
                if (dataGridRow?.DataContext is PropRow propRow)
                {
                    mainWindow.UpdateImageProperty(propRow.Property, Value);
                }
            }
        }

        /// <summary>
        /// Selects all text in the TxtImagePath TextBox when it receives focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtImagePath_GotFocus(object sender, RoutedEventArgs e)
        {
            TxtImagePath.SelectAll();
        }

        /// <summary>
        /// Updates the image preview when the TxtImagePath TextBox loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtImagePath_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateImagePreview();
        }

        /// <summary>
        /// Opens the image selector dialog when the select image button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selector = new ImageSelectorWindow(SurfacesPath)
                {
                    Owner = Window.GetWindow(this)
                };

                if (selector.ShowDialog() == true && !string.IsNullOrEmpty(selector.SelectedImagePath))
                {
                    Value = selector.SelectedImagePath;
                    UpdateImagePreview();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ImagePropertyControl.BtnSelectImage_Click", "Erro ao abrir seletor de imagens", ex);
                MessageBox.Show("Erro ao abrir seletor de imagens: " + ex.Message, "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Updates the image preview based on the current Value and SurfacesPath properties.
        /// </summary>
        private void UpdateImagePreview()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Value))
                {
                    ImgPreview.Source = null;
                    TxtError.Visibility = Visibility.Collapsed;
                    ImageTooltip = "";
                    return;
                }

                var imagePath = Value;
                if (!Path.IsPathRooted(imagePath) && !string.IsNullOrEmpty(SurfacesPath))
                {
                    imagePath = ImageUtils.ConvertToAbsolutePath(imagePath, SurfacesPath);
                }

                if (!ImageUtils.ImageExists(imagePath))
                {
                    ImgPreview.Source = null;
                    TxtError.Visibility = Visibility.Visible;
                    ImageTooltip = "Arquivo n?o encontrado";
                    return;
                }

                var thumbnail = ImageUtils.CreateThumbnail(imagePath, 36, 36);
                ImgPreview.Source = thumbnail;
                TxtError.Visibility = Visibility.Collapsed;

                var imageInfo = ImageUtils.GetImageInfo(imagePath);
                ImageTooltip = $"{imageInfo.FileName} | {imageInfo.Dimensions} | {imageInfo.SizeFormatted} | {imageInfo.Extension}";
            }
            catch (Exception ex)
            {
                Logger.LogError("ImagePropertyControl.UpdateImagePreview", $"Erro ao atualizar preview da imagem {Value}", ex);
                ImgPreview.Source = null;
                TxtError.Visibility = Visibility.Visible;
                ImageTooltip = "Erro ao carregar imagem";
            }
        }

        /// <summary>
        /// Shows a tooltip with image details when the mouse enters the image preview area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgPreview_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ImgPreview.Source != null && !string.IsNullOrWhiteSpace(Value))
            {
                ShowImageTooltip();
            }
        }

        /// <summary>
        /// Hides the image tooltip when the mouse leaves the image preview area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgPreview_MouseLeave(object sender, MouseEventArgs e)
        {
            HideImageTooltip();
        }

        /// <summary>
        /// Displays a tooltip popup with image information and a preview.
        /// </summary>
        private void ShowImageTooltip()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Value)) return;

                var imagePath = Value;
                if (!Path.IsPathRooted(imagePath) && !string.IsNullOrEmpty(SurfacesPath))
                {
                    imagePath = ImageUtils.ConvertToAbsolutePath(imagePath, SurfacesPath);
                }

                var imageInfo = ImageUtils.GetImageInfo(imagePath);
                if (!imageInfo.Exists) return;

                TxtTooltipInfo.Text = $"{imageInfo.FileName} | {imageInfo.Dimensions} | {imageInfo.SizeFormatted} | {imageInfo.Extension}";

                var fullImage = Core.GetImageSourceFromFileName(imagePath);
                ImgTooltipPreview.Source = fullImage;

                ImageTooltipPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                Logger.LogError("ImagePropertyControl.ShowImageTooltip", "Erro ao mostrar tooltip da imagem", ex);
            }
        }

        /// <summary>
        /// Hides the image tooltip popup.
        /// </summary>
        private void HideImageTooltip()
        {
            ImageTooltipPopup.IsOpen = false;
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Finds the first parent of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }
        #endregion
    }
}
