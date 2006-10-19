using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using CDS.CSharp;
using CDS.Controls;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace CDS.GUI {

	public class GuiBindedString : GuiBinded, ISelectable, IMoveable, IDropMenu, IStateObject, INeedRefresh {
		[XmlAttribute] public string name;
		public int pos_x, pos_y;
		public int ux_bind;
		public float uy_bind;

		const int NAME_SPACING_X = 5;
		const int NAME_SPACING_Y = 3;
		new const int inflate = 2;

		[XmlIgnore] public string title{
			get{ return name; }
			set{
				if( value != name ) {
					name = value;
					parent.RefreshObject(this);
				}
			}
		}

		[XmlIgnore] public ArrayList Associated { get { return null; } }

		public GuiBindedString() {
		}

		public GuiBindedString( string s, GuiObject pt, int x, int y, int ux, float uy ) {

			root = pt;
			parent = root.parent;
			pos_x = x;
			pos_y = y;
			ux_bind = ux;
			uy_bind = uy;

			RecalculatePosition();
			title = s;
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

		private void RecalculatePosition() {
			int X, Y;
			(root as IUniversalCoords).coord_getxy( ux_bind, uy_bind, out X, out Y );

			place.X = X + pos_x;
			place.Y = Y + pos_y;
		}

		public void RefreshView( Graphics g ) {
			SizeF size = g.MeasureString( name, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ),
				1000, parent.cview.GetStringFormat( FormatTypes.CENTER ) );
			place.Width = size.ToSize().Width + NAME_SPACING_X * 2;
			place.Height = size.ToSize().Height + NAME_SPACING_Y * 2;
		}

		#endregion

		#region Paint, Invalidate

		public override void Paint(Graphics g, Rectangle r, int offx, int offy) {
			if( name != null ) {

				Rectangle rect = place;
				rect.X += r.X - offx;
				rect.Y += r.Y - offy;

				if( selected ) {
					g.DrawString( name, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ), 
						Brushes.Red, rect, parent.cview.GetStringFormat( FormatTypes.CENTER ) );
				} else if( root.selected ) {
					g.DrawString( name, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ), 
						Brushes.Blue, rect, parent.cview.GetStringFormat( FormatTypes.CENTER ) );
				} else {
					g.DrawString( name, parent.cview.GetFont( FontTypes.ROLE_NAME, FontStyle.Regular ), 
						Brushes.Black, rect, parent.cview.GetStringFormat( FormatTypes.CENTER ) );
				}

				if( selected ) {
					int X, Y;
					(root as IUniversalCoords).coord_getxy( ux_bind, uy_bind, out X, out Y );
					int to_x = X + r.X - offx;
					int to_y = Y + r.Y - offy;
					using( Pen p = new Pen( new SolidBrush( Color.Purple ) ) ) {
						float[] pattern = new float[2] { 5.0f, 4.0f };
						p.DashStyle = DashStyle.Custom;
						p.DashPattern = pattern;
						g.DrawLine( p, rect.X, rect.Y, to_x, to_y );
					}				

					using( Pen p = new Pen( new HatchBrush( HatchStyle.Percent50, Color.Red, Color.White), inflate ) ) {
						g.DrawRectangle( p, rect.X + inflate/2, rect.Y + inflate/2, rect.Width - inflate, rect.Height - inflate );
					}
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
			return place.Contains( x, y );
		}
		#endregion

		#region IDropMenu Members

		public void Edited( string ns ) {
			ObjectState before = GetState();
			name = ns;
			Invalidate();
			parent.RefreshObject(this);
			Invalidate();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void RenameClick( object o, EventArgs ev ) {
			InPlaceTextEdit.Start( name, place.X, place.Y, Math.Max( place.Width+20, 70 ), place.Height, parent.cview, new StringEditedEvent( Edited ) );
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			FlatMenuItem rename = new FlatMenuItem( "Rename", parent.proj.container.list, 0, false );
			rename.Click += new EventHandler( RenameClick );
			m.MenuItems.Add( rename );
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public string name;
			public int ux, x, y;
			public float uy;
			public Rectangle place;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			ux_bind = t.ux;
			uy_bind = t.uy;
			pos_x = t.x;
			pos_y = t.y;
			name = t.name;
			place = t.place;
			Invalidate();
		}

		public ObjectState GetState() {
			State t = new State();
			t.ux = ux_bind;
			t.uy = uy_bind;
			t.x = pos_x;
			t.y = pos_y;
			t.name = name;
			t.place = place;
			return t;
		}

		#endregion
	}
}