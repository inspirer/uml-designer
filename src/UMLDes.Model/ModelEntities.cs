using System;
using System.Xml.Serialization;
using System.Collections;

namespace UMLDes.Model {

	public class UmlEnumField : UmlObject {
		[XmlAttribute] public string name;

		#region Methods
		public override UmlKind Kind { get { return UmlKind.EnumNode; } }
		public override string Name { get { return name; } }
		#endregion
	}

	public enum UmlMemberKind {
		Attributes, Operations, Properties
	}

	public enum UmlVisibility {
		Public, Private, Internal, Protected, ProtectedInternal
	}

	public abstract class UmlMember : UmlObject {
		[XmlIgnore] public string file;
		[XmlIgnore] public int start_offset, end_offset;
		[XmlAttribute] public string name, signature;
		[XmlAttribute] public UmlVisibility visibility;
		[XmlAttribute] public bool IsAbstract, IsStatic;

		#region Methods

		public abstract UmlMemberKind MemberKind { get; }

		public override string Name { get { return name; } }

		public override void Visit(UMLDes.Model.UmlObject.Visitor v, UmlObject parent) {
			v( this, parent );
		}

		public virtual string AsUml( bool full ) { 
			return VisibilityString + name; 
		}

		public string VisibilityString {
			get {
				switch( visibility ) {
					case UmlVisibility.Public:		return "+ ";
					case UmlVisibility.Protected:	return "# ";
					case UmlVisibility.ProtectedInternal: return "~# ";
					case UmlVisibility.Private:		return "- ";
					case UmlVisibility.Internal:	return "~ ";
				}
				return String.Empty;
			}
		}

		#endregion
	}

	public class UmlField : UmlMember {
		[XmlAttribute] public string Type;

		#region Methods

		public override UmlKind Kind { get { return UmlKind.Field; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Attributes; } }

		public override string AsUml( bool full ) { 
			return base.AsUml(full) + " : " + UmlModel.GetShortName(Type);
		}

		#endregion
	}

	public class UmlConstant : UmlField {
		public override UmlKind Kind { get { return UmlKind.Constant; } }
	}

	public class UmlParameter : UmlObject {
		[XmlAttribute] public string name, Type;

		#region Methods
		public override UmlKind Kind { get { return UmlKind.Parameter; } }
		public override string Name { get { return name; } }
		#endregion
	}

	public class UmlMethod : UmlMember {
		[XmlAttribute] public string ReturnType;
		[XmlElement("Parameter", typeof( UmlParameter ) )] public ArrayList Params;

		#region Methods

		public override UmlKind Kind { get { return UmlKind.Method; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }

		public override string AsUml( bool full ) { 
			if( full )
				return base.AsUml(full) + "(" + GetParametersAsUml(Params) + ")" + ( ReturnType==null ? "":" : " + UmlModel.GetShortName(ReturnType));
			else
				return base.AsUml(full) + "()";
		}

		internal static string GetParametersAsUml( ArrayList l ) {
			if( l == null )
				return String.Empty;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach( UmlParameter p in l ) {
				if( sb.Length > 0 )
					sb.Append( "; " );
				sb.Append( p.name );
				sb.Append( " :" );
				sb.Append( UmlModel.GetShortName(p.Type) );
			}
			return sb.Length == 0 ? String.Empty : " " + sb.ToString() + " ";
		}

		#endregion
	}

	public class UmlProperty : UmlMember {
		[XmlAttribute] public string Type, Accessors;

		#region Methods

		public override UmlKind Kind { get { return UmlKind.Property; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Properties; } }

		public override string AsUml( bool full ) { 
			if( full )
				return base.AsUml(full) + " { " + Accessors + " } : " + UmlModel.GetShortName(Type);
			else
				return base.AsUml(full) + " { " + Accessors + " }";
		}

		#endregion
	}

	public class UmlEvent : UmlMember {
		[XmlAttribute] public string Type, Accessors;

		#region Methods

		public override UmlKind Kind { get { return UmlKind.Event; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Properties; } }

		public override string AsUml( bool full ) { 
			if( full )
				return VisibilityString + "\x00ABevent\xBB " + name + (Accessors==null?"":" { " + Accessors+" }") + " : " + UmlModel.GetShortName(Type);
			else
				return VisibilityString + "\x00ABevent\xBB " + name;
		}

		#endregion
	}

	public class UmlIndexer : UmlMethod {
		#region Methods

		public override UmlKind Kind { get { return UmlKind.Indexer; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }

		public override string AsUml( bool full ) { 
			if( full )
				return VisibilityString + "\x00ABindexer\xBB " + name + "[" + GetParametersAsUml(Params) + "] : " + UmlModel.GetShortName(this.ReturnType);
			else
				return VisibilityString + "\x00ABindexer\xBB " + name + "[]";
		}

		#endregion
	}

	public class UmlOperator : UmlMethod {
		#region Methods
		public override UmlKind Kind { get { return UmlKind.Operator; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }
		#endregion
	}

	public class UmlConstructor : UmlMethod {
		#region Methods
		public override UmlKind Kind { get { return UmlKind.Constructor; } }

		public override string AsUml( bool full ) { 
			if( full )
				return VisibilityString + "\x00ABctor\xBB " + name + "(" + GetParametersAsUml(Params) + ")";
			else
				return VisibilityString + "\x00ABctor\xBB " + name + "()";
		}

		#endregion
	}

	public class UmlDestructor : UmlMethod {
		#region Methods
		public override UmlKind Kind { get { return UmlKind.Destructor; } }

		public override string AsUml( bool full ) { 
			if( full )
				return VisibilityString + "\x00ABdtor\xBB ~" + name + "(" + GetParametersAsUml(Params) + ")";
			else
				return VisibilityString + "\x00ABdtor\xBB ~" + name + "()";
		}
		#endregion
	}
}
