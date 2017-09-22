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

    private Dictionary<string, int> ScaledNotes = new Dictionary<string, int> { { "A",440 }, { "B", 494 }, {"C",524 },
        { "D",587 }, "E":659, "F":698, "G":784, "A2":880,
                                                                                  "B2":988, "C2":1046 };



    public partial class MainWindow : Window
    {
        private DateTime savedTick;
        private bool firstClick;
        private bool isHeld;
        private bool stopPlaying;
        private Timer holdNoteTimer;
        private SoundPlayer[] ahSound;
        private int streamNum;
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
            ahSound[streamNum].Load();
            ahSound[ahSound.Length-1].Load();
        }

        public void NextOption()
        {
            //Set Visible Carousel Option to cover Single Option
            //Reset to original state

            //Load Next Option in second part of carousel
            //

            //Trigger Animation
            Storyboard leave = FindResource("Leave") as Storyboard;
            leave.Begin();

            //Overlay Option Image

            //Recover Carousel and pull back
        }

        private void PlayTone(int frequency)
        {
            stopPlaying = false;
            while (!stopPlaying)
            {
                Console.Beep(frequency,50000);
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
            soundThread = new Thread(new ThreadStart(()=> { PlayTone(524); }));
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

    }
}
