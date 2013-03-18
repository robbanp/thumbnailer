using System;
using System.Globalization;
using System.IO;
using System.Web.Caching;
using System.Web.UI;
using Thumbnailer;

/*
 Copyright (c) 2011 Robert Pohl, robert@sugarcubesolutions.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
namespace WebApplication1
{
    public partial class getimage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string url = Request.QueryString.Get("url");
            string h = Request.QueryString.Get("h");
            string w = Request.QueryString.Get("w");
            string key = url + h + w;
            const bool useCache = true; // use cache?
            byte[] buffer;
            if (Cache[key] == null && useCache)
            {
                ImageScale scale;

                //Url points to an image
                if (url.Contains("jpg") || url.Contains("png") || url.Contains("gif") || url.Contains(".ico") || url.Contains(".bmp") || url.Contains(".tiff") || url.Contains(".tif") || url.Contains(".emf"))
                {
                    scale = new ImageScale(new Uri(url), ".png"); // convert all images to png
                }
                else // url points to a website
                {
                    scale = new ImageScale(new Uri(url), int.Parse(w), int.Parse(h));
                }
                var s = new MemoryStream();
                scale.Height = int.Parse(h);
                scale.Width = int.Parse(w);
                scale.SaveFile(s, 100);
                Response.Clear();
                buffer = s.ToArray();
                Cache.Insert(key, buffer, null, DateTime.Now.AddHours(24), Cache.NoSlidingExpiration); // add image to cache for 24 hrs
            }
            else
            {
                buffer = (byte[]) Cache.Get(key);
            }

            Response.OutputStream.Write(buffer, 0, buffer.Length);
            Response.ContentType = "image/png";
            Response.Flush();
        }
    }
}