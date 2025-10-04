using System;
using System.Globalization;
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
            var text = Text ?? string.Empty;
            text = System.Text.RegularExpressions.Regex.Replace(text, "\u005E[0-9A-Fa-f]{6}", string.Empty);
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
            dc.DrawGeometry(fill, null, geo);

            if (OutlineColor.HasValue)
            {
                var pen = new Pen(new SolidColorBrush(OutlineColor.Value), Math.Max(1.0, FontSize * 0.08));
                pen.Freeze();
                dc.DrawGeometry(null, pen, geo);
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
        #endregion
    }
}


