//#define DEBUG_INVALIDATE

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Drawing.Drawing2D;
using UMLDes.Model;

namespace UMLDes {

	public enum FontTypes : int {
		DEFAULT,
		ROLE_NAME,
		LAST
	}

	public enum FormatTypes : int {
		CENTER,
		LAST
	}

	/// <summary>
	/// UI Control which contains StaticView, hides scrolling
	/// </summary>
	public class ViewCtrl : System.Windows.Forms.UserControl, IDisposable {

		GUI.View curr;
		public object DragObject;

		const int left_margin = 1, top_margin = 1, right_margin = 1, bottom_margin = 1;
		Rectangle DiagramArea;
		int offx, offy;

		/// <summary>current StaticView to show</summary>
		public GUI.View Curr {
			get {
				return curr;
			}
			set {
				if( curr != null )
					curr.cview = null;
				offx = offy = 0;
				curr = value;
				if( value != null )
					value.cview = this;
			}
		}

		public ViewCtrl() {
			this.Name = "ViewCtrl";

#if !DEBUG_INVALIDATE
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
#endif

			curr = null;
			offx = offy = 0;
			x_res = 1119; y_res = 777;

			SetupResources();
		}

		#region Fonts, String Formats

		private Font[,] shared_fonts;
		private StringFormat[] shared_formats;

		public StringFormat GetStringFormat ( FormatTypes type ) {
			return shared_formats[(int)type];
		}

		public Font GetFont( FontTypes type, FontStyle s ) {
			int i = ((s & FontStyle.Bold) == FontStyle.Bold ? 1 : 0 ) |
				((s & FontStyle.Italic) == FontStyle.Italic ? 2 : 0 ) |
				((s & FontStyle.Underline) == FontStyle.Underline ? 4 : 0 );
			if( shared_fonts[(int)type,i] == null )
				shared_fonts[(int)type,i] = new Font( shared_fonts[(int)type,0].FontFamily, shared_fonts[(int)type,0].SizeInPoints, s );

			return shared_fonts[(int)type,i];
		}

		private void SetupResources() {

			shared_fonts = new Font[(int)FontTypes.LAST,8];
			shared_fonts[(int)FontTypes.ROLE_NAME,0] = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			shared_fonts[(int)FontTypes.DEFAULT,0] = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));

			shared_formats = new StringFormat[(int)FormatTypes.LAST];
			shared_formats[(int)FormatTypes.CENTER] = new StringFormat();
			shared_formats[(int)FormatTypes.CENTER].Alignment = shared_formats[(int)FormatTypes.CENTER].LineAlignment = StringAlignment.Center;
		}

		private void CleanupResources() {
			for( int i = 0; i < (int)FontTypes.LAST; i++ )
				for( int e = 0; e < 8; e++ )
					if( shared_fonts[i,e] != null )
						shared_fonts[i,e].Dispose();
	
			foreach( StringFormat s in shared_formats ) {
				s.Dispose();
			}
		}

		#endregion

		public new void Dispose() {
			CleanupResources();
		}

		protected override void OnSizeChanged(EventArgs e) {
			base.OnSizeChanged (e);
			int oldwidth = DiagramArea.Width + right_margin + left_margin, oldheight = DiagramArea.Height + bottom_margin + top_margin;

			if( oldwidth < this.Width )
				Invalidate( new Rectangle( DiagramArea.Right, ClientRectangle.Top, right_margin, ClientRectangle.Height ) );
			else if( oldwidth > this.Width )
				Invalidate( new Rectangle( ClientRectangle.Right-right_margin, ClientRectangle.Top, right_margin, ClientRectangle.Height ) );

			if( oldheight < this.Height )
				Invalidate( new Rectangle( ClientRectangle.Left, DiagramArea.Bottom, ClientRectangle.Width, bottom_margin ) );
			else if( oldheight > this.Height )
				Invalidate( new Rectangle( ClientRectangle.Left, ClientRectangle.Bottom-bottom_margin, ClientRectangle.Width, bottom_margin  ) );

			DiagramArea = new Rectangle( ClientRectangle.X + left_margin, ClientRectangle.Y + top_margin, 
				ClientRectangle.Width - right_margin - left_margin, ClientRectangle.Height - bottom_margin - top_margin );
		}

		public const int gridsize = 24;

		public const int x_pages = 3, y_pages = 3;
		public int x_res, y_res;

		public int scaleview = 1, scaledoc = 1;
		public bool zoomon = false;

		private void DrawPages( Graphics g, Rectangle clip, int ox, int oy ) {

			int bound_x = -ox + clip.X, bound_y = -oy + clip.Y;
			int x, y;

			// fill WHITE

#if DEBUG_INVALIDATE
			g.FillRectangle( Brushes.Black, Rectangle.Intersect( clip, new Rectangle(bound_x,bound_y,x_pages*x_res,y_pages*y_res) ) );
			System.Threading.Thread.Sleep( 200 );
#endif
			g.FillRectangle( Brushes.White, Rectangle.Intersect( clip, new Rectangle(bound_x,bound_y,x_pages*x_res,y_pages*y_res) ) );

			// fill the rest with gray

			if( clip.Top < bound_y )
				g.FillRectangle( Brushes.LightGray, clip.Left, clip.Top, clip.Width, bound_y - clip.Top -1 );

			if( clip.Bottom > bound_y + y_pages*y_res )
				g.FillRectangle( Brushes.LightGray, clip.Left, bound_y + y_pages*y_res, clip.Width, clip.Bottom - bound_y - y_pages*y_res );

			if( clip.Left < bound_x )
				g.FillRectangle( Brushes.LightGray, clip.Left, clip.Top, bound_x - clip.Left - 1, clip.Height );

			if( clip.Right > bound_x + x_pages*x_res )
				g.FillRectangle( Brushes.LightGray, bound_x + x_pages*x_res, clip.Top, clip.Right - bound_x - x_pages*x_res, clip.Bottom );

			// Draw Page lines

			for( x = bound_x-1; x <= bound_x + x_pages * x_res; x += x_res ) {
				if( x >= clip.Left && x <= clip.Right )
					g.DrawLine( Pens.DarkGray, x, bound_y-1, x, bound_y + y_pages*y_res - 1 );
			}

			for( y = bound_y-1; y <= bound_y + y_pages * y_res; y += y_res ) {
				if( y >= clip.Top && y <= clip.Bottom )
					g.DrawLine( Pens.DarkGray, bound_x-1, y, bound_x + x_pages*x_res - 1, y );
			}  

			// Draw Page numbers

			/* TODO
			
			Rectangle re = new Rectangle();
			for( x = 1; x <= x_pages; x++ )
				for( y = 1; y <= y_pages; y++ ) {

					string s = y.ToString() + "-" + x.ToString();
					SizeF sz = g.MeasureString( s, Font );
					
					re.X = x*x_res + bound_x - (int)sz.Width - 1;
					re.Y = y*y_res + bound_y - (int)sz.Height - 1;
					re.Width = (int)sz.Width;
					re.Height = (int)sz.Height;

					if( clip.IntersectsWith( re ) ) {
						g.DrawString( s, Font, Brushes.Black, re.X, re.Y );
					}
				}*/

			// TODO
			//int dx = (gridsize - offx)%gridsize, dy = (gridsize - offy)%gridsize;

			/*for( i = (clip.Left - dx + gridsize - 1) / gridsize; i <= ((clip.Right - dx) / gridsize); i++ )
				g.DrawLine( Pens.LightGray, i*gridsize + dx, clip.Top, i*gridsize + dx, clip.Bottom );
			for( i = (clip.Top - dy + gridsize - 1) / gridsize; i <= ((clip.Bottom - dy) / gridsize); i++ )
				g.DrawLine( Pens.LightGray, clip.Left, i*gridsize + dy, clip.Right, i*gridsize + dy );*/

		}

		private int x_to_doc( int x ) {
			return (int) ((x - left_margin)*(float)scaledoc / scaleview ) + offx;
		}

		private int y_to_doc( int y ) {
			return (int) ((y - top_margin)*(float)scaledoc / scaleview ) + offy;
		}

		protected override void OnPaint(PaintEventArgs e) {
			Graphics g = e.Graphics;
			Rectangle r, clip = e.ClipRectangle;

			if( !zoomon ) {

				// shift to (left_margin,top_margin)
				g.TranslateTransform( left_margin, top_margin );
				r = clip;
				r.X -= left_margin;
				r.Y -= top_margin;

				DrawPages( g, r, offx + r.X, offy + r.Y );
				if( curr != null )
					curr.Paint( g, r, offx + r.X, offy + r.Y );
			} else {

				// rc is extended clip area, transformed to document coords
				int x = (clip.X-left_margin) / scaleview * scaledoc, y = (clip.Y-top_margin) / scaleview * scaledoc;
				r = new Rectangle( x, y, ( clip.Right - left_margin + scaleview - 1 ) / scaleview * scaledoc - x, ( clip.Bottom - top_margin + scaleview - 1 ) / scaleview * scaledoc - y );

				// compute scaling, turn it on
				float scale = (float) scaleview / scaledoc;
				g.ScaleTransform( scale, scale );
				g.TranslateTransform( left_margin, top_margin, MatrixOrder.Append );

				DrawPages( g, r, offx + r.X, offy + r.Y );
				if( curr != null )
					curr.Paint( g, r, offx + r.X, offy + r.Y );
			}
			g.Transform = new Matrix();
			r = ClientRectangle;
			g.DrawLine( Pens.Gray, r.Left, r.Bottom-1, r.Left, r.Top );
			g.DrawLine( Pens.Gray, r.Left, r.Top, r.Right-1, r.Top );
			g.DrawLine( Pens.DarkGray, r.Left, r.Bottom-1, r.Right-1, r.Bottom-1 );
			g.DrawLine( Pens.DarkGray, r.Right-1, r.Bottom-1, r.Right-1, r.Top );
		}

		public Point point_to_screen( int x, int y ) {
			int nx = (x-offx) / scaledoc * scaleview, ny = (y-offy) / scaledoc * scaleview;
			return new Point( nx, ny );
		}

		public void InvalidatePage( Rectangle r ) {

			if( curr == null ) return;
			if( zoomon ) {
				int x = (r.X-offx) / scaledoc * scaleview, y = (r.Y-offy) / scaledoc * scaleview;
				r = Rectangle.Intersect( DiagramArea, new Rectangle( left_margin + x, top_margin + y, ( r.Right - offx + scaledoc - 1 ) / scaledoc * scaleview - x + 1, ( r.Bottom - offy + scaledoc - 1 ) / scaledoc * scaleview - y + 1 ) );
			} else {
				r = Rectangle.Intersect( DiagramArea, new Rectangle( left_margin + r.X - offx, top_margin + r.Y - offy, r.Width, r.Height ) );
			}
			if( r != Rectangle.Empty )
				this.Invalidate( r );
		}


		public void InvalidateRegion( Region r ) {
			if( !zoomon ) {
				r.Translate( left_margin - offx, top_margin - offy );
                r.Intersect( DiagramArea );
				Invalidate( r );

			} else {
				Invalidate();

			}
		}

		public void AdjustPageCoords( int dx, int dy ) {
			if( dx != 0 || dy != 0 ) {
				offx += dx;
				offy += dy;
				Invalidate( DiagramArea );
			}
		}

		public Rectangle PageRectangle {			
			get {
				if( zoomon ) {
					int x = DiagramArea.Left / scaleview * scaledoc, y = DiagramArea.Top / scaleview * scaledoc;
					return new Rectangle( offx, offy, 
						(DiagramArea.Right + scaleview - 1) / scaleview * scaledoc - x,
						(DiagramArea.Bottom + scaleview - 1) / scaleview * scaledoc - y );
				} else
					return new Rectangle( offx, offy, DiagramArea.Width, DiagramArea.Height );			
			}
		}

		public void SetupScale( int up, int down ) {
			scaledoc = down;
			scaleview = up;
			zoomon = down != up;
			Invalidate();
		}

		#region Drag & Drop

		// Drag & drop routines, forward the call to ObjectDropper

		protected override void OnDragDrop(System.Windows.Forms.DragEventArgs drgevent) {
			if( curr == null )
				return;
			curr.mouseagent.Drop();
		}

		protected override void OnDragEnter(DragEventArgs drgevent) {
			if( curr == null )
				return;
			curr.mouseagent.StartDrag( (UmlObject)DragObject );
		}

		protected override void OnDragLeave(EventArgs e) {
			if( curr == null )
				return;
			curr.mouseagent.StopDrag();
		}
		
		protected override void OnDragOver(DragEventArgs drgevent) {
			if( curr == null )
				return;
			drgevent.Effect = DragDropEffects.Copy;
			Point pt = PointToClient(new Point(drgevent.X,drgevent.Y));
			curr.mouseagent.Drag( x_to_doc(pt.X), y_to_doc(pt.Y) );
		}

		#endregion

		#region Mouse moving routines

		protected override void OnMouseWheel(MouseEventArgs e) {
			if( ( Control.ModifierKeys & Keys.Shift ) == Keys.Shift ) { 
				AdjustPageCoords( -e.Delta, 0 );
			} else {
				AdjustPageCoords( 0, -e.Delta );
			}
		}


		protected override void OnMouseDown(MouseEventArgs e) {
			if( curr == null )
				return;
			Focus();
			curr.mouseagent.MouseDown( x_to_doc(e.X), y_to_doc(e.Y), e.Button, Control.ModifierKeys, e.X, e.Y );
			base.OnMouseDown( e );
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			if( curr == null )
				return;
			curr.mouseagent.MouseMove( x_to_doc(e.X), y_to_doc(e.Y), e.Button );
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			if( curr == null )
				return;
			curr.mouseagent.MouseUp( e.Button );
		}

		protected override void OnMouseLeave(EventArgs e) {
			if( curr == null )
				return;
		}

		#endregion

		#region Print

		public void Print( bool preview ) {

			if( curr == null )
				return;

			curr.DoOperation( GUI.View.EditOperation.SelectNone );

			PrintDocument pd = new PrintDocument();
			pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
			pd.DefaultPageSettings.Landscape = true;
			pd.DocumentName = curr.name;
			pd.DefaultPageSettings.Margins = new Margins( 25,25,25,25 );
			pages_printed = 0;
			pages_to_print = 9;

			PrintDialog d = new PrintDialog();
			d.Document = pd;
			DialogResult res = d.ShowDialog();

			if( res != DialogResult.OK )
				return;

			pd.PrinterSettings = d.PrinterSettings;

			if( preview ) {
				PrintPreviewDialog ppd = new PrintPreviewDialog();
				ppd.Document = pd;
				ppd.ShowDialog();
			} else {
				pd.Print();
			}

			pd.Dispose();
		}

		private int pages_printed, pages_to_print;

		private void pd_PrintPage(object sender, PrintPageEventArgs e) {
			Rectangle clip = e.MarginBounds;//e.PageBounds
			if( pages_printed == 0 ) {
				this.x_res = clip.Width;
				this.y_res = clip.Height;
			}
			int ox = (pages_printed%3)*x_res, oy = (pages_printed/3)*y_res;
			e.Graphics.SetClip(clip);
			curr.Paint( e.Graphics, clip, ox, oy );
			e.HasMorePages = ++pages_printed < pages_to_print;
		}

		#endregion
	}
}
