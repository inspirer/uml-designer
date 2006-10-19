using System;
using System.Drawing;

namespace CDS {

	public class ColorManager : IDisposable {

		public readonly Brush menu_background, menu_left_bg, menu_selected_bg, menu_checked_bg;
		public readonly Pen menu_selected_fg, menu_border;
		public readonly Font menu_font;
		public readonly StringFormat menu_str;

		private static ColorManager _man;

		public static ColorManager instance {
			get {
				if( _man == null )
					_man = new ColorManager();
				return _man;
			}
		}

		private ColorManager() {
			menu_background = new SolidBrush(Color.FromArgb(249,248,247));
			menu_left_bg = new SolidBrush(Color.FromArgb(219,216,209));
			menu_selected_bg = new SolidBrush( Color.FromArgb(182,189,210) );
			menu_checked_bg = new SolidBrush( Color.FromArgb(133,146,181) );
			menu_selected_fg = new Pen( Color.FromArgb(10,36,106));
			menu_border = Pens.Gray;
			menu_str = new StringFormat();
			menu_str.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			menu_font = new Font("tahoma",8);
		}

		public void Dispose() {
			_man = null;
			menu_background.Dispose();
			menu_selected_bg.Dispose();
			menu_checked_bg.Dispose();
			menu_left_bg.Dispose();
			menu_selected_fg.Dispose();
			menu_font.Dispose();
			menu_str.Dispose();
		}
	}

}
