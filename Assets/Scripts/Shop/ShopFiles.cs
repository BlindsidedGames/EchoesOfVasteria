using System.IO;
using System.Text;
namespace TimelessEchoes.Shop
{
    internal static class ShopFiles
    {
        public static string Read(string path, int limit) => !File.Exists(path) || new FileInfo(path).Length > limit ? null : File.ReadAllText(path, Encoding.UTF8);
        public static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(text); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
    }
}
