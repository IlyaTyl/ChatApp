using SignalRAppChat.Shared.Models.Entity;
using System.Windows;
using System.Windows.Controls;

namespace WpfClientChat
{
    public class FileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PreviewFile file)
            {
                switch (file.Type)
                {
                    case MessageType.Text:
                        return TextTemplate;

                    case MessageType.Image:
                        return ImageTemplate;

                    case MessageType.Video:
                        return VideoTemplate;

                    default:
                        return FileTemplate;
                }
            }
            return base.SelectTemplate(item, container);
        }
    }

}
