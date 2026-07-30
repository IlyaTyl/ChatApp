using SignalRAppChat.Shared.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WpfClientChat.Helper;

namespace WpfClientChat
{
    public class PreviewFile
    {
        public string Path { get; set; } = null!;
        public string? Caption { get; set; }
        public MessageType Type { get; set; }
        public BitmapSource? PreviewImage =>
            Type == MessageType.Video ? VideoPreviewHelper.GetThumbnail(Path) : null;
        public string FileName => System.IO.Path.GetFileName(Path);
    }

}
