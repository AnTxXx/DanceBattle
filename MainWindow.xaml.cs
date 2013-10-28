//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Timers;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;
        System.Random random = new System.Random();
        private bool gestureDoneP1 = false;
        private bool gestureDoneP2 = false;
        private int pointsP1 = 6;
        private int pointsP2 = 6;
        private string actualGesture = "";
        private bool timerStarted = false;
        private bool gameRunning = false;
        private int round = 0;

        Skeleton player1 = null;
        Skeleton player2 = null;
        //System.Media.SoundPlayer sound = null;
        MediaPlayer m = new MediaPlayer();

        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer displayLimitedTimer;
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.out_main.Source = loadPix("title");
            m.SpeedRatio = 1.2;
            this.out_frame.Source = loadPix("stage");
            this.out_health1.Source = loadPix("pl1_h_6");
            this.out_health2.Source = loadPix("pl2_h_6");
            this.out_p1visible.Source = loadPix("pl_1");
            this.out_p2visible.Source = loadPix("pl_2");
            m.Open(new System.Uri(System.Environment.CurrentDirectory + @"\..\..\music\dance_battle.wav"));
           
            m.MediaEnded += new EventHandler(Media_Ended);
            m.Play();
            //sound = new System.Media.SoundPlayer(@"..\..\music\dance_battle.wav");
            //sound.Play();

            

            
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
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
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
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

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
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

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void Media_Ended(object sender, EventArgs e)
        {
            m.Position = TimeSpan.Zero;
            m.Play();
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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

            //Unser Code
            if (skeletons.Length != 0)
            {
                if (!timerStarted)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            if (player1 == null)
                            {
                                player1 = skel;
                                this.out_p1visible.Visibility = System.Windows.Visibility.Visible;
                                Debug.WriteLine("Player1 found");
                            }
                            if (player1 != null && player2 == null && player1.TrackingId != skel.TrackingId)
                            {
                                player2 = skel;
                                this.out_p2visible.Visibility = System.Windows.Visibility.Visible;
                                Debug.WriteLine("Player2 found, starting game ...");
                                if (!timerStarted)
                                {
                                    aTimer = new System.Timers.Timer(60000);
                                    aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
                                    aTimer.Interval = 2000;
                                    aTimer.Enabled = true;
                                    timerStarted = true;
                                    gameRunning = true;
                                    this.out_main.Source = loadPix("countdown_dance");
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (player1.TrackingId == skel.TrackingId) player1 = skel;
                        if (player2.TrackingId == skel.TrackingId) player2 = skel;
                    }
                }
                if (player1 == null) this.out_p1visible.Visibility = System.Windows.Visibility.Hidden;
                if (player2 == null) this.out_p2visible.Visibility = System.Windows.Visibility.Hidden;

                if(player1!=null & player2!=null) Debug.WriteLine(player1.Position.X + " - " + player1.Position.Y);
                    if (timerStarted && !gestureDoneP1 && actualGesture == this.identifyGesture(player1))
                    {  
                        gestureDoneP1 = true;
                    }
                
                    if (timerStarted && !gestureDoneP2 && actualGesture == this.identifyGesture(player2))
                    {
                        gestureDoneP2 = true;
                    }
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e){

            if (gameRunning)
            {

                this.actualGesture = randomGesture();


                if (round == 0)
                {
                    gestureDoneP1 = true;
                    gestureDoneP2 = true;
                }
                else if (round % 5 == 0)
                {
                    Dispatcher.BeginInvoke(new Action(() => m.SpeedRatio = m.SpeedRatio + 0.07));
                    //Intervall darf nicht 0 werden
                    Dispatcher.BeginInvoke(new Action(() => aTimer.Interval = (Math.Abs(aTimer.Interval - 200)) + 1));
                    Dispatcher.BeginInvoke(new Action(() => this.out_bloody.Source = loadPix("speed_up")));
                }
                else if (round % 5 == 1)
                {
                    Dispatcher.BeginInvoke(new Action(() => this.out_bloody.Source = null));
                }


                if (!gestureDoneP1)
                {
                    pointsP1--;
                    if (pointsP1 >= 0)
                    {
                        Dispatcher.BeginInvoke(new Action(() => this.out_health1.Source = loadPix("pl1_h_" + pointsP1)));
                    }


                }
                if (!gestureDoneP2)
                {
                    pointsP2--;
                    if (pointsP2 >= 0)
                    {
                        Dispatcher.BeginInvoke(new Action(() => this.out_health2.Source = loadPix("pl2_h_" + pointsP2)));
                    }

                }


                if (pointsP1 == 0 && pointsP2 == 0)
                {
                    //unentschieden

                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("win_nobody")));
                    gameRunning = false;
                }
                else if (pointsP1 == 0)
                {
                    //spieler 1 verloren
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("win_pl2")));
                    gameRunning = false;

                }
                else if (pointsP2 == 0)
                {
                    //spieler 2 verloren
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("win_pl1")));
                    gameRunning = false;
                }

                this.gestureDoneP1 = false;
                this.gestureDoneP2 = false;
                this.round++;
            }
            else{ //game not running

                if (this.identifyGesture(player1) == "NEW" && this.identifyGesture(player2) == "NEW")
                {
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("countdown_dance")));
                    Dispatcher.BeginInvoke(new Action(() => this.out_health1.Source = loadPix("pl1_h_6")));
                    Dispatcher.BeginInvoke(new Action(() => this.out_health2.Source = loadPix("pl2_h_6")));
                    Dispatcher.BeginInvoke(new Action(() => aTimer.Interval = 2000));
                    pointsP2 = 6;
                    pointsP1 = 6;
                    Dispatcher.BeginInvoke(new Action(() => m.SpeedRatio = 1.2));
                    round = 0;
                    gameRunning = true;
                }
            }
        }


        private string randomGesture()
        {
            int randomNumber = random.Next(0, 4);
            switch (randomNumber)
            {
                case 0:
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("dir_up")));
                    return "UP";
                case 1:
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("dir_down")));
                    return "DOWN";
                case 2:
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("dir_left")));
                    return "LEFT";
                case 3:
                    Dispatcher.BeginInvoke(new Action(() => this.out_main.Source = loadPix("dir_right")));
                    return "RIGHT";
                default:
                    return "ASDF";
            }
        }




        private string identifyGesture(Skeleton inpSkeleton)
        {
            if (inpSkeleton.Joints[JointType.HandLeft].Position.Y > inpSkeleton.Joints[JointType.Head].Position.Y
               && inpSkeleton.Joints[JointType.HandRight].Position.Y > inpSkeleton.Joints[JointType.Head].Position.Y)
            {
                return "UP";
            }
            else if(inpSkeleton.Joints[JointType.WristLeft].Position.Y < (inpSkeleton.Joints[JointType.HipLeft].Position.Y)
               && inpSkeleton.Joints[JointType.WristRight].Position.Y < (inpSkeleton.Joints[JointType.HipRight].Position.Y))
            {

                return "DOWN";
            } 
            else if(inpSkeleton.Joints[JointType.HandLeft].Position.X < inpSkeleton.Joints[JointType.ShoulderLeft].Position.X
               && inpSkeleton.Joints[JointType.HandRight].Position.X < inpSkeleton.Joints[JointType.ShoulderLeft].Position.X)
            {
                return "LEFT";
            }
            else if (inpSkeleton.Joints[JointType.HandLeft].Position.X > inpSkeleton.Joints[JointType.ShoulderRight].Position.X
             && inpSkeleton.Joints[JointType.HandRight].Position.X > inpSkeleton.Joints[JointType.ShoulderRight].Position.X)
            {
                return "RIGHT";
            }
            else if ((inpSkeleton.Joints[JointType.HandRight].Position.Y > inpSkeleton.Joints[JointType.Head].Position.Y &&
                     inpSkeleton.Joints[JointType.HandLeft].Position.Y > inpSkeleton.Joints[JointType.KneeLeft].Position.Y &&
                     inpSkeleton.Joints[JointType.HandLeft].Position.Y < inpSkeleton.Joints[JointType.ShoulderLeft].Position.Y) ||
                     (inpSkeleton.Joints[JointType.HandLeft].Position.Y > inpSkeleton.Joints[JointType.Head].Position.Y &&
                     inpSkeleton.Joints[JointType.HandRight].Position.Y > inpSkeleton.Joints[JointType.KneeRight].Position.Y &&
                     inpSkeleton.Joints[JointType.HandRight].Position.Y < inpSkeleton.Joints[JointType.ShoulderRight].Position.Y)
                    )
            {
                return "NEW";
            }
            else
            {
                return "NONE";
            }
        }
        

        

        private BitmapImage loadPix(string inpName){
                
                return new BitmapImage(new System.Uri(System.Environment.CurrentDirectory+@"\..\..\Images\"+inpName+".png"));
           
        }
    }
}