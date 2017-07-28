using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Nancy;

namespace NFly.Nancy.AspNet
{
    public class AsyncStreamResponse : Response
    {
        const int BufferSize = 16 * 1024;
        private Stream _source;

        public AsyncStreamResponse(Func<CancellationToken, Task<Stream>> sourceFunc, string contentType,
            long? offset, long? length)
        {
            this.ContentType = contentType;
            this.StatusCode = HttpStatusCode.OK;

            this.WriteContentsAsync = async (ctx, outputStream, token) =>
            {
                token.ThrowIfCancellationRequested();

                using (this._source = await sourceFunc(token))
                {
                    token.ThrowIfCancellationRequested();

                    if (offset != null && offset.Value > 0)
                    {
                        this._source.Seek(offset.Value, SeekOrigin.Current);
                    }

                    if (length != null)
                    {
                        await this._source.CopyRangeAsync(outputStream, 0, length.Value, BufferSize, token);
                    }
                    else
                    {
                        await this._source.CopyToAsync(outputStream, BufferSize, token);
                    }
                }
            };
        }

        public Func<NancyContext, Stream, CancellationToken, Task> WriteContentsAsync { get; private set; }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (this._source != null)
            {
                this._source.Dispose();
            }
        }

    }
}
