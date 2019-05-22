namespace KinectHandTracking
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.SkeletonBasics;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    [CLSCompliant(false)]
    public partial class MainWindow : Window
    {
        //Kinect
        private KinectSensor sensor;
        private DrawingImage imageSource;
        private Image<Gray, byte> My_Image;
        private SkeletonDraw skeletonDraw;
        private Hand hand;
        private DabCounter dabCounter;

        //Eclipse
        private System.Windows.Shapes.Ellipse mouseEllipse;

        //Slides
        private List<UserControl> userControls;
        private int slideIndex = 0;
        private double widthOfControlPanel = 0;

        public MainWindow()
        {
            InitializeComponent();
            //Ukrywanie wyświetlania debugowania
            layoutGrid.Visibility = Visibility.Hidden;

            SetupUserControls();

            hand = new Hand(20);
            dabCounter = new DabCounter();
            
            SetupSlideDispatcher();
            CreateAnEllipse();
            SetupDispatcher();
        }

        ///<summary>    
        /// Creates a blue ellipse with black border    
        ///</summary>    
        public void CreateAnEllipse()
        {
            // Create an Ellipse    
            mouseEllipse = new System.Windows.Shapes.Ellipse();
            mouseEllipse.Height = 100;
            mouseEllipse.Width = 100;
            // Create a blue and a black Brush    
            SolidColorBrush blueBrush = new SolidColorBrush();
            blueBrush.Color = Colors.Blue;
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;
            // Set Ellipse's width and color    
            mouseEllipse.StrokeThickness = 4;
            mouseEllipse.Stroke = blackBrush;
            // Fill rectangle with blue color    
            mouseEllipse.Fill = blueBrush;
            // Add Ellipse to the Grid.    
            mainGrid.Children.Add(mouseEllipse);
            //layoutGrid.Children.Add(mouseEllipse);
        }

        /// <summary>
        /// Kinect stuff below
        /// </summary>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            My_Image = new Image<Gray, byte>((int)Image.Width, (int)Image.Height, new Gray(0));
            imgBox.Source = BitmapSourceConvert.ToBitmapSource(My_Image);

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            skeletonDraw = new SkeletonDraw(sensor, Image);
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            skeletonDraw.Draw(skeletons);

            bool isScelTracked = false;
            if (skeletons.Length != 0)
            {
                foreach (Skeleton skeleton in skeletons)
                {
                    if (isScelTracked) break;

                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        Updates(skeleton);
                        isScelTracked = true;
                    }
                    else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                    {

                    }
                }
            }
        }
        private void Updates(Skeleton skeleton)
        {
            //Dab counter
            dabCounter.Update(skeleton);
            ((UserControlDab) userControls[1]).label2.Content = dabCounter.dabCounter;

            Joint handJoint;
            float downLimit;

            if (skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.HandRight].Position.Y)
            {
                handJoint = skeleton.Joints[JointType.HandLeft];
                downLimit = skeleton.Joints[JointType.ElbowLeft].Position.Y;
            }
            else
            {
                handJoint = skeleton.Joints[JointType.HandRight];
                downLimit = skeleton.Joints[JointType.ElbowRight].Position.Y;
            }

            //Hand has to be higher then
            if (downLimit < handJoint.Position.Y)
            {
                Point p = SkeletonPointToScreen(handJoint.Position);

                hand.Update(p);
                hand.Draw(My_Image);

                Hand.Gesture g = hand.CheckForGesture();
                switch (g)
                {
                    case Hand.Gesture.SWIPE_LEFT:
                        ((UserControlAutobus)userControls[0]).label1.Content = "Left";
                        setSlideManual(slideIndex--);
                        
                        break;
                    case Hand.Gesture.SWIPE_RIGTH:
                        ((UserControlAutobus)userControls[0]).label1.Content = "Right";
                        setSlideManual(slideIndex++);
                        break;
                    case Hand.Gesture.SWIPE_UP:
                        ((UserControlAutobus)userControls[0]).label1.Content = "Up";
                        break;
                    case Hand.Gesture.SWIPE_DOWN:
                        ((UserControlAutobus)userControls[0]).label1.Content = "Down";

                        break;
                    default:
                        break;
                }
                
                Point p2 = hand.LastPoint();
                int x = (int)(p2.X * 3);
                int y = (int)(p2.Y * 2.25);

                NativeMethods.SetCursorPos(x, y);

                double left = x - (1980 / 2);
                double top  = y - (1080 / 2);

                mouseEllipse.Margin = new Thickness(left, top, 0, 0);
            }
            else
            {
                hand.resetSmallRatius();
            }
            
            // Set wpf Image to EmguImage
            imgBox.Source = BitmapSourceConvert.ToBitmapSource(My_Image);
            My_Image.SetZero();
        }
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Slides
        /// </summary>
        private void SetupUserControls()
        {
            userControls = new List<UserControl>();
            UserControl autobusUC = new UserControlAutobus();
            userControls.Add(autobusUC);
            UserControl dabUC = new UserControlDab();
            userControls.Add(dabUC);
            UserControl pogodaUC = new UserControlWeather();
            userControls.Add(pogodaUC);
            UserControl infoUC = new UserControlInfo();
            userControls.Add(infoUC);
            widthOfControlPanel = ControlsGrid.Width;
            userControlGrid.Children.Add(userControls[0]);
        }

        private void setSlideAuto()
        {
            if (!dispatcherTimer.IsEnabled)
                dispatcherTimer.Start();

            FadeOutAndRemoveSlide();
            //userControlGrid.Children.RemoveAt(1);

            slideIndex++;
            if (slideIndex >= userControls.Count)
                slideIndex = 0;

            userControlGrid.Children.Add(userControls[slideIndex]);
        }

        private void setSlideManual(int index)
        {
            dispatcherTimerSlide.Stop();
            FadeOutAndRemoveSlide();
            //userControlGrid.Children.RemoveAt(1);
            if(slideIndex >= userControls.Count)
            {
                slideIndex = 0;
            } else if(slideIndex < 0)
            {
                slideIndex = userControls.Count - 1;
            }
            userControlGrid.Children.Add(userControls[index]);
        }

        public void FadeOutAndRemoveSlide()
        {
            for (int i = 1000; i >= 0; i--)
            {
                userControlGrid.Children[1].Opacity = (double)(i / 1000);
                //Thread.Sleep(1);
            }
            userControlGrid.Children.RemoveAt(1);
        }

        /// <summary>
        /// Buttons
        /// </summary>
        private void Button_bus_Click(object sender, RoutedEventArgs e)
        {
            setSlideManual(0);
        }

        private void Button_dab_Click(object sender, RoutedEventArgs e)
        {
            setSlideManual(1);
        }

        private void Button_weather_Click(object sender, RoutedEventArgs e)
        {
            setSlideManual(2);
        }

        private void Button_info_Click(object sender, RoutedEventArgs e)
        {
            setSlideManual(3);
        }

        /// <summary>
        /// Dispatchers
        /// </summary>
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        void SetupDispatcher()
        {
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //NativeMethods.SetCursorPos(100, 100);
            Point p = NativeMethods.GetMousePosition();
            int x = (int)p.X;
            int y = (int)p.Y;

            double left = x;// - (1980 / 2);
            double top = y;// - (1080 / 2);

            mouseEllipse.Margin = new Thickness(left, top, 0, 0);
        }

        System.Windows.Threading.DispatcherTimer dispatcherTimerSlide = new System.Windows.Threading.DispatcherTimer();
        void SetupSlideDispatcher()
        {
            dispatcherTimerSlide.Tick += dispatcherTimer_SlideTick;
            dispatcherTimerSlide.Interval = new TimeSpan(0, 0, 0, 3, 0);
            dispatcherTimerSlide.Start();
        }
        private void dispatcherTimer_SlideTick(object sender, EventArgs e)
        {
            //HideAndShowButtons
            //if (ControlsGrid.Width == 0)
            //    ControlsGrid.Width = widthOfControlPanel;
            //else
            //    ControlsGrid.Width = 0;
            setSlideAuto();
        }
    }
}