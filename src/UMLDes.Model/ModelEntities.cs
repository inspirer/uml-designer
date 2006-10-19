using System;
using System.Xml.Serialization;
using System.Collections;

namespace UMLDes.Model {

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

		public abstract UmlMemberKind MemberKind { get; }

		public override string Name { get { return name; } }

		[XmlAttribute] public bool IsAbstract, IsStatic;

		#region Uml utils

		public virtual string AsUml { get { return VisibilityString + name; } }

		public string VisibilityString {
			get {
				switch( visibility ) {
					case UmlVisibility.Public:		return "+ ";
					case UmlVisibility.Protected:	return "# ";
					case UmlVisibility.ProtectedInternal: return "@# ";
					case UmlVisibility.Private:		return "- ";
					case UmlVisibility.Internal:	return "@ ";
				}
				return String.Empty;
			}
		}

		#endregion
	}

	public class UmlField : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Field; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Attributes; } }

		[XmlAttribute] public string Type;

		public override string AsUml {
			get {
				return base.AsUml + " : " + UmlModel.GetShortName(Type);
			}
		}
	}

	public class UmlConstant : UmlField {
		public override UmlKind Kind { get { return UmlKind.Constant; } }
	}

	public class UmlParameter : UmlObject {
		public override UmlKind Kind { get { return UmlKind.Parameter; } }
		public override string Name { get { return name; } }

		[XmlAttribute] public string name, Type;
	}

	public class UmlMethod : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Method; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }

		[XmlAttribute] public string ReturnType;
		[XmlElement("Parameter", typeof( UmlParameter ) )] public ArrayList Params;

		public override string AsUml {
			get {
				return base.AsUml + "(" + GetParametersAsUml(Params) + ") : " + UmlModel.GetShortName(ReturnType);
			}
		}

		internal static string GetParametersAsUml( ArrayList l ) {
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
	}

	public class UmlProperty : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Property; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Properties; } }

		[XmlAttribute] public string Type, Accessors;

		public override string AsUml {
			get {
				return base.AsUml + " { " + Accessors + " } : " + UmlModel.GetShortName(Type);
			}
		}
	}

	public class UmlEvent : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Event; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Attributes; } }

		[XmlAttribute] public string Type;

		public override string AsUml {
			get {
				return "event " + base.AsUml + " : " + UmlModel.GetShortName(Type);
			}
		}
	}

	public class UmlIndexer : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Indexer; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }

		[XmlAttribute] public string Type;

		public override string AsUml {
			get {
				return base.AsUml + "[] : " + UmlModel.GetShortName(Type);
			}
		}
	}

	public class UmlOperator : UmlMethod {
		public override UmlKind Kind { get { return UmlKind.Operator; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }
	}

	public class UmlConstructor : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Constructor; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }
	}

	public class UmlDestructor : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Destructor; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }
	}
}
