using System.Windows;
using System.Windows.Controls;
using SignalRAppChat.Shared.Models.Dto;
using SignalRAppChat.Shared.Models.Entity;

namespace WpfClientChat
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            MessageType? type = null;

            if (item is Message msg)
                type = msg.Type;
            else if (item is MessageDto dto)
                type = dto.Type;

            if (type.HasValue)
            {
                switch (type.Value)
                {
                    case MessageType.Text:
                        return TextTemplate;

                    case MessageType.Image:
                        return ImageTemplate;

                    case MessageType.Video:
                        return VideoTemplate;

                    case MessageType.File:
                        return FileTemplate;

                    default:
                        return FileTemplate;
                }
            }
            return base.SelectTemplate(item, container);
        }
    }
}
