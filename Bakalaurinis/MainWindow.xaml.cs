using Bakalaurinis.Properties;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

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
        private int ballCoordX = 0;
        private int ballCoordY = 0;
        private VideoCapture _videoCapture = null;
        private float recordedHeight = 0;
        private bool ballIsInHand = false;
        private int frames = 0;
        private int handFrames = 0;
        private int ballFrames = 0;
        private InitiateRule rule = InitiateRule.FirstRule;
        private List<int> lastHalfSecondRadiuses = new List<int>();
        private CircleF lastFoundBall = new CircleF();
        private Mat lastFrame;
        private CircleF lastFrameBall = new CircleF();
        private CircleF lastFrameBallInHand = new CircleF();
        private bool isLastFrameBallInHand = false;
        private LineSegment2D line;

        public MainWindow()
        {
            InitializeComponent();
        }

        public enum InitiateRule
        {
            FirstRule = 0,
            SecondRule = 1,
            Waiting = 2
        }

        private Mat DetectServing(Mat mat)
        {
            mat = mat.ToImage<Bgr, byte>().Rotate(90, new Bgr(0, 0, 0)).Mat;
            Mat result = mat.Clone();
            var processedImage = PreProcessImage(mat);
            var detectedBall = DetectBall(processedImage);
            var detectedHand = DetectHand(processedImage);

            switch (rule)
            {
                case InitiateRule.FirstRule:
                    DetectFirstRule(detectedBall, detectedHand, result);
                    break;

                case InitiateRule.SecondRule:
                    DetectSecondRule(detectedBall, lastFrameBall, lastFrameBallInHand, detectedHand, result);
                    break;

                default:
                    rule = InitiateRule.FirstRule;

                    break;
            }

            /*bool isLegal = DetectFirstRule(detectedBall, detectedHand, result);
            if (isLegal)
            {
                DetectSecondRule(detectedBall, lastFoundBall, lastFrameBallInHand, detectedHand, result);
            }*/

            result = DrawObjects(detectedBall, detectedHand, mat);
            lastFrame = mat;
            lastFrameBall = detectedBall.Center.X != 0 ? detectedBall : lastFrameBall;
            return result;
        }

        private void DetectSecondRule(CircleF currentFrameBall, CircleF lastFrameBall, CircleF lastFrameBallInHand, VectorOfPoint hand, Mat image)
        {
            // compare last frame ball coordinates with current frame ball coordinates

            // if y axis is highier in current frame - loop again
            if (currentFrameBall.Center.Y < lastFrameBall.Center.Y)
            {
                //recordedHeight = ((lastFrameBallInHand.Center.Y - currentFrameBall.Center.Y) * 2) / lastFrameBall.Radius;
                return;
            }

            if (currentFrameBall.Center.Y > lastFrameBall.Center.Y && currentFrameBall.Center.Y > lastFrameBallInHand.Center.Y)
            {
                rule = InitiateRule.FirstRule;
                recordedHeight = -1;
                //recordedHeight = ((lastFrameBallInHand.Center.Y - currentFrameBall.Center.Y) * 2) / lastFrameBall.Radius;
                return;
            }

            // if y axis is highier than when ball was in hand but lower that last frame - calculate height (last frame - last fame in hand
            if (currentFrameBall.Center.Y > lastFrameBall.Center.Y && currentFrameBall.Center.Y < lastFrameBallInHand.Center.Y)
            {
                float height = lastFrameBallInHand.Center.Y - lastFrameBall.Center.Y;
                if (height >= (lastFrameBall.Radius * 2 * 4))
                {
                    line = new LineSegment2D(new System.Drawing.Point(
                        (int)lastFrameBall.Center.X, 
                        (int)lastFrameBall.Center.Y),
                        new System.Drawing.Point((int)lastFrameBallInHand.Center.X,
                        (int)lastFrameBallInHand.Center.Y)
                        );
                }
                recordedHeight = (height * 2) / lastFrameBall.Radius;
                rule = InitiateRule.FirstRule;
                return;
            }
            // if ball is not recognized for 1 second - count as failed pass - go to first rule.

            // need logging
        }

        private bool DetectFirstRule(CircleF ball, VectorOfPoint detectedHand, Mat image)
        {
            Mat circle = new Mat(image.Rows, image.Cols, image.Depth, image.NumberOfChannels);
            Mat hand = new Mat(image.Rows, image.Cols, image.Depth, image.NumberOfChannels);
            circle.SetTo(new MCvScalar(0));
            hand.SetTo(new MCvScalar(0));

            int averageBallRadius = lastHalfSecondRadiuses.Count > 0 ? lastHalfSecondRadiuses.Max() : (int)ball.Radius;

            if (ball.Radius > 0)
            {
                CvInvoke.Circle(circle, System.Drawing.Point.Round(ball.Center), averageBallRadius, new Bgr(System.Drawing.Color.White).MCvScalar, -1);
            }

            if (detectedHand.Size != 0)
            {
                var cont = new VectorOfVectorOfPoint(detectedHand);
                VectorOfPoint hull = new VectorOfPoint();
                CvInvoke.ConvexHull(detectedHand, hull, false, true);
                cont = new VectorOfVectorOfPoint(hull);
                CvInvoke.DrawContours(hand, cont, 0, new Bgr(System.Drawing.Color.White).MCvScalar, -1);
            }

            Mat res = new Mat(image.Rows, image.Cols, image.Depth, image.NumberOfChannels);
            CvInvoke.BitwiseAnd(circle, hand, res);
            CvInvoke.CvtColor(res, res, ColorConversion.Hsv2Bgr);
            CvInvoke.CvtColor(res, res, ColorConversion.Bgr2Gray);
            bool ballInHand = CvInvoke.CountNonZero(res) > 0;
            /*double isInSideSouth = -1.0;
            double isInSideNorth = -1.0;
            double isInSideEast = -1.0;
            double isInSideWest = -1.0;*/
            /*if (detectedHand.Size != 0 && ball.Radius > 0)
            {
                isInSideSouth = CvInvoke.PointPolygonTest(detectedHand, new PointF(ball.Center.X, ball.Center.Y + ball.Radius), false);
                isInSideNorth = CvInvoke.PointPolygonTest(detectedHand, new PointF(ball.Center.X, ball.Center.Y - ball.Radius), false);
                isInSideEast = CvInvoke.PointPolygonTest(detectedHand, new PointF(ball.Center.X + ball.Radius, ball.Center.Y), false);
                isInSideWest = CvInvoke.PointPolygonTest(detectedHand, new PointF(ball.Center.X - ball.Radius, ball.Center.Y), false);
            }
            if (isInSideSouth == 1 || isInSideSouth == 0 || isInSideNorth == 1 || isInSideNorth == 0 || isInSideEast == 1 || isInSideEast == 0 || isInSideWest == 1 || isInSideWest == 0)
            {
                ballInHand = true;
            }
            else
            {
                ballInHand = false;
            }*/
            if (ballInHand == true)
            {
                lastFrameBallInHand = ball;
            }
            if (ballInHand == false && ballIsInHand == true)
            {
                rule = InitiateRule.SecondRule;
            }
            ballIsInHand = ballInHand;
            return ballInHand;
        }

        private VectorOfPoint DetectHand(Mat processedImage)
        {
            Mat copy = new Mat();
            CvInvoke.CvtColor(processedImage, copy, ColorConversion.Bgr2Hsv);
            /*CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(low_H, low_S, low_V)), new ScalarArray(new MCvScalar(high_H, high_S, high_V)), copy);*/
            CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(0, 63, 59)), new ScalarArray(new MCvScalar(19, 228, 247)), copy);
            CvInvoke.Erode(copy, copy, null, new System.Drawing.Point(-1, -1), 5, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.Dilate(copy, copy, null, new System.Drawing.Point(-1, -1), 5, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);

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
                CvInvoke.ApproxPolyDP(biggestContour, biggestContour, 0.0015, true);
                handFrames++;
                return biggestContour;
            }
            return new VectorOfPoint();
        }

        private Mat DrawObjects(CircleF detectedBall, VectorOfPoint detectedHand, Mat mat)
        {
            if (detectedHand.Size != 0)
            {
                VectorOfPoint hull = new VectorOfPoint();
                CvInvoke.ConvexHull(detectedHand, hull, false, true);
                var cont = new VectorOfVectorOfPoint(hull);
                CvInvoke.DrawContours(mat, cont, 0, new Bgr(System.Drawing.Color.Green).MCvScalar, -1);
            }

            if (detectedBall.Radius > 0)
            {
                int averageBallRadius = lastHalfSecondRadiuses.Count > 0 ? lastHalfSecondRadiuses.Max() : (int)detectedBall.Radius;
                CvInvoke.Circle(mat, System.Drawing.Point.Round(detectedBall.Center), averageBallRadius, new Bgr(System.Drawing.Color.Red).MCvScalar, -1);
            }
            else
            {
                if (lastFoundBall.Radius > 0 && lastFoundBall.Center.X != 0)
                {
                    int averageBallRadius = lastHalfSecondRadiuses.Count > 0 ? lastHalfSecondRadiuses.Max() : (int)detectedBall.Radius;
                    CvInvoke.Circle(mat, System.Drawing.Point.Round(lastFoundBall.Center), averageBallRadius, new Bgr(System.Drawing.Color.Red).MCvScalar, -1);
                }
            }
            if (line.P1.X != 0)
            {
                CvInvoke.Line(mat, new System.Drawing.Point(line.P1.X, line.P1.Y), new System.Drawing.Point(line.P2.X, line.P2.Y), new Bgr(System.Drawing.Color.Yellow).MCvScalar, 3);
            }
            //CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Hsv);
            //CvInvoke.InRange(mat, new ScalarArray(new MCvScalar(low_H, low_S, low_V)), new ScalarArray(new MCvScalar(high_H, high_S, high_V)), mat);
            return mat;
        }

        private CircleF DetectBall(Mat processedImage)
        {
            Mat copy = new Mat();
            CvInvoke.CvtColor(processedImage, copy, ColorConversion.Bgr2Hsv);
            //CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(low_H, low_S, low_V)), new ScalarArray(new MCvScalar(high_H, high_S, high_V)), copy);
            CvInvoke.InRange(processedImage, new ScalarArray(new MCvScalar(94, 130, 91)), new ScalarArray(new MCvScalar(122, 217, 255)), copy);
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
                ballFrames++;
            }
            if (biggestContour.Size != 0)
            {
                var circle = CvInvoke.MinEnclosingCircle(biggestContour);
                lastHalfSecondRadiuses.Add((int)circle.Radius);
                if (lastHalfSecondRadiuses.Count > 15)
                {
                    lastHalfSecondRadiuses.RemoveAt(0);
                }
                lastFoundBall = circle;
                ballCoordX = (int)circle.Center.X;
                ballCoordY = (int)circle.Center.Y;
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
                frames++;
                mat = DetectServing(mat);
                var realImage = ToBitmapSource(mat.ToImage<Bgr, byte>().Resize(640, 420, Inter.Linear));
                this.Dispatcher.Invoke(() =>
                {
                    if (rule == InitiateRule.FirstRule)
                    {
                        logBlock.Text = rule.ToString() + "\n" + logBlock.Text;
                    }
                    if (recordedHeight >= 16 && recordedHeight > 0)
                    {
                        logBlock.Text = $"Success! Ball height was {recordedHeight} - rule passed\n" + logBlock.Text;
                    }
                    if (recordedHeight < 16 && recordedHeight > 0)
                    {
                        logBlock.Text = $" Failure! Ball was not high enough: {recordedHeight}\n" + logBlock.Text;
                    }
                    if (recordedHeight < 0 )
                    {
                        logBlock.Text = $"Ball was thrown down. \n" + logBlock.Text;
                        recordedHeight = 0;
                    }
                    if (rule == InitiateRule.SecondRule)
                    {
                        logBlock.Text = $"Waiting for ball to go up...\n" + logBlock.Text;
                    }
                    if (ballIsInHand)
                    {
                        ballIsInHandLabel.Content = "Yes";
                    }
                    else
                    {
                        ballIsInHandLabel.Content = "No";
                    }
                    ruleStateLabel.Content = rule.ToString();
                    ballXLabel.Content = ballCoordX.ToString();
                    ballYLabel.Content = ballCoordY.ToString();
                    ballHeightLabel.Content = recordedHeight.ToString();
                    frameLabel.Content = frames.ToString();
                    ballLabel.Content = ballFrames.ToString();
                    handLabel.Content = handFrames.ToString();
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
                ofd.Filter = "Video Files |*.mov;*.mp4";
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