using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinHarpe2.Laser2Midi;
using KinHarpe2.ViewModels;
using Microsoft.Kinect;
using Sanford.Multimedia.Midi;

namespace KinHarpe2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinHarpeMainViewModel currentViewModel = null;
        public MainWindow()
        {
            InitializeComponent();
            currentViewModel=new KinHarpeMainViewModel();
            this.DataContext = currentViewModel;
        }

        private OutputDevice midiDevice = null;

        private  async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await InitMidi();
            await InitKinHarpe();
        }

        private LaserToMidiAdapter leftHandAdapter;
        private LaserToMidiAdapter rightHandAdapter;

        #region KINECT event and data management
        private KinectSensor kinectSensor = null;
        private MultiSourceFrameReader multiSourceFrameReader = null;

        private const int MapDepthToByte = 8000 / 256;
        private byte[] depthPixels = null;


        async Task InitMidi()
        {
            if (OutputDevice.DeviceCount < 1)
            {
                Debug.WriteLine("NO MIDI DEVICE FOUND !!!! NO SOUND WILL BE PLAYED !!!!");
            }
            else
            {
                midiDevice = new OutputDevice(0);
            }
            leftHandAdapter = new LaserToMidiAdapter(midiDevice, 0); // left hand -> play on midi channel 0
            rightHandAdapter = new LaserToMidiAdapter(midiDevice, 1); // right hand -> play on midi channel 1
        }

        async Task InitKinHarpe()
        {
            sbHideLaser = FindResource("sbHideLaser") as Storyboard;
            sbShowLaser = FindResource("sbShowLaser") as Storyboard;

            this.kinectSensor = KinectSensor.GetDefault();
            this.currentViewModel.Status = this.kinectSensor.IsAvailable ? "KINECT found" : "NO KINECT !!";
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // get a multi frame source reader and bind event
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color /*| FrameSourceTypes.Depth*/ | FrameSourceTypes.Body);
            this.multiSourceFrameReader.MultiSourceFrameArrived += multiSourceFrameReader_MultiSourceFrameArrived;

            // COLOR create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.currentViewModel.VideoImageSource = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // DEPTH
            this.depthPixels = new byte[this.kinectSensor.DepthFrameSource.FrameDescription.Width * this.kinectSensor.DepthFrameSource.FrameDescription.Height];
            this.currentViewModel.DepthImageSource = new WriteableBitmap(this.kinectSensor.DepthFrameSource.FrameDescription.Width, this.kinectSensor.DepthFrameSource.FrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // open the sensor
            this.kinectSensor.Open();
        }

        void multiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multisourceFrame = e.FrameReference.AcquireFrame();

            if (multisourceFrame == null)
                return;

            using (ColorFrame colorFrame = multisourceFrame.ColorFrameReference.AcquireFrame())
            {
                HandleColorFrame(colorFrame);
            }

            using (DepthFrame depthFrame = multisourceFrame.DepthFrameReference.AcquireFrame())
            {
                HandleDepthFrame(depthFrame);
            }

            using (BodyFrame bodyFrame = multisourceFrame.BodyFrameReference.AcquireFrame())
            {
                HandleBodyFrame(bodyFrame);
            }
        }

     

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.currentViewModel.Status = this.kinectSensor.IsAvailable ? "KINECT found" : "NO KINECT !!";
        }

        #region DEPTH bitmap handling
        void HandleDepthFrame(DepthFrame depthFrame)
        {
            if (depthFrame == null)
            {
                this.currentViewModel.DepthImageOpacity = 0;
                return;
            }
            this.currentViewModel.DepthImageOpacity = 0.8;

            bool depthFrameProcessed = false;
            // the fastest way to process the body index data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
            {
               

                // verify data and write the color data to the display bitmap
                if (((this.kinectSensor.DepthFrameSource.FrameDescription.Width * this.kinectSensor.DepthFrameSource.FrameDescription.Height) == (depthBuffer.Size / this.kinectSensor.DepthFrameSource.FrameDescription.BytesPerPixel)) &&
                    (this.kinectSensor.DepthFrameSource.FrameDescription.Width == this.currentViewModel.DepthImageSource.PixelWidth) && (this.kinectSensor.DepthFrameSource.FrameDescription.Height == this.currentViewModel.DepthImageSource.PixelHeight))
                {
                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                    // we are setting maxDepth to the extreme potential depth threshold
                    ushort maxDepth = ushort.MaxValue;

                    // If you wish to filter by reliable depth distance, uncomment the following line:
                    //// maxDepth = depthFrame.DepthMaxReliableDistance

                    this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                    depthFrameProcessed = true;
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }


        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.kinectSensor.DepthFrameSource.FrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.currentViewModel.DepthImageSource.WritePixels(
                new Int32Rect(0, 0, this.currentViewModel.DepthImageSource.PixelWidth, this.currentViewModel.DepthImageSource.PixelHeight),
                this.depthPixels,
                this.currentViewModel.DepthImageSource.PixelWidth,
                0);
        }

        #endregion


        #region COLOR frame handling
        private void HandleColorFrame(ColorFrame colorFrame)
        {
            if (colorFrame == null)
                return;
               
            FrameDescription colorFrameDescription = colorFrame.FrameDescription;
            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
            {
                this.currentViewModel.VideoImageSource.Lock();
                if ((colorFrameDescription.Width == this.currentViewModel.VideoImageSource.PixelWidth) && 
                    (colorFrameDescription.Height == this.currentViewModel.VideoImageSource.PixelHeight))
                {
                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        this.currentViewModel.VideoImageSource.BackBuffer,
                        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                        ColorImageFormat.Bgra);

                    this.currentViewModel.VideoImageSource.AddDirtyRect(
                        new Int32Rect(0, 0, this.currentViewModel.VideoImageSource.PixelWidth, this.currentViewModel.VideoImageSource.PixelHeight));
                }
                this.currentViewModel.VideoImageSource.Unlock();
            }
                
        }

        #endregion


        #region LASER management
        Storyboard sbHideLaser = null;
        Storyboard sbShowLaser = null;
        private bool laserVisible = false;
        void ShowLaser()
        {
            if (laserVisible)
                return;
            laserVisible = true;
            sbShowLaser.Begin();
        }

        void HideLaser()
        {
            if (!laserVisible)
                return;
            laserVisible = false;
            sbHideLaser.Begin();
        }


        private void PlayLaserUnderLeftHand(double x, double y,HandState handState)
        {
            var point = new Point(x, y);
            FrameworkElement touched = canvLaser.InputHitTest(point) as FrameworkElement;
            if (touched == null) // no laser under lefthand -> we stop note 
            {
                leftHandAdapter.ReleaseLaser();
                return;
            }
            LaserRay selectedLaser = touched.TemplatedParent as LaserRay;
            leftHandAdapter.PlayLaser(selectedLaser.Tag.ToString());
        }

        private void PlayLaserUnderRightHand(double x, double y, HandState handState)
        {
            var point = new Point(x, y);
            FrameworkElement touched = canvLaser.InputHitTest(point) as FrameworkElement;
            if (touched == null) // no laser under right hand -> we stop note 
            {
                rightHandAdapter.ReleaseLaser();
                return;
            }
            LaserRay selectedLaser = touched.TemplatedParent as LaserRay;
            rightHandAdapter.PlayLaser(selectedLaser.Tag.ToString());

        }


        #endregion

        #region BODY frame handling
        SolidColorBrush openedHandColor = new SolidColorBrush(Colors.GreenYellow);
        SolidColorBrush closedHandColor = new SolidColorBrush(Colors.PaleVioletRed);
        SolidColorBrush unusableHandColor = new SolidColorBrush(Colors.Transparent);

        private Body[] bodies = null;
        private ColorSpacePoint posMappedLeft;
        private ColorSpacePoint posMappedRight;
        private void HandleBodyFrame(BodyFrame bodyFrame)
        {
            if (bodyFrame == null)
                return;
            if (bodies == null)
            {
                bodies=new Body[bodyFrame.BodyCount];
            }
            bodyFrame.GetAndRefreshBodyData(bodies);

            var jmjBody = bodies.FirstOrDefault(b => b.IsTracked); // we get the first tracker body 
            if (jmjBody == null)
            {
                HideLaser();
                this.currentViewModel.Status = "NO BODY";
                return;
            }
            ShowLaser();

            posMappedLeft = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(jmjBody.Joints[JointType.HandLeft].Position);
            this.currentViewModel.LeftHandX = posMappedLeft.X;
            this.currentViewModel.LeftHandY = posMappedLeft.Y;

            posMappedRight = this.kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(jmjBody.Joints[JointType.HandRight].Position);
            this.currentViewModel.RightHandX = posMappedRight.X;
            this.currentViewModel.RightHandY = posMappedRight.Y;

            
            switch (jmjBody.HandLeftState)
            {
                case HandState.Open:
                    this.currentViewModel.LeftHandColor = openedHandColor;
                    break;
                case HandState.Closed:
                    this.currentViewModel.LeftHandColor = closedHandColor;
                    break;
                default:
                    this.currentViewModel.LeftHandColor = unusableHandColor;
                    break;

            }
            switch (jmjBody.HandRightState)
            {
                case HandState.Open:
                    this.currentViewModel.RightHandColor = openedHandColor;
                    break;
                case HandState.Closed:
                    this.currentViewModel.RightHandColor = closedHandColor;
                    break;
                default:
                    this.currentViewModel.RightHandColor = unusableHandColor;
                    break;

            }

            this.currentViewModel.LeftHandStatus = jmjBody.HandLeftState.ToString();
            this.currentViewModel.RightHandStatus = jmjBody.HandRightState.ToString();

            if (jmjBody.Joints[JointType.ShoulderLeft].Position.Z - jmjBody.Joints[JointType.HandLeft].Position.Z > 0.40)
            {
                PlayLaserUnderLeftHand(posMappedLeft.X,posMappedLeft.Y,jmjBody.HandLeftState);
                this.currentViewModel.LeftHandTorsoDistance = "PLAY";
            }
            else
            {
                leftHandAdapter.ReleaseLaser();
                this.currentViewModel.LeftHandTorsoDistance = "";
            }
            if (jmjBody.Joints[JointType.ShoulderRight].Position.Z - jmjBody.Joints[JointType.HandRight].Position.Z > 0.40)
            {
                PlayLaserUnderRightHand(posMappedRight.X,posMappedRight.Y,jmjBody.HandRightState);
                this.currentViewModel.RightHandTorsoDistance = "PLAY";
            }
            else
            {
                rightHandAdapter.ReleaseLaser();
                this.currentViewModel.RightHandTorsoDistance = "";
            }
        }
        #endregion

      
        #endregion

      
    }
}
