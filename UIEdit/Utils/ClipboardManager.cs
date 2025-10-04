using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using UIEdit.Models;

// By: SpinxDev 2025 xD
namespace UIEdit.Utils
{
    public static class ClipboardManager
    {
        #region Injection Properties
        private static List<UIControl> _copiedControls = new List<UIControl>();
        private static bool _hasCopiedControls = false;
        private static string _copiedControlXml = "";
        private static double _referenceX = 0;
        private static double _referenceY = 0;
        public static bool HasCopiedControls => _hasCopiedControls;
        public static bool HasCopiedControlXml => !string.IsNullOrEmpty(_copiedControlXml);
        #endregion

        #region Constructor
        #endregion

        #region Methods
        /// <summary>
        /// Copys the given list of controls to the clipboard manager.
        /// </summary>
        /// <param name="controls"></param>
        public static void CopyControls(List<UIControl> controls)
        {
            if (controls == null || controls.Count == 0)
            {
                _copiedControls.Clear();
                _hasCopiedControls = false;
                return;
            }

            _copiedControls.Clear();

            foreach (var control in controls)
            {
                var copiedControl = CloneControl(control);
                if (copiedControl != null)
                {
                    _copiedControls.Add(copiedControl);
                }
            }

            _hasCopiedControls = _copiedControls.Count > 0;

            if (_hasCopiedControls)
            {
                Logger.LogAction("ClipboardManager.CopyControls",
                    $"Copiados {_copiedControls.Count} controles para o clipboard");
            }
        }

        /// <summary>
        /// Returns a copy of the currently copied controls.
        /// </summary>
        /// <returns></returns>
        public static List<UIControl> GetCopiedControls()
        {
            return _copiedControls.ToList();
        }

        /// <summary>
        /// Pastes the copied controls, generating unique names and adjusting positions.
        /// </summary>
        /// <param name="existingControls"></param>
        /// <returns></returns>
        public static List<UIControl> PasteControls(List<UIControl> existingControls)
        {
            if (!_hasCopiedControls || _copiedControls.Count == 0)
            {
                return new List<UIControl>();
            }

            var pastedControls = new List<UIControl>();
            var existingNames = existingControls?.Select(c => c.Name).ToHashSet() ?? new HashSet<string>();

            foreach (var copiedControl in _copiedControls)
            {
                var pastedControl = CloneControl(copiedControl);
                if (pastedControl != null)
                {
                    pastedControl.Name = GenerateUniqueName(copiedControl.Name, existingNames);
                    existingNames.Add(pastedControl.Name);

                    pastedControl.X += 10;
                    pastedControls.Add(pastedControl);
                }
            }

            if (pastedControls.Count > 0)
            {
                Logger.LogAction("ClipboardManager.PasteControls",
                    $"Colados {pastedControls.Count} controles do clipboard");
            }

            return pastedControls;
        }

        /// <summary>
        /// Clones a UIControl, creating a new instance with the same properties.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private static UIControl CloneControl(UIControl original)
        {
            if (original == null) return null;

            try
            {
                Logger.LogAction("ClipboardManager.CloneControl", $"Clonando controle {original.Name} do tipo {original.GetType().Name}");

                switch (original)
                {
                    case UIEditBox editBox:
                        var clonedEdit = new UIEditBox
                        {
                            Name = editBox.Name,
                            X = editBox.X,
                            Y = editBox.Y,
                            Width = editBox.Width,
                            Height = editBox.Height,
                            Text = editBox.Text,
                            FontSize = editBox.FontSize,
                            Color = editBox.Color,
                            ReadOnly = editBox.ReadOnly,
                            FileName = editBox.FileName
                        };
                        Logger.LogAction("ClipboardManager.CloneControl", $"UIEditBox clonado: {clonedEdit.Name}");
                        return clonedEdit;

                    case UIImagePicture imagePicture:
                        var clonedImage = new UIImagePicture
                        {
                            Name = imagePicture.Name,
                            X = imagePicture.X,
                            Y = imagePicture.Y,
                            Width = imagePicture.Width,
                            Height = imagePicture.Height,
                            FileName = imagePicture.FileName,
                            GfxFileName = imagePicture.GfxFileName
                        };
                        Logger.LogAction("ClipboardManager.CloneControl", $"UIImagePicture clonado: {clonedImage.Name}");
                        return clonedImage;

                    case UIStillImageButton button:
                        var clonedButton = new UIStillImageButton
                        {
                            Name = button.Name,
                            X = button.X,
                            Y = button.Y,
                            Width = button.Width,
                            Height = button.Height,
                            SoundEffect = button.SoundEffect,
                            Command = button.Command,
                            Hint = button.Hint,
                            Text = button.Text,
                            UpImage = button.UpImage,
                            DownImage = button.DownImage,
                            HoverImage = button.HoverImage,
                            Color = button.Color,
                            OutlineColor = button.OutlineColor,
                            InnerColor = button.InnerColor
                        };
                        Logger.LogAction("ClipboardManager.CloneControl", $"UIStillImageButton clonado: {clonedButton.Name} - UpImage: {clonedButton.UpImage}");
                        return clonedButton;

                    case UILabel label:
                        var clonedLabel = new UILabel
                        {
                            Name = label.Name,
                            X = label.X,
                            Y = label.Y,
                            Width = label.Width,
                            Height = label.Height,
                            Text = label.Text,
                            Color = label.Color,
                            OutlineColor = label.OutlineColor,
                            TextUpperColor = label.TextUpperColor,
                            TextLowerColor = label.TextLowerColor,
                            Align = label.Align
                        };
                        Logger.LogAction("ClipboardManager.CloneControl", $"UILabel clonado: {clonedLabel.Name}");
                        return clonedLabel;

                    default:
                        var clonedControl = new UIControl
                        {
                            Name = original.Name,
                            X = original.X,
                            Y = original.Y,
                            Width = original.Width,
                            Height = original.Height
                        };
                        Logger.LogAction("ClipboardManager.CloneControl", $"UIControl basico clonado: {clonedControl.Name}");
                        return clonedControl;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ClipboardManager.CloneControl",
                    $"Erro ao clonar controle {original.Name}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Generates a unique name based on the original name and existing names.
        /// </summary>
        /// <param name="originalName"></param>
        /// <param name="existingNames"></param>
        /// <returns></returns>
        private static string GenerateUniqueName(string originalName, HashSet<string> existingNames)
        {
            if (string.IsNullOrEmpty(originalName))
            {
                originalName = "Control";
            }

            var baseName = originalName;
            var counter = 1;
            var newName = $"{baseName}_Copy_{counter}";

            while (existingNames.Contains(newName))
            {
                counter++;
                newName = $"{baseName}_Copy_{counter}";
            }

            return newName;
        }

        /// <summary>
        /// Copy the given control XML to the clipboard manager, along with a reference position.
        /// </summary>
        /// <param name="controlXml"></param>
        /// <param name="referenceX"></param>
        /// <param name="referenceY"></param>
        public static void CopyControlXml(string controlXml, double referenceX = 0, double referenceY = 0)
        {
            _copiedControlXml = controlXml;
            _referenceX = referenceX;
            _referenceY = referenceY;
            Logger.LogAction("ClipboardManager.CopyControlXml", $"XML do controle copiado para o clipboard - Posicao de referencia: ({referenceX}, {referenceY})");
        }

        /// <summary>
        /// Pastes the copied control XML, generating unique names and adjusting positions.
        /// </summary>
        /// <returns></returns>
        public static string PasteControlXml()
        {
            if (string.IsNullOrEmpty(_copiedControlXml))
            {
                return "";
            }

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(_copiedControlXml);

                var rootElement = xmlDoc.DocumentElement;
                if (rootElement != null)
                {
                    var originalName = rootElement.Attributes["Name"]?.Value;
                    var newName = GenerateUniqueNameFromXml(originalName);
                    rootElement.SetAttribute("Name", newName);

                    if (rootElement.Attributes["Command"] != null)
                    {
                        var newCommand = GenerateUniqueCommandName();
                        rootElement.SetAttribute("Command", newCommand);
                        Logger.LogAction("ClipboardManager.PasteControlXml", $"Command definido como: {newCommand}");
                    }

                    if (rootElement.Attributes["x"] != null)
                    {
                        var x = double.Parse(rootElement.Attributes["x"].Value);
                        var newX = _referenceX + 10;
                        rootElement.SetAttribute("x", newX.ToString());
                        Logger.LogAction("ClipboardManager.PasteControlXml", $"Posicao ajustada: {x} -> {newX} (referencia: {_referenceX})");
                    }
                }

                Logger.LogAction("ClipboardManager.PasteControlXml", "XML do controle preparado para colagem");
                return xmlDoc.OuterXml;
            }
            catch (Exception ex)
            {
                Logger.LogError("ClipboardManager.PasteControlXml", "Erro ao preparar XML para colagem", ex);
                return "";
            }
        }

        /// <summary>
        /// Generates a unique control name based on the original name by checking against existing names in the current XML.
        /// </summary>
        /// <param name="originalName"></param>
        /// <returns></returns>
        private static string GenerateUniqueNameFromXml(string originalName)
        {
            if (string.IsNullOrEmpty(originalName))
            {
                originalName = "Control";
            }

            var baseName = originalName;
            var counter = 1;
            var newName = $"{baseName}_Copy_{counter}";

            while (DoesControlNameExistInCurrentXml(newName))
            {
                counter++;
                newName = $"{baseName}_Copy_{counter}";
            }

            return newName;
        }

        /// <summary>
        /// Checks if a control name already exists in the current XML of the MainWindow.
        /// </summary>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private static bool DoesControlNameExistInCurrentXml(string controlName)
        {
            try
            {
                var mainWindow = Application.Current?.MainWindow as UIEdit.Windows.MainWindow;
                if (mainWindow?.CurrentSourceFile == null || !File.Exists(mainWindow.CurrentSourceFile.FileName))
                {
                    return false;
                }

                var currentXml = File.ReadAllText(mainWindow.CurrentSourceFile.FileName);
                if (string.IsNullOrEmpty(currentXml))
                {
                    return false;
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(currentXml);

                var dialogElement = xmlDoc.DocumentElement;
                if (dialogElement == null) return false;

                foreach (XmlNode childNode in dialogElement.ChildNodes)
                {
                    if (childNode.Attributes != null && childNode.Attributes["Name"] != null)
                    {
                        var name = childNode.Attributes["Name"].Value;
                        if (string.Equals(name, controlName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError("ClipboardManager.DoesControlNameExistInCurrentXml",
                    $"Erro ao verificar se nome {controlName} existe no XML", ex);
                return false;
            }
        }

        /// <summary>
        /// Generates a unique command name for pasted controls.
        /// </summary>
        /// <returns></returns>
        private static string GenerateUniqueCommandName()
        {
            var counter = 1;
            var newCommand = $"command_copy_{counter}";

            return newCommand;
        }

        /// <summary>
        /// Clears the clipboard manager state.
        /// </summary>
        public static void Clear()
        {
            _copiedControls.Clear();
            _hasCopiedControls = false;
            _copiedControlXml = "";
            _referenceX = 0;
            _referenceY = 0;
            Logger.LogAction("ClipboardManager.Clear", "Clipboard limpo");
        }
        #endregion
    }
}
