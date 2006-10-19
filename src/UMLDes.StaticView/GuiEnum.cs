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
	public class GuiEnum : GuiRectangle, IStateObject, IDropMenu {

		[XmlAttribute] public bool show_members = true, show_full_qual = false;

		[XmlIgnore] public UmlEnum st;

		public GuiEnum() {
			parent = null;
		}

		#region Content

		protected override void fillContent(ArrayList l) {

			string name = show_full_qual || st == null ? UmlModel.LongTypeName2Short(this.name) : st.Name;

			if( st == null || st.Deleted ) {
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABdeleted\xBB" ) );
				l.Add( new GuiString( FontStyle.Bold, FontTypes.DEFAULT, true, name ) );
				return;
			} else {
				l.Add( new GuiString( FontStyle.Regular, FontTypes.DEFAULT, true, "\x00ABenumeration\xBB" ) );
			}
			l.Add( new GuiString( FontStyle.Bold, FontTypes.DEFAULT, true, name ) );

			if( show_members ) {
				l.Add( new GuiString() );
				if( st.Members != null )
					foreach( UmlEnumField m in st.Members )
						l.Add( new GuiString( 0, FontTypes.DEFAULT, false, m.Name ) );
			}
		}

		#endregion

		#region Creation/PostLoad

		public static GuiEnum fromUML( UmlEnum st ) {
			GuiEnum s = new GuiEnum();
			s.name = st.UniqueName;
			s.st = st;
			s.Created();
			return s;
		}

		public override void PostLoad() {
			st = parent.proj.model.GetObject( name ) as UmlEnum;
			base.PostLoad();
		}

		#endregion

		#region IStateObject Members

		class State : ObjectState {
			public int x, y;
			public bool b1, b2, hidden;
		}

		public void Apply(ObjectState v) {
			State t = v as State;
			X = t.x;
			Y = t.y;
			show_members = t.b1;
			show_full_qual = t.b2;
			RefreshContent();
			SetHidden( t.hidden ); 
		}

		public ObjectState GetState() {
			State t = new State();
			t.x = X;
			t.y = Y;
			t.b1 = show_members;
			t.b2 = show_full_qual;
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

		public void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y ) {
			FlatMenuItem curr;
			EventHandler evh;

			// Display Options
			evh = new EventHandler( DisplayOptions );
			curr = new FlatMenuItem( "Display &Options...", null, 0, false );
			parent.AddItem( curr, "Show full &qualified name", ToolBarIcons.show_qual, show_full_qual, evh );
			parent.AddItem( curr, "&Show members", ToolBarIcons.None, show_members, evh );
			m.MenuItems.Add( curr );
		}

		#endregion
	}
}