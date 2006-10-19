using System;
using System.IO;
using System.Collections;
using System.Reflection;

namespace UMLDes.Model.MSIL {

	public class ModelBuilder {

		public static UmlProject CreateProject( string dllname ) {
			if( !File.Exists( dllname ) )
				return null;
			System.Reflection.Assembly assem = System.Reflection.Assembly.LoadFile( dllname );
			if( assem == null )
				return null;
			UmlProject proj = new UmlProject();
			proj.filename = dllname;
			proj.name = assem.FullName;
			proj.guid = "{" + System.Guid.NewGuid().ToString().ToLower() + "}";
			proj.root = new UmlNamespace();
			proj.root.name = String.Empty;
			return proj;
		}

		public static UmlProject CreateProjectFromName( string name ) {
			System.Reflection.Assembly assem = System.Reflection.Assembly.LoadWithPartialName( name );
			if( assem == null )
				return null;
			UmlProject proj = new UmlProject();
			proj.filename = assem.GetModules()[0].FullyQualifiedName;
			proj.name = assem.FullName;
			proj.guid = "{" + System.Guid.NewGuid().ToString().ToLower() + "}";
			proj.root = new UmlNamespace();
			proj.root.name = String.Empty;
			return proj;
		}

		public static void ClearDeleted( UmlObject v, UmlObject parent) {
			v.Deleted = false;
		}

		public static bool UpdateProject( UmlProject proj ) {
			string dllname = proj.filename;

			if( !File.Exists( dllname ) )
				return false;
			if( proj.write_time.Equals( File.GetLastWriteTime( dllname ) ) ) {
				proj.Visit( new UmlObject.Visitor( ClearDeleted ), null );
				return true;
			}
			System.Reflection.Assembly assem = System.Reflection.Assembly.LoadFile( dllname );
			if( assem == null )
				return false;

			proj.name = assem.FullName;
			proj.write_time = File.GetLastWriteTime( dllname );
			proj.refs = new ArrayList();
			foreach( AssemblyName refasm in assem.GetReferencedAssemblies() )
				proj.refs.Add( refasm.FullName );

			ModelBuilder mb = new ModelBuilder();
			mb.assem = assem;
			mb.root = proj.root;
			proj.classes = mb.classes = new Hashtable();
			proj.name_to_class = mb.name_to_class = new Hashtable();
			return mb.Update();
		}

		internal Assembly assem;
		internal UmlNamespace root;
		internal Hashtable classes;
		internal Hashtable name_to_class;


		#region Assembly Updater

		private bool Update() {

			Hashtable h = new Hashtable();
			
			foreach( Type t in assem.GetExportedTypes() ) {
				if( t.DeclaringType != null )
					continue;
				UmlNamespace ns = root;
				string typename = t.FullName;
				int lastdot = typename.LastIndexOf( '.' );
				if( lastdot >= 0 ) {
					string N = typename.Substring( 0, lastdot );
					typename = typename.Substring( lastdot + 1 );
					if( h.ContainsKey( N ) )
						ns = (UmlNamespace)h[N];
					else
						h[N] = ns = EnsureNamespaceCreated( root, N );
				}
				if( typename.Length == 0 )
					return false;
				if( ns.Types == null )
					ns.Types = new ArrayList();
				UmlType found = UpdateTypeInArray( ns.Types, t, typename );
				if( !UpdateMembers( found, t ) )
					return false;
			}				

			return true;
		}

		private UmlType UpdateTypeInArray( ArrayList Types, Type t, string typename ) {
			// resolve kind
			UmlKind kind = UmlKind.Class;
			if( t.IsInterface )
				kind = UmlKind.Interface;
			else if( t.IsEnum )
				kind = UmlKind.Enum;
			else if( t.IsValueType )
				kind = UmlKind.Struct;

			// search existing or create a new
			UmlType found = null;
			foreach( UmlType e in Types )
				if( e.name.Equals( typename ) ) {
					found = e;
					break;
				}
			recreate:
				if( found == null ) {
					if( kind == UmlKind.Enum ) {
						found = new UmlEnum();
					} else {
						found = new UmlClass();
						((UmlClass)found).kind = kind;
					}
					found.name = typename;
					Types.Add( found );
				} else {
					if( found.Kind == UmlKind.Enum && kind != UmlKind.Enum || found.Kind != UmlKind.Enum && kind == UmlKind.Enum ) {
						Types.Remove( found );
						found = null;
						goto recreate;
					}
					if( found.Kind != UmlKind.Enum ) {
						((UmlClass)found).kind = kind;
					}
					found.Deleted = false;
				}
			return found;
		}

		private bool UpdateMembers( UmlType cl, Type tpcl ) {

			classes[tpcl] = cl;
			name_to_class[tpcl.FullName] = cl;

			if( cl is UmlClass ) {
				// nested
				UmlClass ucl = (UmlClass)cl;
				foreach( Type t in tpcl.GetNestedTypes() ) {
					if( ucl.Types == null )
						ucl.Types = new ArrayList();
					UmlType found = UpdateTypeInArray( ucl.Types, t, t.Name );
					if( !UpdateMembers( found, t ) )
						return false;
				}
			}

			return true;
		}

		private UmlNamespace EnsureNamespaceCreated( UmlNamespace curr, string newname ) {

			foreach( string name in newname.Split( new char[]{ '.' } ) ) {
				curr.Deleted = false;
				if( curr.SubNamespaces != null )
					foreach( UmlNamespace child in curr.SubNamespaces )
						if( child.name.Equals( name ) ) {
							curr = child;
							goto found;
						}
				if( curr.SubNamespaces == null )
					curr.SubNamespaces = new ArrayList();
				UmlNamespace newns = new UmlNamespace();
				curr.SubNamespaces.Add( newns );
				curr = newns;
				curr.name = name;
			found:;
			}
			curr.Deleted = false;
			return curr;
		}

		#endregion

		#region Inheritance resolver

		/// <summary>
		/// Resolve inheritances in DllProject, use referenced projects to search
		/// </summary>
		public static void Inheritances( UmlProject proj, Hashtable dllprojs, ArrayList errors ) {

			foreach( DictionaryEntry ent in proj.classes ) {
				Type t = (Type)ent.Key;
				UmlType tp = (UmlType)ent.Value;

				if( tp is UmlClass )
					if( t.BaseType != null && !t.BaseType.Equals( typeof( object ) ) ) {
						UmlClass cl = (UmlClass)tp;
						Type base_type = t.BaseType;

						cl.BaseList = new ArrayList();
						UmlProject base_proj = (UmlProject)dllprojs[base_type.Assembly.FullName];

						if( base_proj == null ) {
							errors.Add( "unknown assembly: " + base_type.Assembly.FullName );
							continue;
						}

						UmlClass base_class = (UmlClass)base_proj.name_to_class[ base_type.FullName ];

						if( base_class == null ) {
							errors.Add( "unknown class in assembly: " + base_type.FullName );
							continue;
						}

						cl.BaseList.Add( base_class );
					} else
						((UmlClass)tp).BaseList = null;
			}

		}

		#endregion
	}

}
