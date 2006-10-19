using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using CDS.CSharp;
using System.Xml.Serialization;

namespace CDS.UML {

	/// <summary>
	/// Root element for all things, which don't depend on representation
	/// </summary>
	public class UmlItem {
		[XmlAttribute] public string fullname;
	}

	/// <summary>
	/// attribute, operation, etc.
	/// </summary>
	public class UmlMember : UmlItem {
		[XmlAttribute] public bool _static, _abstract;
	}

	/// <summary>
	/// section of operations/attributes/etc.
	/// </summary>
	public class UmlSection : UmlItem {
		[XmlElement("member",typeof(UmlMember))]
		public ArrayList members = new ArrayList();

	}

	/// <summary>
	/// Base information about class, which does not depend on representation
	/// </summary>
	public class UmlClass : UmlItem {
		
		public string stereotype;
		public UmlSection attrs = new UmlSection(), opers = new UmlSection();
		public ArrayList sections = new ArrayList();
		public ArrayList bases; // of string (FullNames)
		[XmlElement]
		public bool _abstract, _sealed, _interface;

		public UmlClass() {
		}

	}

	/// <summary>
	/// Static information about relations
	/// </summary>
	public class UmlRelation : UmlItem {


	}
}