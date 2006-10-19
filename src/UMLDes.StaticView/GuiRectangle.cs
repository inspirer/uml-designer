using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using UMLDes.Model;
using UMLDes.Controls;

namespace UMLDes.GUI {

	#region Text information

	public class GuiString {

		internal SizeF size;
		internal int y_offset;
		//internal Rectangle area;
		internal FontStyle font_style;
		internal FontTypes font_type;
		internal bool Center;
		internal string Text;

		public GuiString( FontStyle font_style, FontTypes font_type, bool Center, string text ) {
			this.font_style = font_style;
			this.font_type = font_type;
			this.Center = Center;
			this.Text = text;
		}

		public GuiString() {}
	}

	#endregion

	/// <summary>
	/// Rectangle item, which can draw text
	/// </summary>
	public abstract class GuiRectangle : GuiPolygonItem, IDynamicContent {

		internal ArrayList content = new ArrayList();

		public GuiRectangle() {
			parent = null;
		}

		#region Paint

		public const int padding = 8, line_space = 2, vpadding = 5;

		protected override Point[] GetPoints() {
			return new Point[] { new Point(X,Y), new Point(X,Y+Height), new Point(X+Width,Y+Height), new Point(X+Width,Y) };
		}


		/// <summary>
		/// Calculates width and height of object
		/// </summary>
		/// <param name="g">graphics object for measurements</param>
		public override void RefreshView( Graphics g ) {
			int width = 50, y = vpadding;
			bool was_string = false;

			foreach( GuiString s in content )
				if( s.Text != null ) {
					Font f = parent.cview.GetFont( s.font_type, s.font_style );
					s.size = g.MeasureString( s.Text, f );
					s.y_offset = y;
					width = Math.Max( (int)s.size.Width + 2*padding, width );
					y += (int)s.size.Height + line_space;
					was_string = true;
				} else {
					y += was_string ? vpadding - line_space : vpadding;
					was_string = false;
					s.y_offset = y;
					y += vpadding;
				}

			Width = width;
			Height = y + (was_string ? vpadding - line_space : vpadding);
			setup_edges();
		}

		public override void Paint( Graphics g, int x, int y ) {
			foreach( GuiString s in content ) {
				if( s.Text != null ) {
					Font f = parent.cview.GetFont( s.font_type, s.font_style );
					int textdx = (s.Center ? (( Width - (int)s.size.Width ) / 2) : padding);
					g.DrawString( s.Text, f, Brushes.Black, x + textdx, y + s.y_offset );
				} else {
					g.DrawLine( Pens.Black, x, y + s.y_offset, x + Width - 1, y + s.y_offset );
				}
			}
		}

		#endregion

		#region Content, PostLoad

		protected abstract void fillContent( ArrayList l );

		public override void PostLoad() {
			content.Clear();
			fillContent( content );
			parent.RefreshObject( this );
			setup_edges();
			base.PostLoad ();
		}

		public virtual void RefreshContent() {
			content.Clear();
			fillContent( content );
			StateChanged();
		}

		public void Created() {
			content.Clear();
			fillContent( content );
		}

		#endregion
	}

}
