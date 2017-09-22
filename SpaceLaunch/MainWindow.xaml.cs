using System;
using System.Collections.Generic;
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
        private DateTime savedTick;
        private bool firstClick;
        private bool isHeld;
        private bool stopPlaying;
        private Timer holdNoteTimer;
        private SoundPlayer[] ahSound;
        private int streamNum;
        private int currentNote;
        private string[] loadedOption = new string[] { "planet.png", "planet2.png" };
        int currOptInd;
        private Dictionary<string, int> ScaleNotes = new Dictionary<string, int>() { { "A",440 }, { "B", 494 }, {"C",524 },
                                                                                  {"D",587 }, {"E", 659}, {"F",698}, {"G",784}, {"A2",880},
                                                                                  {"B2",988}, {"C2",1046 } };

        public MainWindow()
        {
            InitializeComponent();

            //Load Notes CDEFGABC!<===== Where C! is highest C!
            //Scale keeps going up and up but we only have 4 stages or something.
            //Timer the change in scale. Reperesent timer in flash countdown in middle.
            firstClick = true;
            savedTick = DateTime.Now;
            ahSound = new SoundPlayer[] { new SoundPlayer("a.wav"), new SoundPlayer("a.wav") };
            streamNum = 0;
            currentNote = 0;
            currOptInd = 0;
            ahSound[streamNum].Load();
            ahSound[ahSound.Length - 1].Load();
        }





        private void PlayTone(string Note)
        {
            int frequency = ScaleNotes[Note];
            stopPlaying = false;
            while (!stopPlaying)
            {
                Console.Beep(frequency, 50000);
            }
        }

        private void StopTone()
        {
            stopPlaying = true;
        }
        Thread soundThread;
        private void Option2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isHeld = true;
            Console.WriteLine(((Image)sender).Name);
            DateTime currTick = DateTime.Now;
            int diff = currTick.Millisecond - savedTick.Millisecond;
            savedTick = currTick;
            soundThread = new Thread(new ThreadStart(() => { PlayTone("D"); }));
            soundThread.Start();
        }

        private void Option_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.Beep(1000, 1);
            soundThread.Abort();
            StopTone();

            isHeld = false;
            DateTime currTick = DateTime.Now;
            int diff = currTick.Millisecond - savedTick.Millisecond;
            Console.WriteLine("Lift! " + diff);
            savedTick = currTick;
            savedTick = DateTime.Now;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            NextOption();
        }

        public void NextOption()
        {

            //Increase Index and make sure it loops around
            currOptInd++;
            currOptInd %= loadedOption.Length;

            //Set Visible Carousel Option to cover Single Option
            //Reset to original state
            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            Storyboard leave = FindResource("Leave") as Storyboard;
            leave.Seek(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            Option2.BeginAnimation(Image.RenderTransformProperty, null);
            leave.Stop();
            CurrentOption.Visibility = Visibility.Hidden;
            Option1.Visibility = Visibility.Visible;
            Option2.Visibility = Visibility.Visible;

            //Load Next Option in second part of carousel
            Option2.Source = new BitmapImage(new Uri(@"/images/"+loadedOption[currOptInd], UriKind.Relative));


            //Trigger Animation
            leave.Begin();
            leave.Completed += LeaveOption_Completed;
        
            //Recover Carousel and pull back
        }

        private void LeaveOption_Completed(object sender, EventArgs e)
        {
            //Ensure that when the carousel rolls back it loads up the 'current image'
            Option1.Source = new BitmapImage(new Uri(@"/images/" + loadedOption[currOptInd], UriKind.Relative));
        
            //Overlay Option Image
            CurrentOption.Source = new BitmapImage(new Uri(@"/images/" + loadedOption[currOptInd], UriKind.Relative));
          //  CurrentOption.Visibility = Visibility.Visible;
            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
        }
    }
}
