using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Controls;

namespace UMLDes.GUI {

	public class GuiMemo : GuiPolygonItem, IStateObject, IDropMenu, IHasStereotype {

		public string text, stereo;

		public GuiMemo() {
			parent = null;
			text = "memo1";
		}

		#region Paint

		protected override Point[] GetPoints() {
			return new Point[] { new Point(X,Y), new Point(X,Y+Height), new Point(X+Width,Y+Height), new Point(X+Width,Y+angle), new Point(X+Width-angle,Y) };
		}


		public const int padding = 10, line_space = 2, vpadding = 6, angle = 12;
		SizeF text_size, stereo_size;

		/// <summary>
		/// Calculates width and height of object
		/// </summary>
		/// <param name="g">graphics object for measurements</param>
		public override void RefreshView( Graphics g ) {
			int width = 50, height = 0;

			if( stereo != null ) {
				stereo_size = g.MeasureString( "\x00AB"+stereo+"\xBB", parent.cview.GetFont(FontTypes.ROLE_NAME, FontStyle.Regular) );
				width = Math.Max( (int)stereo_size.Width + 2*padding, width );
				height += (int)stereo_size.Height;
			}

			text_size = g.MeasureString( text, parent.cview.GetFont(FontTypes.DEFAULT, FontStyle.Regular) );
			width = Math.Max( (int)text_size.Width + 2*padding, width );
			height += (int)text_size.Height + 2*vpadding;

			Width = width;
			Height = height;
			setup_edges();
		}

		public override void Paint( Graphics g, int x, int y ) {

			int textdx, curr_y = y + vpadding;
			g.DrawLine( Pens.Black, x + Width - angle, y, x + Width - angle, y + angle );
			g.DrawLine( Pens.Black, x + Width - angle, y + angle, x + Width, y + angle );

			if( stereo != null ) {
				textdx = ( Width - (int)stereo_size.Width ) / 2;
				g.DrawString( "\x00AB"+stereo+"\xBB", parent.cview.GetFont(FontTypes.ROLE_NAME,FontStyle.Regular), Brushes.Black, x + textdx, curr_y-2 );
				curr_y += (int)stereo_size.Height;
			}

			textdx = ( Width - (int)text_size.Width ) / 2;
			g.DrawString( text, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Regular), Brushes.Black, x + textdx, curr_y );
			curr_y += (int)text_size.Height;
		}

		#endregion

		#region PostLoad

		public override void PostLoad() {
			text = text.Replace("\n", "\r\n");
			parent.RefreshObject( this );

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
			parent.AddItem( m, "Edit text", ToolBarIcons.None, false, new EventHandler( RenameClick ) );
			m.MenuItems.Add( new StereoTypeHelper(this).GetStereoMenu() );
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public string text, stereo;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			X = t.x;
			Y = t.y;
			stereo = t.stereo;
			text = t.text;
			StateChanged();
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			t.text = text;
			t.stereo = stereo;
			return t;
		}

		#endregion

		#region IHasStereotype Members

		static string[] stereo_list = new string[] {
			"requirement",
			"responsibility",
			"semantics",
            null,
			"precondition",
			"postcondition",
			"invariant"
		};

		string[] IHasStereotype.StereoList {
			get {
				return stereo_list;
			}
		}

		string IHasStereotype.Stereo {
			get {
				return stereo;
			}
			set {
				if( stereo != value ) {
					ObjectState before = GetState();
					stereo = value;
					StateChanged();
					parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
				}
			}
		}

		Rectangle IHasStereotype.EditRect { 
			get {
				return new Rectangle( place.X+inflate+1, place.Y+inflate+1, place.Width, 0 );
			}
		}

		#endregion
	}

}