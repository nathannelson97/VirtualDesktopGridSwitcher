using System;
using System.Diagnostics;
using WindowsDesktop.Interop;

namespace WindowsDesktop
{
	public static class VirtualDesktopHelper
	{
		internal static void ThrowIfNotSupported()
		{
			if (!VirtualDesktop.IsSupported)
			{
				throw new NotSupportedException("Need to include the app manifest in your project so as to target Windows 10. And, run without debugging.");
			}
		}


		public static bool IsCurrentVirtualDesktop(IntPtr handle)
		{
			ThrowIfNotSupported();

			return ComObjects.VirtualDesktopManager.IsWindowOnCurrentVirtualDesktop(handle);
		}

		public static void MoveToDesktop(IntPtr hWnd, VirtualDesktop virtualDesktop)
		{
			Console.WriteLine("MoveToDesktop 1");
			ThrowIfNotSupported();
			Console.WriteLine("MoveToDesktop 2");

			int processId;
			NativeMethods.GetWindowThreadProcessId(hWnd, out processId);

			Console.WriteLine("MoveToDesktop 3");

			if (Process.GetCurrentProcess().Id == processId)
			{
				Console.WriteLine("MoveToDesktop 4.1");
				var guid = virtualDesktop.Id;
				ComObjects.VirtualDesktopManager.MoveWindowToDesktop(hWnd, ref guid);
				Console.WriteLine("MoveToDesktop 4.2");
			}
			else
			{
				Console.WriteLine("MoveToDesktop 5.1");
				try
				{
					Console.WriteLine("MoveToDesktop 5.2");
					IntPtr view;
					Console.WriteLine("MoveToDesktop 5.3");
					ComObjects.ApplicationViewCollection.GetViewForHwnd(hWnd, out view);
					Console.WriteLine("MoveToDesktop 5.4");
					ComObjects.VirtualDesktopManagerInternal.MoveViewToDesktop(view, virtualDesktop.ComObject);
					Console.WriteLine("MoveToDesktop 5.5");
				}
				catch (System.Runtime.InteropServices.COMException ex)
				{
					Console.WriteLine("MoveToDesktop 6.1");
					if (ex.Match(HResult.TYPE_E_ELEMENTNOTFOUND))
					{
						Console.WriteLine("MoveToDesktop 6.2");
						throw new ArgumentException("hWnd");
					}
					Console.WriteLine("MoveToDesktop 6.3");
					throw;
				}
			}
		}

		public static bool IsPinnedWindow(IntPtr hWnd)
		{
			ThrowIfNotSupported();

			var view = hWnd.GetApplicationView();

			if (view == IntPtr.Zero)
			{
				throw new ArgumentException("hWnd");
			}

			return ComObjects.VirtualDesktopPinnedApps.IsViewPinned(view);
		}

		public static void PinWindow(IntPtr hWnd)
		{
			ThrowIfNotSupported();

			var view = hWnd.GetApplicationView();

			if (view == IntPtr.Zero)
			{
				throw new ArgumentException("hWnd");
			}

			if (!ComObjects.VirtualDesktopPinnedApps.IsViewPinned(view))
			{
				ComObjects.VirtualDesktopPinnedApps.PinView(view);
			}
		}

		public static void UnpinWindow(IntPtr hWnd)
		{
			ThrowIfNotSupported();

			var view = hWnd.GetApplicationView();

			if (view == IntPtr.Zero)
			{
				throw new ArgumentException("hWnd");
			}

			if (ComObjects.VirtualDesktopPinnedApps.IsViewPinned(view))
			{
				ComObjects.VirtualDesktopPinnedApps.UnpinView(view);
			}
		}

		public static void TogglePinWindow(IntPtr hWnd)
		{
			ThrowIfNotSupported();

			var view = hWnd.GetApplicationView();

			if (view == IntPtr.Zero)
			{
				throw new ArgumentException("hWnd");
			}

			if (ComObjects.VirtualDesktopPinnedApps.IsViewPinned(view))
			{
				ComObjects.VirtualDesktopPinnedApps.UnpinView(view);
			}
			else
			{
				ComObjects.VirtualDesktopPinnedApps.PinView(view);
			}
		}

		private static IntPtr GetApplicationView(this IntPtr hWnd)
		{
			try
			{
				IntPtr view;
				ComObjects.ApplicationViewCollection.GetViewForHwnd(hWnd, out view);
				return view;
			}
			catch (System.Runtime.InteropServices.COMException ex)
			{
                if (ex.Match(HResult.TYPE_E_ELEMENTNOTFOUND))
				    return IntPtr.Zero;
                throw;
			}
		}
	}
}
