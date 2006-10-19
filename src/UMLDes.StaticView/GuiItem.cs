using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace UMLDes.GUI {

	/// <summary>
	/// GUI object which can repaint itself, accept children and have PostLoad method
	/// </summary>
	public abstract class GuiObject : IPostload, IDrawable {
		[XmlIgnore] public Rectangle place;
		[XmlIgnore]	public StaticView parent;

		[XmlIgnore]	public virtual bool Hidden { get { return false; } set {} }
		[XmlIgnore] public virtual bool RawHidden { get { return Hidden; } }

		#region children

		[
		 XmlElement("String", typeof(GuiBindedString)), 
		 XmlElement("ConnectionPoint",typeof(GuiConnectionPoint)),
		 XmlElement("Stereotype", typeof(GuiBindedStereotype)),
		 XmlElement("Point",typeof(GuiIntermPoint)) 
		]
        public ArrayList children;
		ArrayList _children;       // used by IRemoveable

		public void add_child( GuiBinded p, string type ) {
			p.root = this;
			if( children == null )
				children = new ArrayList();
			children.Add( p );
			if( type != null )
				p.type = type;
		}

		public void remove_child( GuiBinded p ) {
			if( children != null )
				children.Remove( p );
		}

		// base Remove logic
		public virtual bool Destroy() {
			if( children != null ) {
				_children = children;
				children = null;
				foreach( GuiBinded b in _children ) {
					if( b is IRemoveableChild )
						( b as IRemoveableChild ).Unlink();
					else
						b.Invalidate();
				}
			}
			return false;
		}

		public virtual void Restore() {
			children = _children;
			if( children != null )
				foreach( GuiBinded b in children )
					if( b is IRemoveableChild )
						( b as IRemoveableChild ).Relink();
			invalidate_children();
			_children = null;
		}

		public GuiBinded find_child( string type ) {
			if( children != null )
				foreach( GuiBinded p in children )
					if( p.type.Equals( type ) )
						return p;
			return null;
		}

		public void invalidate_children() {
			if( children != null )
				foreach( GuiObject o in children )
					o.Invalidate();
		}

		public void notify_children() {
			if( children != null )
				foreach( GuiBinded o in children )
					o.ParentChanged();
		}		

		#endregion

		// drawing
		public virtual void Invalidate() {
			parent.cview.InvalidatePage( place );
		}

		public virtual Rectangle ContainingRect {
			get {
				if( children != null ) {
					Rectangle sum = place;
					foreach( GuiBinded b in children )
						sum = Rectangle.Union( b.ContainingRect, sum );
					return sum;
				} else 
					return place;
			}
		}

		public abstract void Paint( Graphics g, Rectangle r, int offx, int offy );
		// loading
		public virtual void PostLoad() {
			if( children != null )
                foreach( GuiBinded p in children ) {
					p.root = this;
					p.parent = parent;
					p.PostLoad();
				}
		}

		// selection
		[XmlIgnore] public bool selected;

		public virtual void SelectionChanged() {}

		// TODO: make abstract, remove it from GuiObject
		public virtual bool NeedRepaint(Rectangle page) {
			return place.IntersectsWith(page);
		}

		public virtual void ModifyUniversalCoords() {
			if( children != null ) {
				foreach( GuiBinded c in children ) {
					c.UpdateCoords( this );
				}
			}
		}
	}

	public abstract class GuiActive : GuiObject, IHasID {
		[XmlAttribute] public string id;

		public string ID { get { return id; } }
		abstract public string Name { get; }
	}

	public abstract class GuiBinded : GuiObject {
		[XmlIgnore] public GuiObject root;
		[XmlAttribute] public string type;

		public virtual void ParentChanged() { }
		public virtual void UpdateCoords( GuiObject orig ) {}

		public override void SelectionChanged() {
			// TODO ???? 
			if( selected )
				parent.SelectedObjects.Add( root );
		}

	}

	/// <summary>
	/// Root class for all classificators (actor, class, component, node, signal etc.)
	/// </summary>
	public abstract class GuiItem : GuiActive, IAcceptConnection, IHyphenSupport, IAroundObject {

		// item can accept connections

		[XmlIgnore]	public ArrayList cpoints = new ArrayList();

		public void add_connection_point( GuiConnectionPoint p ) {
			cpoints.Add( p );
		}

		public void remove_connection_point( GuiConnectionPoint p ) {
			cpoints.Remove( p );
		}

		public abstract Rectangle AroundRect { get; }

		public Geometry.Direction direction( int ux ) {
			Point[] edges = con_edges;
			int max = edges.Length;
			if( ux < 0 || ux >= max )
				return Geometry.Direction.Null;
			int x1 = edges[ux].X, y1 = edges[ux].Y, x2 = edges[(ux+1)%max].X, y2 = edges[(ux+1)%max].Y;

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

		[XmlIgnore]	public abstract Point[] con_points { get; }
		[XmlIgnore]	public abstract Point[] con_edges { get; }

		public bool coord_nearest( int x, int y, out int ux, out float uy ) {
			Point[] points = con_points;
			Point[] edges = con_edges;
			float distance = 10000*10000; // infinity
			int nx = x, ny = y, nux = 0;
			float nuy = 0;

			if( points != null ) {
				for( int i = 0; i < points.Length; i++ ) {
					Point p = points[i];
					if( (p.X - x)*(p.X - x) + (p.Y - y)*(p.Y - y) < distance ) {
						distance = (p.X - x)*(p.X - x) + (p.Y - y)*(p.Y - y);
						nx = p.X; ny = p.Y; nux = -1; nuy = i;
					}                
				}
			}

			if( edges != null && distance > 4*4 ) {
				for( int i = 0; i < edges.Length; i++ ) {
					Point p1 = edges[i], p2 = edges[(i+1)%edges.Length];
					int x3, y3;
					Geometry.nearest_point_from_segment( x, y, p1, p2, out x3, out y3 );
					float dist = (((float)(x3-x))*(x3-x) + ((float)(y3-y))*(y3-y));

					if( dist < distance ) {
						distance = dist;
						nx = x3; ny = y3;
						nux = i; 
						nuy = (float)(Math.Abs(nx - p1.X) + Math.Abs(ny - p1.Y)) / (Math.Abs(p2.X - p1.X) + Math.Abs(p2.Y - p1.Y));

						if( Math.Abs( x3 - (p1.X + p2.X)/2 ) < 8 && Math.Abs( y3 - (p1.Y + p2.Y)/2 ) < 8 )
							nuy = 0.5f;
					}
				}

			}
			
			x = nx; y = ny;
			ux = nux; uy = nuy;
			return distance < 10000*10000;
		}

		public void coord_getxy( int ux, float uy, out int x, out int y ) {
			if( ux == -1 ) {
				Point p = con_points[(int)uy];
				x = p.X; y = p.Y;
			} else {
				Point[] edges = con_edges;
				Point p1 = edges[ux], p2 = edges[(ux+1)%edges.Length];
				x = (int)(p2.X * uy + p1.X * (1 - uy));
				y = (int)(p2.Y * uy + p1.Y * (1 - uy));
			}
		}

		public void translate_coords( ref int ux, ref float uy ){
			ux = ux;
			uy = uy;
		}

		public float get_empty_point_on_edge( int ux ) {

			int i = 0;
			float[] pnts = new float[16];

			foreach( GuiConnectionPoint p in cpoints )
				if( p.ux == ux ) {
					if( i == 14 )
						return .5f;
					pnts[i++] = p.uy;
				}

			pnts[i++] = 0f;
			pnts[i++] = 1f;
			Array.Sort( pnts, 0, i );

			float max_length = 0.05f;
			float new_point = .5f;

			for( int e = 0; e < i - 1; e++ ) {
				if( pnts[e+1] - pnts[e] > max_length ) {
					new_point = (pnts[e+1] + pnts[e])/2;
					max_length = pnts[e+1] - pnts[e];
				}
			}

            return new_point;
		}
	}
}