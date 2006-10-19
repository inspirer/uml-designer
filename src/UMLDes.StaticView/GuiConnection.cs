using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Controls;
using UMLDes.Model;

namespace UMLDes.GUI {

	public enum GuiConnectionNavigation {
		None, Left, Right
	}

	public enum GuiConnectionStyle {
		Line, Segmented, Quadric, Besier
	}

	// pattern: State
	public abstract class ConnectionState {
		protected GuiConnection conn;
		abstract public GuiConnectionStyle style { get; }

		// object to which the movepoint was assigned has changed its location
		abstract public void EndPointPositionChanging( GuiConnectionPoint movepoint );
		// called to change connection during creation
		abstract public void DoCreationFixup( bool converted );
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
		[XmlAttribute] public UmlRelationType type;
		[XmlAttribute] public GuiConnectionNavigation nav;
		[XmlIgnore] bool created = false;
		[XmlIgnore] public ConnectionState Style;
		[XmlIgnore] public GuiBindedString conn_name;
		[XmlIgnore] public GuiBindedStereotype conn_stereo;
		[XmlAttribute] public string relation_id;
		[XmlAttribute] public bool hidden;

		const int MINIMUM_DISTANCE = 10;

		public override string Name {
			get {
				return type + ": " + ((GuiActive)first.item).Name + " <-> " + ((GuiActive)second.item).Name;
			}
		}

		[XmlAttribute] public GuiConnectionStyle style { 
			get { 
				return Style.style; 
			}
			set {
				switch( value ) {
					case GuiConnectionStyle.Line:
						Style = new ConnectionStateLine(this);
						break;
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

		public void EndPointPositionChanging( GuiConnectionPoint movepoint ) {
			if( !created )
				return;

			Style.EndPointPositionChanging( movepoint );
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			notify_children();
		}                                                                                                      

		public bool CanHaveHyphen { 
			get {
				return style != GuiConnectionStyle.Line;
			}
		}

		public void EndPointPositionChanged() {
		}

		#region Constructors

		public GuiConnection() {
		}
								 
		public GuiConnection( GuiConnectionPoint p1, GuiConnectionPoint p2, UmlRelationType t, StaticView par, GuiConnectionStyle style ) {
			first = p1;
			second = p2;
			type = t;
			parent = par;
			this.style = style;
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
			if( ux == Int32.MaxValue )
				return;
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

		public void insert_point_child( int at_pos ) {
			GuiIntermPoint ip = insert_point( at_pos );
			add_child( ip, null );
			ip.Invalidate();
		}

		public void remove_point_child( int at_pos ) {
			(ipoints[at_pos] as GuiIntermPoint).Invalidate();
			remove_child( (GuiBinded)ipoints[at_pos] );
			remove_point( at_pos );
		}

		public bool RemoveIntermediate( int at_pos ) { 

			if( at_pos <= 0 || at_pos >= ipoints.Count - 1 )
				return false;

			switch( style ) {
				case GuiConnectionStyle.Segmented:
					Invalidate();
					remove_point_child( at_pos );
					Invalidate();
					break;
			}										  

			return true;
		}

		#endregion

		#region Paint, Invalidate, DrawTemporary, ContainingRect

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
			Pen color = selected ? Pens.Blue : Pens.Black, p = color;

			switch( type ) {
				case UmlRelationType.Attachment:
				case UmlRelationType.Dependency:
				case UmlRelationType.Realization:
					p = new Pen( p.Color );
					p.DashStyle = DashStyle.Dash;
					p.DashOffset = 0;
					p.DashPattern = new float[] { 10, 5 };
					break;
				default:
					g.SmoothingMode = SmoothingMode.HighQuality;
					break;
			}

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
				case UmlRelationType.Aggregation:
				case UmlRelationType.Composition:
					Geometry.point_rhomb_on_segment( out pnts, x1, y1, x2, y2, 7, 5 );
					g.FillPolygon( type == UmlRelationType.Composition ? Brushes.Black : Brushes.White, pnts );
					g.DrawPolygon( color, pnts );
					if( nav == GuiConnectionNavigation.Left ) {
						x1 = pnts[2].X; y1 = pnts[2].Y;
						goto case UmlRelationType.Association;
					} else if( nav == GuiConnectionNavigation.Right )
						goto case UmlRelationType.Association;
					break;
				case UmlRelationType.Association:
					if( nav == GuiConnectionNavigation.Left ) {
						Geometry.point_triangle_on_segment( out pnts, x1, y1, x2, y2, 7, 5 );
						g.DrawLines( color, pnts );
					} else if( nav == GuiConnectionNavigation.Right ) {
						pnt = (GuiPoint)ipoints[ipoints.Count-2];
						x1 = second.x + r.X - offx; y1 = second.y + r.Y - offy;
						x2 = pnt.x + r.X - offx; y2 = pnt.y + r.Y - offy;
						Geometry.point_triangle_on_segment( out pnts, x1, y1, x2, y2, 7, 5 );
						g.DrawLines( color, pnts );
					}
					break;
				case UmlRelationType.Inheritance:
				case UmlRelationType.Realization:
					Geometry.point_triangle_on_segment( out pnts, x1, y1, x2, y2, 10, 7 );
					g.FillPolygon( Brushes.White, pnts );
					g.DrawPolygon( color, pnts );
					break;
				case UmlRelationType.Dependency:
					Geometry.point_triangle_on_segment( out pnts, x1, y1, x2, y2, 7, 5 );
					g.DrawLines( color, pnts );
					break;
				case UmlRelationType.Attachment:
					break;
			}
			g.SmoothingMode = SmoothingMode.Default;

			switch( type ) {
				case UmlRelationType.Attachment:
				case UmlRelationType.Dependency:
				case UmlRelationType.Realization:
					p.Dispose();
					break;
			}
		}

		private void DrawBesierConnection( Graphics g, Rectangle r, int offx, int offy ) {
		}

		internal static int decor_size = 24;

		public override bool NeedRepaint(Rectangle page) {

			bool need = false;

			switch( style ) {
				case GuiConnectionStyle.Line:
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
					need |= page.IntersectsWith( new Rectangle( first.x - decor_size, first.y - decor_size, decor_size*2, decor_size*2 ) );
					need |= page.IntersectsWith( new Rectangle( second.x - decor_size, second.y - decor_size, decor_size*2, decor_size*2 ) );
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
					parent.cview.InvalidatePage( new Rectangle(first.x - decor_size, first.y - decor_size, 2*decor_size, 2*decor_size ) );
					parent.cview.InvalidatePage( new Rectangle(second.x - decor_size, second.y - decor_size, 2*decor_size, 2*decor_size ) );
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

		public override Rectangle ContainingRect {
			get {
				Rectangle r = new Rectangle(first.x - decor_size, first.y - decor_size, 2*decor_size, 2*decor_size);
				for( int i = 1; i < ipoints.Count; i++ )
					r = Rectangle.Union( r, new Rectangle(((GuiPoint)ipoints[i]).x - decor_size, ((GuiPoint)ipoints[i]).y - decor_size, 2*decor_size, 2*decor_size) );

				if( children != null )
					foreach( GuiBinded b in children )
						r = Rectangle.Union( b.ContainingRect, r );
				return r;
			}
		}


		#endregion

		#region "Fixup functions: CheckIntersection, PostLoad, ConnectionCreated, DoCreationFixup"

		public bool CheckIntersection( ArrayList /* of IAroundObject */ arobjs, Hashtable states ) {
			return Style.CheckIntersection( arobjs, states );
		}

		public void DoCreationFixup() {
			Style.DoCreationFixup(false);
		}

		private GuiBindedString create_label( string label, int ux, float uy, Geometry.Direction dir, int offset, bool hidden ) {
			GuiBindedString bs = new GuiBindedString( label, this, 0, -offset, ux, uy, hidden );

			switch( dir ) {
				case Geometry.Direction.West:
					bs.pos_x -= bs.place.Width;
					bs.RecalculatePosition();
					break;
				case Geometry.Direction.North:
					bs.pos_y -= bs.place.Height;
					bs.RecalculatePosition();
					break;
			}
			return bs;
		}

		// registers connection in StaticView structures:
		public void ConnectionCreated( StaticView parent_view, string role1, string role2, string name, string stereo ) {

			//  1. register connection ipoints in objects
			first.item.add_connection_point( first );
			second.item.add_connection_point( second );
			//  2. create ID for connection
			id = parent_view.RegisterItemID( type.ToString(), this );
			//  3. create default roles
			first.role = create_label( role1 != null ? role1 : "Left", -1, 0f, first.hyphen_dir, 0, role1 == null );
			second.role = create_label( role2 != null ? role2 : "Right", -second.number_in_conn-1, 0f, second.hyphen_dir, 0, role2 == null );
			conn_name = create_label( name != null ? name : "Name", Int32.MaxValue, 0f, Geometry.Direction.Null, 0, name == null );
			conn_stereo = new GuiBindedStereotype( stereo != null ? stereo : "Usage", this, 0, -15, Int32.MaxValue, 0f, stereo == null );
			//  4. register children, make them drawable and serializable
			add_child( first, "LeftPoint" );
			add_child( second, "RightPoint" );
			add_child( first.role, "Role 1" );
			add_child( second.role, "Role 2" );
			add_child( conn_name, "Name" );
			add_child( conn_stereo, "Stereotype" );

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
			conn_name = find_child( "Name" ) as GuiBindedString;
			conn_stereo = find_child( "Stereotype" ) as GuiBindedStereotype;
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
					break;
				case GuiConnectionStyle.Segmented:
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

			// specific UC (MaxValue,0) means center of connection
			if( ux == Int32.MaxValue ) {
				if( (ipoints.Count%2) == 0 ) {
					ux = ipoints.Count/2-1;
					uy = .5f;
				} else {
					ux = -ipoints.Count/2-1;
				}
			}

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

		#region MENU

		public void ChangeStyleClick( object o, EventArgs ev ) {
			ObjectState before = GetState();
			Invalidate();
			switch( (ToolBarIcons)(o as FlatMenuItem).ImageIndex ) {
				case ToolBarIcons.straight_conn:	// Line
					style = GuiConnectionStyle.Line;
					break;
				case ToolBarIcons.segmented_conn: // Segmented
					style = GuiConnectionStyle.Segmented;
					break;
				case ToolBarIcons.quadric_conn:	// Quadric
					style = GuiConnectionStyle.Quadric;
					break;
				case ToolBarIcons.curved_conn:	// Bezier (disabled)
					break;
			}
			first.UpdatePosition(false);
			second.UpdatePosition(false);
			Style.DoCreationFixup(true);
			Invalidate();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void ChangeTypeClick( object o, EventArgs ev ) {
			ObjectState before = GetState();
			this.Invalidate();
			switch( (ToolBarIcons)(o as FlatMenuItem).ImageIndex ) {
				case ToolBarIcons.conn_inher:	// Inheritance
					type = UmlRelationType.Inheritance;
					break;
				case ToolBarIcons.conn_assoc:	// Association
					type = UmlRelationType.Association;
					break;
				case ToolBarIcons.conn_aggregation: // Aggregation
					type = UmlRelationType.Aggregation;
					break;
				case ToolBarIcons.conn_composition:	// Composition
					type = UmlRelationType.Composition;
					break;
				case ToolBarIcons.conn_attachm:	// Attachment
					type = UmlRelationType.Attachment;
					break;
				case ToolBarIcons.conn_realiz:	// Realization
					type = UmlRelationType.Realization;
					break;
				case ToolBarIcons.conn_dependence:	// Dependency
					type = UmlRelationType.Dependency;
					break;
			}
			this.Invalidate();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void ChangeNavigationClick( object o, EventArgs ev ) {
			ObjectState before = GetState();
			this.Invalidate();
			switch( (o as FlatMenuItem).Index ) {
				case 0: // None
					nav = GuiConnectionNavigation.None;
					break;
				case 1: // Left
					nav = GuiConnectionNavigation.Left;
					break;
				case 2: // Right
					nav = GuiConnectionNavigation.Right;
					break;
			}
			this.Invalidate();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void DisplayOptions( object o, EventArgs ev ) { 
			switch( (o as FlatMenuItem).Index ) {
				case 0: // Show roles
					ShowRoles = !ShowRoles;
					break;
				case 1: // Show connection name
					conn_name.Visible = !conn_name.Visible;
					break;
				case 2: // Show stereotype
					conn_stereo.Visible = !conn_stereo.Visible;
					break;

				case 4: // show all
					conn_stereo.Visible = conn_name.Visible = ShowRoles = true;
					break;
				case 5: // hide all
					conn_stereo.Visible = conn_name.Visible = ShowRoles = false;
					break;
			}
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {

			FlatMenuItem curr;
			EventHandler evh;

			// Style
			curr = new FlatMenuItem( "Style", null, 0, false );
			evh = new EventHandler( ChangeStyleClick );
			parent.AddItem( curr, "Line", ToolBarIcons.straight_conn, this.style == GuiConnectionStyle.Line, evh );
			parent.AddItem( curr, "Segmented", ToolBarIcons.segmented_conn, this.style == GuiConnectionStyle.Segmented, evh );
			parent.AddItem( curr, "Quadric", ToolBarIcons.quadric_conn, this.style == GuiConnectionStyle.Quadric, evh );
			parent.AddItem( curr, "Bezier", ToolBarIcons.curved_conn, this.style == GuiConnectionStyle.Besier, null );
			m.MenuItems.Add( curr );

			// Type
			evh = new EventHandler( ChangeTypeClick );
			curr = new FlatMenuItem( "Type", null, 0, false );
			parent.AddItem( curr, "Inheritance", ToolBarIcons.conn_inher, this.type == UmlRelationType.Inheritance, evh );
			parent.AddItem( curr, "Association", ToolBarIcons.conn_assoc, this.type == UmlRelationType.Association, evh );
			parent.AddItem( curr, "Aggregation", ToolBarIcons.conn_aggregation, this.type == UmlRelationType.Aggregation, evh );
			parent.AddItem( curr, "Composition", ToolBarIcons.conn_composition, this.type == UmlRelationType.Composition, evh );
			parent.AddItem( curr, "Attachment", ToolBarIcons.conn_attachm, this.type == UmlRelationType.Attachment, evh );
			parent.AddItem( curr, "Dependancy/Usage", ToolBarIcons.conn_dependence, this.type == UmlRelationType.Dependency, evh );
			parent.AddItem( curr, "Realization", ToolBarIcons.conn_realiz, this.type == UmlRelationType.Realization, evh );
			m.MenuItems.Add( curr );

			// Navigation
			evh = new EventHandler( ChangeNavigationClick );
			curr = new FlatMenuItem( "Navigation", null, 0, false );
			parent.AddItem( curr, "None", ToolBarIcons.None, this.nav == GuiConnectionNavigation.None, evh );
			parent.AddItem( curr, "Left", ToolBarIcons.None, this.nav == GuiConnectionNavigation.Left, evh );
			parent.AddItem( curr, "Right", ToolBarIcons.None, this.nav == GuiConnectionNavigation.Right, evh );
			curr.Enabled = ( this.type == UmlRelationType.Association || this.type == UmlRelationType.Aggregation || this.type == UmlRelationType.Composition );
			m.MenuItems.Add( curr );

			// Display Options
			evh = new EventHandler( DisplayOptions );
			curr = new FlatMenuItem( "Display &Options...", null, 0, false );
			parent.AddItem( curr, "Show roles", ToolBarIcons.None, ShowRoles, evh );
			parent.AddItem( curr, "Show connection name", ToolBarIcons.None, conn_name.Visible, evh );
			parent.AddItem( curr, "Show stereotype", ToolBarIcons.None, conn_stereo.Visible, evh );
			parent.AddItem( curr, "-", ToolBarIcons.None, false, null );
			parent.AddItem( curr, "Show all", ToolBarIcons.None, false, evh );
			parent.AddItem( curr, "Hide all", ToolBarIcons.None, false, evh );
			m.MenuItems.Add( curr );
		}

		internal bool ShowRoles {
			get {
                return !first.role.hidden && !second.role.hidden;
			}
			set {
				first.role.Visible = second.role.Visible = value;
			}
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
			public UmlRelationType type;
			public GuiConnectionStyle style;
			public GuiConnectionNavigation nav;
			public System.Collections.ArrayList ipoints = new ArrayList();
			public ObjectState p1, p2;
			public bool hidden;
		}


		public void Apply(ObjectState v) {
			State state = v as State;

			Invalidate();
			invalidate_children();
			style = state.style;
			type = state.type;
			nav = state.nav;

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
			SetHidden( state.hidden ); 
		}

		public ObjectState GetState() {         
			State st = new State();
			st.type = type;
			st.style = style;
			st.nav = nav;
			st.hidden = hidden;
			
			for( int i = 1; i < ipoints.Count - 1; i++ )
				st.ipoints.Add( (ipoints[i] as GuiIntermPoint).Clone() );
			st.p1 = first.GetState();
			st.p2 = second.GetState();
			return st;
		}

		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList l = new ArrayList();
				foreach( GuiConnectionPoint p in cpoints )
					l.Add( p.root );
				return l;
			} 
		}

		#endregion

		#region Hidden

		[XmlIgnore] public override bool Hidden {
			get {
				return ( hidden || ((GuiObject)first.item).Hidden || ((GuiObject)second.item).Hidden );
			}
			set {
				if( hidden != value ) {
					ObjectState before = GetState();
					SetHidden( value );
					parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
				}
			}
		}

		[XmlIgnore] public override bool RawHidden {
			get {
				return hidden;
			}
		}

		protected void SetHidden( bool val ) {
			if( val != hidden ) {
				hidden = val;
				Invalidate();
				invalidate_children();
				parent.InvalidateAllAssociated( this );
			}
		}

		#endregion
	}
}