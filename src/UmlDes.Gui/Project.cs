using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using CDS.CSharp;


namespace CDS {

	public interface IPostload {
		void PostLoad();
	}

	public class Project : IPostload {

		[XmlIgnore] public MainWnd container;
		string projectfile;
		[XmlIgnore] public bool modified { get { return false; } }

        [XmlAttribute] public string name = "unknown";

		[XmlArrayItem ("file", typeof(string)) ]
		public ArrayList cs_files = new ArrayList();

		[XmlArray("static_structure"), XmlArrayItem( "class", typeof(UML.UmlClass) ) ]
		public ArrayList _project_items = new ArrayList();

		[XmlIgnore] public Hashtable items = new Hashtable();

		[XmlArrayItem(typeof(GUI.StaticView))]
		public ArrayList diagrams = new ArrayList();

		[XmlIgnore]
		public TreeNode files, classes, views;


		public Project() {
			projectfile = "";
			classes = new TreeNode(name + " (class tree)", 1,1 );
			views = new TreeNode(name + " (views)",2,2 );
			files = new TreeNode(name + " (files)",3,3 );

			classes.Tag = views.Tag = files.Tag = this;
		}

		public void Save( bool saveas ) {
			if( projectfile == "" || saveas ) {
#if DEBUG
                projectfile = "c:\\temp\\proj.cds";
#else
				OpenFileDialog f = new OpenFileDialog();
				f.DefaultExt = "cds";
				f.ValidateNames = true;
				if( f.ShowDialog() != DialogResult.OK )
					return;
				projectfile = f.FileName;
#endif			
			}
			_project_items = new ArrayList( items.Values );
			XmlSerializer s = new XmlSerializer( typeof(Project) );
			Stream file = new FileStream( projectfile, FileMode.Create );
			s.Serialize( file, this );
			file.Close();
		}

		public static Project Load( MainWnd m ) {
			string fname;
#if DEBUG
			fname = "C:\\temp\\proj.cds";
#else
			OpenFileDialog f = new OpenFileDialog();
			f.CheckFileExists = true;
			f.Filter = "Project files (*.cds)|*.cds|All files (*.*)|*.*";
			if( f.ShowDialog() != DialogResult.OK )
				return null;
			fname = f.FileName;
#endif

			Project p;
			XmlSerializer s = new XmlSerializer( typeof(Project) );
			Stream file = new FileStream( fname, FileMode.Open );
			p = s.Deserialize( file ) as Project;
			file.Close();
			p.projectfile = /*f.FileName*/@"C:\temp\proj.cds";

			// post load steps
			foreach( UML.UmlItem i in p._project_items )
				p.items[i.fullname] = i;

			p.container = m;
			p.PostLoad();
			return p;
		}

		public static Project createNew() {
			Project p = new Project(); 
			p.diagrams.Add( new GUI.StaticView() );
			p.PostLoad();
			return p;
		}

		public GUI.View newStaticView() {
			GUI.StaticView d = new GUI.StaticView();
			diagrams.Add( d );
			TreeNode t = new TreeNode( d.name, 2, 2 );
			t.Tag = d;
			views.Nodes.Add( t );

			d.proj = this;
			return d;
		}

		/// <summary>
		/// Fixes GUI trees and references
		/// </summary>
		public void PostLoad( ) {

			foreach( string q in cs_files )
				files.Nodes.Add( new TreeNode( new FileInfo(q).Name, 4, 4 ) );

			foreach( GUI.View d in diagrams )	{
				TreeNode t = new TreeNode( d.name, 2, 2 );
				t.Tag = d;
				views.Nodes.Add( t );

				d.proj = this;
				if( container != null )
					container.SelectView( d, false );	// TODO bad code
				d.PostLoad();
			}
		}

		/// <summary>
		/// Shows AddFile to project dialog
		/// </summary>
		public void AddFile() {
			OpenFileDialog f = new OpenFileDialog();
			f.Multiselect = true;
			f.Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*";
			if( f.ShowDialog() != DialogResult.OK )
				return;
			foreach( string name in f.FileNames ) {
				cs_files.Add( name );
				files.Nodes.Add( new TreeNode( new FileInfo(name).Name, 4, 4 ) );
			}
		}

		/// <summary>
		/// Parses all project files, rebuilds project tree
		/// </summary>
		public void Refresh() {
			bool parsed = true;

			// TODO refresh
			
			/*CodeStructureReader csr = new CodeStructureReader();
			foreach( string name in cs_files ) {
				parsed = csr.parse(name );
				if( !parsed )
					break;
			}
			
			if( parsed ) {
				csr.ResolveTypes();
				Merge( csr.rootns );
				classes.Expand();
				foreach( TreeNode n in classes.Nodes )
					if( n.Tag != null && n.Tag is CS_Namespace )
						n.Expand();
			}*/
			GC.Collect();
		}
		
		/// <summary>
		/// determines the visibility of the element (looks at modifiers)
		/// </summary>
		/// <param name="e">C# element</param>
		/// <returns>0 - public, 1 - private, 2 - protected</returns>
		int access_of_modifier( CS_Element e ) {
			if( e.modifiers == null || e.modifiers.IndexOf( "private" ) != -1 )
				return 1;
			if( e.modifiers.IndexOf( "public" ) != -1 || e.parent.type == elType.cs_interface )
				return 0;
			if( e.modifiers.IndexOf( "protected" ) != -1 )
				return 2;
			return 1;
		}
		
		/// <summary>
		/// returns the number of icon for the given element
		/// </summary>
		/// <param name="e">C# element</param>
		int IconForElement( CS_Element e ) {
			switch( e.type ) {
				case elType.cs_namespace:
					return 5;
				case elType.cs_class:
					return 6 + access_of_modifier(e);
				case elType.cs_interface:
					return 9 + access_of_modifier(e);
				case elType.cs_struct:
					return 12 + access_of_modifier(e);
				case elType.cs_method:
					return 15 + access_of_modifier(e);
				case elType.cs_delegate:
					return 18 + access_of_modifier(e);
				case elType.cs_enum:
					return 21 + access_of_modifier(e);
				case elType.cs_var:
					return 25 + access_of_modifier(e);
				case elType.cs_event:
					return 28 + access_of_modifier(e);
				case elType.cs_indexer:
					return 31 + access_of_modifier(e);
				case elType.cs_operator:
					return 34;
			}
			
			// unknown element, strange
			return 24; 
		}

		/// <summary>
		/// recursive routine to transform elements to Tree Nodes
		/// </summary>
		/// <param name="dir">destination node</param>
		/// <param name="from">source element tree</param>
		void CopyNodes( TreeNode dir, CS_Element from ) {

			TreeNode t;

			if( from is CS_Namespace || from is CS_Class ) {
				CS_Namespace ns = (CS_Namespace)from;
				foreach( CS_Element s in ns.subtypes.Values ) {
					int icon = IconForElement( s );
					t = new TreeNode( s.ToString(), icon, icon );
					CopyNodes( t, s );
					t.Tag = s;
					dir.Nodes.Add( t );
				}
				
				if( from is CS_Class ) {
					CS_Class cl = (CS_Class)from;
					foreach( CS_Element m in cl.members ) {
						int icon = IconForElement( m );
						t = new TreeNode( m.ToString(), icon, icon );
						dir.Nodes.Add( t );
					}
				}
			} 
			
		}

		/// <summary>
		/// Loads parsed elements tree into the treeview
		/// </summary>
		/// <param name="root">C# elements tree</param>
		void Merge( CS_Namespace root ) {
			classes.Nodes.Clear();
			CopyNodes( classes, root );
		}
	}
}
