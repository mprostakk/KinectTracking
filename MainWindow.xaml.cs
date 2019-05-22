namespace KinectHandTracking
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.SkeletonBasics;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Input;
    using System.Windows.Threading;

    [CLSCompliant(false)]
    public partial class MainWindow : Window
    {

        private KinectSensor sensor;

        private DrawingImage imageSource;
        private Image<Gray, byte> My_Image;

        private SkeletonDraw skeletonDraw;
        private Hand hand;
        private DabCounter dabCounter;
        public System.Windows.Shapes.Ellipse mouseEllipse;
        public System.Windows.Shapes.Ellipse mouseEllipseSmall;

        DispatcherTimer dispatcherTimer;

        private List<UserControl> userControls;
        private int slideIndex = 0;

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        void SetupDispatcher()
        {
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //NativeMethods.SetCursorPos(100, 100);
            //Point p = NativeMethods.GetMousePosition();
        }
        public MainWindow()
        {
            InitializeComponent();
            //Ukrywanie wyświetlania debugowania
            layoutGrid.Visibility = Visibility.Hidden;

            userControls = new List<UserControl>();
            UserControl autobusUC = new UserControlAutobus();
            userControls.Add(autobusUC);
            UserControl dabUC = new UserControlDab();
            userControls.Add(dabUC);


            userControlGrid.Children.Add(userControls[0]);
            
            hand = new Hand(20);
            dabCounter = new DabCounter();

            SetupSlideDispatcher();

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

            setSlideAuto();
        }

        private void setSlideAuto()
        {

            if (!dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Start();
            }

            userControlGrid.Children.RemoveAt(1);
            slideIndex++;

            if (slideIndex >= userControls.Count)
                slideIndex = 0;


            userControlGrid.Children.Add(userControls[slideIndex]);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        private void SetupKinectSensor()

        {

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
        }

        public MainWindow()
        {
            InitializeComponent();   
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.None;

            My_Image = new Image<Gray, byte>((int)Image.Width, (int)Image.Height, new Gray(0));
            imgBox.Source = BitmapSourceConvert.ToBitmapSource(My_Image);
            dispatcherTimer = new DispatcherTimer();

            hand = new Hand(20);
            dabCounter = new DabCounter();

            CreateAnEllipse(mouseEllipse);
            CreateAnEllipse(mouseEllipseSmall);

            mouseEllipse.Height = hand.radiusBig * 2;
            mouseEllipse.Width = hand.radiusBig * 2;

            mouseEllipseSmall.Height = hand.radiusBig * 2;
            mouseEllipseSmall.Width = hand.radiusBig * 2;

            SolidColorBrush GreenYellowBrush = new SolidColorBrush();
            GreenYellowBrush.Color = Colors.GreenYellow;
            SolidColorBrush WhiteBrush = new SolidColorBrush();
            WhiteBrush.Color = Colors.White;

            mouseEllipse.StrokeThickness = 1;
            mouseEllipse.Stroke = WhiteBrush;

            mouseEllipseSmall.Stroke = GreenYellowBrush;
            mouseEllipseSmall.StrokeThickness = 1;


            SetupDispatcher();
            //dispatcherTimer.Start();
            //mouseEllipse.
            SetupKinectSensor();
            skeletonDraw = new SkeletonDraw(sensor, Image);
        }
        
        public void CreateAnEllipse(System.Windows.Shapes.Ellipse ellipse)
        {
            ellipse = new System.Windows.Shapes.Ellipse();
            
            //SolidColorBrush blueBrush = new SolidColorBrush();
            //blueBrush.Color = Colors.Blue;
            //SolidColorBrush blackBrush = new SolidColorBrush();
            //blackBrush.Color = Colors.Black;
            //ellipse.StrokeThickness = 5;
            //ellipse.Stroke = blackBrush;
            //ellipse.Fill = blueBrush;
            //canvas.Children.Add(ellipse);
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
                        ((UserControlDab)userControls[1]).label1.Content = "Left";
                        break;
                    case Hand.Gesture.SWIPE_RIGTH:
                        ((UserControlDab)userControls[1]).label1.Content = "Right";
                        break;
                    case Hand.Gesture.SWIPE_UP:
                        ((UserControlDab)userControls[1]).label1.Content = "Up";
                        break;
                    case Hand.Gesture.SWIPE_DOWN:
                        ((UserControlDab)userControls[1]).label1.Content = "Down";

                        break;
                    default:
                        break;
                }
                
                Point p2 = hand.LastPoint();
                int x = (int)(p2.X * 3);
                int y = (int)(p2.Y * 2.25);

                NativeMethods.SetCursorPos(x, y);

                double left = x;
                double top  = y;

                mouseEllipse.Margin = new Thickness(left, top, 0, 0);
                mouseEllipse.StrokeThickness = hand.radiusSmall;
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

        /* button actions - obsluga przyciskow*/

        int i = 0;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Button clicked");
            //Window.Background = new SolidColorBrush(Colors.Green);
            button.Content = String.Format("Clicked {0}!", i);
            i++;

        }
        public void btn_mouseEnter(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("hover");
            Variables.getInstance().isHovering = 1;
        }
        public void btn_mouseLeave(object sender, RoutedEventArgs e)
        {
             System.Diagnostics.Debug.WriteLine("leave hover");
             Variables.getInstance().isHovering = 0;

        }
        //CheckBoxSeatedModeChanged
    }
}