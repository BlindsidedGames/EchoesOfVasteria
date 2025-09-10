#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Android
{
    public class AssetMediaContent : IMediaContent
    {
        private NativeMediaContentBase m_nativeMediaContent;

        public AssetMediaContent(NativeMediaContentBase nativeMediaContent)
        {
            m_nativeMediaContent = nativeMediaContent;
        }
        public void AsFilePath(string destinationDirectory, string fileName, EventCallback<string> onComplete)
        {
            m_nativeMediaContent.AsFilePath(destinationDirectory, fileName, new NativeMediaContentPathCallback() {
                onSuccessCallback = (path) => {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, path, null);
                },
                onFailureCallback = (error) => {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, null, error != null ? MediaServicesError.DataNotAvailable(error.GetDescription()) : null);
                }
            });
        }

        public void AsRawMediaData(EventCallback<RawMediaData> onComplete)
        {
            m_nativeMediaContent.AsRawMediaData(new NativeMediaContentRawDataCallback() {
                onSuccessCallback = (data, mime) => {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, new RawMediaData(data.GetBytes(), mime), null);
                },
                onFailureCallback = (error) => {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, null, error != null ? MediaServicesError.DataNotAvailable(error.GetDescription()) : null);
                }
            });
        }

        public void AsTexture2D(EventCallback<Texture2D> onComplete)
        {
            AsRawMediaData((rawData, error) => {
                if(error == null) {
                    Texture2D texture = new TextureData(rawData.Bytes).GetTexture();
                    CallbackDispatcher.InvokeOnMainThread(onComplete, texture, null);

                } else {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, null, error);
                }

            });
        }
    }
}
#endif