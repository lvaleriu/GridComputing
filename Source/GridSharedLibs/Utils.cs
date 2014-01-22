using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GridSharedLibs
{
    public static class Utils
    {
        public static string GetMd5HashFromFile(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);

                var sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}