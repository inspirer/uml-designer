using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using UMLDes.Model;
using UMLDes.Model.CSharp;

namespace UMLDes.Controls {

	public class UmlSolutionTree : TreeView {

		UmlDesignerSolution solution;

		public UmlSolutionTree() {
		}

		#region Tag Class

		private class NodeTag {
			internal object n;
			internal bool initialized;

			internal NodeTag( object n ) {
				this.n = n;
			}
		}

		#endregion

		#region Name/Icon for UmlObject

		string getName( object obj ) {
			if( obj is UmlObject )
				switch( ((UmlObject)obj).Kind ) {
					case UmlKind.Project:
						return ((UmlProject)obj).uid != null ? ((UmlProject)obj).uid : ((UmlProject)obj).name;
					default:
						return ((UmlObject)obj).Name;
				}
			else if( obj is UMLDes.GUI.View ) 
				return ((UMLDes.GUI.View)obj).name;

			return null;
		}

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

		#endregion

		#region Adding/Merging nodes

		void add_nodes( TreeNodeCollection nodes, ArrayList l, int param ) {
			foreach( object o in l ) {
				int icon = param >= 0 ? param : IconForElement( (UmlObject)o );
				TreeNode t = new TreeNode( getName(o), icon, icon );
				t.Tag = new NodeTag( o );
				if( param == 1 ) {
					int i = nodes.IndexOf( refs );
					nodes.Insert( i >= 0 ? i : nodes.Count, t );
				} else 
					nodes.Add( t );
			}
		}

		void merge_nodes( TreeNodeCollection nodes, ArrayList l, int param ) {
			for( int i = 0; i < nodes.Count; i++ ) {
				TreeNode tn = nodes[i];
				NodeTag obj = tn.Tag as NodeTag;
				if( obj == null )
					continue;
				int ind = l.IndexOf( obj.n );
				if( ind == -1 ) {
                    nodes.RemoveAt( i );
					i--;
				} else {
                    l.RemoveAt( ind );
				}
			}
			add_nodes( nodes, l, param );
		}

		#endregion

		#region Creating children

		void get_children( UmlObject obj, ArrayList to ) {

			if( obj is UmlProject )
				obj = ((UmlProject)obj).root;

			UmlTypeHolder from = obj as UmlTypeHolder;

			if( from == null )
				return;

			UmlNamespace ns = from as UmlNamespace;
			if( ns != null && ns.SubNamespaces != null )
				foreach( UmlObject s in ns.SubNamespaces )
					to.Add( s );

			foreach( UmlObject s in ((UmlTypeHolder)from).Types )
				to.Add( s );
				
			if( from is UmlClass ) {
				UmlClass cl = (UmlClass)from;
				if( cl.Members != null )
					foreach( UmlObject m in cl.Members )
						to.Add( m );
			}
		}

		void init_children( TreeNode t ) {
			if( t.Tag != null && t.Tag is NodeTag ) {
				NodeTag nt = (NodeTag)t.Tag;
				UmlObject obj = nt.n as UmlObject;
				if( !nt.initialized && obj != null ) {
					ArrayList l = new ArrayList();
					get_children( obj, l );
                    add_nodes( t.Nodes, l, -1 );
					nt.initialized = true;
				}			
			}
		}

		void setup_nodes( TreeNodeCollection nodes ) {
			foreach( TreeNode t in nodes )
				init_children( t );
		}

		void postfix_nodes( TreeNodeCollection nodes, bool is_visible ) {
			foreach( TreeNode t in nodes ) {
				if( t.Tag != null && t.Tag is NodeTag ) {
					NodeTag nt = (NodeTag)t.Tag;
					UmlObject obj = nt.n as UmlObject;
					if( obj != null ) {

						if( is_visible ) {

							ArrayList l = new ArrayList();
							get_children( obj, l );

							if( !nt.initialized ) {
								add_nodes( t.Nodes, l, -1 );
								nt.initialized = true;
							} else {
								merge_nodes( t.Nodes, l, -1 );
								postfix_nodes( t.Nodes, t.IsExpanded );
							}
						} else {
							t.Nodes.Clear();
							nt.initialized = false;
						}
					}
				}
			}
		}

		protected override void OnBeforeExpand(TreeViewCancelEventArgs e) {
			setup_nodes( e.Node.Nodes );
			base.OnBeforeExpand (e);
		}

		#endregion

		TreeNode views, refs;

		public void NewSolution( UmlDesignerSolution solution ) {
			this.solution = solution;

			if( refs == null )
				refs = new TreeNode( "References", 35, 35 );
			if( views == null )
				views = new TreeNode( "Diagrams", 2, 2 );

			refs.Nodes.Clear();
			views.Nodes.Clear();
			Nodes.Clear();

			// source projects
			add_nodes( Nodes, solution.model.projects, 1 );
			setup_nodes( Nodes );
			Nodes.Add( refs );
			add_nodes( refs.Nodes, solution.model.dllprojs, 36 );
			setup_nodes( refs.Nodes );
			Nodes.Add( views );
			add_nodes( views.Nodes, solution.diagrams, 2 );
		}


		public void RefreshDiagrams() {
			merge_nodes( views.Nodes, new ArrayList( solution.diagrams ), 2 );
		}

		public void RefreshModel() {
			merge_nodes( Nodes, new ArrayList(solution.model.projects), 1 );
			postfix_nodes( Nodes, true );
			merge_nodes( refs.Nodes, new ArrayList(solution.model.dllprojs), 36 );
			postfix_nodes( refs.Nodes, true );
		}

		public object GetNodeObject( TreeNode t ) {
			if( t.Tag == null )
				return null;
			if( t.Tag is NodeTag )
				return ((NodeTag)t.Tag).n;
			return t.Tag;
		}
	}

}