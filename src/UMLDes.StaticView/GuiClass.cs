using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Model;
using UMLDes.Controls;

namespace UMLDes.GUI {

	/// <summary>
	/// UML representation of class (classificator)
	/// </summary>
	public class GuiClass : GuiRectangle, IStateObject, IDropMenu {

		[XmlAttribute] public bool show_members = true, show_vars = true, show_properties = true;
		[XmlAttribute] public bool show_full_qual = false, show_method_signatures = false;

		[XmlIgnore] public UmlClass st;

		public GuiClass() {
			parent = null;
		}

		#region Content

		protected override void fillContent(ArrayList l) {

			string name = show_full_qual || st == null ? UmlModel.LongTypeName2Short(this.name) : st.Name;

			if( st == null || st.Deleted ) {
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABdeleted\xBB" ) );
				l.Add( new GuiString( (st != null && st.IsAbstract ? FontStyle.Italic : 0) | FontStyle.Bold, FontTypes.DEFAULT, true, name ) );
				return;
			} else if( st.Kind == UmlKind.Interface )
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABinterface\xBB" ) );
			else if( st.Kind == UmlKind.Struct )
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABstruct\xBB" ) );

			l.Add( new GuiString( (st != null && st.IsAbstract ? FontStyle.Italic : 0) | FontStyle.Bold, FontTypes.DEFAULT, true, name ) );

			if( show_vars && st.Kind != UmlKind.Interface ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Attributes )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

			if( show_members ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Operations )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

			if( show_properties ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlMember m in st.Members )
						if( m.MemberKind == UmlMemberKind.Properties )
							l.Add( new GuiString( (m.IsAbstract ? FontStyle.Italic : 0) | ( m.IsStatic ? FontStyle.Underline : 0), FontTypes.DEFAULT, false, m.AsUml(show_method_signatures) ) );
			}

		}

		#endregion

		#region Creation/PostLoad

		public static GuiClass fromUML( UmlClass st ) {
			GuiClass s = new GuiClass();
			s.name = st.FullQualName;
			s.st = st;
			s.Created();
			return s;
		}

		public override void PostLoad() {
			st = parent.proj.model.GetObject( name ) as UmlClass;
			base.PostLoad();
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public bool b1, b2, b3, b4, b5;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			Invalidate();
			X = t.x;
			Y = t.y;
			show_members = t.b1;
			show_vars = t.b2;
			show_properties = t.b3;
			show_full_qual = t.b4;
			show_method_signatures = t.b5;
			RefreshContent();
			Invalidate();
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
				default:
					return;
			}
			RefreshContent();
			parent.Undo.Push( new StateOperation( this, before, GetState() ), false );
		}

		private int x_coord_counter;
		private bool ancest;

		public void ImportClass( UmlClass cl ) {
            GuiClass gc = GuiElementFactory.CreateClass( parent, x_coord_counter, ancest ? Y - 100 : Y + Height + 100, cl );
			x_coord_counter += 20 + gc.Width;
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

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {

			FlatMenuItem curr;
			EventHandler evh;

			// Display Options
			evh = new EventHandler( DisplayOptions );
			curr = new FlatMenuItem( "Display &Options...", null, 0, false );
			AddItem( curr, "&Attributes", ToolBarIcons.show_attrs, show_vars, evh );
			AddItem( curr, "&Operations", ToolBarIcons.show_opers, show_members, evh );
			AddItem( curr, "&Properties", ToolBarIcons.show_properties, show_properties, evh );
			AddItem( curr, "Show full &qualified name", ToolBarIcons.show_qual, show_full_qual, evh );
			AddItem( curr, "Show operations &signature", ToolBarIcons.oper_signature, show_method_signatures, evh );
			m.MenuItems.Add( curr );

			evh = new EventHandler( Import );
			curr = new FlatMenuItem( "Import", parent.proj.icon_list, (int)ToolBarIcons.add_related, false );
			AddItem( curr, "Import ancestor && interfaces", ToolBarIcons.None, false, evh );
			AddItem( curr, "Import successors", ToolBarIcons.None, false, evh );
			m.MenuItems.Add( curr );


		}

		#endregion
	}
}