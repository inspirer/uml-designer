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

		private static void try_load( string out_file, string[] projs ) {

			Console.WriteLine( Path.GetFileNameWithoutExtension( out_file ) );

			ArrayList errors;
			UmlModel m = ModelBuilder.CreateEmptyModel();
			foreach( string s in projs ) 
				ModelBuilder.AddProject( m, s );
			ModelBuilder.UpdateModel( m, out errors );
			if( errors != null ) {
				Console.WriteLine( out_file );
				foreach( string err in errors )
					Console.WriteLine( err );
				return;
			}

			try {
				XmlSerializer ser = new XmlSerializer( typeof( UmlModel ) );
				StreamWriter sw = new StreamWriter( out_file );
				ser.Serialize( sw, m );
				Console.WriteLine( "passed" );
			}
			catch( Exception ex ) {
				Console.WriteLine( ex.ToString() );
				Console.WriteLine( "failed" );
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {

			/*if( args.Length > 0 ) {
				string text = new StreamReader(args[0]).ReadToEnd();
				NamespaceDecl node = parser.parse( text );
				Console.WriteLine( "parsed\n" );
				NodeView.ShowNode( node, text );
				return;
			}*/

			string base_dir = @"D:\projects\CDS\report\";
			if( !Directory.Exists( base_dir ) )
				Directory.CreateDirectory( base_dir );


			try_load( base_dir + "UMLDes.xml", new string[] {
													@"D:\projects\CDS\UMLDes.Model.CSharp\UMLDes.Model.CSharp.csproj",
													@"D:\projects\CDS\UMLDes.Model\UMLDes.Model.csproj",
													@"D:\projects\CDS\UmlDes.Gui\UmlDes.Gui.csproj",
													@"D:\projects\CDS\UMLDes.Model.Tests\UMLDes.Model.Tests.csproj"
												} );

			try_load( base_dir + "test_lib.xml", new string[] { 
													@"D:\projects\test_lib\WindowsApplication1\WindowsApplication1.csproj"
												} );
		}
	}
}
