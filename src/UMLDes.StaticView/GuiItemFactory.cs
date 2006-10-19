using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

using UMLDes.Model;

namespace UMLDes.GUI {

	class GuiElementFactory {

		public static GuiItem CreateElement( UmlObject obj ) {
			return GuiClass.fromUML( obj as UmlClass );
		}

		private static GuiClass CreateClass( UmlClass cl ) {
			return null;
		}

		public static GuiMemo CreateMemo( StaticView parent, int x, int y ) {
			GuiMemo m = new GuiMemo();
			m.parent = parent;
			m.id = parent.RegisterItemID( "memo", m );
			m.X = x;
			m.Y = y;
			m.PostLoad();
			m.Invalidate();
			parent.Undo.Push( new CreateOperation( (IRemoveable)m ), false );
			return m;
		}

	}


}