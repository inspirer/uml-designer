using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Model;
using UMLDes.Controls;

namespace UMLDes.GUI {

	/// <summary>
	/// Represents a member of the classificator
	/// </summary>
	public class GuiMember {

		public UmlMember st;
		public SizeF size;
		public int y_offset;
		public Rectangle area;
		public FontStyle font {
			get { return (st.IsAbstract ? FontStyle.Italic : 0) | ( st.IsStatic ? FontStyle.Underline : 0); }
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
		public bool Hidden;

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
	public class GuiClass : GuiPolygonItem, IStateObject, IDropMenu {

		public bool show_members = true, show_vars = true;

		[XmlIgnore] public ArrayList sections = new ArrayList();
		[XmlIgnore] public UmlClass st;

		public GuiClass() {
			parent = null;
		}

		#region Paint

		public const int padding = 10, line_space = 2, vpadding = 6;
		[XmlIgnore] SizeF name_size, stereo_size;

		[XmlIgnore] public FontStyle font {
			get { return (st.IsAbstract ? FontStyle.Italic : 0); }
		}

		protected override Point[] GetPoints() {
			return new Point[] { new Point(X,Y), new Point(X,Y+Height), new Point(X+Width,Y+Height), new Point(X+Width,Y) };
		}


		/// <summary>
		/// Calculates width and height of object
		/// </summary>
		/// <param name="g">graphics object for measurements</param>
		public override void RefreshView( Graphics g ) {
			int width = 50, height = 0;

			name_size = g.MeasureString( name, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Bold|this.font) );
			width = Math.Max( (int)name_size.Width + 2*padding, width );
			height = (int)name_size.Height + 2*vpadding;

			if( st.Stereotype != null ) {
				stereo_size = g.MeasureString( st.Stereotype, parent.cview.Font );
				width = Math.Max( (int)stereo_size.Width + 2*padding, width );
				height += (int)stereo_size.Height + line_space;
			} else
				stereo_size = SizeF.Empty;

			foreach( GuiSection sect in sections )
				if( !sect.Hidden ) {
					height += 2*vpadding;

					foreach( GuiMember m in sect.members ) {
						m.size = g.MeasureString( m.st.AsUml, parent.cview.GetFont(FontTypes.DEFAULT,m.font) );
						width = Math.Max( (int)m.size.Width + 2*padding, width );
						height += (int)m.size.Height + line_space;
					}
				}

			Width = width;
			Height = height;
		}

		public override void Paint( Graphics g, int x, int y ) {
			int textdx, curr_y = y + vpadding;

			if( st.Stereotype != null ) {
				textdx = ( Width - (int)stereo_size.Width ) / 2;
				g.DrawString( st.Stereotype, parent.cview.Font, Brushes.Black, x + textdx, curr_y );
				curr_y += (int)stereo_size.Height + line_space;
			}

			textdx = ( Width - (int)name_size.Width ) / 2;
			g.DrawString( name, parent.cview.GetFont(FontTypes.DEFAULT,FontStyle.Bold|this.font), Brushes.Black, x + textdx, curr_y );
			curr_y += (int)name_size.Height;

			foreach( GuiSection sect in sections )
				if( !sect.Hidden ) {
					curr_y += vpadding;
					g.DrawLine( Pens.Black, x, curr_y, x + Width - 1, curr_y );
					curr_y += vpadding;
					foreach( GuiMember m in sect.members ) {
						g.DrawString( m.st.AsUml, parent.cview.GetFont(FontTypes.DEFAULT,m.font), Brushes.Black, x + padding, curr_y );
						m.y_offset = curr_y;
						curr_y += (int)m.size.Height + line_space;
					}
				}
		}

		#endregion

		#region Creation/PostLoad

		public static GuiClass fromUML( UmlClass st ) {
			GuiClass s = new GuiClass();
			s.name = st.FullQualName;
			s.st = st;
			s.sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Attributes ) ) );
			s.sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Operations ) ) );
			s.sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Properties ) ) );
			return s;
		}

		public override void PostLoad() {
			st = (UmlClass)parent.proj.model.GetObject( name );
			sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Attributes ) ) );
			sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Operations ) ) );
			sections.Add( GuiSection.fromUML( new UmlSection( st, UmlMemberKind.Properties ) ) );

			parent.RefreshObject( this );
			setup_edges();

			base.PostLoad();
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			X = t.x;
			Y = t.y;
			// TODO
			setup_edges();
			Invalidate();
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			return t;
		}

		#endregion

		#region Menu

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
			StateChanged();
		}

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			// Diplay Options
			FlatMenuItem dispopt = new FlatMenuItem( "Display &Options...", null, 0, false );
			EventHandler hdl = new EventHandler( DisplayOptions );
			FlatMenuItem mi = new FlatMenuItem( "&Attributes", null, 0, show_vars );
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