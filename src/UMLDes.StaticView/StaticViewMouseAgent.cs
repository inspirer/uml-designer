using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using UMLDes.Model;

namespace UMLDes.GUI {

	public class StaticViewMouseAgent : MouseAgent {
		const int SCROLL_VALUE = 25;	/* pixels */
		const int SCROLL_TIMEOUT = 50;	/* msecs */

		enum MouseAction {
			None, Move, Drag, Select, Scroll, CreateConnection
		}			

		enum CurrentOperation {
			Select, DrawConnection, DrawComment
		};

		StaticView parent;
		MouseAction action;
		GuiItem dropitem;
		UmlObject dropobj;

		public StaticViewMouseAgent( StaticView p ) {
			parent = p;
			dropitem = null;
			action = MouseAction.None;
			scroll_active = false;
			scroll_dx = scroll_dy = 0;
			scroll_buttons = MouseButtons.None;
			scroll_timer = new System.Threading.Timer( new TimerCallback( ScrollTimerCallback ), null, Timeout.Infinite, SCROLL_TIMEOUT );
		}

		// Drag-n-drop functions

		public override void StartDrag( UmlObject elem ) {

			dropobj = elem;
			dropitem = GuiElementFactory.CreateElement( elem );
			System.Diagnostics.Debug.Assert( dropitem != null && dropitem is IMoveable && dropitem is IRemoveable, "wrong element created" );
			dropitem.parent = parent;
			if( dropitem is INeedRefresh )
				parent.RefreshObject( (INeedRefresh)dropitem );
			action = MouseAction.Drag;
		}

		public override void StopDrag() {
			if( dropitem == null )
				throw new ArgumentException( "have nothing to stop" );
			dropitem.Invalidate();
			dropitem = null;
			action = MouseAction.None;
		}

		private void NewRelation( GuiClass c1, GuiClass c2, GuiConnectionType t ) {
			GuiConnection c = new GuiConnection( new GuiConnectionPoint( c1, 1, .5f, 0 ), new GuiConnectionPoint( c2, 3, .5f, 1 ), t, parent, (GuiConnectionStyle)current_param2 );
			c.first.UpdatePosition( true );
			c.second.UpdatePosition( true );
			c.DoCreationFixup();
			c.ConnectionCreated( parent );
			c.Invalidate();
			parent.Undo.Push( new CreateOperation( c ), false );
		}

		public override void Drop( ) {
			if( dropitem == null )
				throw new ArgumentException( "have nothing to drop" );

			dropitem.id = parent.RegisterItemID( UmlModel.GetUniversal(dropobj), dropitem );
			dropitem.Invalidate();
			parent.Undo.Push( new CreateOperation( (IRemoveable)dropitem ), false );
			action = MouseAction.None;

			// insert Inheritance
			ArrayList l = new ArrayList( parent.active_objects );
			foreach( GuiObject a in l )
				if( a is GuiClass ) {
					GuiClass c = a as GuiClass;

					// TODO ?????????
					/*if( c.st.bases != null && c.st.bases.Contains( dropitem.st.fullname ) ) {
						NewRelation( dropitem, c, GuiConnectionType.Inheritance );
					} else if( dropitem.st.bases != null && dropitem.st.bases.Contains( c.st.fullname ) ) {
						NewRelation( c, dropitem, GuiConnectionType.Inheritance );
					}*/
				}
			dropitem = null;
		}

		public override void Drag( int x, int y ) {
			int ux = 0;
			float uy = 0;

			if( dropitem != null )
				((IMoveable)dropitem).Moving(x,y,ref ux,ref uy);
		}

		IMoveable moveitem;
		ArrayList movelist;
		int moveux, selx, sely;
		bool first_move;
		float moveuy;
		Rectangle selrect;
		GuiObject original_selected;
		Hashtable movestates = new Hashtable();

		IAcceptConnection conn_item;
		GuiConnection conn;

		private void AddAssociatedObjects() {
			ArrayList Keys = new ArrayList(movestates.Keys);
			for( int i = 0; i < Keys.Count; i++ ) {
				ArrayList l = (Keys[i] as IStateObject).Associated;
				if( l != null ) 
					foreach( IStateObject s in l ) {
						if( !movestates.ContainsKey(s) ) {
							movestates[s] = s.GetState();
                            Keys.Add( s );
						}
					}
			}
#if DEBUG
			System.Diagnostics.Debug.WriteLine( "Move" );
			foreach( GuiObject b in movestates.Keys )
				System.Diagnostics.Debug.WriteLine( b.GetType().ToString(), b.ToString() );
#endif
		}

		public override void MouseDown( int x, int y, MouseButtons b, Keys modif, int realx, int realy ) {

			// Left mouse button

			if( action != MouseAction.None )
				return;

			if( b == MouseButtons.Left ) {

				if( current_operation == (int)MouseOp.DrawMemo ) {

					GuiMemo m = GuiElementFactory.CreateMemo( parent, x, y );

					moveitem = m;
					first_move = true;
					moveux = 0;
					moveuy = 0;
					action = MouseAction.Move;

				} else if( (modif & Keys.Control) == Keys.Control || current_operation == (int)MouseOp.DrawConnection ) {

					conn_item = parent.FindItem( x, y, out moveux, out moveuy, true ) as IAcceptConnection;
					if( conn_item == null ) {
						action = MouseAction.Scroll;
						selx = x;
						sely = y;
						return;
					}

					int ux;
					float uy;
					conn_item.coord_nearest( x, y, out ux, out uy );
					action = MouseAction.CreateConnection;

					conn = new GuiConnection( new GuiConnectionPoint( conn_item, ux, uy, 0 ), new GuiConnectionPoint( x, y, 1 ), (GuiConnectionType)current_param1, parent, (GuiConnectionStyle)current_param2 );
					conn.first.item.coord_nearest( x, y, out conn.first.ux, out conn.first.uy );
					conn.first.UpdatePosition( true );
					conn.DoCreationFixup();
					conn.InvalidateTemporary();
					conn.Invalidate();


				} else if( ( modif & Keys.Shift) == Keys.Shift ) {

					GuiObject obj = parent.FindItem( x, y, false );
					if( obj != null ) {
						parent.SelectedObjects.Add( obj );
						obj.Invalidate();
					}

				} else {

					//   Left button click:
					//      select       
					//      move, move multiple

					GuiObject s = parent.FindItem( x, y, out moveux, out moveuy, false );
					if( s == null ) {
						parent.SelectedObjects.Clear();
						action = MouseAction.Select;
						selx = x;
						sely = y;
						return;
					}

					if( !s.selected ) {
						parent.SelectedObjects.Clear();
						parent.SelectedObjects.Add( s );
					}

					// deciding: to move, or not ...

                    moveitem = null;
					movelist = null;
					movestates.Clear();
					original_selected = null;
					GuiObject t = parent.FindItem( x, y, out moveux, out moveuy, true );
					if( t != null ) {
						if( t is IMoveRedirect ) {
							if( t.selected )
								original_selected = t;
							moveitem = (t as IMoveRedirect).MoveRedirect( ref moveux, ref moveuy );
						} else if( t is IMoveMultiple && (t as IMoveMultiple).CanMoveInGroup ) {
							movelist = new ArrayList();
							if( !t.selected )
								movelist.Add( t );
							foreach( GuiObject o in parent.SelectedObjects )
								if( o is IMoveMultiple && (o as IMoveMultiple).CanMoveInGroup )
									movelist.Add( o );
							selx = x;
							sely = y;

						} else if( t is IMoveable && (t as IMoveable).IsMoveable( x, y ) )
							moveitem = t as IMoveable;

						if( moveitem != null || movelist != null ) {
							first_move = true;
							action = MouseAction.Move;
						} else if( t is IClickable ) {
                            (t as IClickable).LeftClick( false, x, y );
						}
					}

				}

			} else if( b == MouseButtons.Right ) {

				ISelectable obj = parent.FindItem( x, y, false ) as ISelectable;
				if( obj != null ) {

					if( obj is IDropMenu ) {

						parent.SelectedObjects.Clear();
						parent.SelectedObjects.Add( obj as GuiObject );

						System.Windows.Forms.ContextMenu m = new ContextMenu();
						(obj as IDropMenu).AddMenuItems( m, x, y );
						if( m.MenuItems.Count > 0 )
							m.Show( parent.cview, new Point( realx, realy ) );
					}
					
				} else {
					action = MouseAction.Scroll;
					Cursor.Current = Cursors.Hand;
					selx = x;
					sely = y;
				}
			}
		}

		private bool MouseMoveAction( int x, int y, MouseButtons b ) {	
			switch( action ) {
				case MouseAction.Select:	
					if( selrect != Rectangle.Empty )
						parent.cview.InvalidatePage( selrect );

					selrect.X = Math.Min( x, selx );
					selrect.Y = Math.Min( y, sely );
					selrect.Width = Math.Abs( x - selx );
					selrect.Height = Math.Abs( y - sely );
					parent.cview.InvalidatePage( selrect );
					break;
				case MouseAction.Move:

					// on first move
					if( first_move ) {
						if( original_selected != null ) {
							parent.SelectedObjects.Remove( original_selected );
							original_selected = null;
						}
						if( moveitem != null ) {
							if( moveitem is IStateObject )
								movestates[moveitem] = (moveitem as IStateObject).GetState();
							if( !(moveitem as GuiObject).selected ) {
								parent.SelectedObjects.Clear();
								parent.SelectedObjects.Add( moveitem as GuiObject );
							}
						} else {
							parent.SelectedObjects.Clear();
							foreach( GuiObject t in movelist )
								parent.SelectedObjects.Add( t );
							foreach( GuiObject t in movelist )
								if( t is IStateObject )
									movestates[t] = (t as IStateObject).GetState();
						}
						AddAssociatedObjects();
						first_move = false;
					}

					// usual move
					if( moveitem != null ) {
						moveitem.Moving( x, y, ref moveux, ref moveuy );

					} else {
						foreach( IMoveMultiple i in movelist ) {
							i.ShiftShape( x - selx, y - sely );
						}
						selx = x;
						sely = y;
					}
					break;
				case MouseAction.Scroll:
					parent.cview.AdjustPageCoords( selx-x, sely-y );
					break;
				case MouseAction.CreateConnection:
					conn.Invalidate();
					conn.InvalidateTemporary();

					conn.second.item = parent.FindItem( x, y, out moveux, out moveuy, true ) as IAcceptConnection;
					if( conn.second.item == null || conn.second.item == conn.first.item ) {
						conn.second.x = x;
						conn.second.y = y;
						conn.second.item = null;
					} else {
						conn.second.item.coord_nearest( x, y, out conn.second.ux, out conn.second.uy );
						conn.second.UpdatePosition( true );
					}
					conn.DoCreationFixup();
					conn.InvalidateTemporary();
					conn.Invalidate();
					break;
			}
			return action != MouseAction.Scroll;
		}

		public override void MouseUp(MouseButtons b) {

			if( scroll_active ) {
				StopScrolling();
			}

			parent.proj.SetDefaultDrawingMode();
			original_selected = null;

			switch( action ) {
				case MouseAction.Scroll:
					Cursor.Current = Cursors.Arrow;
					break;
				case MouseAction.CreateConnection:
					if( conn.second.item == null )
						conn.Invalidate();
					else {
						conn.ConnectionCreated( parent );
						parent.Undo.Push( new CreateOperation( conn ), false );
					}
					conn = null;
					break;
				case MouseAction.Move:

					ArrayList movedobjects = new ArrayList();
					foreach( GuiObject o in movestates.Keys )
						if( o is IAroundObject )
							movedobjects.Add( o );
					if( movedobjects.Count > 0 )
						parent.AroundObjectsMoved( movedobjects, movestates );

					foreach( IMoveable o in movestates.Keys )
						o.Moved();

					if( movestates.Count == 1 ) {
						foreach( IStateObject t in movestates.Keys )
							parent.Undo.Push( new StateOperation( t, movestates[t] as ObjectState, t.GetState() ), false );

					} else if( movestates.Count > 1 ) {
						MultipleOperation p = new MultipleOperation();
						foreach( IStateObject t in movestates.Keys )
							p.l.Add( new StateOperation( t, movestates[t] as ObjectState, t.GetState() ) );
						parent.Undo.Push( p, false );
					}
					movestates.Clear();
					moveitem = null;
					movelist = null;
					break;
				case MouseAction.Select:
					parent.SelectedObjects.Clear();
					parent.SelectInRectangle( selrect );
					parent.cview.InvalidatePage( selrect );
					selrect = Rectangle.Empty;
					break;
			}
			action = MouseAction.None;
		}

		public override void MouseMove(int mouse_x, int mouse_y, MouseButtons b) {
			int x, y;
			MouseBoundaryTest( mouse_x, mouse_y, b, out x, out y );
			MouseMoveAction( x, y, b );
		}

		public override void DrawTemporary( Graphics g, Rectangle r, int offx, int offy, Rectangle pagepiece ) {

			switch( action ) {
				case MouseAction.Select:
					g.DrawRectangle( Pens.Blue, selrect.X + r.X - offx, selrect.Y + r.Y - offy, selrect.Width-1, selrect.Height-1 );
					break;
				case MouseAction.Drag:
					if( dropitem != null && dropitem.place.IntersectsWith(pagepiece) ) {
						int x = dropitem.place.X + r.X - offx, y = dropitem.place.Y + r.Y - offy;
						dropitem.Paint( g, r, offx, offy );
						using( Pen p = new Pen( Color.White ) ) {
							p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
							g.DrawRectangle( p, x + GuiPolygonItem.inflate, y + GuiPolygonItem.inflate, dropitem.place.Width - 2*GuiPolygonItem.inflate, dropitem.place.Height - 2*GuiPolygonItem.inflate );
						}
					}

					break;
				case MouseAction.CreateConnection:
					if( conn != null ) {
						conn.Paint( g, r, offx, offy );
						conn.DrawTemporary( g, r, offx, offy );
					}
					break;
			}
		}

		#region Screen Scrolling

		bool scroll_active;
		int scroll_x, scroll_y, scroll_dx, scroll_dy;
		MouseButtons scroll_buttons;
		System.Threading.Timer scroll_timer;

		public void ScrollTimerCallback( object arg ) {
			//parent.cview.Invoke( new ViewCtrl.AdjustPageCoordsDelegate( parent.cview.AdjustPageCoords ), new object[] {scroll_dx, scroll_dy} );
			parent.cview.Invoke( new MethodInvoker( /*parent.cview.*/ScrollCallback ) );
		}

		public void ScrollCallback() {
			scroll_x += scroll_dx;
			scroll_y += scroll_dy;
			MouseMoveAction( scroll_x, scroll_y, scroll_buttons );
			parent.cview.AdjustPageCoords( scroll_dx, scroll_dy );
		}

		private void MouseBoundaryTest( int x, int y, MouseButtons b, out int out_x, out int out_y ) {
			out_x = scroll_x = x;
			out_y = scroll_y = y;

			if( action != MouseAction.None && action != MouseAction.Scroll ) {
				Rectangle r = parent.cview.PageRectangle;
				r.X++;
				r.Y++;
				r.Width -= 2;
				r.Height -= 2;

				if( !r.Contains( x, y ) ) {
					if( x > r.Right ) {
						scroll_dx = SCROLL_VALUE;
						out_x = r.Right;
						scroll_x = out_x;
					} else if( x < r.Left ) {
						scroll_dx = -SCROLL_VALUE;
						out_x = r.Left;
						scroll_x = out_x;
					}

					if( y > r.Bottom ) {
						scroll_dy = SCROLL_VALUE;
						out_y = r.Bottom ;
						scroll_y = out_y;
					} else if( y < r.Top ) {
						scroll_dy = -SCROLL_VALUE;
						out_y = r.Top;
						scroll_y = out_y;
					}
					scroll_active = true;
					scroll_buttons = b;
					scroll_timer.Change( SCROLL_TIMEOUT, SCROLL_TIMEOUT );
				} else {
					if( scroll_active ) {
						scroll_timer.Change( Timeout.Infinite, SCROLL_TIMEOUT );
						scroll_dx = scroll_dy = scroll_x = scroll_y = 0;
						scroll_buttons = MouseButtons.None;
						scroll_active = false;
					}	
				}
			}
			return;
		}

		private void StopScrolling() {
			scroll_timer.Change( Timeout.Infinite, SCROLL_TIMEOUT );
			scroll_active = false;
			scroll_dx = scroll_dy = scroll_x = scroll_y = 0;
			scroll_buttons = MouseButtons.None;
		}

		#endregion
	}

}