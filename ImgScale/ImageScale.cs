using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;


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
    /// <summary>
    /// Handles imgae manipulation
    /// </summary>
    public class ImageScale
    {
        public bool ConstrainPorportions = true;
        private int _height = -1;
        private int _width = -1;
        private int _cropX = -1;
        private int _cropY = -1;
        private Bitmap mySource;
        private Rectangle? cropRectangle = null;
        private ImageFormat imageFormat = ImageFormat.Png;

        /// <summary>
        /// Open a local file
        /// </summary>
        /// <param name="InFile">file with path</param>
        public ImageScale(string InFile)
        {
            FileStream fs = new FileStream(InFile, FileMode.Open, FileAccess.Read);
            this.mySource = new Bitmap(fs);
            fs.Close();
            this._height = this.mySource.Height;
            this._width = this.mySource.Width;
            this._cropY = this._height;
            this._cropX = this._width;
            this.imageFormat = ParseImageFormat(Path.GetExtension(InFile));
        }



        /// <summary>
        /// Get image from web URI
        /// </summary>
        /// <param name="uri">URI to image</param>
        /// <param name="format">ex. ".jpg"</param>
        public ImageScale(Uri uri, string format)
        {
            WebRequest wr = WebRequest.Create(uri);
            try
            {
                HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

                Stream responseStream = response.GetResponseStream();
                Stream s = CopyStream(responseStream);

                this.mySource = new Bitmap(s);
                this._height = this.mySource.Height;
                this._width = this.mySource.Width;
                this._cropY = this._height;
                this._cropX = this._width;
                this.imageFormat = ParseImageFormat(Path.GetExtension(format));
            }
            catch (WebException ex) //image could not be read, use a default one.
            {
                HttpWebResponse webResponse = (HttpWebResponse)ex.Response;
                if (webResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    var InFile = HttpContext.Current.Request.PhysicalApplicationPath + "/white.gif";
                    FileStream fs = new FileStream(InFile, FileMode.Open, FileAccess.Read);
                    this.mySource = new Bitmap(fs);
                    fs.Close();
                    this._height = this.mySource.Height;
                    this._width = this.mySource.Width;
                    this._cropY = this._height;
                    this._cropX = this._width;
                    this.imageFormat = ParseImageFormat(Path.GetExtension(InFile));
                }
            }
        }

        /// <summary>
        /// Get screenshot from a website url
        /// </summary>
        /// <param name="uri">URI to website</param>
        /// <param name="w">image width</param>
        /// <param name="h">image height</param>
        public ImageScale(Uri uri,int w, int h)
        {
            const string format = ".png";
            mySource = BrowserImage.GetWebSiteThumbnail(uri.ToString(), 1024, 768, w, h);
            this._height = this.mySource.Height;
            this._width = this.mySource.Width;
            this._cropY = this._height;
            this._cropX = this._width;
            this.imageFormat = ParseImageFormat(format);

        }

        /// <summary>
        /// Get image from a stream
        /// </summary>
        /// <param name="stream">input stream containing the image data</param>
        /// <param name="format">ex. ".jpg"</param>
        public ImageScale(Stream stream, string format)
        {

            mySource = new Bitmap(stream);
            _height = this.mySource.Height;
            _width = this.mySource.Width;
            _cropY = this._height;
            _cropX = this._width;
            imageFormat = ParseImageFormat(Path.GetExtension(format));
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
                _cropX = v;
                _cropY = _height;
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
                _cropY = v;
                _cropX = _width;
            }
        }

        /// <summary>
        /// Height to crop from 0
        /// </summary>
        public int CropY
        {
            get
            {
                return _cropY;
            }
            set
            {
                _cropY = value;
            }
        }
        /// <summary>
        /// Width to crop from 0
        /// </summary>
        public int CropX
        {
            get
            {
                return _cropX;
            }
            set
            {
                _cropX = value;
            }
        }

        /// <summary>
        /// Crop a bitmap
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        private Bitmap CropBitmap(Bitmap bitmap, Rectangle rect)
        {
            return bitmap.Clone(rect, bitmap.PixelFormat);
        }
        /// <summary>
        /// Save image to byte array that is returned
        /// </summary>
        /// <returns></returns>
        public byte[] SaveFile()
        {
            MemoryStream stream = new MemoryStream();
            try
            {
                Bitmap TargetBitmap = new Bitmap(this._cropX, this._cropY);
                TargetBitmap.MakeTransparent();
                Graphics bmpGraphics = Graphics.FromImage(TargetBitmap);
                // set Drawing Quality
                bmpGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);

                Rectangle compressionRectangle = cropRectangle.HasValue ? cropRectangle.Value : new Rectangle(0, 0, this._width, this._height);

                bmpGraphics.DrawImage(mySource, compressionRectangle);
                this.mySource.Dispose();
                TargetBitmap.Save(stream, imageFormat);

                byte[] bmpBytes = stream.GetBuffer();
                TargetBitmap.Dispose();
                stream.Close();
                return bmpBytes;
            }
            catch
            {
                throw new Exception("Could not scale image stream");
            }
        }
        /// <summary>
        /// Saves the file to a stream that is returned
        /// </summary>
        /// <param name="quality"></param>
        /// <returns></returns>
        public MemoryStream SaveFile(int quality)
        {
            MemoryStream stream = new MemoryStream();
            try
            {
                Bitmap TargetBitmap = new Bitmap(this._cropX, this._cropY);
                Graphics bmpGraphics = Graphics.FromImage(TargetBitmap);
                // set Drawing Quality
                bmpGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);

                Rectangle compressionRectangle = cropRectangle.HasValue ? cropRectangle.Value : new Rectangle(0, 0, this._width, this._height);
                bmpGraphics.DrawImage(mySource, compressionRectangle);
                this.mySource.Dispose(); 
                TargetBitmap.Save(stream, imageFormat);

                return stream;
            }
            catch
            {
                throw new Exception("Could not scale image stream");
            }
        }

        /// <summary>
        /// Save image to stream
        /// </summary>
        /// <param name="stream">ouput stream</param>
        /// <param name="quality">jpeg quality</param>
        public void SaveFile(Stream stream, int quality)
        {
            //Create an EncoderParameters collection to contain the
            //parameters that control the dest format's encoder
            EncoderParameters destEncParams = new EncoderParameters(1);

            //Use quality parameter
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            destEncParams.Param[0] = qualityParam;

            try
            {
                Bitmap TargetBitmap = new Bitmap(this._cropX, this._cropY);
                TargetBitmap.MakeTransparent();
                Graphics bmpGraphics = Graphics.FromImage(TargetBitmap);
                // set Drawing Quality
                bmpGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);

                Rectangle compressionRectangle = cropRectangle.HasValue ? cropRectangle.Value : new Rectangle(0, 0, this._width, this._height);
                bmpGraphics.DrawImage(mySource, compressionRectangle);
                this.mySource.Dispose(); // release source bitmap.

                if(imageFormat == ImageFormat.Jpeg)
                {
                    TargetBitmap.Save(stream, GetCodecInfo("jpeg"), destEncParams);
                }
                else
                {
                    TargetBitmap.Save(stream, imageFormat);
                }
                TargetBitmap.Dispose();
            }
            catch(Exception)
            {
                throw new Exception("Could not scale image stream");
            }
        }
        /// <summary>
        /// Save Image to file with quality
        /// </summary>
        /// <param name="path">path and filename</param>
        /// <param name="quality">The quality.</param>
        public void SaveFile(string path, int quality)
        {
            //Create an EncoderParameters collection to contain the
            //parameters that control the dest format's encoder
            EncoderParameters destEncParams = new EncoderParameters(1);

            //Use quality parameter
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            destEncParams.Param[0] = qualityParam;

            Bitmap TargetBitmap = new Bitmap(this._cropX, this._cropY);//,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics bmpGraphics = Graphics.FromImage(TargetBitmap);
            // set Drawing Quality
            bmpGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            bmpGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            mySource.RotateFlip(RotateFlipType.RotateNoneFlipNone);

            Rectangle compressionRectangle = new Rectangle(0, 0, this._width, this._height);
            bmpGraphics.DrawImage(mySource, compressionRectangle);

            //TODO: implement in all methods
            if (cropRectangle.HasValue)
            {
                Bitmap crop = new Bitmap(cropRectangle.Value.Width, cropRectangle.Value.Height);
                Graphics gfx = Graphics.FromImage(crop);
                gfx.DrawImage(TargetBitmap, new Rectangle(0, 0, cropRectangle.Value.Width, cropRectangle.Value.Height), cropRectangle.Value, GraphicsUnit.Pixel);
                TargetBitmap.Dispose();
                TargetBitmap = crop;
            }
            string fullPath = path;
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }

            if(imageFormat == ImageFormat.Jpeg)
            {
                TargetBitmap.Save(fullPath, GetCodecInfo("jpeg"), destEncParams);
            }
            else
            {
                TargetBitmap.Save(fullPath, imageFormat);
            }

            TargetBitmap.Dispose();
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
            this.mySource.Dispose(); // release source bitmap.
        }

        /// <summary>
        /// Copy a stream
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        private static Stream CopyStream(Stream inputStream)
        {
            const int readSize = 256;
            byte[] buffer = new byte[readSize];
            MemoryStream ms = new MemoryStream();

            int count = inputStream.Read(buffer, 0, readSize);
            while (count > 0)
            {
                ms.Write(buffer, 0, count);
                count = inputStream.Read(buffer, 0, readSize);
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
