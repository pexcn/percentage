using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace percentage
{
    class TrayIcon
    {
        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        private const int fontSize = 18;
        private const string font = "Microsoft YaHei";

        private NotifyIcon notifyIcon;
        private Font textFont;
        private Color themeColor;
        private String lastPercentage = string.Empty;
        private bool lastIsCharging = false;

        public TrayIcon()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();

            notifyIcon = new NotifyIcon();

            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem });

            menuItem.Click += new System.EventHandler(MenuItemClick);
            menuItem.Index = 0;
            menuItem.Text = "退出";

            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Visible = true;

            textFont = new Font(font, fontSize);
            themeColor = GetSystemThemeColor();

            Timer timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += new EventHandler(TimerTick);
            timer.Start();
        }

        private Color GetSystemThemeColor()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                bool isLightTheme = (int)key.GetValue("SystemUsesLightTheme") == 1;
                key.Close();
                return isLightTheme ? Color.Black : Color.White;
            }
            return Color.White;
        }

        private Bitmap GetTextBitmap(String text, Font font, Color fontColor)
        {
            SizeF imageSize = GetStringImageSize(text, font);
            Bitmap bitmap = new Bitmap((int)imageSize.Width, (int)imageSize.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(0, 0, 0, 0));
                using (Brush brush = new SolidBrush(fontColor))
                {
                    graphics.DrawString(text, font, brush, 0, 2.0F);
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.Save();
                }
            }
            return bitmap;
        }

        private static SizeF GetStringImageSize(string text, Font font)
        {
            using (Image image = new Bitmap(1, 1))
            using (Graphics graphics = Graphics.FromImage(image))
            return graphics.MeasureString(text, font);
        }

        private void MenuItemClick(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            int batteryPercentage = (int)Math.Round(powerStatus.BatteryLifePercent * 100);
            String percentage = batteryPercentage > 99 ? "FL" : batteryPercentage.ToString();
            bool isCharging = powerStatus.PowerLineStatus == PowerLineStatus.Online;

            if (percentage == lastPercentage && isCharging == lastIsCharging)
            {
                return;
            }
            lastPercentage = percentage;
            lastIsCharging = isCharging;

            UpdateTrayIcon(percentage, isCharging);
        }

        private void UpdateTrayIcon(string percentage, bool isCharging)
        {
            using (Bitmap bitmap = new Bitmap(GetTextBitmap(percentage, textFont, themeColor)))
            {
                System.IntPtr intPtr = bitmap.GetHicon();
                try
                {
                    if (notifyIcon.Icon != null)
                    {
                        notifyIcon.Icon.Dispose();
                    }
                    using (Icon icon = Icon.FromHandle(intPtr))
                    {
                        notifyIcon.Icon = icon;
                        String toolTipText = (isCharging ? "正在充电：" : "使用电池：") + percentage + "%";
                        notifyIcon.Text = toolTipText;
                    }
                }
                finally
                {
                    DestroyIcon(intPtr);
                }
            }
        }
    }
}
