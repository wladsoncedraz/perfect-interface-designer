namespace UIEdit.Models
{
    public class UIResource
    {
        public string FileName { get; set; }
        public UIResourceType Type { get; set; }
    }
    public class UIResourceClock : UIResource
    {
    }
}
