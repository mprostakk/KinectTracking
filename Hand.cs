using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Emgu.CV;
using Emgu.CV.Structure;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    public class Hand
    {
        List<Point> points;
        double radiusSmall;
        double radiusBig;
        double minDistance;
        double rate;
        bool tracking;
        bool wasTracking;

        public enum Gesture { SWIPE_LEFT, SWIPE_RIGTH, NULL };
        
        public Hand(double radius = 5, double rate = 1, double minDistance = 100)
        {
            points = new List<Point>();
            points.Add(new Point(0, 0));
            radiusSmall = 0;
            radiusBig = radius;
            this.rate = rate;
            this.minDistance = minDistance;
            tracking = false;
            wasTracking = false;
        }

        public Point LastPoint()
        {
            return points[points.Count - 1];
        }

        public System.Drawing.Point ConvertPoint(Point p)
        {
            return new System.Drawing.Point((int)p.X, (int)p.Y);
        }

        static public double distance(Point p1, Point p2)
        {
            return (Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public void resetSmallRatius()
        {
            radiusSmall = 0;
        }

        public void Update(Point p)
        {
            CheckForGesture();
            double dist = distance(p, LastPoint());
            if(dist <= minDistance)
            {
                if (radiusSmall < radiusBig)
                {
                    radiusSmall += rate;
                }
                else
                {
                    radiusSmall = radiusBig;
                    tracking = true;
                }
            }
            else
            {
                wasTracking = tracking;
                tracking = false;
                radiusSmall -= rate * 2.5;
                if (radiusSmall < 0)
                    radiusSmall = 0;
            }
            points.Add(p);
        }

        public Point ArithmeticAverage(int val = 3)
        {
            int amount = 0;
            double sumX = 0;
            double sumY = 0;
            for (int i = points.Count - 1; i >= 0 && i >= points.Count - 1 - val; i--)
            {
                sumX += points[i].X;
                sumY += points[i].Y;
                amount++;
            }
            return new Point(sumX / amount, sumY / amount);
        }

        bool checkingForGesture = false;
        int gestureCounter = 0;
        public Gesture CheckForGesture()
        {
            if (!tracking)
            {
                if (wasTracking)
                {
                    checkingForGesture = true;
                }
            }
            if(gestureCounter > 15)
            {
                Point gPoint = ArithmeticAverage(15);
                gestureCounter = 0;
                checkingForGesture = false;

                double angle = Math.Atan(gPoint.X / gPoint.Y);
                
                if(angle > 140 && angle < 200)
                {
                    return Gesture.SWIPE_LEFT;
                }
            }

            if (checkingForGesture && !tracking)
            {
                gestureCounter++;
            }

            return Gesture.NULL;
        }

        public void Draw(Image<Gray, byte> image)
        {
            CvInvoke.Circle(image, ConvertPoint( LastPoint() ), (int)radiusBig, new MCvScalar(150, 150, 150));
            if(tracking)
            {
                CvInvoke.Circle(image, ConvertPoint(LastPoint()), (int)radiusSmall, new MCvScalar(255, 255, 255), -1);
            }
            else
            {
                CvInvoke.Circle(image, ConvertPoint( LastPoint() ), (int)radiusSmall, new MCvScalar(150, 150, 150), -1);
            }
        }
    }
}
