using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace NFly.Nancy.AspNet.Demo
{
    public class HomeModule:NancyModule
    {
        public HomeModule()
        {
            this.Get["/"] = _ =>
            {
                return "Hello NancyFx";
            };


            this.Get["/download", true] = async (ctx, tk) =>
            {


                var fullPath = HostingEnvironment.MapPath("~/App_Data/eclipse_theme_monokai.epf");

                var httpContext = HttpContext.Current;
                httpContext.ThreadAbortOnTimeout = false;
                httpContext.Response.Buffer = false;
                httpContext.Response.BufferOutput = false;
                httpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                httpContext.Response.Cache.SetMaxAge(TimeSpan.Zero);
                httpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

                await Task.Delay(100);

                string fileName = Path.GetFileName(fullPath);

                return this.Response.AsAsyncStream(() =>
                {
                    Stream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    return Task.FromResult(fs);
                }, "application/octet-stream")
                    .WithHeader("Content-Disposition", "attachment;filename=" + fileName);
            };

        }



    }
}