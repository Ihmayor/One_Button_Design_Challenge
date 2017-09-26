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
        private Dictionary<string, int> loadedOption = new Dictionary<string, int> { { "Gun.png", 4 }, { "Stick.png", -1 }, { "gundamhammer.png", 3}, { "lazersword.png", 4 } };
        
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

        //Keep track of place as it loops through the frequency dictionary
        private int currentNoteFrequencyIndex;

        //Current frequency of pressed notes
        private string noteFrequencySelected;

        //Record of notes played
        private Tuple<string, string> record = new Tuple<string, string>("", "");

        //Sound Thread + Timing
        private Thread soundThread;
        private Thread drumThread;
        private Thread feedbackThread;
        private SoundPlayer tempoSound = new SoundPlayer("dubchords.wav");
        private SoundPlayer warningsound = new SoundPlayer("Warning.wav");
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
        private readonly int zeon_zaku_power = 5;

        private bool finishedAnim;

        //Variables to keep track of user's entered rhythm 
        private int halfNoteCount;
        private int quarterNoteCount;
        private int eighthNoteCount;

        public MainWindow()
        {
            InitializeComponent();

            //Index Inits
            currentNoteFrequencyIndex = 0;
            currOptIndex = 0;

            //Timer Used to check pauses between entries
            pauseTimer = new DispatcherTimer();
            pauseTimer.Interval = TimeSpan.FromMilliseconds(3000);
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

            //Load End Fight Scene Animation
            fight = FindResource("FightScene") as Storyboard;
            fight.Completed += Fight_Completed;

            //Load Tutorial Scene
            tutscene = FindResource("TutScene") as Storyboard;
            tutscene.Completed += Tutscene_Completed;

            //Load Sound to keep tempo
            tempoSound.Load();
            //Load initial intro sound
            warningsound.Load();

            //Sound Init and Sound Hold Init Vars
            noteFrequencySelected = "A";
            watch = new Stopwatch();

            //Various bools to prevent count checks from happening when they don't need to happen
            disableInteraction = true;
            isFirstClick = false;
            isFighting = false;
            finishedAnim = true;

            //Timer loops playing the theme as it waits for the user's input
            themeTimer = new DispatcherTimer();
            themeTimer.Interval = TimeSpan.FromMilliseconds(7000);
            themeTimer.Tick += ThemeTimer_Tick;
            themeTimer.Start();
        }

        private void FeedbackTimer_Tick(object sender, EventArgs e)
        {
            //Rehid the feedback as soon as the user has seen it
            ResultCheck.Visibility = Visibility.Hidden;

            //We only want this to happen once
            feedbackTimer.Stop();
        }

        //Loop the playing of the theme 
        private void ThemeTimer_Tick(object sender, EventArgs e)
        {
            //Prevent overlapping threads of the theme playing
            if (!isFirstClick)
            {
                if (soundThread == null)
                {
                    soundThread = new Thread(new ThreadStart(() => { PlayTheme(); }));
                    soundThread.Start();
                }

            }
        }

        //Hardcoded Notes, Pauses of the first bit of Mobile Suit Gundam Theme
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

            //Clean up the used thread.
            if (soundThread != null)
            {
                soundThread.Abort();
                soundThread = null;
            }
        }

        //Loop through the scales of notes
        private void ScaleTimer_Tick(object sender, EventArgs e)
        {
            currentNoteFrequencyIndex++;
            currentNoteFrequencyIndex %= ScaleNotes.Count;
            noteFrequencySelected = ScaleNotes.ToList<KeyValuePair<string, int>>()[currentNoteFrequencyIndex].Key;
            ScaleHolder.Source = new BitmapImage(new Uri(@"images/Notes/note_" + noteFrequencySelected + ".png", UriKind.Relative));
        }

        //Load the intor of the game
        private void LoadStart()
        {
            //Trigger Tutorial Animation
            warningsound.Play();
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_off.png", UriKind.Relative));
            tutscene.Begin();
        }

        //Upon finishing the tutorial animation, load up the main game
        private void Tutscene_Completed(object sender, EventArgs e)
        {
            StartScene.Visibility = Visibility.Hidden;
            tutscene.Stop();

            disableInteraction = false;
            GameGrid.Visibility = Visibility.Visible;

            drumThread = new Thread(new ThreadStart(() => { tempoSound.PlayLooping(); }));
            drumThread.Start();

            NextOption().Wait();

            themeTimer.Stop();
            scaleTimer.Start();
        }

        //If a long pause occurrs check if the users entries are correct
        private void PauseTimer_Tick(object sender, EventArgs e)
        {
            CheckNoteCountMatch().Wait();
        }

        //Play the tone given and hold it until interrupted
        private void PlayTone(string Note)
        {
            int frequency = ScaleNotes[Note];
            stopPlaying = false;
            while (!stopPlaying)
            {
                Console.Beep(frequency, 100000);
            }
        }

        //Stop the tone from playing
        private void StopTone()
        {
            stopPlaying = true;
        }

        //Clear the notes counts after they have been checked
        private void ClearNoteCount()
        {
            halfNoteCount = 0;
            quarterNoteCount = 0;
            eighthNoteCount = 0;
        }

        //Check if the user's input matches the rhythm shown on screen
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
                //Play Ding Right Sound on a different thread
                //Ensure the thread is being used by something else.
                if (feedbackThread != null)
                {
                    feedbackThread.Abort();
                    feedbackThread = null;
                }

                //Start new thread and play feedback sounds
                feedbackThread = new Thread(new ThreadStart(() => {
                    Console.Beep(ScaleNotes["E"], 200);
                    Console.Beep(ScaleNotes["G"], 500);
                    Console.Beep(ScaleNotes["C2"], 200);
                    Console.Beep(ScaleNotes["C2"], 1000);
                }));

                feedbackThread.Start();

                //Show Feedback Checkmark to inform the user they have entered correctly
                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_right.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                //Increase Gundam Power based on item's attributes
                TotalPower += loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Value;
                feedbackTimer.Start();
             }
            else if (enteredInput == "")
            {
               //Avoid doing anything with empty input
            }
            else
            {
                //Play 'Wrong' Sound
                //Placed upon a different thread to avoid conflicts
                Tuple<string, string> fetchedErrorCode = errorCodes[new Random().Next(0, errorCodes.Count)];
                //Ensure nothing else is using the thread
                if (feedbackThread != null)
                {
                    feedbackThread.Abort();
                    feedbackThread = null;
                }
                feedbackThread = new Thread(new ThreadStart(() => { PlayRecord(fetchedErrorCode);}));
                feedbackThread.Start();

                //Decrease power automatically when mistakes are made
                TotalPower--;

                //Give user feedback that their entry was wrong
                ResultCheck.Source = new BitmapImage(new Uri(@"images/check_wrong.png", UriKind.Relative));
                ResultCheck.Visibility = Visibility.Visible;
                feedbackTimer.Start();

            }
            

            //Every entry is one step closer to battle.
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
                //Check if we are prepared to fight and not already fighting the enemy

                //Stop any more rhythm checks
                pauseTimer.Stop();

                //Disabled necessary UI elements in this thread instead of the async thread.
                GameGrid.Visibility = Visibility.Hidden;
                MessageHolder.Visibility = Visibility.Visible;
                EndScene.Visibility = Visibility.Visible;
                TheButton.Visibility = Visibility.Hidden;
                DisabledButton.Visibility = Visibility.Hidden;
                NoteHolder.Visibility = Visibility.Hidden;

                //Clean up Feedbackthread
                if (feedbackThread != null)
                {
                    feedbackThread.Abort();
                    feedbackThread = null;
                }
                
                //Init various fight variables and trigger the animation
                BeginFight();

                //Play triumphant gundam theme if player has won, playback their potentially messy play record.
                if (TotalPower > zeon_zaku_power)
                    new Thread(new ThreadStart(() => { PlayTheme(); })).Start();
                else
                    new Thread(new ThreadStart(() => { PlayRecord(record); })).Start();
            }

        }


        //Fight begins so we have both trigger the fighting animations as well as disable any interactive elements
        private void BeginFight()
        {
            tempoSound.Stop();
            disableInteraction = true;
            isFighting = true;
            leave.Stop();

            if (TotalPower > zeon_zaku_power)
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamGun.png", UriKind.Relative));
            else
                GundamImage.Source = new BitmapImage(new Uri(@"images/GundamNoGun.png", UriKind.Relative));

            fight.Begin();

        }

        //Upon Fighting animation completed, display the outcome animation
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


        //Helper method that plays the string of notes according to their specifications
        //Pauses are not accounted for
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


        //ON BUTTON CLICK
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

        //On BUTTON RELEASE
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
                pauseTimer.Start();
            }
        }

        //ON BUTTON HOVER PROVIDE FEEDBACK TO USER THAT THE BUTTON IS PRESSABLE
        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_over.png", UriKind.Relative));
        }


        //ON BUTTON LEAVE HOVER PROVIDE FEEDBACK TO USER THAT THE BUTTON IS PRESSABLE
        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isHeld)
                Image_MouseUp(sender, new MouseButtonEventArgs(e.MouseDevice, 0, new MouseButton()));
            TheButton.Source = new BitmapImage(new Uri(@"images/ver1button_up.png", UriKind.Relative));
        }


        //Load up next option in the carousel.
        //Trigger leave animation 
        public async Task NextOption()
        {
            //Check for note entries when option has changed
            //Or alternatively disable this function when necessary
            if (!finishedAnim)
                return;
            else if (disableInteraction)
                return;

            if (NoteHolder.Text != "")
            {
                await CheckNoteCountMatch();
            }

            NoteHolder.Text = "";

            //Increase Index and make sure it loops around
            currOptIndex = new Random().Next(0, loadedOption.Count);

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

        //When the Animation completes this function ensures the carousel of options loops 
        private void LeaveOption_Completed(object sender, EventArgs e)
        {
            Option1.Source = new BitmapImage(new Uri(@"/images/" + loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex ].Key, UriKind.Relative));

            //Overlay Option Image
            CurrentOption.Source = new BitmapImage(new Uri(@"/images/" + loadedOption.ToList<KeyValuePair<string, int>>()[currOptIndex].Key, UriKind.Relative));
            CurrentOption.Visibility = Visibility.Visible;

            Option1.Visibility = Visibility.Hidden;
            Option2.Visibility = Visibility.Hidden;
            finishedAnim = true;
            //Ensure that when the carousel rolls back it loads up the 'current image'
            NextOption();

        }


        //ENSURE THAT ALL THREADS ARE CLOSED AND CLEANED UP WHEN APPLICATION IS LEFT!
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            tempoSound.Stop();
            if (soundThread != null)
            {
                soundThread.Abort();
                soundThread = null;
            }
            if (drumThread != null)
            {
                drumThread.Abort();
                drumThread = null;
            }

            if (feedbackThread !=null)
            {
                feedbackThread.Abort();
                feedbackThread = null;
            }

            watch.Stop();
            pauseTimer.Stop();
            scaleTimer.Stop();
            themeTimer.Stop();
            feedbackTimer.Stop();
        }
    }
}
