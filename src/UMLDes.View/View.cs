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
		public abstract void RefreshContent();
		public abstract ArrayList LoadToolbars();

		[XmlIgnore] public abstract Undo undo { get; }
	}

	public interface IPostload {
		void PostLoad();
	}
}
