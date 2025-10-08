using System.Runtime.InteropServices;

namespace DocuSign.Requests
{
    internal class DsHelper
    {
        internal static string PrepareFullPrivateKeyFilePath(string fileName)
        {
            const string defaultRsaPrivateKeyFileName = "private.key";

            var fileNameOnly = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(fileNameOnly))
            {
                fileNameOnly = defaultRsaPrivateKeyFileName;
            }

            var filePath = Path.GetDirectoryName(fileName);
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Directory.GetCurrentDirectory();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Directory.GetCurrentDirectory().Contains("bin"))
            {
                fileNameOnly = defaultRsaPrivateKeyFileName;
                filePath = Path.GetFullPath(filePath);
            }

            return Path.Combine(filePath, fileNameOnly);
        }

        internal static byte[] ReadFileContent(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
