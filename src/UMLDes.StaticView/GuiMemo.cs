using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Controls;

namespace UMLDes.GUI {

	public class GuiMemo : GuiPolygonItem, IStateObject, IDropMenu {

		public string text;

		public GuiMemo() {
			parent = null;
			text = "memo1";
		}

		#region Paint

		protected override Point[] GetPoints() {
			return new Point[] { new Point(X,Y), new Point(X,Y+Height), new Point(X+Width,Y+Height), new Point(X+Width,Y+angle), new Point(X+Width-angle,Y) };
		}


		public const int padding = 10, line_space = 2, vpadding = 6, angle = 12;
		SizeF text_size;

		/// <summary>
		/// Calculates width and height of object
		/// </summary>
		/// <param name="g">graphics object for measurements</param>
		public override void RefreshView( Graphics g ) {
			int width = 50, height = 0;

			text_size = g.MeasureString( text, parent.cview.GetFont(FontTypes.DEFAULT, FontStyle.Regular) );
			width = Math.Max( (int)text_size.Width + 2*padding, width );
			height = (int)text_size.Height + 2*vpadding;

			Width = width;
			Height = height;
		}

		public override void Paint( Graphics g, int x, int y ) {

			int textdx, curr_y = y + vpadding;
			g.DrawLine( Pens.Black, x + Width - angle, y, x + Width - angle, y + angle );
			g.DrawLine( Pens.Black, x + Width - angle, y + angle, x + Width, y + angle );

			textdx = ( Width - (int)text_size.Width ) / 2;
			g.DrawString( text, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Regular), Brushes.Black, x + textdx, curr_y );
			curr_y += (int)text_size.Height;
		}

		#endregion

		#region PostLoad

		public override void PostLoad() {
			text = text.Replace("\n", "\r\n");
			parent.RefreshObject( this );
			setup_edges();

			base.PostLoad();
		}

		#endregion

		#region IDropMenu Members

		public void Edited( string ns ) {
			ObjectState before = GetState();
			text = ns;
			StateChanged();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void RenameClick( object o, EventArgs ev ) {
			InPlaceTextEdit.Start( "Edit memo text", text, parent.cview.point_to_screen(place.X+inflate+1, place.Y+inflate+1), Math.Max( place.Width+20, 70 ), place.Height+40, parent.cview, new StringEditedEvent( Edited ), true );
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			FlatMenuItem rename = new FlatMenuItem( "Rename", parent.proj.icon_list, 0, false );
			rename.Click += new EventHandler( RenameClick );
			m.MenuItems.Add( rename );
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public string text;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			X = t.x;
			Y = t.y;
			text = t.text;
			StateChanged();
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			t.text = text;
			return t;
		}

		#endregion
	}

}