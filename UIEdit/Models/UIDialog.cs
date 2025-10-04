using System.Collections.Generic;

namespace UIEdit.Models
{
    public class UIDialog
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public UIResourceFrameImage FrameImage { get; set; }
        public List<UIImagePicture> ImagePictures { get; set; }
        public List<UIScroll> Scrolls { get; set; }
        public List<UIEditBox> Edits { get; set; }
        public List<UIStillImageButton> StillImageButtons { get; set; }
        public List<UIRadioButton> RadioButtons { get; set; }
        public List<UILabel> Labels { get; set; }
        public List<UIList> Lists { get; set; }
        public List<UIProgress> Progresses { get; set; }

        public UIDialog()
        {
            ImagePictures = new List<UIImagePicture>();
            Scrolls = new List<UIScroll>();
            Edits = new List<UIEditBox>();
            StillImageButtons = new List<UIStillImageButton>();
            RadioButtons = new List<UIRadioButton>();
            Labels = new List<UILabel>();
            Lists = new List<UIList>();
            Progresses = new List<UIProgress>();
        }
    }
}
