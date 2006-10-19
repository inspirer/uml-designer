
// delegate & attributes test

[A, B, C] [return:Q] [I(a,b,c=5)] public delegate void PPP ( Q a, P b );

// interface test

[q] internal interface A  : P3, p4 {
	int A { [a] get; }
	char C { [b] set; [c] get; }
	
	new void B( [q] int c, char e );
	event B2 yo;
	QQQ this[ int c, char b] { get; set; }
}

// enum test

public enum A : int { A, B = 4 }

// Arrays type test, Initializers

class ArrayTest {
// In effect, the rank-specifiers are read from left to right before 
// the final non-array element type. [Example: The type int[][,,][,]
// is a single-dimensional array of three-dimensional arrays of 
// two-dimensional arrays of int. end example]

	int[][,,][,] q;
	static int[,][] p = new int[,][] { { {1,2}, {3,4} } } ;
}

// struct test

[a] public struct A : b,c,d { int i; }


// constructor/destructor

class ConstructorDestructor :Q  {
	[a] public ConstructorDestructor( int e ) : base(1,2,3) { }
	[c] extern ~ConstructorDestructor() { }
}

// operators, indexers

class Operator {
	[a] public int operator ++ ( int a ) { }
	[a] public int operator + ( int a, int b ) { }
	[a] public implicit operator char( int b ) { }
	[a] implicit operator char( int b ) { }
	[a] int operator + ( int a, int b ) { }

	[a] public static System.Int32 this[int a, char b] { get; set; }
	[a] public static System.Int32 QA.this[byte c] { set { } }
}


// Events, Properties

class Events {
    [p] public event Chat a, b, c;
	[q] static event Yoyo q.p.t { get; set; }

	[prop] public static int yo.yo { get {} set; }
}

// Methods

class Methods {
    [meth] public yo a.p.q ( );
    [meth] public yo a.p.q ( [p] ref int a, [q] out char b, params yo[] id ) { e = 5; }
    [meth] public yo a.p.q ( [a] params yo[] id ) { e = 5; }

}

// Const & Fields

class Vars {
	[a] public int i;
	const char c = 5;
	[qp] public static const string[] yo = new string[] { "yo " };
}

// Namespaces, Usings

namespace A.P.Q {
	using A = P.Q.D;
	namespace D {
		using aaaa;
		using ca.QQQ;

		class  A {}
	}
}


// types

class A {
	void Method() {
		Aaaaa.qqq.p a = null;
		DDD.p[] a = null;
		DDD[][,,][,] q;
		int.a.b p;
		int.a p;
		int p;
	}
}

// expressions

class Expr {
	int e = 1 << a + b;
	int q = 1>> c -b;
	char q = 1 && 2 || c * true;
	char p = (1);
	long r = a.b.c;
	long m = int.MaxValue;
	int i = ++p / --q + p++ + q++ % !sdf % ~qwe;

	string[] s = new string[] { "yo" };
	string[][,] s = new string[3][,] { "yo2" };

	int i = this;
	char c = base.X;
	sdf q = base[i];
	Type t = typeof(int) + a();

	A a = new A(1,2) + (a is A) - (b as B);
	int i = sizeof(int) + checked(i) + unchecked(e);
}


// casting

// (x)y, (x)(y), and (x)(-y) are cast-expressions, 
// but (x)-y is not, even if x identifies a type. 
// if x == int, then all four forms are cast-expressions 

class a { void m() {
	
	// cast
	int e = (x)y;
	int q = (x)(y);
	int p = (x)(-y);
	// not-cast
	int t = (x)-y;
	// cast
	int t2 = (int)-y;
	int t = (x[][,])-y;
} }


class Statement {
	void m() {
		if( a ) b(); else c();
		if( a ) b();
		while( a ) b();
		while( A ) { c = 5; }
		do a();
		while(c);
		foreach( a b in c ) {
		}
		for( int i = 0; i < 5; i++ ) { }

	}
}