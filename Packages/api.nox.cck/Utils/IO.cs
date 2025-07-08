using System.IO;
using Cysharp.Threading.Tasks;

namespace Nox.CCK.Utils
{
    public class IO
    {
        public static async UniTask<byte[]> ReadFileAsync(string path)
        {
            await using FileStream sourceStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                useAsync: true);
            var buffer = new byte[sourceStream.Length];
            _ = await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
            return buffer;
        }
    }
}