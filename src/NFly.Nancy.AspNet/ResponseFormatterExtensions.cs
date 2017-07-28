using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NFly.Nancy.AspNet
{
    public static class ResponseFormatterExtensions
    {
        public static Response AsAsyncStream(this IResponseFormatter response,
            Stream inputStream, string contentType, long? offset = null, long? length = null)
        {
            return new AsyncStreamResponse(token => Task.FromResult(inputStream),
                contentType, offset, length);
        }


        public static Response AsAsyncStream(this IResponseFormatter response,
            Func<Task<Stream>> streamFunc, string contentType, long? offset = null, long? length = null)
        {
            return new AsyncStreamResponse(token => streamFunc(), contentType, offset, length);
        }


        public static Response AsAsyncStream(this IResponseFormatter response,
            Func<CancellationToken, Task<Stream>> streamFunc, string contentType, long? offset = null, long? length = null)
        {
            return new AsyncStreamResponse(streamFunc, contentType, offset, length);
        }

    }
}
