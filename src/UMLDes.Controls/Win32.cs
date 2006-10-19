// originally from http://www.codeproject.com/cs/miscctrl/FlatMenuForm.asp

using System;
using System.Runtime.InteropServices;

namespace UMLDes.Controls {

	public class Win32 	{
		#region Device context

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr GetWindowDC(IntPtr handle);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr GetDCEx(IntPtr handle, IntPtr clip, int flags );

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr ReleaseDC(IntPtr handle, IntPtr hDC);

		[DllImport("Gdi32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		#endregion

		public delegate int MyWndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
		public delegate int HookProc(int code, IntPtr wparam, ref Win32.CWPSTRUCT cwp);

		public const int WM_DESTROY = 0x0002, WM_PRINT = 0x0317, WM_NCPAINT = 0x0085, WM_CREATE = 0x0001,
			WM_SIZE = 5, WM_NCCALCSIZE = 0x83, WM_WINDOWPOSCHANGING = 0x0046, SWP_NOSIZE = 1;
		public const int DCX_INTERSECTRGN = 0x80, DCX_WINDOW = 1, 
			WVR_HREDRAW = 0x0100, WVR_VREDRAW = 0x0200, WVR_REDRAW = (WVR_HREDRAW|WVR_VREDRAW);

		#region Subclassing P/Invokes

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern int CallWindowProc(IntPtr wndProc, IntPtr hwnd, 
			int msg, IntPtr wparam,	IntPtr lparam);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern int GetWindowThreadProcessId(IntPtr hwnd, int ID);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern int GetClassName(IntPtr hwnd, char[] className, int maxCount);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern int CallNextHookEx(IntPtr hookHandle, 
								int code, IntPtr wparam, ref CWPSTRUCT cwp);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowLong(IntPtr hwnd, int index, MyWndProc my);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern int GetWindowLong(IntPtr hwnd, int index );

		[DllImport("User32.dll",CharSet = CharSet.Auto, EntryPoint = "SetWindowLong" )]
		public static extern IntPtr SetWindowLong2(IntPtr hwnd, int index, int newa);

		[DllImport("User32.dll",CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(int type, HookProc hook, 
			IntPtr instance, int threadID);

		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public new string ToString() {
				return "{rect:" + left + "," + top + "," + right + "," + bottom + "}";
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CWPSTRUCT
		{
			public IntPtr lparam;
			public IntPtr wparam;
			public int message;
			public IntPtr hwnd;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public uint flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NCCALCSIZE_PARAMS
		{
			public RECT r1, r2, r3;
			public IntPtr lppos;
		}

		#endregion
	}
}
