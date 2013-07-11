using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Thumbnailer;

namespace MvcApplication1.Controllers
{
    public class HomeController : AsyncController
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ViewResult> GetImage()
        {
            ViewBag.SyncOrAsync = "Asynchronous";

            string url = Request.QueryString.Get("url");
            string h = Request.QueryString.Get("h");
            string w = Request.QueryString.Get("w");
            byte[] buffer;
            
                GetWebSite scale;

                //Url points to an image
                if (url.Contains("jpg") || url.Contains("png") || url.Contains("gif") || url.Contains(".ico") || url.Contains(".bmp") || url.Contains(".tiff") || url.Contains(".tif") || url.Contains(".emf"))
                {
                    scale = await GetWebSite.GetWebImageAsync(new Uri(url)); // convert all images to png
                }
                else // url points to a website
                {
                    scale = new GetWebSite(new Uri(url), int.Parse(w), int.Parse(h));
                }
            
                scale.Height = int.Parse(h);
                scale.Width = int.Parse(w);
                Response.Clear();
                buffer = scale.Save(ImageFormat.Png).ToArray(); //s.ToArray());
               
            Response.OutputStream.Write(buffer, 0, buffer.Length);
            Response.ContentType = "image/png";
            Response.Flush();
             

            return View();
        }

    }
}
