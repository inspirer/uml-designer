using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Windows.Forms;

namespace UMLDes.Controls {

	public delegate void MouseClickEvent( int index );

	public enum FlatButtonType {
        Simple, Line, Control, Radio, RadioDown
	}

	public enum FlatButtonState {
		Basic, Hover, Clicked
	}

	/// <summary>
	/// Describes one element on the panel
	/// </summary>
	public class FlatToolBarButton {
		public bool disabled;
		public int image_index, width;
		public FlatButtonType style;
		public FlatButtonState state;
		public MouseClickEvent ev;
		public FlatToolBarPanel parent;
		public string title;
		Control cb;

		public void Paint( Graphics g, Rectangle r, ImageList list, Color bg ) {

			if( style == FlatButtonType.Control ) {
				return;
			}

			if( style == FlatButtonType.Line )
				g.DrawLine( Pens.DarkGray, r.X + 2, r.Top, r.X + 2, r.Bottom - 1 );
			else {

				if( style == FlatButtonType.RadioDown ) {
					g.FillRectangle( Brushes.Lavender, r.X, r.Y, r.Width - 1, r.Height - 1 );
					bg = Color.Lavender;
				} else if( state != FlatButtonState.Basic ) {
					g.FillRectangle( state == FlatButtonState.Hover ? Brushes.LightSteelBlue : Brushes.SteelBlue, r.X, r.Y, r.Width - 1, r.Height - 1 ); 
					bg = state == FlatButtonState.Hover ? Color.LightSteelBlue : Color.SteelBlue;
				}

				if( style == FlatButtonType.RadioDown || state != FlatButtonState.Basic )
					g.DrawRectangle( Pens.DarkBlue, r.Left, r.Top, r.Width - 1, r.Height - 1 );
				
				int hover = state == FlatButtonState.Hover ? 1 : 0;
				if( disabled )
					ControlPaint.DrawImageDisabled( g, list.Images[image_index], r.X + 3 - hover, r.Y + 3 - hover, bg );
				else
                    list.Draw( g, r.X + 3 - hover, r.Y + 3 - hover, 16, 16, image_index );
			}
		}

		public FlatToolBarButton( FlatButtonType t, int index, MouseClickEvent e, string title, FlatToolBarPanel p ) {
			style = t;
			this.title = title;
			ev = e;
			state = FlatButtonState.Basic;
			image_index = index;
			parent = p;
			switch( t ) {
				case FlatButtonType.Control: 
					throw new ArgumentException( "wrong type: control" );
				case FlatButtonType.Line: width = FlatToolBarPanel.LineWidth; break;
				case FlatButtonType.Radio: case FlatButtonType.RadioDown: case FlatButtonType.Simple:
					width = 23;break;
			}

		}

		public FlatToolBarButton( Control c, FlatToolBarPanel p ) {
			style = FlatButtonType.Control;
			cb = c;
			state = FlatButtonState.Basic;
			parent = p;
			parent.parent.Controls.Add( cb );
			width = c.Width + 4;
			c.Height = 18;
		}

		public void ChangedPosition( int x, int y ) {
			if( style == FlatButtonType.Control )
				cb.Location = new Point( x+2, y + 2 );           
		}

	}

	/// <summary>
	/// Panel is the part of toolbar, which can contain buttons
	/// </summary>
	public class FlatToolBarPanel : IComparable {

		public string Name;
		public Rectangle place;
		public FlatToolBar parent;
		ArrayList buttons;

		public const int LeftMargin = 10, RightMargin = 3, LineWidth = 6;

		public FlatToolBarPanel( int x, int y, string name, FlatToolBar p ) {
			parent = p;
			Name = name;
			buttons = new ArrayList();
			place.Height = FlatToolBar.BarPanelHeight;
			place.Width = LeftMargin + RightMargin;
			place.Location = new Point( x, y );
		}

		public FlatToolBarButton AddButton( FlatButtonType t, int imageindex, string title, MouseClickEvent ev ) {
			FlatToolBarButton b = new FlatToolBarButton( t, imageindex, ev, title, this );
			buttons.Add( b );
			ChangedPosition();
			parent.Invalidate();
			return b;
		}

		public FlatToolBarButton AddControl( Control c ) {
			FlatToolBarButton b = new FlatToolBarButton( c, this );
			buttons.Add( b );
			ChangedPosition();
			parent.Invalidate();
			return b;
		}

		public void ChangedPosition() {
			int width = LeftMargin + RightMargin;
			int X = parent.ClientRectangle.X + place.Left + LeftMargin, Y = parent.ClientRectangle.Y + place.Top;
			foreach( FlatToolBarButton b in buttons ) {
				b.ChangedPosition( X, Y );
				X += b.width;
				width += b.width;
			}
			place.Width = width;
		}

		#region Painting

		public void Paint( Graphics g, ImageList l, Rectangle parent ) {
			g.FillRectangle( this.parent.back, place );
			int X = parent.X + place.X, Y = parent.Y + place.Y;

			for( int i = 4; i < 20; i += 2 )
				g.DrawLine( Pens.DarkGray, X + 2, Y + i, X + 4, Y + i );

			X += LeftMargin;

			foreach( FlatToolBarButton b in buttons ){
				b.Paint( g, new Rectangle( X, Y + 1, b.width, 22), l, this.parent.BackColor );
				X += b.width;
			}
		}

		public void InvalidateButton( FlatToolBarButton b ) {
			int X = parent.ClientRectangle.X + place.X + LeftMargin, Y = parent.ClientRectangle.Y + place.Y;

			foreach( FlatToolBarButton p in buttons ) {
				if( p == b ) {
					parent.Invalidate( new Rectangle( X, Y + 1, b.width, 22) );
					return;
				}
				X += p.width;
			}
		}

		#endregion

		#region MouseBehavior

		FlatToolBarButton highlighted;

		public bool MouseMove( int x, int y ) {
			int X;
			X = LeftMargin;
			foreach( FlatToolBarButton b in buttons )
				if( b.style != FlatButtonType.Line && !b.disabled ) {
					if( x >= X && x <= X + b.width && y >= 1 && y <= 22 ) {
						if( highlighted != b ) {
							RemoveHighlight();
							highlighted = b;
							b.state = FlatButtonState.Hover;
							InvalidateButton( b );
							parent.SetCurrentTip( b.title );
						}
						return true;
					}
					X += b.width;
				} else if( b.style != FlatButtonType.Line )
					X += b.width;
				else
					X += LineWidth;

			return false;
		}

		public void RemoveHighlight() {
			if( highlighted != null ) {
				highlighted.state = FlatButtonState.Basic;
				InvalidateButton( highlighted );
			}
			highlighted = null;
		}

		public void MouseDown() {
			if( highlighted != null ) {
				highlighted.state = FlatButtonState.Clicked;
				InvalidateButton( highlighted );
			}
		}

		public void MakeRadioDown( FlatToolBarButton bb ) {
			foreach( FlatToolBarButton b in buttons )
				if( b.style == FlatButtonType.RadioDown ) {
					b.style = FlatButtonType.Radio;
					InvalidateButton( b );
				}
			bb.style = FlatButtonType.RadioDown;
			InvalidateButton( bb );

		}

		public void MouseUp() {
			if( highlighted != null ) {
				if( highlighted.state == FlatButtonState.Clicked ) {
					if( highlighted.style == FlatButtonType.Radio )
						MakeRadioDown( highlighted );

					if( highlighted.ev != null )
						highlighted.ev( highlighted.image_index );
				}
				if( highlighted != null ) {
					highlighted.state = FlatButtonState.Hover;
					InvalidateButton( highlighted );
				}
			}
		}

		#endregion

		#region Comparable
		public int CompareTo( object obj ) {
			if( obj is FlatToolBarPanel ) {
				FlatToolBarPanel t = (FlatToolBarPanel) obj;
				return place.X.CompareTo(t.place.X);
			}
        
			throw new ArgumentException("wrong object in compare");
		}
		#endregion
	}

	/// <summary>
	/// ToolBar with flat buttons
	/// </summary>
	public class FlatToolBar : System.Windows.Forms.UserControl, IDisposable {

		ArrayList panels;
		ImageList list1;
		public Brush back;
		ToolTip tt = new ToolTip();

		public const int BarPanelHeight = 24;

		public ImageList images {
			get { return list1; }
			set { list1 = value; }
		}	

		void FixupHeightAndRows( FlatToolBarPanel pp, int prev_y ) {

			bool need_validation = false;

			// fix the height of the Toolbar control
			if( pp.place.Bottom > this.Height ) {
				this.Height = pp.place.Bottom;
				need_validation = true;
			}
			int height = BarPanelHeight;
			foreach( FlatToolBarPanel p in panels )
				if( p.place.Bottom > height )
					height = p.place.Bottom;
			if( height < this.Height ) {
				this.Height = height;
				need_validation = true;
			}

			FlatToolBarPanel[] arr = (FlatToolBarPanel[])panels.ToArray(typeof(FlatToolBarPanel));
			Array.Sort( arr );

			int x = 0;

			// fixup Panels in the row of pp
			foreach( FlatToolBarPanel p in arr )
				if( p.place.Y == pp.place.Y ) {
					if( p.place.X != x ) {
						need_validation = true;
						p.place.X = x;
						p.ChangedPosition();
					}
					x = p.place.Right + 4;
				}

			if( prev_y != -1 ) {
				x = 0;
				foreach( FlatToolBarPanel p in arr )
					if( p.place.Y == prev_y ) {
						if( p.place.X != x ) {
							need_validation = true;
							p.place.X = x;
							p.ChangedPosition();
						}
						x = p.place.Right + 4;
					} 
			}

			if( need_validation )
				Invalidate();
		}

		public FlatToolBarPanel AddPanel( int row, string name ) {

			FlatToolBarPanel p = new FlatToolBarPanel( 100000, row  * (BarPanelHeight + 2), name, this );
			panels.Add( p );
			FixupHeightAndRows( p, -1 );
			p.ChangedPosition();
			return p;
		}

		public void RemovePanel( FlatToolBarPanel p ) {

			if( highlighted != null )
				highlighted.RemoveHighlight();
			highlighted = null;

			if( panels.IndexOf( p ) != -1 ) {
				panels.Remove( p );
				FixupHeightAndRows( p, -1 );
			}
		}

		public FlatToolBar() {
			panels = new ArrayList();
			this.BackColor = Color.FromArgb( 219, 216, 209 );
			back = new SolidBrush( BackColor );
			SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			SetStyle( ControlStyles.DoubleBuffer, true );
			tt.Active = false;
		}

		public void SetCurrentTip( string s ) {
			tt.SetToolTip( this, s );
			tt.Active = true;
		}

		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e) {
			Graphics g = e.Graphics;

			g.FillRectangle( SystemBrushes.Control, e.ClipRectangle );
			foreach( FlatToolBarPanel p in panels ) {
				p.Paint( g, list1, ClientRectangle );
			}

			base.OnPaint( e );
		}

		#region Mouse

		bool moving = false, ready_to_move = false;
		int move_x, move_y;
		FlatToolBarPanel move_panel, highlighted;

		protected override void OnMouseMove(MouseEventArgs e) {
			int x = e.X - ClientRectangle.X, y = e.Y - ClientRectangle.Y;

			if( !moving ) {
				foreach( FlatToolBarPanel p in panels )
					if( p.place.Contains( x, y ) ) {
						if( !p.MouseMove(x-p.place.X,y-p.place.Y ) ) {
							Cursor.Current = Cursors.SizeAll;
							move_x = x - p.place.X;
							move_y = y - p.place.Y;
							ready_to_move = true;
							move_panel = p;

							if( highlighted != null )
								highlighted.RemoveHighlight();
							highlighted = null;
							tt.Active = false;
							return;
						} else {
							if( highlighted != p && highlighted != null )
								highlighted.RemoveHighlight();
							highlighted = p;
						}
					}
				ready_to_move = false;
				Cursor.Current = Cursors.Arrow;
			} else {
				int old_y = move_panel.place.Y;
				move_panel.place.Y = ( y + 3 ) / (BarPanelHeight+2) * (BarPanelHeight+2);
				if( move_panel.place.Y < 0 )
					move_panel.place.Y = 0;
				move_panel.place.X = x - move_x;

				FixupHeightAndRows( move_panel, old_y );
				move_panel.ChangedPosition();
			}
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			if( e.Button == MouseButtons.Left ) {
				if( ready_to_move ) {
					moving = true;
				} else if( highlighted != null ) {
					highlighted.MouseDown();
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			if( e.Button == MouseButtons.Left ) {
				if( moving ) {
					moving = false;
					ready_to_move = false;
				} else if( highlighted != null ) {
					highlighted.MouseUp();
				}
			}
		}

		protected override void OnMouseLeave(EventArgs e) {
			if( highlighted != null )
				highlighted.RemoveHighlight();
			highlighted = null;
			tt.Active = false;
		}


		#endregion

		#region IDisposable Members

		public new void Dispose() {
			if( back != null )
				back.Dispose();
			back = null;
			if( tt != null )
				tt.Dispose();
			base.Dispose();
		}

		#endregion
	}
}
	