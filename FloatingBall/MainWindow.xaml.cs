﻿﻿﻿﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.IO;
﻿﻿﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;




namespace FloatingBall
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private System.Windows.Point touchStartPosition;
        private double screenWidth;

        public MainWindow()
        {
            screenRecorder = new ScreenRecorder();
            InitializeComponent();
            screenWidth = SystemParameters.PrimaryScreenWidth;
            this.ShowInTaskbar = false;
            this.Topmost = true;

            var helper = new WindowInteropHelper(this);
            int exStyle = (int)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_TOOLWINDOW));

            this.Deactivated += (s, e) => SetTransparency(true);
            this.Activated += (s, e) => SetTransparency(false);
            this.SourceInitialized += Window_SourceInitialized;

            // Start the timer to ensure Always-On-Top
            DispatcherTimer alwaysOnTopTimer = new DispatcherTimer();
            alwaysOnTopTimer.Interval = TimeSpan.FromSeconds(2);
            alwaysOnTopTimer.Tick += (s, e) => SetAlwaysOnTop();
            alwaysOnTopTimer.Start();
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private void SetTransparency(bool transparent)
        {
            WindowInteropHelper h = new WindowInteropHelper(this);
            int style = GetWindowLong(h.Handle, GWL_EXSTYLE);
            SetWindowLong(h.Handle, GWL_EXSTYLE, (IntPtr)(transparent ? (style | WS_EX_TRANSPARENT) : (style & ~WS_EX_TRANSPARENT)));
            this.Opacity = transparent ? 0.3 : 1;
        }

        private void SetAlwaysOnTop()
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
        }

        private void EllipseBall_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ShowMenu();
            else if (e.ClickCount == 1)
                DragMoveSafely();
        }

        private void DragMoveSafely()
        {
            try { DragMove(); }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void EllipseBall_TouchDown(object sender, TouchEventArgs e)
        {
            isDragging = true;
            touchStartPosition = e.GetTouchPoint(this).Position;
        }

        private void EllipseBall_TouchMove(object sender, TouchEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPos = e.GetTouchPoint(this).Position;
                Left += currentPos.X - touchStartPosition.X;
                Top += currentPos.Y - touchStartPosition.Y;
            }
        }

        private void EllipseBall_TouchUp(object sender, TouchEventArgs e)
        {
            isDragging = false;
            MoveToSide();
        }

        private void MoveToSide()
        {
            double moveToX = (Left + Width / 2) < (screenWidth / 2) ? 10 : screenWidth - Width - 10;
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
            timer.Tick += (s, e) =>
            {
                if (Math.Abs(Left - moveToX) < 5)
                {
                    Left = moveToX;
                    timer.Stop();
                }
                else
                {
                    Left += (moveToX - Left) / 10;
                }
            };
            timer.Start();
        }

        private void ShowMenu()
        {
            ContextMenu menu = new ContextMenu();
            menu.Items.Add(CreateMenuItem("📸 Screenshot", TakeScreenshot));
            menu.Items.Add(CreateMenuItem("🎥 Screen Record", StartStopScreenRecord));
            menu.Items.Add(CreateMenuItem("🌍 Open Google", () => Process.Start(new ProcessStartInfo("https://www.google.com") { UseShellExecute = true })));
            menu.Items.Add(CreateMenuItem("⚙ Open Settings", OpenSettings));
            menu.Items.Add(CreateMenuItem("🔔 Open Notification Bar", OpenNotificationBar));
            menu.Items.Add(CreateMenuItem("❌ Exit", () => Application.Current.Shutdown()));
            this.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private MenuItem CreateMenuItem(string header, Action action)
        {
            MenuItem item = new MenuItem { Header = header };
            item.Click += (s, e) => action();
            return item;
        }

        private void TakeScreenshot()
        {
            keybd_event(0x2C, 0, 0, 0); // Press PrintScreen key
            keybd_event(0x2C, 0, 2, 0); // Release PrintScreen key
        }

        private ScreenRecorder screenRecorder;
        private bool isRecording = false;

        private void StartStopScreenRecord()
        {
            try
            {
                if (!isRecording)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), $"recording_{timestamp}.mp4");
                    screenRecorder.StartRecording(outputPath);
                    isRecording = true;
                    MessageBox.Show($"Screen recording started!\nSaving to: {outputPath}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    screenRecorder.StopRecording();
                    isRecording = false;
                    MessageBox.Show("Screen recording stopped!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Screen recording error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings()
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:home") { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Failed to open settings: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void OpenNotificationBar()
        {
            keybd_event(0x5B, 0, 0, 0); // Press Left Windows Key
            keybd_event(0x4E, 0, 0, 0); // Press N Key
            keybd_event(0x4E, 0, 2, 0); // Release N Key
            keybd_event(0x5B, 0, 2, 0); // Release Left Windows Key
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            SetAlwaysOnTop();
        }

        private void EllipseBall_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Activate();
        }
    }
}
