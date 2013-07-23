using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

/*
 Copyright (c) 2013 Robert Pohl, robert@sugarcubesolutions.com

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
    /// <summary>
    ///     Allowed formats
    /// </summary>
    public enum ImageFormat
    {
        Jpeg,
        Gif,
        Png,
        Bmp,
        Tiff,
        Emf
    }

    /// <summary>
    ///     Handles image manipulation
    /// </summary>
    public class ImageScale
    {
        # region Properties

        public bool ConstrainPorportions = true;
        private int _height = -1;
        internal Bitmap _mySource;
        private int _width = -1;
        public string LastError { get; set; }
        public MemoryStream BitmapStream { get; set; }
        private InterpolationMode _interpolation { get; set; }
        private short _bitmapQualityValue { get; set; }

        private Bitmap _targetBitmap { get; set; }

        /// <summary>
        ///     Image width in pixels.
        ///     By default ConstrainPorportions is true and this will also affect the height.
        /// </summary>
        public int Width
        {
            get { return _width; }
            set
            {
                int v = 1;
                if (value > 0)
                    v = value;

                if (ConstrainPorportions)
                {
                    _height = Convert.ToInt32(_height*(Convert.ToDouble(v)/Convert.ToDouble(_width)));
                }
                _width = v;
                CropX = v;
                CropY = _height;
            }
        }

        /// <summary>
        ///     Image height in pixels.
        ///     By default ConstrainPorportions is true and this will also affect the width.
        /// </summary>
        public int Height
        {
            get { return _height; }
            set
            {
                int v = 1;
                if (value > 0)
                    v = value;

                if (ConstrainPorportions)
                {
                    _width = Convert.ToInt32(_width*(v/(float) _height));
                }

                _height = v;
                CropY = v;
                CropX = _width;
            }
        }

        /// <summary>
        ///     Height to crop from 0
        /// </summary>
        public int CropY { get; set; }

        /// <summary>
        ///     Width to crop from 0
        /// </summary>
        public int CropX { get; set; }

        #endregion

        /// <summary>
        ///     Open a local file
        /// </summary>
        /// <param name="inFile">file with path</param>
        public static ImageScale LoadImage(string inFile)
        {
            //Async
            var img = new ImageScale {CropX = -1, CropY = -1};
            var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read);
            img._mySource = new Bitmap(fs);
            fs.Close();
            img.PrepareBitmap();
            return img;
        }

        /// <summary>
        ///     Open local file async
        /// </summary>
        /// <param name="inFile">file with path</param>
        /// <returns></returns>
        public static async Task<ImageScale> LoadImageAsync(string inFile)
        {
            //Async
            var img = new ImageScale {CropX = -1, CropY = -1};
            var stream = new MemoryStream();
            using (FileStream fileStream = File.OpenRead(inFile))
            {
                await fileStream.CopyToAsync(stream);
            }
            img._mySource = new Bitmap(stream);
            img.PrepareBitmap();
            return img;
        }

        /// <summary>
        ///     Get screen shot of website async
        /// </summary>
        /// <param name="uri">uri to website</param>
        /// <param name="w">width of image</param>
        /// <param name="h">height of image</param>
        /// <returns></returns>
        public static async Task<ImageScale> ScreenCaptureAsync(Uri uri, int w, int h)
        {
            var img = new ImageScale
            {
                CropX = -1,
                CropY = -1,
                _mySource = await Task.Run(() => BrowserImage.GetWebSiteThumbnail(uri.ToString(), 1024, 768, w, h))
            };
            //Async
            img.PrepareBitmap();
            return img;
        }

        /// <summary>
        ///     Get screen shot of website
        /// </summary>
        /// <param name="uri">uri to website</param>
        /// <param name="w">width of image</param>
        /// <param name="h">height of image</param>
        /// <returns></returns>
        public static ImageScale ScreenCapture(Uri uri, int w, int h)
        {
            var img = new ImageScale
            {
                CropX = -1,
                CropY = -1,
                _mySource = BrowserImage.GetWebSiteThumbnail(uri.ToString(), 1024, 768, w, h)
            };
            //Async
            img.PrepareBitmap();
            return img;
        }


        /// <summary>
        ///     Get image from a stream
        /// </summary>
        /// <param name="stream">input stream containing the image data</param>
        public static ImageScale LoadStream(Stream stream)
        {
            var img = new ImageScale {CropX = -1, CropY = -1, _mySource = new Bitmap(stream)};
            img.PrepareBitmap();
            return img;
        }

        private void PrepareBitmap()
        {
            _height = _mySource.Height;
            _width = _mySource.Width;
            CropY = _height;
            CropX = _width;
            _interpolation = InterpolationMode.HighQualityBilinear;
        }

        /// <summary>
        ///     Get image from web URI async
        /// </summary>
        /// <param name="uri">URI to image</param>
        public static async Task<ImageScale> GetWebImageAsync(Uri uri)
        {
            var img = new ImageScale();
            WebRequest wr = WebRequest.Create(uri);
            var stream = new MemoryStream();
            bool success = true;
            try
            {
                using (WebResponse response = await wr.GetResponseAsync())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        await responseStream.CopyToAsync(stream);
                    }
                }
                img.CropX = -1;
                img.CropY = -1;
                img._mySource = new Bitmap(stream);
                img.PrepareBitmap();
                return img;
            }
            catch (WebException ex) //image could not be read, use a default one.
            {
                success = false;
            }

            if (!success)
            {
                img.LastError = "Image was not found";
                string inFile = HttpContext.Current.Request.PhysicalApplicationPath + "/white.gif";

                using (FileStream SourceStream = File.Open(inFile, FileMode.Open))
                {
                    await SourceStream.CopyToAsync(stream);
                }
                img._mySource = new Bitmap(stream);
                img.PrepareBitmap();
            }
            return img;
        }

        /// <summary>
        ///     Get image from web
        /// </summary>
        /// <param name="uri">Uri to image</param>
        /// <returns></returns>
        public static ImageScale GetWebImage(Uri uri)
        {
            var img = new ImageScale();
            WebRequest wr = WebRequest.Create(uri);
            var stream = new MemoryStream();
            bool success = true;
            try
            {
                using (WebResponse response = wr.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            responseStream.CopyTo(stream);
                        }
                    }
                }
                img.CropX = -1;
                img.CropY = -1;
                img._mySource = new Bitmap(stream);
                img.PrepareBitmap();
                return img;
            }
            catch (WebException ex) //image could not be read, use a default one.
            {
                success = false;
            }

            if (!success)
            {
                img.LastError = "Image was not found";
                string inFile = HttpContext.Current.Request.PhysicalApplicationPath + "/white.gif";

                using (FileStream SourceStream = File.Open(inFile, FileMode.Open))
                {
                    SourceStream.CopyTo(stream);
                }
                img._mySource = new Bitmap(stream);
                img.PrepareBitmap();
            }
            return img;
        }

        /// <summary>
        ///     Save image and return stream
        /// </summary>
        /// <param name="format">Image format</param>
        /// <param name="bitmapQuality">If jpeg, then select quality 1-100. Default 50</param>
        /// <param name="cropRectangle">Do you want to crop?</param>
        /// <returns></returns>
        public MemoryStream Save(ImageFormat format, int bitmapQuality = 50, Rectangle? cropRectangle = null)
        {
            BitmapStream = new MemoryStream();
            _targetBitmap = new Bitmap(CropX, CropY);
            _targetBitmap.MakeTransparent();
            Graphics bmpGraphics = Graphics.FromImage(_targetBitmap);
            // set Drawing Quality
            bmpGraphics.InterpolationMode = _interpolation;
            bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);
            Rectangle compressionRectangle = cropRectangle.HasValue
                ? cropRectangle.Value
                : new Rectangle(0, 0, _width, _height);
            bmpGraphics.DrawImage(_mySource, compressionRectangle);
            _mySource.Dispose();

            if (format == ImageFormat.Jpeg)
            {
                var destEncParams = new EncoderParameters(1);
                var qualityParam = new EncoderParameter(Encoder.Quality, bitmapQuality);
                destEncParams.Param[0] = qualityParam;

                _targetBitmap.Save(BitmapStream, GetCodecInfo("jpeg"), destEncParams);
            }
            else
            {
                _targetBitmap.Save(BitmapStream, FmtConv(format));
            }
            _targetBitmap.Dispose();
            return BitmapStream;
        }

        /// <summary>
        ///     Save to file async
        /// </summary>
        /// <param name="filePath">full path to desination file</param>
        /// <param name="format">file format</param>
        /// <param name="bitmapQuality">If jpeg, then select quality 1-100. Default 50</param>
        /// <param name="cropRectangle">Wanna crop?</param>
        public async void SaveAsync(string filePath, ImageFormat format, int bitmapQuality = 50,
            Rectangle? cropRectangle = null)
        {
            MemoryStream stream = Save(format, bitmapQuality, cropRectangle);
            using (FileStream fileStream = File.Create(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }
        }

        /// <summary>
        ///     Save to file
        /// </summary>
        /// <param name="filePath">full path to desination file</param>
        /// <param name="format">file format</param>
        /// <param name="bitmapQuality">If jpeg, then select quality 1-100. Default 50</param>
        /// <param name="cropRectangle">Wanna crop?</param>
        public void Save(string filePath, ImageFormat format, int bitmapQuality = 50, Rectangle? cropRectangle = null)
        {
            MemoryStream stream = Save(format, bitmapQuality, cropRectangle);
            using (FileStream fileStream = File.Create(filePath))
            {
                stream.CopyTo(fileStream);
            }
        }


        /// <summary>
        ///     get codec info from file extension
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        private ImageCodecInfo GetCodecInfo(string fmt)
        {
            return
                ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(c => c.MimeType != null && c.MimeType.IndexOf(fmt, StringComparison.Ordinal) > -1);
        }

        /// <summary>
        ///     Sets the max proportions. This wills keep the porportions of the image and set the height and width within the h,w
        ///     limits
        /// </summary>
        /// <param name="h">The max height.</param>
        /// <param name="w">The max width.</param>
        public void SetMaxProportions(int h, int w)
        {
            ConstrainPorportions = true;
            if (Height > Width)
            {
                if (Height > h)
                {
                    Height = h;
                }
            }
            else
            {
                if (Width > w)
                {
                    Width = w;
                }
            }
        }

        public void Dispose()
        {
            _mySource.Dispose(); // release source bitmap.
            _targetBitmap.Dispose();
        }

        private System.Drawing.Imaging.ImageFormat FmtConv(ImageFormat fmt)
        {
            switch (fmt)
            {
                case ImageFormat.Jpeg:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case ImageFormat.Bmp:
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                case ImageFormat.Emf:
                    return System.Drawing.Imaging.ImageFormat.Emf;
                    break;
                case ImageFormat.Gif:
                    return System.Drawing.Imaging.ImageFormat.Gif;
                    break;
                case ImageFormat.Png:
                    return System.Drawing.Imaging.ImageFormat.Png;
                    break;
                case ImageFormat.Tiff:
                    return System.Drawing.Imaging.ImageFormat.Tiff;
                    break;
                default:
                    return System.Drawing.Imaging.ImageFormat.Png;
            }
        }
    }
}