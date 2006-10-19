using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Model;
using UMLDes.Controls;

namespace UMLDes.GUI {

	public class GuiMember : ICloneable {
		[XmlAttribute] public string signature, stereo, constraint;
		[XmlAttribute] public bool visible;

		public bool isDefault() {
			return visible == true && stereo == null && constraint == null;
		}
		
		#region ICloneable Members

		public object Clone() {
			GuiMember m = new GuiMember();
			m.signature = signature;
			m.stereo = stereo;
			m.constraint = constraint;
			m.visible = visible;
			return m;
		}

		#endregion
	}

	/// <summary>
	/// UML representation of class (classificator)
	/// </summary>
	public class GuiClass : GuiRectangle, IStateObject, IDropMenu, IHasStereotype {

		[XmlAttribute] public bool show_members = true, show_vars = true, show_properties = true, show_only_public = false;
		[XmlAttribute] public bool show_full_qual = false, show_method_signatures = false;
		[XmlElement("member_params",typeof(GuiMember))] public ArrayList members;

		[XmlIgnore] public UmlClass st;
		[XmlAttribute] public string stereo;

		public GuiClass() {
			parent = null;
		}

		#region Content

		private Hashtable hash = new Hashtable();

		private void fillHash() {
            hash.Clear();
			if( members != null ) 
				foreach( GuiMember m in members )
					hash[m.signature] = m;
		}

		private bool isMemberVisible( string signature ) {
			return hash.ContainsKey( signature ) ? ((GuiMember)hash[signature]).visible : true;
		}

		protected override void fillContent(ArrayList l) {

			string name = show_full_qual || st == null ? UmlModel.LongTypeName2Short(this.name) : st.Name;

			if( st == null || st.Deleted ) {
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABdeleted\xBB" ) );
				l.Add( new GuiString( (st != null && st.IsAbstract ? FontStyle.Italic : 0) | FontStyle.Bold, FontTypes.DEFAULT, true, name ) );
				return;
			} 
			
			string stereotype = null;
			if( st.Kind == UmlKind.Interface )
				stereotype = "interface";
			else if( st.Kind == UmlKind.Struct )
				stereotype = "structure";

			if( stereotype == null )
				stereotype = stereo;
			else if( stereo != null )
				stereotype += ", " + stereo;

			if( stereotype != null )
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00AB"+stereotype+"\xBB" ) );

			l.Add( new GuiString( (st != null && st.IsAbstract ? FontStyle.Italic : 0) | FontStyle.Bold, FontTypes.DEFAULT, true, name ) );

			if( show_vars && st.Kind != UmlKind.Interface ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Attributes && (!show_only_public || m.visibility == UmlVisibility.Public ) && isMemberVisible(m.signature) )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

			if( show_members ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Operations && (!show_only_public || m.visibility == UmlVisibility.Public ) && isMemberVisible(m.signature) )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

			if( show_properties ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Properties && (!show_only_public || m.visibility == UmlVisibility.Public ) && isMemberVisible(m.signature) )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

		}

		#endregion

		#region Creation/PostLoad

		public static GuiClass fromUML( UmlClass st ) {
			GuiClass s = new GuiClass();
			s.name = st.UniqueName;
			s.st = st;
			s.Created();
			return s;
		}

		public override void PostLoad() {
			st = parent.proj.model.GetObject( name ) as UmlClass;
			fillHash();
			base.PostLoad();
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public bool b1, b2, b3, b4, b5, b6, hidden;
			public string stereo;
			public ArrayList members;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			X = t.x;
			Y = t.y;
			show_members = t.b1;
			show_vars = t.b2;
			show_properties = t.b3;
			show_full_qual = t.b4;
			show_method_signatures = t.b5;
			show_only_public = t.b6;
			stereo = t.stereo;

			if( t.members != null ) {
				members = new ArrayList( t.members.Count );
				foreach( GuiMember m in t.members )
					members.Add( m.Clone() );
			} else
				members = null;
			fillHash();

			RefreshContent();
			SetHidden( t.hidden ); 
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			t.b1 = show_members;
			t.b2 = show_vars;
			t.b3 = show_properties;
			t.b4 = show_full_qual;
			t.b5 = show_method_signatures;
			t.b6 = show_only_public;
			t.stereo = stereo;
			t.hidden = hidden;

			if( members != null ) {
				t.members = new ArrayList( members.Count );
				foreach( GuiMember m in members )
					t.members.Add( m.Clone() );
			} else
				t.members = null;
			return t;
		}

		#endregion

		#region Menu

		public void DisplayOptions( object o, EventArgs ev ) { 
			ObjectState before = GetState();
			switch( (o as FlatMenuItem).Index ) {
				case 0: // Attributes
					show_vars = !show_vars;
					break;
				case 1: // Operations
					show_members = !show_members;
					break;
				case 2: // Properties
					show_properties = !show_properties;
					break;
				case 3: // full title
					show_full_qual = !show_full_qual;
					break;
				case 4:	// method signatures
					show_method_signatures = !show_method_signatures;
					break;
				case 5: // only public
					show_only_public = !show_only_public;
					break;
				default:
					return;
			}
			RefreshContent();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		private int x_coord_counter;
		private bool ancest;

		public void ImportClass( UmlClass cl ) {
			if( parent.FindClass( cl ) == null ) {
				GuiClass gc = GuiElementFactory.CreateClass( parent, x_coord_counter, ancest ? Y - 100 : Y + Height + 100, cl );
				x_coord_counter += 20 + gc.Width;
			}
		}

		public void VisitClassAndImport( UmlObject v, UmlObject parent ) {
			if( v.Kind == UmlKind.Class || v.Kind == UmlKind.Interface ) {
				if( ((UmlClass)v).BaseObjects != null )
					foreach( string s in ((UmlClass)v).BaseObjects )
						if( s.Equals( name ) )
							ImportClass( (UmlClass)v );
			}
		}

		public void Import( object o, EventArgs ev ) {
			if( st == null )
				return;
			switch( (o as FlatMenuItem).Index ) {
				case 0: // ancestor & interfaces
					x_coord_counter = X;
					ancest = true;
					if( st.BaseObjects != null )
						foreach( string s in st.BaseObjects ) {
							UmlClass imp_cl = parent.proj.model.GetObject( s ) as UmlClass;
							if( imp_cl != null )
								ImportClass( imp_cl );
						}
					break;
				case 1: // successors
					ancest = false;
					x_coord_counter = X;
					parent.proj.model.Visit( new UmlObject.Visitor( VisitClassAndImport ), null );
					break;
				default:
					return;
			}
		}

		#region Show/Hide Members

		public class WrappedMember : IVisible {

			GuiClass cl;
			UmlMember memb;

			public WrappedMember( GuiClass cl, UmlMember memb ) {
				this.cl = cl;
				this.memb = memb;
			}

			#region IVisible Members

			public bool Visible {
				get {
					return cl.hash.ContainsKey( memb.signature ) ? ((GuiMember)cl.hash[memb.signature]).visible : true ;
				}
				set {
					if( cl.members == null )
						cl.members = new ArrayList();
					if( cl.hash.ContainsKey( memb.signature ) ) {
						GuiMember mb = (GuiMember)cl.hash[memb.signature];
						mb.visible = value;
						if( mb.isDefault() ) {
							cl.members.Remove( mb );
							cl.hash.Remove( mb );
						}
					} else if( value == false ) {
						GuiMember m = new GuiMember();
						m.signature = memb.signature;
						m.visible = false;
						cl.members.Add( m );
						cl.hash[m.signature] = m;
					}
				}
			}

			public string Name {
				get {
					return memb.AsUml( true );
				}
			}

			public int ImageIndex { 
				get {
					return IconUtility.IconForElement( memb );
				}
			}

			#endregion
		}


		public void showhide( object o, EventArgs ev ) {
			ArrayList l = new ArrayList();
			if( st != null && st.Members != null )
				foreach( UmlMember mm in st.Members )
					l.Add( new WrappedMember( this, mm ) );

			ObjectState before = GetState();
			bool done = ShowHideDialog.Process( parent.cview.FindForm(), l, parent.proj.project_icon_list );

			if( done ) {
				RefreshContent();
				parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
			}
		}

		#endregion

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {

			FlatMenuItem curr;
			EventHandler evh;

			// Display Options
			evh = new EventHandler( DisplayOptions );
			curr = new FlatMenuItem( "Display &Options...", null, 0, false );
			parent.AddItem( curr, "&Attributes", ToolBarIcons.show_attrs, show_vars, evh );
			parent.AddItem( curr, "&Operations", ToolBarIcons.show_opers, show_members, evh );
			parent.AddItem( curr, "&Properties", ToolBarIcons.show_properties, show_properties, evh );
			parent.AddItem( curr, "Show full &qualified name", ToolBarIcons.show_qual, show_full_qual, evh );
			parent.AddItem( curr, "Show operations &signature", ToolBarIcons.oper_signature, show_method_signatures, evh );
			parent.AddItem( curr, "Only public", ToolBarIcons.None, show_only_public, evh );
			m.MenuItems.Add( curr );

			evh = new EventHandler( Import );
			curr = new FlatMenuItem( "Import", parent.proj.icon_list, (int)ToolBarIcons.add_related, false );
			parent.AddItem( curr, "Import ancestor && interfaces", ToolBarIcons.None, false, evh );
			parent.AddItem( curr, "Import successors", ToolBarIcons.None, false, evh );
			m.MenuItems.Add( curr );

			m.MenuItems.Add( new StereoTypeHelper( this ).GetStereoMenu() );

			parent.AddItem( m, "Show/Hide Members", ToolBarIcons.None, false, new EventHandler( showhide ) );
		}

		#endregion

		#region IHasStereotype Members

		static string[] stereo_list = new string[] {
			"actor",
			"exception",
			"signal",
			"process",
			"thread",
			"type",
			null,
			"metaclass",
			"powertype",
			"stereotype",
			"utility",
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
				return new Rectangle( place.X+inflate+1, place.Y+inflate+1, place.Width, 0 );
			}
		}

		#endregion
	}
}