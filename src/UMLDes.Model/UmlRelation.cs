using System;
using System.Xml.Serialization;
using System.Collections;

namespace UMLDes.Model {

	public enum UmlRelationType {
		Inheritance, Association, Aggregation, Attachment,
		Dependency, Composition, Realization
	}

	public class UmlRelation {
        public UmlClass src, dest;
		public UmlRelationType type;
		public string src_role, dest_role;
		public string name, stereo;
		public string ID;

		#region Constructor

		internal UmlRelation( UmlClass src, UmlClass dest, UmlRelationType type, string src_role, string dest_role, string name, string stereo ) {
			this.src = src;
			this.dest = dest;
			this.type = type;
			this.src_role = src_role;
			this.dest_role = dest_role;
			this.name = name;
			this.stereo = stereo;

			ID = type.ToString() + "#" + src.UniqueName + "#" + dest.UniqueName;
			if( src_role != null )
				ID += "#[src]" + src_role;
			if( dest_role != null )
				ID += "#[dest]" + dest_role;
			if( stereo != null )
				ID += "#[stereo]" + stereo;
		}

		#endregion
	}

	public class RelationsHelper {

		public static IEnumerable GetRelations( UmlClass cl, UmlModel m ) {

			ArrayList l = new ArrayList();
			if( cl.BaseObjects != null )
				foreach( string s in cl.BaseObjects ) {
					UmlClass obj = m.GetObject( s ) as UmlClass;
					if( obj != null )
						l.Add( new UmlRelation( obj, cl, UmlRelationType.Inheritance, null, null, null, null ) );
				}

			return l;
		}

	}

}