using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using UMLDes.Model;
using UMLDes.Model.CSharp;

namespace UMLDes {

	public class UmlDesignerSolution : GUI.IPostload, GUI.ISolution {

        [XmlAttribute]	
		public string projectfile, name = "unknown";
		public UmlModel model;
		[XmlArrayItem(typeof(GUI.StaticView))]   
		public ArrayList diagrams = new ArrayList();

		internal ArrayList projects = new ArrayList();
		internal MainWnd container;
		internal bool modified { get { return false; } }

		public UmlDesignerSolution() {
			projectfile = String.Empty;
			model = ModelBuilder.CreateEmptyModel();
		}

		public void Save( bool saveas ) {
			if( projectfile.Length == 0 || saveas ) {
#if DEBUG
                projectfile = "c:\\temp\\proj.umldes";
#else
				SaveFileDialog f = new SaveFileDialog();
				f.AddExtension = true;
				f.DefaultExt = "umldes";
				if( f.ShowDialog() != DialogResult.OK )
					return;
				projectfile = f.FileName;
#endif			
				name = Path.GetFileNameWithoutExtension( projectfile );
				container.RefreshTitle();
			}
			XmlSerializer s = new XmlSerializer( typeof(UmlDesignerSolution) );
			Stream file = new FileStream( projectfile, FileMode.Create );
			s.Serialize( file, this );
			file.Close();
		}

		public static UmlDesignerSolution Load( MainWnd m ) {
			string fname;
#if DEBUG
			fname = "C:\\temp\\proj.umldes";
#else
			OpenFileDialog f = new OpenFileDialog();
			f.CheckFileExists = true;
			f.Filter = "Project files (*.umldes)|*.umldes|All files (*.*)|*.*";
			if( f.ShowDialog() != DialogResult.OK )
				return null;
			fname = f.FileName;
#endif

			UmlDesignerSolution p;
			XmlSerializer s = new XmlSerializer( typeof(UmlDesignerSolution) );
			Stream file = new FileStream( fname, FileMode.Open );
			p = s.Deserialize( file ) as UmlDesignerSolution;
			file.Close();
			if( p != null ) {
				// post load steps
				p.projectfile = fname;
				p.container = m;
				p.PostLoad();
			}
			return p;
		}

		public static UmlDesignerSolution createNew() {
			UmlDesignerSolution p = new UmlDesignerSolution(); 
			p.diagrams.Add( new GUI.StaticView() );
			p.PostLoad();
			return p;
		}

		private void SelectNameFor( GUI.View v ) {
			string name = v.name, tname;
			if( name.IndexOf( '1' ) != -1 )
				name = name.Substring( 0, name.IndexOf( '1' ) );
			int i = 1;
			do {
				tname = name + i.ToString();
				foreach( GUI.View d in diagrams )
					if( d.name.Equals( tname ) ) {
						tname = null;
						break;
					}
				i++;

			} while(tname == null);
			v.name = tname;
		}

		public GUI.View newStaticView() {
			GUI.StaticView d = new GUI.StaticView();
			SelectNameFor( d );
			diagrams.Add( d );
			d.proj = this;
			container.RefreshProjectTree(false);
			return d;
		}

		/// <summary>
		/// Fixes GUI trees and references
		/// </summary>
		public void PostLoad( ) {

			if( model != null )
				ModelBuilder.PostLoad( model );
			else
				model = ModelBuilder.CreateEmptyModel();

			foreach( GUI.View d in diagrams )	{
				d.proj = this;
				if( container != null )
					container.SelectView( d, false );	// TODO bad code
				d.PostLoad();
			}

			RebuildProjectTree();
		}

		/// <summary>
		/// Shows AddFile to project dialog
		/// </summary>
		public void AddFile() {
			OpenFileDialog f = new OpenFileDialog();
			f.Multiselect = true;
			f.Filter = "C# project files (*.csproj)|*.csproj|All files (*.*)|*.*";
			if( f.ShowDialog() != DialogResult.OK )
				return;
			foreach( string name in f.FileNames ) {
				ModelBuilder.AddProject( model, name );
			}
			RebuildProjectTree();
		}

		/// <summary>
		/// Parses all project files, rebuilds project tree
		/// </summary>
		public void Refresh() {
			ArrayList errors;
			ModelBuilder.UpdateModel( model, out errors );
			if( errors != null && errors.Count > 0 ) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach( string s in errors )
					sb.Append( s + "\n" );
				MessageBox.Show( sb.ToString(), "Errors", MessageBoxButtons.OK, MessageBoxIcon.Error );
			} else {
				RebuildProjectTree();
			}
			GC.Collect();
		}
		
		#region Icon for UmlObject

		/// <summary>
		/// determines the visibility of the element (looks at modifiers)
		/// </summary>
		/// <param name="e">C# element</param>
		/// <returns>0 - public, 1 - private, 2 - protected</returns>
		int access_of_modifier( UmlObject e ) {
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
		int IconForElement( UmlObject e ) {
			switch( e.Kind ) {
				case UmlKind.Namespace:
					return 5;
				case UmlKind.Class:
					return 6 + access_of_modifier(e);
				case UmlKind.Interface:
					return 9 + access_of_modifier(e);
				case UmlKind.Struct:
					return 12 + access_of_modifier(e);
				case UmlKind.Method:
					return 15 + access_of_modifier(e);
				case UmlKind.Delegate:
					return 18 + access_of_modifier(e);
				case UmlKind.Enum:
					return 21 + access_of_modifier(e);
				case UmlKind.Field:
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

		#endregion

		#region ReBuild projects Tree for the current model

		/// <summary>
		/// recursive routine to transform elements to Tree Nodes
		/// </summary>
		/// <param name="dir">destination node</param>
		/// <param name="from">source element tree</param>
		void CopyNodes( TreeNode dir, UmlObject from ) {

			TreeNode t;

			if( from is UmlTypeHolder ) {

				UmlNamespace ns = from as UmlNamespace;
				if( ns != null && ns.SubNamespaces != null )
					foreach( UmlObject s in ns.SubNamespaces ) {
						int icon = IconForElement( s );
						t = new TreeNode( s.Name, icon, icon );
						CopyNodes( t, s );
						t.Tag = s;
						dir.Nodes.Add( t );
					}

				foreach( UmlObject s in ((UmlTypeHolder)from).Types ) {
					int icon = IconForElement( s );
					t = new TreeNode( s.Name, icon, icon );
					CopyNodes( t, s );
					t.Tag = s;
					dir.Nodes.Add( t );
				}
				
				if( from is UmlClass ) {
					UmlClass cl = (UmlClass)from;
					if( cl.Members != null )
						foreach( UmlObject m in cl.Members ) {
							int icon = IconForElement( m );
							t = new TreeNode( m.Name, icon, icon );
							dir.Nodes.Add( t );
						}
				}
			} 
			
		}

		/// <summary>
		/// Loads parsed elements tree into the treeview
		/// </summary>
		/// <param name="root">C# elements tree</param>
		void RebuildProjectTree() {
			TreeNode t;
			projects.Clear();

			// source projects
			foreach( UmlProject p in model.projects ) {
				t = new TreeNode( p.uid != null ? p.uid : p.name, 1, 1 );
				CopyNodes( t, p.root );
				t.Tag = p;
				projects.Add( t );
			}

			// references
			if( model.dllprojs.Count > 0 ) {
                TreeNode refs = new TreeNode( "References", 35, 35 );
				projects.Add( refs );
				foreach( UmlProject p in model.dllprojs ) {
					t = new TreeNode( p.uid != null ? p.uid : p.name, 36, 36 );
					CopyNodes( t, p.root );
					t.Tag = p;
					refs.Nodes.Add( t );
				}
			}

			// refresh tree
			if( container != null )
				container.RefreshProjectTree(true);
		}

		#endregion

		#region ISolution support

		UmlModel GUI.ISolution.model { 
			get {
				return model;
			}
		}

		ImageList GUI.ISolution.icon_list { 
			get {
				return container.list;
			}
		}

		void GUI.ISolution.UpdateToolBar() {
			container.UpdateToolBar();
		}

		void GUI.ISolution.SetDefaultDrawingMode() {
			container.SetDefaultDrawingMode();
		}

		#endregion
	}
}
