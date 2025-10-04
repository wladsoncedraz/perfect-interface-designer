using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using UIEdit.Utils;

// By: SpinxDev 2025 xD
namespace UIEdit.Controllers
{
    public class AvalonEditorSearchController
    {
        #region Injection Properties
        public string SearchString { get; set; }
        public List<int> FoundPositions { get; private set; }
        public int LastFoundPosition { get; private set; }
        private readonly TextEditor _textEditor;
        #endregion

        #region Constructor
        public AvalonEditorSearchController(TextEditor textEditor)
        {
            _textEditor = textEditor;
            FoundPositions = new List<int>();
            LastFoundPosition = -1;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Resets the search state, clearing found positions, last found position, and search string.
        /// </summary>
        public void Reset()
        {
            FoundPositions.Clear();
            LastFoundPosition = -1;
            SearchString = "";
            Core.ClearMemory();
        }

        /// <summary>
        /// Searches for the next occurrence of the specified search fragment in the editor.
        /// Highlights and scrolls to the found text. If the search fragment changes, resets the search state.
        /// Returns the position of the found occurrence, or -1 if not found.
        /// </summary>
        public int NextSearch(string searchFragment)
        {
            if (_textEditor == null || string.IsNullOrEmpty(_textEditor.Text)) return -1;
            Core.ClearMemory();

            var nIndex = -1;
            if (searchFragment != SearchString)
            {
                FoundPositions.Clear();
                var pos = -1;
                while (true)
                {
                    if (_textEditor.Text.Length <= pos) break;
                    pos = _textEditor.Text.IndexOf(searchFragment, pos + 1, StringComparison.OrdinalIgnoreCase);
                    if (pos == -1) break;
                    FoundPositions.Add(pos);
                }

                if (FoundPositions.Count > 0)
                {
                    nIndex = FoundPositions[0];
                    LastFoundPosition = nIndex;
                }
                SearchString = searchFragment;
            }
            else
            {
                if (FoundPositions.OrderBy(t => t).Any(t => t > LastFoundPosition))
                {
                    nIndex = FoundPositions.OrderBy(t => t).First(t => t > LastFoundPosition);
                    LastFoundPosition = nIndex;
                }
            }

            if (nIndex == -1) return nIndex;

            var nLineNumber = _textEditor.Document.GetLineByOffset(nIndex).LineNumber;
            var nColNumber = _textEditor.Document.GetLocation(nIndex).Column;
            _textEditor.Select(nIndex, searchFragment.Length);
            _textEditor.ScrollTo(nLineNumber, nColNumber);

            return LastFoundPosition;
        }

        /// <summary>
        /// Searches for the previous occurrence of the specified search fragment in the editor.
        /// </summary>
        /// <param name="searchFragment"></param>
        /// <returns></returns>
        public int PrevSearch(string searchFragment)
        {
            if (_textEditor == null || string.IsNullOrEmpty(_textEditor.Text)) return -1;
            Core.ClearMemory();

            var nIndex = -1;
            if (FoundPositions.OrderBy(t => t).Any(t => t < LastFoundPosition))
            {
                nIndex = FoundPositions.OrderBy(t => t).Last(t => t < LastFoundPosition);
                LastFoundPosition = nIndex;
            }

            if (nIndex == -1) return nIndex;

            var nLineNumber = _textEditor.Document.GetLineByOffset(nIndex).LineNumber;
            var nColNumber = _textEditor.Document.GetLocation(nIndex).Column;
            _textEditor.Select(nIndex, searchFragment.Length);
            _textEditor.ScrollTo(nLineNumber, nColNumber);

            return LastFoundPosition;
        }
        #endregion
    }
}