using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using UIEdit.Models;

// By: SpinxDev 2025 xD
namespace UIEdit.Windows
{
    public partial class AttributesWindow : Window
    {
        #region Injection Properties
        public class AttributeEntry
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public Func<string> Getter { get; set; }
            public Action<string> Setter { get; set; }
            public bool IsImageProperty { get; set; }
        }
        private readonly UIControl _model;
        private readonly List<AttributeEntry> _entries;
        public string SurfacesPath { get; set; }
        #endregion

        #region Constructor
        public AttributesWindow(UIControl model)
        {
            InitializeComponent();
            _model = model;
            _entries = BuildEntries(model);
            GridAttrs.ItemsSource = _entries;

            DataContext = this;
        }
        #endregion

        /// <summary>
        /// Builds a list of attribute entries for the given UIControl model, including standard properties and any additional properties.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static List<AttributeEntry> BuildEntries(UIControl m)
        {
            var list = new List<AttributeEntry>();
            list.Add(new AttributeEntry { Name = "Name", Getter = () => m.Name, Setter = v => m.Name = v, Value = m.Name, IsImageProperty = false });
            list.Add(new AttributeEntry { Name = "X", Getter = () => m.X.ToString(CultureInfo.InvariantCulture), Setter = v => m.X = ToDouble(v, m.X), Value = m.X.ToString(CultureInfo.InvariantCulture), IsImageProperty = false });
            list.Add(new AttributeEntry { Name = "Y", Getter = () => m.Y.ToString(CultureInfo.InvariantCulture), Setter = v => m.Y = ToDouble(v, m.Y), Value = m.Y.ToString(CultureInfo.InvariantCulture), IsImageProperty = false });
            list.Add(new AttributeEntry { Name = "Width", Getter = () => m.Width.ToString(CultureInfo.InvariantCulture), Setter = v => m.Width = ToDouble(v, m.Width), Value = m.Width.ToString(CultureInfo.InvariantCulture), IsImageProperty = false });
            list.Add(new AttributeEntry { Name = "Height", Getter = () => m.Height.ToString(CultureInfo.InvariantCulture), Setter = v => m.Height = ToDouble(v, m.Height), Value = m.Height.ToString(CultureInfo.InvariantCulture), IsImageProperty = false });

            var properties = m.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "Name" || prop.Name == "X" || prop.Name == "Y" || prop.Name == "Width" || prop.Name == "Height") continue;

                if (prop.PropertyType == typeof(string))
                {
                    var value = (string)prop.GetValue(m, null) ?? string.Empty;
                    var isImageProp = IsImageProperty(prop.Name);
                    list.Add(new AttributeEntry
                    {
                        Name = prop.Name,
                        Getter = () => (string)prop.GetValue(m, null) ?? string.Empty,
                        Setter = v => prop.SetValue(m, v, null),
                        Value = value,
                        IsImageProperty = isImageProp
                    });
                }
                else if (prop.PropertyType == typeof(double))
                {
                    var value = (double)prop.GetValue(m, null);
                    list.Add(new AttributeEntry
                    {
                        Name = prop.Name,
                        Getter = () => ((double)prop.GetValue(m, null)).ToString(CultureInfo.InvariantCulture),
                        Setter = v => prop.SetValue(m, ToDouble(v, (double)prop.GetValue(m, null)), null),
                        Value = value.ToString(CultureInfo.InvariantCulture),
                        IsImageProperty = false
                    });
                }
                else if (prop.PropertyType == typeof(int))
                {
                    var value = (int)prop.GetValue(m, null);
                    list.Add(new AttributeEntry
                    {
                        Name = prop.Name,
                        Getter = () => ((int)prop.GetValue(m, null)).ToString(),
                        Setter = v => prop.SetValue(m, int.TryParse(v, out var i) ? i : (int)prop.GetValue(m, null), null),
                        Value = value.ToString(),
                        IsImageProperty = false
                    });
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    var value = (bool)prop.GetValue(m, null);
                    list.Add(new AttributeEntry
                    {
                        Name = prop.Name,
                        Getter = () => ((bool)prop.GetValue(m, null)).ToString(),
                        Setter = v => prop.SetValue(m, bool.TryParse(v, out var b) ? b : (bool)prop.GetValue(m, null), null),
                        Value = value.ToString(),
                        IsImageProperty = false
                    });
                }
            }

            return list.OrderBy(x => x.IsImageProperty ? 1 : 0).ThenBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Converts a string to a double, using invariant culture first, then current culture, and falling back to a default value if parsing fails.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private static double ToDouble(string v, double fallback)
        {
            if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            if (double.TryParse(v, out d)) return d;
            return fallback;
        }

        /// <summary>
        /// Determines if a property name corresponds to an image-related property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static bool IsImageProperty(string propertyName)
        {
            var imageProperties = new[] {
                "UpImage", "DownImage", "HoverImage", "FrameImage", "ScrollImage",
                "BarImage", "HilightImage", "BarFrameImage", "NormalImage",
                "CheckedImage", "FillImage", "FileName", "GfxFileName"
            };
            return imageProperties.Contains(propertyName);
        }

        /// <summary>
        /// Handles the OK button click event, applying changes to the model and closing the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOk(object sender, RoutedEventArgs e)
        {
            foreach (var entry in _entries)
            {
                entry.Setter?.Invoke(entry.Value);
            }
            DialogResult = true;
            Close();
        }
    }
}

