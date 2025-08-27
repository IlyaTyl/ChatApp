using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WpfClientChat.Converter
{
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath)
            {
                var ext = Path.GetExtension(filePath)?.ToLower();

                switch (ext)
                {
                    case ".pdf":
                        return new BitmapImage(new Uri("/Images/pdf.png", UriKind.Relative));

                    case ".doc":
                    case ".docx":
                        return new BitmapImage(new Uri("/Images/doc.png", UriKind.Relative));

                    case ".xls":
                    case ".xlsx":
                        return new BitmapImage(new Uri("/Images/xls.png", UriKind.Relative));

                    default:
                        return new BitmapImage(new Uri("/Images/file.png", UriKind.Relative));
                }
            }
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
