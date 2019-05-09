namespace KinectHandTracking
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Microsoft.Kinect;
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;

    [CLSCompliant(false)]

    public partial class MainWindow : Window
    {
        private KinectSensor sensor;

        private DrawingImage imageSource;
        private Image<Gray, byte> My_Image;

        private SkeletonDraw skeletonDraw;
        private Hand hand;
        private DabCounter dabCounter;

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        void SetupDispatcher()
        {
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //NativeMethods.SetCursorPos(100, 100);
        }

        public MainWindow()
        {
            InitializeComponent();
            hand = new Hand(20);
            dabCounter = new DabCounter();
        }
        
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
            label2.Content = dabCounter.dabCounter;

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
                        label1.Content = "Left";
                        break;
                    case Hand.Gesture.SWIPE_RIGTH:
                        label1.Content = "Right";
                        break;
                    case Hand.Gesture.SWIPE_UP:
                        label1.Content = "Up";
                        break;
                    case Hand.Gesture.SWIPE_DOWN:
                        label1.Content = "Down";

                        break;
                    default:
                        break;
                }

                //NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
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

        //CheckBoxSeatedModeChanged
    }
}