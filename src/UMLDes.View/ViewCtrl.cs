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

	#region Common resource IDs

	public enum FontTypes : int {
		DEFAULT,
		ROLE_NAME,
		LAST
	}

	public enum FormatTypes : int {
		CENTER,
		LAST
	}

	#endregion

	/// <summary>
	/// UI Control which contains StaticView, hides scrolling
	/// </summary>
	public class ViewCtrl : System.Windows.Forms.UserControl, IDisposable {

		GUI.View curr;
		public object DragObject;

		const int scroller = 15, left_margin = 1, top_margin = 1, right_margin = scroller+1, bottom_margin = scroller+1;
		Rectangle DiagramArea, VScrollRect, HScrollRect, PanelScroll;
		int offx, offy;

		#region Constructor

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

		#endregion

		#region Current GUI.View to show

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

		#endregion

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

		public new void Dispose() {
			CleanupResources();
		}

		#endregion

		#region Size change, refresh rectangles

		protected override void OnSizeChanged(EventArgs e) {
			base.OnSizeChanged (e);
			int oldwidth = DiagramArea.Width + right_margin + left_margin, oldheight = DiagramArea.Height + bottom_margin + top_margin;

			if( oldwidth < this.Width )
				Invalidate( new Rectangle( DiagramArea.Right, ClientRectangle.Top, ClientRectangle.Right-DiagramArea.Right, ClientRectangle.Height ) );
			else
				Invalidate( new Rectangle( ClientRectangle.Right-right_margin, ClientRectangle.Top, right_margin, ClientRectangle.Height ) );

			if( oldheight < this.Height )
				Invalidate( new Rectangle( ClientRectangle.Left, DiagramArea.Bottom, ClientRectangle.Width, ClientRectangle.Bottom - DiagramArea.Bottom ) );
			else
				Invalidate( new Rectangle( ClientRectangle.Left, ClientRectangle.Bottom-bottom_margin, ClientRectangle.Width, bottom_margin  ) );

			DiagramArea = new Rectangle( ClientRectangle.X + left_margin, ClientRectangle.Y + top_margin, 
				ClientRectangle.Width - right_margin - left_margin, ClientRectangle.Height - bottom_margin - top_margin );

			PanelScroll = new Rectangle( ClientRectangle.Right - right_margin, ClientRectangle.Bottom - bottom_margin, right_margin - 1, bottom_margin - 1 );
			VScrollRect = new Rectangle( ClientRectangle.Right - right_margin, ClientRectangle.Y + top_margin, right_margin - 1, ClientRectangle.Height - bottom_margin - top_margin);
			HScrollRect = new Rectangle( ClientRectangle.X + left_margin, ClientRectangle.Bottom - bottom_margin, ClientRectangle.Width - right_margin - left_margin, bottom_margin - 1);
		}

		#endregion

		#region Paint

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

			// Draw scroller
			Rectangle page_rect = PageRectangle;
			DrawScroller( g, true, page_rect );
			DrawScroller( g, false, page_rect );
			g.FillRectangle( Brushes.LightGray, PanelScroll );

			// Draw external frame
			g.DrawLine( Pens.Gray, r.Left, r.Bottom-1, r.Left, r.Top );
			g.DrawLine( Pens.Gray, r.Left, r.Top, r.Right-1, r.Top );
			g.DrawLine( Pens.DarkGray, r.Left, r.Bottom-1, r.Right-1, r.Bottom-1 );
			g.DrawLine( Pens.DarkGray, r.Right-1, r.Bottom-1, r.Right-1, r.Top );
		}

		#endregion

		#region Scroller

		private int ScrollMax( bool vertical ) {
			return vertical ? y_pages*y_res : x_pages*x_res;
		}

		private int ScrollLeft( bool vertical, Rectangle page_rect ) {
			return vertical ? page_rect.Top : page_rect.Left;
		}

		private int ScrollRight( bool vertical, Rectangle page_rect ) {
			return vertical ? page_rect.Bottom : page_rect.Right;
		}

		private void SetScrollLeft( bool vertical, int val, int max ) {
			int new_pos = (int) ( (double)val * (ScrollMax( vertical )-(vertical ? PageRectangle.Height : PageRectangle.Width)) / max );

			if( vertical )
				AdjustPageCoords( 0, new_pos - offy );
			else
				AdjustPageCoords( new_pos - offx, 0 );
		}

		bool[] pressed = new bool[] { false, false, false, false };

		private void DrawScrollerButton( Graphics g, int x, int y, int direction ) {
			using( Brush b = new SolidBrush( Color.FromArgb( 237, 234, 229 ) ) )
				g.FillRectangle( b, x+1, y+1, scroller-2, scroller-2 );
			g.DrawRectangle( Pens.DarkGray, x, y, scroller-1, scroller-1 );
			int cx = x + scroller/2, cy = y + scroller/2;
			Brush br = pressed[direction] ? Brushes.White : Brushes.Black;

			switch( direction ) {
				case 0: // up
					g.FillPolygon( br, new Point[] { new Point( cx, cy-3), new Point( cx-4, cy+2), new Point( cx+4, cy+2) } );
					break;
				case 1: // right
					g.FillPolygon( br, new Point[] { new Point( cx+3, cy), new Point( cx-1, cy+4), new Point( cx-1, cy-4) } );
					break;
				case 2: // down
					g.FillPolygon( br, new Point[] { new Point( cx+4, cy-1), new Point( cx, cy+3), new Point( cx-3, cy-1) } );
					break;
				case 3: // left
					g.FillPolygon( br, new Point[] { new Point( cx-2, cy), new Point( cx+2, cy+4), new Point( cx+2, cy-4) } );
					break;
			}

		}

		private Rectangle GetScrollRectangle( bool vertical, int max, int pos_start, int pos_end ) {
			Rectangle scroll_rect = vertical ? VScrollRect : HScrollRect;
			int length = (vertical ? scroll_rect.Height : scroll_rect.Width ) - 2*scroller;

			// fix wrong values
			if( pos_start < 0 ) pos_start = 0;
			if( pos_end > max ) pos_end = max;

			pos_start = (int)((double)pos_start * length / max);
			pos_end = (int)((double)pos_end * length / max);

			if( pos_end - pos_start < 5 )
				pos_end = pos_start+5;
			if( pos_end > length ) {
				pos_start = length - 5;
				pos_end = length;
			}

			if( vertical ) {
				scroll_rect.Y += pos_start + scroller;
				scroll_rect.Height = pos_end - pos_start;
			} else {
				scroll_rect.X += pos_start + scroller;
				scroll_rect.Width = pos_end - pos_start;
			}
			return scroll_rect;
		}

		bool[] bar_selected = new bool[] { false, false };

		private void DrawScroller( Graphics g, bool vertical, Rectangle page_rect ) {
			Rectangle r = vertical ? VScrollRect : HScrollRect;
			using( Brush b = new LinearGradientBrush( Rectangle.Inflate(r,1,1), Color.FromArgb( 227, 224, 219 ), Color.FromArgb( 247, 244, 239 ), vertical ? 0f : 90f ) )
				g.FillRectangle( b, r );

			// check for space
			System.Diagnostics.Debug.Assert( vertical && r.Width == scroller || !vertical && r.Height == scroller );
			if( vertical && r.Height < 3*scroller || !vertical && r.Width < 3*scroller ) {
				return;
			}

			int max = ScrollMax( vertical ), pos_start = ScrollLeft( vertical, page_rect ), pos_end = ScrollRight( vertical, page_rect );

			DrawScrollerButton( g, r.X, r.Y, vertical ? 0 : 3 );
			DrawScrollerButton( g, r.Right-scroller, r.Bottom - scroller, vertical ? 2 : 1 );

			Rectangle scroll_rect = GetScrollRectangle( vertical, max, pos_start, pos_end );
			using( Brush b = new SolidBrush( Color.FromArgb( 227, 224, 219 ) ) )
				g.FillRectangle( b, scroll_rect.X+1, scroll_rect.Y+1, scroll_rect.Width-2, scroll_rect.Height-2 );

			Pen top = Pens.DarkGray, bottom = Pens.Gray;

			if( bar_selected[vertical?0:1] ) {
				top = Pens.Gray;
				bottom = Pens.DarkGray;
			}

			g.DrawLine( top, scroll_rect.X, scroll_rect.Y, scroll_rect.X, scroll_rect.Bottom-1 );
			g.DrawLine( top, scroll_rect.X, scroll_rect.Y, scroll_rect.Right-1, scroll_rect.Y );
			g.DrawLine( bottom, scroll_rect.Right-1, scroll_rect.Y, scroll_rect.Right-1, scroll_rect.Bottom-1 );
			g.DrawLine( bottom, scroll_rect.X, scroll_rect.Bottom-1, scroll_rect.Right-1, scroll_rect.Bottom-1 );
		}

		private enum MouseAction { MouseMove, LeftButtonDown, LeftButtonUp };

		bool scroll_on = false, scroll_vert, scroll_bar;
		int scroll_pos, scroll_length;

		void ScrollMouseAction( bool vertical, int x, int y, MouseAction ma ) {
			if( scroll_on && ma != MouseAction.LeftButtonDown )
				vertical = scroll_vert;

			Rectangle r = vertical ? VScrollRect : HScrollRect;
			if( !r.Contains( x, y ) && ma == MouseAction.LeftButtonDown || ma != MouseAction.LeftButtonDown && !scroll_on )
				return;

			// check for space
			System.Diagnostics.Debug.Assert( vertical && r.Width == scroller || !vertical && r.Height == scroller );
			if( vertical && r.Height < 3*scroller || !vertical && r.Width < 3*scroller ) {
				return;
			}

			int left = vertical ? r.Top : r.Left, right = vertical ? r.Bottom : r.Right, mouse_pos = vertical ? y : x;

			switch( ma ) {
				case MouseAction.MouseMove:
					if( scroll_on && scroll_bar ) {

						int target = mouse_pos - scroll_pos;
						if( target < left + scroller ) target = left + scroller;
						if( target >= right - scroller - scroll_length ) target = right - scroller - scroll_length;
						SetScrollLeft( scroll_vert, target - (left + scroller), right - left - 2*scroller - scroll_length );

					}
					break;

				case MouseAction.LeftButtonDown:

					scroll_vert = vertical;
					if( mouse_pos >= left && mouse_pos < left + scroller ) {
						pressed[vertical?0:3] = true;
						AdjustPageCoords( vertical ? 0 : -20, vertical ? -20 : 0 );
						scroll_on = true;
						scroll_bar = false;
					} else if( mouse_pos < right && mouse_pos >= right - scroller ) {
						pressed[vertical?2:1] = true;
						AdjustPageCoords( vertical ? 0 : 20, vertical ? 20 : 0 );
						scroll_on = true;
						scroll_bar = false;
					} else {
						Rectangle page_rect = PageRectangle;
						Rectangle scroll_rect = GetScrollRectangle( vertical, ScrollMax( vertical ), ScrollLeft( vertical, page_rect), ScrollRight( vertical, page_rect ) );
						if( scroll_rect.Contains( x, y ) ) {
							scroll_pos = vertical ? y - scroll_rect.Top : x - scroll_rect.Left;
							scroll_length = vertical ? scroll_rect.Height : scroll_rect.Width;
							scroll_bar = scroll_on = true;
							bar_selected[vertical?0:1] = true;
							Invalidate(r);

						} else {
							if( mouse_pos < (vertical ? scroll_rect.Top : scroll_rect.Left ) ) {
								AdjustPageCoords( vertical ? 0 : -DiagramArea.Width/5, vertical ? -DiagramArea.Height/5 : 0 );
							} else if( mouse_pos >= (vertical ? scroll_rect.Bottom : scroll_rect.Right) ) {
								AdjustPageCoords( vertical ? 0 : DiagramArea.Width/5, vertical ? DiagramArea.Height/5 : 0 );
							}
						}
					}

					break;
				case MouseAction.LeftButtonUp:
					scroll_on = false;
					bar_selected[0] = bar_selected[1] = pressed[0] = pressed[1] = pressed[2] = pressed[3] = false;
					Invalidate( r );
					break;
			}
		}

		#endregion

		#region Coordinates transformation

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

		private int x_to_doc( int x ) {
			return (int) ((x - left_margin)*(float)scaledoc / scaleview ) + offx;
		}

		private int y_to_doc( int y ) {
			return (int) ((y - top_margin)*(float)scaledoc / scaleview ) + offy;
		}

		public Point point_to_screen( int x, int y ) {
			int nx = (x-offx) / scaledoc * scaleview, ny = (y-offy) / scaledoc * scaleview;
			return new Point( nx, ny );
		}

		#endregion

		#region Invalidate

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

		#endregion

		#region Adjust viewport parameters

		public void AdjustPageCoords( int dx, int dy ) {
			if( dx != 0 || dy != 0 ) {
				offx += dx;
				offy += dy;
				Invalidate();
			}
		}

		public void SetupScale( int up, int down ) {
			scaledoc = down;
			scaleview = up;
			zoomon = down != up;
			Invalidate();
		}

		public static int[] scalevalue = new int[] {
												3, 1,		// 300 %
												2, 1,		// 200 %
												5, 3,		// 166 %
												3, 2,		// 150 %
												4, 3,		// 133 %
												1, 1,		// 100 %
												9, 10,		// 90 %
												3, 4,		// 75 %
												2, 3,		// 66 %
												1, 2,		// 50 %
												1, 3,		// 33 %
												1, 4,		// 25 %
												1, 5,		// 20 %
		};

		public ComboBox scalecombo = null;

		public void ScaleChanged( object v, EventArgs e ) {
			if( scalecombo != null )
				SetupScale( scalevalue[scalecombo.SelectedIndex*2], scalevalue[scalecombo.SelectedIndex*2+1] );			
		}

		public void ZoomOut() {
			if( scalecombo != null && scalecombo.SelectedIndex < scalecombo.Items.Count-1 )
				scalecombo.SelectedIndex++;
		}

		public void ZoomIn() {
			if( scalecombo != null && scalecombo.SelectedIndex > 0 )
				scalecombo.SelectedIndex--;
		}

		#endregion

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

		bool hold = false, last_over;

		private void TryScrollbars( int x, int y, MouseAction ma ) {
			if( ma == MouseAction.MouseMove ) {
				if( hold )
					ScrollMouseAction( last_over, x, y, ma );
			} else if( hold && ma == MouseAction.LeftButtonUp ) {
				ScrollMouseAction( last_over, x, y, ma );
				hold = false;
			} else if( HScrollRect.Contains( x, y ) ) {
				ScrollMouseAction( last_over = false, x, y, ma );
				hold = (ma == MouseAction.LeftButtonDown);

			} else if( VScrollRect.Contains( x, y ) ) {
				ScrollMouseAction( last_over = true, x, y, ma );
				hold = (ma == MouseAction.LeftButtonDown);

			}
		}

		protected override void OnMouseWheel(MouseEventArgs e) {
			if( ( Control.ModifierKeys & Keys.Shift ) == Keys.Shift || HScrollRect.Contains( e.X, e.Y ) ) { 
				AdjustPageCoords( -e.Delta, 0 );
			} else {
				AdjustPageCoords( 0, -e.Delta );
			}
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			if( curr == null )
				return;
			Focus();
			if( DiagramArea.Contains( e.X, e.Y ) )
				curr.mouseagent.MouseDown( x_to_doc(e.X), y_to_doc(e.Y), e.Button, Control.ModifierKeys, e.X, e.Y );
			if( ( e.Button & MouseButtons.Left ) == MouseButtons.Left )
				TryScrollbars( e.X, e.Y, MouseAction.LeftButtonDown );
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			if( curr == null )
				return;
			curr.mouseagent.MouseUp( e.Button );
			if( ( e.Button & MouseButtons.Left ) == MouseButtons.Left )
				TryScrollbars( e.X, e.Y, MouseAction.LeftButtonUp );
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			if( curr == null )
				return;
			curr.mouseagent.MouseMove( x_to_doc(e.X), y_to_doc(e.Y), e.Button );
			TryScrollbars( e.X, e.Y, MouseAction.MouseMove );
		}

		protected override void OnMouseLeave(EventArgs e) {
			if( curr == null )
				return;
		}

		#endregion

		#region Print

		#region PageToPrint structure
		private struct PageToPrint {

			public int x, y;

			public PageToPrint( int x, int y ) {
				this.x = x;
				this.y = y;
			}
		}
		#endregion

		public ArrayList pages_to_print;

		private void fillPagesToPrint() {
			pages_to_print = new ArrayList();
			for( int i = 0; i < x_pages; i++ )
				for( int e = 0; e < y_pages; e++ ) {
					Rectangle r = new Rectangle( i*x_res, e*y_res, x_res, y_res );
					if( curr.IfContainsSmth( r ) )
						pages_to_print.Add( new PageToPrint( i*x_res, e*y_res ) );
				}
            if( pages_to_print.Count == 0 )
				pages_to_print.Add( new PageToPrint(0, 0) );
		}

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

		private int pages_printed;

		private void pd_PrintPage(object sender, PrintPageEventArgs e) {
			Rectangle clip = e.MarginBounds;//e.PageBounds
			if( pages_printed == 0 ) {
				this.x_res = clip.Width;
				this.y_res = clip.Height;
				fillPagesToPrint();
			}
			PageToPrint ptp = (PageToPrint)pages_to_print[pages_printed];
			e.Graphics.SetClip(clip);
			curr.Paint( e.Graphics, clip, ptp.x, ptp.y );
			e.HasMorePages = ++pages_printed < pages_to_print.Count;
			if( pages_printed == pages_to_print.Count )
				pages_printed = 0;
		}

		#endregion

		#region Save To Image

		public Bitmap PrintToImage() {
			curr.DoOperation( GUI.View.EditOperation.SelectNone );
			Rectangle rect = curr.GetContentRectangle();
			if( rect.IsEmpty )
				return null;
			Bitmap bmp = new Bitmap( rect.Width, rect.Height );
			using( Graphics gr = Graphics.FromImage( bmp ) ) {
				gr.FillRectangle( Brushes.White, 0, 0, rect.Width, rect.Height );
				curr.Paint( gr, new Rectangle( new Point(0,0), rect.Size ), rect.X, rect.Y );
			}
			return bmp;
		}

		#endregion

		#region Keyboard actions

		protected override bool IsInputKey(Keys keyData) {
			switch( keyData ) {
				case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down:
					return true;
			}
			return base.IsInputKey (keyData);
		}


		protected override void OnKeyDown(KeyEventArgs e) {
			base.OnKeyDown (e);

			switch( e.KeyCode ) {
				case Keys.Add:
					ZoomIn();
					break;
				case Keys.Subtract:
					ZoomOut();
					break;
				case Keys.NumPad4: case Keys.Left:
					AdjustPageCoords( -50, 0 );
					break;
				case Keys.NumPad6: case Keys.Right:
					AdjustPageCoords( 50, 0 );
					break;
				case Keys.NumPad8: case Keys.Up:
					AdjustPageCoords( 0, -50 );
					break;
				case Keys.NumPad2: case Keys.Down:
					AdjustPageCoords( 0, 50 );
					break;
			}
		}

		#endregion
	}
}
