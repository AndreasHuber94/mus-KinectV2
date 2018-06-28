﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Kinect4ddr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mode _mode = Mode.Depth;
        bool _displayBody = true;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = StartKinect();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }

            ControlGrid.Background = Brushes.LightGray;
            tbUp.Background = Brushes.DarkGray;
            tbDown.Background = Brushes.DarkGray;
            tbLeft.Background = Brushes.DarkGray;
            tbRight.Background = Brushes.DarkGray;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                // Draw skeleton.
                                if (_displayBody)
                                {
                                    canvas.DrawSkeleton(body);
                                }
                                DisplayInfo(body);
                                DisplayControls(body);
                            }
                        }
                    }
                }
            }
        }

        private KinectSensor StartKinect()
        {
            _sensor = KinectSensor.GetDefault();
            _sensor.Open();
            TbPseudoConsole.Text = "started Kinect";
            TbKinectStatus.Text = "ONLINE";
            TbKinectStatus.Background = Brushes.Green;
            return _sensor;
        }

        public enum Mode
        {
            Color,
            Depth,
            Infrared
        }

        private void DisplayInfo(Body body)
        {
            tbCenterBottom.Text = body.Joints[JointType.SpineBase].ToPositionString();
            tbCenterHead.Text = body.Joints[JointType.SpineShoulder].ToPositionString();
            tbLeftFoot.Text = body.Joints[JointType.AnkleLeft].ToPositionString();
            tbRightFoot.Text = body.Joints[JointType.AnkleRight].ToPositionString();
        }

        private void DisplayControls(Body body)
        {
            ControlGrid.Background = Brushes.LightGray;
            tbUp.Background = Brushes.DarkGray;
            tbDown.Background = Brushes.DarkGray;
            tbLeft.Background = Brushes.DarkGray;
            tbRight.Background = Brushes.DarkGray;

            float th_lr = 0.2f;
            float th_up = 0.3f;
            float th_down = -0.1f;

            var jbase = body.Joints[JointType.SpineBase].Position;
            var jleft = body.Joints[JointType.AnkleLeft].Position;
            var jright = body.Joints[JointType.AnkleRight].Position;

            if((jbase.X - jleft.X) > th_lr)
            {
                tbLeft.Background = Brushes.Green;
            }
            if ((jbase.X - jright.X) < (th_lr * - 1))
            {
                tbRight.Background = Brushes.Green;
            }
            if ((jbase.Z - jleft.Z) > th_up
                || (jbase.Z - jright.Z) > th_up)
            {
                tbUp.Background = Brushes.Green;
            }
            if ((jbase.Z - jleft.Z) < th_down
                || (jbase.Z - jright.Z) < th_down)
            {
                tbDown.Background = Brushes.Green;
            }
        }
    }
}