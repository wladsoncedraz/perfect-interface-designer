using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using UIEdit.Controllers;
using UIEdit.Models;
using UIEdit.Utils;

// By: SpinxDev 2025 xD
namespace UIEdit.Windows
{
    public partial class MainWindow
    {
        #region Injection Properties
        public ProjectController ProjectController { get; set; }
        public SourceFile CurrentSourceFile { get; set; }
        public LayoutController LayoutController { get; set; }
        public AvalonEditorSearchController TextSearchController { get; set; }
        private DragDropController DragDropController { get; set; }
        private readonly Dictionary<string, Stack<LayoutSnapshot>> _undoModelsByFile = new Dictionary<string, Stack<LayoutSnapshot>>();
        private readonly Dictionary<string, Stack<LayoutSnapshot>> _redoModelsByFile = new Dictionary<string, Stack<LayoutSnapshot>>();

        private double _currentZoom = 1.0;
        private const double MinZoom = 0.1;
        private const double MaxZoom = 5.0;
        private const double ZoomStep = 0.1;

        private const int RulerMajorTick = 50;
        private const int RulerMinorTick = 10;

        private readonly ObservableCollection<PropRow> _propRows = new ObservableCollection<PropRow>();
        private UIControl _propTarget;
        private UIEdit.Models.UIDialog _propDialog;

        private readonly ObservableCollection<LayerRow> _layerRows = new ObservableCollection<LayerRow>();
        private bool _isUpdatingGridSelection = false;
        private bool _isSelectingFromGrid = false;

        private bool _isDark = false;
        #endregion

        #region Helper Class
        private class LayoutSnapshot
        {
            public List<ControlState> Controls { get; set; } = new List<ControlState>();
        }
        private class ControlState
        {
            public string TypeName { get; set; }
            public string Name { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string Text { get; set; }
            public string Hint { get; set; }
            public double FontSize { get; set; }
            public string Color { get; set; }
            public int ZIndex { get; set; }
        }
        private class LayerRow
        {
            public string Name { get; set; }
            public int Layer { get; set; }
            public UIControl Model { get; set; }
            public bool IsDialog { get; set; } = false;
        }
        private class PropRow
        {
            public string Property { get; set; }
            public string Value { get; set; }
            public bool IsImageProperty { get; set; }
        }
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            Dispatcher.UnhandledException += ApplicationOnDispatcherUnhandledException;

            Logger.LogAction("MainWindow.Constructor", "Inicializando aplicação UIEdit");

            Logger.CleanOldLogs(30);
            ProjectController = new ProjectController();
            LayoutController = new LayoutController();
            TextSearchController = new AvalonEditorSearchController(TeFile);
            LbDialogs.SelectionChanged += LbDialogsOnSelectionChanged;
            TeFile.TextChanged += TeFileOnTextChanged;
            TxtSearch.TextChanged += TxtSearchOnTextChanged;
            TbSearchInText.TextChanged += TbSearchInTextOnTextChanged;
            TbSearchInText.PreviewKeyDown += TbSearchInTextOnPreviewKeyDown;
            DragDropController = new DragDropController(DialogCanvas);
            DragDropController.OnDragStateChanged += state =>
            {
                if (state == false)
                {
                    TeFile.IsModified = true;
                    LockEditor(true);
                    PushModelUndoSnapshot();
                    UpdateUndoRedoUiState();
                }
            };
            DragDropController.OnDragBegin += () => { };

            this.PreviewKeyDown += (s, e) =>
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                {
                    BtnUndo_OnClick(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Z)
                {
                    BtnRedo_OnClick(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
                {
                    var selectedControls = DialogCanvas.Children.OfType<Canvas>()
                        .Where(c => c.Tag is UIControl && c.Children.OfType<Rectangle>()
                            .Any(r => r.Stroke == Brushes.LimeGreen && r.StrokeThickness == 2))
                        .ToList();

                    if (selectedControls.Any())
                    {
                        var control = selectedControls.First().Tag as UIControl;
                        if (control != null)
                        {
                            CopySelectedControls();
                            e.Handled = true;
                            return;
                        }
                    }
                }
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
                {
                    if (ClipboardManager.HasCopiedControlXml)
                    {
                        PasteCopiedControls();
                        e.Handled = true;
                        return;
                    }
                }
                if (DragDropController == null || DragDropController.SelectedContainer == null) return;
                var step = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? 10 : 1;
                if (e.Key == Key.Left) { DragDropController.MoveSelectedBy(-step, 0, false); e.Handled = true; }
                if (e.Key == Key.Right) { DragDropController.MoveSelectedBy(step, 0, false); e.Handled = true; }
                if (e.Key == Key.Up) { DragDropController.MoveSelectedBy(0, -step, false); e.Handled = true; }
                if (e.Key == Key.Down) { DragDropController.MoveSelectedBy(0, step, false); e.Handled = true; }
                if (e.Handled) { TeFile.IsModified = true; LockEditor(true); PushModelUndoSnapshot(); UpdateUndoRedoUiState(); }
            };
            DragDropController.OnItemDoubleClick += OpenAttributesEditor;
            DragDropController.OnSelectionChanged += m =>
            {
                var currentSelection = GetSelectedModelsFromController();
                SyncLayerSelection(currentSelection);
                RefreshPropertiesView(currentSelection);
            };
            DragDropController.OnDialogSelected += dialog =>
            {
                SyncLayerSelection(Enumerable.Empty<UIControl>());
                SyncDialogSelection(dialog);
                RefreshDialogPropertiesView(dialog);
            };
            DragDropController.OnDialogResized += dialog =>
            {
                TeFile.IsModified = true;
                LockEditor(true);
                PushModelUndoSnapshot();
                UpdateUndoRedoUiState();
                RefreshDialogPropertiesView(dialog);
            };
            DialogCanvas.KeyDown += (s, e) =>
            {
                if (DragDropController.SelectedContainer == null) return;
                var step = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? 10 : 1;
                if (e.Key == Key.Left) { DragDropController.MoveSelectedBy(-step, 0, false); e.Handled = true; }
                if (e.Key == Key.Right) { DragDropController.MoveSelectedBy(step, 0, false); e.Handled = true; }
                if (e.Key == Key.Up) { DragDropController.MoveSelectedBy(0, -step, false); e.Handled = true; }
                if (e.Key == Key.Down) { DragDropController.MoveSelectedBy(0, step, false); e.Handled = true; }
                if (e.Handled) { TeFile.IsModified = true; LockEditor(true); PushModelUndoSnapshot(); UpdateUndoRedoUiState(); }
            };

            SetupZoomControls();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Handles the ContentRendered event of the window.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            try
            {
                var lastInterfaces = UIEdit.Properties.Settings.Default.LastInterfacesPath;
                if (!string.IsNullOrEmpty(lastInterfaces) && Directory.Exists(lastInterfaces))
                {
                    ProjectController.LoadInterfacesFromPath(lastInterfaces);
                    LbDialogs.ItemsSource = ProjectController.Files;
                }
            }
            catch { }
            try
            {
                var lastSurfaces = UIEdit.Properties.Settings.Default.LastSurfacesPath;
                if (!string.IsNullOrEmpty(lastSurfaces) && Directory.Exists(lastSurfaces))
                {
                    ProjectController.LoadSurfacesFromPath(lastSurfaces);
                }
            }
            catch { }
            try
            {
                var xshd = UIEdit.Properties.Settings.Default.LastXshdPath;
                var lastMode = UIEdit.Properties.Settings.Default.LastThemeMode;
                var light = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", "Light.xshd");
                var dark = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", "Dark.xshd");

                if (string.IsNullOrEmpty(xshd)) xshd = light;

                if (string.Equals(lastMode, "Dark", StringComparison.OrdinalIgnoreCase))
                {
                    App.ApplyDarkTheme();
                    if (File.Exists(dark)) ApplyXshdTheme(dark);
                    else if (File.Exists(xshd)) ApplyXshdTheme(xshd);
                    _isDark = true;
                }
                else
                {
                    App.ApplyLightTheme();
                    if (File.Exists(light)) ApplyXshdTheme(light);
                    else if (File.Exists(xshd)) ApplyXshdTheme(xshd);
                    _isDark = false;
                }
            }
            catch { }
        }

        /// <summary>
        /// Search in text box key down event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbSearchInTextOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                TextSearchController.PrevSearch(TbSearchInText.Text);
            if (e.Key == Key.Down)
                TextSearchController.NextSearch(TbSearchInText.Text);
        }

        /// <summary>
        /// Search in text box text changed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbSearchInTextOnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextSearchController.NextSearch(TbSearchInText.Text);
        }

        /// <summary>
        /// Search previous text fragment button click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSearchPrevTextFragment_OnClick(object sender, RoutedEventArgs e)
        {
            TextSearchController.PrevSearch(TbSearchInText.Text);
        }

        /// <summary>
        /// Search next text fragment button click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSearchNextTextFragment_OnClick(object sender, RoutedEventArgs e)
        {
            TextSearchController.NextSearch(TbSearchInText.Text);
        }

        /// <summary>
        /// Handles unhandled exceptions in the application dispatcher.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError("MainWindow.ApplicationOnDispatcherUnhandledException",
                "Erro não tratado na aplicação", e.Exception);

            File.AppendAllText("error.log", string.Format(@"{0} {1} {2}{4}{3}{4}{4}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), e.Exception, e.Exception.Message, e.Exception.StackTrace, Environment.NewLine));
        }

        /// <summary>
        /// Search box text changed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearchOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ProjectController.Files == null) return;
            LbDialogs.ItemsSource = null;
            var files = TxtSearch.Text == ""
                ? ProjectController.Files.OrderBy(t => t.ShortFileName)
                : ProjectController.Files.Where(f => f.ShortFileName.Contains(TxtSearch.Text)).OrderBy(t => t.ShortFileName);
            LbDialogs.ItemsSource = files;
            if (CurrentSourceFile == null) return;
            var i = -1;
            foreach (var sourceFile in files)
            {
                i++;
                if (sourceFile.FileName != CurrentSourceFile.FileName) continue;
                LbDialogs.SelectedIndex = i;
                break;
            }
        }

        /// <summary>
        /// Text editor text changed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TeFileOnTextChanged(object sender, EventArgs e)
        {
            LockEditor(TeFile.IsModified);
            TextSearchController.Reset();
            var exceptionParse = LayoutController.Parse(TeFile.Text, ProjectController.SurfacesPath);
            if (exceptionParse == null)
            {
                if (CurrentSourceFile != null && LayoutController.Dialog != null)
                {
                    LayerMetadataService.TryLoadAndApply(CurrentSourceFile.FileName, LayoutController.Dialog);
                }
                LayoutController.RefreshLayout(DialogCanvas);
                UpdateCanvasSize();
                UpdateRulers();
                EnsureInitialUndoSnapshotIfNeeded();
                RefreshLayersView();
            }
            else
            {
                DialogCanvas.Children.Clear();
                DialogCanvas.Children.Add(
                    new TextBlock
                    {
                        Text = exceptionParse.Message,
                        MaxWidth = DialogCanvas.ActualWidth,
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush { Color = Colors.Red },
                    });
            }
        }

        /// <summary>
        /// Ensures that an initial undo snapshot is created for the current source file if needed.
        /// </summary>
        private void EnsureInitialUndoSnapshotIfNeeded()
        {
            if (CurrentSourceFile == null) return;
            var key = CurrentSourceFile.FileName;
            if (!_undoModelsByFile.ContainsKey(key)) _undoModelsByFile[key] = new Stack<LayoutSnapshot>();
            if (!_redoModelsByFile.ContainsKey(key)) _redoModelsByFile[key] = new Stack<LayoutSnapshot>();
            var ustack = _undoModelsByFile[key];
            if (ustack.Count == 0)
            {
                var snap = CaptureSnapshotFromDialog();
                if (snap != null)
                {
                    ustack.Push(snap);
                    _redoModelsByFile[key].Clear();
                    UpdateUndoRedoUiState();
                }
            }
        }

        /// <summary>
        /// Locks or unlocks the editor UI based on the specified state.
        /// </summary>
        /// <param name="state"></param>
        private void LockEditor(bool state)
        {
            GbSourceFiles.IsEnabled = true;
            EditorButtonPanel.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Handles the selection changed event of the dialogs list box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbDialogsOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            CurrentSourceFile = (SourceFile)e.AddedItems[0];

            Logger.LogFileOperation("MainWindow.LbDialogsOnSelectionChanged", "Carregar", CurrentSourceFile.FileName);

            TeFile.Text = ProjectController.GetSourceFileContent(CurrentSourceFile);
            LockEditor(false);
            InitModelUndoForCurrentFile();
            if (LayoutController.Dialog != null)
            {
                if (LayerMetadataService.TryLoadAndApply(CurrentSourceFile.FileName, LayoutController.Dialog))
                {
                    LayoutController.RefreshLayout(DialogCanvas);
                }
            }
            RefreshLayersView();
        }

        /// <summary>
        /// Handles the click event of the commit button to save changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCommit_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.LogOperationStart("MainWindow.BtnCommit_OnClick", "Salvar alterações");

                var originalXml = TeFile.Text;

                System.Diagnostics.Debug.WriteLine($"XML original (primeiros 200 chars): {originalXml?.Substring(0, Math.Min(200, originalXml?.Length ?? 0))}");

                var updatedXml = LayoutController.ApplyModelToXml(originalXml);

                var fileContent = ProjectController.GetSourceFileContent(CurrentSourceFile);
                if (string.IsNullOrEmpty(updatedXml) || updatedXml == fileContent)
                {
                    System.Diagnostics.Debug.WriteLine("ApplyModelToXml retornou XML vazio ou inalterado em relação ao arquivo");
                    Logger.LogAction("MainWindow.BtnCommit_OnClick", "Nenhuma alteração detectada para salvar");
                    return;
                }

                TeFile.Text = updatedXml;
                ProjectController.SaveSourceFileContent(CurrentSourceFile, updatedXml);
                LockEditor(false);
                LayerMetadataService.Save(CurrentSourceFile.FileName, LayoutController.Dialog);
                RefreshLayersView();

                Logger.LogFileOperation("MainWindow.BtnCommit_OnClick", "Salvar", CurrentSourceFile.FileName, true);
                Logger.LogOperationEnd("MainWindow.BtnCommit_OnClick", "Salvar alterações", true);
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.BtnCommit_OnClick", "Erro ao salvar alterações", ex);
                System.Diagnostics.Debug.WriteLine($"Erro em BtnCommit_OnClick: {ex.Message}");
                try
                {
                    File.AppendAllText("commit_error.log",
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Erro em BtnCommit_OnClick: {ex.Message}\n{ex.StackTrace}\n\n");
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Handles the click event of the cancel button to revert changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Logger.LogAction("MainWindow.BtnCancel_OnClick", "Cancelar alterações - revertendo para estado original");
            TeFile.Text = ProjectController.GetSourceFileContent(CurrentSourceFile);
            LockEditor(false);
        }

        /// <summary>
        /// Handles the click event of the clear filter button to reset the search box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            TxtSearch.Focus();
        }

        /// <summary>
        /// Handles the click event of the interfaces path button to select a new path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnInterfacesPath_OnClick(object sender, RoutedEventArgs e) { }

        /// <summary>
        /// Handles the click event of the surfaces path button to select a new path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSurfacesPath_OnClick(object sender, RoutedEventArgs e) { }

        /// <summary>
        /// Handles the click event of the GitHub button to open the project repository.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnGotoGithub_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/perfectdev/UIEdit");
        }

        /// <summary>
        /// Handles the click event of the settings button to open the settings window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(ProjectController.InterfacesPath, ProjectController.SurfacesPath) { Owner = this };
            var ok = dlg.ShowDialog();
            if (ok.HasValue && ok.Value)
            {
                if (!string.IsNullOrWhiteSpace(dlg.InterfacesPath))
                {
                    ProjectController.LoadInterfacesFromPath(dlg.InterfacesPath);
                    LbDialogs.ItemsSource = ProjectController.Files;
                }
                if (!string.IsNullOrWhiteSpace(dlg.SurfacesPath))
                {
                    ProjectController.LoadSurfacesFromPath(dlg.SurfacesPath);
                }
            }
        }


        /// <summary>
        /// Handles the click event of the theme toggle button to switch between light and dark themes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnThemeToggle_OnClick(object sender, RoutedEventArgs e)
        {
            _isDark = !_isDark;
            if (_isDark) App.ApplyDarkTheme(); else App.ApplyLightTheme();
            try
            {
                var fg = (System.Windows.Media.SolidColorBrush)Application.Current.Resources["Color.Text"];
                var bg = (System.Windows.Media.SolidColorBrush)Application.Current.Resources["Color.PrimaryBackground"];
                TeFile.Foreground = fg; TeFile.Background = bg;
                TeFile.LineNumbersForeground = fg;
            }
            catch { }

            try
            {
                var themeFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes", _isDark ? "Dark.xshd" : "Light.xshd");
                if (File.Exists(themeFile))
                {
                    ApplyXshdTheme(themeFile);
                    UIEdit.Properties.Settings.Default.LastXshdPath = themeFile;
                    UIEdit.Properties.Settings.Default.LastThemeMode = _isDark ? "Dark" : "Light";
                    UIEdit.Properties.Settings.Default.Save();
                }
                else
                {
                    TeFile.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML");
                }
            }
            catch { }
        }

        /// <summary>
        /// Applies the syntax highlighting theme from the specified XSHD file.
        /// </summary>
        /// <param name="filePath"></param>
        private void ApplyXshdTheme(string filePath)
        {
            try
            {
                using (var reader = XmlReader.Create(filePath, new XmlReaderSettings { IgnoreComments = false, IgnoreProcessingInstructions = false, IgnoreWhitespace = true }))
                {
                    reader.MoveToContent();
                    var rootName = reader.Name;
                    var rootNs = reader.NamespaceURI ?? string.Empty;
                    if (string.Equals(rootName, "ThemeSyntaxDefinition", StringComparison.OrdinalIgnoreCase) || rootNs.Contains("themesyntaxdefinition"))
                    {
                        ApplyThemeSyntaxDefinition(filePath);
                        return;
                    }
                }

                using (var reader2 = XmlReader.Create(filePath))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader2);
                    var def = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                    TeFile.SyntaxHighlighting = def;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Falha ao aplicar tema: " + ex.Message, "Theme", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Applies a theme syntax definition from the specified file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="Exception"></exception>
        private void ApplyThemeSyntaxDefinition(string filePath)
        {
            System.Xml.Linq.XNamespace ns = "http://icsharpcode.net/sharpdevelop/themesyntaxdefinition/2019";
            var doc = System.Xml.Linq.XDocument.Load(filePath, System.Xml.Linq.LoadOptions.PreserveWhitespace | System.Xml.Linq.LoadOptions.SetLineInfo);
            var root = doc.Root;
            if (root == null)
                throw new Exception("Arquivo de tema inválido.");

            var globals = root.Element(ns + "GlobalStyles");
            if (globals != null)
            {
                var defStyle = globals.Element(ns + "DefaultStyle");
                if (defStyle != null)
                {
                    var fg = (string)defStyle.Attribute("foreground");
                    var bg = (string)defStyle.Attribute("background");
                    if (!string.IsNullOrWhiteSpace(bg)) TeFile.Background = new SolidColorBrush(ParseColor(bg));
                    if (!string.IsNullOrWhiteSpace(fg)) TeFile.Foreground = new SolidColorBrush(ParseColor(fg));
                }
            }

            var synXml = root.Elements(ns + "SyntaxDefinition").FirstOrDefault(e =>
                string.Equals((string)e.Attribute("name"), "XML", StringComparison.OrdinalIgnoreCase));

            var xmlDef = HighlightingManager.Instance.GetDefinition("XML");
            if (xmlDef == null)
                throw new Exception("Definição padrão 'XML' não encontrada no AvalonEdit.");

            if (synXml != null)
            {
                foreach (var colorEl in synXml.Elements(ns + "Color"))
                {
                    var name = (string)colorEl.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var hc = xmlDef.GetNamedColor(name);
                    if (hc == null) continue;

                    var fg = (string)colorEl.Attribute("foreground");
                    var bg = (string)colorEl.Attribute("background");
                    var fw = (string)colorEl.Attribute("fontWeight");
                    var fs = (string)colorEl.Attribute("fontStyle");
                    if (!string.IsNullOrWhiteSpace(fg)) hc.Foreground = new AvaloniaCompatHighlightingBrush(new SolidColorBrush(ParseColor(fg)));
                    if (!string.IsNullOrWhiteSpace(bg)) hc.Background = new AvaloniaCompatHighlightingBrush(new SolidColorBrush(ParseColor(bg)));
                    if (!string.IsNullOrWhiteSpace(fw)) hc.FontWeight = string.Equals(fw, "bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : (FontWeight?)FontWeights.Normal;
                    if (!string.IsNullOrWhiteSpace(fs)) hc.FontStyle = string.Equals(fs, "italic", StringComparison.OrdinalIgnoreCase) ? FontStyles.Italic : (FontStyle?)FontStyles.Normal;
                }
            }

            TeFile.SyntaxHighlighting = xmlDef;
        }

        /// <summary>
        /// Parses a color string and returns the corresponding Color object.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Color ParseColor(string value)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(value);
            }
            catch
            {
                if (value != null && value.Length == 7) return (Color)ColorConverter.ConvertFromString("#FF" + value.Substring(1));
                throw;
            }
        }

        /// <summary>
        /// Opens the attributes editor window for the specified control.
        /// </summary>
        /// <param name="control"></param>
        private void OpenAttributesEditor(UIEdit.Models.UIControl control)
        {
            PushModelUndoSnapshot();
            var dlg = new AttributesWindow(control)
            {
                Owner = this,
                SurfacesPath = ProjectController?.SurfacesPath
            };
            var ok = dlg.ShowDialog();
            if (ok.HasValue && ok.Value)
            {
                LayoutController.RefreshLayout(DialogCanvas);
                TeFile.IsModified = true;
                LockEditor(true);
                UpdateUndoRedoUiState();
            }
        }

        /// <summary>
        /// Handles the change event of the toggle outlines checkbox to show or hide control outlines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleOutlines_OnChanged(object sender, RoutedEventArgs e)
        {
            var isOn = (sender as System.Windows.Controls.Primitives.ToggleButton)?.IsChecked == true;
            LayoutController.ShowAllOutlines = isOn;
            LayoutController.RefreshLayout(DialogCanvas);
        }

        /// <summary>
        /// Initializes the undo and redo stacks for the current source file.
        /// </summary>
        private void InitModelUndoForCurrentFile()
        {
            if (CurrentSourceFile == null) return;
            var key = CurrentSourceFile.FileName;
            if (!_undoModelsByFile.ContainsKey(key)) _undoModelsByFile[key] = new Stack<LayoutSnapshot>();
            if (!_redoModelsByFile.ContainsKey(key)) _redoModelsByFile[key] = new Stack<LayoutSnapshot>();
            _undoModelsByFile[key].Clear();
            _redoModelsByFile[key].Clear();
            var snap = CaptureSnapshotFromDialog();
            if (snap != null) _undoModelsByFile[key].Push(snap);
            UpdateUndoRedoUiState();
        }

        /// <summary>
        /// Compares two layout snapshots for equality.
        /// </summary>
        private void PushModelUndoSnapshot()
        {
            if (CurrentSourceFile == null) return;
            var key = CurrentSourceFile.FileName;
            if (!_undoModelsByFile.ContainsKey(key)) _undoModelsByFile[key] = new Stack<LayoutSnapshot>();
            if (!_redoModelsByFile.ContainsKey(key)) _redoModelsByFile[key] = new Stack<LayoutSnapshot>();
            var ustack = _undoModelsByFile[key];
            var rstack = _redoModelsByFile[key];
            var currentSnap = CaptureSnapshotFromDialog();
            if (currentSnap == null) return;
            if (ustack.Count > 0 && AreSnapshotsEqual(ustack.Peek(), currentSnap)) return;
            ustack.Push(currentSnap);
            rstack.Clear();
        }

        /// <summary>
        /// Compares two layout snapshots for equality.
        /// </summary>
        private void UpdateUndoRedoUiState()
        {
            if (CurrentSourceFile == null)
            {
                if (BtnUndo != null) BtnUndo.IsEnabled = false;
                if (BtnRedo != null) BtnRedo.IsEnabled = false;
                return;
            }
            var key = CurrentSourceFile.FileName;
            var canUndo = _undoModelsByFile.ContainsKey(key) && _undoModelsByFile[key].Count > 1;
            var canRedo = _redoModelsByFile.ContainsKey(key) && _redoModelsByFile[key].Count > 0;
            if (BtnUndo != null) BtnUndo.IsEnabled = canUndo;
            if (BtnRedo != null) BtnRedo.IsEnabled = canRedo;
        }

        /// <summary>
        /// Handles the click event of the undo button to revert the last change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUndo_OnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentSourceFile == null) return;
            var key = CurrentSourceFile.FileName;
            if (!_undoModelsByFile.ContainsKey(key)) return;
            var ustack = _undoModelsByFile[key];
            if (!_redoModelsByFile.ContainsKey(key)) _redoModelsByFile[key] = new Stack<LayoutSnapshot>();
            var rstack = _redoModelsByFile[key];
            if (ustack.Count <= 1) return;

            Logger.LogAction("MainWindow.BtnUndo_OnClick", "Executar Undo");

            var current = ustack.Pop();
            rstack.Push(current);
            var previous = ustack.Peek();
            ApplySnapshotToDialog(previous);
            LayoutController.RefreshLayout(DialogCanvas);
            TeFile.IsModified = true;
            LockEditor(true);
            UpdateUndoRedoUiState();
        }

        /// <summary>
        /// Handles the click event of the redo button to reapply the last undone change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRedo_OnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentSourceFile == null) return;
            var key = CurrentSourceFile.FileName;
            if (!_redoModelsByFile.ContainsKey(key)) return;
            var rstack = _redoModelsByFile[key];
            if (!_undoModelsByFile.ContainsKey(key)) _undoModelsByFile[key] = new Stack<LayoutSnapshot>();
            var ustack = _undoModelsByFile[key];
            if (rstack.Count == 0) return;

            Logger.LogAction("MainWindow.BtnRedo_OnClick", "Executar Redo");

            var next = rstack.Pop();
            ustack.Push(next);
            ApplySnapshotToDialog(next);
            LayoutController.RefreshLayout(DialogCanvas);
            TeFile.IsModified = true;
            LockEditor(true);
            UpdateUndoRedoUiState();
        }

        /// <summary>
        /// Captures the current layout snapshot from the dialog.
        /// </summary>
        /// <returns></returns>
        private LayoutSnapshot CaptureSnapshotFromDialog()
        {
            if (LayoutController == null || LayoutController.Dialog == null) return null;
            var snap = new LayoutSnapshot();
            foreach (var c in LayoutController.Dialog.Edits) snap.Controls.Add(ToState(c));
            foreach (var c in LayoutController.Dialog.ImagePictures) snap.Controls.Add(ToState(c));
            foreach (var c in LayoutController.Dialog.Scrolls) snap.Controls.Add(ToState(c));
            foreach (var c in LayoutController.Dialog.RadioButtons) snap.Controls.Add(ToState(c));
            foreach (var c in LayoutController.Dialog.StillImageButtons) snap.Controls.Add(ToState(c));
            foreach (var c in LayoutController.Dialog.Labels) snap.Controls.Add(ToState(c));
            return snap;
        }

        /// <summary>
        /// Converts a UIControl to its ControlState representation.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static ControlState ToState(UIControl c)
        {
            return new ControlState
            {
                TypeName = c.GetType().Name,
                Name = c.Name,
                X = c.X,
                Y = c.Y,
                Width = c.Width,
                Height = c.Height,
                Text = (c as UILabel)?.Text ?? (c as UIStillImageButton)?.Text,
                Hint = (c as UIStillImageButton)?.Hint ?? (c as UIRadioButton)?.Hint,
                FontSize = (c as UILabel)?.FontSize > 0 ? (c as UILabel).FontSize : (c as UIStillImageButton)?.FontSize ?? 0,
                Color = (c as UILabel)?.Color ?? (c as UIStillImageButton)?.Color,
                ZIndex = c.ZIndex
            };
        }

        /// <summary>
        /// Applies a layout snapshot to the dialog, updating control properties accordingly.
        /// </summary>
        /// <param name="snap"></param>
        private void ApplySnapshotToDialog(LayoutSnapshot snap)
        {
            foreach (var st in snap.Controls)
            {
                UIControl target = null;
                switch (st.TypeName)
                {
                    case nameof(UIEditBox): target = LayoutController.Dialog.Edits.FirstOrDefault(x => x.Name == st.Name); break;
                    case nameof(UIImagePicture): target = LayoutController.Dialog.ImagePictures.FirstOrDefault(x => x.Name == st.Name); break;
                    case nameof(UIScroll): target = LayoutController.Dialog.Scrolls.FirstOrDefault(x => x.Name == st.Name); break;
                    case nameof(UIRadioButton): target = LayoutController.Dialog.RadioButtons.FirstOrDefault(x => x.Name == st.Name); break;
                    case nameof(UIStillImageButton): target = LayoutController.Dialog.StillImageButtons.FirstOrDefault(x => x.Name == st.Name); break;
                    case nameof(UILabel): target = LayoutController.Dialog.Labels.FirstOrDefault(x => x.Name == st.Name); break;
                }
                if (target == null) continue;
                target.X = st.X; target.Y = st.Y; target.Width = st.Width; target.Height = st.Height; target.ZIndex = st.ZIndex;
                var lbl = target as UILabel; if (lbl != null) { lbl.Text = st.Text; lbl.FontSize = st.FontSize; lbl.Color = st.Color; }
                var btn = target as UIStillImageButton; if (btn != null) { btn.Text = st.Text; btn.FontSize = st.FontSize; btn.Color = st.Color; btn.Hint = st.Hint; }
                var radio = target as UIRadioButton; if (radio != null) { radio.Hint = st.Hint; }
            }
        }

        /// <summary>
        /// Compares two layout snapshots for equality.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool AreSnapshotsEqual(LayoutSnapshot a, LayoutSnapshot b)
        {
            if (a == null || b == null) return false;
            if (a.Controls.Count != b.Controls.Count) return false;
            for (int i = 0; i < a.Controls.Count; i++)
            {
                var ca = a.Controls[i]; var cb = b.Controls[i];
                if (ca.TypeName != cb.TypeName || ca.Name != cb.Name) return false;
                if (Math.Abs(ca.X - cb.X) > 0.0001 || Math.Abs(ca.Y - cb.Y) > 0.0001) return false;
                if (Math.Abs(ca.Width - cb.Width) > 0.0001 || Math.Abs(ca.Height - cb.Height) > 0.0001) return false;
                if (ca.Text != cb.Text || ca.Hint != cb.Hint || Math.Abs(ca.FontSize - cb.FontSize) > 0.0001 || ca.Color != cb.Color) return false;
                if (ca.ZIndex != cb.ZIndex) return false;
            }
            return true;
        }

        /// <summary>
        /// Adjusts the Z-Index of the selected controls based on the provided adjustment function.
        /// </summary>
        /// <param name="adjust"></param>
        private void AdjustZIndex(System.Func<int, int> adjust)
        {
            if (DragDropController == null || DragDropController.SelectedContainer == null) return;
            var containers = DialogCanvas.Children.OfType<Canvas>().Where(c => c.Tag is UIEdit.Models.UIControl).ToList();
            var selected = containers.Where(c => DragDropController.SelectedContainer == c || DragDropController.SelectedContainer != null && DragDropController.SelectedContainer != null && DragDropController.SelectedContainer.Tag == c.Tag).ToList();
            var multi = typeof(UIEdit.Controllers.DragDropController).GetField("_selectedContainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(DragDropController) as System.Collections.Generic.List<Canvas>;
            if (multi != null && multi.Any()) selected = multi;

            if (!selected.Any()) return;
            foreach (var c in selected)
            {
                var m = (UIEdit.Models.UIControl)c.Tag;
                var newZ = adjust(m.ZIndex);
                m.ZIndex = newZ;
            }
            var selectedModels = selected.Select(c => (UIEdit.Models.UIControl)c.Tag).ToList();
            LayoutController.RefreshLayout(DialogCanvas);
            if (CurrentSourceFile != null)
                LayerMetadataService.Save(CurrentSourceFile.FileName, LayoutController.Dialog);
            RefreshLayersView();
            ReselectModels(selectedModels);
        }

        /// <summary>
        /// Reselects the specified models in the drag-and-drop controller and updates the layer selection accordingly.
        /// </summary>
        /// <param name="models"></param>
        private void ReselectModels(IEnumerable<UIControl> models)
        {
            if (DragDropController == null || models == null) return;
            try
            {
                var miClear = typeof(UIEdit.Controllers.DragDropController).GetMethod("ClearSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                miClear?.Invoke(DragDropController, null);
            }
            catch { }
            var containers = DialogCanvas.Children.OfType<Canvas>().Where(c => c.Tag is UIControl).ToList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var model in models)
            {
                if (model == null || seen.Contains(model.Name)) continue;
                var c = containers.FirstOrDefault(k => ((UIControl)k.Tag).Name == model.Name);
                if (c != null)
                {
                    var miAdd = typeof(UIEdit.Controllers.DragDropController).GetMethod("AddToSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (miAdd != null) miAdd.Invoke(DragDropController, new object[] { c, (UIControl)c.Tag });
                    seen.Add(model.Name);
                }
            }
            SyncLayerSelection(models);
        }

        /// <summary>
        /// Gets the currently selected models from the drag-and-drop controller using reflection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<UIControl> GetSelectedModelsFromController()
        {
            try
            {
                var f = typeof(UIEdit.Controllers.DragDropController).GetField("_selectedModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var list = f?.GetValue(DragDropController) as System.Collections.IEnumerable;
                if (list == null) return Enumerable.Empty<UIControl>();
                var result = new List<UIControl>();
                foreach (var item in list) if (item is UIControl m) result.Add(m);
                return result;
            }
            catch { return Enumerable.Empty<UIControl>(); }
        }

        /// <summary>
        /// Synchronizes the selection in the layers DataGrid with the specified models.
        /// </summary>
        /// <param name="models"></param>
        private void SyncLayerSelection(IEnumerable<UIControl> models)
        {
            if (DgLayers == null || models == null) return;
            var names = new HashSet<string>(models.Select(m => m.Name));
            _isUpdatingGridSelection = true;
            try
            {
                DgLayers.SelectedItems.Clear();
                foreach (var item in DgLayers.Items)
                {
                    var row = item as LayerRow;
                    if (row != null && names.Contains(row.Name)) DgLayers.SelectedItems.Add(item);
                }
            }
            finally { _isUpdatingGridSelection = false; }
        }

        /// <summary>
        /// Synchronizes the selection in the layers DataGrid with the specified dialog.
        /// </summary>
        /// <param name="dialog"></param>
        private void SyncDialogSelection(UIEdit.Models.UIDialog dialog)
        {
            if (DgLayers == null || dialog == null) return;
            _isUpdatingGridSelection = true;
            try
            {
                DgLayers.SelectedItems.Clear();
                foreach (var item in DgLayers.Items)
                {
                    var row = item as LayerRow;
                    if (row != null && row.IsDialog && row.Name == dialog.Name)
                    {
                        DgLayers.SelectedItems.Add(item);
                        break;
                    }
                }
            }
            finally { _isUpdatingGridSelection = false; }
        }

        /// <summary>
        /// Handles the selection changed event of the layers DataGrid to update the drag-and-drop controller and properties view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DragDropController == null) return;
            if (_isUpdatingGridSelection) return;
            var selectedRows = DgLayers.SelectedItems.Cast<LayerRow>().ToList();
            if (!selectedRows.Any()) return;

            var dialogRow = selectedRows.FirstOrDefault(r => r.IsDialog);
            if (dialogRow != null && LayoutController?.Dialog != null)
            {
                DragDropController.SelectDialog(DialogCanvas, LayoutController.Dialog);
                RefreshDialogPropertiesView(LayoutController.Dialog);
                return;
            }

            var models = selectedRows.Select(r => r.Model).Where(m => m != null).ToList();
            _isSelectingFromGrid = true;
            try { ReselectModels(models); }
            finally { _isSelectingFromGrid = false; }
            var currentSelection = GetSelectedModelsFromController();
            RefreshPropertiesView(currentSelection);
        }

        /// <summary>
        /// Refreshes the properties view for the specified dialog.
        /// </summary>
        private void RefreshLayersView()
        {
            if (DgLayers == null) return;
            _layerRows.Clear();
            if (LayoutController == null || LayoutController.Dialog == null) { DgLayers.ItemsSource = _layerRows; return; }

            _layerRows.Add(new LayerRow
            {
                Name = LayoutController.Dialog.Name,
                Layer = -1,
                Model = null,
                IsDialog = true
            });

            Action<IEnumerable<UIControl>> add = controls => { foreach (var c in controls) _layerRows.Add(new LayerRow { Name = c.Name, Layer = c.ZIndex, Model = c }); };
            add(LayoutController.Dialog.ImagePictures);
            add(LayoutController.Dialog.Scrolls);
            add(LayoutController.Dialog.Edits);
            add(LayoutController.Dialog.StillImageButtons);
            add(LayoutController.Dialog.RadioButtons);
            add(LayoutController.Dialog.Labels);
            var ordered = _layerRows.OrderByDescending(r => r.Layer).ThenBy(r => r.Name).ToList();
            DgLayers.ItemsSource = ordered;
            var currentSelection = GetSelectedModelsFromController();
            SyncLayerSelection(currentSelection);
            RefreshPropertiesView(currentSelection);
        }

        /// <summary>
        /// Handles the cell edit ending event of the layers DataGrid to update the layer of the corresponding model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgLayers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            var row = e.Row.Item as LayerRow;
            if (row == null) return;

            if (row.IsDialog) return;

            if (row.Model == null) return;
            int newLayer;
            var tb = e.EditingElement as TextBox;
            if (tb == null || !int.TryParse(tb.Text, out newLayer)) return;
            row.Layer = newLayer;
            row.Model.ZIndex = newLayer;
            LayoutController.RefreshLayout(DialogCanvas);
            if (CurrentSourceFile != null)
                LayerMetadataService.Save(CurrentSourceFile.FileName, LayoutController.Dialog);
            RefreshLayersView();
            ReselectModels(GetSelectedModelsFromController());
        }

        /// <summary>
        /// Refreshes the properties view for the specified dialog.
        /// </summary>
        /// <param name="currentSelection"></param>
        private void RefreshPropertiesView(IEnumerable<UIControl> currentSelection)
        {
            if (DgProps == null) return;
            _propRows.Clear();
            _propTarget = null;
            _propDialog = null;
            var selected = currentSelection?.Where(sel => sel != null).ToList() ?? new List<UIControl>();
            if (selected.Count != 1) { DgProps.ItemsSource = _propRows; return; }
            var single = selected[0];
            _propTarget = single;

            _propRows.Add(new PropRow { Property = "Name", Value = single.Name, IsImageProperty = false });
            _propRows.Add(new PropRow { Property = "X", Value = single.X.ToString(), IsImageProperty = false });
            _propRows.Add(new PropRow { Property = "Y", Value = single.Y.ToString(), IsImageProperty = false });
            _propRows.Add(new PropRow { Property = "Width", Value = single.Width.ToString(), IsImageProperty = false });
            _propRows.Add(new PropRow { Property = "Height", Value = single.Height.ToString(), IsImageProperty = false });
            _propRows.Add(new PropRow { Property = "ZIndex", Value = single.ZIndex.ToString(), IsImageProperty = false });

            var lbl = single as UILabel;
            if (lbl != null)
            {
                _propRows.Add(new PropRow { Property = "Text", Value = lbl.Text, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "FontSize", Value = lbl.FontSize.ToString(), IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "Color", Value = lbl.Color, IsImageProperty = false });
            }

            var btn = single as UIStillImageButton;
            if (btn != null)
            {
                _propRows.Add(new PropRow { Property = "Text", Value = btn.Text, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "Hint", Value = btn.Hint, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "FontSize", Value = btn.FontSize.ToString(), IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "Color", Value = btn.Color, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "UpImage", Value = btn.UpImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "DownImage", Value = btn.DownImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "HoverImage", Value = btn.HoverImage ?? "", IsImageProperty = true });
            }

            var radio = single as UIRadioButton;
            if (radio != null)
            {
                _propRows.Add(new PropRow { Property = "Hint", Value = radio.Hint, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "NormalImage", Value = radio.NormalImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "CheckedImage", Value = radio.CheckedImage ?? "", IsImageProperty = true });
            }

            var edit = single as UIEditBox;
            if (edit != null)
            {
                _propRows.Add(new PropRow { Property = "Text", Value = edit.Text, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "FontSize", Value = edit.FontSize.ToString(), IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "Color", Value = edit.Color, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "FrameImage", Value = edit.FrameImage?.FileName ?? "", IsImageProperty = true });
            }

            var image = single as UIImagePicture;
            if (image != null)
            {
                _propRows.Add(new PropRow { Property = "FileName", Value = image.FileName ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "GfxFileName", Value = image.GfxFileName ?? "", IsImageProperty = true });
            }

            var scroll = single as UIScroll;
            if (scroll != null)
            {
                _propRows.Add(new PropRow { Property = "UpImage", Value = scroll.UpImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "DownImage", Value = scroll.DownImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "ScrollImage", Value = scroll.ScrollImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "BarFrameImage", Value = scroll.BarFrameImage ?? "", IsImageProperty = true });
            }

            var list = single as UIList;
            if (list != null)
            {
                _propRows.Add(new PropRow { Property = "Text", Value = list.Text, IsImageProperty = false });
                _propRows.Add(new PropRow { Property = "FrameImage", Value = list.FrameImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "HilightImage", Value = list.HilightImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "UpImage", Value = list.UpImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "DownImage", Value = list.DownImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "ScrollImage", Value = list.ScrollImage ?? "", IsImageProperty = true });
                _propRows.Add(new PropRow { Property = "BarImage", Value = list.BarImage ?? "", IsImageProperty = true });
            }

            var progress = single as UIProgress;
            if (progress != null)
            {
                _propRows.Add(new PropRow { Property = "FillImage", Value = progress.FillImage ?? "", IsImageProperty = true });
            }

            var orderedProps = _propRows.OrderBy(x => x.IsImageProperty ? 1 : 0).ThenBy(x => x.Property).ToList();
            _propRows.Clear();
            foreach (var prop in orderedProps)
            {
                _propRows.Add(prop);
            }

            DgProps.ItemsSource = _propRows;
        }

        /// <summary>
        /// Refreshes the properties view for the specified dialog.
        /// </summary>
        /// <param name="dialog"></param>
        private void RefreshDialogPropertiesView(UIEdit.Models.UIDialog dialog)
        {
            if (DgProps == null || dialog == null) return;
            _propRows.Clear();
            _propTarget = null;
            _propDialog = dialog;

            _propRows.Add(new PropRow { Property = "Name", Value = dialog.Name });
            _propRows.Add(new PropRow { Property = "Width", Value = dialog.Width.ToString() });
            _propRows.Add(new PropRow { Property = "Height", Value = dialog.Height.ToString() });

            DgProps.ItemsSource = _propRows;
        }

        /// <summary>
        /// Handles the loading row event of the properties DataGrid to set the data context for image properties.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgProps_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row.Item as PropRow;
            if (row != null && row.IsImageProperty)
            {
                e.Row.DataContext = row;
            }
        }

        /// <summary>
        /// Updates the specified image property of the currently selected control.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="newValue"></param>
        public void UpdateImageProperty(string propertyName, string newValue)
        {
            try
            {
                if (_propTarget == null) return;

                Logger.LogActionWithParams("MainWindow.UpdateImageProperty",
                    "Atualizar propriedade de imagem", propertyName, newValue);

                switch (propertyName)
                {
                    case "UpImage":
                        if (_propTarget is UIStillImageButton btn) btn.UpImage = newValue;
                        else if (_propTarget is UIList list) list.UpImage = newValue;
                        else if (_propTarget is UIScroll scroll) scroll.UpImage = newValue;
                        break;
                    case "DownImage":
                        if (_propTarget is UIStillImageButton btn2) btn2.DownImage = newValue;
                        else if (_propTarget is UIList list2) list2.DownImage = newValue;
                        else if (_propTarget is UIScroll scroll2) scroll2.DownImage = newValue;
                        break;
                    case "HoverImage":
                        if (_propTarget is UIStillImageButton btn3) btn3.HoverImage = newValue;
                        break;
                    case "NormalImage":
                        if (_propTarget is UIRadioButton radio) radio.NormalImage = newValue;
                        break;
                    case "CheckedImage":
                        if (_propTarget is UIRadioButton radio2) radio2.CheckedImage = newValue;
                        break;
                    case "FrameImage":
                        if (_propTarget is UIEditBox edit)
                        {
                            if (edit.FrameImage == null) edit.FrameImage = new UIResourceFrameImage();
                            edit.FrameImage.FileName = newValue;
                        }
                        else if (_propTarget is UIList list3) list3.FrameImage = newValue;
                        break;
                    case "FileName":
                        if (_propTarget is UIImagePicture img) img.FileName = newValue;
                        break;
                    case "GfxFileName":
                        if (_propTarget is UIImagePicture img2) img2.GfxFileName = newValue;
                        break;
                    case "ScrollImage":
                        if (_propTarget is UIScroll scroll3) scroll3.ScrollImage = newValue;
                        else if (_propTarget is UIList list4) list4.ScrollImage = newValue;
                        break;
                    case "BarFrameImage":
                        if (_propTarget is UIScroll scroll4) scroll4.BarFrameImage = newValue;
                        break;
                    case "HilightImage":
                        if (_propTarget is UIList list5) list5.HilightImage = newValue;
                        break;
                    case "BarImage":
                        if (_propTarget is UIList list6) list6.BarImage = newValue;
                        break;
                    case "FillImage":
                        if (_propTarget is UIProgress progress) progress.FillImage = newValue;
                        break;
                }

                LayoutController.RefreshLayout(DialogCanvas);
                TeFile.IsModified = true;
                LockEditor(true);

                var propRow = _propRows.FirstOrDefault(r => r.Property == propertyName);
                if (propRow != null)
                {
                    propRow.Value = newValue;
                }

            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.UpdateImageProperty", $"Erro ao atualizar propriedade de imagem {propertyName}", ex);
            }
        }

        /// <summary>
        /// Handles the cell edit ending event of the properties DataGrid to update the corresponding property of the selected control or dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgProps_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            var row = e.Row.Item as PropRow; if (row == null) return;

            if (row.IsImageProperty) return;

            var tb = e.EditingElement as TextBox; if (tb == null) return;
            var val = tb.Text ?? "";

            Logger.LogActionWithParams("MainWindow.DgProps_CellEditEnding",
                "Alterar propriedade", row.Property, val);

            if (_propDialog != null)
            {
                switch (row.Property)
                {
                    case "Name": _propDialog.Name = val; break;
                    case "Width":
                        if (double.TryParse(val, out var w))
                        {
                            _propDialog.Width = Math.Max(100, w);
                            var dialogContainer = DialogCanvas.Children.OfType<Canvas>()
                                .FirstOrDefault(c => c.Tag == _propDialog);
                            if (dialogContainer != null)
                            {
                                dialogContainer.Width = _propDialog.Width;
                                var image = dialogContainer.Children.OfType<Image>().FirstOrDefault();
                                if (image != null) image.Width = _propDialog.Width;
                            }
                        }
                        break;
                    case "Height":
                        if (double.TryParse(val, out var h))
                        {
                            _propDialog.Height = Math.Max(100, h);
                            var dialogContainer = DialogCanvas.Children.OfType<Canvas>()
                                .FirstOrDefault(c => c.Tag == _propDialog);
                            if (dialogContainer != null)
                            {
                                dialogContainer.Height = _propDialog.Height;
                                var image = dialogContainer.Children.OfType<Image>().FirstOrDefault();
                                if (image != null) image.Height = _propDialog.Height;
                            }
                        }
                        break;
                }
                LayoutController.RefreshLayout(DialogCanvas);
                TeFile.IsModified = true;
                LockEditor(true);
                return;
            }

            if (_propTarget == null) return;
            switch (row.Property)
            {
                case "Name": _propTarget.Name = val; break;
                case "X": if (double.TryParse(val, out var x)) _propTarget.X = x; break;
                case "Y": if (double.TryParse(val, out var y)) _propTarget.Y = y; break;
                case "Width": if (double.TryParse(val, out var w)) _propTarget.Width = w; break;
                case "Height": if (double.TryParse(val, out var h)) _propTarget.Height = h; break;
                case "ZIndex": if (int.TryParse(val, out var z)) _propTarget.ZIndex = z; break;
                case "Text":
                    var lbl = _propTarget as UILabel; if (lbl != null) { lbl.Text = val; break; }
                    var btn = _propTarget as UIStillImageButton; if (btn != null) { btn.Text = val; break; }
                    break;
                case "Hint":
                    var b2 = _propTarget as UIStillImageButton; if (b2 != null) { b2.Hint = val; break; }
                    var r2 = _propTarget as UIRadioButton; if (r2 != null) { r2.Hint = val; break; }
                    break;
                case "FontSize":
                    var lbl2 = _propTarget as UILabel; if (lbl2 != null && double.TryParse(val, out var fs1)) { lbl2.FontSize = fs1; break; }
                    var btn2 = _propTarget as UIStillImageButton; if (btn2 != null && double.TryParse(val, out var fs2)) { btn2.FontSize = fs2; break; }
                    break;
                case "Color":
                    var lbl3 = _propTarget as UILabel; if (lbl3 != null) { lbl3.Color = val; break; }
                    var btn3 = _propTarget as UIStillImageButton; if (btn3 != null) { btn3.Color = val; break; }
                    break;
            }
            LayoutController.RefreshLayout(DialogCanvas);
            if (row.Property == "ZIndex" && CurrentSourceFile != null)
                LayerMetadataService.Save(CurrentSourceFile.FileName, LayoutController.Dialog);
            RefreshLayersView();
            var currentSelection = GetSelectedModelsFromController();
            ReselectModels(currentSelection);
        }

        /// <summary>
        /// Handles the click event of the "Send Backward" button to decrease the Z-Index of the selected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendBackward_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustZIndex(z => z - 1);
        }

        /// <summary>
        /// Handles the click event of the "Bring Forward" button to increase the Z-Index of the selected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBringForward_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustZIndex(z => z + 1);
        }

        /// <summary>
        /// Handles the click event of the "Align Horizontal" button to align selected controls horizontally with a fixed gap.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAlignHorizontal_OnClick(object sender, RoutedEventArgs e)
        {
            const double gap = 10.0;
            if (DragDropController == null) return;

            Logger.LogAction("MainWindow.BtnAlignHorizontal_OnClick", "Alinhar controles horizontalmente");
            var selected = typeof(UIEdit.Controllers.DragDropController)
                .GetField("_selectedContainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(DragDropController) as System.Collections.Generic.List<Canvas>;
            if (selected == null || selected.Count < 2) return;

            var ordered = selected.OrderBy(c => c.Margin.Left).ToList();
            var baseY = ordered.First().Margin.Top;

            double x = ordered.First().Margin.Left;
            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                var m = (UIControl)c.Tag;
                var y = baseY;
                if (i == 0)
                {
                    c.Margin = new Thickness(x, y, 0, 0);
                    m.X = x; m.Y = y;
                }
                else
                {
                    var prev = ordered[i - 1];
                    x = prev.Margin.Left + prev.Width + gap;
                    c.Margin = new Thickness(x, y, 0, 0);
                    m.X = x; m.Y = y;
                }
            }

            LayoutController.RefreshLayout(DialogCanvas);
            ReselectModels(ordered.Select(k => (UIControl)k.Tag));
            TeFile.IsModified = true;
            LockEditor(true);
        }

        /// <summary>
        /// Handles the click event of the "Align Vertical" button to align selected controls vertically with a fixed gap.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAlignVertical_OnClick(object sender, RoutedEventArgs e)
        {
            const double gap = 10.0;
            if (DragDropController == null) return;

            Logger.LogAction("MainWindow.BtnAlignVertical_OnClick", "Alinhar controles verticalmente");
            var selected = typeof(UIEdit.Controllers.DragDropController)
                .GetField("_selectedContainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(DragDropController) as System.Collections.Generic.List<Canvas>;
            if (selected == null || selected.Count < 2) return;

            var ordered = selected.OrderBy(c => c.Margin.Top).ToList();
            var baseX = ordered.First().Margin.Left;

            double y = ordered.First().Margin.Top;
            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                var m = (UIControl)c.Tag;
                if (i == 0)
                {
                    c.Margin = new Thickness(baseX, y, 0, 0);
                    m.X = baseX; m.Y = y;
                }
                else
                {
                    var prev = ordered[i - 1];
                    y = prev.Margin.Top + prev.Height + gap;
                    c.Margin = new Thickness(baseX, y, 0, 0);
                    m.X = baseX; m.Y = y;
                }
            }

            LayoutController.RefreshLayout(DialogCanvas);
            ReselectModels(ordered.Select(k => (UIControl)k.Tag));
            TeFile.IsModified = true;
            LockEditor(true);
        }

        /// <summary>
        /// Sets up the zoom controls and initializes the canvas size and rulers.
        /// </summary>
        private void SetupZoomControls()
        {
            UpdateZoomTextBox();
            UpdateCanvasSize();
            SetupRulers();
        }

        /// <summary>
        /// Handles the mouse wheel event on the canvas scroll viewer to zoom in or out when the Control key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            e.Handled = true;

            var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            var newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, _currentZoom + delta));

            SetZoom(newZoom);
        }

        /// <summary>
        /// Sets the current zoom level, clamping it within the defined minimum and maximum limits, and updates the canvas and rulers accordingly.
        /// </summary>
        /// <param name="zoom"></param>
        private void SetZoom(double zoom)
        {
            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
            var oldZoom = _currentZoom;
            _currentZoom = zoom;

            if (Math.Abs(oldZoom - zoom) > 0.01)
            {
                Logger.LogActionWithParams("MainWindow.SetZoom", "Alterar zoom",
                    $"De {oldZoom:P0} para {zoom:P0}");
            }

            if (CanvasScaleTransform != null)
            {
                CanvasScaleTransform.ScaleX = zoom;
                CanvasScaleTransform.ScaleY = zoom;
            }

            UpdateCanvasSize();
            UpdateZoomTextBox();
            UpdateRulers();
        }

        /// <summary>
        /// Updates the zoom text box to reflect the current zoom level as a percentage.
        /// </summary>
        private void UpdateZoomTextBox()
        {
            if (TxtZoom != null)
            {
                TxtZoom.Text = $"{Math.Round(_currentZoom * 100)}%";
            }
        }

        /// <summary>
        /// Updates the size of the dialog canvas based on the current zoom level and the dimensions of the dialog.
        /// </summary>
        private void UpdateCanvasSize()
        {
            if (LayoutController?.Dialog != null)
            {
                var baseWidth = Math.Max(800, LayoutController.Dialog.Width + 100);
                var baseHeight = Math.Max(600, LayoutController.Dialog.Height + 100);

                DialogCanvas.Width = baseWidth * _currentZoom;
                DialogCanvas.Height = baseHeight * _currentZoom;

                if (CanvasBorder != null)
                {
                    CanvasBorder.Width = DialogCanvas.Width;
                    CanvasBorder.Height = DialogCanvas.Height;
                }
            }
        }

        /// <summary>
        /// Handles the key down event on the zoom text box to apply the zoom level when the Enter key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtZoom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyZoomFromTextBox();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the lost focus event on the zoom text box to apply the zoom level when the text box loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtZoom_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyZoomFromTextBox();
        }

        /// <summary>
        /// Parses the zoom level from the zoom text box and applies it, or resets the text box if the input is invalid.
        /// </summary>
        private void ApplyZoomFromTextBox()
        {
            var text = TxtZoom.Text.Replace("%", "").Trim();
            if (double.TryParse(text, out var percentage))
            {
                var zoom = percentage / 100.0;
                SetZoom(zoom);
            }
            else
            {
                UpdateZoomTextBox();
            }
        }

        /// <summary>
        /// Handles the click event of the "Zoom In" button to increase the zoom level.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        /// <summary>
        /// Handles the click event of the "Zoom In" button to increase the zoom level.
        /// </summary>
        private void SetupRulers()
        {
            UpdateRulers();
        }

        /// <summary>
        /// Updates the rulers to match the current zoom level and dialog dimensions.
        /// </summary>
        private void UpdateRulers()
        {
            if (HorizontalRuler == null || VerticalRuler == null) return;

            HorizontalRuler.Children.Clear();
            VerticalRuler.Children.Clear();

            if (HorizontalRulerScale != null)
            {
                HorizontalRulerScale.ScaleX = _currentZoom;
                HorizontalRulerScale.ScaleY = 1.0;
            }
            if (VerticalRulerScale != null)
            {
                VerticalRulerScale.ScaleX = 1.0;
                VerticalRulerScale.ScaleY = _currentZoom;
            }

            CreateHorizontalRuler();
            CreateVerticalRuler();
        }

        /// <summary>
        /// Creates the horizontal ruler with ticks and labels based on the current dialog width.
        /// </summary>
        private void CreateHorizontalRuler()
        {
            if (LayoutController?.Dialog == null) return;

            var maxWidth = Math.Max(1000, LayoutController.Dialog.Width + 200);
            HorizontalRuler.Width = maxWidth;

            for (int x = 0; x <= maxWidth; x += RulerMinorTick)
            {
                var isMajor = x % RulerMajorTick == 0;
                var tickHeight = isMajor ? 15 : 8;
                var tickColor = isMajor ? Colors.Black : Colors.Gray;

                var tick = new Rectangle
                {
                    Width = 1,
                    Height = tickHeight,
                    Fill = new SolidColorBrush(tickColor),
                    Opacity = 0.7
                };
                Canvas.SetLeft(tick, x);
                Canvas.SetTop(tick, 25 - tickHeight);
                HorizontalRuler.Children.Add(tick);

                if (isMajor && x > 0)
                {
                    var text = new TextBlock
                    {
                        Text = x.ToString(),
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Black),
                        RenderTransform = new TranslateTransform(-8, 0)
                    };
                    Canvas.SetLeft(text, x);
                    Canvas.SetTop(text, 2);
                    HorizontalRuler.Children.Add(text);
                }
            }
        }

        /// <summary>
        /// Creates the vertical ruler with ticks and labels based on the current dialog height.
        /// </summary>
        private void CreateVerticalRuler()
        {
            if (LayoutController?.Dialog == null) return;

            var maxHeight = Math.Max(800, LayoutController.Dialog.Height + 200);
            VerticalRuler.Height = maxHeight;

            for (int y = 0; y <= maxHeight; y += RulerMinorTick)
            {
                var isMajor = y % RulerMajorTick == 0;
                var tickWidth = isMajor ? 15 : 8;
                var tickColor = isMajor ? Colors.Black : Colors.Gray;

                var tick = new Rectangle
                {
                    Width = tickWidth,
                    Height = 1,
                    Fill = new SolidColorBrush(tickColor),
                    Opacity = 0.7
                };
                Canvas.SetLeft(tick, 25 - tickWidth);
                Canvas.SetTop(tick, y);
                VerticalRuler.Children.Add(tick);

                if (isMajor && y > 0)
                {
                    var text = new TextBlock
                    {
                        Text = y.ToString(),
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Black),
                        RenderTransform = new RotateTransform(-90)
                    };
                    Canvas.SetLeft(text, 12);
                    Canvas.SetTop(text, y + 8);
                    VerticalRuler.Children.Add(text);
                }
            }
        }

        /// <summary>
        /// Handles the scroll changed event of the canvas scroll viewer to synchronize the scroll positions of the rulers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (HorizontalRulerScroll != null)
            {
                HorizontalRulerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            if (VerticalRulerScroll != null)
            {
                VerticalRulerScroll.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        /// <summary>
        /// Copies the XML of the currently selected controls to the clipboard.
        /// </summary>
        private void CopySelectedControls()
        {
            try
            {
                var selectedControls = DialogCanvas.Children.OfType<Canvas>()
                    .Where(c => c.Tag is UIControl && c.Children.OfType<Rectangle>()
                        .Any(r => r.Stroke == Brushes.LimeGreen && r.StrokeThickness == 2))
                    .ToList();

                if (!selectedControls.Any())
                {
                    Logger.LogAction("MainWindow.CopySelectedControls", "Nenhum controle selecionado para copiar");
                    return;
                }

                var control = selectedControls.First().Tag as UIControl;
                if (control == null)
                {
                    Logger.LogAction("MainWindow.CopySelectedControls", "Controle selecionado é null");
                    return;
                }

                Logger.LogAction("MainWindow.CopySelectedControls", $"Tentando copiar controle: {control.Name} na posição atual ({control.X}, {control.Y})");

                var originalXml = GetOriginalXmlForSelectedControl();
                if (!string.IsNullOrEmpty(originalXml))
                {
                    ClipboardManager.CopyControlXml(originalXml, control.X, control.Y);
                    Logger.LogAction("MainWindow.CopySelectedControls", $"Copiado XML original do controle {control.Name} - Length: {originalXml.Length} - Posição: ({control.X}, {control.Y})");
                }
                else
                {
                    Logger.LogAction("MainWindow.CopySelectedControls", $"Nao foi possivel extrair XML do controle {control.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.CopySelectedControls", "Erro ao copiar controles selecionados", ex);
            }
        }

        /// <summary>
        /// Pastes the copied controls from the clipboard into the current dialog, updating the XML and refreshing the layout.
        /// </summary>
        private void PasteCopiedControls()
        {
            try
            {
                if (!ClipboardManager.HasCopiedControlXml)
                {
                    Logger.LogAction("MainWindow.PasteCopiedControls", "Nenhum XML de controle copiado no clipboard");
                    return;
                }

                if (CurrentSourceFile == null || !File.Exists(CurrentSourceFile.FileName))
                {
                    Logger.LogAction("MainWindow.PasteCopiedControls", "Nenhum arquivo carregado para colar controles");
                    return;
                }

                var currentFileContent = File.ReadAllText(CurrentSourceFile.FileName);
                if (string.IsNullOrEmpty(currentFileContent))
                {
                    Logger.LogAction("MainWindow.PasteCopiedControls", "Arquivo atual está vazio");
                    return;
                }

                var pastedXml = ClipboardManager.PasteControlXml();
                if (!string.IsNullOrEmpty(pastedXml))
                {
                    Logger.LogAction("MainWindow.PasteCopiedControls", $"XML colado - Length: {pastedXml.Length}");

                    var updatedXml = AddPastedControlXmlToFile(currentFileContent, pastedXml);
                    Logger.LogAction("MainWindow.PasteCopiedControls", $"XML atualizado - Length: {updatedXml.Length}");

                    var preview = updatedXml.Length > 200 ? updatedXml.Substring(0, 200) + "..." : updatedXml;
                    Logger.LogAction("MainWindow.PasteCopiedControls", $"XML preview: {preview}");

                    File.WriteAllText(CurrentSourceFile.FileName, updatedXml);

                    TeFile.Document.BeginUpdate();
                    TeFile.Document.Text = updatedXml;
                    TeFile.Document.EndUpdate();

                    if (string.IsNullOrWhiteSpace(updatedXml) || !updatedXml.TrimStart().StartsWith("<"))
                    {
                        Logger.LogError("MainWindow.PasteCopiedControls", "XML inválido após colagem");
                        return;
                    }

                    var parseResult = LayoutController.Parse(updatedXml, System.IO.Path.GetDirectoryName(CurrentSourceFile.FileName));
                    if (parseResult == null)
                    {
                        Logger.LogAction("MainWindow.PasteCopiedControls", "Parse bem-sucedido, fazendo refresh");

                        LayoutController.RefreshLayout(DialogCanvas);

                        DialogCanvas.UpdateLayout();
                        DialogCanvas.InvalidateVisual();

                        Logger.LogAction("MainWindow.PasteCopiedControls", $"Modelo atualizado - StillImageButtons: {LayoutController.Dialog?.StillImageButtons?.Count ?? 0}");

                        TeFile.IsModified = true;
                        LockEditor(true);

                        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                        {
                            var controlName = GetControlNameFromXml(pastedXml);
                            Logger.LogAction("MainWindow.PasteCopiedControls", $"Tentando selecionar controle: {controlName}");
                            SelectPastedControlByName(controlName);
                            EnsureInitialUndoSnapshotIfNeeded();
                        }));

                        Logger.LogAction("MainWindow.PasteCopiedControls", "Controle colado com sucesso");
                    }
                    else
                    {
                        Logger.LogError("MainWindow.PasteCopiedControls", "Erro no parse após colagem", parseResult);
                    }
                }
                else
                {
                    Logger.LogAction("MainWindow.PasteCopiedControls", "Nenhum XML valido foi colado");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.PasteCopiedControls", "Erro ao colar controles", ex);
            }
        }

        /// <summary>
        /// Selects the pasted control in the canvas by its name.
        /// </summary>
        /// <returns></returns>
        private string GetOriginalXmlForSelectedControl()
        {
            try
            {
                if (DragDropController?.SelectedContainer?.Tag is UIControl control)
                {
                    return FindControlXmlInCurrentFile(control.Name);
                }
                return "";
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.GetOriginalXmlForSelectedControl", "Erro ao extrair XML do controle selecionado", ex);
                return "";
            }
        }

        /// <summary>
        /// Finds the XML definition of a control by its name in the current source file.
        /// </summary>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private string FindControlXmlInCurrentFile(string controlName)
        {
            try
            {
                if (CurrentSourceFile == null || !File.Exists(CurrentSourceFile.FileName))
                {
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", "Arquivo atual não encontrado");
                    return "";
                }

                var fileContent = File.ReadAllText(CurrentSourceFile.FileName);
                if (string.IsNullOrEmpty(fileContent))
                {
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", "Arquivo está vazio");
                    return "";
                }

                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", "Arquivo contém apenas espaços em branco");
                    return "";
                }

                if (!fileContent.TrimStart().StartsWith("<"))
                {
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", $"Arquivo não parece ser XML válido. Primeiros 50 chars: {fileContent.Substring(0, Math.Min(50, fileContent.Length))}");
                    return "";
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(fileContent);

                var controlElement = FindControlElementByName(xmlDoc.DocumentElement, controlName);
                if (controlElement != null)
                {
                    var xml = controlElement.OuterXml;
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", $"Encontrado controle {controlName} no XML - Length: {xml.Length}");
                    return xml;
                }
                else
                {
                    Logger.LogAction("MainWindow.FindControlXmlInCurrentFile", $"Controle {controlName} não encontrado no XML");
                }

                return "";
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.FindControlXmlInCurrentFile", $"Erro ao buscar XML do controle {controlName}", ex);
                return "";
            }
        }

        /// <summary>
        /// Recursively searches for a control element by its Name attribute in the given XML element and its children.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private XmlElement FindControlElementByName(XmlElement parent, string controlName)
        {
            if (parent == null) return null;

            if (parent.HasAttribute("Name") && parent.GetAttribute("Name") == controlName)
            {
                return parent;
            }

            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child is XmlElement childElement)
                {
                    var found = FindControlElementByName(childElement, controlName);
                    if (found != null) return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the pasted control XML to the current dialog XML, replacing any existing control with the same name.
        /// </summary>
        /// <param name="currentXml"></param>
        /// <param name="controlXml"></param>
        /// <returns></returns>
        private string AddPastedControlXmlToFile(string currentXml, string controlXml)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(currentXml);

                var dialogElement = xmlDoc.DocumentElement;
                if (dialogElement == null) return currentXml;

                var controlDoc = new XmlDocument();
                controlDoc.LoadXml(controlXml);
                var controlElement = controlDoc.DocumentElement;

                if (controlElement != null)
                {
                    var controlName = controlElement.Attributes["Name"]?.Value;
                    Logger.LogAction("MainWindow.AddPastedControlXmlToFile", $"Adicionando controle: {controlName}");

                    var existingControl = FindControlInXml(dialogElement, controlName);
                    if (existingControl != null)
                    {
                        Logger.LogAction("MainWindow.AddPastedControlXmlToFile", $"Controle {controlName} já existe no XML, removendo duplicata");
                        existingControl.ParentNode.RemoveChild(existingControl);
                    }

                    var importedNode = xmlDoc.ImportNode(controlElement, true);
                    dialogElement.AppendChild(importedNode);

                    Logger.LogAction("MainWindow.AddPastedControlXmlToFile", $"Controle {controlName} adicionado com sucesso ao XML");

                    var settings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true,
                        IndentChars = "    ",
                        NewLineHandling = NewLineHandling.Replace,
                        NewLineChars = "\r\n"
                    };

                    var sb = new StringBuilder();
                    using (var xw = XmlWriter.Create(new StringWriter(sb), settings))
                    {
                        xmlDoc.Save(xw);
                    }

                    return sb.ToString();
                }

                return currentXml;
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.AddPastedControlXmlToFile", "Erro ao adicionar XML do controle ao arquivo", ex);
                return currentXml;
            }
        }

        /// <summary>
        /// Finds a control node by its Name attribute within the given dialog XML element.
        /// </summary>
        /// <param name="dialogElement"></param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private XmlNode FindControlInXml(XmlNode dialogElement, string controlName)
        {
            try
            {
                foreach (XmlNode childNode in dialogElement.ChildNodes)
                {
                    if (childNode.Attributes != null && childNode.Attributes["Name"] != null)
                    {
                        var name = childNode.Attributes["Name"].Value;
                        if (string.Equals(name, controlName, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogAction("MainWindow.FindControlInXml", $"Encontrado controle {controlName} no XML");
                            return childNode;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.FindControlInXml", $"Erro ao buscar controle {controlName} no XML", ex);
                return null;
            }
        }

        /// <summary>
        /// Extracts the Name attribute from the given control XML string.
        /// </summary>
        /// <param name="controlXml"></param>
        /// <returns></returns>
        private string GetControlNameFromXml(string controlXml)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(controlXml);

                var rootElement = xmlDoc.DocumentElement;
                if (rootElement != null && rootElement.Attributes["Name"] != null)
                {
                    return rootElement.Attributes["Name"].Value;
                }

                return "";
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.GetControlNameFromXml", "Erro ao extrair nome do controle do XML", ex);
                return "";
            }
        }

        /// <summary>
        /// Selects the pasted control in the canvas by its name.
        /// </summary>
        /// <param name="controlName"></param>
        private void SelectPastedControlByName(string controlName)
        {
            try
            {
                if (string.IsNullOrEmpty(controlName))
                {
                    Logger.LogAction("MainWindow.SelectPastedControlByName", "Nome do controle está vazio");
                    return;
                }

                var canvasControls = DialogCanvas.Children.OfType<Canvas>().Where(c => c.Tag is UIControl).ToList();
                Logger.LogAction("MainWindow.SelectPastedControlByName", $"Canvas tem {canvasControls.Count} controles");

                var targetContainer = canvasControls.FirstOrDefault(c => c.Tag is UIControl control && control.Name == controlName);

                if (targetContainer != null)
                {
                    DragDropController.SelectControl(targetContainer);
                    Logger.LogAction("MainWindow.SelectPastedControlByName", $"Controle {controlName} selecionado após colagem");
                }
                else
                {
                    Logger.LogAction("MainWindow.SelectPastedControlByName", $"Controle {controlName} não encontrado no canvas após colagem");
                    var availableNames = canvasControls.Select(c => ((UIControl)c.Tag).Name).ToList();
                    Logger.LogAction("MainWindow.SelectPastedControlByName", $"Controles disponíveis: {string.Join(", ", availableNames)}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.SelectPastedControlByName", $"Erro ao selecionar controle {controlName}", ex);
            }
        }

        /// <summary>
        /// Adds the pasted controls to the LayoutController's dialog model.
        /// </summary>
        /// <param name="pastedControls"></param>
        private void AddControlsToModel(List<UIControl> pastedControls)
        {
            foreach (var control in pastedControls)
            {
                switch (control)
                {
                    case UIEditBox editBox:
                        if (LayoutController.Dialog.Edits == null)
                        {
                            LayoutController.Dialog.Edits = new List<UIEditBox>();
                        }
                        LayoutController.Dialog.Edits.Add(editBox);
                        Logger.LogAction("MainWindow.AddControlsToModel", $"Adicionado UIEditBox {editBox.Name} ao modelo");
                        break;

                    case UIImagePicture imagePicture:
                        if (LayoutController.Dialog.ImagePictures == null)
                        {
                            LayoutController.Dialog.ImagePictures = new List<UIImagePicture>();
                        }
                        LayoutController.Dialog.ImagePictures.Add(imagePicture);
                        Logger.LogAction("MainWindow.AddControlsToModel", $"Adicionado UIImagePicture {imagePicture.Name} ao modelo");
                        break;

                    case UIStillImageButton button:
                        if (LayoutController.Dialog.StillImageButtons == null)
                        {
                            LayoutController.Dialog.StillImageButtons = new List<UIStillImageButton>();
                        }
                        LayoutController.Dialog.StillImageButtons.Add(button);
                        Logger.LogAction("MainWindow.AddControlsToModel", $"Adicionado UIStillImageButton {button.Name} ao modelo");
                        break;

                    case UILabel label:
                        if (LayoutController.Dialog.Labels == null)
                        {
                            LayoutController.Dialog.Labels = new List<UILabel>();
                        }
                        LayoutController.Dialog.Labels.Add(label);
                        Logger.LogAction("MainWindow.AddControlsToModel", $"Adicionado UILabel {label.Name} ao modelo");
                        break;

                    default:
                        Logger.LogAction("MainWindow.AddControlsToModel", $"Tipo de controle não suportado: {control.GetType().Name}");
                        break;
                }
            }
        }

        /// <summary>
        /// Selects the pasted control in the canvas.
        /// </summary>
        /// <param name="control"></param>
        private void SelectPastedControl(UIControl control)
        {
            try
            {
                if (control == null || DragDropController == null) return;

                var container = DialogCanvas.Children.OfType<Canvas>()
                    .FirstOrDefault(c => c.Tag == control);

                if (container != null)
                {
                    DragDropController.SelectControl(container);
                    Logger.LogAction("MainWindow.SelectPastedControl", $"Controle {control.Name} selecionado após colar");
                }
                else
                {
                    Logger.LogAction("MainWindow.SelectPastedControl", $"Container não encontrado para controle {control.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.SelectPastedControl", "Erro ao selecionar controle colado", ex);
            }
        }

        /// <summary>
        /// Adds the pasted controls to the given XML text and returns the updated XML string.
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="pastedControls"></param>
        /// <returns></returns>
        private string AddPastedControlsToXml(string xmlText, List<UIControl> pastedControls)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlText) || pastedControls == null || pastedControls.Count == 0)
                {
                    return xmlText;
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlText);

                var dialogElement = xmlDoc.DocumentElement;
                if (dialogElement == null) return xmlText;

                foreach (var control in pastedControls)
                {
                    var controlElement = CreateXmlElementForControl(xmlDoc, control);
                    if (controlElement != null)
                    {
                        dialogElement.AppendChild(controlElement);
                        Logger.LogAction("MainWindow.AddPastedControlsToXml", $"Adicionado {control.Name} ao XML");
                    }
                }

                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    IndentChars = "    ",
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineChars = "\r\n"
                };
                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(new StringWriter(sb), settings))
                {
                    xmlDoc.Save(xw);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.AddPastedControlsToXml", "Erro ao adicionar controles colados ao XML", ex);
                return xmlText;
            }
        }

        /// <summary>
        /// Creates an XML element representing the given UI control.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        private XmlElement CreateXmlElementForControl(XmlDocument xmlDoc, UIControl control)
        {
            try
            {
                XmlElement element = null;

                switch (control)
                {
                    case UIEditBox editBox:
                        element = xmlDoc.CreateElement("EDIT");
                        AddAttribute(xmlDoc, element, "Name", editBox.Name);
                        AddAttribute(xmlDoc, element, "x", editBox.X.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "y", editBox.Y.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Width", editBox.Width.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Height", editBox.Height.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Text", editBox.Text);
                        AddAttribute(xmlDoc, element, "FontSize", editBox.FontSize.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Color", editBox.Color);
                        AddAttribute(xmlDoc, element, "ReadOnly", editBox.ReadOnly.ToString().ToLower());
                        AddAttribute(xmlDoc, element, "FileName", editBox.FileName);
                        break;

                    case UIImagePicture imagePicture:
                        element = xmlDoc.CreateElement("IMAGEPICTURE");
                        AddAttribute(xmlDoc, element, "Name", imagePicture.Name);
                        AddAttribute(xmlDoc, element, "x", imagePicture.X.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "y", imagePicture.Y.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Width", imagePicture.Width.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Height", imagePicture.Height.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "FileName", imagePicture.FileName);
                        AddAttribute(xmlDoc, element, "GfxFileName", imagePicture.GfxFileName);
                        break;

                    case UIStillImageButton button:
                        element = xmlDoc.CreateElement("STILLIMAGEBUTTON");
                        AddAttribute(xmlDoc, element, "Name", button.Name);
                        AddAttribute(xmlDoc, element, "x", button.X.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "y", button.Y.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Width", button.Width.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Height", button.Height.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "SoundEffect", button.SoundEffect);
                        AddAttribute(xmlDoc, element, "Command", button.Command);

                        if (!string.IsNullOrEmpty(button.Hint))
                        {
                            var hintElement = xmlDoc.CreateElement("Hint");
                            var hintAttr = xmlDoc.CreateAttribute("String");
                            hintAttr.Value = button.Hint;
                            hintElement.Attributes.Append(hintAttr);
                            element.AppendChild(hintElement);
                        }

                        if (!string.IsNullOrEmpty(button.Text))
                        {
                            var textElement = xmlDoc.CreateElement("Text");
                            var textAttr = xmlDoc.CreateAttribute("String");
                            textAttr.Value = button.Text;
                            textElement.Attributes.Append(textAttr);
                            element.AppendChild(textElement);
                        }

                        var resourceElement = xmlDoc.CreateElement("Resource");
                        var upImage = ConvertToRelativePath(button.UpImage);
                        var downImage = ConvertToRelativePath(button.DownImage);
                        var hoverImage = ConvertToRelativePath(button.HoverImage);

                        var frameUpElement = xmlDoc.CreateElement("FrameUpImage");
                        var frameUpAttr = xmlDoc.CreateAttribute("FileName");
                        frameUpAttr.Value = upImage;
                        frameUpElement.Attributes.Append(frameUpAttr);
                        resourceElement.AppendChild(frameUpElement);

                        var frameDownElement = xmlDoc.CreateElement("FrameDownImage");
                        var frameDownAttr = xmlDoc.CreateAttribute("FileName");
                        frameDownAttr.Value = downImage;
                        frameDownElement.Attributes.Append(frameDownAttr);
                        resourceElement.AppendChild(frameDownElement);

                        var frameHoverElement = xmlDoc.CreateElement("FrameOnHoverImage");
                        var frameHoverAttr = xmlDoc.CreateAttribute("FileName");
                        frameHoverAttr.Value = hoverImage;
                        frameHoverElement.Attributes.Append(frameHoverAttr);
                        resourceElement.AppendChild(frameHoverElement);

                        element.AppendChild(resourceElement);
                        break;

                    case UILabel label:
                        element = xmlDoc.CreateElement("LABEL");
                        AddAttribute(xmlDoc, element, "Name", label.Name);
                        AddAttribute(xmlDoc, element, "x", label.X.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "y", label.Y.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Width", label.Width.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Height", label.Height.ToString(CultureInfo.InvariantCulture));
                        AddAttribute(xmlDoc, element, "Color", label.Color);
                        AddAttribute(xmlDoc, element, "OutlineColor", label.OutlineColor);
                        AddAttribute(xmlDoc, element, "TextUpperColor", label.TextUpperColor);
                        AddAttribute(xmlDoc, element, "TextLowerColor", label.TextLowerColor);
                        AddAttribute(xmlDoc, element, "Align", label.Align.ToString());

                        if (!string.IsNullOrEmpty(label.Text))
                        {
                            var textElement = xmlDoc.CreateElement("Text");
                            var textAttr = xmlDoc.CreateAttribute("String");
                            textAttr.Value = label.Text;
                            textElement.Attributes.Append(textAttr);
                            element.AppendChild(textElement);
                        }
                        break;
                }

                return element;
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.CreateXmlElementForControl",
                    $"Erro ao criar elemento XML para controle {control.Name}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Adds an attribute to the given XML element if the value is not null or empty.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void AddAttribute(XmlDocument xmlDoc, XmlElement element, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var attribute = xmlDoc.CreateAttribute(name);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }

        /// <summary>
        /// Converts an absolute file path to a relative path based on known directory structures.
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        private string ConvertToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return absolutePath;

            try
            {
                if (!absolutePath.StartsWith("C:") && !absolutePath.StartsWith("\\"))
                {
                    return absolutePath;
                }

                var surfacesIndex = absolutePath.IndexOf("\\surfaces\\", StringComparison.OrdinalIgnoreCase);
                if (surfacesIndex >= 0)
                {
                    return absolutePath.Substring(surfacesIndex + "\\surfaces".Length);
                }

                var versionIndex = absolutePath.IndexOf("\\version02\\", StringComparison.OrdinalIgnoreCase);
                if (versionIndex >= 0)
                {
                    return absolutePath.Substring(versionIndex);
                }

                return absolutePath;
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow.ConvertToRelativePath", $"Erro ao converter caminho {absolutePath}: {ex.Message}");
                return absolutePath;
            }
        }
        #endregion
    }
}
