using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

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

namespace Thumbnailer
{
    public class BrowserImage
    {
        public static Bitmap GetWebSiteThumbnail(string Url, int BrowserWidth, int BrowserHeight, int ThumbnailWidth,
                                                 int ThumbnailHeight)
        {
            var thumbnailGenerator = new WebsiteThumbnailImage(Url, BrowserWidth, BrowserHeight, ThumbnailWidth,
                                                               ThumbnailHeight);
            return thumbnailGenerator.GenerateWebSiteThumbnailImage();
        }
    }


    internal class WebsiteThumbnailImage
    {
        private readonly TimeSpan _maxTime = TimeSpan.FromSeconds(20);
        private DateTime _beginTime;
        private Bitmap _mBitmap;
        private int _mBrowserHeight;
        private int _mBrowserWidth;
        private string _mUrl;

        public WebsiteThumbnailImage(string Url, int BrowserWidth, int BrowserHeight, int ThumbnailWidth,
                                     int ThumbnailHeight)
        {
            isDone = false;
            _mUrl = Url;
            _mBrowserWidth = BrowserWidth;
            _mBrowserHeight = BrowserHeight;
            this.ThumbnailHeight = ThumbnailHeight;
            this.ThumbnailWidth = ThumbnailWidth;
        }

        private bool isDone { get; set; }


        public string Url
        {
            get { return _mUrl; }

            set { _mUrl = value; }
        }


        public Bitmap ThumbnailImage
        {
            get { return _mBitmap; }
        }


        public int ThumbnailWidth { get; set; }


        public int ThumbnailHeight { get; set; }


        public int BrowserWidth
        {
            get { return _mBrowserWidth; }

            set { _mBrowserWidth = value; }
        }


        public int BrowserHeight
        {
            get { return _mBrowserHeight; }

            set { _mBrowserHeight = value; }
        }


        public Bitmap GenerateWebSiteThumbnailImage()
        {
            //Async
            var mThread = new Thread(_GenerateWebSiteThumbnailImage);
            mThread.SetApartmentState(ApartmentState.STA);
            mThread.Start();
            mThread.Join();
            return _mBitmap;
        }


        private void webBrowser1_NewWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void _GenerateWebSiteThumbnailImage()
        {
            try
            {
                _beginTime = DateTime.Now;
                var mWebBrowser = new WebBrowser();
                mWebBrowser.ScrollBarsEnabled = false;
                mWebBrowser.Navigate(_mUrl);
                mWebBrowser.ScriptErrorsSuppressed = true;
                mWebBrowser.Height = BrowserHeight;
                mWebBrowser.Width = BrowserWidth;
                mWebBrowser.NewWindow += webBrowser1_NewWindow;
                mWebBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                while (mWebBrowser.ReadyState != WebBrowserReadyState.Complete && isDone == false) //
                {
                    TimeSpan ts = DateTime.Now - _beginTime;
                    if (ts > _maxTime) // did our request time out?
                    {
                        isDone = true;
                        DocumentCompletedFully(mWebBrowser, null);
                    }
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
                mWebBrowser.Dispose();
            }
            catch (Exception x)
            {
            }
        }


        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (((WebBrowser) sender).Document.Url.Equals(e.Url) && isDone == false)
            {
                DocumentCompletedFully(sender, e);
                isDone = true;
            }
        }

        private void DocumentCompletedFully(object sender, WebBrowserDocumentCompletedEventArgs args)
        {
            var webBrowser = (WebBrowser) sender;

            var bitmap = new Bitmap(_mBrowserWidth, _mBrowserHeight);
            var bitmapRect = new Rectangle(0, 0, _mBrowserWidth, _mBrowserHeight);
            webBrowser.DrawToBitmap(bitmap, bitmapRect);
            _mBitmap = bitmap;
        }
    }
}