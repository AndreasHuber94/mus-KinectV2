using Microsoft.Kinect;
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
using WindowsInput;

namespace Kinect4ddr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mode _mode = Mode.Depth;
        bool _displayBody = true;

        bool _customBaseSet = false;
        CameraSpacePoint _flexBase;
        CameraSpacePoint _customBase;
        Vector4 _floor;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;

        // current stances
        bool _isUp;
        bool _isLeft;
        bool _isDown;
        bool _isRight;

        // threshholds
        double th_lr = 0.2;
        double th_up = 0.3;
        double th_down = -0.1;
        double th_high = 0.1;

        bool _useEnhancedMode = true;
        bool _useAnkles = false;

       InputSimulator inputSim;


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
            _isLeft = false;
            _isRight = false;
            _isUp = false;
            _isRight = false;

            tbThBack.Text = th_down.ToString();
            tbThFront.Text = th_up.ToString();
            tbThLeftRight.Text = th_lr.ToString();
            tbThTab.Text = th_high.ToString();

            inputSim = new InputSimulator();
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
            Double.TryParse(tbThTab.Text, out th_high);
            Double.TryParse(tbThLeftRight.Text, out th_lr);
            Double.TryParse(tbThFront.Text, out th_up);
            Double.TryParse(tbThBack.Text, out th_down);


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

                    _floor = frame.FloorClipPlane;

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
                                //DisplayInfo(body);
                                if (_useEnhancedMode)
                                {
                                    DisplayControlsV2(body);
                                }
                                else
                                {
                                    DisplayControls(body);
                                }
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

        //private void DisplayInfo(Body body)
        //{
        //    tbCenterBottom.Text = body.Joints[JointType.SpineBase].ToPositionString();
        //    tbCenterHead.Text = body.Joints[JointType.SpineShoulder].ToPositionString();
        //    tbLeftFoot.Text = body.Joints[JointType.AnkleLeft].ToPositionString();
        //    tbRightFoot.Text = body.Joints[JointType.AnkleRight].ToPositionString();
        //}

        private void DisplayControls(Body body)
        {
            _flexBase = body.Joints[JointType.SpineBase].Position;

            var jbase = _customBaseSet ? _customBase : _flexBase;
            var jleft = body.Joints[JointType.AnkleLeft].Position;
            var jright = body.Joints[JointType.AnkleRight].Position;

            // left field
            if((jbase.X - jleft.X) > th_lr)
            {
                if (!_isLeft)
                {
                    _isLeft = true;
                    tbLeft.Background = Brushes.Green;
                    inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LEFT);
                }
            }
            else
            {
                if (_isLeft)
                {
                    _isLeft = false;
                    tbLeft.Background = Brushes.DarkGray;
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.LEFT);
                }
            }

            // right field
            if ((jbase.X - jright.X) < (th_lr * - 1))
            {
                if (!_isRight)
                {
                    _isRight = true;
                    tbRight.Background = Brushes.Green;
                    inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RIGHT);
                }
            }
            else
            {
                if (_isRight)
                {
                    _isRight = false;
                    tbRight.Background = Brushes.DarkGray;
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RIGHT);
                }
            }

            // up field
            if ((jbase.Z - jleft.Z) > th_up
                || (jbase.Z - jright.Z) > th_up)
            {
                if (!_isUp)
                {
                    _isUp = true;
                    tbUp.Background = Brushes.Green;
                    inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.UP);
                }
            }
            else
            {
                if (_isUp)
                {
                    _isUp = false;
                    tbUp.Background = Brushes.DarkGray;
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.UP);
                }
            }

            // down field
            if ((jbase.Z - jleft.Z) < th_down
                || (jbase.Z - jright.Z) < th_down)
            {
                if (!_isDown)
                {
                    _isDown = true;
                    tbDown.Background = Brushes.Green;
                    inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                }
            }
            else
            {
                if (_isDown)
                {
                    _isDown = false;
                    tbDown.Background = Brushes.DarkGray;
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                }
            }

        }

        private void DisplayControlsV2(Body body)
        {
            _flexBase = body.Joints[JointType.SpineBase].Position;

            var jbase = _customBaseSet ? _customBase : _flexBase;
            var jleft = _useAnkles ? body.Joints[JointType.AnkleLeft].Position : body.Joints[JointType.FootLeft].Position;
            var jright = _useAnkles ? body.Joints[JointType.AnkleRight].Position : body.Joints[JointType.FootRight].Position;

            //TbPseudoConsole.Text = "height = " + DistanceFrom(jleft);

            // left field
            if((jbase.X - jleft.X) > th_lr)
            {  
                if(DistanceFrom(jleft) < th_high)
                {
                    tbLeft.Background = Brushes.Yellow;
                    if (!_isLeft)
                    {
                        _isLeft = true;
                        inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LEFT);
                    }
                }
                else
                {
                    tbLeft.Background = Brushes.Green;
                    if (_isLeft)
                    {
                        _isLeft = false;
                        inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.LEFT);
                    }
                }
            }
            else
            {
                if (_isLeft)
                {
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.LEFT);
                }
                _isLeft = false;
                tbLeft.Background = Brushes.DarkGray;
            }


            // right field
            if ((jbase.X - jright.X) < (th_lr * - 1))
            {
                //send button down
                if (DistanceFrom(jright) < th_high)
                {
                    tbRight.Background = Brushes.Yellow;
                    if (!_isRight)
                    {
                        _isRight = true;
                        inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RIGHT);
                    }
                }
                else
                {
                    tbRight.Background = Brushes.Green;
                    if (_isRight)
                    {
                        _isRight = false;
                        inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RIGHT);
                    }
                }
            }
            else
            {
                if (_isRight)
                {
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RIGHT);
                }
                _isRight = false;
                tbRight.Background = Brushes.DarkGray;
            }

            // up field
            if ((jbase.Z - jleft.Z) > th_up
                || (jbase.Z - jright.Z) > th_up)
            {
                if ((jbase.Z - jleft.Z) > th_up && DistanceFrom(jleft) < th_high
                    || (jbase.Z - jright.Z) > th_up && DistanceFrom(jright) <th_high)
                {
                    tbUp.Background = Brushes.Yellow;
                    if (!_isUp)
                    {
                        _isUp = true;
                        inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.UP);
                    }
                }
                else
                {
                    tbUp.Background = Brushes.Green;
                    if (_isUp)
                    {
                        _isUp = false;
                        inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.UP);
                    }
                }
            }
            else
            {
                if (_isUp)
                {
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.UP);
                }
                _isUp = false;
                tbUp.Background = Brushes.DarkGray;
            }

            // down field
            if ((jbase.Z - jleft.Z) < th_down
                || (jbase.Z - jright.Z) < th_down)
            {
                if ((jbase.Z - jleft.Z) < th_down && DistanceFrom(jleft) < th_high
                    || (jbase.Z - jright.Z) < th_down && DistanceFrom(jright) < th_high)
                {
                    tbDown.Background = Brushes.Yellow;
                    if (!_isDown)
                    {
                        _isDown = true;
                        inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                    }
                }
                else
                {
                    tbDown.Background = Brushes.Green;
                    if (_isDown)
                    {
                        _isDown = false;
                        inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                    }
                }
            }
            else
            {
                if (_isDown)
                {
                    inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                }
                _isDown = false;
                tbDown.Background = Brushes.DarkGray;
            }

        }


        public double DistanceFrom(CameraSpacePoint point)
        {
            double numerator = _floor.X * point.X + _floor.Y * point.Y + _floor.Z * point.Z + _floor.W;
            double denominator = Math.Sqrt(_floor.X * _floor.X + _floor.Y * _floor.Y + _floor.Z * _floor.Z);

            return numerator / denominator;
        }

        private void BtnSetBase_Click(object sender, RoutedEventArgs e)
        {
            _customBase = _flexBase;
            _customBaseSet = true;
            BtnRemoveBase.IsEnabled = true;
            BtnSetBase.IsEnabled = false;
        }

        private void BtnRemoveBase_Click(object sender, RoutedEventArgs e)
        {
            _customBaseSet = false;
            BtnRemoveBase.IsEnabled = false;
            BtnSetBase.IsEnabled = true;
        }

        private void BtnEnhanced_Click(object sender, RoutedEventArgs e)
        {
            _useEnhancedMode = true;
            BtnEnhanced.IsEnabled = false;
            BtnBasic.IsEnabled = true;
        }

        private void BtnBasic_Click(object sender, RoutedEventArgs e)
        {
            _useEnhancedMode = false;
            BtnEnhanced.IsEnabled = true;
            BtnBasic.IsEnabled = false;
        }

        private void BtnAnkle_Click(object sender, RoutedEventArgs e)
        {
            _useAnkles = true;
            BtnAnkle.IsEnabled = false;
            BtnFoot.IsEnabled = true;
        }

        private void BtnFoot_Click(object sender, RoutedEventArgs e)
        {
            _useAnkles = false;
            BtnFoot.IsEnabled = false;
            BtnAnkle.IsEnabled = true;
        }
    }
}
