using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
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

    public partial class MainWindow : Window
    {
        //Carousel Animation
        private Storyboard leave;
        private Dictionary<string, int> loadedOption = new Dictionary<string, int> { { "Gun.png", 4 }, { "Stick.png", -1 } };
        private string[] loadedCode = new string[] { "ehqq", "eeqe", "h", "hh", "qqe", "eqe", "qqq", "q", "e", "eq", "heq" };


        private List<Tuple<string, string>> errorCodes = new List<Tuple<string, string>>() {
            new Tuple<string, string>("e,q,e", "F,D,C"),
            new Tuple<string, string>("h,q,", "B,A,")};
     
        int currOptIndex;

        //Interaction
        private bool isHeld;
        private bool isFirstClick;
        private bool disableInteraction;

        //Sound
        private Dictionary<string, int> ScaleNotes = new Dictionary<string, int>() { { "A", 440 }, { "B", 494 }, { "C", 524 }, { "D", 587 }, { "E", 659 }, { "F", 698 }, { "G", 784 }, { "A2", 880 }, { "B2", 988 }, { "C2", 1046 } };
        private int currentNoteIndex;
        private string noteSelected;


        private Tuple<string, string> record = new Tuple<string, string>("","");

        //Sound Thread + Timing
        private Stopwatch watch;
        private Thread soundThread;
        private DispatcherTimer pauseTimer;
        private DispatcherTimer scaleTimer;
        private DispatcherTimer themeTimer;
        private Thread drumThread;
        private SoundPlayer drumSound = new SoundPlayer("drumbeat.wav");

        private bool stopPlaying;
        private long savedTick;


        //Fighting Vairables
        private int TotalPower;
        private int prepareLevel;
        private bool isFirstFight;

        public MainWindow()
        {
            //Music Box Storage
            


            InitializeComponent();
            DataContext = this;
            //Timer the change in scale. Reperesent timer in flash countdown in middle.

            currentNoteIndex = 0;
            currOptIndex = 0;
            finishedAnim = true;


            //Timer Used to check pauses between entries
            pauseTimer = new DispatcherTimer();
            pauseTimer.Tick += PauseTimer_Tick;

            //Timer used to alternate scales
            scaleTimer = new DispatcherTimer();
            scaleTimer.Interval = TimeSpan.FromMilliseconds(400);
            scaleTimer.Tick += ScaleTimer_Tick;

            //Animation Resources + Init
            leave = FindResource("Leave") as Storyboard;
            leave.Completed += LeaveOption_Completed;
            Thread.Sleep(2000);

            drumSound.Load();

            //Sound Init and Sound Hold Init Vars
            noteSelected = "A";
            watch = new Stopwatch();

            disableInteraction = true;
            isFirstClick = false;
            isFirstFight = true;

            themeTimer = new DispatcherTimer();
            themeTimer.Interval = TimeSpan.FromMilliseconds(2000);
            themeTimer.Tick += ThemeTimer_Tick;
            themeTimer.Start();
        }

        private void ThemeTimer_Tick(object sender, EventArgs e)
        {
            if (!isFirstClick)
                PlayTheme();
        }

        private void PlayTheme()
        {
            Console.Beep(ScaleNotes["E"], 700);
            Thread.Sleep(1000);
            Console.Beep(ScaleNotes["G"], 300);
            Console.Beep(ScaleNotes["C2"], 300);
            Console.Beep(ScaleNotes["C2"], 300);
            Console.Beep(ScaleNotes["C2"], 800);
            Thread.Sleep(300);
            Console.Beep(ScaleNotes["E"], 700);
            Console.Beep(ScaleNotes["G"], 300);
            Console.Beep(ScaleNotes["C2"], 700);
            Console.Beep(ScaleNotes["C2"], 300);
            Thread.Sleep(300);
            Console.Beep(ScaleNotes["C2"], 700);
            Thread.Sleep(800);
            Console.Beep(ScaleNotes["F"], 700);
            Console.Beep(ScaleNotes["A2"], 700);
            Console.Beep(ScaleNotes["C2"], 700);
            Console.Beep(ScaleNotes["C2"], 300);
            Thread.Sleep(300);
            Console.Beep(ScaleNotes["C2"], 700);
            Console.Beep(ScaleNotes["B2"], 700);
            Console.Beep(ScaleNotes["A2"], 700);
            Console.Beep(ScaleNotes["C2"], 300);
            Thread.Sleep(200);
            Console.Beep(ScaleNotes["B2"], 700);
            Thread.Sleep(800);
            Console.Beep(ScaleNotes["G"], 700);
            Console.Beep(ScaleNotes["G"], 700);

        }

        private void ScaleTimer_Tick(object sender, EventArgs e)
        {
            currentNoteIndex++;
            currentNoteIndex %= ScaleNotes.Count;
            noteSelected = ScaleNotes.ToList<KeyValuePair<string, int>>()[currentNoteIndex].Key;
            ScaleHolder.Source = new BitmapImage(new Uri(@"images/Notes/note_" + noteSelected + ".png", UriKind.Relative));
        }

        private void LoadStart()
        {
            StartScene.Visibility = Visibility.Hidden;
            //Trigger Zeon_Zaku Animation

            //THE BELOW WILL GO IN THE FUNCTION 
            disableInteraction = false;
            GameGrid.Visibility = Visibility.Visible;

            drumThread = new Thread(new ThreadStart(() => { drumSound.PlayLooping(); }));
            drumThread.Start();

            NextOption().Wait();

            themeTimer.Stop();
            scaleTimer.Start();
        }



        private void PauseTimer_Tick(object sender, EventArgs e)
        {
            CheckNoteCountMatch().Wait();
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


        private int halfNoteCount;
        private int quarterNoteCount;
        private int eighthNoteCount;

        private void ClearNoteCount()
        {
            halfNoteCount = 0;
            quarterNoteCount = 0;
            eighthNoteCount = 0;
        }

        private async Task CheckNoteCountMatch()
        {
            if (!isFirstFight)
                return;
            //Cancel any running noise
            Console.Beep(1000, 1);

            //Get Current Option to compare to
            string currentCode = CodeHolder.Text;

            //Get Code 
            string enteredInput = NoteHolder.Text;

            //Set the bool if the code is a match
            if (currentCode == enteredInput)
            {
                //Play Ding Right Sound
                Console.Beep(ScaleNotes["A"], 800);
                Console.Beep(ScaleNotes["D"], 1000);

                  
                //Trigger Animation

                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_right.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                TotalPower += loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Value;
                //      ResultCheck.Visibility = Visibility.Hidden;
            }
            else if (enteredInput == "")
            {
                //Console.Beep(ScaleNotes["A"], 500);
                //Console.Beep(ScaleNotes["B"], 500);
                //Console.Beep(ScaleNotes["C"], 500);

            }
            else
            {
                //Play 'Wrong' Sound
                Tuple<string,string> fetchedErrorCode = errorCodes[new Random().Next(0, errorCodes.Count)];
                PlayRecord(fetchedErrorCode);

                TotalPower--;
                //Trigger Animation
                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_wrong.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                //    ResultCheck.Visibility = Visibility.Hidden;

            }

            if (enteredInput != "")
            {
                prepareLevel++;
                PrepareProgress.Value++;
            }
            //Reset Any Button Disabling and Clear Note Count
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
            ClearNoteCount();
            NoteHolder.Text = "";
            if (prepareLevel > 6 && isFirstFight)
            {
                pauseTimer.Stop();
                //drumSound.Stop(); 
                //Disabled UI elements in this thread instead of the async thread.
                GameGrid.Visibility = Visibility.Hidden;
                MessageHolder.Visibility = Visibility.Visible;
                EndScene.Visibility = Visibility.Visible;
                TheButton.Visibility = Visibility.Hidden;
                TheButton.Visibility = Visibility.Hidden;
                NoteHolder.Visibility = Visibility.Hidden;

                BeginFight();
                new Thread(new ThreadStart(() => { PlayRecord(record); })).Start();
            }

        }

        private void BeginFight()
        {
            drumSound.Stop();
            disableInteraction = true;
            isFirstFight = false;
            //Begin Animation of fight

            leave.Stop();


            int zeon_zaku_power = 2;
            if (TotalPower > zeon_zaku_power)
            {
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamGun.png", UriKind.Relative));
                //Show Win Animation
            }
            else
            {
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamNoGun.png", UriKind.Relative));
                //Show Lose Animation
            }
          }

        private void PlayRecord(Tuple<string,string> recordNotes)
        {
            int eighth = 200;
            int quarter = 800;
            int half = 1200;
            string[] patternNotes = recordNotes.Item1.Split(',');
            string[] scaleNotes = recordNotes.Item2.Split(',');



            for (int i = 0;i < patternNotes.Length; i++)
            {
                string length = patternNotes[i];
                string note = scaleNotes[i];
                switch (length)
                {
                    case "e":
                        Console.Beep(ScaleNotes[note], eighth);
                        break;
                    case "q":
                        Console.Beep(ScaleNotes[note], quarter);
                        break;
                    case "h":
                        Console.Beep(ScaleNotes[note], half);
                        break;
                }
            }

        }



        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_down.png", UriKind.Relative));

            if (!isFirstClick)
            {

                isFirstClick = true;
                   LoadStart();
                return;
            }
            if (disableInteraction)
                return;
            pauseTimer.Stop();
            watch.Start();
            savedTick = watch.ElapsedMilliseconds;

            //noteSelected = ScaleNotes.ToList<KeyValuePair<string, int>>()[new Random().Next(0, ScaleNotes.Count)].Key;
            isHeld = true;

            // PlayTone(noteSelect);
            soundThread = new Thread(new ThreadStart(() => { PlayTone(noteSelected); }));

            soundThread.Start();

        }

        private async void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.Beep(1000, 1);
            watch.Stop();
            long currTick = watch.ElapsedMilliseconds;
            long diff = currTick - savedTick;
            int limitNote = 6;

            if (diff >= 60 && diff < 300)
            {
                eighthNoteCount++;
                if (NoteHolder.Text.Length <= limitNote)
                {
                    //Record Notes
                    NoteHolder.Text += "e";
                    record = new Tuple<string, string>(record.Item1 + ",e", record.Item2 + "," + noteSelected);
                }
            }
            else if (diff >= 300 && diff < 700)
            {
                quarterNoteCount++;
                if (NoteHolder.Text.Length <= limitNote)
                {
                    NoteHolder.Text += "q";
                    record = new Tuple<string, string>(record.Item1 + ",q", record.Item2 + "," + noteSelected);
                }
            }
            else if (diff >= 700 && diff <= 1600)
            {
                halfNoteCount++;
                if (NoteHolder.Text.Length <= limitNote)
                {
                    NoteHolder.Text += "h";
                    record = new Tuple<string, string>(record.Item1 + ",h", record.Item2 + "," + noteSelected);
                }
            }
            else if (diff >= 3000)
            {
                await CheckNoteCountMatch();
            }

            if (NoteHolder.Text.Length >= 5)
            {
                TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_off.png", UriKind.Relative));
                await CheckNoteCountMatch();
                return;
            }

            //   NoteHolder.Text = ": "+diff;
            savedTick = currTick;

            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
            // soundThread.
            StopTone();
            if (soundThread != null)
                soundThread.Abort();
            soundThread = null;
            isHeld = false;

            pauseTimer.Interval = TimeSpan.FromMilliseconds(4000);
            pauseTimer.Start();
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_over.png", UriKind.Relative));
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isHeld)
                Image_MouseUp(sender, new MouseButtonEventArgs(e.MouseDevice, 0, new MouseButton()));
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
        }


        private bool finishedAnim;


        public async Task NextOption()
        {
            if (!finishedAnim)
                return;
            else if (disableInteraction)
                return;



            //Increase Index and make sure it loops around
            currOptIndex++;
            currOptIndex %= loadedOption.Count;

            //Set Visible Carousel Option to cover Single Option
            //Reset to original state
            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            CurrentOption.Visibility = Visibility.Hidden;
            Option1.Visibility = Visibility.Visible;
            Option2.Visibility = Visibility.Visible;

            //Load Next Option in second part of carousel
            Option2.Source = new BitmapImage(new Uri(@"/images/" + loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Key, UriKind.Relative));
            CodeHolder.Text = loadedCode[new Random().Next(0, loadedCode.Length)];

            //Trigger Animation
            leave.Begin();
            finishedAnim = false;
            //Recover Carousel and pull back
        }

        private void LeaveOption_Completed(object sender, EventArgs e)
        {
            Option1.Source = new BitmapImage(new Uri(@"/images/" + loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Key, UriKind.Relative));

            //Overlay Option Image
            CurrentOption.Source = new BitmapImage(new Uri(@"/images/" + loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Key, UriKind.Relative));
            CurrentOption.Visibility = Visibility.Visible;


            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            finishedAnim = true;
            //Ensure that when the carousel rolls back it loads up the 'current image'
            NextOption();

        }


    }
}
