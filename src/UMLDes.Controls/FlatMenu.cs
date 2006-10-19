using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace UMLDes.Controls {

	public class FlatMenuItem : MenuItem {
		public ImageList imagelist;
		public int index;

		public object Tag;

		public ImageList Images {
			get { return imagelist; }
			set { imagelist = value; }
		}

		public int ImageIndex {
			get { return index; }
			set { index = value; }
		}

		public const int LeftVerticalBarSize = 23, IconX = 3, IconY = 4, TextX = LeftVerticalBarSize + 6,
			LineHeight = 23, RightMargin = 15, HLineHeight = 4, HLineY = 2;
		int text_height, text_width, shortcut_width;

		public FlatMenuItem() {
			OwnerDraw = true;
		}

		public FlatMenuItem( string text, ImageList iml, int index, bool Checked ) : base( text ) {
            OwnerDraw = true;
			imagelist = iml;
			this.index = index;
			this.Checked = Checked;
		}

		public bool IsMain {
			get {
				return this.Parent is MainMenu;
			}
		}

		public bool IsSubMain {
			get {
				return Parent is FlatMenuItem && (Parent as FlatMenuItem).IsMain;
			}
		}

		public string shortcut_name {
			get {
				string s = Shortcut.ToString();
				if( s.StartsWith( "CtrlShift" ) )
					s = "Ctrl+Shift+" + s.Substring(9);
				else if( s.StartsWith( "Ctrl" ) ) 
					s = "Ctrl+" + s.Substring(4);
				else if( s.StartsWith( "Alt" ) ) 
					s = "Alt+" + s.Substring(3);
				else if( s.StartsWith( "Shift" ) ) 
					s = "Shift+" + s.Substring(5);
				return s;
			}
		}

		protected override void OnDrawItem(DrawItemEventArgs e) {

			Color bg;

			if( Text == "-" ) {
				e.Graphics.FillRectangle( ColorManager.instance.menu_left_bg, e.Bounds.X, e.Bounds.Y, LeftVerticalBarSize, e.Bounds.Height );
				e.Graphics.FillRectangle( ColorManager.instance.menu_background, e.Bounds.X + LeftVerticalBarSize, e.Bounds.Y, e.Bounds.Width - LeftVerticalBarSize, e.Bounds.Height );
				e.Graphics.DrawLine( ColorManager.instance.menu_border, e.Bounds.X + TextX, e.Bounds.Y + HLineY, e.Bounds.Right-2, e.Bounds.Y + HLineY );
			} else if( IsMain ) {

				if((e.State & DrawItemState.HotLight) != 0) {
					e.Graphics.FillRectangle( ColorManager.instance.menu_selected_bg, e.Bounds.X+1, e.Bounds.Y+1, e.Bounds.Width-1, e.Bounds.Height-1 );
					e.Graphics.DrawRectangle( ColorManager.instance.menu_selected_fg, e.Bounds.X, e.Bounds.Y, e.Bounds.Width-1, e.Bounds.Height-1 );
				} else if((e.State & DrawItemState.Selected) != 0) {
					e.Graphics.DrawRectangle( ColorManager.instance.menu_border, e.Bounds.X, e.Bounds.Y, e.Bounds.Width-1, e.Bounds.Height-1 );
					e.Graphics.FillRectangle( ColorManager.instance.menu_left_bg, e.Bounds.X+1, e.Bounds.Y+1, e.Bounds.Width-2, e.Bounds.Height-1 );
				} else {
					e.Graphics.FillRectangle( SystemBrushes.Menu, e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height );
				}
				e.Graphics.DrawString( Text, ColorManager.instance.menu_font, SystemBrushes.ControlText, e.Bounds.X + (e.Bounds.Width-text_width+1)/2, e.Bounds.Y + (e.Bounds.Height - text_height + 1)/2, ColorManager.instance.menu_str );
			} else {

				if( Enabled && (e.State & DrawItemState.Selected) != 0) {
					e.Graphics.FillRectangle( ColorManager.instance.menu_selected_bg, e.Bounds.X+1, e.Bounds.Y+2, e.Bounds.Width-1, e.Bounds.Height-2 );
					e.Graphics.DrawRectangle( ColorManager.instance.menu_selected_fg, e.Bounds.X, e.Bounds.Y+1, e.Bounds.Width-1, e.Bounds.Height-2 );
					e.Graphics.FillRectangle( ColorManager.instance.menu_left_bg, e.Bounds.X, e.Bounds.Y, LeftVerticalBarSize, 1 );
					e.Graphics.FillRectangle( ColorManager.instance.menu_background, e.Bounds.X + LeftVerticalBarSize, e.Bounds.Y, e.Bounds.Width - LeftVerticalBarSize, 1 );
					bg = Color.FromArgb(182,189,210);
				} else {
					e.Graphics.FillRectangle( ColorManager.instance.menu_left_bg, e.Bounds.X, e.Bounds.Y, LeftVerticalBarSize, e.Bounds.Height );
					e.Graphics.FillRectangle( ColorManager.instance.menu_background, e.Bounds.X + LeftVerticalBarSize, e.Bounds.Y, e.Bounds.Width - LeftVerticalBarSize, e.Bounds.Height );
					bg = Color.FromArgb(219,216,209);
				}

				if( Checked && Enabled ) {
					int dt = (imagelist == null) ? 0 : 1;
					e.Graphics.FillRectangle( ((e.State & DrawItemState.Selected) != 0) ? ColorManager.instance.menu_checked_bg : ColorManager.instance.menu_selected_bg, e.Bounds.X+2-dt, e.Bounds.Y+3-dt, LeftVerticalBarSize-4+2*dt, e.Bounds.Height-4+2*dt );
					e.Graphics.DrawRectangle( ColorManager.instance.menu_selected_fg, e.Bounds.X+1-dt, e.Bounds.Y+2-dt, LeftVerticalBarSize-4+2*dt, e.Bounds.Height-4+2*dt );
					bg = ((e.State & DrawItemState.Selected) != 0) ? Color.FromArgb(133,146,181) : Color.FromArgb(182,189,210) ;
				}

				e.Graphics.DrawString( Text, ColorManager.instance.menu_font, Enabled ? SystemBrushes.ControlText : Brushes.Gray, e.Bounds.X + TextX, e.Bounds.Y + (e.Bounds.Height - text_height + 1)/2, ColorManager.instance.menu_str );

				if( shortcut_width > 0 )
					e.Graphics.DrawString( shortcut_name, ColorManager.instance.menu_font, SystemBrushes.ControlText, e.Bounds.Right - shortcut_width - RightMargin, e.Bounds.Y + (e.Bounds.Height - text_height + 1)/2, ColorManager.instance.menu_str );

				int delta = ((e.State & DrawItemState.Selected) != 0 || Enabled && Checked && (e.State & DrawItemState.Selected) == 0) ? 1 : 0;

				if( imagelist == null && Checked ) {
					e.Graphics.DrawLine( Pens.Black, e.Bounds.X + 7, e.Bounds.Y+11, e.Bounds.X + 9, e.Bounds.Y+13 );
					e.Graphics.DrawLine( Pens.Black, e.Bounds.X + 7, e.Bounds.Y+12, e.Bounds.X + 9, e.Bounds.Y+14 );
					e.Graphics.DrawLine( Pens.Black, e.Bounds.X + 10, e.Bounds.Y+12, e.Bounds.X + 13, e.Bounds.Y+9 );
					e.Graphics.DrawLine( Pens.Black, e.Bounds.X + 10, e.Bounds.Y+13, e.Bounds.X + 13, e.Bounds.Y+10 );
				}

				if( imagelist != null && index >= 0 && index < imagelist.Images.Count )
					if(Enabled) {
						if( (e.State & DrawItemState.Selected) != 0 )
							ControlPaint.DrawImageDisabled( e.Graphics, imagelist.Images[index], e.Bounds.X + IconX + delta, e.Bounds.Y + IconY + delta, bg );
						e.Graphics.DrawImage( imagelist.Images[index], e.Bounds.X + IconX - delta, e.Bounds.Y + IconY - delta );
					} else 
						ControlPaint.DrawImageDisabled( e.Graphics, imagelist.Images[index], e.Bounds.X + IconX - delta, e.Bounds.Y + IconY - delta, bg );
			}
			base.OnDrawItem(e);
		}

		protected override void OnMeasureItem(MeasureItemEventArgs e) {

			SizeF text_size = e.Graphics.MeasureString( Text, ColorManager.instance.menu_font, new Point(0,0), ColorManager.instance.menu_str );
			text_height = (int)text_size.Height+1;
			text_width = (int)text_size.Width+1;
			shortcut_width = 0;
			if( Text == "-" ) {
				e.ItemHeight = HLineHeight;
				e.ItemWidth = 5;
			} else if( IsMain ) {
				e.ItemHeight = text_height + 2;
				e.ItemWidth = text_width;
			} else {
				if( Shortcut != Shortcut.None ) {
					text_size = e.Graphics.MeasureString( shortcut_name, ColorManager.instance.menu_font, new Point(0,0), ColorManager.instance.menu_str );
					if( (int)text_size.Height+1 > text_height )
						text_height = (int)text_size.Height + 1;
					shortcut_width = (int)text_size.Width + 1;
				}

				e.ItemHeight = Math.Max( LineHeight-2, text_height) + 2;
				e.ItemWidth = TextX + text_width + RightMargin + (shortcut_width > 0 ? shortcut_width + /*spacing*/15 : 0);
			}
		}

		public static FlatMenuItem Create( string text, ImageList iml, int index, bool Checked, EventHandler click_handler, object Tag ) {
			FlatMenuItem fmi = new FlatMenuItem( text, iml, index, Checked );
			if( click_handler != null )
				fmi.Click += click_handler;
			else
				fmi.Enabled = false;
			fmi.Tag = Tag;
			return fmi;
		}
	}

	/// <summary>
	/// Summary description for FlatMenu.
	/// </summary>
	public class FlatMenuForm : System.Windows.Forms.Form {

		IntPtr defaultWndProc = IntPtr.Zero;	// Pointer to menu's default WndProc
		Win32.MyWndProc subWndProc;				// Delegate of type MyWndProc - needed to subclass the window
		Win32.HookProc hookProc;				// This is delegate of type HookProc - needed to process the hooked window
		IntPtr hookHandle = IntPtr.Zero;		// Pointer to hookProc

		public FlatMenuForm() {
			hookProc = new Win32.HookProc(Hooked);
			subWndProc = new Win32.MyWndProc(SubclassWndProc);
			hookHandle = Win32.SetWindowsHookEx(4, hookProc, IntPtr.Zero,
				Win32.GetWindowThreadProcessId(Handle,0));
		}

		#region SubclassedWndProc, Hooked, DrawBorder

		int Hooked(int code, IntPtr wparam, ref Win32.CWPSTRUCT cwp) {
			switch(code) {
				case 0:
				switch(cwp.message) {
					case Win32.WM_CREATE:
						string s = string.Empty;
						char[] className = new char[10];
						int length = Win32.GetClassName( cwp.hwnd, className, 9 );
						for( int i = 0; i < length; i++ )
							s += className[i];
						if( s == "#32768" ) { // System class for menu
							defaultWndProc = Win32.SetWindowLong( cwp.hwnd, (-4), subWndProc );
							//int old = Win32.GetWindowLong( cwp.hwnd, (-20) );
							//Win32.SetWindowLong2( cwp.hwnd,  (-20)/*GWL_EXSTYLE=(-20)*/, 0x20/*WS_EX_TRANSPARENT=0x20*/ );
						}

						break;
				}
				break;
			}
			return Win32.CallNextHookEx(hookHandle,code,wparam, ref cwp);
		}

		int SubclassWndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam) {
			switch(msg) {
				case Win32.WM_WINDOWPOSCHANGING:
					Win32.WINDOWPOS pos = (Win32.WINDOWPOS)System.Runtime.InteropServices.Marshal.PtrToStructure(lparam,typeof(Win32.WINDOWPOS));
					if( (pos.flags & Win32.SWP_NOSIZE) == 0 ) {
						pos.cx -= 2;
						pos.cy -= 3;
					}
					System.Runtime.InteropServices.Marshal.StructureToPtr( pos, lparam, true );
					return 0;
				case Win32.WM_NCPAINT:
					IntPtr menuDC  = Win32.GetWindowDC( hwnd );					
					Graphics g = Graphics.FromHdc( menuDC );
					DrawBorder( g );
					Win32.ReleaseDC( hwnd, menuDC );
					g.Dispose();
					return 0;
				case Win32.WM_NCCALCSIZE:
					Win32.NCCALCSIZE_PARAMS calc = (Win32.NCCALCSIZE_PARAMS)System.Runtime.InteropServices.Marshal.PtrToStructure(lparam,typeof(Win32.NCCALCSIZE_PARAMS));
					calc.r1.left += 2;
					calc.r1.top += 1;
					calc.r1.right -= 2;
					calc.r1.bottom -= 2;
					System.Runtime.InteropServices.Marshal.StructureToPtr( calc, lparam, true );
					return Win32.WVR_REDRAW;
			}
			return Win32.CallWindowProc( defaultWndProc, hwnd, msg, wparam, lparam );
		}

		void DrawBorder(Graphics g) {

			Rectangle r = new Rectangle( 0,0, (int)g.VisibleClipBounds.Width-1, (int)g.VisibleClipBounds.Height-1 );
			g.DrawRectangle( ColorManager.instance.menu_border, r );
			g.FillRectangle( ColorManager.instance.menu_left_bg, r.X+1, r.Y+1, 1, r.Height-1 );
			g.FillRectangle( ColorManager.instance.menu_left_bg, r.X+1, r.Y+1, FlatMenuItem.LeftVerticalBarSize+2, 1 );
			g.FillRectangle( ColorManager.instance.menu_background, r.X+3+FlatMenuItem.LeftVerticalBarSize, r.Y+1, r.Width-FlatMenuItem.LeftVerticalBarSize-3, 1 );
			g.FillRectangle( ColorManager.instance.menu_left_bg, r.X+1, r.Bottom-1, FlatMenuItem.LeftVerticalBarSize+2, 1 );
			g.FillRectangle( ColorManager.instance.menu_background, r.X+3+FlatMenuItem.LeftVerticalBarSize, r.Bottom-1, r.Width-FlatMenuItem.LeftVerticalBarSize-3, 1 );
			g.FillRectangle( ColorManager.instance.menu_background, r.Right-1, r.Top+1, 1, r.Height-1 );
		}

		#endregion

		protected override void WndProc(ref Message m) {
			switch(m.Msg) {
				case Win32.WM_DESTROY:
					Win32.UnhookWindowsHookEx(hookHandle);					
					base.WndProc(ref m);
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

	}
}
