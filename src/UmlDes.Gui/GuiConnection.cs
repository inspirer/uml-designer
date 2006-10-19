using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using CDS.CSharp;
using CDS.Controls;
using System.Xml.Serialization;

namespace CDS.GUI {

	public enum GuiConnectionType {
		Inheritance, Aggregation, Using, Association, n_ary
	}

	public enum GuiConnectionStyle {
		Line, Besier, Quadric, Segmented
	}

	public enum GuiConnectionNavigation {
		None, Left, Right
	}

	// pattern: State
	public abstract class ConnectionState {
		protected GuiConnection conn;
		abstract public GuiConnectionStyle style { get; }

		// object to which the movepoint was assigned has changed its location
		abstract public void EndPointPositionChanging( GuiConnectionPoint movepoint );
		// called to change connection during creation
		abstract public void DoCreationFixup();
		// moving using points and lines
		abstract public void Moving( int x, int y, ref int ux, ref float uy );
		// fixes the connection to not intersect the given list of objects
		abstract public bool CheckIntersection( ArrayList /* of IAroundObject */ arobjs, Hashtable states );
		// simpifies connection (ex:removes loops and steps)
		abstract public void OptimizeConnection();
	}

	public class GuiConnection : GuiActive, ISelectable, IAcceptConnection, IMoveMultiple, IDropMenu, IRemoveable, IStateObject {

		[XmlIgnore] public GuiConnectionPoint first, second;
		[XmlIgnore] public ArrayList ipoints = new ArrayList();	// Array of GuiPoints, contains also first and second
		[XmlIgnore] public ArrayList cpoints = new ArrayList();
		[XmlAttribute] public GuiConnectionType type;
		[XmlAttribute] public GuiConnectionNavigation nav;
		[XmlIgnore] bool created = false;
		[XmlIgnore] public ConnectionState Style;

		const int MINIMUM_DISTANCE = 10;

		[XmlAttribute] public GuiConnectionStyle style { 
			get { 
				return Style.style; 
			}
			set {
				switch( value ) {
					case GuiConnectionStyle.Quadric:
						Style = new ConnectionStateQuadric(this);
						break;
					case GuiConnectionStyle.Segmented:
						Style = new ConnectionStateSegmented(this);
						break;
					default:
						throw new Exception( "wrong style" );
				}
			}
		}

		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList l = new ArrayList();
				foreach( GuiConnectionPoint p in cpoints )
					l.Add( p.root );
				return l;
			} 
		}

		public void EndPointPositionChanging( GuiConnectionPoint movepoint ) {
			if( !created )
				return;

			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Style.EndPointPositionChanging( movepoint );
			notify_children();
		}                                                                                                      

		public void EndPointPositionChanged() {
            
		}

		#region Constructors

		public GuiConnection() {
		}
								 
		public GuiConnection( GuiConnectionPoint p1, GuiConnectionPoint p2, GuiConnectionType t, StaticView par ) {
			first = p1;
			second = p2;
			type = t;
			parent = par;
			Style = new ConnectionStateQuadric(this);
			nav = GuiConnectionNavigation.None;
			p1.parent = p2.parent = par;
			p1.root = p2.root = this;
			ipoints.Add( p1 );
			ipoints.Add( p2 );
		}

		#endregion

		#region Intermediate points ops, affect Universal Coords

		public override void ModifyUniversalCoords() {
			base.ModifyUniversalCoords ();
			foreach( GuiConnectionPoint c in cpoints ) {
				c.UpdateCoords( this );
			}
		}

		enum Translation { DeletePoint, AddPoint };
		Translation transl_action;
		int transl_action_param;

		public void translate_coords( ref int ux, ref float uy ){
			if( transl_action == Translation.AddPoint ) {
				if( ux < 0 ) {
					if( -ux-1 >= transl_action_param )
						ux--;
				} else {
					if( ux >= transl_action_param )
						ux++;
					// TODO else if( ux == transl_action_param - 1 ) 
				}

			} else if( transl_action == Translation.DeletePoint ) {
				if( ux < 0 ) {
					if( -ux-1 > transl_action_param )
						ux++;
				} else {
					if( ux >= transl_action_param )
						ux--;
					else if( ux == transl_action_param - 1 )
						ux--; // TODO 
				}
			}
		}

		public GuiIntermPoint insert_point( int after_num ) {
			if( after_num < 0 || after_num >= ipoints.Count - 1 )
				return null;
			GuiIntermPoint p = new GuiIntermPoint();
			p.root = this;
			p.parent = parent;
			p.number_in_conn = after_num + 1;
			ipoints.Insert( after_num + 1, p );
			for( int i = after_num + 2; i < ipoints.Count; i++ )
				((GuiPoint)ipoints[i]).number_in_conn = i;

			// perform UC translation
			transl_action = Translation.AddPoint;
			transl_action_param = after_num + 1;
			ModifyUniversalCoords();
			return p;
		}

		public void remove_point( int at_pos ) {
			if( at_pos <= 0 || at_pos >= ipoints.Count - 1 )
				return;
			ipoints.RemoveAt( at_pos );
			for( int i = at_pos; i < ipoints.Count; i++ )
				((GuiPoint)ipoints[i]).number_in_conn = i;

			// perform UC translation
			transl_action = Translation.DeletePoint;
			transl_action_param = at_pos;
			ModifyUniversalCoords();
		}

		public bool RemoveIntermediate( int at_pos ) { 

			if( at_pos <= 0 || at_pos >= ipoints.Count - 1 )
				return false;

			switch( style ) {
				case GuiConnectionStyle.Segmented:
					Invalidate();
					(ipoints[at_pos] as GuiIntermPoint).Invalidate();
					remove_child( (GuiBinded)ipoints[at_pos] );
					remove_point( at_pos );
					Invalidate();
					break;
			}										  

			return true;
		}

		#endregion

		#region Paint, Invalidate, DrawTemporary

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			switch( style ) {
				case GuiConnectionStyle.Besier:
					DrawBesierConnection( g, r, offx, offy );
					break;
				case GuiConnectionStyle.Line: case GuiConnectionStyle.Quadric: case GuiConnectionStyle.Segmented:
					DrawSegmentedConnection( g, r, offx, offy );
					break;
			}
		}

		private void DrawSegmentedConnection( Graphics g, Rectangle r, int offx, int offy ) {
			int x1 = first.x + r.X - offx, y1 = first.y + r.Y - offy;
			int x2, y2;
			Pen p = selected ? Pens.Blue : Pens.Black;

			if( type == GuiConnectionType.Using ) {
				p = new Pen( p.Color );
				p.DashStyle = DashStyle.Dash;
				p.DashOffset = 100f; // ???
			} else 
				g.SmoothingMode = SmoothingMode.AntiAlias;

			Point[] pnts = new Point[ipoints.Count];
			for( int i = 0; i < ipoints.Count; i++ ) {
				GuiPoint pt = (GuiPoint)ipoints[i];
				pnts[i].X = pt.x + r.X - offx; 
				pnts[i].Y = pt.y + r.Y - offy; 
			}
			g.DrawLines( p, pnts );

			GuiPoint pnt = (GuiPoint)ipoints[1];
			x2 = pnt.x + r.X - offx; y2 = pnt.y + r.Y - offy;

			switch( type ) {
				case GuiConnectionType.Aggregation:
					Geometry.point_rhomb_on_segment( out pnts, x1, y1, x2, y2, 7, 5 );
					g.FillPolygon( Brushes.White, pnts );
					g.DrawPolygon( Pens.Black, pnts );
					break;
				case GuiConnectionType.Association:
					break;
				case GuiConnectionType.Inheritance:
					Geometry.point_triangle_on_segment( out pnts, x1, y1, x2, y2, 10, 7 );
					g.FillPolygon( Brushes.White, pnts );
					g.DrawPolygon( Pens.Black, pnts );
					break;
				case GuiConnectionType.Using:
					break;
			}
			g.SmoothingMode = SmoothingMode.Default;

			if( type == GuiConnectionType.Using )
				p.Dispose();
		}

		private void DrawBesierConnection( Graphics g, Rectangle r, int offx, int offy ) {
		}

		public override bool NeedRepaint(Rectangle page) {

			bool need = false;

			switch( style ) {
				case GuiConnectionStyle.Segmented:
				case GuiConnectionStyle.Quadric:
					int x1 = first.x, y1 = first.y, x2, y2;
					for( int i = 1; i < ipoints.Count; i++ ) {
						x2 = ((GuiPoint)ipoints[i]).x;
						y2 = ((GuiPoint)ipoints[i]).y;
						if( Geometry.rectangle_intersects_with_line( page, x1, y1, x2, y2 ) ) {
							need = true;
							break;
						}
						x1 = x2; y1 = y2;
					}
					return need;
			}

			return base.NeedRepaint (page);
		}

		public override void Invalidate() {
			switch( style ) {
				case GuiConnectionStyle.Line: case GuiConnectionStyle.Segmented: case GuiConnectionStyle.Quadric:
					GraphicsPath p = new GraphicsPath();
					p.FillMode = FillMode.Winding;
					Point[] pts = new Point[4];
					int x1 = first.x, y1 = first.y, x2, y2;
					for( int i = 1; i < ipoints.Count; i++ ) {
						x2 = ((GuiPoint)ipoints[i]).x;
						y2 = ((GuiPoint)ipoints[i]).y;
						Geometry.polygon_around_segment( ref pts, x1,y1,x2,y2, 6 );
						p.AddPolygon( pts );
						x1 = x2; y1 = y2;
					}
					Region r = new Region( p );
					parent.cview.InvalidateRegion( r );
					parent.cview.InvalidatePage( new Rectangle(first.x - 14, first.y - 14, 28,28 ) );
					break;
			}
		}

		public void DrawTemporary( Graphics g, Rectangle r, int offx, int offy ) {
			first.Paint( g, r, offx, offy );
			if( second.item != null )
				second.Paint( g, r, offx, offy );
		}

		public void InvalidateTemporary() {
			first.Invalidate();
			if( second.item != null )
				second.Invalidate();
		}

		#endregion

		#region "Fixup functions: CheckIntersection, PostLoad, ConnectionCreated, DoCreationFixup"

		public bool CheckIntersection( ArrayList /* of IAroundObject */ arobjs, Hashtable states ) {
			return Style.CheckIntersection( arobjs, states );
		}

		public void DoCreationFixup() {
			Style.DoCreationFixup();
		}

		// registers connection in StaticView structures:
		public void ConnectionCreated( StaticView parent_view ) {

			//  1. register connection ipoints in objects
			first.item.add_connection_point( first );
			second.item.add_connection_point( second );
			//  2. create ID for connection
			id = parent_view.RegisterItemID( type.ToString(), this );
			//  3. create default roles
			first.role = new GuiBindedString( "Left", this, 20, 20, -1, 0f );
			second.role = new GuiBindedString( "Right", this, 20, 20, -second.number_in_conn-1, 0f );
			//  4. register children, make them drawable and serializable
			add_child( first, "LeftPoint" );
			add_child( second, "RightPoint" );
			add_child( first.role, "Role 1" );
			add_child( second.role, "Role 2" );

			for( int i = 1; i < ipoints.Count - 1; i++ )
				add_child( ipoints[i] as GuiBinded, null );

			//  5. Redraw
			created = true;
			invalidate_children();
			Invalidate();
		}

		public int interm_count {
			get {
				return ipoints.Count;
			}
			set {
				loadtime_iterm_count = value;
			}
		}

		private int loadtime_iterm_count;

		public override void PostLoad() {

			base.PostLoad();
			first = find_child( "LeftPoint" ) as GuiConnectionPoint;
			second = find_child( "RightPoint" ) as GuiConnectionPoint;
			first.UpdatePosition( true );
			second.UpdatePosition( true );
			first.role = find_child( "Role 1" ) as GuiBindedString;
			second.role = find_child( "Role 2" ) as GuiBindedString;
			ipoints.Add( first );
			for( int i = 1; i < loadtime_iterm_count - 1; i++ )
				ipoints.Add( find_child( "Point #" + i ) );
			ipoints.Add( second );
			created = true;

			notify_children();
		} 

		#endregion

		#region Selection

		public override void SelectionChanged() {
			notify_children();
		}

		public bool TestSelected(Rectangle sel) {
			bool res = false;

			// TODO

			switch( style ) {
				case GuiConnectionStyle.Line:
					if( sel.IntersectsWith( place ) ) {
						res = true;
					}
					break;
				case GuiConnectionStyle.Segmented:
					// TODO
					break;
				default:
					break;
			}

			return res;
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {
			int x2, y2, segn;

			ux = 0;
			uy = 0f;

			switch( style ) {
				case GuiConnectionStyle.Line:
					Geometry.nearest_point_from_segment( x, y, new Point(first.x,first.y), new Point(second.x,second.y), out x2, out y2 );
					if( ( (x-x2)*(x-x2)+(y-y2)*(y-y2) ) <= 16 ) {
						ux = 0;
						uy = Geometry.point_to_uy( x2, y2, new Point(first.x,first.y), new Point(second.x,second.y) );
						return true;
					}
					break;
				case GuiConnectionStyle.Segmented:
				case GuiConnectionStyle.Quadric:
					Geometry.nearest_point_from_segment_list( x, y, ipoints, out segn, out x2, out y2 );
					if( ( (x-x2)*(x-x2)+(y-y2)*(y-y2) ) <= 16 ) {
						ux = segn;
						uy = Geometry.point_to_uy( x2, y2, new Point(((GuiPoint)ipoints[segn]).x,((GuiPoint)ipoints[segn]).y), new Point(((GuiPoint)ipoints[segn+1]).x,((GuiPoint)ipoints[segn+1]).y) );
						return true;
					}
					break;
			}

			return false;
		}

		#endregion

		#region Universal coordinates

		//  ux:  -1   first connection point
		//       -2   Interm
		//       -n   second
		//       0,0..1    First segment
		//       1,0..1    Second segment

		public bool coord_nearest(int x, int y, out int ux, out float uy) {
			int x2, y2;
			Geometry.nearest_point_from_segment_list( x, y, ipoints, out ux, out x2, out y2 );
			uy = Geometry.point_to_uy( x2, y2, new Point(((GuiPoint)ipoints[ux]).x,((GuiPoint)ipoints[ux]).y), new Point(((GuiPoint)ipoints[ux+1]).x,((GuiPoint)ipoints[ux+1]).y) );
			return true;
		}

		public void coord_getxy(int ux, float uy, out int x, out int y) {
			x = y = -1;

			if( ux < 0 ) {
				int n = -ux-1;

				if( n < ipoints.Count ) {
					x = ((GuiPoint)ipoints[n]).x;
					y = ((GuiPoint)ipoints[n]).y;
				}
			} else if( ux < ipoints.Count ) {
				Geometry.uy_to_point( uy, new Point(((GuiPoint)ipoints[ux]).x,((GuiPoint)ipoints[ux]).y), new Point(((GuiPoint)ipoints[ux+1]).x,((GuiPoint)ipoints[ux+1]).y), out x, out y );
			}
		}

		public void add_connection_point(GuiConnectionPoint p) {
			cpoints.Add( p );
		}

		public void remove_connection_point(GuiConnectionPoint p) {
			cpoints.Remove( p );
		}

		#endregion

		#region Moving routines

		public void Moving(int x, int y, ref int ux, ref float uy) {

			Style.Moving( x, y, ref ux, ref uy );
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			notify_children();
		}

		public void Moved() {
			foreach( GuiConnectionPoint p in cpoints )
				p.Moved();
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		public bool CanMoveInGroup { 
			get { 
				return (first.item as GuiObject).selected && (second.item as GuiObject).selected; 
			} 
		}

		public void ShiftShape( int dx, int dy ) {
			Invalidate();
			for( int i = 1; i < ipoints.Count - 1; i++ ) {
				(ipoints[i] as GuiPoint).x += dx;
				(ipoints[i] as GuiPoint).y += dy;
			}
			Invalidate();
		}

		#endregion

		#region Change Line Style (MENU)

		public void ChangeStyleClick( object o, EventArgs ev ) {
			switch( (o as FlatMenuItem).Index ) {
				case 0:	// Line
					break;
				case 1: // Segmented
					if( style == GuiConnectionStyle.Quadric )
						style = GuiConnectionStyle.Segmented;
					break;
				case 2:	// Quadric
					//create_quadric_connection( style == GuiConnectionStyle.Segmented );
					//style = GuiConnectionStyle.Quadric;
					break;
				case 3:	// Bezier
					break;
			}
		}

		public void ChangeTypeClick( object o, EventArgs ev ) {
			this.Invalidate();
			switch( (o as FlatMenuItem).Index ) {
				case 0:	// Association
					type = GuiConnectionType.Association;
					break;
				case 1: // Aggregation
					type = GuiConnectionType.Aggregation;
					break;
				case 2:	// Inheritance
					type = GuiConnectionType.Inheritance;
					break;
				case 3:	// Usage
					type = GuiConnectionType.Using;
					break;
			}
			this.Invalidate();
		}
                      
		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {

			// Style
			FlatMenuItem tmp, style = new FlatMenuItem( "Style", null, 0, false );
			EventHandler evh = new EventHandler( ChangeStyleClick );
			style.MenuItems.Add( tmp = new FlatMenuItem( "Line" , parent.proj.container.list, 10, ( this.style == GuiConnectionStyle.Line) ));
			tmp.Click += evh;
			style.MenuItems.Add( tmp = new FlatMenuItem( "Segmented" , parent.proj.container.list, 12, ( this.style == GuiConnectionStyle.Segmented) ));
			tmp.Click += evh;
			style.MenuItems.Add( tmp = new FlatMenuItem( "Quadric" , parent.proj.container.list, 8, ( this.style == GuiConnectionStyle.Quadric) ));
			tmp.Click += evh;
			style.MenuItems.Add( tmp = new FlatMenuItem( "Bezier" , parent.proj.container.list, 11, ( this.style == GuiConnectionStyle.Besier) ));
			tmp.Click += evh;
			m.MenuItems.Add( style );
			// Type
			evh = new EventHandler( ChangeTypeClick );
			FlatMenuItem type = new FlatMenuItem( "Type", null, 0, false );
			type.MenuItems.Add( tmp = new FlatMenuItem( "Association" , parent.proj.container.list, 22, ( this.type == GuiConnectionType.Association) ));
			tmp.Click += evh;
			type.MenuItems.Add( tmp = new FlatMenuItem( "Aggregation" , parent.proj.container.list, 23, ( this.type == GuiConnectionType.Aggregation) ));
			tmp.Click += evh;
			type.MenuItems.Add( tmp = new FlatMenuItem( "Inheritance" , parent.proj.container.list, 21, ( this.type == GuiConnectionType.Inheritance) ));
			tmp.Click += evh;
			type.MenuItems.Add( tmp = new FlatMenuItem( "Usage" , null, 0, ( this.type == GuiConnectionType.Using) ));
			tmp.Click += evh;
			m.MenuItems.Add( type );
		}

		#endregion

		#region Remove/Restore

		public override bool Destroy( ) {
			while( cpoints.Count > 0 )
				parent.Destroy( (cpoints[0] as GuiConnectionPoint).root as IRemoveable );

			Invalidate();
			first.item.remove_connection_point( first );
			second.item.remove_connection_point( second );
			parent.UnregisterObject( this.ID, this );
			base.Destroy();
			return true;
		}

		public override void Restore() {
			first.item.add_connection_point( first );
			second.item.add_connection_point( second );
			id = parent.RegisterItemID( type.ToString(), this );
			base.Restore();
			Invalidate();
		}

		#endregion

		#region IStateObject Members

		public class State : ObjectState {
			public GuiConnectionType type;
			public GuiConnectionStyle style;
			public System.Collections.ArrayList ipoints = new ArrayList();
			public ObjectState p1, p2;
		}


		public void Apply(ObjectState v) {
			State state = v as State;

			Invalidate();
			invalidate_children();
			style = state.style;
			type = state.type;

			for( int i = 1; i < ipoints.Count - 1; i++ )
				remove_child( ipoints[i] as GuiBinded );
			ipoints.Clear();

			ipoints.Add( first );
			foreach( GuiPoint pt in state.ipoints ) {
				ipoints.Add( pt );
				add_child( pt, null );
			}
			ipoints.Add( second );

			second.number_in_conn = ipoints.Count - 1;
			second.role.ux_bind = -ipoints.Count;

			first.Apply( state.p1 );
			second.Apply( state.p2 );

			notify_children();
			invalidate_children();
			Invalidate();
		}

		public ObjectState GetState() {         
			State st = new State();
			st.type = type;
			st.style = style;
			
			for( int i = 1; i < ipoints.Count - 1; i++ )
				st.ipoints.Add( (ipoints[i] as GuiIntermPoint).Clone() );
			st.p1 = first.GetState();
			st.p2 = second.GetState();
			return st;
		}

		#endregion
	}
}