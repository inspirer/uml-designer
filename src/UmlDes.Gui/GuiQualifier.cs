using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using CDS.Controls;
using CDS.UML;

namespace CDS.GUI {

	public class GuiQualifier : GuiBinded, ISelectable, IMoveable, IAcceptConnection, IHyphenSupport, IHasID, IRemoveableChild, IPostload, IStateObject, IDropMenu, INeedRefresh {
		[XmlElement("members")] public UmlSection st;
		[XmlAttribute] public int bind_ux;
		[XmlAttribute] public float bind_uy;
		[XmlIgnore] public ArrayList members = new ArrayList();

		private GuiMember highlighted;

		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList l = new ArrayList();
				foreach( GuiConnectionPoint p in cpoints )
					l.Add( p.root );
				return l;
			} 
		}

		public static GuiQualifier create( int bind_ux, float bind_uy, UmlSection st, GuiObject root, StaticView par ) {
			GuiQualifier s = new GuiQualifier();
			s.parent = par;
			s.root = root;
			s.st = st;
			s.bind_ux = bind_ux;
			s.bind_uy = bind_uy;
			foreach( UmlMember m in st.members )
				s.members.Add( GuiMember.fromUML( m ) );
			s.id = par.RegisterItemID( (root as GuiItem).id + ".Qualifier", s );
			return s;
		}

		#region Positioning

		public GuiQualifier() {
			p.edges = new Point[4];
		}

		public struct Position {
			public Geometry.Direction bind_dir;
			public int rem_x1, rem_y1, rem_x2, rem_y2,
				bnd_x1, bnd_y1, bnd_x2, bnd_y2,
				queue1, queue2;
			public Point[] edges;
			public int base_edge;
		}

		Position p;

		public void UpdatePosition() {
			int x1, y1, x2, y2;

			// get default place
			(root as GuiItem).coord_getxy( bind_ux, bind_uy, out x1, out y1 );
			place.X = x1;
			place.Y = y1;

			// get segment
			(root as GuiItem).coord_getxy( bind_ux, 0f, out x1, out y1 );
			(root as GuiItem).coord_getxy( bind_ux, 1f, out x2, out y2 );
			p.bind_dir = (root as GuiItem).direction( bind_ux );
			if( y1 == y2 ) {
				place.X -= place.Width/2;
				p.queue1 = ( place.Left < Math.Min(x1,x2)-inflate ) ? Math.Min(x1,x2) - place.Left : 0;
				p.queue2 = ( place.Right > Math.Max(x1,x2)-inflate ) ? place.Right - Math.Max(x1,x2) : 0;
				if( x2 > x1 ) { // South
					place.Y = y1 - (1 + inflate);
					p.bnd_y1 = p.bnd_y2 = y1 + 1;
					p.rem_y1 = p.rem_y2 = y1 + place.Height  - inflate - inflate/2 - 1;
					if( p.queue1 > 0 )
						p.bnd_y1 -= inflate/2 + 2;
					if( p.queue2 > 0 )
						p.bnd_y2 -= inflate/2 + 2;
				} else {	// North
					place.Y = y1 - (place.Height - inflate - 1);
					p.bnd_y1 = p.bnd_y2 = y1 - 1;
					p.rem_y1 = p.rem_y2 = y1 - place.Height + inflate + inflate/2 + 1;
					if( p.queue1 > 0 )
						p.bnd_y1 += inflate/2 + 2;
					if( p.queue2 > 0 )
						p.bnd_y2 += inflate/2 + 2;
				}
				p.bnd_x1 = p.rem_x1 = place.Left + inflate/2;
				p.bnd_x2 = p.rem_x2 = place.Right - inflate/2;

			} else if( x1 == x2 ) {
				place.Y -= place.Height/2;
				p.queue1 = ( place.Top < Math.Min(y1,y2)-inflate ) ? Math.Min(y1,y2) - place.Top : 0;
				p.queue2 = ( place.Bottom > Math.Max(y1,y2)-inflate ) ? place.Bottom - Math.Max(y1,y2) : 0;
				if( y1 > y2 ) { // East
					place.X = x1 - (1 + inflate);
					p.bnd_x1 = p.bnd_x2 = x1 + 1;
					p.rem_x1 = p.rem_x2 = x1 + place.Width - inflate - inflate/2 - 1;
					if( p.queue1 > 0 )
						p.bnd_x1 -= inflate/2 + 2;
					if( p.queue2 > 0 )
						p.bnd_x2 -= inflate/2 + 2;
				} else {  // West
					place.X = x1 - (place.Width - inflate - 1);
					p.bnd_x1 = p.bnd_x2 = x1 - 1;
					p.rem_x1 = p.rem_x2 = x1 - place.Width + inflate + inflate/2 + 1;
					if( p.queue1 > 0 )
						p.bnd_x1 += inflate/2 + 2;
					if( p.queue2 > 0 )
						p.bnd_x2 += inflate/2 + 2;
				}
				p.bnd_y1 = p.rem_y1 = place.Top + inflate/2;
				p.bnd_y2 = p.rem_y2 = place.Bottom - inflate/2;
			} else
				throw new Exception("wrong segment");

			p.edges[1].X = p.edges[0].X = place.Left + inflate;
			p.edges[3].Y = p.edges[0].Y = place.Top + inflate;
			p.edges[3].X = p.edges[2].X = place.Right - inflate;
			p.edges[1].Y = p.edges[2].Y = place.Bottom - inflate;
			p.base_edge = (int)p.bind_dir;
		}

		public override void ParentChanged() {
			Invalidate();
			UpdatePosition();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}
		#endregion

		#region Paint, RefreshView

		public void RefreshView( Graphics g ) {
			int width = 0, height = 0;

			foreach( GuiMember m in members ) {
				m.size = g.MeasureString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font) );
				width = Math.Max( (int)m.size.Width + 2*GuiClass.padding, width );
				height += (int)m.size.Height + GuiClass.line_space;
			}
			if( width == 0 )
				width = 2*GuiClass.padding;
			place.Width = width + 2*inflate;
			place.Height = height + 2*inflate + 2*GuiClass.vpadding;
		}

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			int x = place.X + r.X - offx + inflate, y = place.Y + r.Y - offy + inflate;

			int width = place.Width - 2*inflate, height = place.Height - 2*inflate;

			int curr_y = y + GuiClass.vpadding;
			g.FillRectangle( Brushes.WhiteSmoke, x, y, width-1, height-1 );
			g.DrawRectangle( Pens.Black, x, y, width-1, height-1 );

			foreach( GuiMember m in members ) {
				g.DrawString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font), Brushes.Black, x + GuiClass.padding, curr_y );
				m.y_offset = curr_y - r.Y + offy;
				m.area = new Rectangle( x + GuiClass.padding + offx - r.X, curr_y + offy - r.Y, (int)m.size.Width, (int)m.size.Height );
				if( highlighted == m )
					g.DrawRectangle( Pens.Gray, m.area.X - offx + r.X, m.area.Y - offy + r.Y, m.area.Width, m.area.Height );
				curr_y += (int)m.size.Height + GuiClass.line_space;
			}
			if( root.selected ) 
				using( Pen pn = new Pen( new HatchBrush( HatchStyle.Percent50, Color.White, Color.Gray ), inflate ) ) {
					g.DrawLines( pn, new Point[4] {
						new Point( p.bnd_x1+(r.X-offx), p.bnd_y1+(r.Y-offy) ),
						new Point( p.rem_x1+(r.X-offx), p.rem_y1+(r.Y-offy) ),
						new Point( p.rem_x2+(r.X-offx), p.rem_y2+(r.Y-offy) ),
						new Point( p.bnd_x2+(r.X-offx), p.bnd_y2+(r.Y-offy) ) } );
					if( p.queue1 > 0 )
						g.DrawLine( pn, p.bnd_x1 - Math.Sign(p.rem_x2-p.rem_x1)*(inflate/2)+(r.X-offx), p.bnd_y1-Math.Sign(p.rem_y2-p.rem_y1)*(inflate/2)+(r.Y-offy), 
									   p.bnd_x1 + Math.Sign(p.rem_x2-p.rem_x1)*(p.queue1-2)+(r.X-offx),  p.bnd_y1 +Math.Sign(p.rem_y2-p.rem_y1)*(p.queue1-2)+(r.Y-offy) );
					if( p.queue2 > 0 )
						g.DrawLine( pn, p.bnd_x2 - Math.Sign(p.rem_x1-p.rem_x2)*(inflate/2)+(r.X-offx), p.bnd_y2 - Math.Sign(p.rem_y1-p.rem_y2)*(inflate/2) + (r.Y-offy), 
									   p.bnd_x2 + Math.Sign(p.rem_x1-p.rem_x2)*(p.queue2-2)+(r.X-offx),  p.bnd_y2 + Math.Sign(p.rem_y1-p.rem_y2)*(p.queue2-2)+(r.Y-offy) );

				}
			else if( selected ) {
				using( Pen p = new Pen( new HatchBrush( HatchStyle.Percent50, Color.White, Color.Gray ), inflate ) ) {
					g.DrawRectangle( p, x - inflate/2, y - inflate/2, place.Width - inflate, place.Height - inflate );
				}
			}
		}

		#endregion

		#region Selection

		public override void SelectionChanged() {
			// we override it to not select parent
		}

		public bool TestSelected(Rectangle sel) {
			return sel.IntersectsWith( place );
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {

			ux = x - place.X;
			uy = y - place.Y;
			return place.Contains( x, y );
		}

		#endregion

		#region IUniversalCoords Members

		public void coord_getxy(int ux, float uy, out int x, out int y) {
			if( ux < 0 || ux >= p.edges.Length - 1 )
				ux = 0;
			Point p1 = p.edges[(ux+p.base_edge)%p.edges.Length], p2 = p.edges[(ux+p.base_edge+1)%p.edges.Length];
			x = (int)(p2.X * uy + p1.X * (1 - uy));
			y = (int)(p2.Y * uy + p1.Y * (1 - uy));
		}

		public bool coord_nearest(int x, int y, out int ux, out float uy) {
			float distance = -1; // infinity

			ux = 0;
			uy = 0f;
			for( int i = 0; i < p.edges.Length-1; i++ ) {
				int x3, y3;
				Point p1 = p.edges[(i+p.base_edge)%p.edges.Length], p2 = p.edges[(i+p.base_edge+1)%p.edges.Length];
				Geometry.nearest_point_from_segment( x, y, p1, p2, out x3, out y3 );
				float dist = (((float)(x3-x))*(x3-x) + ((float)(y3-y))*(y3-y));

				if( dist < distance || distance == -1 ) {
					distance = dist;
					ux = i; 
					uy = (float)(Math.Abs(x3 - p1.X) + Math.Abs(y3 - p1.Y)) / (Math.Abs(p2.X - p1.X) + Math.Abs(p2.Y - p1.Y));
				}
			}
			return true;
		}

		public void translate_coords(ref int ux, ref float uy) {
		}

		#endregion

		#region IAcceptConnection Members

		[XmlIgnore] public ArrayList cpoints = new ArrayList();

		public void add_connection_point(GuiConnectionPoint p) {
			cpoints.Add( p );
		}

		public void remove_connection_point(GuiConnectionPoint p) {
			cpoints.Remove( p );
		}

		[XmlAttribute] public string id;

		[XmlIgnore] public string ID { get { return id; } }

		#endregion

		#region IHyphenSupport Members

		public Geometry.Direction direction( int ux ) {
			if( ux < 0 || ux >= p.edges.Length - 1 )
				return Geometry.Direction.Null;
			int x1 = p.edges[(ux+p.base_edge)%p.edges.Length].X, y1 = p.edges[(ux+p.base_edge)%p.edges.Length].Y, 
				x2 = p.edges[(ux+p.base_edge+1)%p.edges.Length].X, y2 = p.edges[(ux+p.base_edge+1)%p.edges.Length].Y;

			if( x1 == x2 ) {
				if( y1 < y2 )
					return Geometry.Direction.West;
				else 
					return Geometry.Direction.East;
			} else if( y1 == y2 ) {
				if( x1 < x2 )
					return Geometry.Direction.South;
				else 
					return Geometry.Direction.North;
			} else 
				return Geometry.Direction.Null;
		}

		#endregion

		#region Removeable

		public override bool Destroy( ) {
			bool res = Unlink();
			root.remove_child( this );
			(root as GuiClass).qualifiers.Remove( this );
			return res;
		}

		public override void Restore() {
			(root as GuiClass).qualifiers.Add( this );
			root.add_child( this, null );
			Relink();
		}

		public bool Unlink() {
			while( cpoints.Count > 0 )
				parent.Destroy( (cpoints[0] as GuiConnectionPoint).root as IRemoveable );

			Invalidate();
			base.Destroy();
			parent.UnregisterObject( this.ID, this );
			return true;
		}


		public void Relink() {
			id = parent.RegisterItemID( (root as GuiItem).id + ".Qualifier", this );
			base.Restore();
			Invalidate();
		}

		#endregion

		#region IPostload Members

		public override void PostLoad() {

			foreach( UmlMember m in st.members )
				members.Add( GuiMember.fromUML( m ) );
			base.PostLoad();
		}

		#endregion
	
		#region IStateObject Members

		class State : ObjectState {
			public int ux;
			public float uy;
			public Position pos;
			public Rectangle place;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			this.bind_uy = t.uy;
			this.bind_ux = t.ux;
			this.p = t.pos;
			place = t.place;
			Invalidate();
		}

		public ObjectState GetState() {
			State t = new State();
			t.ux = this.bind_ux;
			t.uy = this.bind_uy;
			t.pos = this.p;
			t.place = this.place;
			return t;
		}

		#endregion

		#region Menu

		public void Edited( string ns ) {
			if( currentmember != null ) {
				ObjectState before = GetState();
				Invalidate();
				currentmember.st.fullname = ns;
				parent.RefreshObject( this );
				UpdatePosition();
				Invalidate();
				parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
			}
		}

		GuiMember currentmember;

		public void RenameClick( object o, EventArgs ev ) {
			if( currentmember != null ) {
				InPlaceTextEdit.Start( currentmember.st.fullname, currentmember.area.X - 1, currentmember.area.Y - 2, 
					currentmember.area.Width, currentmember.area.Height, parent.cview, new StringEditedEvent( Edited ) );
			}
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			currentmember = null;
			foreach( GuiMember mm in members )
				if( mm.area.Contains( x, y ) ) {
					FlatMenuItem rename = new FlatMenuItem( "Rename: " + mm.st.fullname, null, 0, false );
					currentmember = mm;
					rename.Click += new EventHandler( RenameClick );
					m.MenuItems.Add( rename );
					break;
				}

			if( m.MenuItems.Count > 0 )
				m.MenuItems.Add( new FlatMenuItem( "-", null, 0, false ) );

			FlatMenuItem plus = new FlatMenuItem( "Add member to qualifier", null, 0, false );
			plus.Enabled = false;
			//add_qual.Click += new EventHandler( AddQualifier );
			m.MenuItems.Add( plus );

		}

		#endregion

		#region IMoveable Members

		public void Moving(int x, int y, ref int ux, ref float uy) {
			Invalidate();
			(root as GuiItem).coord_nearest( x - ux + place.Width/2, y - (int)uy + place.Height/2, out bind_ux, out bind_uy );
			UpdatePosition();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}

		public void Moved() {
			foreach( GuiConnectionPoint p in cpoints )
				p.Moved();
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		#endregion
	}
}
