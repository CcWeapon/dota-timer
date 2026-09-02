using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DotaTimer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TimerForm());
        }
    }

    public class TimerForm : Form
    {
        private readonly Label timeLabel;
        private readonly Label statusLabel;
        private readonly Button pinButton;
        private readonly Button startButton;
        private readonly Button resetButton;
        private readonly NumericUpDown minInput;
        private readonly NumericUpDown secInput;
        private readonly Timer uiTimer;
        private readonly ToolTip tips;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem trayClickThroughItem;
        private readonly ToolStripMenuItem trayMinute40BeepItem;
        private readonly ToolStripMenuItem trayRuneVoiceItem;
        private readonly ToolStripMenuItem trayXpVoiceItem;

        private bool running;
        private bool topMostEnabled = true;
        private bool clickThroughEnabled;
        private bool minute40BeepEnabled = true;
        private bool runeVoiceEnabled = true;
        private bool xpVoiceEnabled = true;
        private DateTime startedAt;
        private int initialSeconds;
        private int lastSecond = -1;
        private Point dragMouseStart;
        private Point dragFormStart;
        private bool dragging;

        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public TimerForm()
        {
            Text = "Dota Timer";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(520, 82);
            MinimumSize = new Size(420, 78);
            MaximumSize = new Size(570, 88);
            TopMost = topMostEnabled;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(20, 24, 28);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Opacity = 0.92;
            DoubleBuffered = true;

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 28;
            header.BackColor = Color.FromArgb(36, 42, 50);
            header.MouseDown += DragMouseDown;
            header.MouseMove += DragMouseMove;
            header.MouseUp += DragMouseUp;
            Controls.Add(header);

            Label title = new Label();
            title.Text = "Dota Timer";
            title.ForeColor = Color.FromArgb(245, 201, 95);
            title.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
            title.AutoSize = true;
            title.Location = new Point(10, 6);
            title.MouseDown += DragMouseDown;
            title.MouseMove += DragMouseMove;
            title.MouseUp += DragMouseUp;
            header.Controls.Add(title);

            pinButton = new Button();
            pinButton.Text = "钉";
            pinButton.FlatStyle = FlatStyle.Flat;
            pinButton.FlatAppearance.BorderSize = 0;
            pinButton.ForeColor = Color.White;
            pinButton.Size = new Size(30, 24);
            pinButton.Location = new Point(430, 2);
            pinButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pinButton.Click += delegate { ToggleTopMost(); };
            header.Controls.Add(pinButton);

            Button closeButton = new Button();
            closeButton.Text = "X";
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.BackColor = Color.FromArgb(70, 48, 48);
            closeButton.ForeColor = Color.White;
            closeButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            closeButton.Size = new Size(30, 24);
            closeButton.Location = new Point(464, 2);
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Click += delegate { Close(); };
            header.Controls.Add(closeButton);

            timeLabel = new Label();
            timeLabel.Text = "00:00";
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Font = new Font("Consolas", 27F, FontStyle.Bold, GraphicsUnit.Point);
            timeLabel.ForeColor = Color.FromArgb(110, 233, 183);
            timeLabel.Location = new Point(6, 29);
            timeLabel.Size = new Size(126, 48);
            timeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            timeLabel.MouseDown += DragMouseDown;
            timeLabel.MouseMove += DragMouseMove;
            timeLabel.MouseUp += DragMouseUp;
            Controls.Add(timeLabel);

            statusLabel = new Label();
            statusLabel.Text = "已置顶 | 可拖动窗口";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = Color.FromArgb(180, 190, 200);
            statusLabel.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.Location = new Point(142, 59);
            statusLabel.Size = new Size(360, 18);
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(statusLabel);

            Label startLabel = new Label();
            startLabel.Text = "初始时间";
            startLabel.AutoSize = true;
            startLabel.Location = new Point(142, 38);
            Controls.Add(startLabel);

            minInput = new NumericUpDown();
            minInput.Minimum = 0;
            minInput.Maximum = 999;
            minInput.Width = 50;
            minInput.Location = new Point(204, 34);
            minInput.BackColor = Color.FromArgb(28, 34, 40);
            minInput.ForeColor = Color.White;
            minInput.BorderStyle = BorderStyle.FixedSingle;
            minInput.ValueChanged += delegate { if (!running) UpdateTimeDisplay(GetInputSeconds()); };
            Controls.Add(minInput);

            Label colonLabel = new Label();
            colonLabel.Text = "分";
            colonLabel.AutoSize = true;
            colonLabel.Location = new Point(258, 38);
            Controls.Add(colonLabel);

            secInput = new NumericUpDown();
            secInput.Minimum = 0;
            secInput.Maximum = 59;
            secInput.Width = 46;
            secInput.Location = new Point(276, 34);
            secInput.BackColor = Color.FromArgb(28, 34, 40);
            secInput.ForeColor = Color.White;
            secInput.BorderStyle = BorderStyle.FixedSingle;
            secInput.ValueChanged += delegate { if (!running) UpdateTimeDisplay(GetInputSeconds()); };
            Controls.Add(secInput);

            Label secLabel = new Label();
            secLabel.Text = "秒";
            secLabel.AutoSize = true;
            secLabel.Location = new Point(326, 38);
            Controls.Add(secLabel);

            startButton = new Button();
            startButton.Text = "开始";
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.FlatAppearance.BorderColor = Color.FromArgb(110, 233, 183);
            startButton.BackColor = Color.FromArgb(24, 74, 61);
            startButton.ForeColor = Color.White;
            startButton.Size = new Size(58, 26);
            startButton.Location = new Point(350, 33);
            startButton.Click += StartButtonClick;
            Controls.Add(startButton);

            resetButton = new Button();
            resetButton.Text = "重置";
            resetButton.FlatStyle = FlatStyle.Flat;
            resetButton.FlatAppearance.BorderColor = Color.FromArgb(245, 201, 95);
            resetButton.BackColor = Color.FromArgb(78, 59, 26);
            resetButton.ForeColor = Color.White;
            resetButton.Size = new Size(58, 26);
            resetButton.Location = new Point(414, 33);
            resetButton.Click += ResetButtonClick;
            Controls.Add(resetButton);

            tips = new ToolTip();
            tips.SetToolTip(header, "按住拖动窗口");
            tips.SetToolTip(timeLabel, "按住拖动窗口");
            tips.SetToolTip(pinButton, "固定/取消固定在屏幕最上层");
            tips.SetToolTip(statusLabel, "提醒开关和鼠标穿透在右下角托盘图标右键菜单里");
            UpdatePinButton();

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示窗口");
            showItem.Click += delegate { ShowFromTray(); };
            ToolStripMenuItem resetPositionItem = new ToolStripMenuItem("重置窗口位置");
            resetPositionItem.Click += delegate { ResetWindowPosition(); };
            trayClickThroughItem = new ToolStripMenuItem("开启鼠标穿透");
            trayClickThroughItem.Click += delegate { SetClickThrough(!clickThroughEnabled); };
            trayMinute40BeepItem = new ToolStripMenuItem("40秒屯野语音");
            trayMinute40BeepItem.Click += delegate { ToggleMinute40Beep(); };
            trayRuneVoiceItem = new ToolStripMenuItem("神符语音：奇数分30秒");
            trayRuneVoiceItem.Click += delegate { ToggleRuneVoice(); };
            trayXpVoiceItem = new ToolStripMenuItem("经验符语音：每6分30秒");
            trayXpVoiceItem.Click += delegate { ToggleXpVoice(); };
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += delegate { Close(); };
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(resetPositionItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayClickThroughItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayMinute40BeepItem);
            trayMenu.Items.Add(trayRuneVoiceItem);
            trayMenu.Items.Add(trayXpVoiceItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "Dota Timer";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };
            UpdateTrayClickThroughText(false);
            UpdateTrayAlertTexts();

            uiTimer = new Timer();
            uiTimer.Interval = 200;
            uiTimer.Tick += UiTimerTick;
            uiTimer.Start();

            UpdateTimeDisplay(0);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(Color.FromArgb(245, 201, 95), 2))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            }
        }

        private void DragMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            dragging = true;
            dragMouseStart = ((Control)sender).PointToScreen(e.Location);
            dragFormStart = Location;
        }

        private void DragMouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point screen = ((Control)sender).PointToScreen(e.Location);
            Location = new Point(
                dragFormStart.X + screen.X - dragMouseStart.X,
                dragFormStart.Y + screen.Y - dragMouseStart.Y);
        }

        private void DragMouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void StartButtonClick(object sender, EventArgs e)
        {
            if (!running)
            {
                initialSeconds = CurrentSecondsForResume();
                startedAt = DateTime.Now;
                lastSecond = initialSeconds - 1;
                running = true;
                startButton.Text = "暂停";
                statusLabel.Text = "计时中 | 提醒开关在右下角菜单";
            }
            else
            {
                initialSeconds = GetElapsedSeconds();
                running = false;
                startButton.Text = "继续";
                statusLabel.Text = "已暂停";
                SetInputFromSeconds(initialSeconds);
            }
        }

        private void ResetButtonClick(object sender, EventArgs e)
        {
            running = false;
            startButton.Text = "开始";
            initialSeconds = GetInputSeconds();
            lastSecond = -1;
            UpdateTimeDisplay(initialSeconds);
            statusLabel.Text = "已重置，可设置初始时间";
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
            if (startButton.Text == "继续") return initialSeconds;
            return GetInputSeconds();
        }

        private int GetElapsedSeconds()
        {
            return initialSeconds + (int)Math.Floor((DateTime.Now - startedAt).TotalSeconds);
        }

        private int GetInputSeconds()
        {
            return ((int)minInput.Value * 60) + (int)secInput.Value;
        }

        private void SetInputFromSeconds(int total)
        {
            int minutes = Math.Min(999, Math.Max(0, total / 60));
            int seconds = Math.Max(0, total % 60);
            minInput.Value = minutes;
            secInput.Value = seconds;
        }

        private void UpdateTimeDisplay(int total)
        {
            int minutes = Math.Max(0, total / 60);
            int seconds = Math.Max(0, total % 60);
            timeLabel.Text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void CheckAlerts(int total)
        {
            int minute = total / 60;
            int second = total % 60;

            if (second == 40 && minute40BeepEnabled)
            {
                SpeakAsync("屯野");
                statusLabel.Text = "屯野提醒 " + FormatTime(total);
            }

            if (second == 30 && minute % 2 == 1 && runeVoiceEnabled)
            {
                SpeakAsync("神符");
                statusLabel.Text = "神符提醒 " + FormatTime(total);
            }

            if (second == 30 && minute > 0 && minute % 6 == 0 && xpVoiceEnabled)
            {
                SpeakAsync("经验符");
                statusLabel.Text = "经验符提醒 " + FormatTime(total);
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
                // SAPI flag 1 means asynchronous speech.
                voiceType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { text, 1 });
            }
            catch
            {
                PlayBeep();
            }
        }

        private void ApplyClickThrough(bool enabled)
        {
            int style = GetWindowLong(Handle, GWL_EXSTYLE);
            clickThroughEnabled = enabled;
            if (enabled)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
                statusLabel.Text = "鼠标穿透已开启；可从右下角托盘图标关闭";
            }
            else
            {
                SetWindowLong(Handle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
                statusLabel.Text = "鼠标穿透已关闭";
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

        private void ToggleMinute40Beep()
        {
            minute40BeepEnabled = !minute40BeepEnabled;
            UpdateTrayAlertTexts();
            statusLabel.Text = "40秒屯野语音：" + OnOff(minute40BeepEnabled);
        }

        private void ToggleRuneVoice()
        {
            runeVoiceEnabled = !runeVoiceEnabled;
            UpdateTrayAlertTexts();
            statusLabel.Text = "神符语音：" + OnOff(runeVoiceEnabled);
        }

        private void ToggleXpVoice()
        {
            xpVoiceEnabled = !xpVoiceEnabled;
            UpdateTrayAlertTexts();
            statusLabel.Text = "经验符语音：" + OnOff(xpVoiceEnabled);
        }

        private void UpdateTrayAlertTexts()
        {
            if (trayMinute40BeepItem == null) return;
            trayMinute40BeepItem.Checked = minute40BeepEnabled;
            trayRuneVoiceItem.Checked = runeVoiceEnabled;
            trayXpVoiceItem.Checked = xpVoiceEnabled;
            trayMinute40BeepItem.Text = "40秒屯野语音：" + OnOff(minute40BeepEnabled);
            trayRuneVoiceItem.Text = "神符语音：奇数分30秒 " + OnOff(runeVoiceEnabled);
            trayXpVoiceItem.Text = "经验符语音：每6分30秒 " + OnOff(xpVoiceEnabled);
        }

        private static string OnOff(bool enabled)
        {
            return enabled ? "开" : "关";
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            TopMost = topMostEnabled;
            Activate();
        }

        private void ToggleTopMost()
        {
            topMostEnabled = !topMostEnabled;
            TopMost = topMostEnabled;
            UpdatePinButton();
            statusLabel.Text = topMostEnabled ? "已固定在屏幕最上层" : "已取消最上层固定";
        }

        private void UpdatePinButton()
        {
            if (pinButton == null) return;
            pinButton.BackColor = topMostEnabled ? Color.FromArgb(126, 86, 22) : Color.FromArgb(62, 68, 76);
            pinButton.Text = "钉";
        }

        private void ResetWindowPosition()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 24, area.Top + 80);
            ShowFromTray();
            statusLabel.Text = "窗口位置已重置";
        }
    }
}
