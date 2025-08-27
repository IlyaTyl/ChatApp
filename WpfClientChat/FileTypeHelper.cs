using SignalRAppChat.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfClientChat
{
    public static class FileTypeHelper
    {
        private static readonly string[] ImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private static readonly string[] VideoExt = { ".mp4", ".avi", ".mov", ".mkv", ".wmv" };
        private static readonly string[] DocExt = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };

        public static MessageType GetMessageTypeByExtension(string ext)
        {
            ext = ext.ToLower();

            if(ImageExt.Contains(ext)) return MessageType.Image;
            if(VideoExt.Contains(ext)) return MessageType.Video;
            if(DocExt.Contains(ext)) return MessageType.File;

            return MessageType.File;
        }
    }
}
