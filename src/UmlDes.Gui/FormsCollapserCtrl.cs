using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Threading;

namespace CDS.Controls {
	/// <summary>
	/// Summary description for FormsCollapserCtrl.
	/// </summary>
	public class FormsCollapserCtrl : System.Windows.Forms.UserControl {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private ArrayList items;
		private StringFormat str_format;
		private Font font;
		private CollapsedItem current, pointed_item;
		private bool timer_active, watchdog_timer_active, mouse_triggered;
		private enum ControlPosition {
			POSITION_LEFT, 
			POSITION_RIGHT };
		private ControlPosition position;
		private BoundsWatcher bounds_watcher;
		private System.Threading.Timer timer;
		Rectangle mouse_rect;
		public CDS.ViewCtrl parent_control;

		const int X_OFFSET = 0;
		const int X_POSTGAP = 2;
		const int INITIAL_SPACING = 2;
		const int FINAL_SPACING = 8 - 2;
		const int ITEM_SPACING = 0;
		const int IMAGE_SIZE = 16;
		const int IMAGE_SPACING = 3;
		const int TEXT_SPACING = 7 - 2;
		const int ITEM_CEILING = 2;
		const int ITEM_POST_CEILING = 1;
		const int ITEM_WIDTH = IMAGE_SIZE + ITEM_CEILING + ITEM_POST_CEILING;
		const int CONTROL_SPACING = 2;
		const int SELECTED_WIDTH = 0;
		private System.Windows.Forms.ContextMenu contextMenu1;
		const int CONTROL_WIDTH = ITEM_WIDTH + X_OFFSET + SELECTED_WIDTH + X_POSTGAP + 1 /* 1: we must include the bounding line ! */;
		const int MOUSE_TIMEOUT = 500;	/* milliseconds  */
		const int WATCHDOG_TIMEOUT = 1000;	/* milliseconds */

		public class BoundsWatcher {
			private System.Threading.Timer watchdog_timer;
			private int watchdog_timeout;
			public event EventHandler watchdog_event;

			public BoundsWatcher( int timeout ) {
				watchdog_timer = new System.Threading.Timer( new TimerCallback( BoundsWatcher_TimerCallback ), null, Timeout.Infinite, Timeout.Infinite );
				watchdog_timeout = timeout;
			}
		
			public void StartWatching() {
				watchdog_timer.Change( watchdog_timeout, Timeout.Infinite );
			}

			public void StopWatching() {
				watchdog_timer.Change( Timeout.Infinite, Timeout.Infinite );
			}

			private void BoundsWatcher_TimerCallback( object sender ) {
				StopWatching();
				watchdog_event(this, EventArgs.Empty );
			}
		}

		private class CollapsedItem {
			public Rectangle rect;
			public String name;
			public Image image;
			public bool selected;
			private CollapsedPage child_page;

			public CollapsedPage page {
				get {
					return child_page;
				}
			}
			
			public CollapsedItem( Control c, String s, BoundsWatcher watcher ) {
				image = null;
				selected = false;
				name = s;
				child_page = new CollapsedPage( c, s, watcher );
				rect = Rectangle.Empty;
			}

		}		

		public class CollapsedPage : System.Windows.Forms.UserControl {
			public Control control;
			public FormsCollapserCtrl collapser_ctrl;
			private String title;
			private Font font;
			private SolidBrush active_text_brush, inactive_text_brush;
			private StringFormat str_format;
			private BoundsWatcher bounds_watcher;
			public bool focused;

			const int BORDER_WIDTH = 0;
			const int BACK_BORDER_WIDTH = 2;
			const int SYSTEM_AREA_HEIGHT = 15;
			const int PRE_BUTTON_SPACING = 10;
			const int POST_BUTTON_SPACING = 5;
			const int BUTTON_WIDTH = 8;
			const int BUTTON_SPACING = 4;
			const int NUM_OF_BUTTONS = 2;
			public const int MIN_PAGE_WIDTH = PRE_BUTTON_SPACING + NUM_OF_BUTTONS * BUTTON_WIDTH + 
				NUM_OF_BUTTONS-1	* BUTTON_SPACING + POST_BUTTON_SPACING;

			public CollapsedPage( Control c, String s, BoundsWatcher watcher ) {
				this.Visible = false;
				control = c;
				title = s;
				c.Visible = true;
				focused = false;
				c.Location = new Point( BORDER_WIDTH + 1, SYSTEM_AREA_HEIGHT + BORDER_WIDTH + 1 );
				this.Paint += new System.Windows.Forms.PaintEventHandler(this.CollapsedPage_Paint);
				this.SizeChanged += new EventHandler(CollapsedPage_SizeChanged);
				if( c.Width < MIN_PAGE_WIDTH ) {
					c.Width = MIN_PAGE_WIDTH;
				}
				this.Controls.Add( c );

				Size size = c.Size;
				c.Height -= 50;
				size.Height += SYSTEM_AREA_HEIGHT + 1;
				this.Size = size;
				this.Width += BACK_BORDER_WIDTH;

				font = new Font( "Tahoma", 8.0f );
				active_text_brush = new SolidBrush( Color.White );
				inactive_text_brush = new SolidBrush( Color.Black );
				str_format = new StringFormat();
				str_format.Alignment = StringAlignment.Near;
				str_format.LineAlignment = StringAlignment.Center;
				str_format.Trimming = StringTrimming.EllipsisCharacter;

				this.SetStyle( ControlStyles.DoubleBuffer, true );
				this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );

				this.Enter += new EventHandler(CollapsedPage_Enter);
				this.control.Enter += new EventHandler(CollapsedPage_Enter);
				this.Leave += new EventHandler(CollapsedPage_Leave);
				this.control.Leave += new EventHandler(CollapsedPage_Leave);
				this.MouseEnter += new EventHandler( CollapsedPage_MouseEnter );
				this.MouseLeave += new EventHandler( CollapsedPage_MouseLeave );
				this.control.MouseEnter += new EventHandler( CollapsedPage_MouseEnter );
				//this.control.MouseLeave += new EventHandler( CollapsedPage_MouseLeave );
                
				bounds_watcher = watcher;
			}

			protected override void Dispose(bool disposing) {
				if( disposing ) {
					font.Dispose();
					str_format.Dispose();
					active_text_brush.Dispose();
					inactive_text_brush.Dispose();
				}
				base.Dispose (disposing);
			}

			private void CollapsedPage_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {
				Graphics g = e.Graphics;
				Rectangle rect = this.ClientRectangle;

				// drawing the bounding rectangle
				g.DrawRectangle( new Pen( SystemColors.Control, BORDER_WIDTH ), rect );
				g.DrawRectangle( Pens.Black, BORDER_WIDTH, BORDER_WIDTH + SYSTEM_AREA_HEIGHT, rect.Width - BORDER_WIDTH * 2 - 2 - BACK_BORDER_WIDTH, rect.Height - 2*BORDER_WIDTH - SYSTEM_AREA_HEIGHT - 1 );
				g.DrawLine( Pens.Gray, rect.Right - 1, 0, rect.Right - 1, rect.Bottom );

				rect.X += BORDER_WIDTH;
				rect.Y += BORDER_WIDTH;
				rect.Height = SYSTEM_AREA_HEIGHT - 1;
				rect.Width -= BORDER_WIDTH * 2;				

				if( this.control.Focused ) {
					e.Graphics.FillRectangle( Brushes.DarkBlue, rect );
					if( rect.Width > MIN_PAGE_WIDTH ) {
						rect.Width -= MIN_PAGE_WIDTH;
						g.DrawString( title, font, active_text_brush, rect, str_format ); 
					}
				} else {
					e.Graphics.DrawRectangle( Pens.Gray, rect );
					if( rect.Width > MIN_PAGE_WIDTH ) {
						rect.Width -= MIN_PAGE_WIDTH;
						g.DrawString( title, font, inactive_text_brush, rect, str_format ); 
					}
				}
			}

			private void CollapsedPage_SizeChanged( object sender, System.EventArgs e ) {
				control.Size = new Size( this.Width - BORDER_WIDTH * 2 - 3 - BACK_BORDER_WIDTH, this.Height - SYSTEM_AREA_HEIGHT - BORDER_WIDTH * 2 - 2 );
				Invalidate();
			}

			private void CollapsedPage_Enter(object sender, System.EventArgs e) {
				if( !this.focused ) {
					collapser_ctrl.SetExclusiveFocus( this );
				} else {
					this.focused = true;
				}
				Invalidate( false );
			}

			private void CollapsedPage_Leave(object sender, System.EventArgs e) {
				if( this.collapser_ctrl.Focused ) {								
				} else {
					collapser_ctrl.ShowCollapsedPage( this, false );
				}
			}

			private void CollapsedPage_MouseEnter( object sender, EventArgs e ) {
				bounds_watcher.StopWatching();
			}

			private void CollapsedPage_MouseLeave( object sender, EventArgs e ) {
				if( !this.focused ) {
					bounds_watcher.StartWatching();
				}
			}

			public void ClosingPage() {
			}

			public void OpeningPage() {
			}

			public void PostOpeningPage( CollapsedPage prev ) {
				Invalidate( false );
			}
		}

		public FormsCollapserCtrl() {
			parent_control = null;
			InitializeCollapserControl();
		}		

		public FormsCollapserCtrl( CDS.ViewCtrl parent_ctrl ) {
			// This call is required by the Windows.Forms Form Designer.
			this.parent_control = parent_ctrl;
			InitializeCollapserControl();
		}

		private void InitializeCollapserControl() {
			InitializeComponent();
			
			items = new ArrayList();
			font = new Font( "Tahoma", 8.0f );
			str_format = new StringFormat();
			str_format.Alignment = StringAlignment.Near;
			str_format.LineAlignment = StringAlignment.Center;
			str_format.FormatFlags = StringFormatFlags.DirectionVertical;
			current = pointed_item = null;
			position = ControlPosition.POSITION_LEFT;
			timer = new System.Threading.Timer( new TimerCallback( this.FormsCollapserCtrl_TimerCallback ), null, 10000, MOUSE_TIMEOUT );
			timer_active = watchdog_timer_active = mouse_triggered = false;
			mouse_rect = Rectangle.Empty;

			bounds_watcher = new BoundsWatcher( WATCHDOG_TIMEOUT );
			bounds_watcher.watchdog_event += new System.EventHandler( FormsCollapserCtrl_WatchdogTimerCallback );			

			this.SizeChanged += new EventHandler(FormsCollapserCtrl_SizeChanged);
			this.Enter +=new EventHandler(FormsCollapserCtrl_Enter);

			this.SetStyle( ControlStyles.ResizeRedraw, false );
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.DoubleBuffer, true );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if(components != null) {
					components.Dispose();
				}
				font.Dispose();
				str_format.Dispose();
			}
			base.Dispose( disposing );
		}

		private void InstallMouseCallback() {
			if( !mouse_triggered ) {
				mouse_triggered = true;
				parent_control.MouseDown += new MouseEventHandler(FormsCollapserCtrl_ParentMouseDown);
			}
		}

		private void DeinstallMouseCallback() {
			if( mouse_triggered ) {
				mouse_triggered = false;
				parent_control.MouseDown -=new MouseEventHandler(FormsCollapserCtrl_ParentMouseDown);
				if( current != null ) {
					ShowCollapsedPage( current.page, false );
				}
			}
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.contextMenu1 = new System.Windows.Forms.ContextMenu();
			// 
			// contextMenu1
			// 
			this.contextMenu1.Popup += new System.EventHandler(this.contextMenu1_Popup);
			// 
			// FormsCollapserCtrl
			// 
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ContextMenu = this.contextMenu1;
			this.Name = "FormsCollapserCtrl";
			this.Size = new System.Drawing.Size(128, 136);
			this.Resize += new System.EventHandler(this.FormsCollapserCtrl_Resize);
			this.Load += new System.EventHandler(this.FormsCollapserCtrl_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.FormsCollapserCtrl_Paint);
			this.MouseEnter += new System.EventHandler(this.FormsCollapserCtrl_MouseEnter);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FormsCollapserCtrl_MouseMove);
			this.MouseLeave += new System.EventHandler(this.FormsCollapserCtrl_MouseLeave);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FormsCollapserCtrl_MouseDown);

		}
		#endregion

		private void FormsCollapserCtrl_SizeChanged( object sender, EventArgs e ) {
			if( current != null ) {
				current.page.Height = this.Height;
			}
		}

		private void FormsCollapserCtrl_Enter( object sender, EventArgs e ) {
			InstallMouseCallback();
		}

		private void FormsCollapserCtrl_ParentMouseDown( object sender, MouseEventArgs e ) {
			DeinstallMouseCallback();
		}

		public void SetExclusiveFocus( CollapsedPage page ) {
			foreach( CollapsedItem item in items ) {
				if( item.page != page ) {
					item.page.focused = false;
				} else {
					page.focused = true;
				}
			}
		}

		private void PopulateItem( CollapsedItem item ) {
			if( items.Count == 0 ) {
				item.selected = true;
			}
			//this.Controls.Add( item.page );
			parent_control.Controls.Add( item.page );
			items.Add( item );
			item.page.collapser_ctrl = this;
			AddPageToMenu( item );
			Invalidate();
		}

		public int AddControl( Control ctrl, String name ) {
			CollapsedItem item = new CollapsedItem( ctrl, name, this.bounds_watcher );
			PopulateItem( item );
			return items.Count - 1;
		}

		private void FormsCollapserCtrl_MouseLeave(object sender, System.EventArgs e) {
			mouse_rect = Rectangle.Empty;
			bounds_watcher.StartWatching();			
		}

		private void FormsCollapserCtrl_MouseEnter(object sender, System.EventArgs e) {
			bounds_watcher.StopWatching();
		}

		private void FormsCollapserCtrl_WatchdogTimerCallback( object sender, EventArgs e ) {
			if( current != null ) {
				ShowCollapsedPage( current.page, false );
				current = null;
			}
		}

		public void TimerDelegate( object sender, EventArgs e ) {
			SwitchToCollapsedItem( pointed_item );	
		}

		private void FormsCollapserCtrl_TimerCallback( object param ) {
			timer.Change( Timeout.Infinite, Timeout.Infinite );
			timer_active = false;
			if( pointed_item != current && pointed_item != null ) {	
				this.Invoke( new EventHandler( this.TimerDelegate ) );
			}
		}


		private void FormsCollapserCtrl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			CollapsedItem item = FindItemByCoords( e.X, e.Y );
			
			if( item != null ) {
				if( !mouse_rect.Contains( e.X, e.Y ) ) {
					timer.Change( MOUSE_TIMEOUT, MOUSE_TIMEOUT );
					timer_active = true;
					pointed_item = item;
					mouse_rect = item.rect;
				}
			} else {
				if( timer_active ) {
					timer.Change( Timeout.Infinite, MOUSE_TIMEOUT );
					timer_active = false;
					pointed_item = null;
					mouse_rect = Rectangle.Empty;
				}
			}
		}

		private void AddPageToMenu( CollapsedItem item ) {
			MenuItem menu_item;

			menu_item = new MenuItem( item.name, new EventHandler( this.PopupCollapsedPage ) ); 
			contextMenu1.MenuItems.Add( menu_item );			
		}

		private void PopupCollapsedPage( object sender, EventArgs e ) {	
		}

		private void contextMenu1_Popup(object sender, System.EventArgs e) {
		}

		private void FormsCollapserCtrl_Resize(object sender, System.EventArgs e) {
			if( current != null && current.page.Visible == true ) {
				current.page.Bounds = GetPageRectangle( current.page );
			}
			Invalidate();
		}

		private void FormsCollapserCtrl_Load(object sender, System.EventArgs e) {	
			this.Width = CONTROL_WIDTH;
		}

		private CollapsedItem FindItemByCoords( int x, int y ) {
			foreach( CollapsedItem item in items ) {
				if( item.rect.Contains( x, y ) ) {
					return item;
				}
			}
			return null;
		}

		private Point[] GetCollapsedItemDrawPoints( CollapsedItem item ) {
			Point[] pts = new Point[3];

			switch( position ) {
				case ControlPosition.POSITION_LEFT:
					pts[0].X = 0;
					pts[0].Y = item.rect.Y;
					pts[1].X = pts[2].X = item.rect.Right;
					pts[1].Y = pts[2].Y = item.rect.Bottom;	
					pts[2].X -= SELECTED_WIDTH;
					break;
				case ControlPosition.POSITION_RIGHT:
					break;
				default:
					throw new Exception( "FormsCollapserCtrl accepts only right or left docking !" );
			}
			return pts;
		}

		private int GetCollapsedItemXCoord() {
			int x;

			switch( position ) {
				case ControlPosition.POSITION_LEFT:
					x = 0;
					break;
				case ControlPosition.POSITION_RIGHT:
					x = CONTROL_WIDTH - ITEM_WIDTH;
					break;
				default:
					throw new Exception( "FormsCollapserCtrl accepts only left or right docking !" );
					//break;
			}
			return x;
		}

		private Rectangle GetPageRectangle( CollapsedPage p ) {
			Rectangle r;

			switch( position ) {
				case ControlPosition.POSITION_LEFT:
					r = new Rectangle( parent_control.ClientRectangle.X, parent_control.ClientRectangle.Y, p.Width, parent_control.Height );
					break;
				case ControlPosition.POSITION_RIGHT:
					r = new Rectangle( parent_control.ClientRectangle.Right - CONTROL_WIDTH - p.Width, parent_control.ClientRectangle.Y, p.Width, p.Height );
					break;
				default:
					throw new Exception( "FormsCollapserCtrl accepts only left or right docking !" );
			}
			return r;
		}

		private Rectangle GetDrawingRectangle() {
			if( position == ControlPosition.POSITION_LEFT ) { // this.Dock == DockStyle.Left ) {
				return new Rectangle( 0, 0, CONTROL_WIDTH, this.Height );
			} else if( position == ControlPosition.POSITION_RIGHT ) { //this.Dock == DockStyle.Right ) {
				if( current != null && current.page.Visible == true ) {
					return new Rectangle( this.ClientRectangle.Right - CONTROL_WIDTH, 0, CONTROL_WIDTH, this.Height );
				} else {
					return new Rectangle( 0, 0, CONTROL_WIDTH, this.Height );
				}
			} else {			
				throw new Exception( "FormsCollapserCtrl accepts only left or right docking !" );
			}			
		}

		private void ShowCollapsedPage( CollapsedPage page, bool visible ) {
			if( page != null ) {
				if( visible ) {
					page.Bounds = GetPageRectangle( page );
					current.page.OpeningPage();
					page.Visible = true;
					if( page.focused ) {
						page.control.Focus();
					}
					InstallMouseCallback();
				} else {
					if( current != null ) {
						current.page.ClosingPage();
					}
					page.Visible = false;
				}
			}
		}

		private void SwitchToCollapsedItem( CollapsedItem item ) {   
			this.SuspendLayout();
    	
			if( current != null ) {
				ShowCollapsedPage( current.page, false );
				if( current.selected ) {
					current.rect.Width -= SELECTED_WIDTH;
					current.selected = false;
				}
			}
			if( item != null ) {
				CollapsedPage page = null;
				if( current != null ) {
					page = current.page;
				}

				current = item;

				foreach( CollapsedItem i in items ) {
					if( i != current && i.selected ) {
						i.selected = false;
						i.rect.Width -= SELECTED_WIDTH;
					}
				}

				if( !current.selected ) {
					current.selected = true;
					current.rect.Width += SELECTED_WIDTH;
				}
				ShowCollapsedPage( item.page, true );
				item.page.PostOpeningPage( page );
			}
			this.ResumeLayout( true );
		}

		private void FormsCollapserCtrl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			if( e.Button == MouseButtons.Left ) {
				CollapsedItem item = FindItemByCoords( e.X, e.Y );
				if( item != null ) {
					timer.Change( Timeout.Infinite, MOUSE_TIMEOUT );
					timer_active = false;

					if( item != current ) {
						if( current != null ) {
							SwitchToCollapsedItem( item );
						} else {
							SwitchToCollapsedItem( item );
						}
					} else {
						ShowCollapsedPage( current.page, false );
						current = null;
					}
				} else {  // Collapse item
					if( timer_active ) {
						timer.Change( Timeout.Infinite, MOUSE_TIMEOUT );
						timer_active = false;
						pointed_item = null;
					}
					if( current != null ) {
						ShowCollapsedPage( current.page, false );
						current = null;
					}
				}
			} else if( e.Button == MouseButtons.Right ) {

				System.Windows.Forms.ContextMenu m = new ContextMenu();
				int i = 0;
				foreach(CollapsedItem item in items ) {
					FlatMenuItem mi = new FlatMenuItem( item.name, null, i, item == current );
					mi.Click += new EventHandler(change_tab);
					m.MenuItems.Add( mi );
					i++;
				}
				m.Show( this, this.PointToClient(Cursor.Position) );
			}
		}

		private void change_tab(object sender, EventArgs e) {
			if( items[(sender as FlatMenuItem).Index] != current )
				SwitchToCollapsedItem( items[(sender as FlatMenuItem).Index] as CollapsedItem );
		}

		private void FormsCollapserCtrl_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {
			Graphics g = e.Graphics;
			SolidBrush backgr_brush = new SolidBrush( Color.Gainsboro );
			Pen border_pen = new Pen( Color.Gray );
			Rectangle r, rect;
			int height, y = INITIAL_SPACING;
			Point[] draw_pts;

			rect = GetDrawingRectangle();
			using( Brush b = new SolidBrush( Color.FromArgb( 247, 243, 233 ) ) )
				g.FillRectangle( b, rect );

			foreach( CollapsedItem item in items ) {
				if( item.rect.Size == Size.Empty ) {
					SizeF sizef = g.MeasureString( item.name, font, 200, str_format );
					height = (int)Math.Ceiling(sizef.Height) + IMAGE_SPACING;

					if( item.image != null ) {
						height += IMAGE_SIZE + TEXT_SPACING;
					}
					item.rect.Height = height + FINAL_SPACING; // + 1;   // +1: correcting error caused by type conversion
					item.rect.Width = ITEM_WIDTH;
					if( item.selected ) {
						item.rect.Width += SELECTED_WIDTH;
					}
					item.rect.Y = y;
					item.rect.X = GetCollapsedItemXCoord();
				}
			
				r = item.rect;
				r.X += rect.X;
				if( item == current ) {
					// TODO
					g.FillRectangle( SystemBrushes.Control, r );
				} else {
					g.FillRectangle( SystemBrushes.Control, r );
				}

				draw_pts = GetCollapsedItemDrawPoints( item );
				g.DrawLine( border_pen, draw_pts[0].X, draw_pts[0].Y, draw_pts[1].X, draw_pts[0].Y );
				g.DrawLine( border_pen, draw_pts[1].X, draw_pts[0].Y, draw_pts[1].X, draw_pts[1].Y );
				if( item.selected ) {
					g.DrawLine( border_pen, draw_pts[1].X, draw_pts[1].Y, draw_pts[2].X, draw_pts[2].Y );
				}

				r.X += ITEM_CEILING;
				r.Y += IMAGE_SPACING;
				r.Width -= 2*ITEM_CEILING;
				r.Height -= INITIAL_SPACING + FINAL_SPACING;
				if( item.image != null ) {
					g.DrawImage( item.image, r.X, r.Y );
					r.Y += IMAGE_SIZE + TEXT_SPACING;
					r.Height -= IMAGE_SIZE + TEXT_SPACING;
				}

				g.DrawString( item.name, font, new SolidBrush( Color.FromArgb( 64, 64, 64 )) , r, str_format );
				y += item.rect.Height + ITEM_SPACING;

				if( items.IndexOf( item ) == items.Count - 1 ) {
					g.DrawLine( border_pen, draw_pts[1].X, draw_pts[1].Y, draw_pts[0].X, draw_pts[1].Y );
				}
			}
		}

		public int AddControl( Control ctrl, String name, Image image ) {
			CollapsedItem item = new CollapsedItem( ctrl, name, this.bounds_watcher );
			item.image = image;
			PopulateItem( item );
			return items.Count - 1;
		}
	}
}
