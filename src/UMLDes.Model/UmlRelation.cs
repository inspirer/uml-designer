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
			if( stereo != null )
				ID += "#[stereo]" + stereo;
		}

		#endregion
	}

	public class RelationsHelper {

		public static IEnumerable GetRelations( UmlClass cl, UmlModel m ) {

			ArrayList l = new ArrayList();

			// get inheritances
			if( cl.BaseObjects != null )
				foreach( string s in cl.BaseObjects ) {
					UmlClass obj = m.GetObject( s ) as UmlClass;
					if( obj != null )
						l.Add( new UmlRelation( obj, cl, obj.kind == UmlKind.Interface ? UmlRelationType.Realization : UmlRelationType.Inheritance, null, null, null, null ) );
				}

			// get associations
			Hashtable h = new Hashtable();
			if( cl.Members != null )
				foreach( UmlMember mb in cl.Members ) {
					string name = mb.name, type = null;
					switch( mb.Kind ) {
						case UmlKind.Property:
							type = ((UmlProperty)mb).Type;
							break;
						case UmlKind.Field:
							type = ((UmlField)mb).Type;
							break;
					}

					if( type != null ) {
						if( type.IndexOf( '[' ) >= 0 ) {
							int i = type.IndexOf( '[' );
							name += type.Substring(i);
							type = type.Substring(0, i);
						}

						UmlClass obj = m.GetObject( type ) as UmlClass;

						if( obj != null && obj != cl ) {
							if( h.ContainsKey( obj ) )
								h[obj] = (string)h[obj] + "," + name;
							else
								h[obj] = name;
						}
					}
				}

			foreach( UmlClass obj in h.Keys ) {
				l.Add( new UmlRelation( obj, cl, UmlRelationType.Association, (string)h[obj], null, null, null ) );

			}

			return l;
		}

	}

}