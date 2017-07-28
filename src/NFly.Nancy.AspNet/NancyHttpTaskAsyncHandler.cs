using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.Hosting.Aspnet;
using Nancy.IO;

namespace NFly.Nancy.AspNet
{
    public class NancyHttpTaskAsyncHandler : HttpTaskAsyncHandler
    {
        private static readonly INancyEngine _engine;

        static NancyHttpTaskAsyncHandler()
        {
            var bootstrapper = NancyBootstrapperLocator.Bootstrapper;
            bootstrapper.Initialise();
            _engine = bootstrapper.GetEngine();
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            var token = context.Request.TimedOutToken;
            var request = CreateNancyRequest(context);
            using (var nancyContext = await _engine.HandleRequest(request, null, token))
            {
                var httpResponse = context.Response;
                using (var response = nancyContext.Response)
                {
                    //check if client disconnected
                    if (!httpResponse.IsClientConnected)
                    {
                        context.ApplicationInstance.CompleteRequest();
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    SetHttpResponseHeaders(context, response);


                    if (response.ReasonPhrase != null)
                    {
                        httpResponse.Status = response.ReasonPhrase;
                    }

                    string contentType = response.ContentType;
                    if (contentType != null)
                    {
                        httpResponse.ContentType = contentType;
                    }

                    httpResponse.StatusCode = (int)response.StatusCode;

                    var asyncResponse = response as AsyncStreamResponse;
                    if (asyncResponse != null)
                    {
                        httpResponse.Buffer = false;
                        httpResponse.BufferOutput = false;

                        await asyncResponse.WriteContentsAsync.Invoke(nancyContext, httpResponse.OutputStream,
                            context.ThreadAbortOnTimeout ? token : CancellationToken.None);
                    }
                    else
                    {
                        if (IsOutputBufferDisabled())
                        {
                            httpResponse.BufferOutput = false;
                        }

                        response.Contents.Invoke(httpResponse.OutputStream);
                    }
                }
            }
        }


        private static long GetExpectedRequestLength(IDictionary<string, IEnumerable<string>> incomingHeaders)
        {
            if (incomingHeaders == null)
            {
                return 0;
            }

            if (!incomingHeaders.ContainsKey("Content-Length"))
            {
                return 0;
            }

            var headerValue =
                incomingHeaders["Content-Length"].SingleOrDefault();

            if (headerValue == null)
            {
                return 0;
            }

            long contentLength;
            if (!long.TryParse(headerValue, NumberStyles.Any, CultureInfo.InvariantCulture, out contentLength))
            {
                return 0;
            }

            return contentLength;
        }

        private static void SetHttpResponseHeaders(HttpContext context, Response response)
        {
            foreach (var header in response.Headers.ToDictionary(x => x.Key, x => x.Value))
            {
                context.Response.AddHeader(header.Key, header.Value);
            }

            foreach (var cookie in response.Cookies.ToArray())
            {
                context.Response.AddHeader("Set-Cookie", cookie.ToString());
            }
        }

        private static bool IsOutputBufferDisabled()
        {
            var configurationSection =
                ConfigurationManager.GetSection("nancyFx") as NancyFxSection;

            if (configurationSection == null || configurationSection.DisableOutputBuffer == null)
            {
                return false;
            }

            return configurationSection.DisableOutputBuffer.Value;
        }

        private static Request CreateNancyRequest(HttpContext context)
        {
            var incomingHeaders = context.Request.Headers.ToDictionary();

            var expectedRequestLength =
                GetExpectedRequestLength(incomingHeaders);

            var basePath = context.Request.ApplicationPath.TrimEnd('/');

            var path = context.Request.Url.AbsolutePath.Substring(basePath.Length);
            path = string.IsNullOrWhiteSpace(path) ? "/" : path;

            var nancyUrl = new Url
            {
                Scheme = context.Request.Url.Scheme,
                HostName = context.Request.Url.Host,
                Port = context.Request.Url.Port,
                BasePath = basePath,
                Path = path,
                Query = context.Request.Url.Query,
            };
            byte[] certificate = null;

            if (context.Request.ClientCertificate != null &&
                context.Request.ClientCertificate.IsPresent &&
                context.Request.ClientCertificate.Certificate.Length != 0)
            {
                certificate = context.Request.ClientCertificate.Certificate;
            }

            RequestStream body = null;

            if (expectedRequestLength != 0)
            {
                body = RequestStream.FromStream(context.Request.InputStream, expectedRequestLength, StaticConfiguration.DisableRequestStreamSwitching ?? true);
            }

            var protocolVersion = context.Request.ServerVariables["HTTP_VERSION"];

            return new Request(context.Request.HttpMethod.ToUpperInvariant(),
                nancyUrl,
                body,
                incomingHeaders,
                context.Request.UserHostAddress,
                certificate,
                protocolVersion);
        }

    }
}
