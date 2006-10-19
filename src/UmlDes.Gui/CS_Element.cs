using System;
using System.Xml.Serialization;
using System.Collections;
//using int = CDS.Reader.CSharpParser.lapg_place;

namespace CDS.CSharp
{
	public enum elType {
		cs_namespace, cs_class, cs_struct, cs_interface, cs_method,
		cs_operator, cs_property, cs_var, cs_delegate, cs_event, cs_indexer,
		cs_enum
	}

	/// <summary>
	/// base class for all C# elements
	/// </summary>
	public abstract class CS_Element {

		public struct Place {
			public int start, end;
			public string file;
		}

		protected string uml_modifier {
			get {
				if( modifiers == null || modifiers.IndexOf( "private" ) != -1 )
					return "!";
				if( modifiers.IndexOf( "public" ) != -1 || parent.type == elType.cs_interface )
					return "+";
				if( modifiers.IndexOf( "protected" ) != -1 )
					return "-";
				return "!";
			}
		}

		public virtual string qualifiedName {
			get {
				return null;
			}
		}

		public elType type;
		public string modifiers;
		public string name, comment;
		[XmlIgnore]
		public Place place;
		[XmlIgnore]
		public CS_Element parent;

		// common variables
		public CS_Element Typename;
		public string typename;
		public ArrayList vars;

		public CS_Element( string name, elType type, int start ) {
			this.name = name;
			this.type = type;
			place = new Place();
			place.start = start;
		}

		public CS_Element( elType type ) {
			this.type = type;
		}

		public CS_Namespace getUpperNS() {
			CS_Element u = this;

			while( u != null ) {
				if( u is CS_Namespace )
					return (CS_Namespace)u;
				u = u.parent;
			}

			throw new ArgumentException( "Element has no containing namespace" );
		}

		public abstract string umlname { get; }

		public virtual UML.UmlItem ToUML() {
			return null;
		}
	}


	/// <summary>
	/// Namespace
	/// </summary>
	public class CS_Namespace : CS_Element {

		public Hashtable subtypes;
		public ArrayList used_ns;
		public Hashtable alias_ns;

		public override string qualifiedName {
			get {
				if( name != null )
					if( parent.name != null )
						return ((CS_Namespace)parent).qualifiedName + "." + name;
					else
						return name;
				else
					return "<rootns>";
			}
		}

		public override string ToString() {
			return name;
		}

		public CS_Namespace( string name, CS_Namespace outer, int start ) : base( name, elType.cs_namespace, start ) {
			subtypes = new Hashtable();
			used_ns = new ArrayList();
			alias_ns = new Hashtable();
			this.parent = outer;
		}

		/// <summary>
		/// Special constructor for root namespace (without name and parent)
		/// </summary>
		public CS_Namespace() : base( elType.cs_namespace ) {
			subtypes = new Hashtable();
			used_ns = new ArrayList();
			alias_ns = new Hashtable();
			this.parent = null;
		}

		public override string umlname { get { return null; } }
	}


	/// <summary>
	/// Class, struct, interface (inherits Namespace functionality)
	/// </summary>
	public class CS_Class : CS_Namespace {

		public ArrayList bases;
		public string baseclasses;
		public ArrayList members;

		public CS_Class( string name, CS_Namespace outer, int start ) : base( name, outer, start ) {
			type = elType.cs_class;
			members = new ArrayList();
		}
		
		public override UML.UmlItem ToUML() {
			UML.UmlClass ci = new UML.UmlClass();

			ci.fullname = qualifiedName;
			ci._abstract = modifiers.IndexOf( "abstract" ) != -1;
			ci._interface = this.type == elType.cs_interface;

			if( ci._interface )
				ci.stereotype = "interface";

			foreach( CS_Element m in members ) {
				if( m is CS_Method )
					ci.opers.members.Add( m.ToUML() );
				else if( m is CS_Var )
					ci.attrs.members.Add( m.ToUML() );
			}

			if( bases != null )
				foreach( CS_Element c in bases ) {
					string s = c.qualifiedName;
					if( s != null ) {
						if( ci.bases == null )
							ci.bases = new ArrayList();
						ci.bases.Add( c.qualifiedName );
					}
				}
			return ci;
		}
	}

	/// <summary>
	/// Method
	/// </summary>
	public class CS_Method : CS_Element {

		public CS_Method( string name, string type, string modif, ArrayList param, string comment, int st, int end )  : base( name, elType.cs_method, st ) {
			typename = type;
			modifiers = modif;
			this.comment = comment;
			place.end = end;
			vars = param;		
			
			if( param != null )
				foreach( CS_Var v in param )
					v.parent = this;
		}
		
		public override string ToString() {
			string s = ( modifiers != null ? modifiers + " " : "" ) + typename + " " + name + "(";
			if( vars != null )
				foreach( CS_Var v in vars ) {
					if( s.EndsWith( "(" ) )
						s += v.ToString();
					else
						s += ", " + v.ToString();				
				}
			return s + ")";
		}
		
		public override string umlname { 
			get { 
				string s = uml_modifier + " " + name + "(";
				if( vars != null )
					foreach( CS_Var v in vars ) {
						if( s.EndsWith( "(" ) )
							s += v.umlname;
						else
							s += ", " + v.umlname;				
					}
				if( typename == "void" )
					return s + ")";
				else
                    return s + ") : " + typename;
			} 
		}

		public override UML.UmlItem ToUML() {
			UML.UmlMember m = new UML.UmlMember();
			m.fullname = this.umlname;
			m._static = modifiers != null && modifiers.IndexOf( "static" ) != -1;
			m._abstract = modifiers != null && modifiers.IndexOf( "abstract" ) != -1;

			return m;
		}
	}

	/// <summary>
	/// Variable
	/// </summary>
	public class CS_Var : CS_Element {

		public CS_Var( string name, string vartype, string modif, int start, int end ) : base( name, elType.cs_var, start ) {
			typename = vartype;
			modifiers = modif;
			place.end = end;
		}
		
		public override string ToString() {
			return (modifiers != null ? modifiers + " " + typename : typename ) + " " + name;
		}
		
		public override string umlname { 
			get {
				return (modifiers != null ? modifiers + " " + name : name ) + " : " + typename;
			}
		}

		public override UML.UmlItem ToUML() {
			UML.UmlMember m = new UML.UmlMember();
			m.fullname = this.umlname;
			m._static = modifiers != null && modifiers.IndexOf( "static" ) != -1;
			m._abstract = modifiers != null && modifiers.IndexOf( "abstract" ) != -1;
			return m;
		}
	}

	/// <summary>
	/// Operator
	/// </summary>
	public class CS_Operator : CS_Element {
		
		public enum OpType {
			ot_convexplicit,
			ot_convimplicit,
			ot_unary,
			ot_binary,
		};
		
		public OpType op_type;
		public CS_Var param1, param2;

		public CS_Operator( string name, OpType ot, string rtype, CS_Var p1, CS_Var p2, int start ) : base( name, elType.cs_operator, start ) {
			op_type = ot;
			typename = rtype;
			param1 = p1;
			if( param1 != null )
				param1.parent = this;
			param2 = p2;
			if( param2 != null )
				param2.parent = this;
		}
		
		public override string ToString() {
			switch( op_type ) {
				case OpType.ot_binary:
					return ( modifiers != null ? modifiers + " " : "" ) + typename + " operator " + name + "(" + param1.ToString() + ", " + param2.ToString() + ")";
				case OpType.ot_unary:
					return ( modifiers != null ? modifiers + " " : "" ) + typename + " operator " + name + "(" + param1.ToString() + ")";
				case OpType.ot_convexplicit:
					return ( modifiers != null ? modifiers + " " : "" ) + "explicit operator " + typename + "(" + param1.ToString() + ")";
				case OpType.ot_convimplicit:
					return ( modifiers != null ? modifiers + " " : "" ) + "implicit operator " + typename + "(" + param1.ToString() + ")";
			}
			return "";
		}

		public override string umlname { get { return null; } }
	}

	/// <summary>
	/// Delegate
	/// </summary>
	public class CS_Delegate : CS_Element {

		public CS_Delegate( string name, string dtype, string modif, string comment, ArrayList param, int start, int end ) : base( name, elType.cs_delegate, start ) {
			typename = dtype;
			modifiers = modif;
			this.comment = comment;
			place.end = end;
			vars = param;

			if( param != null )
				foreach( CS_Var v in param )
					v.parent = this;
		}
		
		public override string ToString() {
			string s = ( modifiers != null ? modifiers + " " : "" ) + "delegate " + typename + " " + name + "(";
			if( vars != null )
				foreach( CS_Var v in vars ) {
					if( s.EndsWith( "(" ) )
						s += v.ToString();
					else
						s += ", " + v.ToString();				
				}
			return s + ")";
		}

		public override string umlname { get { return null; } }
	}

	/// <summary>
	/// Property
	/// </summary>
	public class CS_Property : CS_Element {

		public string accessors;

		public CS_Property( string name, string type, string modif, string access, string comment, int st, int end ) : base( name, elType.cs_property, st ) {
			typename = type;
			modifiers = modif;
			this.comment = comment;
			place.end = end;
			accessors = access;
		}
		
		public override string ToString() {
			return ( modifiers != null ? modifiers + " " : "" ) + typename + " " + name + " { " + accessors + " }";
		}

		public override string umlname { get { return null; } }
	}


	/// <summary>
	/// Event
	/// </summary>
	public class CS_Event : CS_Element {

		public string accessors;

		public CS_Event( string name, string type, string modif, string comment, string access, int st, int end ) : base( name, elType.cs_event, st ) {
			typename = type;
			modifiers = modif;
			this.comment = comment;
			accessors = access;
			place.end = end;
		}
		
		public override string ToString() {
			return ( modifiers != null ? modifiers + " " : "" ) + "event " + typename + " " + name + ( accessors != null ? " { " + accessors + " }" : "" );
		}

		public override string umlname { get { return null; } }
	}

	/// <summary>
	/// Indexer
	/// </summary>
	public class CS_Indexer : CS_Element {

		public string accessors;

		public CS_Indexer( string id, string type, string access, string modif, ArrayList param, string comment, int st, int end ) : base( id, elType.cs_indexer, st ) {
			typename = type;
			accessors = access;
			modifiers = modif;
			this.comment = comment;
			place.end = end;
			vars = param;

			foreach( CS_Var v in param )
				v.parent = this;
		}
		
		public override string ToString() {
			string s = ( modifiers != null ? modifiers + " " : "" ) + typename + " " + name + "[";
			foreach( CS_Var v in vars ) {
				if( s.EndsWith( "[" ) )
					s += v.ToString();
				else
					s += ", " + v.ToString();				
			}
	
			return s + "] { " + accessors + " }";
		}

		public override string umlname { get { return null; } }
	}

	/// <summary>
	/// Enumeration
	/// </summary>
	public class CS_Enum : CS_Element {

		public string basetype;

		public CS_Enum( string id, string baset, string modif, string comment, int st, int end ) : base( id, elType.cs_enum, st ) {
			basetype = baset;
			modifiers = modif;
			this.comment = comment;
			place.end = end;
		}
		
		public override string ToString() {
			return ( modifiers != null ? modifiers + " enum " : "enum " ) + name + (basetype != null ? " : " + basetype : "" );
		}

		public override string umlname { get { return null; } }
	}

}
