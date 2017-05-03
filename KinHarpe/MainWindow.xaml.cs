using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Diagnostics;
using Sanford.Multimedia.Midi;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;

using Microsoft.Kinect;


namespace KinHarpe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //Runtime kinectRuntime = null;
        
        KinectSensor sensor = null;



        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

      /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Bitmap that will hold DEPTH information
        /// </summary>
        private WriteableBitmap depthBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private short[] depthPixels;

  



        //BitmapSource colorImage = null;
        //BitmapSource depthImage = null;
        int nbFrameWithoutSkeleton = 0;

        List<LaserRay> lasers = new List<LaserRay>();

        Dictionary<string, int> laserToMidiTranslation = new Dictionary<string, int>();
        OutputDevice midiSynth = null;
        int midiChannel = 0;

        Storyboard sbHideLaser = null;
        Storyboard sbShowLaser = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                sbHideLaser = FindResource("sbHideLaser") as Storyboard;
                sbShowLaser = FindResource("sbShowLaser") as Storyboard;

                sbHideLaser.Begin();

                if (OutputDevice.DeviceCount < 1)
                {
                    MessageBox.Show("No midi output device", "No midi", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Close();
                }
                midiSynth = new OutputDevice(0);    // on prend le premier périphérique de sortie Midi

                foreach (var r in Enum.GetValues(typeof(Sanford.Multimedia.Midi.GeneralMidiInstrument)))
                {
                    cbxInstrument.Items.Add(r);
                }
                cbxInstrument.SelectedItem = GeneralMidiInstrument.ChoirAahs;

                lasers.Add(laserA);
                lasers.Add(laserB);
                lasers.Add(laserC);
                lasers.Add(laserD);
                lasers.Add(laserE);
                lasers.Add(laserF);
                lasers.Add(laserG);
                lasers.Add(laserH);
                lasers.Add(laserI);

                laserToMidiTranslation.Add("A", 60);
                laserToMidiTranslation.Add("B", 62);
                laserToMidiTranslation.Add("C", 64);
                laserToMidiTranslation.Add("D", 66);
                laserToMidiTranslation.Add("E", 68);
                laserToMidiTranslation.Add("F", 70);
                laserToMidiTranslation.Add("G", 72);
                laserToMidiTranslation.Add("H", 74);
                laserToMidiTranslation.Add("I", 76);

                try
                {
                    sensor = (from k in KinectSensor.KinectSensors where k.Status == KinectStatus.Connected select k).FirstOrDefault();
                    if (sensor == null)
                    {
                        MessageBox.Show("Il n'y a pas de kinect connecté sur cette machine", "Pas de kinect", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                    this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                    this.webcamImage.Source = this.colorBitmap;
                    sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);


                    
                    sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    this.depthPixels = new short[this.sensor.DepthStream.FramePixelDataLength];
                    this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray8, null);
                    this.depthcamImage.Source = this.depthBitmap;
                    sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);


                    //var smoothparameters = new TransformSmoothParameters
                    //{
                    //    Smoothing = 1.0f,
                    //    Correction = 0.1f,
                    //    Prediction = 0.1f,
                    //    JitterRadius = 0.05f,
                    //    MaxDeviationRadius = 0.05f
                    //};

                    sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectRuntime_SkeletonFrameReady);
                    //sensor.SkeletonStream.Enable(smoothparameters);
                    sensor.SkeletonStream.Enable();


                    sensor.Start();
                }
                catch (COMException cex)
                {
                    MessageBox.Show("Erreur d'initialisation du Kinect\r\n\r\nEst ce que votre kinect est branché ? et que les drivers sont bien installés ?", "Kinect indisponible",MessageBoxButton.OK,MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Erreur d'initialization KinHarpe");
            }
        }

        void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame skeletonFrame = e.OpenSkeletonFrame();
            if (skeletonFrame == null)
            {
                debugValue.Text = "Skeleton not tracked";
                return;
            }

            Skeleton[] rawskels = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(rawskels);

            var trackedSkels = rawskels.Where((s) => s.TrackingState != SkeletonTrackingState.NotTracked).Select( (s)=>s).ToList();


            if (trackedSkels.Count==0)
            {
                nbFrameWithoutSkeleton++;
                if (nbFrameWithoutSkeleton == 30)
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        sbHideLaser.Begin();
                    }));
                return;
            }

            //SkeletonData data = (from s in skeletonFrame.Skeletons
            //                     where s.TrackingState == SkeletonTrackingState.Tracked
            //                     select s).FirstOrDefault();    // on ne traite que le 1er skelete détecté
            //if (data == null)
            //{
            //    debugValue.Text = "Skeleton not tracked";
            //    return;
            //}

            if (nbFrameWithoutSkeleton != 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    sbShowLaser.Begin();
                }));
            }

            nbFrameWithoutSkeleton = 0;
            if (laserHidden)
            {
                laserHidden = false;
                sbShowLaser.Begin();
                //if (canvLaser.Visibility != Visibility.Visible)
                //    canvLaser.Visibility = Visibility.Visible;
            }

            //var neck = data.Joints[JointID.ShoulderCenter];
            var neck = trackedSkels[0].Joints[JointType.ShoulderCenter];
            MovetoJointPosition(cou, neck);

            //var leftHandJoint = data.Joints[JointID.HandLeft];
            var leftHandJoint = trackedSkels[0].Joints[JointType.HandLeft];
            MovetoJointPosition(leftHand, leftHandJoint);

            if (neck.Position.Z - leftHandJoint.Position.Z > 0.30f) // si la main est au moins a 36cm en avant des épaules.
            {
                leftHand.Fill = Brushes.White;
                LaserRay oldleftLaser = leftHand.Tag as LaserRay;
                var newleftlaser = FindLaserUnder(leftHand);
                if (newleftlaser != oldleftLaser)
                {
                    if (oldleftLaser != null)
                    {
                        StopNoteFor((string)oldleftLaser.Tag);
                    }
                    if (newleftlaser != null)
                        PlayMidiNoteFor((string)newleftlaser.Tag);
                }
            }
            else
            {
                LaserRay leftLaser = leftHand.Tag as LaserRay;
                if (leftLaser != null)
                {
                    StopNoteFor((string)leftLaser.Tag);
                    leftLaser.Untouched();
                }
                leftHand.Fill = Brushes.Red;
            }

            //var rightHandJoint = data.Joints[JointID.HandRight];
            var rightHandJoint = trackedSkels[0].Joints[JointType.HandRight];
            MovetoJointPosition(rightHand, rightHandJoint);
            if (neck.Position.Z - rightHandJoint.Position.Z > 0.30f) // si la main est au moins a 36cm en avant des épaules
            {
                rightHand.Fill = Brushes.White;
                LaserRay oldrightLaser = rightHand.Tag as LaserRay;
                var newlaser = FindLaserUnder(rightHand);
                if (newlaser != oldrightLaser)
                {
                    if (oldrightLaser != null)
                    {
                        StopNoteFor((string)oldrightLaser.Tag);
                    }
                    if (newlaser != null)
                        PlayMidiNoteFor((string)newlaser.Tag);
                }
            }
            else
            {
                LaserRay rightLaser = rightHand.Tag as LaserRay;
                if (rightLaser != null)
                {
                    StopNoteFor((string)rightLaser.Tag);
                    rightLaser.Untouched();
                }

                rightHand.Fill = Brushes.Red;
            }
        }


        private LaserRay FindLaserUnder(Ellipse hand)
        {
            var point = new Point(Canvas.GetLeft(hand), canvLaser.ActualHeight - Canvas.GetBottom(hand));
            FrameworkElement touched = canvLaser.InputHitTest(point) as FrameworkElement;
            if (touched == null)
                return null;
            LaserRay selectedLaser = touched.TemplatedParent as LaserRay;

            if (hand.Tag!=null && hand.Tag != selectedLaser) // ancien laser sous la main != laser actuelle
            {
                //((LaserRay)hand.Tag).Fill = Brushes.LightGreen;
                ((LaserRay)hand.Tag).Untouched();
                hand.Tag = null;
            }

            if (selectedLaser != null)
            {
                debugValue.Text = selectedLaser.Tag.ToString();
                //selectedLaser.Fill = Brushes.White;
                selectedLaser.Touched(0, 0);
                hand.Tag = selectedLaser;
                
            }
            else
                debugValue.Text = string.Format("NO LASER TOUCHED : {0}",touched.Name) ;
            return selectedLaser;
        }


        private void MovetoJointPosition(FrameworkElement jointView, Joint joint)
        {

            var x = (canvPosition.ActualWidth / 2) + joint.Position.X * (canvPosition.ActualWidth / 2);
            Canvas.SetLeft(jointView, x);
            //debugValue.Text = leftHandJoint.Position.X.ToString();

            var y = (canvPosition.ActualHeight / 2) + joint.Position.Y * (canvPosition.ActualHeight / 2);
            Canvas.SetBottom(jointView, y);
            //debugValue.Text += "  " + leftHandJoint.Position.X.ToString();
        }


     

        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            //using (var depthFrame = e.OpenDepthImageFrame())
            //{
            //    if (depthFrame != null)
            //    {
            //        // Copy the pixel data from the image to a temporary array
            //        depthFrame.CopyPixelDataTo(this.depthPixels);

            //        // Write the pixel data into our bitmap
            //        this.depthBitmap.WritePixels(
            //            new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
            //            this.depthPixels,
            //            this.depthBitmap.PixelWidth * sizeof(int),
            //            0);
            //    }
            //}
        }


        bool laserHidden = false;

        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
          
        }

      
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (midiSynth != null)
            {
                midiSynth.Close();
            }
            if (sensor != null)
            {
                sensor.Stop();
                sensor.DepthFrameReady -= new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);
                sensor.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);
                sensor.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(kinectRuntime_SkeletonFrameReady);
            }
          
        }

        private void PlayMidiNoteFor(string laserNote)
        {
            if (midiSynth == null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOn;
            builder.MidiChannel = midiChannel;
            builder.Data1 = laserToMidiTranslation[laserNote];
            builder.Data2 = 127;
            builder.Build();
            midiSynth.Send(builder.Result);
        }

        private void StopNoteFor(string laserNote)
        {
            if (midiSynth == null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.NoteOff;
            builder.MidiChannel = midiChannel;
            builder.Data1 = laserToMidiTranslation[laserNote];
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }
        private void cbxInstrument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (midiSynth == null) return;
            ChannelMessageBuilder builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.ProgramChange;
            builder.MidiChannel = midiChannel;
            builder.Data1 = Convert.ToInt32(cbxInstrument.SelectedItem); // INSTRUMENT ID
            builder.Data2 = 0;
            builder.Build();
            midiSynth.Send(builder.Result);
        }

    }
}
