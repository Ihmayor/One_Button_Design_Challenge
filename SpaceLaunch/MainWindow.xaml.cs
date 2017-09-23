using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace SpaceLaunch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    //private string NextSound { get; set; }
    //public AutoResetEvent PlayASoundFlag = new AutoResetEvent(false);
    //private Dictionary<string, SoundPlayer> Sounds { get; set; }
    //private object soundLocker = new object();
    //public string SoundPlaying { get; private set; }
    //public bool Stopping { get; set; }

    public partial class MainWindow : Window
    {
        //Carousel Animation
        private Storyboard leave;
        private string[] loadedOption = new string[] { "planet.png", "planet2.png","PlaceholderGundam.png" };
        int currOptInd;

        //Interaction
        private bool firstClick;
        private bool isHeld;

        //Sound
        private Dictionary<string, int> ScaleNotes = new Dictionary<string, int>() { { "A", 440 }, { "B", 494 }, { "C", 524 }, { "D", 587 }, { "E", 659 }, { "F", 698 }, { "G", 784 }, { "A2", 880 }, { "B2", 988 }, { "C2", 1046 } };
        private int currentNoteIndex;
        private string noteSelected;

        private Dictionary<string, float> RecordedPresses = new Dictionary<string, float>() { };

        //Sound Thread + Timing
        private Stopwatch watch;
        private Thread soundThread;
        private SoundPlayer[] ahSound;
        private int streamNum;
        private bool stopPlaying;
        private long savedTick;

        private DispatcherTimer timer;



        public MainWindow()
        {
            InitializeComponent();

            //Load Notes CDEFGABC!<===== Where C! is highest C!
            //Scale keeps going up and up but we only have 4 stages or something.
            //Timer the change in scale. Reperesent timer in flash countdown in middle.
            firstClick = true;
            ahSound = new SoundPlayer[] { new SoundPlayer("a.wav"), new SoundPlayer("a.wav") };
            streamNum = 0;
            currentNoteIndex = 0;
            currOptInd = 0;
            ahSound[streamNum].Load();
            ahSound[ahSound.Length - 1].Load();
            finishedAnim = true;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400);
            timer.Tick += Timer_Tick;
            leave = FindResource("Leave") as Storyboard;
            leave.Completed += LeaveOption_Completed;
            Thread.Sleep(2000);
            NextOption().Wait();
            noteSelected = "D";
            watch = new Stopwatch();

        }


        private void Timer_Tick(object sender, EventArgs e)
        {

        }

        private void PlayTone(string Note)
        {
            int frequency = ScaleNotes[Note];
            stopPlaying = false;
            while (!stopPlaying)
            {
                Console.Beep(frequency, 100000);
            }
        }

        private void StopTone()
        {
            stopPlaying = true;
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri(@"images/ver1button_over.png", UriKind.Relative));
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isHeld)
                Image_MouseUp(sender,new MouseButtonEventArgs(e.MouseDevice, 0,new MouseButton()));
            ((Image)sender).Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
        }

        private int halfNoteCount;
        private int quarterNoteCount;
        private int eighthNoteCount;

        private void ClearNoteCount()
        {
            halfNoteCount = 0;
            quarterNoteCount = 0;
            eighthNoteCount = 0;
        }

        private void CheckNoteCountMatch()
        {
            //Get Current Option
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            watch.Stop();
            long currTick = watch.ElapsedMilliseconds;
            long diff = currTick - savedTick;
            if (diff > 5000)
            {
                CheckNoteCountMatch();
                ClearNoteCount();
            }

            watch.Start();
            savedTick = watch.ElapsedMilliseconds;

            noteSelected = ScaleNotes.ToList<KeyValuePair<string, int>>()[new Random().Next(0, ScaleNotes.Count)].Key;
            ((Image)sender).Source = new BitmapImage(new Uri(@"images/ver1button_down.png", UriKind.Relative));
            isHeld = true;
         
            // PlayTone(noteSelect);
            soundThread = new Thread(new ThreadStart(() => { PlayTone(noteSelected); }));
            soundThread.Start();
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            watch.Stop();
            long currTick = watch.ElapsedMilliseconds;
            long diff = currTick - savedTick;

            if (diff >= 90 && diff <= 150)
            {
                eighthNoteCount++;
                NoteHolder.Text += "e";
            }
            else if(diff >= 360 && diff <= 540)
            {
                quarterNoteCount++;
                NoteHolder.Text += "q";
            }
            else if (diff >= 1200 && diff<=1600 )
            {
                halfNoteCount++;
                NoteHolder.Text += "h";
            }

            Console.WriteLine(NoteHolder.FontFamily.Source);
         //   NoteHolder.Text = ": "+diff;
            savedTick = currTick;

            ((Image)sender).Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
            Console.Beep(1000, 1);
            // soundThread.
            StopTone();
            if (soundThread !=null)
                soundThread.Abort();
            soundThread = null;
            isHeld = false;

            watch.Start();//List for pause.
        }


        private void Button_Click(object sender, MouseButtonEventArgs e)
        {


        }



        private bool finishedAnim;

        public async Task NextOption()
        {
            if (!finishedAnim)
                return;
            //Increase Index and make sure it loops around
            currOptInd++;
            currOptInd %= loadedOption.Length;

            //Set Visible Carousel Option to cover Single Option
            //Reset to original state
            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            CurrentOption.Visibility = Visibility.Hidden;
            Option1.Visibility = Visibility.Visible;
            Option2.Visibility = Visibility.Visible;

            //Load Next Option in second part of carousel
            Option2.Source = new BitmapImage(new Uri(@"/images/" + loadedOption[currOptInd], UriKind.Relative));

            //Trigger Animation
            leave.Begin();
            finishedAnim = false;
            //Recover Carousel and pull back
        }

        private void LeaveOption_Completed(object sender, EventArgs e)
        {
            Option1.Source = new BitmapImage(new Uri(@"/images/" + loadedOption[currOptInd], UriKind.Relative));

            //Overlay Option Image
            CurrentOption.Source = new BitmapImage(new Uri(@"/images/" + loadedOption[currOptInd], UriKind.Relative));
            CurrentOption.Visibility = Visibility.Visible;
            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            finishedAnim = true;
            //Ensure that when the carousel rolls back it loads up the 'current image'
            NextOption();
        }


    }
}
