using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Windows.Forms;

namespace UMLDes.Controls {

	public delegate void StringEditedEvent( string new_str );

	public class InPlaceTextEdit {

		StringEditedEvent callback;
		Control view;
		TextBox text_box;
		Label l1, l2;

		public static void Start( string Title, string to_edit, Point screen_point, int width, int height, Control view, StringEditedEvent cb, bool multiline ) {
			InPlaceTextEdit te = new InPlaceTextEdit();
			te.callback = cb;
			te.view = view;

			System.Windows.Forms.TextBox tb = new System.Windows.Forms.TextBox();
			tb.Multiline = multiline;
			tb.Text = to_edit;
			tb.BorderStyle = BorderStyle.FixedSingle;
			tb.LostFocus +=new EventHandler(te.tb_LostFocus);
			tb.KeyDown +=new KeyEventHandler(te.tb_KeyDown);
			tb.Location = screen_point;
			tb.Width = width;
			tb.Height = height;

			te.l1 = new Label();
			te.l1.Text = multiline ? "Press Ctrl-Enter to accept" : "Press Enter to accept";
			te.l1.SetBounds( tb.Left, tb.Bottom+2, 130, 15 );
			te.l1.BackColor = Color.White;
			te.l1.Font = new Font( "Arial", 8 );

			te.l2 = new Label();
			te.l2.Text = Title;
			te.l2.SetBounds( tb.Left, tb.Top-17, 130, 15 );
			te.l2.BackColor = Color.White;
			te.l2.Font = new Font( "Arial", 8 );

			using( Graphics g = Graphics.FromHwnd( view.Handle ) ) {
				te.l1.Width = (int)g.MeasureString( te.l1.Text, te.l1.Font ).Width+4;
				te.l2.Width = (int)g.MeasureString( te.l2.Text, te.l2.Font ).Width+4;
			}

			te.text_box = tb;
			view.Controls.Add( tb );
			view.Controls.Add( te.l1 );
			view.Controls.Add( te.l2 );
			tb.Focus();
		}

		private void UnregisterControls() {
			view.Controls.Remove( text_box );
			view.Controls.Remove( l1 );
			view.Controls.Remove( l2 );
			view.Focus();
		}

		private void tb_KeyDown(object sender, KeyEventArgs e) {
			if( (e.KeyCode == Keys.Enter && (!text_box.Multiline || e.Control)) || e.KeyCode == Keys.Escape ) {
				text_box.LostFocus -= new EventHandler(tb_LostFocus);
				UnregisterControls();
				if( e.KeyCode != Keys.Escape )
					callback( text_box.Text );
			}
		}

		private void tb_LostFocus(object sender, EventArgs e) {
			UnregisterControls();
		}
	}
}
