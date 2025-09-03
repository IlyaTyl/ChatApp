using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WpfClientChat.Service
{
    public class FileCacheService
    {
        private readonly string cacheFolder;

        public FileCacheService()
        {
            cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyChatApp", "Cache");

            if(!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
        }

        public string GetCachedFilePath(string fileName)
        {
            return Path.Combine(cacheFolder, fileName);
        }

        public async Task SaveToCacheAsync(string fileName, byte[] data)
        {
            var path = GetCachedFilePath(fileName);
            await File.WriteAllBytesAsync(path, data);
        }

        public bool TryGetCachedFile(string fileName, out string filePath)
        {
            filePath = GetCachedFilePath(fileName);
            return File.Exists(filePath);
        }

        public async Task<string> GetOrDownloadFileAsync(string fileName,string fileUrl)
        {
            string path = GetCachedFilePath(fileName);

            if(File.Exists(path))
                return path;

            using (var client = new HttpClient())
            {
                var data = await client.GetByteArrayAsync(fileUrl);
                await SaveToCacheAsync(fileName, data);
            }

            return path;
        }
    }
}
