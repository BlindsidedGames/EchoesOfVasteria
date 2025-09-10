using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class MediaContent : IMediaContent
    {
        protected byte[] Data { get;  set; }
        protected string Mime { get;  set; }

        public virtual void AsTexture2D(EventCallback<Texture2D> onComplete)
        {
            var texture = new TextureData(Data).GetTexture();
            CallbackDispatcher.InvokeOnMainThread(onComplete, texture, null);
        }

        public void AsRawMediaData(EventCallback<RawMediaData> onComplete)
        {
            CallbackDispatcher.InvokeOnMainThread(onComplete, new RawMediaData(Data, Mime), null);
        }

        public virtual void AsFilePath(string destinationDirectory, string fileName, EventCallback<string> onComplete)
        {
            string extension = MimeType.GetExtensionForType(Mime);
            string destinationDirectoryPath = Path.Combine(Application.persistentDataPath, destinationDirectory);
            string path = Path.Combine(destinationDirectoryPath, $"{fileName}.{extension}");
            IOServices.CreateDirectory(destinationDirectoryPath);
            IOServices.CreateFile(path, Data);
            CallbackDispatcher.InvokeOnMainThread(onComplete, path, null);
        }

        public static MediaContent From(byte[] data, string mime)
        {
            return new MediaContent() {
                Data = data,
                Mime = mime
            };
        }

        public static MediaContent From(Texture2D texture)
        {
            var data = texture.EncodeToPNG();

            return new MediaContent() {
                Data = data,
                Mime = MimeType.kPNGImage
            };
        }

    }
}