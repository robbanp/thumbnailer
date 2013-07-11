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
    /// Handles imgae manipulation
    /// </summary>
    public class GetWebSite
    {
        public bool ConstrainPorportions = true;

        private int _height = -1;
        private int _width = -1;
        internal Bitmap _mySource;
        private ImageFormat _imageFormat = ImageFormat.Png;
        public string LastError { get; set; }
        public MemoryStream BitmapStream { get; set; }
        private InterpolationMode _interpolation { get; set; }
        private short _bitmapQualityValue { get; set; }
 
        private Bitmap _targetBitmap { get; set; }

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
        public GetWebSite()
        {
            
        }
        private void PrepareBitmap()
        {
            this._height = this._mySource.Height;
            this._width = this._mySource.Width;
            this.CropY = this._height;
            this.CropX = this._width;
            _interpolation = InterpolationMode.HighQualityBilinear;
        }

        /// <summary>
        /// Open a local file
        /// </summary>
        /// <param name="inFile">file with path</param>
        public GetWebSite(string inFile)
        {
            //Async
            CropX = -1;
            CropY = -1;
            var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read);
            this._mySource = new Bitmap(fs);
            fs.Close();
            PrepareBitmap();
        }



        /// <summary>
        /// Get image from web URI
        /// </summary>
        /// <param name="uri">URI to image</param>
        /// <param name="format">ex. ".jpg"</param>
        public static async Task<GetWebSite> GetWebImageAsync(Uri uri)
        {
            var img = new GetWebSite();
            WebRequest wr = WebRequest.Create(uri);
            var stream = new MemoryStream();
            var success = true;
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
                    var inFile = HttpContext.Current.Request.PhysicalApplicationPath + "/white.gif";

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
        /// Get screenshot from a website url
        /// </summary>
        /// <param name="uri">URI to website</param>
        /// <param name="w">image width</param>
        /// <param name="h">image height</param>
        public GetWebSite(Uri uri, int w, int h)
        {
            CropX = -1;
            CropY = -1;
            const string format = ".png";
            _mySource = BrowserImage.GetWebSiteThumbnail(uri.ToString(), 1024, 768, w, h);
            PrepareBitmap();
        }

        /// <summary>
        /// Get image from a stream
        /// </summary>
        /// <param name="stream">input stream containing the image data</param>
        /// <param name="format">ex. ".jpg"</param>
        public GetWebSite(Stream stream)
        {
            CropX = -1;
            CropY = -1;
            _mySource = new Bitmap(stream);
            PrepareBitmap();
        }
        /// <summary>
        /// Image width in pixels.
        /// By default ConstrainPorportions is true and this will also affect the height.
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                int v = 1;
                if (value > 0)
                    v = value;

                if (ConstrainPorportions)
                {
                    _height = Convert.ToInt32(_height * (Convert.ToDouble(v) / Convert.ToDouble(_width))); 
                }
                _width = v;
                CropX = v;
                CropY = _height;
            }
        }
        /// <summary>
        /// Image height in pixels.
        /// By default ConstrainPorportions is true and this will also affect the width.
        /// </summary>
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                int v = 1;
                if (value > 0)
                    v = value;

                if (ConstrainPorportions)
                {
                    _width = Convert.ToInt32((float)_width * (float)((float)v / (float)this._height)); 
                }

                _height = v;
                CropY = v;
                CropX = _width;
            }
        }

        /// <summary>
        /// Height to crop from 0
        /// </summary>
        public int CropY { get; set; }

        /// <summary>
        /// Width to crop from 0
        /// </summary>
        public int CropX { get; set; }

 
        public MemoryStream Save(ImageFormat format,  int bitmapQuality = 50, Rectangle? cropRectangle = null)
        {
            BitmapStream = new MemoryStream();
                _targetBitmap = new Bitmap(this.CropX, this.CropY);
                _targetBitmap.MakeTransparent();
                Graphics bmpGraphics = Graphics.FromImage(_targetBitmap);
                // set Drawing Quality
                bmpGraphics.InterpolationMode = _interpolation;
                bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                _mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                Rectangle compressionRectangle = cropRectangle.HasValue ? cropRectangle.Value : new Rectangle(0, 0, this._width, this._height);
                bmpGraphics.DrawImage(_mySource, compressionRectangle);
                this._mySource.Dispose();

                if (format == ImageFormat.Jpeg)
                {
                    var destEncParams = new EncoderParameters(1);
                    var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, bitmapQuality);
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

         public async void SaveAsync(string filePath, ImageFormat format, int bitmapQuality = 50, Rectangle? cropRectangle = null)
         {
             var stream = Save(format, bitmapQuality, cropRectangle);
             using (var fileStream = File.Create(filePath))
             {
                 await stream.CopyToAsync(fileStream);
             }
         }

         public void Save(string filePath, ImageFormat format, int bitmapQuality = 50, Rectangle? cropRectangle = null)
         {
             var stream = Save(format, bitmapQuality, cropRectangle);
             using (var fileStream = File.Create(filePath))
             {
                  stream.CopyToAsync(fileStream);
             }
         }
       
      
        /// <summary>
        /// get codec
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        private ImageCodecInfo GetCodecInfo(string fmt)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType.IndexOf(fmt) > -1);
        }

        /// <summary>
        /// Get the ImageFormat type from a file extenstion string
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        private static ImageFormat ParseImageFormat(string fmt)
        {
            switch (fmt.ToLower())
            {
                case ".jpeg":
                    return ImageFormat.Jpeg;
                    break;
                case ".jpg":
                    return ImageFormat.Jpeg;
                    break;
                case ".gif":
                    return ImageFormat.Gif;
                    break;
                case ".png":
                    return ImageFormat.Png;
                    break;
                case ".bmp":
                    return ImageFormat.Bmp;
                    break;
                case ".tiff":
                    return ImageFormat.Tiff;
                    break;
                case ".tif":
                    goto case ".tiff";
                    break;
                case ".emf":
                    return ImageFormat.Emf;
                    break;
                default:
                    throw new Exception("Unsupported image format");
                    break;
            }
        }

        /// <summary>
        /// Sets the max proportions. This wills keep the porportions of the image and set the height and width within the h,w limits
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
            this._mySource.Dispose(); // release source bitmap.
            _targetBitmap.Dispose();
        }

    }
}
