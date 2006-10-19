using System;
using System.Collections;
using System.Xml.Serialization;

namespace UMLDes.Model {

	public enum UmlKind {
		Model, 
		Project, 
		Namespace, 
		Class, 
		Delegate, 
		Enum, 
		Struct, 
		Interface,

		Constant,
		Field,
		Method,
		Property,
		Event,
		Indexer,
		Operator,
		Constructor,
		Destructor,

		Parameter,
		EnumNode
	}

	/// <summary>
	/// Persistent object, which can be saved (with its children) in XML format
	/// </summary>
	public abstract class UmlObject {

		public delegate void Visitor( UmlObject elem, UmlObject parent );

		public virtual void Visit( Visitor v, UmlObject parent ) {}

		public abstract UmlKind Kind { get; }

		public abstract string Name { get; }

		public string FullName { get { return UmlModel.GetFullQualified(this); } }
		public string FullQualName { get { return UmlModel.GetUniversal(this); } }

		[XmlIgnore] public bool Deleted;

		[XmlIgnore] public UmlObject Parent;
	}

	/// <summary>
	/// Model is smth like solution in VS.NET, it consists of Projects
	/// </summary>
	public class UmlModel : UmlObject {

		[XmlElement("UmlProject",typeof(UmlProject))]   
		public ArrayList projects;	// Source projects 

		[XmlElement("DllProject",typeof(UmlProject))]
		public ArrayList dllprojs;	// binary .dlls

		#region UmlObject

		public override UmlKind Kind { 
			get { 
				return UmlKind.Model; 
			} 
		}

		public override string Name { 
			get { 
				return String.Empty; 
			} 
		}

		public override void Visit( Visitor v, UmlObject parent ) {
			foreach( UmlProject proj in projects )
				proj.Visit( v, this );
			foreach( UmlProject proj in dllprojs )
				proj.Visit( v, this );
		}

		#endregion

		#region Common

		public void AssignUID( UmlProject p ) {
			string buid = p.name;
			if( buid.IndexOf( ',' ) != -1 )
				buid = buid.Substring( 0, buid.IndexOf( ',' ) );
			string uid = buid;
			int counter = 1;
			while( true ) {
				UmlProject f = null;
				foreach( UmlProject p2 in projects )
					if( p2.uid != null && p2.uid.Equals( uid ) )
						f = p2;
				foreach( UmlProject p3 in dllprojs )
					if( p3.uid != null && p3.uid.Equals( uid ) )
						f = p3;
				if( f == null ) {
					p.uid = uid;
					break;
				}
				counter++;
				uid = buid + counter.ToString();
			}

		}

		public static string GetFullQualified( UmlObject obj ) {
			string name = obj.Name;
			if( name == null || name.Length == 0 )
				return String.Empty;
			while( obj.Parent != null ) {
				obj = obj.Parent;
				if( obj.Name != null && obj.Name.Length > 0 )
					name = obj.Name + "." + name;
				else
					break;
			}
			return name;
		}

		public static string GetUniversal( UmlObject obj ) {
			string name = obj.Name;
			if( name == null || name.Length == 0 )
				return String.Empty;
			while( obj.Parent != null ) {
				obj = obj.Parent;
				if( obj.Name != null && obj.Name.Length > 0 )
					name = obj.Name + "." + name;
				else {
					while( obj.Parent != null && !(obj is UmlProject) )
						obj = obj.Parent;
					if( obj != null )
						name = ((UmlProject)obj).uid + "/" + name;
					
					break;
				}
			}
			return name;
		}

		public UmlObject GetObject( string name ) {
			int index = name.IndexOf( '/' );
			if( index == -1 )
				return null;
			string proj_name = name.Substring( 0, index );
			UmlProject project = null;
			foreach( UmlProject p in projects )
				if( p.uid.Equals( proj_name ) ) {
					project = p;
					break;
				}
			if( project == null )
				foreach( UmlProject p in dllprojs )
					if( p.uid.Equals( proj_name ) ) {
						project = p;
						break;
					}

			if( project != null ) {
				UmlObject o = project.root;
				string[] genid = name.Substring(index+1).Split( new char[] { '.' } );
				foreach( string s in genid ) {
					Hashtable hash = null;
					if( o is UmlNamespace )
						hash = ((UmlNamespace)o).Children;
					else if( o is UmlClass )
						hash = ((UmlClass)o).Children;
					if( hash == null || !hash.ContainsKey( s ) )
						return null;
					o = (UmlObject)hash[s];
				}
				return o;
			}

			return null;
		}

		public static string LongTypeName2Short( string tn ) {
			if( tn == null || tn.IndexOf( '/' ) == -1 )
				return tn;
			return tn.Substring( tn.IndexOf( '/' )+1 );
		}

		public static string GetShortName( string l ) {
			l = LongTypeName2Short( l );
			return ( l.LastIndexOf( '.' ) != -1 ) ? l.Substring( l.LastIndexOf( '.' ) + 1 ) : l;
		}

		#endregion
	}

	/// <summary>
	/// One .csproj or .dll
	/// </summary>
	public class UmlProject : UmlObject {
		public string filename;
		[XmlAttribute] public string name, guid, uid;
		[XmlElement("Root")] public UmlNamespace root;

		// ignored, valid only after update
		[XmlIgnore] public ArrayList files;		// string
		[XmlIgnore] public ArrayList refs;		// string, then UmlProjects
		[XmlIgnore] public Hashtable classes, name_to_class; // DllProject features

		internal DateTime write_time;

		#region UmlObject

		public override UmlKind Kind {
			get {
				return UmlKind.Project;
			}
		}

		public override string Name {
			get {
				return filename;
			}
		}

		public override void Visit(UMLDes.Model.UmlObject.Visitor v, UmlObject parent) {
			if( root != null )
				root.Visit( v, this );
			v( this, parent );
		}

		#endregion
	}

	/// <summary>
	/// Update time construction
	/// </summary>
	public class UsingBlock {
		public UsingBlock parent;
		public UmlNamespace related;
		public ArrayList list;
		public Hashtable aliases;
	}

	/// <summary>
	/// Class or Namespace
	/// </summary>
	public interface UmlTypeHolder {
		ArrayList Types { get; }
	}

	/// <summary>
	/// Namespace
	/// </summary>
	public class UmlNamespace : UmlObject, UmlTypeHolder {

		[XmlAttribute] 
		public string name;

		[XmlElement("Namespace",typeof(UmlNamespace))]	
		public ArrayList SubNamespaces;

		[XmlElement("Class",typeof(UmlClass)), XmlElement("Enum",typeof(UmlEnum)), XmlElement("Delegate",typeof(UmlDelegate))]
		public ArrayList Types;

		[XmlIgnore] public Hashtable Children;

		ArrayList UmlTypeHolder.Types { get { if( Types == null ) Types = new ArrayList(); return Types; } }

		#region UmlObject

		public override UmlKind Kind { 
			get { 
				return UmlKind.Namespace; 
			} 
		}

		public override string Name { 
			get { 
				return name; 
			} 
		}

		public override void Visit( Visitor v, UmlObject parent ) {
			if( SubNamespaces != null )
				foreach( UmlNamespace o in SubNamespaces )
					o.Visit( v, this );
			if( Types != null )
				foreach( UmlType o in Types )
					o.Visit( v, this );
			v( this, parent );
		}

		#endregion
	}
}