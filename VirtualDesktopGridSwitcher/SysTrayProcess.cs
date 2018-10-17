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

delegate bool WindowEnumCallbackProc(IntPtr hwnd, int holder);

namespace VirtualDesktopGridSwitcher {
    class SysTrayProcess : IDisposable {

        private NotifyIcon notifyIcon;
        private HashSet<int> occupiedDesktops;
        private Dictionary<VirtualDesktop, int> desktopIdLookup;

        private ContextMenus contextMenu;

        public SysTrayProcess(SettingValues settings) {
            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Text = "Virtual Desktop Grid Switcher";

            contextMenu = new ContextMenus(settings);
            notifyIcon.ContextMenuStrip = contextMenu.MenuStrip;

            occupiedDesktops = new HashSet<int>();
        }

        public void Dispose() {
            notifyIcon.Dispose();
        }


        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(WindowEnumCallbackProc enumProc, int holder);

        protected bool WindowEnumCallback(IntPtr hwnd, int holder)
        {
            var desktop = VirtualDesktop.FromHwnd(hwnd);
            if (desktop != null)
            {
                int desktopId = desktopIdLookup[desktop];
                occupiedDesktops.Add(desktopId);
            }
            return true;
        }


        public void ShowIconForDesktop(int desktopIndex, Dictionary<VirtualDesktop, int> desktopIdLookupIn,
                int gridWidth, int gridHeight, int desktopCol, int desktopRow) {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            occupiedDesktops.Clear();
            desktopIdLookup = desktopIdLookupIn;
            EnumWindows(WindowEnumCallback, 0);

            int lineThickness = 1;
            int cellWidth = 4;
            int cellLinedWidth = (cellWidth + lineThickness);
            int iconWidth = cellLinedWidth * gridWidth + lineThickness;
            int iconHeight = cellLinedWidth * gridHeight + lineThickness;

            Bitmap bmp = new Bitmap(iconWidth, iconHeight, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Brush brush = Brushes.White;

                // Draw the outline.
                g.FillRectangle(brush, 0, 0, lineThickness, iconHeight);
                g.FillRectangle(brush, iconWidth - lineThickness, 0, lineThickness, iconHeight);
                g.FillRectangle(brush, 0, 0, iconWidth, lineThickness);
                g.FillRectangle(brush, 0, iconHeight - lineThickness, iconWidth, lineThickness);

                // Draw the grid.
                for (int col = 1; col < gridWidth; ++col)
                {
                    g.FillRectangle(brush, col * cellLinedWidth, 0, lineThickness, iconHeight);
                }
                for (int row = 1; row < gridWidth; ++row)
                {
                    g.FillRectangle(brush, 0, row * cellLinedWidth, iconWidth, lineThickness);
                }

                // Fill the cell that represents the current desktop.
                g.FillRectangle(brush, desktopCol * cellLinedWidth + lineThickness, desktopRow * cellLinedWidth + lineThickness, cellWidth, cellWidth);

                brush = Brushes.Gray;

                // Fill (with a different color) the cells that represent desktops with windows on them.
                for(int col = 0; col < gridWidth; ++col)
                {
                    for(int row = 0; row < gridHeight; ++row)
                    {
                        int index = (row * gridWidth) + col;
                        if(index != desktopIndex && occupiedDesktops.Contains(index))
                        {
                            g.FillRectangle(brush, col * cellLinedWidth + lineThickness, row * cellLinedWidth + lineThickness, cellWidth, cellWidth);
                        }
                    }
                }
            }

            notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());

            watch.Stop();
            long elapsedMS = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMS);
        }
    }
}
