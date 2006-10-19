using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

using UMLDes.Model;

namespace UMLDes.GUI {

	class GuiElementFactory {

		public static GuiItem CreateElement( UmlObject obj ) {

			if( obj is UmlClass )
				return GuiClass.fromUML( obj as UmlClass );
			else if( obj is UmlEnum )
				return GuiEnum.fromUML( obj as UmlEnum );
			else if( obj is UmlNamespace )
				return GuiPackage.fromUML( obj as UmlNamespace );
			else
				return null;
		}

		public static GuiClass CreateClass( StaticView parent, int x, int y, UmlClass cl ) {
			GuiClass c = GuiClass.fromUML(cl);
			c.X = x;
			c.Y = y;
			parent.AddObject( c, UmlModel.GetUniversal(cl) );
			return c;
		}

		public static GuiMemo CreateMemo( StaticView parent, int x, int y ) {
			GuiMemo m = new GuiMemo();
			m.X = x;
			m.Y = y;
			parent.AddObject( m, "memo" );
			return m;
		}

		public static GuiPackage CreatePackage( StaticView parent, int x, int y ) {
			GuiPackage m = new GuiPackage();
			m.name = "Package1";
			m.source_dependant = false;
			m.X = x;
			m.Y = y;
			m.Created();
			parent.AddObject( m, "package" );
			return m;
		}
	}


}