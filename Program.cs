using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DotaTimer
{
    public class AppEntry : Application
    {
        [STAThread]
        public static void Main()
        {
            AppEntry app = new AppEntry();
            app.Run(new TimerWindow());
        }
    }

    public class TimerWindow : Window
    {
        private readonly Border outerBorder;
        private readonly Border headerBorder;
        private readonly TextBlock timeText;
        private readonly TextBlock statusText;
        private readonly Button minimizeButton;
        private readonly Button pinButton;
        private readonly Button startButton;
        private readonly Button resetButton;
        private readonly TextBox minInput;
        private readonly TextBox secInput;
        private readonly Slider opacitySlider;
        private readonly DispatcherTimer uiTimer;
        private readonly Forms.NotifyIcon trayIcon;
        private readonly Forms.ToolStripMenuItem trayClickThroughItem;
        private readonly Forms.ToolStripMenuItem trayMinute40VoiceItem;
        private readonly Forms.ToolStripMenuItem trayRuneVoiceItem;
        private readonly Forms.ToolStripMenuItem trayXpVoiceItem;

        private bool running;
        private bool topMostEnabled = true;
        private bool clickThroughEnabled;
        private bool minute40VoiceEnabled = true;
        private bool runeVoiceEnabled = true;
        private bool xpVoiceEnabled = true;
        private DateTime startedAt;
        private int initialSeconds;
        private int lastSecond = -1;

        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public TimerWindow()
        {
            Title = "Dota Timer";
            Width = 520;
            Height = 82;
            MinWidth = 420;
            MinHeight = 78;
            MaxWidth = 570;
            MaxHeight = 88;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = topMostEnabled;
            ShowInTaskbar = true;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            FontSize = 12;

            outerBorder = new Border();
            outerBorder.CornerRadius = new CornerRadius(4);
            outerBorder.BorderThickness = new Thickness(2);
            outerBorder.BorderBrush = Brushes.Transparent;
            Content = outerBorder;

            Canvas canvas = new Canvas();
            outerBorder.Child = canvas;

            headerBorder = new Border();
            headerBorder.Height = 28;
            headerBorder.Width = Width;
            headerBorder.CornerRadius = new CornerRadius(4, 4, 0, 0);
            headerBorder.MouseLeftButtonDown += DragWindow;
            canvas.Children.Add(headerBorder);
            Canvas.SetLeft(headerBorder, 0);
            Canvas.SetTop(headerBorder, 0);

            TextBlock title = new TextBlock();
            title.Text = "Dota Timer";
            title.Foreground = new SolidColorBrush(Color.FromRgb(245, 201, 95));
            title.FontFamily = new FontFamily("Segoe UI Semibold");
            title.FontWeight = FontWeights.Bold;
            title.FontSize = 12;
            title.MouseLeftButtonDown += DragWindow;
            canvas.Children.Add(title);
            Canvas.SetLeft(title, 10);
            Canvas.SetTop(title, 6);

            TextBlock opacityLabel = new TextBlock();
            opacityLabel.Text = "透明";
            opacityLabel.Foreground = new SolidColorBrush(Color.FromRgb(210, 216, 224));
            opacityLabel.FontSize = 12;
            opacityLabel.MouseLeftButtonDown += DragWindow;
            canvas.Children.Add(opacityLabel);
            Canvas.SetLeft(opacityLabel, 118);
            Canvas.SetTop(opacityLabel, 6);

            opacitySlider = new Slider();
            opacitySlider.Minimum = 45;
            opacitySlider.Maximum = 100;
            opacitySlider.Value = 92;
            opacitySlider.Width = 120;
            opacitySlider.Height = 18;
            opacitySlider.TickFrequency = 5;
            opacitySlider.IsSnapToTickEnabled = false;
            opacitySlider.ValueChanged += delegate { ApplyBackgroundOpacity(); };
            canvas.Children.Add(opacitySlider);
            Canvas.SetLeft(opacitySlider, 156);
            Canvas.SetTop(opacitySlider, 5);

            minimizeButton = MakeHeaderButton("_", Color.FromRgb(62, 68, 76));
            minimizeButton.Click += delegate { MinimizeToTray(); };
            canvas.Children.Add(minimizeButton);
            Canvas.SetLeft(minimizeButton, 396);
            Canvas.SetTop(minimizeButton, 2);

            pinButton = MakeHeaderButton("钉", Color.FromRgb(126, 86, 22));
            pinButton.Click += delegate { ToggleTopMost(); };
            canvas.Children.Add(pinButton);
            Canvas.SetLeft(pinButton, 430);
            Canvas.SetTop(pinButton, 2);

            Button closeButton = MakeHeaderButton("X", Color.FromRgb(70, 48, 48));
            closeButton.Click += delegate { Close(); };
            canvas.Children.Add(closeButton);
            Canvas.SetLeft(closeButton, 464);
            Canvas.SetTop(closeButton, 2);

            timeText = new TextBlock();
            timeText.Text = "00:00";
            timeText.Foreground = new SolidColorBrush(Color.FromRgb(110, 233, 183));
            timeText.FontFamily = new FontFamily("Consolas");
            timeText.FontWeight = FontWeights.Bold;
            timeText.FontSize = 36;
            timeText.Width = 126;
            timeText.Height = 48;
            timeText.TextAlignment = TextAlignment.Center;
            timeText.MouseLeftButtonDown += DragWindow;
            canvas.Children.Add(timeText);
            Canvas.SetLeft(timeText, 6);
            Canvas.SetTop(timeText, 28);

            TextBlock startLabel = new TextBlock();
            startLabel.Text = "初始时间";
            startLabel.Foreground = Brushes.White;
            startLabel.FontSize = 12;
            canvas.Children.Add(startLabel);
            Canvas.SetLeft(startLabel, 142);
            Canvas.SetTop(startLabel, 38);

            minInput = MakeTimeInput("0", 50);
            canvas.Children.Add(minInput);
            Canvas.SetLeft(minInput, 204);
            Canvas.SetTop(minInput, 33);

            TextBlock minuteLabel = MakeSmallLabel("分");
            canvas.Children.Add(minuteLabel);
            Canvas.SetLeft(minuteLabel, 258);
            Canvas.SetTop(minuteLabel, 38);

            secInput = MakeTimeInput("0", 46);
            canvas.Children.Add(secInput);
            Canvas.SetLeft(secInput, 276);
            Canvas.SetTop(secInput, 33);

            TextBlock secondLabel = MakeSmallLabel("秒");
            canvas.Children.Add(secondLabel);
            Canvas.SetLeft(secondLabel, 326);
            Canvas.SetTop(secondLabel, 38);

            startButton = MakeMainButton("开始", Color.FromRgb(24, 74, 61), Color.FromRgb(110, 233, 183));
            startButton.Click += StartButtonClick;
            canvas.Children.Add(startButton);
            Canvas.SetLeft(startButton, 350);
            Canvas.SetTop(startButton, 32);

            resetButton = MakeMainButton("重置", Color.FromRgb(78, 59, 26), Color.FromRgb(245, 201, 95));
            resetButton.Click += ResetButtonClick;
            canvas.Children.Add(resetButton);
            Canvas.SetLeft(resetButton, 414);
            Canvas.SetTop(resetButton, 32);

            statusText = new TextBlock();
            statusText.Text = "已置顶 | 可拖动窗口";
            statusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 190, 200));
            statusText.FontSize = 11;
            statusText.Width = 360;
            statusText.Height = 18;
            canvas.Children.Add(statusText);
            Canvas.SetLeft(statusText, 142);
            Canvas.SetTop(statusText, 59);

            ToolTipService.SetToolTip(opacitySlider, "拖动调整背景透明度，不影响按钮和时间显示");
            ToolTipService.SetToolTip(minimizeButton, "最小化到右下角托盘，计时和提醒会继续运行");
            ToolTipService.SetToolTip(pinButton, "固定/取消固定在屏幕最上层");
            ToolTipService.SetToolTip(statusText, "提醒开关和鼠标穿透在右下角托盘图标右键菜单里");

            outerBorder.MouseEnter += delegate { outerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 201, 95)); };
            outerBorder.MouseLeave += delegate { outerBorder.BorderBrush = Brushes.Transparent; };
            ApplyBackgroundOpacity();

            Forms.ContextMenuStrip trayMenu = new Forms.ContextMenuStrip();
            Forms.ToolStripMenuItem showItem = new Forms.ToolStripMenuItem("显示窗口");
            showItem.Click += delegate { ShowFromTray(); };
            Forms.ToolStripMenuItem resetPositionItem = new Forms.ToolStripMenuItem("重置窗口位置");
            resetPositionItem.Click += delegate { ResetWindowPosition(); };
            trayClickThroughItem = new Forms.ToolStripMenuItem("开启鼠标穿透");
            trayClickThroughItem.Click += delegate { SetClickThrough(!clickThroughEnabled); };
            trayMinute40VoiceItem = new Forms.ToolStripMenuItem("40秒屯野语音");
            trayMinute40VoiceItem.Click += delegate { ToggleMinute40Voice(); };
            trayRuneVoiceItem = new Forms.ToolStripMenuItem("神符语音：奇数分30秒");
            trayRuneVoiceItem.Click += delegate { ToggleRuneVoice(); };
            trayXpVoiceItem = new Forms.ToolStripMenuItem("经验符语音：每6分30秒");
            trayXpVoiceItem.Click += delegate { ToggleXpVoice(); };
            Forms.ToolStripMenuItem exitItem = new Forms.ToolStripMenuItem("退出");
            exitItem.Click += delegate { Close(); };
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(resetPositionItem);
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add(trayClickThroughItem);
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add(trayMinute40VoiceItem);
            trayMenu.Items.Add(trayRuneVoiceItem);
            trayMenu.Items.Add(trayXpVoiceItem);
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new Forms.NotifyIcon();
            trayIcon.Icon = Drawing.SystemIcons.Application;
            trayIcon.Text = "Dota Timer";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };
            UpdateTrayClickThroughText(false);
            UpdateTrayAlertTexts();

            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(200);
            uiTimer.Tick += UiTimerTick;
            uiTimer.Start();

            UpdateTimeDisplay(0);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private Button MakeHeaderButton(string text, Color background)
        {
            Button button = new Button();
            button.Content = text;
            button.Width = 30;
            button.Height = 24;
            button.Foreground = Brushes.White;
            button.Background = new SolidColorBrush(background);
            button.BorderThickness = new Thickness(0);
            button.FontFamily = new FontFamily("Segoe UI");
            button.FontWeight = FontWeights.Bold;
            button.FontSize = 12;
            return button;
        }

        private Button MakeMainButton(string text, Color background, Color border)
        {
            Button button = new Button();
            button.Content = text;
            button.Width = 58;
            button.Height = 26;
            button.Foreground = Brushes.White;
            button.Background = new SolidColorBrush(background);
            button.BorderBrush = new SolidColorBrush(border);
            button.BorderThickness = new Thickness(1);
            button.FontFamily = new FontFamily("Microsoft YaHei UI");
            button.FontSize = 12;
            return button;
        }

        private TextBox MakeTimeInput(string text, double width)
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.Width = width;
            box.Height = 24;
            box.Foreground = Brushes.White;
            box.Background = new SolidColorBrush(Color.FromRgb(28, 34, 40));
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(74, 84, 96));
            box.HorizontalContentAlignment = HorizontalAlignment.Right;
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.FontSize = 12;
            box.PreviewTextInput += DigitsOnly;
            box.TextChanged += delegate { if (!running) UpdateTimeDisplay(GetInputSeconds()); };
            return box;
        }

        private TextBlock MakeSmallLabel(string text)
        {
            TextBlock label = new TextBlock();
            label.Text = text;
            label.Foreground = Brushes.White;
            label.FontSize = 12;
            return label;
        }

        private void DigitsOnly(object sender, TextCompositionEventArgs e)
        {
            int ignored;
            e.Handled = !int.TryParse(e.Text, out ignored);
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            try { DragMove(); } catch { }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (!running)
            {
                initialSeconds = CurrentSecondsForResume();
                startedAt = DateTime.Now;
                lastSecond = initialSeconds - 1;
                running = true;
                startButton.Content = "暂停";
                statusText.Text = "计时中 | 提醒开关在右下角菜单";
            }
            else
            {
                initialSeconds = GetElapsedSeconds();
                running = false;
                startButton.Content = "继续";
                statusText.Text = "已暂停";
                SetInputFromSeconds(initialSeconds);
            }
        }

        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            running = false;
            startButton.Content = "开始";
            initialSeconds = GetInputSeconds();
            lastSecond = -1;
            UpdateTimeDisplay(initialSeconds);
            statusText.Text = "已重置，可设置初始时间";
        }

        private void UiTimerTick(object sender, EventArgs e)
        {
            int total = running ? GetElapsedSeconds() : GetInputSeconds();
            UpdateTimeDisplay(total);

            if (running && total != lastSecond)
            {
                lastSecond = total;
                CheckAlerts(total);
            }
        }

        private int CurrentSecondsForResume()
        {
            if (startButton.Content != null && startButton.Content.ToString() == "继续") return initialSeconds;
            return GetInputSeconds();
        }

        private int GetElapsedSeconds()
        {
            return initialSeconds + (int)Math.Floor((DateTime.Now - startedAt).TotalSeconds);
        }

        private int GetInputSeconds()
        {
            int minutes = ParseInput(minInput, 0, 999);
            int seconds = ParseInput(secInput, 0, 59);
            return minutes * 60 + seconds;
        }

        private int ParseInput(TextBox box, int min, int max)
        {
            int value;
            if (!int.TryParse(box.Text, out value)) value = min;
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        private void SetInputFromSeconds(int total)
        {
            int minutes = Math.Min(999, Math.Max(0, total / 60));
            int seconds = Math.Max(0, total % 60);
            minInput.Text = minutes.ToString();
            secInput.Text = seconds.ToString();
        }

        private void UpdateTimeDisplay(int total)
        {
            int minutes = Math.Max(0, total / 60);
            int seconds = Math.Max(0, total % 60);
            timeText.Text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void CheckAlerts(int total)
        {
            int minute = total / 60;
            int second = total % 60;

            if (second == 40 && minute40VoiceEnabled)
            {
                SpeakAsync("屯野");
                statusText.Text = "屯野提醒 " + FormatTime(total);
            }

            if (second == 30 && minute % 2 == 1 && runeVoiceEnabled)
            {
                SpeakAsync("神符");
                statusText.Text = "神符提醒 " + FormatTime(total);
            }

            if (second == 30 && minute > 0 && minute % 6 == 0 && xpVoiceEnabled)
            {
                SpeakAsync("经验符");
                statusText.Text = "经验符提醒 " + FormatTime(total);
            }
        }

        private static string FormatTime(int total)
        {
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static void PlayBeep()
        {
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
                try { Console.Beep(950, 120); } catch { }
            }
        }

        private static void SpeakAsync(string text)
        {
            try
            {
                Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (voiceType == null) return;
                object voice = Activator.CreateInstance(voiceType);
                voiceType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { text, 1 });
            }
            catch
            {
                PlayBeep();
            }
        }

        private void ApplyClickThrough(bool enabled)
        {
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int style = GetWindowLong(handle, GWL_EXSTYLE);
            clickThroughEnabled = enabled;
            if (enabled)
            {
                SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
                statusText.Text = "鼠标穿透已开启；可从右下角托盘图标关闭";
            }
            else
            {
                SetWindowLong(handle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
                statusText.Text = "鼠标穿透已关闭";
            }
            UpdateTrayClickThroughText(enabled);
        }

        private void SetClickThrough(bool enabled)
        {
            ApplyClickThrough(enabled);
        }

        private void UpdateTrayClickThroughText(bool enabled)
        {
            if (trayClickThroughItem == null) return;
            trayClickThroughItem.Checked = enabled;
            trayClickThroughItem.Text = enabled ? "关闭鼠标穿透" : "开启鼠标穿透";
        }

        private void ToggleMinute40Voice()
        {
            minute40VoiceEnabled = !minute40VoiceEnabled;
            UpdateTrayAlertTexts();
            statusText.Text = "40秒屯野语音：" + OnOff(minute40VoiceEnabled);
        }

        private void ToggleRuneVoice()
        {
            runeVoiceEnabled = !runeVoiceEnabled;
            UpdateTrayAlertTexts();
            statusText.Text = "神符语音：" + OnOff(runeVoiceEnabled);
        }

        private void ToggleXpVoice()
        {
            xpVoiceEnabled = !xpVoiceEnabled;
            UpdateTrayAlertTexts();
            statusText.Text = "经验符语音：" + OnOff(xpVoiceEnabled);
        }

        private void UpdateTrayAlertTexts()
        {
            if (trayMinute40VoiceItem == null) return;
            trayMinute40VoiceItem.Checked = minute40VoiceEnabled;
            trayRuneVoiceItem.Checked = runeVoiceEnabled;
            trayXpVoiceItem.Checked = xpVoiceEnabled;
            trayMinute40VoiceItem.Text = "40秒屯野语音：" + OnOff(minute40VoiceEnabled);
            trayRuneVoiceItem.Text = "神符语音：奇数分30秒 " + OnOff(runeVoiceEnabled);
            trayXpVoiceItem.Text = "经验符语音：每6分30秒 " + OnOff(xpVoiceEnabled);
        }

        private static string OnOff(bool enabled)
        {
            return enabled ? "开" : "关";
        }

        private void ApplyBackgroundOpacity()
        {
            if (outerBorder == null || headerBorder == null || opacitySlider == null) return;
            byte alpha = (byte)Math.Max(0, Math.Min(255, (int)(opacitySlider.Value * 255 / 100)));
            outerBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, 20, 24, 28));
            headerBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, 36, 42, 50));
            statusText.Text = "背景透明度：" + ((int)opacitySlider.Value) + "%";
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Topmost = topMostEnabled;
            Activate();
        }

        private void MinimizeToTray()
        {
            Hide();
            if (trayIcon != null)
            {
                trayIcon.BalloonTipTitle = "Dota Timer";
                trayIcon.BalloonTipText = "已最小化到右下角，计时和提醒会继续运行。";
                trayIcon.ShowBalloonTip(1200);
            }
        }

        private void ToggleTopMost()
        {
            topMostEnabled = !topMostEnabled;
            Topmost = topMostEnabled;
            UpdatePinButton();
            statusText.Text = topMostEnabled ? "已固定在屏幕最上层" : "已取消最上层固定";
        }

        private void UpdatePinButton()
        {
            if (pinButton == null) return;
            pinButton.Background = new SolidColorBrush(topMostEnabled ? Color.FromRgb(126, 86, 22) : Color.FromRgb(62, 68, 76));
            pinButton.Content = "钉";
        }

        private void ResetWindowPosition()
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - Width - 24;
            Top = area.Top + 80;
            ShowFromTray();
            statusText.Text = "窗口位置已重置";
        }
    }
}
