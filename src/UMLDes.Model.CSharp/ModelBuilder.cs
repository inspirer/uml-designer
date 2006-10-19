using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using UMLDes.Model;
using UMLDes.CSharp;
using UMLDes.CSharp.Nodes;

namespace UMLDes.Model.CSharp {

	public class ModelBuilder {

		public static UmlModel CreateEmptyModel() {
			UmlModel m = new UmlModel();
			m.projects = new ArrayList();
			m.dllprojs = new ArrayList();
			return m;
		}

		public static void AddProject( UmlModel m, string filename ) {
			UmlProject p = new UmlProject();
			p.filename = filename;
			p.name = Path.GetFileNameWithoutExtension( filename );
			m.projects.Add( p );
		}

		public static void UpdateModel( UmlModel model, out ArrayList errors ) {
			ModelBuilder mb = new ModelBuilder();
			mb.model = model;
			mb.Update();
			errors = mb.errors.Count > 0 ? mb.errors : null;
		}

		public static void PostLoad( UmlModel model ) {
			model.Visit( new UmlObject.Visitor( SetParentAndBuildHash ), null );
		}

		// model engine vars

		UmlModel model;
		ArrayList errors = new ArrayList();
		Hashtable classes = new Hashtable();
		ArrayList added_classes = new ArrayList();
		ArrayList deleted_classes = new ArrayList();

		#region Model Update

		void Update() {

			if( !UpdateStudioProjects( model ) ) {
				errors.Add( "Project files are absent or corrupt" );
				return;
			}

			Hashtable project_files = new Hashtable();

			// parse all files, collect NS
			foreach( UmlProject p in model.projects ) {
				ArrayList parsed_files = new ArrayList();
				foreach( string file in p.files ) {
					string text;
					try {
						text = new StreamReader(file).ReadToEnd();
						ArrayList parse_errors;
						NamespaceDecl node = parser.parse( text, out parse_errors );
						if( parse_errors.Count > 0 ) {
							foreach( string err in parse_errors )
								errors.Add( file + ": " + err );
						} else {
							parsed_files.Add( node );
						}
					} catch {
						errors.Add( "cannot read file " + file + " from " + p.name );
					}
				}
				project_files[p] = parsed_files;
			}

			if( errors.Count > 0 )
				return;

			// mark model as Deleted
			model.Visit( new UmlObject.Visitor( SetDeletedFlag ), null );

			// Stage #1: build namespaces and types for each project
			foreach( UmlProject p in model.projects ) {

				if( p.root == null ) {
					p.root = new UmlNamespace();
					p.name = String.Empty;
				}
				p.Deleted = false;
				ArrayList parsed_files = (ArrayList)project_files[p];

				foreach( NamespaceDecl ns in parsed_files )
					FillNamespace( p.root, ns, null );
			}

			// end of Stage #1: each project contains tree of Types

			// Stage #2: Building project references, Resolving inheritances

			// fix references, initialize resolving system
			BuildReferences();

			if( errors.Count > 0 )
				return;

			// collect deleted, setup parent
			foreach( UmlProject p in model.projects )
				CollectDeleted( p.root );
			model.Visit( new UmlObject.Visitor( SetParentAndBuildHash ), null );

			// resolve inheritaces for classes
			foreach( UmlType ent in classes.Keys )
                switch( ent.Kind ) {
					case UmlKind.Interface:
					case UmlKind.Class:
					case UmlKind.Struct:
						ClassDecl cdecl = (ClassDecl)classes[ent];
						if( cdecl.inheritance != null && cdecl.inheritance.nodes.Count > 0 ) {
							((UmlClass)ent).BaseList = new ArrayList();
							foreach( IdentNode id in cdecl.inheritance.nodes ) {
								UmlType resolved = ResolveType( id.identifier, ent );
								if( resolved == null )
									errors.Add( "cannot resolve inheritance: " + id.identifier + " in " + ent.Name );
								else
									((UmlClass)ent).BaseList.Add( resolved );
							}
						}
						break;
				}

			// for MSIL we build only Base class
			BuildMSILInheritances();

			// Stage #3: Building members
			foreach( UmlType ent in classes.Keys )
				switch( ent.Kind ) {
					case UmlKind.Interface:
					case UmlKind.Class:
					case UmlKind.Struct:
						ClassDecl cdecl = (ClassDecl)classes[ent];
						UpdateClass( (UmlClass)ent, cdecl );
						break;
				}

			if( errors.Count > 0 )
				return;
		}

		private static void SetParentAndBuildHash( UmlObject elem, UmlObject parent ) {
			elem.Parent = parent;
			if( elem is UmlClass ) {
				// hash class Children
                UmlClass cl = (UmlClass)elem;
				cl.Children = new Hashtable();
				if( cl.Types != null )
					foreach( UmlType t in cl.Types )
						cl.Children[t.name] = t;

			} else if( elem is UmlNamespace ) {
				// hash namespace Children
				UmlNamespace ns = (UmlNamespace)elem;
				ns.Children = new Hashtable();
				if( ns.Types != null )
					foreach( UmlType t in ns.Types )
						ns.Children[t.name] = t;
				if( ns.SubNamespaces != null )
					foreach( UmlNamespace n in ns.SubNamespaces )
						ns.Children[n.name] = n;
			}
		}

		#endregion

		#region Stage #0: Loading Project information from .csproj

		private static void CreateProjectFromXml( XmlNode n, UmlProject proj ) {
			string fname = null;
			if( n.Name.Equals( "File" ) ) {
				fname = n.Attributes["RelPath"].Value;
				if( !fname.EndsWith( ".cs" ) )
					fname = null;
				else {
					fname = Path.GetFullPath( fname );
					proj.files.Add( fname );
				}

			} else if( n.Name.Equals( "CSHARP" ) ) {
				proj.guid = n.Attributes[ "ProjectGuid" ].Value;
				if( proj.guid != null && !proj.guid.StartsWith( "{" ) )
					proj.guid = null;
				if( proj.guid != null )
					proj.guid = proj.guid.ToLower();

			} else if( n.Name.Equals( "Reference" ) ) {
				foreach( XmlAttribute attr in n.Attributes )
					if( attr.Name.Equals( "Project" ) ) {
						// project reference
						proj.refs.Add( attr.Value.ToLower() );

					} else if( attr.Name.Equals( "HintPath" ) ) {
						// external reference
						fname = attr.Value.ToLower();
						if( !fname.EndsWith( ".dll" ) )
							fname = null;
						else {
							fname = Path.GetFullPath( fname ).ToLower();
							proj.refs.Add( fname );
						}
					}
			} else if( n.Name.Equals( "Settings" ) ) {
				fname = n.Attributes["AssemblyName"].Value;
				if( fname != null && fname.Length > 0 )
					proj.name = fname;

			} 
			
			if( n.HasChildNodes ) {
				foreach( XmlNode sub in n )
					CreateProjectFromXml( sub, proj );
			}
		}

		private static bool UpdateStudioProjects( UmlModel m ) {
			foreach( UmlProject p in m.projects ) {
				try {
					Directory.SetCurrentDirectory( Path.GetDirectoryName( p.filename ) );
					XmlDocument doc = new XmlDocument();
					doc.Load( p.filename );
					p.name = null;
					p.files = new ArrayList();
					p.refs = new ArrayList();
					p.root = new UmlNamespace();
					CreateProjectFromXml( doc, p );
					if( p.name == null )
						p.name = Path.GetFileNameWithoutExtension(p.filename);
					if( p.guid == null )
						p.guid = "{" + System.Guid.NewGuid().ToString().ToLower() + "}";
					if( p.uid == null )
						m.AssignUID( p );
				} catch {
					return false;
				}
			}
			return true;
		}

		#endregion

		#region Stage #1: Filling namespaces and Types (FillNamespace)

		private static string Combine( string s1, string s2 ) {
			if( s1.Length == 0 ) 
				return s2;
			else
				return s1 + "." + s2;
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

		private UsingBlock create_Usings( NamespaceDecl node, UsingBlock prev, UmlNamespace assoc_with ) {
			if( node.usings != null && node.usings.nodes.Count > 0 ) {
				UsingBlock newblock = new UsingBlock();
				newblock.parent = prev;
				newblock.list = new ArrayList();
				newblock.aliases = new Hashtable();
				newblock.related = assoc_with;
				foreach( UsingNode un in node.usings.nodes ) {
					if( un.alias_id != null )
						newblock.aliases[un.alias_id.identifier] = un.type_name.identifier;
					else
						newblock.list.Add( un.type_name.identifier );
				}
				return newblock;
			}
			return prev;
		}

		private void FillNamespace( UmlNamespace curr, NamespaceDecl nsnode, UsingBlock usings ) {
			if( nsnode != null && nsnode.members != null ) {
				usings = create_Usings( nsnode, usings, curr );
				foreach( Node n in nsnode.members.nodes ) {
					switch( n.kind ) {
						case Kind.Interface:
						case Kind.Struct:
						case Kind.Class:
							LoadClass( curr, (ClassDecl)n, usings );
							break;
						case Kind.Delegate:
							LoadDelegate( curr, (DelegateNode)n, usings );
							break;
						case Kind.Enum:
							LoadEnum( curr, (EnumDecl)n, usings );
							break;
						case Kind.Namespace:
							NamespaceDecl nsdecl = (NamespaceDecl)n;
							FillNamespace( EnsureNamespaceCreated( curr, nsdecl.name.identifier ), nsdecl, usings );
							break;
					} 
				}
			}
		}

		private static void SetDeletedFlag( UmlObject elem, UmlObject parent ) {
			elem.Deleted = true;
		}

		private void LoadClass( UmlTypeHolder scope, ClassDecl decl, UsingBlock usings ) {

			// get cl from old classes, or create a new one
			UmlClass cl = null;
			foreach( UmlType ent in scope.Types ) {
				if( ent.name.Equals( decl.name.identifier ) && ent is UmlClass ) {
					cl = (UmlClass)ent;
                    break;
				}
			}
			if( cl == null ) {
				cl = new UmlClass();
				scope.Types.Add( cl );
				added_classes.Add( cl );
			}

			// register class
			classes[cl] = decl;

			// fill with information
			switch( decl.kind ) {
				case Kind.Class:
					cl.kind = UmlKind.Class; break;
				case Kind.Interface:
					cl.kind = UmlKind.Interface; break;
				case Kind.Struct:
					cl.kind = UmlKind.Struct; break;
			}
			cl.name = decl.name.identifier;
			cl.using_chain = usings;
			cl.Deleted = false;
			FillClassWithTypes( cl, decl );
		}

		private void LoadDelegate( UmlTypeHolder scope, DelegateNode decl, UsingBlock usings ) {
			// get cl from old classes, or create a new one
			UmlDelegate deleg = null;
			foreach( UmlType ent in scope.Types ) {
				if( ent.name.Equals( decl.name.identifier ) && ent is UmlDelegate ) {
					deleg = (UmlDelegate)ent;
					break;
				}
			}
			if( deleg == null ) {
				deleg = new UmlDelegate();
				scope.Types.Add( deleg );
				added_classes.Add( deleg );
			}

			// register class
			classes[deleg] = decl;

			// fill with information
			deleg.Deleted = false;
			deleg.name = decl.name.identifier;		
		}
			
		private void LoadEnum( UmlTypeHolder scope, EnumDecl decl, UsingBlock usings ) {
			// get cl from old classes, or create a new one
			UmlEnum en = null;
			foreach( UmlType ent in scope.Types ) {
				if( ent.name.Equals( decl.name.identifier ) && ent is UmlEnum ) {
					en = (UmlEnum)ent;
					break;
				}
			}
			if( en == null ) {
				en = new UmlEnum();
				scope.Types.Add( en );
				added_classes.Add( en );
			}

			// register class
			classes[en] = decl;

			// fill with information
			en.Deleted = false;
			en.name = decl.name.identifier;		
		}

		private void FillClassWithTypes( UmlClass cl, ClassDecl classdecl ) {
			if( classdecl.members != null )
				foreach( DeclNode decl in classdecl.members.nodes )
					switch( decl.kind ) {
						case Kind.Interface:
						case Kind.Struct:
						case Kind.Class:
							LoadClass( cl, (ClassDecl)decl, null );
							break;
						case Kind.Delegate:
							LoadDelegate( cl, (DelegateNode)decl, null );
							break;
						case Kind.Enum:
							LoadEnum( cl, (EnumDecl)decl, null );
							break;
					}
		}

		#region Collect Deleted Types

		void FilterDeleted( ref ArrayList /* of UmlObject */ list ) {
			for( int i = 0; i < list.Count; i++ ) {
				UmlObject obj = (UmlObject)list[i];
				if( obj.Deleted ) {
					deleted_classes.Add( obj );
					list.RemoveAt( i );
					i--;
				} else {
					if( obj is UmlNamespace )
						CollectDeleted( (UmlNamespace)obj );
					else if( obj is UmlClass )
						CollectDeleted( (UmlClass)obj );
				}
			}

		}

		void CollectDeleted( UmlNamespace scope ) {
			if( scope.Types != null )
				FilterDeleted( ref scope.Types );
			if( scope.SubNamespaces != null )
				FilterDeleted( ref scope.SubNamespaces );
		}

		void CollectDeleted( UmlClass scope ) {
			if( scope.Types != null )
				FilterDeleted( ref scope.Types );
		}

		#endregion

		#endregion

		#region Stage #2: Building project references, Resolving inheritances

		private void BuildReferences() {

			// each C# project references to mscorlib
			UmlProject mscorlib = null;
			if( model.dllprojs == null )
				model.dllprojs = new ArrayList();
			foreach( UmlProject p in model.dllprojs )
				if( p.name.StartsWith( "mscorlib," ) ) {
                    mscorlib = p;
					break;
				}
			if( mscorlib == null ) {
				mscorlib = MSIL.ModelBuilder.CreateProjectFromName( typeof(System.Int32).Assembly.FullName );
				model.AssignUID( mscorlib );
				model.dllprojs.Add( mscorlib );
			} else
				mscorlib.Deleted = false;
			if( !MSIL.ModelBuilder.UpdateProject( mscorlib ) )
				errors.Add( "mscorlib was not found: " + mscorlib.filename );

			// rebuild references for each project
			foreach( UmlProject p in model.projects ) {
				ArrayList refproj = new ArrayList();
				refproj.Add( mscorlib );				// add mscorlib
				foreach( string s in p.refs ) {
					UmlProject found = null;

					if( s.StartsWith( "{" ) ) {
						foreach( UmlProject srcp in model.projects )
							if( srcp.guid.Equals( s ) ) {
								found = srcp;
								break;
							}

						if( found == null )
							errors.Add( "project with guid=" + s + " was not found in the model" );

					} else {
						foreach( UmlProject dllp in model.dllprojs ) {
							if( dllp.filename.Equals( s ) ) {
								found = dllp;
								break;
							}
						}

						// create new project
						if( found == null ) {
							found = MSIL.ModelBuilder.CreateProject( s );
							if( found != null && MSIL.ModelBuilder.UpdateProject( found ) ) {
								model.AssignUID( found );
								model.dllprojs.Add( found );
							} else
								errors.Add( "assembly was not found: " + s );

						// update old project
						} else if( found.Deleted ) {
							if( !MSIL.ModelBuilder.UpdateProject( found ) )
								errors.Add( "assembly was not found: " + found.filename );

							found.Deleted = false;
						}
					}
					if( found != null )
						refproj.Add( found );
				}
				p.refs = refproj;
			}

			// add assemblies, which ref new assem
			reload_assemblies:

			int added = 0;
			foreach( UmlProject p in model.dllprojs ) {
				foreach( string refname in p.refs ) {
					UmlProject found = null;
                    
					foreach( UmlProject dllp in model.dllprojs ) {
						if( dllp.name.Equals( refname ) ) {
							found = dllp;
							break;
						}
					}

					// create new project
					if( found == null ) {
						found = MSIL.ModelBuilder.CreateProjectFromName( refname );
						if( found != null && MSIL.ModelBuilder.UpdateProject( found ) ) {
							added++;
							model.AssignUID( found );
							model.dllprojs.Add( found );
						} else
							errors.Add( "assembly was not found: " + refname );

					// update old project
					} else if( found.Deleted ) {
						if( !MSIL.ModelBuilder.UpdateProject( found ) )
							errors.Add( "assembly was not found: " + found.filename );
						else
							added++;

						found.Deleted = false;
					}

					if( added > 0 )
						goto reload_assemblies;
				}
			}

			// unlink Deleted projects
			for( int i = 0; i < model.dllprojs.Count; i++ ) {
				UmlObject obj = (UmlObject)model.dllprojs[i];
				if( obj.Deleted ) {
					model.dllprojs.RemoveAt( i );
					i--;
				}
			}
		}

		private void BuildMSILInheritances() {

			Hashtable asms = new Hashtable();
			foreach( UmlProject p in model.dllprojs )
				asms[p.name] = p;

			foreach( UmlProject p in model.dllprojs )
				MSIL.ModelBuilder.Inheritances( p, asms, errors );
		}

		#endregion

		#region Stage #3: Filling members

		private string[] base_type_to_type = new string[] {
			"System.Object",
			"System.String",
			"System.Boolean",
			"System.Decimal",
			"System.Single",
			"System.Double",
			"void",
			"System.SByte",
			"System.Byte",
			"System.Int16",
			"System.UInt16",
			"System.Int32",
			"System.UInt32",
			"System.Int64",
			"System.UInt64",
			"System.Char",
		};

		private string GetTypeName( TypeNode type ) {
			switch( type.kind ) {
				case Kind.BaseType:
					return base_type_to_type[(int)((BaseType)type).typeid];
				case Kind.TypeName:
					return ((TypeName)type).typename.identifier;
				case Kind.ArrayType:
					return GetTypeName( ((ArrayType)type).parent ) + "[" + new string( ',', ((ArrayType)type).dim.count-1 ) + "]";
				case Kind.PointerType:
					return GetTypeName( ((PointerType)type).parent ) + "*";
			}
			System.Diagnostics.Debug.Fail( "wrong type" );
			return null;
		}

		private string GetFullTypeName( string name, UmlObject context ) {
			int index_sq = name.IndexOf( '[' ), index_star = name.IndexOf( '*' );
			int index;

			if( name.Equals( "void" ) )
				return name;

			if( index_sq == -1 )
				index = index_star;
			else if( index_star == -1 )
				index = index_sq;
			else
				index = Math.Min( index_sq, index_star );

			string qual_name = name;
			if( index >= 0 )
				qual_name = name.Substring( 0, index );

			UmlType type = ResolveType( qual_name, context );
			if( type == null ) {
				errors.Add( "type is not resolved in " + UmlModel.GetUniversal( context ) + ": " + name );
                return name;
			}
			qual_name = UmlModel.GetUniversal( type );
			if( index >= 0 )
				return qual_name + name.Substring( index );
			return qual_name;
		}

		private UmlMember GetMember( string name, string signature, ModifiersNode mod, UmlKind kind, UmlClass cl ) {
			UmlMember member = null;

			if( cl.Members == null )
				cl.Members = new ArrayList();
			else
				foreach( UmlMember m in cl.Members )
					if( m.signature.Equals( signature ) ) {
						member = m;
						break;
					}

			recreate:
			if( member == null ) {
				switch( kind ) {
					case UmlKind.Constant:	member = new UmlConstant(); break;
					case UmlKind.Field:		member = new UmlField(); break;
					case UmlKind.Method:	member = new UmlMethod(); break;
					case UmlKind.Property:	member = new UmlProperty(); break;
					case UmlKind.Event:		member = new UmlEvent(); break;
					case UmlKind.Indexer:	member = new UmlIndexer(); break;
					case UmlKind.Operator:	member = new UmlOperator(); break;
					case UmlKind.Constructor:member = new UmlConstructor(); break;
					case UmlKind.Destructor:member = new UmlDestructor(); break;
				}
				System.Diagnostics.Debug.Assert( member != null, "unknown Member kind" );
				member.signature = signature;
				member.name = name;
				cl.Members.Add( member );
			} else {
				if( member.Kind != kind ) {
					cl.Members.Remove( member );
					member = null;
					goto recreate;
				}

				member.Deleted = false;
			}

			member.visibility = UmlVisibility.Private;
			if( mod != null ) {
				if( (mod.value & (int)Modifiers.Internal) != 0 && (mod.value & (int)Modifiers.Protected) != 0 )
					member.visibility = UmlVisibility.ProtectedInternal;
				else if( (mod.value & (int)Modifiers.Internal) != 0 )
					member.visibility = UmlVisibility.Internal;
				else if( (mod.value & (int)Modifiers.Protected) != 0 )
					member.visibility = UmlVisibility.Protected;
				else if( (mod.value & (int)Modifiers.Public) != 0 )
					member.visibility = UmlVisibility.Public;
				else if( (mod.value & (int)Modifiers.Private) != 0 )
					member.visibility = UmlVisibility.Private;
	
				if( (mod.value & (int)Modifiers.Static) != 0 )
					member.IsStatic = true;
				if( (mod.value & (int)Modifiers.Abstract) != 0 )
					member.IsAbstract = true;
			}

			return member;
		}

		private string GenerateSignature( string name, ListNode parms, UmlClass cl, bool is_indexer ) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
			if( parms != null )
				foreach( ParameterNode pn in parms.nodes ) {
					if( sb.Length != 0 )
						sb.Append( "," );
					string type = GetFullTypeName( GetTypeName( pn.type ), cl );
					if( pn.modifiers != null ) {
						if( (pn.modifiers.value & (int)Modifiers.Out) != 0 )
							sb.Append( "out " );
						if( (pn.modifiers.value & (int)Modifiers.Ref) != 0 )
							sb.Append( "ref " );
						if( (pn.modifiers.value & (int)Modifiers.Params) != 0 )
							sb.Append( "params " );
					}
					sb.Append( type );
				}
			return is_indexer ? name + "[" + sb.ToString() + "]"
							  : name + "(" + sb.ToString() + ")";
		}

		private ArrayList GetParameters( ListNode ln, UmlClass cl ) {
            ArrayList l = new ArrayList();
			if( ln != null )
				foreach( ParameterNode pn in ln.nodes ) {
					UmlParameter param = new UmlParameter();
					param.name = pn.name.identifier;
					param.Type = GetFullTypeName( GetTypeName( pn.type ), cl );
					l.Add( param );
				}
			return l;
		}

		private void UpdateClass( UmlClass cl, ClassDecl classdecl ) {
			if( classdecl.members != null )
				foreach( DeclNode decl in classdecl.members.nodes )
					switch( decl.kind ) {
						case Kind.Const:
						case Kind.Fields:
							FieldsDecl var = (FieldsDecl)decl;
							string type = GetFullTypeName( GetTypeName( var.type ), cl );
							string name;
							foreach( Node n in var.declarators.nodes ) {
								name = n is VariableNode ? ((VariableNode)n).name.identifier : n is ConstantNode ? ((ConstantNode)n).name.identifier : null;
								UmlField f = (UmlField)GetMember( name, name, var.modifiers, decl.kind == Kind.Fields ? UmlKind.Field : UmlKind.Constant, cl );
								f.Type = type;
							}
							break;
						case Kind.UnaryOperator:
						case Kind.BinaryOperator:
						case Kind.Method:
							MethodDecl meth = (MethodDecl)decl;
							UmlKind uml_kind = decl.kind == Kind.Method ? UmlKind.Method : UmlKind.Operator;
							UmlMethod m = (UmlMethod)GetMember( meth.name.identifier, GenerateSignature( meth.name.identifier, meth.parameters, cl, false ), meth.modifiers, uml_kind, cl );
							m.ReturnType = GetFullTypeName( GetTypeName( meth.return_type ), cl );
							m.Params = GetParameters( meth.parameters, cl );
							break;
						case Kind.Constructor:
						case Kind.Destructor:
							MethodDecl cdtor = (MethodDecl)decl;
							name = decl.kind == Kind.Constructor ? cdtor.name.identifier : "~" + cdtor.name.identifier;
							UmlMember cdmemb = GetMember( name, GenerateSignature( name, cdtor.parameters, cl, false ), cdtor.modifiers, decl.kind == Kind.Constructor ? UmlKind.Constructor : UmlKind.Destructor, cl );
							break;
						case Kind.ConversionOperator:
							meth = (MethodDecl)decl;
							name = "operator " + GetFullTypeName( GetTypeName( meth.return_type ), cl );
							UmlOperator op = (UmlOperator)GetMember( name, GenerateSignature( name, meth.parameters, cl, false ), meth.modifiers, UmlKind.Operator, cl );
							break;
						case Kind.Property:
							PropertyNode prop = (PropertyNode)decl;
							UmlProperty p = (UmlProperty)GetMember( prop.name.identifier, prop.name.identifier, prop.modifiers, UmlKind.Property, cl );
							p.Type = GetFullTypeName( GetTypeName( prop.type ), cl );
							p.Accessors = string.Empty;
							foreach( AccessorNode an in prop.accessors.nodes )
								p.Accessors += an.name.identifier + ";";
							break;
						case Kind.EventVars:
							UmlEvent ev;
							EventNode evnt = (EventNode)decl;
							type = GetFullTypeName( GetTypeName( evnt.type ), cl );
							foreach( VariableNode n in evnt.vars.nodes ) {
								ev = (UmlEvent)GetMember( n.name.identifier, n.name.identifier, evnt.modifiers, UmlKind.Event, cl );
								ev.Type = type;
							}
							break;
						case Kind.EventWithAccessors:
							evnt = (EventNode)decl;
							ev = (UmlEvent)GetMember( evnt.name.identifier, evnt.name.identifier, evnt.modifiers, UmlKind.Event, cl );
							ev.Type = GetFullTypeName( GetTypeName( evnt.type ), cl );
							break;
						case Kind.Indexer:
							IndexerNode inode = (IndexerNode)decl;
							UmlIndexer indx = (UmlIndexer)GetMember( inode.name.identifier, GenerateSignature( inode.name.identifier, inode.formal_params, cl, true ), inode.modifiers, UmlKind.Indexer, cl );
							indx.Type = GetFullTypeName( GetTypeName( inode.type ), cl );
							break;
						case Kind.Delegate:
						case Kind.Enum:
						case Kind.Class:
						case Kind.Interface:
						case Kind.Struct:
							break;      // handled earlier
						default:
							System.Diagnostics.Debug.Fail( "unknown class member" );
							break;
					}

			// kill deleted

		}

		/*private void FillEnum( UmlEnum en, EnumDecl enumdecl ) {
		}

		private void FillDelegate( UmlDelegate deleg, DelegateNode decl ) {
		}*/

		#endregion

		#region TypeName Resolver

		/// <summary>
		/// Gets UmlProject for UmlObject
		/// </summary>
		private UmlProject GetProject( UmlObject o ) {
			while( o != null && o.Kind != UmlKind.Project ) 
				o = o.Parent;
			if( o != null )
				return (UmlProject)o;
			System.Diagnostics.Debug.Fail( "element has no project" );
			return null;
		}

		/// <summary>
		/// Search child with spec name in type or Namespace
		/// </summary>
		private UmlObject SearchInTypeOrNS( UmlObject o, string name ) {
			Hashtable hash = null;
			if( o is UmlNamespace )
				hash = ((UmlNamespace)o).Children;
			else if( o is UmlClass )
				hash = ((UmlClass)o).Children;
			if( hash == null || !hash.ContainsKey( name ) )
				return null;
			return (UmlObject)hash[name];
		}

		/// <summary>
		/// Gets type or Namespace name in project
		/// </summary>
		private UmlObject GetTypeOrNS( UmlProject proj, string qualified ) {
			UmlObject curr = proj.root;
			if( qualified.Length == 0 )
				return curr;
			foreach( string s in qualified.Split( new char[] { '.' } ) ) {
                curr = SearchInTypeOrNS( curr, s );
				if( curr == null )
					break;
			}
			return curr;
		}

		/// <summary>
		/// Returns qualified name of the object (without project)
		/// </summary>
		private string GetQualifiedName( UmlObject obj ) {
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

		/// <summary>
		/// Search type in object
		/// </summary>
		private UmlType SearchInside( UmlObject N, string I ) {
			if( I == null )
				return N as UmlType;

			foreach( string s in I.Split( new char[] { '.' } ) ) {
				N = SearchInTypeOrNS( N, s );
				if( N == null )
					break;
			}
			return N as UmlType;
		}

		/// <summary>
		/// Search typename in namespace 'qual_name' in curr and refs projects
		/// </summary>
		private UmlType SearchInUsing( ArrayList refs, UmlProject curr, string qual_name, string typename ) {

			UmlType res;
			UmlObject ns = GetTypeOrNS( curr, qual_name );
			if( ns != null ) {
				res = SearchInside( ns, typename );
				if( res != null )
					return res;
			}

			// search referenced projects
			foreach( UmlProject r in refs ) {
				ns = GetTypeOrNS( r, qual_name );
				if( ns != null ) {
					res = SearchInside( ns, typename );
					if( res != null )
						return res;
				}
			}
			return null;
		}

		/// <summary>
		/// Search typename in current context including referenced projects
		/// </summary>
		private UmlType SearchInScope( ArrayList refs, UmlObject cont, string typename ) {
			string qual_name = GetQualifiedName( cont );
			UmlType res;

			// search in the current project
			res = SearchInside( cont, typename );
			if( res != null )
				return res;

			// search referenced projects
			if( refs != null )
				foreach( UmlProject r in refs ) {
					UmlObject ns = GetTypeOrNS( r, qual_name );
					if( ns != null ) {
						res = SearchInside( ns, typename );
						if( res != null )
							return res;
					}
				}
			return null;

		}

		private UmlType ResolveType( string typename, UmlObject context ) {

			UmlType res;
			UsingBlock usings = null, cusings;
			UmlProject proj = GetProject( context );
			UmlObject current = context;
			int index = typename.IndexOf( '.' );
			string N = index == -1 ? typename : typename.Substring( 0, index ), I = index == -1 ? null : typename.Substring( index + 1 );

			while( current != null ) {

				// calculate usings
				if( usings == null && current is UmlType )
					usings = ((UmlType)current).using_chain;
				if( usings != null && usings.related == current ) {
					cusings = usings;
					usings = usings.parent;
				} else
					cusings = null;

				// search in current scope
				if( current is UmlTypeHolder ) {
					res = SearchInScope( proj.refs, current, typename );
					if( res != null )
						return res;

					if( cusings != null ) {
						if( cusings.aliases.ContainsKey( N ) ) {
							string right = (string)cusings.aliases[N];
							if( I != null )
								res = SearchInUsing( proj.refs, proj, right, I );
							else
								res = SearchInScope( proj.refs, proj.root, right );
							return res;
						}

						foreach( string use in cusings.list ) {
							res = SearchInUsing( proj.refs, proj, use, typename );
							if( res != null )
								return res;
						}
					}

					// try base
					if( current is UmlClass ) { 
						UmlClass cl = (UmlClass)current;
						while( cl.BaseList != null && cl.BaseList.Count > 0 ) {
							UmlObject base_class = (UmlObject)cl.BaseList[0];
							if( base_class is UmlClass )
								cl = (UmlClass)base_class;
							else
								break;
							res = SearchInScope( null, cl, typename );
							if( res != null )
								return res;
						}

					}
				}

				// one step up
				current = current.Parent;
				if( current.Kind == UmlKind.Project )
					break;
			}

			return null;
		}

		#endregion
	}
}
