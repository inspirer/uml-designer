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
		[XmlAttribute] public string name;
		[XmlAttribute] public UmlVisibility visibility;

		public abstract UmlMemberKind MemberKind { get; }

		public override string Name { get { return name; } }

		// public abstract AsUml { get; }
	}

	public class UmlField : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Field; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Attributes; } }

		[XmlAttribute] public string Type;
	}

	public class UmlConstant : UmlField {
		public override UmlKind Kind { get { return UmlKind.Constant; } }
	}

	public class UmlMethod : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Method; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }

		[XmlAttribute] public string ReturnType;
	}

	public class UmlProperty : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Property; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Properties; } }
	}

	public class UmlEvent : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Event; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Attributes; } }
	}

	public class UmlIndexer : UmlMember {
		public override UmlKind Kind { get { return UmlKind.Indexer; } }
		public override UmlMemberKind MemberKind { get { return UmlMemberKind.Operations; } }
	}

	public class UmlOperator : UmlMember {
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
