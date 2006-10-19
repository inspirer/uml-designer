using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using UMLDes.Model.CSharp;

namespace UMLDes.Model {
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1 {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			//string text;

			//if( args.Length > 0 ) 
			//	text = new StreamReader(args[0]).ReadToEnd();
			//else 
			//	text = new StreamReader( /*System.Console.OpenStandardInput()*/ @"D:\projects\CDS\UMLDes.Model\test\test.cs" ).ReadToEnd();

			ArrayList errors;
			UmlModel m = ModelBuilder.CreateEmptyModel();
			//ModelBuilder.AddProject( m, @"D:\projects\CDS\CDS.csproj" );
			ModelBuilder.AddProject( m, @"D:\projects\CDS\UMLDes.Model.CSharp\UMLDes.Model.CSharp.csproj" );
			ModelBuilder.AddProject( m, @"D:\projects\CDS\UMLDes.Model\UMLDes.Model.csproj" );
			//ModelBuilder.AddProject( m, @"D:\projects\test_lib\WindowsApplication1\WindowsApplication1.csproj" );
			ModelBuilder.UpdateModel( m, out errors );
			if( errors != null ) {
				foreach( string err in errors )
					Console.WriteLine( err );
				return;
			}

			try {
				XmlSerializer ser = new XmlSerializer( typeof( UmlModel ) );
				StreamWriter sw = new StreamWriter( @"d:\temp\model.xml" );
				ser.Serialize( sw, m );
			}
			catch( Exception ex ) {
				Console.WriteLine( ex.ToString() );
			}

			//NamespaceDecl node = parser.parse( text );
			//Console.WriteLine( "parsed\n" );
			//NodeView.ShowNode( node, text );
		}
	}
}
