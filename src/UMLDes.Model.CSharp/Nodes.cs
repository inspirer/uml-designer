// Nodes.cs

using System;
using System.Collections;
using Position = UMLDes.CSharp.parser.lapg_place;
using Symbol = UMLDes.CSharp.parser.lapg_symbol;
using System.Windows.Forms;

namespace UMLDes.CSharp.Nodes {

	/*  Notes:
	 *   Each Node has start and end positions.
	 *   Each Node represents some rule from grammar.
	 */

	public enum Kind {

		/* DeclNode */
		Namespace,
		Class, Interface, Struct, Enum,
		Method, Const, Fields, Property, Delegate, Indexer, EventVars, EventWithAccessors, Constructor, Destructor,
		ConversionOperator, UnaryOperator, BinaryOperator, Accessor,
		EnumValue,

		/* TypeNode		*/ BaseType, TypeName, PointerType, ArrayType,
		/* ExprNode		*/ This,
		/* Unary		*/ PreInc, PreDec, PostInc, PostDec, UnPlus, UnMinus, Not, BitNot, Checked, Unchecked, Ref, Out, BaseDot,
		/* Binary		*/ Plus, Minus, Div, Mod, Mult, Shl, Shr, Less, Greater, LessEq, GreaterEq, Equal, NotEqual, And, Or, Xor, AndAnd, OrOr, Arrow, Dot,
		/* ExprAndType	*/ Is, As, Cast, TypeDot,
		/* Triplex		*/ Triplex,
		/* AssignNode	*/ Assign, PlusEq, MinusEq, MultEq, DivEq, ModEq, AndEq, OrEq, XorEq, ShlEq, ShrEq,
		/* IdentNode	*/ Identifier,
		/* TypeAndList	*/ TypeOf, SizeOf, obj_or_deleg_creation,
		/* ExprAndList	*/ Call, Index, BaseIndex,
		/* TypeExpr		*/ DimmedExpr,
		/* NewArray		*/ NewArray,

		/* StatementNode */
		Label, Expr, ExprList, Empty, If, Switch, While, DoWhile, For, 
		Break, Continue, Goto, GotoCase, GotoDefault, Return, Throw, 
		Lock, UsingSt, Unsafe, CheckedSt, UncheckedSt,
		Block, StmtList,
		CaseLabel, Default,
		SwitchSect,
		Try, Finally,

		/* TypedStatementNode */
		ConstDecl, VarDecl, ForEach, Catch,

		/* DimSpecNode */
		DimSpecifier,

		/* UsingNode */
		UsingDir, UsingAlias,

		/* ParamNode */
		Param,

		/* InitializerNode */
		ExprInit, ArrayInit,

		/* VariableNode, ConstantNode */
		Variable, Constant,

		/* ConstructorInitializerNode */
		ThisConstructorInit, BaseConstructorInit,

		/* others */
		AttributeSection, Attribute, Modifiers,

		/* Special */
		List, Text, Literal, DocComment,
	}

	[Flags]
	public enum Modifiers {
		New =		0x01,
		Public =	0x02,
		Protected = 0x04,
		Internal =	0x08,
		Private =	0x10,
		Abstract =	0x20,
		Sealed =	0x40,
		Static =	0x80,
		Readonly =	0x100,
		Virtual =	0x200,
		Override =	0x400,
		Extern =	0x800,
		Volatile =	0x1000,
		Unsafe =	0x2000,

		Ref =		0x4000,
		Out =		0x8000,
		Params =	0x10000,

		Const =		0x20000,
		Event =		0x40000,

		Implicit =	0x80000,
		Explicit =	0x100000,
	}

	public enum BaseTypes {
		_object,
		_string,
		_bool,
		_decimal,
		_float,
		_double,
		_void,
		_sbyte,
		_byte,
		_short,
		_ushort,
		_int,
		_uint,
		_long,
		_ulong,
		_char,
	};

	public class Node {

		internal Kind kind;
		internal Position start, end;

		internal virtual IEnumerable Children {
			get {
				return null;
			}
		}

		public override string ToString() {
			return "( kind=" + kind + ", " + start.offset + " - " + end.offset + " )";
		}
	}

	// {{Nodes}}

	public class ListNode : Node {				// {List} (Node first) { res.nodes = new ArrayList(); res.nodes.Add(first); }
		internal ArrayList nodes;				//!
	}

	internal class ModifiersNode : Node {		// {Modifiers} () { res.start_pos.Add( s.pos.offset ); }
		internal int value;
		internal ArrayList start_pos = new ArrayList();		// !
	}

	internal class DeclNode : Node {
		internal ListNode attributes;
		internal ModifiersNode modifiers;
	}

	internal class NamespaceDecl : Node {		// {Namespace}
		internal IdentNode name;
		internal ListNode usings;
		internal ListNode members;
	}

	internal class MethodDecl : DeclNode {		// {Method,Constructor,Destructor,ConversionOperator,UnaryOperator,BinaryOperator}
		internal IdentNode name;
		internal TypeNode return_type;
		internal ListNode parameters;
		internal StatementNode body;
		internal ConstructorInitializerNode base_init;
	}

	internal class DelegateNode : DeclNode {	// {Delegate}
        internal IdentNode name;
		internal TypeNode return_type;
		internal ListNode parameters;
	}

	internal class FieldsDecl : DeclNode {		// {Field,Const}
		internal TypeNode type;
		internal ListNode declarators;
	}

	internal class ClassDecl : DeclNode {		// {Class,Interface,Struct}
		internal IdentNode name;
		internal ListNode inheritance;
		internal ListNode members;
	}

	internal class ParameterNode : DeclNode {	// {Param}
		internal TypeNode type;
		internal IdentNode name;
	}

	internal class UsingNode : Node {			// {Using,UsingAlias}
        internal IdentNode alias_id;
		internal IdentNode type_name;
	}

	internal class PropertyNode : DeclNode {	// {Property}
		internal TypeNode type;
		internal IdentNode name;
		internal ListNode accessors;
	}

	internal class AccessorNode : Node {		// {Accessor}
        internal ListNode attributes;
		internal IdentNode name;
		internal StatementNode body;
	}

	internal class EventNode : DeclNode {		// {EventVars,EventWithAccessors}
		internal TypeNode type;
		internal IdentNode name;
		internal ListNode accessors;
		internal ListNode vars;
	}

	internal class IndexerNode : DeclNode {		// {Indexer}
		internal TypeNode type;
		internal IdentNode name;
		internal ListNode formal_params;
		internal ListNode accessors;
	}

	internal class EnumDecl : DeclNode {		// {Enum}
		internal IdentNode name;
		internal TypeNode basetype;
		internal ListNode children;
	}

	internal class EnumValueNode : Node {		// {EnumValue}
		internal ListNode attributes;
		internal IdentNode name;
        internal ExprNode expr;
	}

	internal class TypeNode : Node {
	}

	internal class BaseType : TypeNode {		// {BaseType}
		internal BaseTypes typeid;
	}

	internal class TypeName : TypeNode {		// {TypeName}
		internal IdentNode typename;
	}

	internal class ArrayType : TypeNode {		// {ArrayType}
		internal TypeNode parent;
		internal DimSpecNode dim;
	}

	internal class PointerType : TypeNode {		// {PointerType}
		internal TypeNode parent;
	}

	internal class DimSpecNode : Node {			// {DimSpecifier}
		internal int count;
	}

	internal class StatementNode : Node {		// {Label,Expr,Empty,If,Switch,While,DoWhile,For,Foreach,Break,Continue,Goto,GotoCase,GotoDefault,Return,Throw,Lock,Unsafe,CheckedSt,UncheckedSt,Block,VarDecl,SwitchSect,CaseLabel,Default,ExprList,Try,Catch,Finally}
		internal ExprNode expr;
		internal IdentNode label;
		internal StatementNode stmt1;
		internal StatementNode stmt2;
		internal ListNode stmts;
	}

	internal class ExprNode : Node {			// {This,}
	}

	internal class IdentNode : ExprNode {		// {Identifier}
		internal string identifier;
	}

	internal class LiteralNode : ExprNode {		// {Literal}
		internal enum Type { Integer, Float, Char, String, Boolean, Null }

		internal LiteralNode.Type type;
		internal object value;
	}

	internal class BinaryNode : ExprNode {		// {Plus,Minus,Div,Mod,Mult,Shl,Shr,Less,Greater,LessEq,GreaterEq,Equal,NotEqual,And,Or,Xor,AndAnd,OrOr,Arrow,Dot}
		internal ExprNode left;
		internal ExprNode right;
	}

	internal class ExprAndTypeNode : ExprNode {	// {Is,As,Cast}
		internal ExprNode expr;
        internal TypeNode type;
	}

	internal class UnaryNode : ExprNode {		// {PreInc,PreDec,PostInc,PostDec,UnPlus,UnMinus,Not,BitNot,Checked,Unchecked,Ref,Out,BaseDot}
		internal ExprNode expr;
	}

	internal class TriplexNode : ExprNode {		// {Triplex}
		internal ExprNode cond;
		internal ExprNode Then;
		internal ExprNode Else;
	}

	internal class AssignNode : ExprNode {		// {Assign,PlusEq,MinusEq,MultEq,DivEq,ModEq,AndEq,OrEq,XorEq,ShlEq,ShrEq}
		internal ExprNode dest;
		internal ExprNode source;
	}

	internal class ExprAndListNode : ExprNode {	// {Call,Index,ArrType,BaseIndex}
		internal ExprNode expr;
		internal ListNode list;
	}

	internal class TypeAndListNode : ExprNode {	// {TypeOf,SizeOf,obj_or_deleg_creation}
		internal TypeNode type;
		internal ListNode list;
	}

	internal class TypeExprNode : ExprNode {	// {DimmedExpr}
        internal ExprNode expr;
		internal DimSpecNode spec;
	}

	internal class NewArrayNode : ExprNode {	// {NewArray}
		internal TypeNode type;
		internal ListNode exprlist;
		internal ListNode ranks;
		internal Node arrayinit;
	}

	internal class TypedStatementNode : StatementNode {		// {VarDecl,ForEach}
		internal TypeNode type;
	}

	internal class InitializerNode : Node {		// {ExprInit,ArrayInit}
		internal ExprNode expr;
		internal ListNode subinits;
	}

	internal class VariableNode : Node {		// {Variable}
        internal IdentNode name;
		internal InitializerNode init;
	}

	internal class ConstantNode : Node {		// {Constant}
		internal IdentNode name;
		internal ExprNode constant;
	}

	internal class ConstructorInitializerNode : Node {	// {ThisConstructorInit,BaseConstructorInit}
		internal ListNode args;
	}

	internal class AttributeSectionNode : Node {	// {AttributeSection}
		internal IdentNode target;
		internal ListNode attrs;
	}

	internal class AttributeNode : Node {				// {Attribute}
		internal IdentNode name;
		internal ListNode parms;
	}

	// {{End}}

	internal class Make {

		#region Autogenerated by node_builder.pl

		// {{Methods}}

		internal static ListNode List( Node first, Symbol s ) {
			ListNode res = new ListNode();
			res.kind = Kind.List;
			res.start = s.pos;
			res.end = s.endpos;
			res.nodes = new ArrayList();
			res.nodes.Add(first);
			return res;
		}

		internal static ModifiersNode Modifiers( int value, Symbol s ) {
			ModifiersNode res = new ModifiersNode();
			res.kind = Kind.Modifiers;
			res.start = s.pos;
			res.end = s.endpos;
			res.value = value;
			res.start_pos.Add( s.pos.offset );
			return res;
		}

		internal static NamespaceDecl Namespace( IdentNode name, ListNode usings, ListNode members, Symbol s ) {
			NamespaceDecl res = new NamespaceDecl();
			res.kind = Kind.Namespace;
			res.start = s.pos;
			res.end = s.endpos;
			res.name = name;
			res.usings = usings;
			res.members = members;
			return res;
		}

		internal static MethodDecl Method( Kind k, ListNode attributes, ModifiersNode modifiers, IdentNode name, TypeNode return_type, ListNode parameters, StatementNode body, ConstructorInitializerNode base_init, Symbol s ) {
			MethodDecl res = new MethodDecl();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.name = name;
			res.return_type = return_type;
			res.parameters = parameters;
			res.body = body;
			res.base_init = base_init;
			return res;
		}

		internal static DelegateNode Delegate( ListNode attributes, ModifiersNode modifiers, IdentNode name, TypeNode return_type, ListNode parameters, Symbol s ) {
			DelegateNode res = new DelegateNode();
			res.kind = Kind.Delegate;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.name = name;
			res.return_type = return_type;
			res.parameters = parameters;
			return res;
		}

		internal static FieldsDecl Fields( Kind k, ListNode attributes, ModifiersNode modifiers, TypeNode type, ListNode declarators, Symbol s ) {
			FieldsDecl res = new FieldsDecl();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.type = type;
			res.declarators = declarators;
			return res;
		}

		internal static ClassDecl Class( Kind k, ListNode attributes, ModifiersNode modifiers, IdentNode name, ListNode inheritance, ListNode members, Symbol s ) {
			ClassDecl res = new ClassDecl();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.name = name;
			res.inheritance = inheritance;
			res.members = members;
			return res;
		}

		internal static ParameterNode Parameter( ListNode attributes, ModifiersNode modifiers, TypeNode type, IdentNode name, Symbol s ) {
			ParameterNode res = new ParameterNode();
			res.kind = Kind.Param;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.type = type;
			res.name = name;
			return res;
		}

		internal static UsingNode Using( Kind k, IdentNode alias_id, IdentNode type_name, Symbol s ) {
			UsingNode res = new UsingNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.alias_id = alias_id;
			res.type_name = type_name;
			return res;
		}

		internal static PropertyNode Property( ListNode attributes, ModifiersNode modifiers, TypeNode type, IdentNode name, ListNode accessors, Symbol s ) {
			PropertyNode res = new PropertyNode();
			res.kind = Kind.Property;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.type = type;
			res.name = name;
			res.accessors = accessors;
			return res;
		}

		internal static AccessorNode Accessor( ListNode attributes, IdentNode name, StatementNode body, Symbol s ) {
			AccessorNode res = new AccessorNode();
			res.kind = Kind.Accessor;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.name = name;
			res.body = body;
			return res;
		}

		internal static EventNode Event( Kind k, ListNode attributes, ModifiersNode modifiers, TypeNode type, IdentNode name, ListNode accessors, ListNode vars, Symbol s ) {
			EventNode res = new EventNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.type = type;
			res.name = name;
			res.accessors = accessors;
			res.vars = vars;
			return res;
		}

		internal static IndexerNode Indexer( ListNode attributes, ModifiersNode modifiers, TypeNode type, IdentNode name, ListNode formal_params, ListNode accessors, Symbol s ) {
			IndexerNode res = new IndexerNode();
			res.kind = Kind.Indexer;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.type = type;
			res.name = name;
			res.formal_params = formal_params;
			res.accessors = accessors;
			return res;
		}

		internal static EnumDecl Enum( ListNode attributes, ModifiersNode modifiers, IdentNode name, TypeNode basetype, ListNode children, Symbol s ) {
			EnumDecl res = new EnumDecl();
			res.kind = Kind.Enum;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.modifiers = modifiers;
			res.name = name;
			res.basetype = basetype;
			res.children = children;
			return res;
		}

		internal static EnumValueNode EnumValue( ListNode attributes, IdentNode name, ExprNode expr, Symbol s ) {
			EnumValueNode res = new EnumValueNode();
			res.kind = Kind.EnumValue;
			res.start = s.pos;
			res.end = s.endpos;
			res.attributes = attributes;
			res.name = name;
			res.expr = expr;
			return res;
		}

		internal static BaseType BaseType( BaseTypes typeid, Symbol s ) {
			BaseType res = new BaseType();
			res.kind = Kind.BaseType;
			res.start = s.pos;
			res.end = s.endpos;
			res.typeid = typeid;
			return res;
		}

		internal static TypeName TypeName( IdentNode typename, Symbol s ) {
			TypeName res = new TypeName();
			res.kind = Kind.TypeName;
			res.start = s.pos;
			res.end = s.endpos;
			res.typename = typename;
			return res;
		}

		internal static ArrayType ArrayType( TypeNode parent, DimSpecNode dim, Symbol s ) {
			ArrayType res = new ArrayType();
			res.kind = Kind.ArrayType;
			res.start = s.pos;
			res.end = s.endpos;
			res.parent = parent;
			res.dim = dim;
			return res;
		}

		internal static PointerType PointerType( TypeNode parent, Symbol s ) {
			PointerType res = new PointerType();
			res.kind = Kind.PointerType;
			res.start = s.pos;
			res.end = s.endpos;
			res.parent = parent;
			return res;
		}

		internal static DimSpecNode DimSpec( int count, Symbol s ) {
			DimSpecNode res = new DimSpecNode();
			res.kind = Kind.DimSpecifier;
			res.start = s.pos;
			res.end = s.endpos;
			res.count = count;
			return res;
		}

		internal static StatementNode Statement( Kind k, ExprNode expr, IdentNode label, StatementNode stmt1, StatementNode stmt2, ListNode stmts, Symbol s ) {
			StatementNode res = new StatementNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.label = label;
			res.stmt1 = stmt1;
			res.stmt2 = stmt2;
			res.stmts = stmts;
			return res;
		}

		internal static ExprNode Expr( Kind k, Symbol s ) {
			ExprNode res = new ExprNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			return res;
		}

		internal static IdentNode Ident( string identifier, Symbol s ) {
			IdentNode res = new IdentNode();
			res.kind = Kind.Identifier;
			res.start = s.pos;
			res.end = s.endpos;
			res.identifier = identifier;
			return res;
		}

		internal static LiteralNode Literal( LiteralNode.Type type, object value, Symbol s ) {
			LiteralNode res = new LiteralNode();
			res.kind = Kind.Literal;
			res.start = s.pos;
			res.end = s.endpos;
			res.type = type;
			res.value = value;
			return res;
		}

		internal static BinaryNode Binary( Kind k, ExprNode left, ExprNode right, Symbol s ) {
			BinaryNode res = new BinaryNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.left = left;
			res.right = right;
			return res;
		}

		internal static ExprAndTypeNode ExprAndType( Kind k, ExprNode expr, TypeNode type, Symbol s ) {
			ExprAndTypeNode res = new ExprAndTypeNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.type = type;
			return res;
		}

		internal static UnaryNode Unary( Kind k, ExprNode expr, Symbol s ) {
			UnaryNode res = new UnaryNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			return res;
		}

		internal static TriplexNode Triplex( ExprNode cond, ExprNode Then, ExprNode Else, Symbol s ) {
			TriplexNode res = new TriplexNode();
			res.kind = Kind.Triplex;
			res.start = s.pos;
			res.end = s.endpos;
			res.cond = cond;
			res.Then = Then;
			res.Else = Else;
			return res;
		}

		internal static AssignNode Assign( Kind k, ExprNode dest, ExprNode source, Symbol s ) {
			AssignNode res = new AssignNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.dest = dest;
			res.source = source;
			return res;
		}

		internal static ExprAndListNode ExprAndList( Kind k, ExprNode expr, ListNode list, Symbol s ) {
			ExprAndListNode res = new ExprAndListNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.list = list;
			return res;
		}

		internal static TypeAndListNode TypeAndList( Kind k, TypeNode type, ListNode list, Symbol s ) {
			TypeAndListNode res = new TypeAndListNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.type = type;
			res.list = list;
			return res;
		}

		internal static TypeExprNode TypeExpr( ExprNode expr, DimSpecNode spec, Symbol s ) {
			TypeExprNode res = new TypeExprNode();
			res.kind = Kind.DimmedExpr;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.spec = spec;
			return res;
		}

		internal static NewArrayNode NewArray( TypeNode type, ListNode exprlist, ListNode ranks, Node arrayinit, Symbol s ) {
			NewArrayNode res = new NewArrayNode();
			res.kind = Kind.NewArray;
			res.start = s.pos;
			res.end = s.endpos;
			res.type = type;
			res.exprlist = exprlist;
			res.ranks = ranks;
			res.arrayinit = arrayinit;
			return res;
		}

		internal static TypedStatementNode TypedStatement( Kind k, ExprNode expr, IdentNode label, StatementNode stmt1, StatementNode stmt2, ListNode stmts, TypeNode type, Symbol s ) {
			TypedStatementNode res = new TypedStatementNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.label = label;
			res.stmt1 = stmt1;
			res.stmt2 = stmt2;
			res.stmts = stmts;
			res.type = type;
			return res;
		}

		internal static InitializerNode Initializer( Kind k, ExprNode expr, ListNode subinits, Symbol s ) {
			InitializerNode res = new InitializerNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.expr = expr;
			res.subinits = subinits;
			return res;
		}

		internal static VariableNode Variable( IdentNode name, InitializerNode init, Symbol s ) {
			VariableNode res = new VariableNode();
			res.kind = Kind.Variable;
			res.start = s.pos;
			res.end = s.endpos;
			res.name = name;
			res.init = init;
			return res;
		}

		internal static ConstantNode Constant( IdentNode name, ExprNode constant, Symbol s ) {
			ConstantNode res = new ConstantNode();
			res.kind = Kind.Constant;
			res.start = s.pos;
			res.end = s.endpos;
			res.name = name;
			res.constant = constant;
			return res;
		}

		internal static ConstructorInitializerNode ConstructorInitializer( Kind k, ListNode args, Symbol s ) {
			ConstructorInitializerNode res = new ConstructorInitializerNode();
			res.kind = k;
			res.start = s.pos;
			res.end = s.endpos;
			res.args = args;
			return res;
		}

		internal static AttributeSectionNode AttributeSection( IdentNode target, ListNode attrs, Symbol s ) {
			AttributeSectionNode res = new AttributeSectionNode();
			res.kind = Kind.AttributeSection;
			res.start = s.pos;
			res.end = s.endpos;
			res.target = target;
			res.attrs = attrs;
			return res;
		}

		internal static AttributeNode Attribute( IdentNode name, ListNode parms, Symbol s ) {
			AttributeNode res = new AttributeNode();
			res.kind = Kind.Attribute;
			res.start = s.pos;
			res.end = s.endpos;
			res.name = name;
			res.parms = parms;
			return res;
		}

		// {{End}}

		#endregion

		#region Hand-made modification routines

		internal static void AddList( ListNode list, Node next, Symbol s ) {
			list.nodes.Add(next);
			list.end = s.endpos;
		}

		internal static void AddModifier( ModifiersNode mod, int next, int next_offset, Symbol s ) {
            mod.value |= next;
			mod.start_pos.Add( next_offset );
			mod.end = s.endpos;
		}

		internal static void AddIdent( IdentNode id, string postfix, Symbol s ) {
			id.identifier += postfix;
            id.end = s.endpos;
		}

		internal static MethodDecl Operator( ModifiersNode mod, TypeNode type, IdentNode name, ParameterNode op1, ParameterNode op2, Symbol s ) {
            ListNode ln = new ListNode();
			ln.nodes.Add( op1 );
			if( op2 != null ) 
				ln.nodes.Add( op2 );
			ln.start = op1.start;
			ln.end = op2 != null ? op2.end : op1.end;
			ln.kind = Kind.List;
			return Method( name == null ? Kind.ConversionOperator : op2 == null ? Kind.UnaryOperator : Kind.BinaryOperator, null, mod, name, type, ln, null, null, s );
		}

		internal static ArrayType ListArrayType( TypeNode parent, ListNode dims, Symbol s ) {
			TypeNode current = parent;
			foreach( DimSpecNode n in dims.nodes )
				current = OneArrayType( current, n, s );
			return (ArrayType)current;
		}

		internal static ArrayType OneArrayType( TypeNode parent, DimSpecNode dim, Symbol s ) {
			if( parent.kind != Kind.ArrayType )
				return ArrayType( parent, dim, s );
			else {
				ArrayType p = (ArrayType)parent;
				while( p.parent.kind == Kind.ArrayType )
					p = (ArrayType)p.parent;
				p.parent = Make.ArrayType( p.parent, dim, s );
				return (ArrayType)parent;
			}
		}

		internal static ListNode comments = null;

		internal static void Comment( string name, Symbol s ) {
			/*DocCommentNode com = new DocCommentNode( ref s, text );

			if( comments != null ) { 
				int last_offset = ((DocCommentNode)comments.nodes[comments.nodes.Count-1]).end.offset;
				if( this.text.Substring( last_offset, s.pos.offset - last_offset ).Trim().Length != 0 )
					comments = null;
			}
			if( comments == null )
				comments = new ListNode( ref s, com );
			else
				comments.Add( ref s, com );*/
		}

		internal static void checkDeclComment( Symbol s ) {
			/*if( comments != null ) { 
				int last_offset = ((DocCommentNode)comments.nodes[comments.nodes.Count-1]).end.offset;
				if( this.text.Substring( last_offset, s.pos.offset - last_offset ).Trim().Length != 0 )
					comments = null;
			}*/
		}

		internal static void Pos( ref Symbol s ) {
			Node n = s.sym as Node;
			if( n != null ) {
				n.start = s.pos;
				n.end = s.endpos;
			}
		}

		internal static ModifiersNode SumModifiers( ModifiersNode m1, ModifiersNode m2 ) {
			m1.end = m2.end;
			m1.value |= m2.value;
			m1.start_pos.AddRange( m2.start_pos );
			return m1;
		}

		#endregion
	}

	internal class Util {

		internal static bool CanBeType( Node expr ) {
			switch( expr.kind ) {
				case Kind.Identifier: case Kind.TypeDot:
					return true;
				case Kind.Dot: 
					return CanBeType( ((BinaryNode)expr).left );
				case Kind.DimmedExpr:
					return CanBeType( ((TypeExprNode)expr).expr );
			}
			return false;
		}

		internal static bool ContainsDimSpec( Node expr ) {
			return expr.kind == Kind.DimmedExpr;
		}

		internal static TypeNode type_from_expr( ExprNode expr ) {
			Symbol s = new Symbol();
			s.pos = expr.start;
			s.endpos = expr.end;

			switch( expr.kind ) {
				case Kind.Identifier:
					return Make.TypeName( (IdentNode)expr, s );
				case Kind.Dot: case Kind.TypeDot:
					string id = String.Empty;
					while( expr.kind == Kind.Dot ) {
						IdentNode idn = ((BinaryNode)expr).right as IdentNode;
						id = "." + idn.identifier + id;
						expr = ((BinaryNode)expr).left;
					}
					if( expr.kind == Kind.TypeDot ) {
						// types like: int.A
						ExprAndTypeNode etn = (ExprAndTypeNode)expr;
						id = (etn.type as BaseType).typeid.ToString().Substring(1) + "." + (etn.expr as IdentNode).identifier + id;
						return Make.TypeName( Make.Ident( id, s ), s );
					} else if( expr.kind == Kind.Identifier ) {
						// types like: a.b.c
						((IdentNode)expr).identifier += id;
						return Make.TypeName( (IdentNode)expr, s );
					}
					return null;
				case Kind.DimmedExpr:
					TypeExprNode ten = (TypeExprNode)expr;
					return Make.OneArrayType( type_from_expr( ten.expr ), ten.spec, s );
			}
			return null;
		}
	}

	#region Node Debug

	class NodeView : TreeView {

		#region Tag Class

		internal class NodeTag {
			internal object n;
            internal bool initialized;

			internal NodeTag( object n ) {
				this.n = n;
			}
		}

		#endregion

		Node n;
		string text;

		private NodeView( Node n, string text ) {
            this.n = n;
			this.text = text;
			TreeNode tn = this.Nodes.Add( n.GetType().Name + " " + n.ToString() );
			tn.Tag = new NodeTag( n );
			init_children( tn );
			tn.Expand();
		}

		void new_child( TreeNode t, string name, object n ) {
			TreeNode tn = t.Nodes.Add( name );
			tn.Tag = new NodeTag( n );
		}

		string object_text( object obj ) {
			if( obj == null )
				return " = null";
			if( obj is string )
				return " = \"" + obj.ToString()+"\"";
			else if( obj is Node )
				return " : " + obj.GetType().Name + "  " + obj.ToString();
			else if( obj.GetType().IsPrimitive )
				return " = " + obj.ToString();
			else
				return " : " + obj.GetType().Name;
		}

		void init_children( TreeNode t ) {
			NodeTag tag = t.Tag as NodeTag;
			if( tag.initialized )
				return;
			tag.initialized = true;
			if( tag.n != null ) {
				
				string nulls = String.Empty;
				foreach( System.Reflection.FieldInfo fi in tag.n.GetType().GetFields( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public ) ) {
					if( fi.DeclaringType.Equals( typeof( Node ) ) )
						continue;
					object val = fi.GetValue( tag.n );
					if( val != null ) {
						if( val is ArrayList ) {
							int i = 0;
							foreach( object o in (val as ArrayList) )
								new_child( t, fi.Name + "[" + (i++) + "]" + object_text( o ), o != null && !o.GetType().IsPrimitive ? o : null );

						} else 
							new_child( t, fi.Name + object_text( val ), val != null && !val.GetType().IsPrimitive ? val : null );
					} else {
						nulls += ", " + fi.Name;
					}
				}
				if( nulls.Length > 0 )
					new_child( t, nulls.Substring(2) + " = null", null );
			}
		}

		protected override void OnBeforeExpand(TreeViewCancelEventArgs e) {
			foreach( TreeNode t in e.Node.Nodes )
				init_children( t );
			base.OnBeforeExpand (e);
		}

		protected override void OnKeyDown(KeyEventArgs e) {
			if( e.KeyCode == Keys.Enter && (this.SelectedNode.Tag as NodeTag).n != null && (this.SelectedNode.Tag as NodeTag).n is Node ) {
				Node n = (this.SelectedNode.Tag as NodeTag).n as Node;
				Form child = new Form();
				child.Width = 700; child.Height = 400;
				child.StartPosition = FormStartPosition.CenterScreen;
				TextBox tb = new TextBox();
				tb.Multiline = true;
				tb.Dock = DockStyle.Fill;
				tb.Text = text.Substring( n.start.offset, n.end.offset - n.start.offset );
				tb.Select(0,0);
				tb.KeyDown += new KeyEventHandler(tb_KeyDown);
				child.Text = n.ToString();
				child.Controls.Add( tb );
				child.ShowDialog();
			} else
				base.OnKeyDown (e);
		}

		private void tb_KeyDown(object sender, KeyEventArgs e) {
            if( e.KeyCode == Keys.Escape )
				(sender as TextBox).FindForm().Close();
		}

		public static void ShowNode( Node n, string text ) {
            Form f = new Form();
			f.Text = "Node Debug";
			f.Width = 800; f.Height = 450;
			NodeView nv = new NodeView( n, text );
			nv.Dock = DockStyle.Fill;
			f.Controls.Add( nv );
			f.ShowDialog();
		}
	}

	#endregion
}
