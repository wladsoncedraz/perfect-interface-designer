using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using UIEdit.Models;
using UIEdit.Utils;

// By: SpinxDev 2025 xD
namespace UIEdit.Controllers
{
    public class LayoutController
    {

        #region Injection Properties
        public string SourceText { get; set; }
        public UIDialog Dialog { get; set; }
        public bool ShowAllOutlines { get; set; }
        #endregion

        #region Constructor
        #endregion

        #region Methods
        /// <summary>
        /// Parses the given XML text into the Dialog model.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public Exception Parse(string text, string path)
        {
            var isValid = true;

            try
            {
                Logger.LogOperationStart("LayoutController.Parse", $"Parse XML - Arquivo: {System.IO.Path.GetFileName(path)}");
                var safe = (text ?? string.Empty).TrimStart('\uFEFF', '\u200B', '\u200E', '\u200F', ' ', '\t', '\r', '\n');
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(safe);

                if (Dialog != null)
                {
                    Dialog.Lists.Clear();
                    Dialog.Progresses.Clear();
                    Dialog.ImagePictures.Clear();
                    Dialog.StillImageButtons.Clear();
                    Dialog.Labels.Clear();
                    Dialog.Edits.Clear();
                    Dialog.RadioButtons.Clear();
                    Dialog.Scrolls.Clear();
                }

                Dialog = new UIDialog
                {
                    Name = xmlDoc.DocumentElement.Attributes["Name"].Value
                };

                if (xmlDoc.DocumentElement.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width"))
                    Dialog.Width = Convert.ToDouble(xmlDoc.DocumentElement.Attributes["Width"].Value);
                if (xmlDoc.DocumentElement.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height"))
                    Dialog.Height = Convert.ToDouble(xmlDoc.DocumentElement.Attributes["Height"].Value);
                if (xmlDoc.DocumentElement.ChildNodes.Cast<XmlNode>().Any(t => t.Name == "Resource"))
                {
                    var dlgRes = xmlDoc.DocumentElement.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "Resource");
                    if (dlgRes.ChildNodes.Cast<XmlNode>().Any(t => t.Name == "FrameImage"))
                        Dialog.FrameImage = new UIResourceFrameImage
                        {
                            FileName = string.Format(@"{0}\{1}", path, dlgRes.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "FrameImage").Attributes["FileName"].Value)
                        };
                }

                foreach (XmlNode element in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (element.Name)
                    {
                        case "LIST":
                            var list = new UIList { Name = element.Attributes["Name"].Value };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) list.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) list.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) list.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) list.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Align")) list.Align = Convert.ToInt32(element.Attributes["Align"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "LineSpace")) list.LineSpace = Convert.ToDouble(element.Attributes["LineSpace"].Value);
                            var resList = element.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "Resource");
                            if (resList != null)
                            {
                                list.FrameImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "FrameImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "FrameImage").Attributes["FileName"].Value) : null;
                                list.HilightImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "HilightImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "HilightImage").Attributes["FileName"].Value) : null;
                                list.UpImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "UpImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "UpImage").Attributes["FileName"].Value) : null;
                                list.DownImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "DownImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "DownImage").Attributes["FileName"].Value) : null;
                                list.ScrollImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "ScrollImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "ScrollImage").Attributes["FileName"].Value) : null;
                                list.BarImage = resList.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "BarImage")?.Attributes?["FileName"]?.Value != null ? string.Format(@"{0}\{1}", path, resList.ChildNodes.Cast<XmlNode>().First(n => n.Name == "BarImage").Attributes["FileName"].Value) : null;
                            }
                            if (element.ChildNodes.Cast<XmlNode>().Any(n => n.Name == "Text")) list.Text = element.ChildNodes.Cast<XmlNode>().First(n => n.Name == "Text").Attributes?["String"]?.Value;
                            Dialog.Lists.Add(list);
                            break;
                        case "PROGRESS":
                            var prog = new UIProgress { Name = element.Attributes["Name"].Value };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) prog.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) prog.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) prog.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) prog.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "VerticalProgress")) prog.VerticalProgress = string.Equals(element.Attributes["VerticalProgress"].Value, "true", StringComparison.OrdinalIgnoreCase);
                            var resProg = element.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "Resource");
                            if (resProg != null)
                            {
                                var fill = resProg.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "FillImage");
                                if (fill != null && fill.Attributes["FileName"] != null)
                                    prog.FillImage = string.Format(@"{0}\{1}", path, fill.Attributes["FileName"].Value);
                            }
                            prog.Value = 0.5;
                            Dialog.Progresses.Add(prog);
                            break;
                        case "IMAGEPICTURE":
                            var imageControl = new UIImagePicture
                            {
                                Name = element.Attributes["Name"].Value,
                            };
                            var resNode = element.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "Resource");
                            if (resNode != null)
                            {
                                var frame = resNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "FrameImage");
                                if (frame != null && frame.Attributes["FileName"] != null)
                                    imageControl.FileName = string.Format(@"{0}\{1}", path, frame.Attributes["FileName"].Value);
                                var gfx = resNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "Gfx");
                                if (gfx != null && gfx.Attributes["FileName"] != null)
                                    imageControl.GfxFileName = string.Format(@"{0}\{1}", path, gfx.Attributes["FileName"].Value);
                            }
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) imageControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) imageControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) imageControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) imageControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            Dialog.ImagePictures.Add(imageControl);
                            break;
                        case "SCROLL":
                            var scrollControl = new UIScroll
                            {
                                Name = element.Attributes["Name"].Value
                            };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) scrollControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) scrollControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) scrollControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) scrollControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            foreach (var node in element.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Resource"))
                            {
                                var upImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "UpImage");
                                if (upImage != null) scrollControl.UpImage = string.Format(@"{0}\{1}", path, upImage.Attributes["FileName"].Value);

                                var downImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "DownImage");
                                if (downImage != null) scrollControl.DownImage = string.Format(@"{0}\{1}", path, downImage.Attributes["FileName"].Value);

                                var scrollImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "ScrollImage");
                                if (scrollImage != null) scrollControl.ScrollImage = string.Format(@"{0}\{1}", path, scrollImage.Attributes["FileName"].Value);

                                var barFrameImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "BarFrameImage");
                                if (barFrameImage != null) scrollControl.BarFrameImage = string.Format(@"{0}\{1}", path, barFrameImage.Attributes["FileName"].Value);
                            }
                            Dialog.Scrolls.Add(scrollControl);
                            break;
                        case "RADIO":
                            var radioControl = new UIRadioButton
                            {
                                Name = element.Attributes["Name"].Value,
                            };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) radioControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) radioControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) radioControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) radioControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.ChildNodes != null && element.ChildNodes[0].Name == "Hint")
                            {
                                radioControl.Hint = element.ChildNodes != null
                                                   ? element.ChildNodes[0].Attributes["String"].Value
                                                   : "";
                            }
                            foreach (var node in element.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Resource"))
                            {
                                var normalImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "NormalImage");
                                if (normalImage != null) radioControl.NormalImage = string.Format(@"{0}\{1}", path, normalImage.Attributes["FileName"].Value);

                                var checkedImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "CheckedImage");
                                if (checkedImage != null) radioControl.CheckedImage = string.Format(@"{0}\{1}", path, checkedImage.Attributes["FileName"].Value);

                            }
                            Dialog.RadioButtons.Add(radioControl);
                            break;
                        case "STILLIMAGEBUTTON":
                            var buttonControl = new UIStillImageButton
                            {
                                Name = element.Attributes["Name"].Value,
                            };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) buttonControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) buttonControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) buttonControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) buttonControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.ChildNodes != null && element.ChildNodes[0].Name == "Hint")
                            {
                                buttonControl.Hint = element.ChildNodes != null
                                                   ? element.ChildNodes[0].Attributes["String"].Value
                                                   : "";
                            }
                            if (element.ChildNodes.Cast<XmlNode>().Any(node => node.Name == "Text"))
                            {
                                var textNode = element.ChildNodes.Cast<XmlNode>().First(node => node.Name == "Text");
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "String")) buttonControl.Text = textNode.Attributes["String"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "FontSize")) buttonControl.FontSize = Convert.ToDouble(textNode.Attributes["FontSize"].Value);
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Color")) buttonControl.Color = textNode.Attributes["Color"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "OutlineColor")) buttonControl.OutlineColor = textNode.Attributes["OutlineColor"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "InnerColor")) buttonControl.InnerColor = textNode.Attributes["InnerColor"].Value;
                            }
                            foreach (var node in element.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Resource"))
                            {
                                var upImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "FrameUpImage");
                                if (upImage != null) buttonControl.UpImage = string.Format(@"{0}\{1}", path, upImage.Attributes["FileName"].Value);

                                var downImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "FrameDownImage");
                                if (downImage != null) buttonControl.DownImage = string.Format(@"{0}\{1}", path, downImage.Attributes["FileName"].Value);

                                var hoverImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "FrameOnHoverImage");
                                if (hoverImage != null) buttonControl.HoverImage = string.Format(@"{0}\{1}", path, hoverImage.Attributes["FileName"].Value);

                            }
                            Dialog.StillImageButtons.Add(buttonControl);
                            break;
                        case "LABEL":
                            var labelControl = new UILabel
                            {
                                Name = element.Attributes["Name"].Value,
                            };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) labelControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) labelControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) labelControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) labelControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.ChildNodes.Cast<XmlNode>().Any(node => node.Name == "Text"))
                            {
                                var textNode = element.ChildNodes.Cast<XmlNode>().First(node => node.Name == "Text");
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "String")) labelControl.Text = textNode.Attributes["String"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "FontSize")) labelControl.FontSize = Convert.ToDouble(textNode.Attributes["FontSize"].Value);
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "FontName")) labelControl.FontName = textNode.Attributes["FontName"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Color")) labelControl.Color = textNode.Attributes["Color"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "OutlineColor")) labelControl.OutlineColor = textNode.Attributes["OutlineColor"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "TextUpperColor")) labelControl.TextUpperColor = textNode.Attributes["TextUpperColor"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "TextLowerColor")) labelControl.TextLowerColor = textNode.Attributes["TextLowerColor"].Value;
                            }
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Align")) labelControl.Align = Convert.ToInt32(element.Attributes["Align"].Value);
                            Dialog.Labels.Add(labelControl);
                            break;
                        case "TEXT":
                            var textLabel = new UILabel { Name = element.Attributes["Name"].Value };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) textLabel.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) textLabel.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) textLabel.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) textLabel.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            var txNode = element.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name == "Text");
                            if (txNode != null)
                            {
                                if (txNode.Attributes.Cast<XmlAttribute>().Any(a => a.Name == "String")) textLabel.Text = txNode.Attributes["String"].Value;
                                if (txNode.Attributes.Cast<XmlAttribute>().Any(a => a.Name == "FontSize")) textLabel.FontSize = Convert.ToDouble(txNode.Attributes["FontSize"].Value);
                                if (txNode.Attributes.Cast<XmlAttribute>().Any(a => a.Name == "Color")) textLabel.Color = txNode.Attributes["Color"].Value;
                            }
                            Dialog.Labels.Add(textLabel);
                            break;
                        case "EDIT":
                            var editControl = new UIEditBox
                            {
                                Name = element.Attributes["Name"].Value
                            };
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "x")) editControl.X = Convert.ToDouble(element.Attributes["x"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "y")) editControl.Y = Convert.ToDouble(element.Attributes["y"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Width")) editControl.Width = Convert.ToDouble(element.Attributes["Width"].Value);
                            if (element.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Height")) editControl.Height = Convert.ToDouble(element.Attributes["Height"].Value);
                            if (element.ChildNodes.Cast<XmlNode>().Any(node => node.Name == "Text"))
                            {
                                var textNode = element.ChildNodes.Cast<XmlNode>().First(node => node.Name == "Text");
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "String")) editControl.Text = textNode.Attributes["String"].Value;
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "FontSize")) editControl.FontSize = Convert.ToDouble(textNode.Attributes["FontSize"].Value);
                                if (textNode.Attributes.Cast<XmlAttribute>().Any(t => t.Name == "Color")) editControl.Color = textNode.Attributes["Color"].Value;
                            }
                            foreach (var node in element.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Resource"))
                            {
                                var frameImage = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(t => t.Name == "FrameImage");
                                if (frameImage != null) editControl.FrameImage = new UIResourceFrameImage { FileName = string.Format(@"{0}\{1}", path, frameImage.Attributes["FileName"].Value) };
                            }
                            Dialog.Edits.Add(editControl);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("LayoutController.Parse", "Erro ao fazer parse do XML", e);
                return e;
            }

            Logger.LogOperationEnd("LayoutController.Parse", $"Parse XML - Arquivo: {System.IO.Path.GetFileName(path)}", true);
            return null;
        }

        /// <summary>
        /// Refreshes the layout on the given canvas based on the current Dialog model.
        /// </summary>
        /// <param name="dialogCanvas"></param>
        public void RefreshLayout(Canvas dialogCanvas)
        {
            dialogCanvas.Children.Clear();
            if (Dialog == null)
            {
                return;
            }

            var dialogContainer = new Canvas
            {
                ToolTip = Dialog.Name,
                Width = Dialog.Width,
                Height = Dialog.Height,
                Margin = new Thickness(0, 0, 0, 0),
                Tag = Dialog,
                Background = new SolidColorBrush(Color.FromArgb(180, 128, 128, 128))
            };

            dialogContainer.Children.Add(new Image
            {
                Width = Dialog.Width,
                Height = Dialog.Height,
                Stretch = Stretch.Fill,
                StretchDirection = StretchDirection.Both,
                Source = Dialog.FrameImage == null ? new BitmapImage() : Core.TrueStretchImage(Dialog.FrameImage.FileName, Dialog.Width, Dialog.Height)
            });

            Panel.SetZIndex(dialogContainer, -1);
            dialogCanvas.Children.Add(dialogContainer);
            foreach (var control in Dialog.Edits.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = control.Name,
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(control.X, control.Y, 0, 0),
                    Tag = control,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, control.ZIndex);
                container.Children.Add(new Image
                {
                    Width = control.Width,
                    Height = control.Height,
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both,
                    Source = Core.TrueStretchImage(control.FrameImage != null ? control.FrameImage.FileName : null, control.Width, control.Height)
                });
                var editText = new TextBlock
                {
                    Text = string.IsNullOrEmpty(control.Text) ? control.Name : control.Text,
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(0, control.FontSize > 1 ? ((control.Height - control.FontSize) / 4) : 0, 0, 0),
                    Foreground = Core.GetColorBrushFromString(control.Color)
                };
                if (control.FontSize > 1) editText.FontSize = control.FontSize;
                container.Children.Add(editText);
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var pic in Dialog.ImagePictures.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = pic.Name,
                    Width = pic.Width,
                    Height = pic.Height,
                    Margin = new Thickness(pic.X, pic.Y, 0, 0),
                    Tag = pic,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, pic.ZIndex);
                if (!string.IsNullOrEmpty(pic.FileName))
                {
                    container.Children.Add(new Image
                    {
                        Width = pic.Width,
                        Height = pic.Height,
                        Stretch = Stretch.Fill,
                        StretchDirection = StretchDirection.Both,
                        Source = Core.GetImageSourceFromFileName(pic.FileName)
                    });
                }
                else if (!string.IsNullOrEmpty(pic.GfxFileName))
                {
                    container.Children.Add(new Rectangle
                    {
                        Width = pic.Width,
                        Height = pic.Height,
                        Stroke = Brushes.LightSkyBlue,
                        StrokeThickness = 1,
                        Fill = Brushes.Transparent
                    });
                    container.Children.Add(new TextBlock
                    {
                        Text = System.IO.Path.GetFileName(pic.GfxFileName),
                        Foreground = Brushes.LightSkyBlue,
                        FontSize = 10,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Width = pic.Width,
                        Height = pic.Height
                    });
                }
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var list in Dialog.Lists.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = list.Name,
                    Width = list.Width,
                    Height = list.Height,
                    Margin = new Thickness(list.X, list.Y, 0, 0),
                    Tag = list,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, list.ZIndex);
                if (!string.IsNullOrEmpty(list.FrameImage))
                {
                    container.Children.Add(new Image
                    {
                        Width = list.Width,
                        Height = list.Height,
                        Stretch = Stretch.Fill,
                        StretchDirection = StretchDirection.Both,
                        Source = Core.TrueStretchImage(list.FrameImage, list.Width, list.Height)
                    });
                }
                if (!string.IsNullOrEmpty(list.ScrollImage))
                {
                    container.Children.Add(new Image
                    {
                        Width = 12,
                        Height = list.Height - 10,
                        Margin = new Thickness(list.Width - 14, 5, 0, 0),
                        Stretch = Stretch.Fill,
                        Source = Core.TrueStretchImage(list.ScrollImage, 12, list.Height - 10)
                    });
                }
                if (!string.IsNullOrEmpty(list.BarImage))
                {
                    container.Children.Add(new Image
                    {
                        Width = 12,
                        Height = 24,
                        Margin = new Thickness(list.Width - 14, 5, 0, 0),
                        Stretch = Stretch.Fill,
                        Source = Core.TrueStretchImage(list.BarImage, 12, 24)
                    });
                }
                if (!string.IsNullOrEmpty(list.Text))
                {
                    var tb = new TextBlock
                    {
                        Text = list.Text,
                        Foreground = Brushes.White,
                        Margin = new Thickness(6, 4, 6, 4),
                        TextWrapping = TextWrapping.Wrap,
                        Width = list.Width - 12,
                        Height = list.Height - 8
                    };
                    container.Children.Add(tb);
                }
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var control in Dialog.Scrolls.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = control.Name,
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(control.X, control.Y, 0, 0),
                    Tag = control,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, control.ZIndex);
                var frameImg = new Image
                {
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both,
                    Width = control.Width,
                    Height = control.Height,
                    Source = Core.GetImageSourceFromFileName(control.BarFrameImage)
                };
                container.Children.Add(frameImg);
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var control in Dialog.RadioButtons.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = string.IsNullOrEmpty(control.Hint) ? control.Name : string.Format("{0}: {1}", control.Name, control.Hint),
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(control.X, control.Y, 0, 0),
                    Tag = control,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, control.ZIndex);
                container.Children.Add(new Image
                {
                    Width = control.Width,
                    Height = control.Height,
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both,
                    Source = Core.GetImageSourceFromFileName(control.NormalImage)
                });
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var pg in Dialog.Progresses.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = pg.Name,
                    Width = pg.Width,
                    Height = pg.Height,
                    Margin = new Thickness(pg.X, pg.Y, 0, 0),
                    Tag = pg,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, pg.ZIndex);
                var bar = new Image
                {
                    Width = pg.Width,
                    Height = pg.Height,
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both,
                    Source = Core.GetImageSourceFromFileName(pg.FillImage)
                };
                if (pg.VerticalProgress)
                {
                    var clipHeight = Math.Max(0, pg.Height * pg.Value);
                    bar.Clip = new RectangleGeometry(new Rect(0, pg.Height - clipHeight, pg.Width, clipHeight));
                }
                else
                {
                    var clipWidth = Math.Max(0, pg.Width * pg.Value);
                    bar.Clip = new RectangleGeometry(new Rect(0, 0, clipWidth, pg.Height));
                }
                container.Children.Add(bar);
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            Logger.LogAction("LayoutController.RefreshLayout", $"Processando {Dialog.StillImageButtons?.Count ?? 0} StillImageButtons");
            foreach (var control in Dialog.StillImageButtons.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                Logger.LogAction("LayoutController.RefreshLayout", $"Criando container para StillImageButton: {control.Name} - UpImage: {control.UpImage}");
                var container = new Canvas
                {
                    ToolTip = string.IsNullOrEmpty(control.Hint) ? control.Name : control.Hint,
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(control.X, control.Y, 0, 0),
                    Tag = control,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, control.ZIndex);
                container.Children.Add(new Image
                {
                    Width = control.Width,
                    Height = control.Height,
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both,
                    Source = Core.TrueStretchImage(control.UpImage, control.Width, control.Height)
                });
                var tb = new TextBlock
                {
                    ToolTip = string.IsNullOrEmpty(control.Hint) ? control.Name : string.Format("{0}: {1}", control.Name, control.Hint),
                    Text = string.IsNullOrEmpty(control.Text) ? control.Name : control.Text,
                    Width = control.Width,
                    Height = control.Height,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, control.FontSize > 1 ? ((control.Height - control.FontSize) / 4) : 0, 0, 0),
                    Foreground = Core.GetColorBrushFromString(control.Color),
                    FontWeight = FontWeight.FromOpenTypeWeight(999)
                };
                if (control.FontSize > 1) tb.FontSize = control.FontSize;
                container.Children.Add(tb);
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
            foreach (var control in Dialog.Labels.OrderBy(c => c.ZIndex).ThenBy(c => c.Name))
            {
                var container = new Canvas
                {
                    ToolTip = control.Name,
                    Width = control.Width,
                    Height = control.Height,
                    Margin = new Thickness(control.X, control.Y, 0, 0),
                    Tag = control,
                    Background = Brushes.Transparent
                };
                Panel.SetZIndex(container, control.ZIndex);
                Color? ParseColorOrNull(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    try
                    {
                        var c = Core.GetColorBrushFromString(s).Color;
                        return c.A == 0 ? (Color?)null : c;
                    }
                    catch { return null; }
                }

                var solid = ParseColorOrNull(control.Color);
                var upper = ParseColorOrNull(control.TextUpperColor);
                var lower = ParseColorOrNull(control.TextLowerColor);
                var outline = ParseColorOrNull(control.OutlineColor);

                var ogl = new UIEdit.Windows.OutlinedGradientText
                {
                    Text = string.IsNullOrEmpty(control.Text) ? control.Name : control.Text,
                    Width = control.Width,
                    Height = control.Height,
                    FontSize = control.FontSize > 1 ? control.FontSize : 12,
                    FillSolid = solid,
                    OutlineColor = outline,
                    FillUpper = upper,
                    FillLower = lower,
                };
                if (!string.IsNullOrWhiteSpace(control.FontName))
                {
                    try { ogl.FontFamily = new FontFamily(control.FontName); } catch { }
                }
                ogl.Align = control.Align;
                container.Children.Add(ogl);
                if (ShowAllOutlines) container.Children.Add(CreateOutline(container.Width, container.Height));
                dialogCanvas.Children.Add(container);
            }
        }

        /// <summary>
        /// Applies the current Dialog model back to the given XML text, updating positions and attributes.
        /// </summary>
        /// <param name="xmlText"></param>
        /// <returns></returns>
        public string ApplyModelToXml(string xmlText)
        {
            try
            {
                Logger.LogOperationStart("LayoutController.ApplyModelToXml", "Aplicar modelo ao XML");

                if (string.IsNullOrEmpty(xmlText))
                {
                    Logger.LogAction("LayoutController.ApplyModelToXml", "XML de entrada está vazio");
                    return xmlText;
                }

                var cleanedXml = CleanXmlForParsing(xmlText);
                if (string.IsNullOrEmpty(cleanedXml))
                {
                    System.Diagnostics.Debug.WriteLine("XML limpo está vazio, tentando usar XML original");
                    cleanedXml = xmlText;
                }

                var xmlDoc = new XmlDocument();
                bool xmlLoaded = false;

                try
                {
                    xmlDoc.LoadXml(cleanedXml);
                    xmlLoaded = true;
                    System.Diagnostics.Debug.WriteLine("XML limpo carregado com sucesso");
                }
                catch (XmlException xmlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao carregar XML limpo: {xmlEx.Message}");
                }

                if (!xmlLoaded)
                {
                    try
                    {
                        xmlDoc.LoadXml(xmlText);
                        xmlLoaded = true;
                        System.Diagnostics.Debug.WriteLine("XML original carregado com sucesso");
                    }
                    catch (XmlException xmlEx2)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao carregar XML original: {xmlEx2.Message}");
                    }
                }

                if (!xmlLoaded)
                {
                    try
                    {
                        var simpleXml = CreateSimpleValidXml(xmlText);
                        if (!string.IsNullOrEmpty(simpleXml))
                        {
                            xmlDoc.LoadXml(simpleXml);
                            xmlLoaded = true;
                            System.Diagnostics.Debug.WriteLine("XML simplificado carregado com sucesso");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao carregar XML simplificado: {ex.Message}");
                    }
                }

                if (!xmlLoaded)
                {
                    System.Diagnostics.Debug.WriteLine("Falha total ao carregar XML - retornando original");
                    return xmlText;
                }

                if (xmlDoc.DocumentElement == null)
                {
                    return xmlText;
                }

                Func<XmlDocument, string, UIControl, XmlNode> CreateControlNodeFromModel = (doc, tagName, control) =>
                {
                    try
                    {
                        var newNode = doc.CreateElement(tagName);

                        newNode.SetAttribute("Name", control.Name);
                        newNode.SetAttribute("x", control.X.ToString(CultureInfo.InvariantCulture));
                        newNode.SetAttribute("y", control.Y.ToString(CultureInfo.InvariantCulture));
                        newNode.SetAttribute("Width", control.Width.ToString(CultureInfo.InvariantCulture));
                        newNode.SetAttribute("Height", control.Height.ToString(CultureInfo.InvariantCulture));

                        if (control is UIStillImageButton button)
                        {
                            if (!string.IsNullOrEmpty(button.Command))
                            {
                                newNode.SetAttribute("Command", button.Command);
                            }
                            if (!string.IsNullOrEmpty(button.Hint))
                            {
                                newNode.SetAttribute("Hint", button.Hint);
                            }
                            if (!string.IsNullOrEmpty(button.Text))
                            {
                                newNode.SetAttribute("Text", button.Text);
                            }
                            if (!string.IsNullOrEmpty(button.SoundEffect))
                            {
                                newNode.SetAttribute("SoundEffect", button.SoundEffect);
                            }

                            if (!string.IsNullOrEmpty(button.UpImage) || !string.IsNullOrEmpty(button.DownImage) || !string.IsNullOrEmpty(button.HoverImage))
                            {
                                var resourceNode = doc.CreateElement("Resource");

                                if (!string.IsNullOrEmpty(button.UpImage))
                                {
                                    var upImageNode = doc.CreateElement("UpImage");
                                    upImageNode.SetAttribute("FileName", System.IO.Path.GetFileName(button.UpImage));
                                    resourceNode.AppendChild(upImageNode);
                                }

                                if (!string.IsNullOrEmpty(button.DownImage))
                                {
                                    var downImageNode = doc.CreateElement("DownImage");
                                    downImageNode.SetAttribute("FileName", System.IO.Path.GetFileName(button.DownImage));
                                    resourceNode.AppendChild(downImageNode);
                                }

                                if (!string.IsNullOrEmpty(button.HoverImage))
                                {
                                    var hoverImageNode = doc.CreateElement("HoverImage");
                                    hoverImageNode.SetAttribute("FileName", System.IO.Path.GetFileName(button.HoverImage));
                                    resourceNode.AppendChild(hoverImageNode);
                                }

                                newNode.AppendChild(resourceNode);
                            }
                        }

                        return newNode;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("LayoutController.CreateControlNodeFromModel", $"Erro ao criar nó para {control.Name}", ex);
                        return null;
                    }
                };

                Action<string, UIControl[]> updatePositions = (tagName, controls) =>
                {
                    if (controls == null) return;

                    foreach (var control in controls)
                    {
                        if (control == null) continue;

                        var matchingNodes = xmlDoc.DocumentElement.ChildNodes.Cast<XmlNode>()
                            .Where(n => n.Name == tagName && n.Attributes != null && n.Attributes.Cast<XmlAttribute>().Any(a => a.Name == "Name" && a.Value == control.Name))
                            .ToList();

                        if (matchingNodes.Count == 0)
                        {
                            Logger.LogAction("LayoutController.ApplyModelToXml", $"Nó não encontrado para controle: {control.Name} (tipo: {tagName})");
                            continue;
                        }

                        if (matchingNodes.Count > 1)
                        {
                            Logger.LogAction("LayoutController.ApplyModelToXml", $"Encontrados {matchingNodes.Count} nós duplicados para {control.Name}, removendo duplicatas");

                            foreach (var duplicateNode in matchingNodes)
                            {
                                duplicateNode.ParentNode.RemoveChild(duplicateNode);
                            }

                            var newNode = CreateControlNodeFromModel(xmlDoc, tagName, control);
                            if (newNode != null)
                            {
                                xmlDoc.DocumentElement.AppendChild(newNode);
                                Logger.LogAction("LayoutController.ApplyModelToXml", $"Nó único recriado para {control.Name} na posição ({control.X}, {control.Y})");
                            }
                        }
                        else
                        {
                            var node = matchingNodes.First();

                            var xAttr = node.Attributes["x"];
                            if (xAttr == null)
                            {
                                xAttr = xmlDoc.CreateAttribute("x");
                                node.Attributes.Append(xAttr);
                            }
                            xAttr.Value = control.X.ToString(CultureInfo.InvariantCulture);

                            var yAttr = node.Attributes["y"];
                            if (yAttr == null)
                            {
                                yAttr = xmlDoc.CreateAttribute("y");
                                node.Attributes.Append(yAttr);
                            }
                            yAttr.Value = control.Y.ToString(CultureInfo.InvariantCulture);

                            var widthAttr = node.Attributes["Width"];
                            if (widthAttr == null)
                            {
                                widthAttr = xmlDoc.CreateAttribute("Width");
                                node.Attributes.Append(widthAttr);
                            }
                            widthAttr.Value = control.Width.ToString(CultureInfo.InvariantCulture);

                            var heightAttr = node.Attributes["Height"];
                            if (heightAttr == null)
                            {
                                heightAttr = xmlDoc.CreateAttribute("Height");
                                node.Attributes.Append(heightAttr);
                            }
                            heightAttr.Value = control.Height.ToString(CultureInfo.InvariantCulture);

                            Logger.LogAction("LayoutController.ApplyModelToXml", $"Posição atualizada para {control.Name}: ({control.X}, {control.Y})");
                        }
                    }
                };

                if (Dialog == null)
                {
                    System.Diagnostics.Debug.WriteLine("Dialog é null em ApplyModelToXml");
                    return xmlText;
                }

                var dialogElement = xmlDoc.DocumentElement;
                if (dialogElement != null && string.Equals(dialogElement.Name, "DIALOG", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Atualizando atributos da janela: Width={Dialog.Width}, Height={Dialog.Height}");

                    if (dialogElement.HasAttribute("Width"))
                    {
                        dialogElement.SetAttribute("Width", Dialog.Width.ToString("0"));
                    }
                    if (dialogElement.HasAttribute("Height"))
                    {
                        dialogElement.SetAttribute("Height", Dialog.Height.ToString("0"));
                    }
                }

                try
                {
                    if (Dialog.ImagePictures != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.ImagePictures.Count} ImagePictures");
                        updatePositions("IMAGEPICTURE", Dialog.ImagePictures.Cast<UIControl>().ToArray());
                    }
                    if (Dialog.Scrolls != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.Scrolls.Count} Scrolls");
                        updatePositions("SCROLL", Dialog.Scrolls.Cast<UIControl>().ToArray());
                    }
                    if (Dialog.RadioButtons != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.RadioButtons.Count} RadioButtons");
                        updatePositions("RADIO", Dialog.RadioButtons.Cast<UIControl>().ToArray());
                    }
                    if (Dialog.StillImageButtons != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.StillImageButtons.Count} StillImageButtons");
                        updatePositions("STILLIMAGEBUTTON", Dialog.StillImageButtons.Cast<UIControl>().ToArray());
                    }
                    if (Dialog.Labels != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.Labels.Count} Labels");
                        updatePositions("LABEL", Dialog.Labels.Cast<UIControl>().ToArray());
                    }
                    if (Dialog.Edits != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Atualizando {Dialog.Edits.Count} Edits");
                        updatePositions("EDIT", Dialog.Edits.Cast<UIControl>().ToArray());
                    }
                }
                catch (Exception updateEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao atualizar posições: {updateEx.Message}");
                }

                var hadDeclaration = false;
                if (!string.IsNullOrEmpty(xmlText))
                {
                    var trimmed = xmlText.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                    hadDeclaration = trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
                }
                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = !hadDeclaration,
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
                var result = sb.ToString();

                if (string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine("Resultado do ApplyModelToXml é vazio");
                    Logger.LogError("LayoutController.ApplyModelToXml", "Resultado do ApplyModelToXml é vazio");
                    return xmlText;
                }

                Logger.LogOperationEnd("LayoutController.ApplyModelToXml", "Aplicar modelo ao XML", true);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("LayoutController.ApplyModelToXml", "Erro ao aplicar modelo ao XML", ex);

                var errorMessage = $"Erro em ApplyModelToXml: {ex.Message}";
                var stackTrace = $"StackTrace: {ex.StackTrace}";

                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine(stackTrace);

                try
                {
                    File.AppendAllText("apply_model_error.log",
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMessage}\n{stackTrace}\n\n");
                }
                catch
                {
                }

                Logger.LogOperationEnd("LayoutController.ApplyModelToXml", "Aplicar modelo ao XML", false);
                return xmlText;
            }
        }

        /// <summary>
        /// Clear and validate the XML text to ensure it can be parsed correctly.
        /// </summary>
        /// <param name="xmlText"></param>
        /// <returns></returns>
        private string CleanXmlForParsing(string xmlText)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlText))
                {
                    System.Diagnostics.Debug.WriteLine("XML de entrada está vazio");
                    return xmlText;
                }

                var originalPreview = xmlText.Substring(0, Math.Min(200, xmlText.Length));
                System.Diagnostics.Debug.WriteLine($"XML original (primeiros 200 chars): {originalPreview}");

                var bytes = Encoding.UTF8.GetBytes(xmlText);
                var hexPreview = string.Join(" ", bytes.Take(20).Select(b => b.ToString("X2")));
                System.Diagnostics.Debug.WriteLine($"Primeiros 20 bytes (hex): {hexPreview}");

                var cleaned = xmlText.TrimStart('\uFEFF', '\u200B', '\u200E', '\u200F', ' ', '\t', '\r', '\n');

                cleaned = Regex.Replace(cleaned, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

                if (!cleaned.StartsWith("<"))
                {
                    System.Diagnostics.Debug.WriteLine($"XML não começa com '<': {cleaned.Substring(0, Math.Min(50, cleaned.Length))}");
                    cleaned = CleanXmlMoreAggressively(xmlText);
                    if (cleaned == null || !cleaned.StartsWith("<"))
                    {
                        System.Diagnostics.Debug.WriteLine("Falha total na limpeza do XML");
                        return null;
                    }
                }

                if (!cleaned.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + cleaned;
                }

                try
                {
                    var testDoc = new XmlDocument();
                    testDoc.LoadXml(cleaned);
                    System.Diagnostics.Debug.WriteLine("XML limpo é válido");
                }
                catch (XmlException xmlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"XML limpo ainda é inválido: {xmlEx.Message}");
                    var moreCleaned = CleanXmlMoreAggressively(cleaned);
                    if (moreCleaned != null)
                    {
                        try
                        {
                            var testDoc2 = new XmlDocument();
                            testDoc2.LoadXml(moreCleaned);
                            cleaned = moreCleaned;
                            System.Diagnostics.Debug.WriteLine("XML limpo com limpeza agressiva é válido");
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("Falha mesmo com limpeza agressiva");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"XML limpo (primeiros 100 chars): {cleaned.Substring(0, Math.Min(100, cleaned.Length))}");

                return cleaned;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao limpar XML: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// More aggressive cleaning of XML text to remove invalid characters and ensure basic structure.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private string CleanXmlMoreAggressively(string xml)
        {
            try
            {
                if (string.IsNullOrEmpty(xml))
                {
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Iniciando limpeza agressiva do XML (tamanho: {xml.Length})");

                var cleaned = Regex.Replace(xml, @"[^\x20-\x7E\n\r\t]", "");

                cleaned = Regex.Replace(cleaned, @"\r\n|\r|\n", "\n");
                cleaned = Regex.Replace(cleaned, @"\n\s*\n", "\n");

                cleaned = Regex.Replace(cleaned, @"\s+", " ");

                if (!cleaned.Contains("<") || !cleaned.Contains(">"))
                {
                    System.Diagnostics.Debug.WriteLine("XML não contém elementos válidos após limpeza agressiva");
                    return null;
                }

                var firstTagIndex = cleaned.IndexOf('<');
                if (firstTagIndex > 0)
                {
                    cleaned = cleaned.Substring(firstTagIndex);
                }

                System.Diagnostics.Debug.WriteLine($"XML após limpeza agressiva (primeiros 100 chars): {cleaned.Substring(0, Math.Min(100, cleaned.Length))}");

                return cleaned;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na limpeza agressiva: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a very simple valid XML structure as a last resort if parsing fails.
        /// </summary>
        /// <param name="xmlText"></param>
        /// <returns></returns>
        private string CreateSimpleValidXml(string xmlText)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlText))
                {
                    return null;
                }

                System.Diagnostics.Debug.WriteLine("Criando XML simplificado como último recurso");

                var match = Regex.Match(xmlText, @"<([^>]+)>(.*?)</\1>", RegexOptions.Singleline);
                if (match.Success)
                {
                    var tagName = match.Groups[1].Value.Split(' ')[0];
                    var content = match.Groups[2].Value;

                    var simpleXml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<{tagName}>{content}</{tagName}>";
                    System.Diagnostics.Debug.WriteLine($"XML simplificado criado: {simpleXml.Substring(0, Math.Min(100, simpleXml.Length))}");
                    return simpleXml;
                }

                var basicXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root></root>";
                System.Diagnostics.Debug.WriteLine("Criando XML básico como fallback");
                return basicXml;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar XML simplificado: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a dashed outline rectangle for visual debugging of control boundaries.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Rectangle CreateOutline(double width, double height)
        {
            return new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.Yellow,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
        }
        #endregion
    }
}
