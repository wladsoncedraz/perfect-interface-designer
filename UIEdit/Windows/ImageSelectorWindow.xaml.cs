using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIEdit.Utils;

// By: SpinxDev 2025 xD
namespace UIEdit.Windows
{
    public partial class ImageSelectorWindow : Window
    {
        #region Injection Properties
        public string SelectedImagePath { get; private set; }
        public string SurfacesPath { get; set; }
        private readonly ObservableCollection<FolderItem> _folders = new ObservableCollection<FolderItem>();
        private readonly ObservableCollection<ImageItem> _images = new ObservableCollection<ImageItem>();
        private readonly List<ImageItem> _allImages = new List<ImageItem>();
        private const int MaxImagesToProcess = 1000; // Limite para evitar sobrecarga
        #endregion

        #region Constructor
        public ImageSelectorWindow(string surfacesPath)
        {
            try
            {
                InitializeComponent();
                SurfacesPath = surfacesPath;
                TvFolders.ItemsSource = _folders;
                LbImages.ItemsSource = _images;

                // Inicializa o ComboBox com o primeiro item selecionado
                if (CmbFormatFilter.Items.Count > 0)
                {
                    CmbFormatFilter.SelectedIndex = 0;
                }

                // Carrega pastas de forma ass赤ncrona para n?o travar a UI
                LoadFoldersAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.Constructor", "Erro ao inicializar ImageSelectorWindow", ex);
                MessageBox.Show($"Erro ao inicializar seletor de imagens: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads folders and images asynchronously to keep the UI responsive.
        /// </summary>
        private async void LoadFoldersAsync()
        {
            try
            {
                Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", "Iniciando carregamento ass赤ncrono de pastas");

                _folders.Clear();
                _allImages.Clear();

                if (string.IsNullOrEmpty(SurfacesPath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", "SurfacesPath est芍 vazio");
                    return;
                }

                if (!Directory.Exists(SurfacesPath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", $"Diret車rio n?o existe: {SurfacesPath}");
                    MessageBox.Show($"Diret車rio de surfaces n?o encontrado: {SurfacesPath}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", "Criando pasta raiz");
                var rootFolder = new FolderItem
                {
                    Name = "Surfaces",
                    FullPath = SurfacesPath,
                    Icon = "?",
                    Children = new ObservableCollection<FolderItem>()
                };

                Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", "Iniciando carregamento recursivo");
                await Task.Run(() => LoadFolderRecursive(rootFolder, SurfacesPath));

                Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", "Adicionando pasta raiz 角 lista");

                // Volta para a thread da UI
                Dispatcher.Invoke(() =>
                {
                    _folders.Add(rootFolder);

                    // Expande a primeira pasta
                    if (rootFolder.Children.Any())
                    {
                        rootFolder.IsExpanded = true;
                    }
                });

                Logger.LogAction("ImageSelectorWindow.LoadFoldersAsync", $"Carregamento conclu赤do. {_allImages.Count} imagens encontradas");

                // Informa se o limite foi atingido
                if (_allImages.Count >= MaxImagesToProcess)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Limite de {MaxImagesToProcess} imagens atingido. Use a busca para filtrar resultados.",
                            "Informa??o", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LoadFoldersAsync", "Erro ao carregar pastas", ex);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erro ao carregar pastas: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Loads folders and images synchronously (used for initial load).
        /// </summary>
        private void LoadFolders()
        {
            try
            {
                Logger.LogAction("ImageSelectorWindow.LoadFolders", $"Iniciando carregamento de pastas. SurfacesPath: {SurfacesPath}");

                _folders.Clear();
                _allImages.Clear();

                if (string.IsNullOrEmpty(SurfacesPath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFolders", "SurfacesPath est芍 vazio");
                    return;
                }

                if (!Directory.Exists(SurfacesPath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFolders", $"Diret車rio n?o existe: {SurfacesPath}");
                    MessageBox.Show($"Diret車rio de surfaces n?o encontrado: {SurfacesPath}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Logger.LogAction("ImageSelectorWindow.LoadFolders", "Criando pasta raiz");
                var rootFolder = new FolderItem
                {
                    Name = "Surfaces",
                    FullPath = SurfacesPath,
                    Icon = "?",
                    Children = new ObservableCollection<FolderItem>()
                };

                Logger.LogAction("ImageSelectorWindow.LoadFolders", "Iniciando carregamento recursivo");
                LoadFolderRecursive(rootFolder, SurfacesPath);

                Logger.LogAction("ImageSelectorWindow.LoadFolders", "Adicionando pasta raiz 角 lista");
                _folders.Add(rootFolder);

                // Expande a primeira pasta
                if (rootFolder.Children.Any())
                {
                    rootFolder.IsExpanded = true;
                }

                Logger.LogAction("ImageSelectorWindow.LoadFolders", $"Carregamento conclu赤do. {_allImages.Count} imagens encontradas");
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LoadFolders", "Erro ao carregar pastas", ex);
                MessageBox.Show($"Erro ao carregar pastas: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recursively loads folders and images from the given folder path.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="folderPath"></param>
        private void LoadFolderRecursive(FolderItem parent, string folderPath)
        {
            try
            {
                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Processando pasta: {folderPath}");

                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Pasta inv芍lida ou n?o existe: {folderPath}");
                    return;
                }

                // Carrega subdiret車rios
                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Buscando subdiret車rios em: {folderPath}");
                var directories = Directory.GetDirectories(folderPath)
                    .OrderBy(d => Path.GetFileName(d))
                    .ToList();

                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Encontrados {directories.Count} subdiret車rios");

                foreach (var dir in directories)
                {
                    try
                    {
                        Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Processando subdiret車rio: {dir}");
                        var folderName = Path.GetFileName(dir);
                        var folderItem = new FolderItem
                        {
                            Name = folderName,
                            FullPath = dir,
                            Icon = "?",
                            Children = new ObservableCollection<FolderItem>()
                        };

                        LoadFolderRecursive(folderItem, dir);
                        parent.Children.Add(folderItem);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ImageSelectorWindow.LoadFolderRecursive", $"Erro ao carregar subdiret車rio {dir}", ex);
                        // Continua com outros diret車rios mesmo se um falhar
                    }
                }

                // Carrega imagens desta pasta
                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Buscando imagens em: {folderPath}");
                var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsImageFile(f))
                    .ToList();

                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Encontradas {imageFiles.Count} imagens");

                // Limita o n迆mero de imagens processadas para evitar sobrecarga
                if (_allImages.Count >= MaxImagesToProcess)
                {
                    Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Limite de imagens atingido ({MaxImagesToProcess}). Parando processamento.");
                    return;
                }

                var remainingSlots = MaxImagesToProcess - _allImages.Count;
                var filesToProcess = imageFiles.Take(remainingSlots).ToList();

                foreach (var imageFile in filesToProcess)
                {
                    try
                    {
                        Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Processando imagem: {Path.GetFileName(imageFile)}");
                        var relativePath = GetRelativePath(imageFile);
                        var imageItem = new ImageItem
                        {
                            FileName = Path.GetFileName(imageFile),
                            FullPath = imageFile,
                            RelativePath = relativePath,
                            FolderPath = folderPath,
                            Thumbnail = null // Thumbnail ser芍 criado sob demanda
                        };

                        _allImages.Add(imageItem);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ImageSelectorWindow.LoadFolderRecursive", $"Erro ao processar imagem {imageFile}", ex);
                        // Continua com outras imagens mesmo se uma falhar
                    }
                }

                parent.Count = imageFiles.Count;
                Logger.LogAction("ImageSelectorWindow.LoadFolderRecursive", $"Pasta processada: {folderPath} - {imageFiles.Count} imagens");
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LoadFolderRecursive", $"Erro ao carregar pasta {folderPath}", ex);
            }
        }

        /// <summary>
        /// Determines if the given file path corresponds to a supported image file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".dds" || extension == ".tga" || extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }

        /// <summary>
        /// Gets the path of the image relative to the SurfacesPath.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(SurfacesPath)) return fullPath;

            try
            {
                var uri1 = new Uri(SurfacesPath + Path.DirectorySeparatorChar);
                var uri2 = new Uri(fullPath);
                var relativeUri = uri1.MakeRelativeUri(uri2);
                return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\');
            }
            catch
            {
                return Path.GetFileName(fullPath);
            }
        }

        /// <summary>
        /// Creates a thumbnail image for the given image path.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private BitmapImage CreateThumbnail(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    Logger.LogAction("ImageSelectorWindow.CreateThumbnail", $"Arquivo n?o existe: {imagePath}");
                    return CreateErrorThumbnail();
                }

                // Tenta carregar a imagem convertida (PNG) primeiro
                var pngPath = imagePath.Replace(".dds", ".png").Replace(".DDS", ".png")
                                      .Replace(".tga", ".png").Replace(".TGA", ".png");

                if (File.Exists(pngPath))
                {
                    return LoadThumbnailFromFile(pngPath);
                }

                // Se n?o existe PNG, tenta carregar o arquivo original
                return LoadThumbnailFromFile(imagePath);
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.CreateThumbnail", $"Erro ao criar thumbnail para {imagePath}", ex);
                return CreateErrorThumbnail();
            }
        }

        /// <summary>
        /// Gets or creates a thumbnail for the given image path.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private BitmapImage GetThumbnailForImage(string imagePath)
        {
            // Cria thumbnail sob demanda
            if (string.IsNullOrEmpty(imagePath))
            {
                return CreateErrorThumbnail();
            }

            return CreateThumbnail(imagePath);
        }

        /// <summary>
        /// Loads a thumbnail image from the specified file path.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private BitmapImage LoadThumbnailFromFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    Logger.LogAction("ImageSelectorWindow.LoadThumbnailFromFile", $"Arquivo inv芍lido: {imagePath}");
                    return CreateErrorThumbnail();
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.DecodePixelWidth = 100; // Reduz para thumbnail
                bitmap.DecodePixelHeight = 100;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                if (bitmap.CanFreeze) bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LoadThumbnailFromFile", $"Erro ao carregar thumbnail: {imagePath}", ex);
                return CreateErrorThumbnail();
            }
        }

        /// <summary>
        /// Creates a default error thumbnail image.
        /// </summary>
        /// <returns></returns>
        private BitmapImage CreateErrorThumbnail()
        {
            try
            {
                // Cria uma imagem de erro usando o recurso existente
                using (var memory = new MemoryStream())
                {
                    Properties.Resources.errorpic.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = memory;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                // Fallback: retorna uma imagem vazia
                return new BitmapImage();
            }
        }

        /// <summary>
        /// Handles folder selection changes to load images for the selected folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TvFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedFolder = e.NewValue as FolderItem;
            if (selectedFolder == null) return;

            LoadImagesForFolder(selectedFolder.FullPath);
        }

        /// <summary>
        /// Loads images for the specified folder path into the image list.
        /// </summary>
        /// <param name="folderPath"></param>
        private void LoadImagesForFolder(string folderPath)
        {
            try
            {
                _images.Clear();

                var folderImages = _allImages
                    .Where(img => img.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(img => img.FileName)
                    .ToList();

                foreach (var img in folderImages)
                {
                    _images.Add(img);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LoadImagesForFolder", $"Erro ao carregar imagens da pasta {folderPath}", ex);
            }
        }

        /// <summary>
        /// Handles image selection changes to display the preview and info of the selected image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedImage = LbImages.SelectedItem as ImageItem;
            if (selectedImage == null)
            {
                ImgPreview.Source = null;
                TxtImageInfo.Text = "";
                TxtImagePath.Text = "";
                BtnSelect.IsEnabled = false;
                return;
            }

            // Carrega preview grande
            try
            {
                var previewImage = Core.GetImageSourceFromFileName(selectedImage.FullPath);
                ImgPreview.Source = previewImage;

                // Informa??es da imagem
                var fileInfo = new FileInfo(selectedImage.FullPath);
                TxtImageInfo.Text = $"Arquivo: {selectedImage.FileName}\nTamanho: {fileInfo.Length / 1024} KB\nFormato: {Path.GetExtension(selectedImage.FullPath).ToUpper()}";
                TxtImagePath.Text = $"Caminho: {selectedImage.RelativePath}";

                BtnSelect.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.LbImages_SelectionChanged", "Erro ao carregar preview da imagem", ex);
                ImgPreview.Source = CreateErrorThumbnail();
                TxtImageInfo.Text = "Erro ao carregar imagem";
                TxtImagePath.Text = selectedImage.RelativePath;
            }
        }

        /// <summary>
        /// Handles text changes in the search box to filter images.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterImages();
        }

        /// <summary>
        /// Handles format filter changes to filter images.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Buscar imagens...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// Handles loss of focus in the search box to restore placeholder text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Buscar imagens...";
                TxtSearch.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        /// <summary>
        /// Filters images based on search text and format selection.
        /// </summary>
        private void FilterImages()
        {
            try
            {
                var searchText = TxtSearch?.Text ?? "";
                var formatFilter = "";

                if (CmbFormatFilter?.SelectedItem is ComboBoxItem selectedItem)
                {
                    formatFilter = selectedItem.Content?.ToString() ?? "";
                }

                if (searchText == "Buscar imagens...") searchText = "";

                var filteredImages = _allImages.AsEnumerable();

                // Filtro por texto
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filteredImages = filteredImages.Where(img =>
                        img.FileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        img.RelativePath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Filtro por formato
                if (!string.IsNullOrEmpty(formatFilter) && formatFilter != "Todos")
                {
                    var extension = "." + formatFilter.ToLower();
                    filteredImages = filteredImages.Where(img =>
                        Path.GetExtension(img.FullPath).ToLowerInvariant() == extension);
                }

                _images.Clear();
                foreach (var img in filteredImages.OrderBy(img => img.FileName))
                {
                    _images.Add(img);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageSelectorWindow.FilterImages", "Erro ao filtrar imagens", ex);
            }
        }

        /// <summary>
        /// Handles the Refresh button click to reload folders and images.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFolders();
        }

        /// <summary>
        /// Handles the Select button click to confirm the selected image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            var selectedImage = LbImages.SelectedItem as ImageItem;
            if (selectedImage != null)
            {
                SelectedImagePath = selectedImage.RelativePath;
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Handles the Cancel button click to close the window without selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
    #endregion

    #region Helper Class
    public class FolderItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Icon { get; set; }
        public int Count { get; set; }
        public bool IsExpanded { get; set; }
        public ObservableCollection<FolderItem> Children { get; set; } = new ObservableCollection<FolderItem>();
    }

    public class ImageItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public string FolderPath { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
    #endregion
}
