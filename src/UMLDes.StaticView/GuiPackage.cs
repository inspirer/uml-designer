using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Model;
using UMLDes.Controls;

namespace UMLDes.GUI {

	/// <summary>
	/// UML representation of enumeration
	/// </summary>
	public class GuiPackage : GuiRectangle, IStateObject, IDropMenu, IDynamicContent, IHasStereotype {

		[XmlAttribute] public bool show_members = true, show_full_qual = false, source_dependant = true;
		[XmlAttribute] public string stereo;
		[XmlIgnore] public UmlNamespace st;

		public GuiPackage() {
			parent = null;
		}

		#region Paint

		protected override Point[] GetPoints() {
			return new Point[] { new Point(X,Y), new Point(X,Y+Height+title_height), new Point(X+Math.Max(Width,title_width+10),Y+Height+title_height), new Point(X+Math.Max(Width,title_width+10),Y+title_height), new Point(X+title_width,Y+title_height), new Point(X+title_width,Y) };
		}

		int title_height, title_width, stereo_height, sw, nw;
		string currName, currStereo;

		public override void RefreshView(Graphics g) {
			Font f = parent.cview.GetFont( FontTypes.DEFAULT, FontStyle.Bold );
			SizeF size = g.MeasureString( currName, f );
			title_height = (int)size.Height+6;
			title_width = nw = (int)size.Width+2*padding;
			if( currStereo != null ) {
				f = parent.cview.GetFont( FontTypes.DEFAULT, 0 );
				size = g.MeasureString( currStereo, f );
				title_width = Math.Max( sw = (int)size.Width+2*padding, title_width );
				title_height += stereo_height = (int)size.Height+1+line_space;
			}
			base.RefreshView( g );
		}

		public override void Paint(Graphics g, int x, int y) {
			Font f;
			int cy = y + 3;
			if( currStereo != null ) {
				f = parent.cview.GetFont( FontTypes.DEFAULT, 0 );
				g.DrawString( currStereo, f, Brushes.Black, x + padding + ((title_width-sw)/2), cy );
                cy += stereo_height;
			}			
			f = parent.cview.GetFont( FontTypes.DEFAULT, FontStyle.Bold );
			g.DrawString( currName, f, Brushes.Black, x + padding + ((title_width-nw)/2), cy );
			g.DrawLine( Pens.Black, x, y+title_height, x+title_width, y+title_height );
			base.Paint( g, x, y+title_height );
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public bool b1, b2, hidden;
			public string name, stereo;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			X = t.x;
			Y = t.y;
			show_members = t.b1;
			show_full_qual = t.b2;
			name = t.name;
			stereo = t.stereo;
			RefreshContent();
			SetHidden( t.hidden ); 
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			t.b1 = show_members;
			t.b2 = show_full_qual;
			t.name = name;
			t.stereo = stereo;
			t.hidden = hidden;
			return t;
		}

		#endregion

		#region Menu

		public void DisplayOptions( object o, EventArgs ev ) { 
			ObjectState before = GetState();
			switch( (o as FlatMenuItem).Index ) {
				case 0: // Show full qualified header
					show_full_qual = !show_full_qual;
					break;
				case 1: // Show members
					show_members = !show_members;
					break;
				default:
					return;
			}
			RefreshContent();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		#region Rename

		public void EditedName( string name ) { 
			ObjectState before = GetState();
			this.name = name;
			RefreshContent();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		public void Rename_click( object o, EventArgs ev ) { 
			Rectangle r = new Rectangle( place.X+inflate+1, place.Y+inflate+1, title_width, 0 );
			InPlaceTextEdit.Start( "Change name", name, parent.cview.point_to_screen(r.X, r.Y), Math.Max( r.Width, 70 ), r.Height, parent.cview, new StringEditedEvent( EditedName ), false );
		}

		#endregion

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {

			if( !source_dependant )
				parent.AddItem( m, "Rename package", ToolBarIcons.None, false, new EventHandler(Rename_click) );

			FlatMenuItem curr;
			EventHandler evh;

			// Display Options
			evh = new EventHandler( DisplayOptions );
			curr = new FlatMenuItem( "Display &Options...", null, 0, false );
			parent.AddItem( curr, "Show full &qualified name", ToolBarIcons.show_qual, show_full_qual, evh );
			parent.AddItem( curr, "&Show members", ToolBarIcons.None, show_members, evh );
			m.MenuItems.Add( curr );

			m.MenuItems.Add( new StereoTypeHelper( this ).GetStereoMenu() );
		}

		#endregion

		#region Content/Creation/PostLoad

		protected override void fillContent(ArrayList l) {

			currName = show_full_qual || st == null ? UmlModel.LongTypeName2Short(this.name) : st.Name;
			currStereo = stereo != null ? "\xAB" + stereo + "\xBB" : null;

			if( source_dependant && (st == null || st.Deleted) ) {
				currStereo = "\x00ABdeleted\xBB";
				return;
			}

			if( show_members ) {

				if( st != null && st.Types != null )
					foreach( UmlType t in st.Types )
						l.Add( new GuiString( 0, FontTypes.DEFAULT, false, t.Name ) );
			}
		}

		public override void PostLoad() {
			st = source_dependant ? parent.proj.model.GetObject( name ) as UmlNamespace : null;
			base.PostLoad ();
		}

		public static GuiPackage fromUML( UmlNamespace st ) {
			GuiPackage s = new GuiPackage();
			s.name = st.UniqueName;
			s.st = st;
			s.Created();
			return s;
		}

		#endregion

		#region IHasStereotype Members

		static string[] stereo_list = new string[] {
			"facade",
			"framework",
			"stub",
			"subsystem",
			"system"
		};

		string[] IHasStereotype.StereoList {
			get {
				return stereo_list;
			}
		}

		string IHasStereotype.Stereo {
			get {
				return stereo;
			}
			set {
				if( stereo != value ) {
					ObjectState before = GetState();
					stereo = value;
					RefreshContent();
					parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
				}
			}
		}

		Rectangle IHasStereotype.EditRect { 
			get {
				return new Rectangle( place.X+inflate+1, place.Y+inflate+1, title_width, 0 );
			}
		}

		#endregion
	}
}