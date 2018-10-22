using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using TouchlessControllerConfiguration = PXCMTouchlessController.ProfileInfo.Configuration;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using TensorFlow;
using System.Collections.Generic;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FF_TouchlessControllerViewer.cs
{
    /// <summary>
    /// The main window of the sample
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Consts and class members
        
        private const double VerticalScrollSensitivity = 15f;
        private const double HorizontalScrollSensitivity = 1000f;
        private const double HorizontalScrollStep = 10f;
        private const double VerticalScrollStep = 0.15f;

        private readonly RealSenseEngine m_rsEngine;
        //private ScrollViewer m_myVerticalListScrollViwer;
        //private ScrollViewer m_myHorizontalListScrollViwer;
        private double m_initialVerticalScrollPoint;
        private double m_initialHorizontalScrollPoint;
        //private double m_initialVerticalScrollOffest;
        //private double m_initialHorizontalScrollOffest;
        private float m_prevZoomZ;
        private float m_zoomDelta;
        private volatile bool m_stopContinuousHorizontalScroll;
        private volatile bool m_stopContinuousVerticalScroll;

        List<Bar> _bar;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                m_rsEngine = new RealSenseEngine();
                m_rsEngine.UXEventFired += OnFiredUxEventDelegate;
                m_rsEngine.AlertFired += OnFiredAlertDelegate;
                m_rsEngine.SetConfiguration(GetCurrentConfiguration());
                m_rsEngine.Start();
            }
            catch (Exception e)
            {
                GestureLoggingTextBox.Text = "Error loading engine: " + e.Message;
                MessageBox.Show(
                    "Error loading engine\n" +
                    "Reason: " + e.Message +
                    "\nSample will run without Touchless Controller", "Engine Error",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            // we're updating the cursor position with each render event - to create smoother movement
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            //---Add Bar objects for each digit from '0' to '9'
            _bar = new List<Bar>();
            for (int i = 0; i < 10; i++)            
                _bar.Add(new Bar() { BarName = i.ToString(), Value = i*10 });                      
            this.DataContext = new RecordCollection(_bar);
        }

        #region App's TouchlessController Configuration Methods

        private TouchlessControllerConfiguration GetCurrentConfiguration()
        {
            //Setting whatever configuration we want to apply to the Touchless Controller
            //  See "Configuration" definition for more info
            var config = TouchlessControllerConfiguration.Configuration_Allow_Zoom;
            config |= TouchlessControllerConfiguration.Configuration_Scroll_Horizontally;
            config |= TouchlessControllerConfiguration.Configuration_Scroll_Vertically;
            config |= TouchlessControllerConfiguration.Configuration_Edge_Scroll_Horizontally;
            config |= TouchlessControllerConfiguration.Configuration_Edge_Scroll_Vertically;
            config |= TouchlessControllerConfiguration.Configuration_Meta_Context_Menu; 
            //config |= TouchlessControllerConfiguration.Configuration_Allow_Back;
            config |= TouchlessControllerConfiguration.Configuration_Allow_Selection;
            return config;
        }

        #endregion

        #region Window Related Methods        

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_rsEngine != null)
            {
                m_rsEngine.Shutdown();                
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            handCursor.Update();
        }

        #endregion

        #region TouchlessController Event Handlers

        private void OnFiredAlertDelegate(PXCMTouchlessController.AlertData data)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                switch (data.type)
                {
                    case PXCMTouchlessController.AlertData.AlertType.Alert_TooClose:
                        alertsDisplay.ShowVisualNotification(RealSenseNavigator.AlertsDisplay.NotificationTypes.TooClose);
                        break;
                    case PXCMTouchlessController.AlertData.AlertType.Alert_TooFar:
                        alertsDisplay.ShowVisualNotification(RealSenseNavigator.AlertsDisplay.NotificationTypes.TooFar);
                        break;
                    case PXCMTouchlessController.AlertData.AlertType.Alert_NoAlerts:
                        alertsDisplay.ShowVisualNotification(RealSenseNavigator.AlertsDisplay.NotificationTypes.NoAlerts);
                        break;
                }
            }));
        }

        private void OnFiredUxEventDelegate(PXCMTouchlessController.UXEventData data)
        {
            Dispatcher.BeginInvoke((Action) (() =>
            {
                handCursor.SetPosition(data.position.x, data.position.y, data.position.z,
                    data.bodySide == PXCMHandData.BodySideType.BODY_SIDE_RIGHT);
                // update the cursor position for the out of window feedback
                outOfScreenBorder.X = data.position.x - 0.5f;
                outOfScreenBorder.Y = data.position.y - 0.5f;
                
               
                switch (data.type)
                {
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorVisible:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Normal);
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Visible);
                        outOfScreenBorder.Visibility = Visibility.Visible;
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorMove:
                        UpdateCursorPositionText(data.position);
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorNotVisible:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Hidden);
                        outOfScreenBorder.Visibility = Visibility.Collapsed;
                        UpdateGestureText("Cursor not visible");
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ReadyForAction:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Scroll);
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_GotoStart:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Wave);
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Normal);
                        OnWave();
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Select:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Select);
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Normal);
                        CheckButtonClicked(data.position);                        
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartZoom:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Scroll);
                        UpdateGestureText(String.Format("Gesture: Start Zoom"));
                        m_prevZoomZ = data.position.z;
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Zoom:
                        m_zoomDelta = m_prevZoomZ - data.position.z;
                        if (Math.Abs(m_zoomDelta) > 0.01)
                        {
                            m_prevZoomZ = data.position.z;
                            // We use a constant zooming option that is set by number of zoom events
                            // To have zooming relative to hand movement just use the `zoomDelta`
                            var zoom = m_zoomDelta > 0 ? 1.05 : .95;
//                            ZoomableImage.Height *= zoom;
//                            ZoomableImage.Width *= zoom;
                            UpdateGestureText(String.Format("Gesture: Zooming x{0:0.00}...", zoom));
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_EndZoom:
                        UpdateGestureText(String.Format("Gesture: End Zoom"));
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartScroll:
                        double height = VisualFeedbackGrid.ActualHeight;
                        double width = VisualFeedbackGrid.ActualWidth;
                        


                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Scroll);
                        UpdateGestureText(String.Format("Gesture: Start Scroll"));
                        m_initialVerticalScrollPoint = data.position.y;
                        m_initialHorizontalScrollPoint = data.position.x;
                        //   m_initialVerticalScrollOffest = m_myVerticalListScrollViwer.VerticalOffset;
                        //   m_initialHorizontalScrollOffest = m_myHorizontalListScrollViwer.HorizontalOffset;

                        var p = new Point((float)((width * m_initialHorizontalScrollPoint)  -digitCanvas.Margin.Left),
                            (float)((height * m_initialVerticalScrollPoint) - digitCanvas.Margin.Top));
                        Interaction_Down(p);

                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Scroll:
                        UpdateGestureText(String.Format("Gesture: Scrolling... [Hand Position: ({0})]",
                            data.position.ToFormattedString()));
                        //paint_test(data.position);

                        height = VisualFeedbackGrid.ActualHeight;
                        width = VisualFeedbackGrid.ActualWidth;

                        m_initialVerticalScrollPoint = data.position.y;
                        m_initialHorizontalScrollPoint = data.position.x;
                        //   m_initialVerticalScrollOffest = m_myVerticalListScrollViwer.VerticalOffset;
                        //   m_initialHorizontalScrollOffest = m_myHorizontalListScrollViwer.HorizontalOffset;

                        p = new Point((float)((width * m_initialHorizontalScrollPoint) - digitCanvas.Margin.Left),
                            (float)((height * m_initialVerticalScrollPoint) - digitCanvas.Margin.Top));

                        Interaction_Progress(p);
                        break;
                
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_EndScroll:
                        Interaction_Up();
                        m_stopContinuousVerticalScroll = true;
                        m_stopContinuousHorizontalScroll = true;
                        UpdateGestureText(String.Format("Gesture: End Scroll"));
                        break;
                    //case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Back:
                    //    handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Back);
                    //    handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Normal);
                    //    OnBack();
                    //    break;

                        /*
                     *  PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Edge_Scroll_Vertically
                     */
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ScrollUp:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Up);
                        Task.Factory.StartNew(() => ContinuousVerticalScroll(-VerticalScrollStep));
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ScrollDown:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Down);
                        Task.Factory.StartNew(() => ContinuousVerticalScroll(VerticalScrollStep));
                        break;

                        /*
                     *  PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Edge_Scroll_Horizontally
                     */
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ScrollLeft:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Left);
                        Task.Factory.StartNew(() => ContinuousHorizontalScroll(-HorizontalScrollStep));
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ScrollRight:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Right);
                        Task.Factory.StartNew(() => ContinuousHorizontalScroll(HorizontalScrollStep));
                        break;

                        /*
                    *  PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Meta_Context_Menu
                    */
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartMetaCounter:
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StopMetaCounter:
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_ShowMetaMenu:
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_HideMetaMenu:
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_MetaPinch:
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Select);
                        handCursor.ChangeCursorState(RealSenseNavigator.CursorDisplay.CursorStates.Normal);
                        CheckCanvasPinched(data.position);                                                
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_MetaOpenHand:
                        UpdateGestureText(String.Format("Gesture: {0}", data.type));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }));
        }

     

        #endregion

        #region App's Visual Feedback Methods

        private void ContinuousVerticalScroll(double step)
        {
            m_stopContinuousVerticalScroll = false;
            while (!m_stopContinuousVerticalScroll)
            {
                Thread.Sleep(50);
             /*   Dispatcher.BeginInvoke(
                    (Action)
                        (() =>
                            m_myVerticalListScrollViwer.ScrollToVerticalOffset(
                                m_myVerticalListScrollViwer.VerticalOffset + step)));
                                */
            }
        }

        private void ContinuousHorizontalScroll(double step)
        {
            m_stopContinuousHorizontalScroll = false;
            while (!m_stopContinuousHorizontalScroll)
            {
                Thread.Sleep(50);
              /*  Dispatcher.BeginInvoke(
                    (Action)
                        (() =>
                            m_myHorizontalListScrollViwer.ScrollToHorizontalOffset(
                                m_myHorizontalListScrollViwer.HorizontalOffset + step)));
                                */
            }
        }

        private void CheckCanvasPinched(PXCMPoint3DF32 pos)
        {
            Point point = new Point();
            point.X = Math.Max(Math.Min(0.9F, pos.x), 0.1F);
            point.Y = Math.Max(Math.Min(0.9F, pos.y), 0.1F);

            //Check if click is on button
            if (!HelperMethods.PointOnCanvas(point, digitCanvas, VisualFeedbackGrid))
                return;

            var p = new TimedPopUp("", "Pinchedd! :)", new TimeSpan(0, 0, 2));
            p.Show();
        }

        private void CheckButtonClicked(PXCMPoint3DF32 pos)
        {
            Point point = new Point();
            point.X = Math.Max(Math.Min(0.9F, pos.x), 0.1F);
            point.Y = Math.Max(Math.Min(0.9F, pos.y), 0.1F);
            
            //Check if click is on button
          //  if (!HelperMethods.PointOnButton(point, TapButton, VisualFeedbackGrid)) 
          //      return;

            var p = new TimedPopUp("", "Clicked :)", new TimeSpan(0, 0, 2));
            p.Show();
        }

        private void OnWave()
        {
            digitCanvas.Children.Clear();
            var popUp = new TimedPopUp("Touchless Controller", "Nice Wave!", new TimeSpan(0, 0, 2));
            popUp.Show();
        }

        //private void OnBack()
        //{
        //    var popUp = new TimedPopUp("Touchless Controller", "Back Detected!", new TimeSpan(0, 0, 2));
        //    popUp.Show();
        //}

        private void UpdateCursorPositionText(PXCMPoint3DF32 pos)
        {
            double height = VisualFeedbackGrid.ActualHeight;
            double width = VisualFeedbackGrid.ActualWidth;
            var p = new PXCMPoint3DF32((float)(width * pos.x), (float)(height * pos.y), pos.z);
            UpdateGestureText(String.Format("Gesture: Navigation [Hand at {0}]", p.ToFormattedString()));
        }

        private void UpdateGestureText(string s)
        {
            GestureLoggingTextBox.Text = s;
        }

        #endregion

        /*
         * The mouse related methods are available alongside TouchlessController
         * Mouse movement disables the visual feedback and takes back control instead of the hands
         */
        #region Mouse Related Methods (For additional user input)
        
        private void VisualFeedbackGrid_OnMouseMove(object sender, MouseEventArgs e)
        {
            Point p = new Point(e.GetPosition(VisualFeedbackGrid).X, e.GetPosition(VisualFeedbackGrid).Y);
            Dispatcher.BeginInvoke((Action)(() => UpdateGestureText(String.Format("Mouse at x: {0}, y: {1}", p.X, p.Y))));
        }

        private void TapButton_OnMouseClick(object sender, RoutedEventArgs e)
        {
            //Show a pop up that fades out
            var popUp = new TimedPopUp("Touchless Controller", "Clicked :)", new TimeSpan(0, 0, 2));
            popUp.Show();
        }



        #endregion

        bool clickStatus = false;
        Point currentPoint;
        private void digitCanvas_MouseMove(object sender, MouseEventArgs e)
        {            
            Interaction_Progress(e.GetPosition(sender as FrameworkElement));
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {            
            Interaction_Down(e.GetPosition(sender as FrameworkElement));
        }

        public void Interaction_Down(Point _point)
        {            
            currentPoint = _point;
            clickStatus = true;

            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            rect.Stroke = System.Windows.Media.Brushes.PapayaWhip;
            rect.StrokeThickness = 2;
            rect.Fill = System.Windows.Media.Brushes.White;
            rect.Width = digitCanvas.Width;
            rect.Height = digitCanvas.Height;

            digitCanvas.Children.Add(rect);            
        }
        public void Interaction_Progress(Point newPoint)
        {
            Line line = new Line();
            line.Stroke = System.Windows.Media.Brushes.Black;
            line.StrokeThickness = 35;
            
            line.X1 = currentPoint.X;
            line.Y1 = currentPoint.Y;
            line.X2 = newPoint.X;
            line.Y2 = newPoint.Y;    

            currentPoint = newPoint;

            if (clickStatus)
                digitCanvas.Children.Add(line);
        }

        public void Interaction_Up()
        {
            clickStatus = false;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)digitCanvas.Width,
                (int)digitCanvas.Height,
                96d, 96d,
                PixelFormats.Pbgra32);

   
            for (int i = 0; i < this.digitCanvas.Children.Count; i++)
                renderBitmap.Render(this.digitCanvas.Children[i]);
            
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            MemoryStream ms = new MemoryStream();
            encoder.Save(ms);


            System.Drawing.Image ImgOut = System.Drawing.Image.FromStream(ms);

            float[,,,] CNN_input = new float[1, 28, 28, 1];
            using (Bitmap bmp = new Bitmap(ImgOut))
            {
                Bitmap resized = new Bitmap(bmp, new System.Drawing.Size(28, 28));                
                
                for (int i = 0; i < resized.Width; i++)
                {
                    for (int j = 0; j < resized.Height; j++)
                    {
                        System.Drawing.Color pixel = resized.GetPixel(j, i);                        
                        float tmp = 1.0f - (float)( Convert.ToSingle(pixel.R) +
                                                    Convert.ToSingle(pixel.G) +
                                                    Convert.ToSingle(pixel.B)) / (3.0f * 255.0f);
                        CNN_input[0, i, j, 0] = tmp;
                    }
                }
            }


            using (var graph = new TFGraph())
            {

                graph.Import(File.ReadAllBytes("my_model.pb"), "");
                var session = new TFSession(graph);
                var runner = session.GetRunner();

                runner.AddInput(graph["conv2d_1_input"][0], new TFTensor(CNN_input));
                runner.Fetch(graph["dense_2/Softmax"][0]);

                var output = runner.Run();

                TFTensor result = output[0];

                var re = (float[,])result.GetValue();

                for (int i = 0; i < re.Length; i++)
                {
                    Console.WriteLine(re[0, i]);
                    _bar[i].Value = (int)(100 * re[0, i]);
                    _bar[i].AccVal = String.Format("{0:0.0}%", 100 * re[0, i]);
                }
                this.DataContext = new RecordCollection(_bar);
            }
        }

        private void digitCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
           Interaction_Up();            
        }

        private void outOfScreenBorder_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }



    class Bar
    {
        public string BarName { set; get; }
        public int Value { set; get; }
        public string AccVal { set; get; }
    }

    class RecordCollection : ObservableCollection<Record>
    {
        public RecordCollection(List<Bar> barvalues)
        {
            foreach (Bar barval in barvalues)
            {                
                Add(new Record(barval.Value, barval.AccVal, barval.BarName));
            }
        }

    }

    class Record : INotifyPropertyChanged
    {
        private System.Windows.Media.Brush _color;
        public System.Windows.Media.Brush Color {
            set {
                if (Data > 70)
                    _color = System.Windows.Media.Brushes.LimeGreen;
                else if (Data > 35)
                    _color = System.Windows.Media.Brushes.Orange;
                else
                    _color = System.Windows.Media.Brushes.PaleVioletRed;
            }
            get
            {
                return _color;
            }
        }

        public string Name { set; get; }

        public int Label_position {
            get;

            set;
        }

        private int _data;
        public int Data
        {
            set
            {
                if (_data != value)
                {
                    _data = value;
                }
            }
            get
            {
                return _data;
            }
        }

        private string _accurateVal;
        public string AccurateVal
        {
            set
            {
                _accurateVal = value;
            }
            get
            {
                return _accurateVal;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Record(int value, string av, string name)
        {
            Data = value;
            AccurateVal = av;
            Color = _color;
            Name = name;
            Label_position = -value-65;
        }

        protected void PropertyOnChange(string propname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propname));
            }
        }
    }
}

