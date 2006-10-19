using System;
using System.Drawing;
using System.Collections;
using System.Xml.Serialization;
using System.Windows.Forms;
using UMLDes.Model;

namespace UMLDes.GUI {

	public enum ToolBarIcons { 
		New, 
		Open, 
		Save, 
		Saveas, 
		cut, 
		copy, 
		paste, 
		arrow, 
		quadric_conn, 
		comment, 
		straight_conn, 
		curved_conn, 
		segmented_conn, 
		new_diagram, 
		help, 
		undo, 
		redo, 
		refresh, 
		print, 
		delete, 
		add_file, 
		conn_inher, 
		conn_assoc, 
		conn_aggregation, 
		print_preview,
		package,
		Class,
		memo,
		component,
		actor,
		show_qual,
		oper_signature,
		add_related,
		show_attrs,
		show_opers,
		show_properties,
		constraint,
		conn_dependence,
		conn_composition,
		conn_realiz,
		conn_attachm,
		None
	}

	public interface ISolution {
		UmlModel model { get; }
		ImageList icon_list { get; }
		UMLDes.Controls.FlatToolBar tool_bar { get; }
		ImageList project_icon_list { get; }

		void UpdateToolBar();
	}

	public abstract class View {

		public enum EditOperation {
            Cut, Copy, Paste, Delete, SelectAll, SelectNone,
		};

		[XmlAttribute] public string name;
		[XmlIgnore] public ViewCtrl cview;
		[XmlIgnore] public MouseAgent mouseagent;
		[XmlIgnore] public ISolution proj;
		public abstract void Paint( Graphics g, Rectangle r, int offx, int offy );
		public abstract void PostLoad();

		public abstract void DoOperation( EditOperation op );
		public abstract bool IfEnabled( EditOperation op );
		public abstract bool IfContainsSmth( Rectangle r );
		public abstract Rectangle GetContentRectangle();
		public abstract void RefreshContent();
		public abstract ArrayList LoadToolbars();

		[XmlIgnore] public abstract Undo undo { get; }
	}

	public interface IPostload {
		void PostLoad();
	}

	#region Icon Utility Class

	public class IconUtility {

		/// <summary>
		/// determines the visibility of the element (looks at modifiers)
		/// </summary>
		/// <param name="e">C# element</param>
		/// <returns>0 - public, 1 - private, 2 - protected</returns>
		private static int access_of_modifier( UmlObject e ) {
			if( e is UmlMember ) 
				switch( ((UmlMember)e).visibility ) {
					case UmlVisibility.Public:
						return 0;
					case UmlVisibility.Internal:
					case UmlVisibility.Private:
						return 1;
					case UmlVisibility.Protected:
					case UmlVisibility.ProtectedInternal:
						return 2;
				}
			else if( e is UmlClass )
				return 0; // TODO
			return 1;
		}
		
		/// <summary>
		/// returns the number of icon for the given element
		/// </summary>
		/// <param name="e">C# element</param>
		public static int IconForElement( UmlObject e ) {
			switch( e.Kind ) {
				case UmlKind.Project:
					return 1;
				case UmlKind.Namespace:
					return 5;
				case UmlKind.Class:
					return 6 + access_of_modifier(e);
				case UmlKind.Interface:
					return 9 + access_of_modifier(e);
				case UmlKind.Struct:
					return 12 + access_of_modifier(e);
				case UmlKind.Method: case UmlKind.Constructor: case UmlKind.Destructor:
					return 15 + access_of_modifier(e);
				case UmlKind.Delegate:
					return 18 + access_of_modifier(e);
				case UmlKind.Enum:
					return 21 + access_of_modifier(e);
				case UmlKind.Field: case UmlKind.Constant:
					return 25 + access_of_modifier(e);
				case UmlKind.Event:
					return 28 + access_of_modifier(e);
				case UmlKind.Indexer:
					return 31 + access_of_modifier(e);
				case UmlKind.Operator:
					return 34;
			}
			
			// unknown element, strange
			return 24; 
		}
	}

	#endregion
}
