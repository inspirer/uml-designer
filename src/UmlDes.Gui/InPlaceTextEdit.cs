using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Windows.Forms;

namespace CDS.Controls {

	public delegate void StringEditedEvent( string new_str );

	public class InPlaceTextEdit {

		StringEditedEvent callback;
		ViewCtrl view;

		public static void Start( string to_edit, int x, int y, int width, int height, ViewCtrl view, StringEditedEvent cb ) {
			InPlaceTextEdit te = new InPlaceTextEdit();
			te.callback = cb;
			te.view = view;

			System.Windows.Forms.TextBox tb = new System.Windows.Forms.TextBox();
			tb.Location = view.point_to_screen( x, y );
			tb.Width = width;
			tb.Height = height;
			tb.Text = to_edit;
			tb.BorderStyle = BorderStyle.FixedSingle;
			tb.LostFocus +=new EventHandler(te.tb_LostFocus);
			tb.KeyDown +=new KeyEventHandler(te.tb_KeyDown);
			view.Controls.Add( tb );
			tb.Focus();
		}

		private void tb_KeyDown(object sender, KeyEventArgs e) {
			if( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape ) {
				TextBox tb = sender as TextBox;
				if( tb != null ) {
					tb.LostFocus -= new EventHandler(tb_LostFocus);
					view.Controls.Remove( tb );
					view.Focus();
					if( e.KeyCode == Keys.Enter )
						callback( tb.Text );
				}
			}
		}

		private void tb_LostFocus(object sender, EventArgs e) {
			TextBox tb = sender as TextBox;
			if( tb != null ) {
				view.Controls.Remove( tb );
				view.Focus();
			}
		}

	}
}
