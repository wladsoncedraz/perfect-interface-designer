using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

// By: SpinxDev 2025 xD
namespace UIEdit.Windows
{
    public class OutlinedGradientText : FrameworkElement
    {
        #region Injection Properties
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
            nameof(FontFamily), typeof(FontFamily), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillUpperProperty = DependencyProperty.Register(
            nameof(FillUpper), typeof(Color?), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillLowerProperty = DependencyProperty.Register(
            nameof(FillLower), typeof(Color?), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillSolidProperty = DependencyProperty.Register(
            nameof(FillSolid), typeof(Color?), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OutlineColorProperty = DependencyProperty.Register(
            nameof(OutlineColor), typeof(Color?), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
            nameof(Align), typeof(int), typeof(OutlinedGradientText), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public new double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
        public new FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
        public Color? FillUpper { get => (Color?)GetValue(FillUpperProperty); set => SetValue(FillUpperProperty, value); }
        public Color? FillLower { get => (Color?)GetValue(FillLowerProperty); set => SetValue(FillLowerProperty, value); }
        public Color? FillSolid { get => (Color?)GetValue(FillSolidProperty); set => SetValue(FillSolidProperty, value); }
        public Color? OutlineColor { get => (Color?)GetValue(OutlineColorProperty); set => SetValue(OutlineColorProperty, value); }
        // 0=Left, 1=Center, 2=Right
        public int Align { get => (int)GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

        #endregion

        #region Constructor

        #endregion

        #region Methods
        /// <summary>
        /// Renders the outlined gradient text onto the drawing context.
        /// </summary>
        /// <param name="dc"></param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var original = Text ?? string.Empty;
            var segments = ParseColoredSegments(original, out var text);
            if (string.IsNullOrEmpty(text)) return;

            var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double maxW = ActualWidth;
            if (double.IsNaN(maxW) || maxW <= 0) maxW = RenderSize.Width;
            if (double.IsNaN(maxW) || maxW <= 0) maxW = Width;
            if (double.IsNaN(maxW) || maxW <= 0) maxW = FontSize * Math.Max(1, text.Length);
            double maxH = ActualHeight;
            if (double.IsNaN(maxH) || maxH <= 0) maxH = RenderSize.Height;
            if (double.IsNaN(maxH) || maxH <= 0) maxH = Height;
            if (double.IsNaN(maxH) || maxH <= 0) maxH = FontSize * 1.5;

            var ft = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, FontSize, Brushes.White, dpi)
            {
                MaxTextWidth = Math.Max(1, maxW),
                MaxTextHeight = Math.Max(1, maxH),
                Trimming = TextTrimming.None,
                TextAlignment = TextAlignment.Left
            };
            double startX = 0;
            if (Align == 1) startX = Math.Max(0, (maxW - ft.WidthIncludingTrailingWhitespace) / 2.0);
            else if (Align == 2) startX = Math.Max(0, maxW - ft.WidthIncludingTrailingWhitespace);
            var start = new Point(startX, Math.Max(0, (maxH - ft.Height) / 2.0));
            var geo = ft.BuildGeometry(start);

            Brush fill;
            if (FillUpper.HasValue && FillLower.HasValue)
            {
                fill = new LinearGradientBrush(FillUpper.Value, FillLower.Value, new Point(0, 0), new Point(0, 1));
            }
            else if (FillSolid.HasValue)
            {
                fill = new SolidColorBrush(FillSolid.Value);
            }
            else
            {
                fill = Brushes.White;
            }

            if (segments.Count == 0)
            {
                segments.Add(new ColoredSegment { Start = 0, Length = text.Length, Color = null });
            }

            ft.SetForegroundBrush(fill, 0, text.Length);

            foreach (var segment in segments)
            {
                if (segment.Length <= 0 || !segment.Color.HasValue) continue;
                var brush = new SolidColorBrush(segment.Color.Value);
                brush.Freeze();
                ft.SetForegroundBrush(brush, segment.Start, segment.Length);
            }

            dc.DrawText(ft, start);

            if (OutlineColor.HasValue)
            {
                var outlineGeo = ft.BuildGeometry(start);
                var pen = new Pen(new SolidColorBrush(OutlineColor.Value), Math.Max(1.0, FontSize * 0.08));
                pen.Freeze();
                dc.DrawGeometry(null, pen, outlineGeo);
            }
        }

        /// <summary>
        /// Measures the desired size of the control based on the available size.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                            double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }

        private struct ColoredSegment
        {
            public int Start;
            public int Length;
            public Color? Color;
        }

        private static List<ColoredSegment> ParseColoredSegments(string input, out string plainText)
        {
            var segments = new List<ColoredSegment>();
            if (string.IsNullOrEmpty(input))
            {
                plainText = string.Empty;
                return segments;
            }

            var sb = new StringBuilder();
            Color? currentColor = null;
            var segmentStart = 0;
            var hasActiveSegment = false;

            void CloseSegment()
            {
                if (!hasActiveSegment || !currentColor.HasValue) { hasActiveSegment = false; return; }
                var length = sb.Length - segmentStart;
                if (length > 0)
                {
                    segments.Add(new ColoredSegment { Start = segmentStart, Length = length, Color = currentColor });
                }
                hasActiveSegment = false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '^' && i + 6 < input.Length)
                {
                    var candidate = input.Substring(i + 1, 6);
                    if (TryParseHexColor(candidate, out var parsedColor))
                    {
                        CloseSegment();
                        currentColor = parsedColor;
                        i += 6;
                        continue;
                    }
                }

                if (!hasActiveSegment)
                {
                    segmentStart = sb.Length;
                    hasActiveSegment = true;
                }
                sb.Append(input[i]);
            }

            CloseSegment();
            plainText = sb.ToString();
            return segments;
        }

        private static bool TryParseHexColor(string hex, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(hex) || hex.Length != 6) return false;
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;
            var r = (byte)((value >> 16) & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)(value & 0xFF);
            color = Color.FromRgb(r, g, b);
            return true;
        }
        #endregion
    }
}


