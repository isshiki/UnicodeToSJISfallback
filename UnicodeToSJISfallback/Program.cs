using System.IO;
using InsidersCMS;

namespace UnicodeToSJISfallback
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = "㉑😀𩸽";

            var bytesSJIS = CmsUtility.Encoding.ShiftJISwithReplaceFallback.GetBytes(text);

            File.WriteAllBytes(@"C:\sample\test.txt", bytesSJIS);
        }
    }
}
