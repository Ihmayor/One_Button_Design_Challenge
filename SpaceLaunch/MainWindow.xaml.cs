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
        //Animation Storyboards
        private Storyboard leave;
        private Storyboard fight;
        private Storyboard tutscene;

        //Error codes
        private readonly List<Tuple<string, string>> errorCodes = new List<Tuple<string, string>>() {
            new Tuple<string, string>("e,q,e,e,e", "F,E,C,B,B"),
            new Tuple<string, string>("h,q,e,e", "A,F,D,B"),
            new Tuple<string, string>("e,e,e,e", "A,A,A,A")};

        //Currently shown option
        int currOptIndex;

        //Carousel Options
        private Dictionary<string, int> loadedOption = new Dictionary<string, int> { { "Gun.png", 4 }, { "Stick.png", -1 } };
        
        //Possible rythm patterns
        private readonly string[] loadedCode = new string[] { "eqq","eee", "eeqe", "h", "hh", "qqe", "eqe", "qqq", "q", "e", "eq", "heq" };

        //Interaction
        private bool isHeld;
        private bool isFirstClick;
        private bool disableInteraction;

        //Sound
        //Frequencies based off: http://www.guitarpitchshifter.com/fig_2_1.png
        private readonly Dictionary<string, int> ScaleNotes = new Dictionary<string, int>() {
            { "A", 440 },
            { "B", 494 },
            { "C", 524 },
            { "D", 587 },
            { "E", 659 },
            { "F", 698 },
            { "G", 784 },
            { "A2", 880 },
            { "B2", 988 },
            { "C2", 1046 } };

        private int currentNoteFrequencyIndex;
        private string noteFrequencySelected;

        //Record of notes played
        private Tuple<string, string> record = new Tuple<string, string>("", "");

        //Sound Thread + Timing
        private Thread soundThread;
        private Thread drumThread;
        private Thread feedbackThread;
        private SoundPlayer drumSound = new SoundPlayer("drumbeat.wav");
        private long savedTick;//Used for timing and holding notes
        private bool stopPlaying;//Prevents unwanted sounds arising from loops

        //Records time elapsed
        private Stopwatch watch;

        //Timers
        private DispatcherTimer pauseTimer;//Handles code checking during massive pauses
        private DispatcherTimer scaleTimer;//Handles alternating frequences
        private DispatcherTimer themeTimer;//Loops the playing of the theme song
        private DispatcherTimer feedbackTimer;//Hides feedback after being showm
        
        //Fighting Vairables
        private int TotalPower;
        private int prepareLevel;
        private bool isFighting;
        private int zeon_zaku_power = 2;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            currentNoteFrequencyIndex = 0;
            currOptIndex = 0;
            finishedAnim = true;


            //Timer Used to check pauses between entries
            pauseTimer = new DispatcherTimer();
            pauseTimer.Tick += PauseTimer_Tick;

            //Timer used to alternate note frequencies
            scaleTimer = new DispatcherTimer();
            scaleTimer.Interval = TimeSpan.FromMilliseconds(400);
            scaleTimer.Tick += ScaleTimer_Tick;

            //Timer used to fade feed back 
            feedbackTimer = new DispatcherTimer();
            feedbackTimer.Interval = TimeSpan.FromMilliseconds(700);
            feedbackTimer.Tick += FeedbackTimer_Tick;

            //Animation Resources + Init
            leave = FindResource("Leave") as Storyboard;
            leave.Completed += LeaveOption_Completed;

            fight = FindResource("FightScene") as Storyboard;
            fight.Completed += Fight_Completed;

            tutscene = FindResource("TutScene") as Storyboard;
            tutscene.Completed += Tutscene_Completed;

            drumSound.Load();

            //Sound Init and Sound Hold Init Vars
            noteFrequencySelected = "A";
            watch = new Stopwatch();

            disableInteraction = true;
            isFirstClick = false;
            isFighting = false;

            themeTimer = new DispatcherTimer();
            themeTimer.Interval = TimeSpan.FromMilliseconds(7000);
            themeTimer.Tick += ThemeTimer_Tick;
            themeTimer.Start();
        }

        private void FeedbackTimer_Tick(object sender, EventArgs e)
        {
            ResultCheck.Visibility = Visibility.Hidden;
            //We only want this to happen once
            feedbackTimer.Stop();
        }

        private void ThemeTimer_Tick(object sender, EventArgs e)
        {
            if (!isFirstClick)
            {
                soundThread = new Thread(new ThreadStart(() => { PlayTheme(); }));
                soundThread.Start();
            }
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
            if (soundThread != null)
            {
                soundThread.Abort();
                soundThread = null;
            }
        }

        private void ScaleTimer_Tick(object sender, EventArgs e)
        {
            currentNoteFrequencyIndex++;
            currentNoteFrequencyIndex %= ScaleNotes.Count;
            noteFrequencySelected = ScaleNotes.ToList<KeyValuePair<string, int>>()[currentNoteFrequencyIndex].Key;
            ScaleHolder.Source = new BitmapImage(new Uri(@"images/Notes/note_" + noteFrequencySelected + ".png", UriKind.Relative));
        }
        private void LoadStart()
        {
            //Trigger Tutorial Animation
            tutscene.Begin();
        }

        private void Tutscene_Completed(object sender, EventArgs e)
        {
            StartScene.Visibility = Visibility.Hidden;
            tutscene.Stop();

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
            if (isFighting)
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
                if (feedbackThread != null)
                {
                    feedbackThread.Abort();
                    feedbackThread = null;
                }

                feedbackThread = new Thread(new ThreadStart(() => {
                    Console.Beep(ScaleNotes["E"], 200);
                    Console.Beep(ScaleNotes["G"], 500);
                    Console.Beep(ScaleNotes["C2"], 200);
                    Console.Beep(ScaleNotes["C2"], 1000);
                }));

                feedbackThread.Start();


                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_right.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                TotalPower += loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Value;
                feedbackTimer.Start();
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
                ///PUT ON DIFFERENT THREAD
                Tuple<string, string> fetchedErrorCode = errorCodes[new Random().Next(0, errorCodes.Count)];
                if (feedbackThread != null)
                {
                    feedbackThread.Abort();
                    feedbackThread = null;
                }

                feedbackThread = new Thread(new ThreadStart(() => { PlayRecord(fetchedErrorCode);}));
                feedbackThread.Start();

                TotalPower--;

                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_wrong.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                feedbackTimer.Start();

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
            if (prepareLevel > 6 && !isFighting)
            {
                pauseTimer.Stop();
                //drumSound.Stop(); 
                //Disabled UI elements in this thread instead of the async thread.
                GameGrid.Visibility = Visibility.Hidden;
                MessageHolder.Visibility = Visibility.Visible;
                EndScene.Visibility = Visibility.Visible;
                TheButton.Visibility = Visibility.Hidden;
                DisabledButton.Visibility = Visibility.Visible;
                NoteHolder.Visibility = Visibility.Hidden;

                BeginFight();
                if (TotalPower > zeon_zaku_power)
                    new Thread(new ThreadStart(() => { PlayTheme(); })).Start();
                else
                    new Thread(new ThreadStart(() => { PlayRecord(record); })).Start();
            }

        }

        private void BeginFight()
        {
            drumSound.Stop();
            disableInteraction = true;
            isFighting = true;
            leave.Stop();

            if (TotalPower > zeon_zaku_power)
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamGun.png", UriKind.Relative));
            else
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamNoGun.png", UriKind.Relative));

            fight.Begin();

        }


        private void Fight_Completed(object sender, EventArgs e)
        {
            Storyboard outcome;
            if (TotalPower > zeon_zaku_power)
            {
                outcome = FindResource("WinFightScene") as Storyboard;
                outcome.Begin();
            }
            else
            {
                outcome = FindResource("LoseFightScene") as Storyboard;
                outcome.Begin();
            }
        }

                private void PlayRecord(Tuple<string, string> recordNotes)
        {
            int eighth = 200;
            int quarter = 800;
            int half = 1200;
            string[] patternNotes = recordNotes.Item1.Split(',');
            string[] scaleNotes = recordNotes.Item2.Split(',');



            for (int i = 0; i < patternNotes.Length; i++)
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

            if (soundThread != null)
            {
                soundThread.Abort();
                soundThread = null;
            }
            soundThread = new Thread(new ThreadStart(() => { PlayTone(noteFrequencySelected); }));

            soundThread.Start();
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_down.png", UriKind.Relative));

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
                    record = new Tuple<string, string>(record.Item1 + ",e", record.Item2 + "," + noteFrequencySelected);
                }
            }
            else if (diff >= 300 && diff < 700)
            {
                quarterNoteCount++;
                if (NoteHolder.Text.Length <= limitNote)
                {
                    NoteHolder.Text += "q";
                    record = new Tuple<string, string>(record.Item1 + ",q", record.Item2 + "," + noteFrequencySelected);
                }
            }
            else if (diff >= 700 && diff <= 1600)
            {
                halfNoteCount++;
                if (NoteHolder.Text.Length <= limitNote)
                {
                    NoteHolder.Text += "h";
                    record = new Tuple<string, string>(record.Item1 + ",h", record.Item2 + "," + noteFrequencySelected);
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

            //Clean up soundthread
            if (soundThread != null)
                soundThread.Abort();
            soundThread = null;

            isHeld = false;

            if (!disableInteraction)
            {
                pauseTimer.Interval = TimeSpan.FromMilliseconds(4000);
                pauseTimer.Start();
            }
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
