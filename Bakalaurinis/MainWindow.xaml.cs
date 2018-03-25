using Bakalaurinis.Properties;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private int low_H = 1;
        private int low_S = 1;
        private int low_V = 1;

        private int high_H = 1;
        private int high_S = 0;
        private int high_V = 0;
        private VideoCapture _videoCapture = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private Mat DetectServing(Mat mat)
        {
            Mat result = mat.Clone();
            var processedImage = PreProcessImage(mat);
            
            var detectedBall = DetectBall(processedImage);
            
            var detectedHand = DetectHand(processedImage);
            Mat isLegal = DetectFirstRule(detectedBall, detectedHand, result);
            /*if (isLegal)
            {
                DetectSecondRule(detectedBalls, detectedHand, result);
            }*/

            result = DrawObjects(detectedBall, detectedHand, mat);

            return result;
        }

        private void DetectSecondRule(List<CircleF> ball, VectorOfPoint hand, Mat image)
        {
            
        }

        private Mat DetectFirstRule(CircleF ball, VectorOfPoint hand, Mat image)
        {
            Mat mat1 = new Mat(image.Rows, image.Cols, image.Depth, 1);
            Mat mat2 = new Mat(image.Rows, image.Cols, image.Depth, 1);
            
            CvInvoke.Circle(mat1, System.Drawing.Point.Round(ball.Center), (int)ball.Radius, new Bgr(System.Drawing.Color.Cyan).MCvScalar, 3);
            
            if (hand.Size != 0)
            {
                var cont = new VectorOfVectorOfPoint(hand);
                CvInvoke.DrawContours(mat2, cont, 0, new Bgr(System.Drawing.Color.Red).MCvScalar, 2);
            }

            Mat mat = new Mat(image.Rows, image.Cols, image.Depth, 1);
            CvInvoke.BitwiseAnd(mat1, mat2, mat);
            return mat;
        }

        private VectorOfPoint DetectHand(Mat processedImage)
        {
            Mat copy = new Mat();
            /*CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(low_H, low_S, low_V)), new ScalarArray(new MCvScalar(high_H, high_S, high_V)), copy);*/
            CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(0, 0, 225)), new ScalarArray(new MCvScalar(50, 190, 255)), copy);
            CvInvoke.Erode(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.Dilate(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            VectorOfPoint biggestContour = new VectorOfPoint();
            CvInvoke.FindContours(copy, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            if (contours.Size > 0)
            {
                biggestContour = contours[0];
                for (int i = 0; i < contours.Size; i++)
                {
                    if (contours[i].Size > biggestContour.Size)
                    {
                        biggestContour = contours[i];
                    }
                }
                return biggestContour;
            }
            return new VectorOfPoint();
        }

        private Mat DrawObjects(CircleF detectedBall, VectorOfPoint detectedHand, Mat mat)
        {
            if (detectedBall.Radius > 0)
            {
                CvInvoke.Circle(mat, System.Drawing.Point.Round(detectedBall.Center), (int)detectedBall.Radius, new Bgr(System.Drawing.Color.Red).MCvScalar, -1);
            }
            
            if (detectedHand.Size != 0)
            {
                var cont = new VectorOfVectorOfPoint(detectedHand);
                CvInvoke.DrawContours(mat, cont, 0, new Bgr(System.Drawing.Color.Cyan).MCvScalar, -1);
            }
            return mat;
        }

        private CircleF DetectBall(Mat processedImage)
        {
            Mat copy = new Mat();
            /*CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(low_H, low_S, low_V)), new ScalarArray(new MCvScalar(high_H, high_S, high_V)), copy);*/
            CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(43, 0, 00)), new ScalarArray(new MCvScalar(121, 255, 255)), copy);
            CvInvoke.Erode(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.Dilate(copy, copy, null, new System.Drawing.Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            /*var ball = CvInvoke.HoughCircles(copy, HoughType.Gradient, 3, 200, 400, 63);*/

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            VectorOfPoint biggestContour = new VectorOfPoint();
            CvInvoke.FindContours(copy, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            if (contours.Size > 0)
            {
                biggestContour = contours[0];
                for (int i = 0; i < contours.Size; i++)
                {
                    if (contours[i].Size > biggestContour.Size)
                    {
                        biggestContour = contours[i];
                    }
                }
            }
            if (biggestContour.Size != 0)
            {
                var circle = CvInvoke.MinEnclosingCircle(biggestContour);
                return circle;
            }

            return new CircleF();
        }

        private Mat PreProcessImage(Mat mat)
        {
            Mat copy = new Mat();
            CvInvoke.CvtColor(mat, copy, ColorConversion.Bgr2Hsv);
            CvInvoke.GaussianBlur(copy, copy, new System.Drawing.Size(3, 3), 0);
            return copy;
        }
        private void _videoCapture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                Mat mat = new Mat();
                bool retrieved = _videoCapture.Retrieve(mat);
                mat = DetectServing(mat);
                var realImage = ToBitmapSource(mat.ToImage<Bgr, byte>().Resize(640, 420, Inter.Linear));
                this.Dispatcher.Invoke(() =>
                {
                    MainImage.Source = ToBitmapSource(mat.ToImage<Bgr, byte>()); //change to converted one.
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void handSlider_H_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            low_H = (int)handSlider_H.Value;
            if (low_H_Label != null)
            {
                low_H_Label.Content = handSlider_H.Value;
            }
        }

        private void handSlider_S_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            low_S = (int)handSlider_S.Value;
            if (low_S_Label != null)
            {
                low_S_Label.Content = handSlider_S.Value;
            }
        }

        private void handSlider_V_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            low_V = (int)handSlider_V.Value;
            if (low_V_Label != null)
            {
                low_V_Label.Content = handSlider_V.Value;
            }
        }

        private void handSlider_high_H_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            high_H = (int)handSlider_high_H.Value;
            if (high_H_Label != null)
            {
                high_H_Label.Content = handSlider_high_H.Value;
            }
        }

        private void handSlider_high_S_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            high_S = (int)handSlider_high_S.Value;
            if (high_S_Label != null)
            {
                high_S_Label.Content = handSlider_high_S.Value;
            }
        }

        private void handSlider_high_V_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            high_V = (int)handSlider_high_V.Value;
            if (high_V_Label != null)
            {
                high_V_Label.Content = handSlider_high_V.Value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Upgrade();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoCapture == null)
            {
                _videoCapture = new VideoCapture(0);
            }
            _videoCapture.ImageGrabbed += _videoCapture_ImageGrabbed;
            _videoCapture.Start();
        }
        private void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoCapture == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Video Files |*.mov";
                if (ofd.ShowDialog() == true)
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


    }
}
