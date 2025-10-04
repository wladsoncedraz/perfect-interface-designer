namespace UIEdit.Models
{
    public class UILabel : UIControl
    {
        public string Text { get; set; }
        public string Color { get; set; }
        public string OutlineColor { get; set; }
        public string TextUpperColor { get; set; }
        public string TextLowerColor { get; set; }
        // 0=Left, 1=Center, 2=Right
        public int Align { get; set; }
    }
}