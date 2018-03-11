using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bakalaurinis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCapture _videoCapture = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            if(_videoCapture == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Video Files |*.mp4";
                if(ofd.ShowDialog() == true)
                {
                    _videoCapture = new VideoCapture(ofd.FileName);
                }
            }
            _videoCapture.ImageGrabbed += _videoCapture_ImageGrabbed;
            _videoCapture.Start();
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);


        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        private void _videoCapture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                Mat mat = new Mat();
                _videoCapture.Retrieve(mat);
                mat = DetectServing(mat);
                var realImage = ToBitmapSource(mat.ToImage<Bgr, byte>());
                this.Dispatcher.Invoke(() =>
                {
                    MainImage.Source = ToBitmapSource(mat.ToImage<Bgr, byte>()); //change to converted one.
                });
            }
            catch(Exception)
            {
                throw;
            }
        }

        private Mat DetectServing(Mat mat)
        {
            Mat result = new Mat();
            var processedImage = PreProcessImage(mat);
            
            var detectedBall = DetectBall(processedImage);
            result = detectedBall;
            return result;
            /*var detectedHand = DetectHand(processedImage);
            bool isLegal = DetectFirstRule();
            if (isLegal)
            {
                DetectSecondRule();
            }*/
        }

        private Mat DetectBall(Mat processedImage)
        {
            Mat copy = new Mat();
            CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(87, 0, 0)), new ScalarArray(new MCvScalar(180, 125, 125)), copy);
            CvInvoke.Erode(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.Dilate(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            var ball = CvInvoke.HoughCircles(copy, HoughType.Gradient, 2.0, 200.0, 1.0, 30.0, 0, 0);

            Mat returnedImage = processedImage;
            if (ball.Length > 0)
            {
                foreach (var b in ball)
                {
                    CvInvoke.Circle(returnedImage, System.Drawing.Point.Round(b.Center), (int)b.Radius, new MCvScalar(105, 100, 50), 3);
                }
            }
            return returnedImage;
        }

        private Mat PreProcessImage(Mat mat)
        {
            Mat copy = new Mat();
            CvInvoke.CvtColor(mat, copy, ColorConversion.Bgr2Hsv);
            CvInvoke.GaussianBlur(copy, copy, new System.Drawing.Size(3, 3), 0);
            return copy;
        }
    }
}
