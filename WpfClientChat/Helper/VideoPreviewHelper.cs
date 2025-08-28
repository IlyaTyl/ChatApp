using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;

namespace WpfClientChat.Helper
{
    public static class VideoPreviewHelper
    {
        //Выводит первый кадр видео, как превью 
        public static BitmapSource GetThumbnail(string videoPath)
        {
            using (var shellFile = ShellFile.FromFilePath(videoPath))
            {
                var thumb = shellFile.Thumbnail.ExtraLargeBitmap;
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    thumb.GetHbitmap(),
                    nint.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(250, 150));
            }
        }
    }
}
