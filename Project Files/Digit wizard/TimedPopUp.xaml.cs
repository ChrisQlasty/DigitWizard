using System;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media.Animation;

namespace FF_TouchlessControllerViewer.cs
{
    /// <summary>
    /// Creates a Pop Up MessageBox that fades out after the given timeout
    /// This class is irrelevant to the sample and is here only to help out
    /// </summary>
    public partial class TimedPopUp : Window
    {
        private Timer popUpTimer;
        public TimedPopUp(string title, string message, TimeSpan timeToLive)
        {
            InitializeComponent();
            Title = title;
            PopUpTextBoxText.Text = message;
            //We subtract 750 milliseconds to allow the pop up to fade out before closing
            popUpTimer = new Timer(Math.Max(0, timeToLive.TotalMilliseconds-750));
            popUpTimer.Elapsed += PopUpTimerOnElapsed;
            popUpTimer.Start();
        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        private void PopUpTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Dispatcher.Invoke(new Action(Close), null);

        }

        public string Message
        {
            get { return PopUpTextBoxText.Text; }
            set { Dispatcher.BeginInvoke((Action) (() => UpdatePopUpText(value))); }
        }

        private void UpdatePopUpText(string value)
        {
            PopUpTextBoxText.Text = value;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            var anim = new DoubleAnimation(0,TimeSpan.FromSeconds(1));
            anim.Completed += (s, _) => Close();
            this.BeginAnimation(OpacityProperty, anim);
        }
    }
}
