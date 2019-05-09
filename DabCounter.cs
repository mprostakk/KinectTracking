using Emgu.CV;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace KinectHandTracking
{
    class DabCounter
    {
        bool rightDabFound;
        bool leftDabFound;
        Skeleton MySkeleton;
        Joint rightHand;
        Joint leftHand;
        Joint rightElbow;
        Joint leftElbow;
        Joint rightShoulder;
        Joint leftShoulder;
        public int dabCounter;

        public DabCounter()
        {
            rightDabFound = false;
            leftDabFound = false;
            dabCounter = 0;
        }

        public void Update(Skeleton skeleton)
        {
            MySkeleton = skeleton;
            rightHand = skeleton.Joints[JointType.HandRight];
            leftHand = skeleton.Joints[JointType.HandLeft];
            rightElbow = skeleton.Joints[JointType.ElbowRight];
            leftElbow = skeleton.Joints[JointType.ElbowLeft];
            rightShoulder = skeleton.Joints[JointType.ShoulderRight];
            leftShoulder = skeleton.Joints[JointType.ShoulderLeft];

            logs();

            if(!leftDabFound && !rightDabFound)
            {
                checkingForRightDab();
                checkingForLeftDab();
            }
            else if (leftDabFound)
            {
                if (checkingForRightDab())
                {
                    leftDabFound = false;
                    rightDabFound = true;
                    dabCounter++;
                }
            }
            else if (rightDabFound)
            {
                if (checkingForLeftDab())
                {
                    leftDabFound = true;
                    rightDabFound = false;
                    dabCounter++;
                }
            }
        }

        public void Draw(Image<Emgu.CV.Structure.Gray, byte> image)
        {
        }

        private void logs()
        {
            System.Diagnostics.Debug.WriteLine(
                "Przedramie R= " + XYToDegrees(rightHand.Position,  rightElbow.Position) + 
                " BicepsR= "     + XYToDegrees(rightElbow.Position, rightShoulder.Position) +
                " PrzedramieL= " + XYToDegrees(leftHand.Position,   leftElbow.Position) +
                " BicepsL = "    + XYToDegrees(leftElbow.Position,  leftShoulder.Position));
        }

        private bool checkingForRightDab()
        {
           bool przedramieR = (XYToDegrees(rightHand.Position, rightElbow.Position) > 290 &&
                XYToDegrees(rightHand.Position, rightElbow.Position) <= 355);
            bool bicepsR = XYToDegrees(rightElbow.Position, rightShoulder.Position) > 290 &&
                XYToDegrees(rightElbow.Position, rightShoulder.Position) < 355;
            bool przedramieL = XYToDegrees(leftHand.Position, leftElbow.Position) > 310 &&
                XYToDegrees(leftHand.Position, leftElbow.Position) < 360;
            bool bicepsL = XYToDegrees(leftElbow.Position, leftShoulder.Position) > 170 &&
                XYToDegrees(leftElbow.Position, leftShoulder.Position) < 280;
            
            if(przedramieL && przedramieR && bicepsL && bicepsR)
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"F:\Pro\KinectTracking\wow.wav");
                player.Play();
                rightDabFound = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool checkingForLeftDab()
        {
            bool przedramieR = (XYToDegrees(rightHand.Position, rightElbow.Position) > 160 &&
                XYToDegrees(rightHand.Position, rightElbow.Position) <= 220);
            bool bicepsR = XYToDegrees(rightElbow.Position, rightShoulder.Position) > 0 &&
                XYToDegrees(rightElbow.Position, rightShoulder.Position) < 100;
            bool przedramieL = XYToDegrees(leftHand.Position, leftElbow.Position) > 160 &&
                XYToDegrees(leftHand.Position, leftElbow.Position) < 255;
            bool bicepsL = XYToDegrees(leftElbow.Position, leftShoulder.Position) > 150 &&
                XYToDegrees(leftElbow.Position, leftShoulder.Position) < 250;
            if (przedramieL && przedramieR && bicepsL && bicepsR)
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"F:\Pro\KinectTracking\wow.wav");
                player.Play();
                leftDabFound = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private float XYToDegrees(Microsoft.Kinect.SkeletonPoint p, Microsoft.Kinect.SkeletonPoint origin)
        { 
            double deltaX = origin.X - p.X;
            double deltaY = (origin.Y - p.Y);

            double radAngle = Math.Atan2(deltaY, deltaX);
            double degreeAngle = radAngle * 180.0 / Math.PI;

            return (float)(180.0 - degreeAngle);
        }
    }
}
