using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace UMLDes.GUI {

	public abstract class GuiPolygonItem : GuiItem, ISelectable, IValidateConnection, IMoveMultiple, IRemoveable, IHasCenter, INeedRefresh {

		public const int inflate = 4;

		[XmlAttribute] public string name;
		[XmlAttribute] public int X, Y, Width, Height;
		[XmlAttribute] public bool hidden;

		public GuiPolygonItem() {
			this.name = null;
			X = Y = 0;
			Width = Height = 40;
		}

		public override string Name {
			get {
				return UMLDes.Model.UmlModel.LongTypeName2Short( name );
			}
		}

		public abstract void Paint( Graphics g, int x, int y );

		public override void Paint(Graphics g, Rectangle r, int offx, int offy) {
			int x = X + r.X - offx, y = Y + r.Y - offy;

			for( int i = 0; i < edges.Length; i++ ) {
				shifted_edges[i].X = edges[i].X + r.X - offx;
				shifted_edges[i].Y = edges[i].Y + r.Y - offy;
			}

			if( selected )
				using( Pen p = new Pen( new HatchBrush( HatchStyle.Percent50, Color.White, Color.Gray ), inflate*2 ) )
					g.DrawPolygon( p, shifted_edges );
			g.FillPolygon( Brushes.White, shifted_edges, FillMode.Winding );
			g.DrawPolygon( Pens.Black, shifted_edges );

			Paint( g, x, y );
		}


		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList l = new ArrayList();
				foreach( GuiConnectionPoint p in cpoints )
					l.Add( p.root );
				return l;
			} 
		}

		[XmlIgnore]	internal Point[] edges, shifted_edges;

		[XmlIgnore]	public override Point[] con_edges {
			get {
				return edges;
			}
		}

		[XmlIgnore]	public override Point[] con_points {
			get {
				return null;
			}
		}

		protected abstract Point[] GetPoints();

		protected void setup_edges() {
			edges = GetPoints();
			Rectangle r = new Rectangle( edges[0].X, edges[0].Y, 1, 1 );

			foreach( Point p in edges ) {
                if( p.X < r.X )
					r.X = p.X;
				if( p.X > r.Right )
					r.Width = p.X - r.X;
				if( p.Y < r.Y )
					r.Y = p.Y;
				if( p.Y > r.Bottom )
					r.Height = p.Y - r.Y;
			}
			shifted_edges = new Point[edges.Length];
			place = Rectangle.Inflate( r, inflate, inflate );
		}

		protected void StateChanged() {
			Invalidate();
			parent.RefreshObject( this );
			setup_edges();
			notify_children();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}

		#region IValidateConnection

		public bool validate_connection( IAcceptConnection obj, GuiConnection connection ) {
			return true;
		}

		#endregion

		#region IMoveable members

		public void Moving( int x, int y, ref int ux, ref float uy ) {
			if( X != x - ux || Y != y - (int) uy ) {

				Invalidate();
				X = x - ux;
				Y = y - (int)uy;

				setup_edges();
				notify_children();
				foreach( GuiConnectionPoint p in cpoints )
					p.UpdatePosition( true );
				Invalidate();
			}
		}

		public void Moved() {
			foreach( GuiConnectionPoint p in cpoints )
				p.Moved();
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		#endregion

		#region ISelectable members

		public bool TestSelected(Rectangle sel) {
			if( sel.IntersectsWith( place ) )
				return true;
			return false;
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {

			ux = x - X;
			uy = y - Y;

			System.Drawing.Drawing2D.GraphicsPath gp = new GraphicsPath();
			gp.AddLines( edges );
			return gp.IsVisible(x,y);//place.Contains( x, y );
		}

		#endregion

		#region IRemoveable members

		public override bool Destroy( ) {
			while( cpoints.Count > 0 )
				parent.Destroy( (cpoints[0] as GuiConnectionPoint).root as IRemoveable );

			Invalidate();
			base.Destroy();
			parent.UnregisterObject( this.ID, this );
			return true;
		}

		public override void Restore() {
			id = parent.RegisterItemID( this.name, this );
			base.Restore();
			Invalidate();
		}

		#endregion

		#region Moving in group

		public bool CanMoveInGroup { get { return true; } }

		public void ShiftShape( int dx, int dy ) {
			Invalidate();
			X += dx;
			Y += dy;

			setup_edges();
			notify_children();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}

		#endregion

		#region IHasCenter Members

		public Point Center {
			get {
				return new Point( place.X + place.Width/2, place.Y + place.Height/2 );
			}
		}

		#endregion

		#region INeedRefresh Members

		public abstract void RefreshView(Graphics g);

		#endregion

		#region IAroundObject Members

		public override Rectangle AroundRect {
			get {
				return Rectangle.Inflate( place, GuiConnectionPoint.DELTA - inflate-1, GuiConnectionPoint.DELTA - inflate-1 );
			}
		}

		#endregion

		#region Hidden

		[XmlIgnore] public override bool Hidden {
			get {
				return hidden;
			}
			set {
				if( hidden != value ) {
					ObjectState before = ((IStateObject)this).GetState();
					SetHidden( value );
					parent.Undo.Push( new StateOperation( (IStateObject)this, before, ((IStateObject)this).GetState() ), false );
				}
			}
		}

		protected void SetHidden( bool val ) {
			if( val != hidden ) {
				hidden = val;
				Invalidate();
				parent.InvalidateAllAssociated( (IStateObject)this );
			}
		}

		#endregion
	}

}