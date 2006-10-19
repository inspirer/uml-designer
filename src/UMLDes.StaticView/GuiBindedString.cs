using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.Windows.Forms;
using UMLDes.Controls;

namespace UMLDes.GUI {

	/// <summary>
	/// GuiBindedStringObject is root class for all signs around connection, it draws string in the rectangle
	/// </summary>
	public abstract class GuiBindedStringObject : GuiBinded, ISelectable, IMoveable, IDropMenu, IStateObject, INeedRefresh {
		[XmlAttribute] public int pos_x, pos_y;
		[XmlAttribute] public int ux_bind;
		[XmlAttribute] public float uy_bind;
        [XmlAttribute] public bool hidden;

		const int NAME_SPACING_X = 3;
		const int NAME_SPACING_Y = 1;
		const int inflate = 2;

		protected abstract string Text { get; set; }
		protected abstract string ToDisplay { get; }

		public override bool Hidden {
			get {
				return hidden;
			}
		}

		[XmlIgnore] public ArrayList Associated { get { return null; } }

		public GuiBindedStringObject() {
		}

		public GuiBindedStringObject( string s, GuiObject pt, int x, int y, int ux, float uy, bool hidden ) {

			root = pt;
			parent = root.parent;
			pos_x = x;
			pos_y = y;
			ux_bind = ux;
			uy_bind = uy;
			this.hidden = hidden;

			RecalculatePosition();
			Text = s;
		}

		public void Moving(int x, int y, ref int ux, ref float uy) {

			Invalidate();

			pos_x += (x - ux) - place.X;
			pos_y += (y - (int)uy) - place.Y;
			RecalculatePosition();
			Invalidate();
		}

		public void Moved() {
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		public override void ParentChanged() {
			Invalidate();
			RecalculatePosition();
			Invalidate();
		}

		public override void PostLoad() {

			parent.RefreshObject(this);
			base.PostLoad();
		}

		#region Universal Coordinates
		
		// TODO
		public virtual void coord_getxy( int ux, float uy, out int x, out int y ) {
			x = y = 0;
		}

		// TODO
		public virtual bool coord_nearest( int x, int y, out int ux, out float uy ) {
			ux = x - place.X; uy = y - place.Y;
			return false;
		}

		public virtual void translate_coords( ref int ux, ref float uy ) {
			ux = ux;
			uy = uy;
		}

		public override void UpdateCoords( GuiObject orig ) {
			if( orig == root )
				(root as IUniversalCoords).translate_coords( ref ux_bind, ref uy_bind );
		}
		#endregion

		#region Recalculate: size, location

		internal void RecalculatePosition() {
			int X, Y;
			(root as IUniversalCoords).coord_getxy( ux_bind, uy_bind, out X, out Y );

			place.X = X + pos_x;
			place.Y = Y + pos_y;
		}

		public void RefreshView( Graphics g ) {
			SizeF size = g.MeasureString( ToDisplay, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ),
				1000, parent.cview.GetStringFormat( FormatTypes.CENTER ) );
			place.Width = (int)size.Width+1 + NAME_SPACING_X * 2 + inflate*2;
			place.Height = (int)size.Height+1 + NAME_SPACING_Y * 2 + inflate*2;

			if( place.Width < 20 )
				place.Width = 20;
			if( place.Height < 13 )
				place.Height = 13;
		}

		#endregion

		#region Paint, Invalidate

		public override void Paint(Graphics g, Rectangle r, int offx, int offy) {
			if( ToDisplay != null ) {

				Rectangle rect = place;
				rect.X += r.X - offx;
				rect.Y += r.Y - offy;

				Brush b;
				if( selected || root.selected ) {
					b = Brushes.Blue;
				} else {
					b = Brushes.Black;
				}


				g.DrawString( ToDisplay, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ), 
					b, rect.X + inflate + NAME_SPACING_X+1, rect.Y + inflate + NAME_SPACING_Y+1 );

				if( selected ) {
					int X, Y;
					(root as IUniversalCoords).coord_getxy( ux_bind, uy_bind, out X, out Y );
					int to_x = X + r.X - offx;
					int to_y = Y + r.Y - offy;
					using( Pen p = new Pen( new SolidBrush( Color.SteelBlue ) ) ) {
						p.DashStyle = DashStyle.Custom;
						p.DashPattern = new float[2] { 5.0f, 4.0f };
						int from_x, from_y;
						Geometry.nearest_point_from_rect( to_x, to_y, rect, out from_x, out from_y );
						g.DrawLine( p, from_x, from_y, to_x, to_y );
					}				

					using( Pen p = new Pen( new HatchBrush( HatchStyle.Percent50, Color.SteelBlue, Color.White), inflate ) ) {
						g.DrawRectangle( p, rect.X + inflate/2, rect.Y + inflate/2, rect.Width - inflate, rect.Height - inflate );
					}
				} else if( root.selected ) {
					g.DrawLine( Pens.SteelBlue, rect.Left + inflate, rect.Bottom - inflate, rect.Right - inflate, rect.Bottom - inflate );
					g.DrawLine( Pens.SteelBlue, rect.Left + inflate, rect.Top + inflate, rect.Right - inflate, rect.Top + inflate );
				}
			}
		}

		public override void Invalidate() {
			Rectangle rect = Rectangle.Empty;

			int X, Y;
			(root as IUniversalCoords).coord_getxy( ux_bind, uy_bind, out X, out Y );
			rect.X = Math.Min( X, place.X );
			rect.Y = Math.Min( Y, place.Y );
			rect.Width = Math.Abs( X - place.X ) + place.Width + 1;
			rect.Height = Math.Abs( Y - place.Y ) + place.Height + 1;

			parent.cview.InvalidatePage( rect );
		}

		#endregion

		#region ISelectable Members

		public bool TestSelected(Rectangle sel) {
			return sel.IntersectsWith( place );
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {

			ux = x - place.X;
			uy = y - place.Y;
			return !hidden && place.Contains( x, y );
		}
		#endregion

		#region IDropMenu Members

		public void Edited( string ns ) {
			ObjectState before = GetState();
			Invalidate();
			Text = ns;
			Invalidate();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void RenameClick( object o, EventArgs ev ) {
			InPlaceTextEdit.Start( "Rename", Text, parent.cview.point_to_screen(place.X, place.Y), Math.Max( place.Width+20, 70 ), place.Height, parent.cview, new StringEditedEvent( Edited ), false );
		}

		internal bool Visible {
			get {
				return !hidden;
			}
			set {
				ObjectState before = GetState();
				hidden = !value;
				Invalidate();
				parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
			}
		}

		private void Hide(object sender, EventArgs e) {
			Visible = false;
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			parent.AddItem( m, "Edit text", ToolBarIcons.None, false, new EventHandler( RenameClick ) );
			parent.AddItem( m, "Hide", ToolBarIcons.None, false, new EventHandler( Hide ) );
		}

		#endregion

		#region IStateObject Members

		protected class State : ObjectState {
			public string name;
			public int ux, x, y;
			public float uy;
			public Rectangle place;
			public bool hidden;

			public object o1, o2, o3;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			ux_bind = t.ux;
			uy_bind = t.uy;
			pos_x = t.x;
			pos_y = t.y;
			place = t.place;
			hidden = t.hidden;
			doApply( t );
			Invalidate();
		}

		protected abstract void doApply( State t );

		public ObjectState GetState() {
			State t = new State();
			t.ux = ux_bind;
			t.uy = uy_bind;
			t.x = pos_x;
			t.y = pos_y;
			t.place = place;
			t.hidden = hidden;
			FillState( t );
			return t;
		}

		protected abstract void FillState( State t );

		#endregion

	}

	/// <summary>
	/// Simple string
	/// </summary>
	public class GuiBindedString : GuiBindedStringObject {
		public GuiBindedString() {}
		public GuiBindedString( string s, GuiObject pt, int x, int y, int ux, float uy, bool hidden ) : base( s, pt, x, y, ux, uy, hidden ) {
		}

		[XmlAttribute] public string name;

		[XmlIgnore] protected override string Text {
			get { 
				return name; 
			}
			set {
				if( value != name ) {
					name = value;
					parent.RefreshObject(this);
				}
			}
		}

		protected override string ToDisplay {
			get {
				return name;
			}
		}


		#region State

		protected override void doApply(UMLDes.GUI.GuiBindedStringObject.State t) {
            name = (string)t.o1;
		}

		protected override void FillState(UMLDes.GUI.GuiBindedStringObject.State t) {
			t.o1 = name;
		}

		#endregion
	}

	/// <summary>
	/// Stereotype container
	/// </summary>
	public class GuiBindedStereotype : GuiBindedStringObject {
		public GuiBindedStereotype() {}
		public GuiBindedStereotype( string s, GuiObject pt, int x, int y, int ux, float uy, bool hidden ) : base( s, pt, x, y, ux, uy, hidden ) {
		}

		[XmlAttribute] public string stereo;

		[XmlIgnore] protected override string Text {
			get { 
				return stereo; 
			}
			set {
				if( value != stereo ) {
					stereo = value;
					parent.RefreshObject(this);
				}
			}
		}

		protected override string ToDisplay {
			get {
				return "\xAB"+stereo+"\xBB";
			}
		}


		#region State

		protected override void doApply(UMLDes.GUI.GuiBindedStringObject.State t) {
			stereo = (string)t.o1;
		}

		protected override void FillState(UMLDes.GUI.GuiBindedStringObject.State t) {
			t.o1 = stereo;
		}

		#endregion
	}
}