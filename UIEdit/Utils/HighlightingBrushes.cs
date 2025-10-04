using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

// By: SpinxDev 2025 xD
namespace UIEdit.Utils
{
    public sealed class AvaloniaCompatHighlightingBrush : HighlightingBrush
    {
        #region Injection Properties
        private readonly SolidColorBrush _brush;
        private readonly Color _color;
        #endregion

        #region Constructor
        public AvaloniaCompatHighlightingBrush(Color color)
        {
            _color = color;
            _brush = new SolidColorBrush(color);
            if (!_brush.IsFrozen) _brush.Freeze();
        }

        public AvaloniaCompatHighlightingBrush(SolidColorBrush brush)
        {
            _brush = brush ?? new SolidColorBrush(Colors.Transparent);
            if (!_brush.IsFrozen) _brush.Freeze();
            _color = _brush.Color;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets a brush for the specified context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Brush GetBrush(ITextRunConstructionContext context)
        {
            return _brush;
        }

        /// <summary>
        /// Gets a color for the specified context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Color? GetColor(ITextRunConstructionContext context)
        {
            return _color;
        }
        #endregion
    }
}


