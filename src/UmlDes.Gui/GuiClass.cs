using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using CDS.CSharp;
using CDS.UML;
using CDS.Controls;

namespace CDS.GUI {

	/// <summary>
	/// Represents a member of the classificator
	/// </summary>
	public class GuiMember {

		public UmlMember st;
		public SizeF size;
		public int y_offset;
		public Rectangle area;
		public FontStyle font {
			get { return (st._abstract ? FontStyle.Italic : 0) | ( st._static ? FontStyle.Underline : 0); }
		}

		public GuiMember() {
		}

		public static GuiMember fromUML( UmlMember st ) {
			GuiMember m = new GuiMember();
			m.st = st;
			return m;
		}
	}

	public class GuiSection {
		public UmlSection st;
		public ArrayList members = new ArrayList();

		public static GuiSection fromUML( UmlSection st ) {
            GuiSection s = new GuiSection();
			s.st = st;
			s.members = new ArrayList();
			foreach( UmlMember m in st.members )
				s.members.Add( GuiMember.fromUML( m ) );
			return s;
		}
	}

	/// <summary>
	/// UML representation of class (classificator)
	/// </summary>
	public class GuiClass : GuiItem, ISelectable, IValidateConnection, IMoveMultiple, IRemoveable, IStateObject, IDropMenu, INeedRefresh {

		[XmlAttribute] public string name;
		public bool show_members = true, show_vars = true;

		[XmlIgnore] public FontStyle font {
			get { return (st._abstract ? FontStyle.Italic : 0); }
		}

		[XmlIgnore] public ArrayList Associated { 
			get { 
				ArrayList l = new ArrayList();
				foreach( GuiConnectionPoint p in cpoints )
					l.Add( p.root );
				if( qualifiers != null )
					foreach( GuiQualifier q in qualifiers )
						l.Add( q );
				return l;
			} 
		}

		[XmlIgnore] public GuiSection attrs = new GuiSection(), opers = new GuiSection();
		[XmlIgnore] public ArrayList sections = new ArrayList();
		[XmlIgnore] public UmlClass st;
		[XmlIgnore]	Point[] edges;
		[XmlIgnore] SizeF name_size, stereo_size;

		[XmlIgnore] public ArrayList qualifiers;

		public const int padding = 10, line_space = 2, vpadding = 6;

		public GuiClass() {
			this.name = null;
			parent = null;
			place = new Rectangle( -1, -1, 50, 50 );
			edges = new Point[] { place.Location, new Point(place.Left,place.Bottom), new Point(place.Right,place.Bottom), new Point(place.Right,place.Top) };
		}

		/// <summary>
		/// Calculates width and height of object
		/// </summary>
		/// <param name="g">graphics object for measurements</param>
		public void RefreshView( Graphics g ) {
			int width = 50, height = 0;

			name_size = g.MeasureString( name, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Bold|this.font) );
			width = Math.Max( (int)name_size.Width + 2*padding, width );
			height = (int)name_size.Height + 2*vpadding;

			if( st.stereotype != null ) {
				stereo_size = g.MeasureString( st.stereotype, parent.cview.Font );
				width = Math.Max( (int)stereo_size.Width + 2*padding, width );
				height += (int)stereo_size.Height + line_space;
			} else
				stereo_size = SizeF.Empty;

			if( show_vars ) {
				height += 2*vpadding;

				foreach( GuiMember m in attrs.members ) {
					m.size = g.MeasureString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font) );
					width = Math.Max( (int)m.size.Width + 2*padding, width );
					height += (int)m.size.Height + line_space;
				}
			}

			if( show_members ) {
				height += 2*vpadding;

				foreach( GuiMember m in opers.members ) {
					m.size = g.MeasureString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font) );
					width = Math.Max( (int)m.size.Width + 2*padding, width );
					height += (int)m.size.Height + line_space;
				}
			}

			place.Width = width + 2*inflate;
			place.Height = height + 2*inflate;
		}

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			int x = place.X + r.X - offx + inflate, y = place.Y + r.Y - offy + inflate;

			int width = place.Width - 2*inflate, height = place.Height - 2*inflate;

			int textdx, curr_y = y + vpadding;
			g.FillRectangle( Brushes.WhiteSmoke, x, y, width-1, height-1 );
			g.DrawRectangle( Pens.Black, x, y, width-1, height-1 );

			if( st.stereotype != null ) {
				textdx = ( width - (int)stereo_size.Width ) / 2;
				g.DrawString( st.stereotype, parent.cview.Font, Brushes.Black, x + textdx, curr_y );
				curr_y += (int)stereo_size.Height + line_space;
			}

			textdx = ( width - (int)name_size.Width ) / 2;
			g.DrawString( name, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Bold|this.font), Brushes.Black, x + textdx, curr_y );
			curr_y += (int)name_size.Height;

			if( show_vars ) {
				curr_y += vpadding;
				g.DrawLine( Pens.Black, x, curr_y, x + width - 1, curr_y );
				curr_y += vpadding;
				foreach( GuiMember m in attrs.members ) {
					g.DrawString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font), Brushes.Black, x + padding, curr_y );
					m.y_offset = curr_y;
					curr_y += (int)m.size.Height + line_space;
				}
			}

			if( show_members ) {
				curr_y += vpadding;
				g.DrawLine( Pens.Black, x, curr_y, x + width - 1, curr_y );
				curr_y += vpadding;
				foreach( GuiMember m in opers.members ) {
					g.DrawString( m.st.fullname, parent.cview.GetFont(FontTypes.DEFAULT,m.font), Brushes.Black, x + padding, curr_y );
					m.y_offset = curr_y;
					curr_y += (int)m.size.Height + line_space;
				}
			}

			if( selected )
				using( Pen p = new Pen( new HatchBrush( HatchStyle.Percent50, Color.White, Color.Gray ), inflate ) ) {
					g.DrawRectangle( p, x - inflate/2, y - inflate/2, place.Width - inflate, place.Height - inflate );
				}
		}

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

		private void setup_edges() {
			edges[1].X = edges[0].X = place.Left + inflate;
			edges[3].Y = edges[0].Y = place.Top + inflate;
			edges[3].X = edges[2].X = place.Right - inflate;
			edges[1].Y = edges[2].Y = place.Bottom - inflate;
		}

		public void Moving( int x, int y, ref int ux, ref float uy ) {
			if( place.X != x - ux || place.Y != y - (int) uy ) {

				Invalidate();
				place.X = x;
				place.Y = y;

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
			if( qualifiers != null )
				foreach( GuiQualifier q in qualifiers )
					q.Moved();
		}

		public bool IsMoveable( int x, int y ) {
			return true;
		}

		public override void SelectionChanged() {
			if( qualifiers != null )
				foreach( GuiQualifier q in qualifiers )
					q.Invalidate();
		}

          
		public static GuiClass fromUML( UmlClass st ) {
			GuiClass s = new GuiClass();
			s.name = st.fullname;
			s.st = st;
			s.attrs = GuiSection.fromUML( st.attrs );
			s.opers = GuiSection.fromUML( st.opers );
			foreach( UmlSection c in st.sections )
				s.sections.Add( GuiSection.fromUML( c ) );
			return s;
		}

		public override void PostLoad() {
			st = (UmlClass)parent.proj.items[name];
			attrs = GuiSection.fromUML( st.attrs );
			opers = GuiSection.fromUML( st.opers );

			parent.RefreshObject( this );
			setup_edges();

			// add qualifiers
			if( children != null )
				foreach( GuiBinded b in children )
					if( b is GuiQualifier ) {
						if( qualifiers == null )
							qualifiers = new ArrayList();
						qualifiers.Add( b );
					}
			base.PostLoad();
			if( qualifiers != null )
				foreach( GuiQualifier q in qualifiers ) {
					parent.RefreshObject( q );
					q.UpdatePosition();
				}
		}

		public bool TestSelected(Rectangle sel) {
			if( sel.IntersectsWith( place ) )
				return true;
			return false;
		}

		public bool HasPoint(int x, int y, out int ux, out float uy ) {

			ux = x - place.X;
			uy = y - place.Y;
			return place.Contains( x, y );
		}

		public bool validate_connection( IAcceptConnection obj, GuiConnection connection ) {
			return true;
		}

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

		#region Moving in group

		public bool CanMoveInGroup { get { return true; } }

		public void ShiftShape( int dx, int dy ) {
			Invalidate();
			place.X += dx;
			place.Y += dy;

			setup_edges();
			notify_children();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			place.X = t.x;
			place.Y = t.y;

			setup_edges();
			Invalidate();
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = place.X;
			t.y = place.Y;
			return t;
		}

		#endregion

		#region Menu

		public void AddQualifier( object o, EventArgs ev ) {
			GuiQualifier q = GuiQualifier.create( 1, 0.3f, new UmlSection(), this, parent );
			parent.RefreshObject( q );
			q.UpdatePosition();
			if( qualifiers == null )
				qualifiers = new ArrayList();
			qualifiers.Add( q );
			add_child( q, null );
			q.Invalidate();
			parent.Undo.Push( new CreateOperation( q ), false );
		}

		public void DisplayOptions( object o, EventArgs ev ) { 
			switch( (o as FlatMenuItem).Index ) {
				case 0: // Attributes
					show_vars = !show_vars;
					break;
				case 1: // Operations
					show_members = !show_members;
					break;
				default:
					return;
			}
			Invalidate();
			parent.RefreshObject( this );
			setup_edges();
			notify_children();
			foreach( GuiConnectionPoint p in cpoints )
				p.UpdatePosition( true );
			Invalidate();
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			// New Qualifier
			FlatMenuItem mi = new FlatMenuItem( "Add qualifier", null, 0, false );
			mi.Click += new EventHandler( AddQualifier );
			m.MenuItems.Add( mi );
			// Diplay Options
			FlatMenuItem dispopt = new FlatMenuItem( "Display &Options...", null, 0, false );
			EventHandler hdl = new EventHandler( DisplayOptions );
			mi = new FlatMenuItem( "&Attributes", null, 0, show_vars );
			mi.Click += hdl;
			dispopt.MenuItems.Add( mi );
			mi = new FlatMenuItem( "O&perations", null, 0, show_members );
			mi.Click += hdl;
			dispopt.MenuItems.Add( mi );
			m.MenuItems.Add( dispopt );
		}

		#endregion
	}
}