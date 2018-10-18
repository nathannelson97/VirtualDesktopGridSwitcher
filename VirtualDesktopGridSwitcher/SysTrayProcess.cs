using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VirtualDesktopGridSwitcher.Settings;
using WindowsDesktop;
using System.Drawing.Imaging;
using System.Threading;

delegate bool WindowEnumCallbackProc(IntPtr hwnd, int holder);

namespace VirtualDesktopGridSwitcher {
    class SysTrayProcess : IDisposable {

        internal SettingValues settings;

        private NotifyIcon notifyIcon;
        internal Dictionary<VirtualDesktop, int> desktopIdLookup;
        private int currentDesktopIndex = 0;

        private HashSet<int> occupiedDesktops = new HashSet<int>();
        private HashSet<int> loadingOccupiedDesktops = new HashSet<int>();

        private volatile bool updateThreadRunning = false;

        private ContextMenus contextMenu;

        public SysTrayProcess(SettingValues settings) {
            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Text = "Virtual Desktop Grid Switcher";

            contextMenu = new ContextMenus(settings);
            notifyIcon.ContextMenuStrip = contextMenu.MenuStrip;
        }

        public void Dispose() {
            notifyIcon.Dispose();
        }

        private void RefreshIcon()
        {
            int lineThickness = 1;
            int cellSize = 4;
            int iconWidth = cellSize * settings.Columns + 2 * lineThickness;
            int iconHeight = cellSize * settings.Rows + 2 * lineThickness;

            Bitmap bmp = new Bitmap(iconWidth, iconHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Brush brush = Brushes.DarkGray;
                Brush brush = Brushes.White;

                // Draw the outline.
                g.FillRectangle(brush, 0, 0, lineThickness, iconHeight);
                g.FillRectangle(brush, iconWidth - lineThickness, 0, lineThickness, iconHeight);
                g.FillRectangle(brush, 0, 0, iconWidth, lineThickness);
                g.FillRectangle(brush, 0, iconHeight - lineThickness, iconWidth, lineThickness);

                brush = new SolidBrush(Color.FromArgb(0xa0, 0xff, 0xff, 0xff));

                // Fill (with a different color) the cells that represent desktops with windows on them.
                for (int col = 0; col < settings.Columns; ++col)
                {
                    for (int row = 0; row < settings.Columns; ++row)
                    {
                        int index = (row * settings.Columns) + col;
                        if (index != currentDesktopIndex && occupiedDesktops.Contains(index))
                        {
                            g.FillRectangle(brush, col * cellSize + lineThickness, row * cellSize + lineThickness, cellSize, cellSize);
                        }
                    }
                }

                brush = Brushes.White;

                // Fill the cell that represents the current desktop.
                g.FillRectangle(brush, ColumnOf(currentDesktopIndex) * cellSize + lineThickness, RowOf(currentDesktopIndex) * cellSize + lineThickness, cellSize, cellSize);
            }

            notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
        }

        public void ShowIconForDesktop(int desktopIndex)
        {
            currentDesktopIndex = desktopIndex;

            RefreshIcon();

            if (!updateThreadRunning)
            {
                updateThreadRunning = true;
                // Start thread to refresh occupied desktops.
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    RefreshOccupiedDesktops();

                    updateThreadRunning = false;
                }).Start();
            }
        }

        private void RefreshOccupiedDesktops()
        {
            loadingOccupiedDesktops = new HashSet<int>();
            EnumDesktopWindows(IntPtr.Zero, WindowEnumCallback, 0);
            occupiedDesktops = loadingOccupiedDesktops;
            RefreshIcon();
        }

        [DllImport("user32.dll")]
        protected static extern bool EnumDesktopWindows(IntPtr hDesktop, WindowEnumCallbackProc enumProc, int holder);

        protected bool WindowEnumCallback(IntPtr hwnd, int holder)
        {
            if (desktopIdLookup == null)
            {
                return false;
            }
            var desktop = VirtualDesktop.FromHwnd(hwnd);
            if (desktop != null)
            {
                int desktopId = desktopIdLookup[desktop];
                loadingOccupiedDesktops.Add(desktopId);
            }
            return true;
        }

        private int ColumnOf(int index)
        {
            return ((index + settings.Columns) % settings.Columns);
        }

        private int RowOf(int index)
        {
            return ((index / settings.Columns) + settings.Columns) % settings.Columns;
        }
    }
}
