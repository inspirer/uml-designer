using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace UMLDes.GUI {

	// root of GuiPoint must be GuiConnection
	public abstract class GuiPoint : GuiBinded, ISelectable {

		[XmlAttribute] public int x, y;
		protected const int POINT_NORMAL_SIZE = 4;
		protected const int POINT_SELECTED_SIZE = 6;

		#region vars: PointNumber

		protected int point_number;

		public virtual int number_in_conn {
			get {
				return point_number;
			}
			set {
				point_number = value;
				type = "Point #" + value;
			}
		}

		#endregion

		// TODO
		public virtual void coord_getxy( int ux, float uy, out int x, out int y ) {
			x = y = 0;
		}

		// TODO
		public virtual bool coord_nearest( int x, int y, out int ux, out float uy ) {
			ux = 0; uy = 0f;
			return false;
		}

		public void translate_coords( ref int ux, ref float uy ){
		}

		public override void PostLoad() {
			base.PostLoad();
		}

		public virtual bool TestSelected(Rectangle sel) {
			// TODO:  Add GuiPoint.TestSelected implementation
			return false;
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {
			ux = 0;
			uy = 0;
			int dist = (selected || root.selected ? POINT_SELECTED_SIZE : POINT_NORMAL_SIZE) / 2 + 1;

			if( Math.Abs( x - this.x ) <= dist && Math.Abs( y - this.y ) <= dist ) {
				ux = x - this.x;
				uy = y - this.y;
				return true;
			}

			return false;
		}

		public virtual void UpdatePlaceRect() {
			place = new Rectangle( x - POINT_SELECTED_SIZE/2, y - POINT_SELECTED_SIZE/2, POINT_SELECTED_SIZE+1, POINT_SELECTED_SIZE+1 );
		}

		#region Paint functions

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			g.SmoothingMode = SmoothingMode.HighQuality;
			if( selected ) {
				g.FillRectangle( Brushes.SteelBlue, x + r.X - offx-POINT_SELECTED_SIZE/2, y + r.Y - offy-POINT_SELECTED_SIZE/2, POINT_SELECTED_SIZE, POINT_SELECTED_SIZE );
			} else if( root.selected ) {
				//g.DrawRectangle( Pens.Green, x + r.X - offx-POINT_SELECTED_SIZE/2, y + r.Y - offy-POINT_SELECTED_SIZE/2, POINT_SELECTED_SIZE, POINT_SELECTED_SIZE );
				g.FillEllipse( Brushes.SteelBlue, x + r.X - offx-POINT_SELECTED_SIZE/2, y + r.Y - offy-POINT_SELECTED_SIZE/2, POINT_SELECTED_SIZE, POINT_SELECTED_SIZE );
			} else {
				//g.DrawRectangle( Pens.Black, x + r.X - offx-POINT_NORMAL_SIZE/2, y + r.Y - offy-POINT_NORMAL_SIZE/2, POINT_NORMAL_SIZE, POINT_NORMAL_SIZE );
				g.FillEllipse( Brushes.SteelBlue, x + r.X - offx-POINT_NORMAL_SIZE/2, y + r.Y - offy-POINT_NORMAL_SIZE/2, POINT_NORMAL_SIZE, POINT_NORMAL_SIZE );
			}
			g.SmoothingMode = SmoothingMode.Default;
		}

		public override void Invalidate() {
			UpdatePlaceRect();
			base.Invalidate ();
		}

		public override bool NeedRepaint(Rectangle page) {
			UpdatePlaceRect();
			return base.NeedRepaint (page);
		}

		#endregion
	}

	public class GuiConnectionPoint : GuiPoint, ISelectable, IMoveable, IStateObject {

		public const int DELTA = 10;

		[XmlIgnore] public IAcceptConnection item;
		[XmlIgnore] public GuiBindedString role;

		[XmlAttribute] public int ux;
		[XmlAttribute] public float uy;

		string loadtime_id;
		[XmlAttribute] public string item_id {
			get { return item.ID; }
			set { loadtime_id = value; }
		}

		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList r = new ArrayList(); 
				r.Add( root );
				return r;
			} 
		}

		#region vars: PointNumber

		public override int number_in_conn {
			get {
				return point_number;
			}
			set {
				point_number = value;
			}
		}

		#endregion

		[XmlAttribute] public bool hyphen = true;
		[XmlIgnore] bool Hyphen { get { return hyphen && ((GuiConnection)root).CanHaveHyphen; } }
		[XmlIgnore] Geometry.Direction hyphen_dir = Geometry.Direction.Null;
		[XmlIgnore] int hx, hy;

		#region Constructors

		public GuiConnectionPoint() {
		}

		public GuiConnectionPoint( int x, int y, int num ) {
			this.x = x;
			this.y = y;
			number_in_conn = num;
		}

		public GuiConnectionPoint( IAcceptConnection c, int ux, float uy, int num ) {
			item = c;
			this.ux = ux;
			this.uy = uy;
			number_in_conn = num;
		}

		#endregion

		#region Universal Coords

		public override void coord_getxy( int ux, float uy, out int x, out int y ) {
			x = this.x;
			y = this.y;
		}

		#endregion

		#region Postload

		public override void PostLoad() {
			item = parent.gui_objects[loadtime_id] as IAcceptConnection;
			if( item == null )
				throw new ArgumentException( "wrong id" );
			item.add_connection_point( this );
			base.PostLoad();
		}

		#endregion

		#region Paint
		
		public override void Paint(Graphics g, Rectangle r, int offx, int offy) {
			if( Hyphen )
				g.DrawLine( Pens.Black, hx + r.X - offx, hy + r.Y - offy, x + r.X - offx, y + r.Y - offy );
			base.Paint( g, r, offx, offy );
		}

		#endregion

		#region UpdateCoords, UpdatePlaceRect, UpdatePosition

		public override void UpdateCoords( GuiObject orig ) {
			if( orig == item )
				item.translate_coords( ref ux, ref uy );
		}

		public void UpdatePosition( bool process_endpoints ) {

			if( item != null ) {
				IHyphenSupport m = item as IHyphenSupport;
				if( m != null )
					hyphen_dir = m.direction( ux );
				int tx, ty, sx, sy;
				item.coord_getxy( ux, uy, out tx, out ty );
				Geometry.shift_direction( tx, ty, out sx, out sy, hyphen_dir, DELTA );

				if( (!Hyphen && (tx != x || ty != y)) || ( Hyphen && (sx != x || sy != y) ) || hx == 0 && hy == 0 ) {
					Invalidate();					
					root.Invalidate();
					if( !Hyphen ) {
						x = tx;
						y = ty;
					} else {
						x = sx;
						y = sy;
						hx = tx;
						hy = ty;
					}
					if( root != null && process_endpoints )
						(root as GuiConnection).EndPointPositionChanging( this );
					UpdatePlaceRect();
					root.Invalidate();
					Invalidate();
				}
			}
		}

		public override void UpdatePlaceRect() {
			int x1 = Math.Min( hx, x - POINT_SELECTED_SIZE/2 ), y1 = Math.Min( hy, y - POINT_SELECTED_SIZE/2 );
			int x2 = Math.Max( hx, x + POINT_SELECTED_SIZE/2 ), y2 = Math.Max( hy, y + POINT_SELECTED_SIZE/2 );
			place = new Rectangle( x1, y1, x2 - x1 + 1, y2 - y1 + 1 );
		}

		#endregion

		#region Moveable

		public virtual void Moving(int x, int y, ref int ux, ref float uy) {
			item.coord_nearest( x, y, out this.ux, out this.uy );
			UpdatePosition( true );
		}

		public void Moved() {
			(root as GuiConnection).EndPointPositionChanged();
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		#endregion

		#region IStateObject

		class State : ObjectState {
			public int ux;
			public float uy;
			public bool hyphen = true;
			public Geometry.Direction hyphen_dir = Geometry.Direction.Null;
			public int hx, hy, x, y;
		}

		public void Apply(ObjectState v) {
			State s = v as State;
			ux = s.ux;
			uy = s.uy;
			x = s.x;
			y = s.y;
			hx = s.hx;
			hy = s.hy;
			hyphen = s.hyphen;
			hyphen_dir = s.hyphen_dir;
			UpdatePlaceRect();
			Invalidate();
		}

		public ObjectState GetState() {
			State s = new State();
			s.ux = ux;
			s.uy = uy;
			s.x = x;
			s.y = y;
			s.hx = hx;
			s.hy = hy;
			s.hyphen = hyphen;
			s.hyphen_dir = hyphen_dir;
			return s;
		}

		#endregion
	}

	public class GuiIntermPoint : GuiPoint, ICloneable, IMoveRedirect {

		//public override bool Destroy() {
		//	return (root as GuiConnection).RemoveIntermediate( point_number );
		//}

		// forward call to the upper Connection
		public virtual IMoveable MoveRedirect( ref int ux, ref float uy ) {
			ux = -point_number-1;
			uy = 0;
			return root as IMoveable;
		}

		public virtual object Clone() {
			GuiIntermPoint pt = new GuiIntermPoint();
			pt.x = this.x;
			pt.y = this.y;
			pt.number_in_conn = this.number_in_conn;
			pt.root = this.root;
			pt.parent = this.parent;
			return pt;
		}
	}
}