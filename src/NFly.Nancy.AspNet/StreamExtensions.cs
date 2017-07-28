using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NFly.Nancy.AspNet
{
    public static class StreamExtensions
    {
        public static Task CopyRangeAsync(this Stream source, Stream destination,
            long offset, long length,
            int bufferSize)
        {
            return CopyRangeAsync(source, destination, offset, length, bufferSize, CancellationToken.None);
        }

        public static async Task CopyRangeAsync(this Stream source, Stream destination,
            long offset, long length,
            int bufferSize, CancellationToken token)
        {
            if (offset > 0)
            {
                source.Seek(offset, SeekOrigin.Current);
            }

            long remain = length;
            int i = 0;
            var buffers = new[] { new byte[bufferSize], new byte[bufferSize] };
            Task writeTask = null;
            while (remain > 0)
            {
                token.ThrowIfCancellationRequested();
                int readCount = remain < bufferSize ? (int)remain : bufferSize;
                var readTask = source.ReadAsync(buffers[i], 0, readCount, token);
                if (writeTask != null)
                {
                    await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
                }
                int bytesRead;
                if (readTask.IsCompleted)
                {
                    bytesRead = readTask.Result;
                }
                else
                {
                    bytesRead = await readTask.ConfigureAwait(false);
                }
                if (bytesRead == 0)
                {
                    break;
                }
                if (!destination.CanWrite) //reduce write errors if destination closed
                {
                    break;
                }
                writeTask = destination.WriteAsync(buffers[i], 0, bytesRead, token);
                i ^= 1; // swap buffers
                remain -= bytesRead;
            }
        }
    }
}
