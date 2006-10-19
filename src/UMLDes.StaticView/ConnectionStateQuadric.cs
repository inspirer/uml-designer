using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace UMLDes.GUI {

	public class ConnectionStateQuadric : ConnectionState {

		const int COLLAPSING_DELTA = 10;

		public ConnectionStateQuadric( GuiConnection c ) {
			conn = c;
		}

		public override GuiConnectionStyle style {
			get {
				return GuiConnectionStyle.Quadric;
			}
		}

		// user moves the end of connection
		public override void EndPointPositionChanging(GuiConnectionPoint movepoint) {
			int s = 0, a;
			fixup_quadric_endpoint_movement( movepoint );
			collapse_quadric_segments( ref s, out a );
			check_current_connection();
		}

		// handles connection's creation
		public override void DoCreationFixup( bool converted ) {
			// get coords of intermediate points
			Point[] p = GuiPathFinder.polyline_quadric_DummyWay( conn.first, conn.second, conn.parent.active_objects );
			// fixup the number of points
			while( p.Length + 2 > conn.ipoints.Count )
				if( converted )
					conn.insert_point_child( 0 );
				else
					conn.insert_point( 0 );
			while( p.Length + 2 < conn.ipoints.Count )
				if( converted )
					conn.remove_point_child( 1 );
				else
					conn.remove_point( 1 );
			// change coords
			for( int i = 0; i < p.Length; i++ ) {
				(conn.ipoints[i+1] as GuiIntermPoint).x = p[i].X;
				(conn.ipoints[i+1] as GuiIntermPoint).y = p[i].Y;
			}
		}

		// user changes quadric connection
		public override void Moving(int x, int y, ref int ux, ref float uy) {
			int ox;

			// moves intermediate point
			if( ux < 0 ) {
				int n = -ux - 1;
				if( n > 0 && n < conn.ipoints.Count - 1 ) {
					if( !move_quadric_point( ref n, out ox, x, y ) )
						ux = -(n + 1);
					else
						ux = ox;
				}

			// moves segment
			} else if( ux >= 0 && ux < conn.ipoints.Count - 1 ) {
				bool vertical = ((conn.ipoints[ux] as GuiPoint).x - (conn.ipoints[ux+1] as GuiPoint).x) == 0;

				int d = (vertical) ? (x - (conn.ipoints[ux] as GuiPoint).x) : (y - (conn.ipoints[ux] as GuiPoint).y);
				move_quadric_segment( ref ux, vertical, d, out ox, true );
			}
			check_current_connection();
		}

		// Params:
		//   segnum - segment number (0..), vertical_sement, delta (offset in points)
		//   affected_segment (if check is set), check - call collapse_quadric_segments
		// Returns:
		//   0 - the transformation affects the first connection point (new segment has been inserted ),
		//   1 - the transformation affects only intermediate points
		//   2 - the transformation affects the second connection point
		private int move_quadric_segment( ref int segnum, bool vertical_segment, int delta, out int affected_segment, bool check ) {
			GuiIntermPoint pt = null;
			GuiPoint orig_pt;
			int ret = -1;

			affected_segment = -1;

			if( delta == 0 )
				return ret;

			conn.Invalidate();

			if( conn.ipoints.Count == 2 ) {
				GuiIntermPoint pt2;

				pt = conn.insert_point( 0 );
				pt2 = conn.insert_point( 1 );

				pt.x = conn.first.x;
				pt.y = conn.first.y;
				pt2.x = conn.second.x;
				pt2.y = conn.second.y;

				if( vertical_segment ) {
					pt.x += delta;
					pt2.x += delta;
				} else {
					pt.y += delta;
					pt2.y += delta;
				}
				conn.add_child( pt, null );
				conn.add_child( pt2, null );
				segnum = 1;
				ret = 0;
			} else {
				if(	segnum != 0	) {
					orig_pt	= conn.ipoints[segnum] as GuiPoint;
				} else {
					orig_pt	= conn.ipoints[1] as GuiPoint;
				}

				if(	vertical_segment ) {
					orig_pt.x += delta;
				} else {
					orig_pt.y += delta;
				}

				// the last segment
				if(	segnum == conn.ipoints.Count - 2	) {
					pt = conn.insert_point( segnum );
					if(	vertical_segment ) {
						pt.x = orig_pt.x;
						pt.y = conn.second.y;
					} else {
						pt.y = orig_pt.y;
						pt.x = conn.second.x;
					}
					ret	= 2;

				// the first segment
				} else if( segnum == 0 ) {
					pt = conn.insert_point( 0 );

					if(	vertical_segment ) {
						pt.x = orig_pt.x;
						pt.y = conn.first.y;
					} else {
						pt.y = orig_pt.y;
						pt.x = conn.first.x;
					}
					segnum = 1;
					ret	= 0;

				// some segment in the middle
				} else {
					GuiPoint sec = conn.ipoints[segnum+1] as GuiPoint;
					if(	vertical_segment ) {
						sec.x += delta;
					} else if( !vertical_segment ) {
						sec.y += delta;
					}
					ret	= 1;
				}
			
				if(	pt != null ) {
					conn.add_child( pt, null );
				}
			}

			if( check )
				collapse_quadric_segments( ref segnum, out affected_segment );
			conn.Invalidate();
			return ret;
		}

		// Params:  ptnum - point number
		// Returns: false - drag point ptnum, true  - drag segment segnum
		private bool move_quadric_point( ref int ptnum, out int segnum, int to_x, int to_y ) {
			GuiPoint neighbour = conn.ipoints[ptnum-1] as GuiPoint;
			GuiPoint move_point = conn.ipoints[ptnum] as GuiPoint;
			bool vertical;
			int delta, n, affected;
			bool ret = false;

			segnum = 0;

			for( int i = -1; i < 1; i++ ) {
				vertical = (neighbour.x - move_point.x) == 0;
				n = ptnum + i;
				delta = ( vertical ) ? to_x - move_point.x : to_y - move_point.y;

				if( delta != 0 ) {
					n = move_quadric_segment( ref n, vertical, delta, out affected, true );
					// if the transformation affects the first connection point,
					// then we have to change the point being moved
					if( affected == -1 ) {
						if( n == 0 ) {
							ptnum++;
						}
					} else {
						if( !conn.ipoints.Contains( move_point ) ){
							if( affected != 0 )
								segnum = affected - 1;
							ret = true;
						} else {
							ptnum = conn.ipoints.IndexOf( move_point );
						}
					}
				}
				if( ptnum < conn.ipoints.Count - 1 )
					neighbour = conn.ipoints[ptnum+1] as GuiPoint;
				else
					break;
			}
			return ret;
		}
		
		// simplifies the connection by removing similar points, then removing points which lies on a line
		// preserves segnum 
		private void normalize_quadric_points( ref int segnum ) {
			if( conn.ipoints.Count <= 2 )
				return;

			// removes similar points
			for( int i = 0; i < conn.ipoints.Count - 1; i++ ) {
				if( (conn.ipoints[i] as GuiPoint).x == (conn.ipoints[i+1] as GuiPoint).x &&
					(conn.ipoints[i] as GuiPoint).y == (conn.ipoints[i+1] as GuiPoint).y ) {

					int to_remove = 0;

					if( i == 0 ) {
						if( conn.ipoints.Count > 2 )
							to_remove = 1;
					} else {
						to_remove = i;
					}

					if( to_remove > 0 ) {
						if( segnum > i )
							segnum --;
						conn.remove_child( conn.ipoints[to_remove] as GuiBound );
						conn.remove_point( to_remove );
						i--;
					}
				}
			}

			// handle the situation when three points are on line, we remove middle one
			for( int i = 1; i < conn.ipoints.Count - 1; i++ ) {
				if( ((conn.ipoints[i] as GuiPoint).x == (conn.ipoints[i-1] as GuiPoint).x && (conn.ipoints[i] as GuiPoint).x == (conn.ipoints[i+1] as GuiPoint).x ) ||
					((conn.ipoints[i] as GuiPoint).y == (conn.ipoints[i-1] as GuiPoint).y && (conn.ipoints[i] as GuiPoint).y == (conn.ipoints[i+1] as GuiPoint).y ) ) {
					if( segnum >= i )
						segnum --;
					conn.remove_child( conn.ipoints[i] as GuiBound );
					conn.remove_point( i );
					i--;
				}
			}

			// debug
			check_current_connection();
		}

		// used in collapsing checks
		private bool bad_segment( int x1, int y1, int x2, int y2 ) {
			foreach( IAroundObject obj in conn.parent.AroundObjects )
				if( Geometry.rect_inters_with_quadric_segment( obj.AroundRect, x1, y1, x2, y2 ) )
					return true;
			return false;
		}

		// checks for collapsing. If something is collapsed, returns the new segment to drag
		// new_segment_to_drag is used when user moves a point and the point is destroyed
		// segnum is used when user moves the segment
		private void collapse_quadric_segments( ref int segnum, out int new_segment_to_drag ) {
			
			new_segment_to_drag = -1;
			normalize_quadric_points( ref segnum );
			if( conn.ipoints.Count <= 2 )
				return;

			// do for all segments, if collapsed => restart
			for( int i = 0; i < conn.ipoints.Count-1; i++ ) {
				bool vertical = ((conn.ipoints[i] as GuiPoint).x - (conn.ipoints[i+1] as GuiPoint).x == 0);

				int size = Math.Abs( vertical ?
					(conn.ipoints[i] as GuiPoint).y - (conn.ipoints[i+1] as GuiPoint).y
					: (conn.ipoints[i] as GuiPoint).x - (conn.ipoints[i+1] as GuiPoint).x );

				// length of segment is too small
				if( size < COLLAPSING_DELTA ) {

					// the first segment
					if( i == 0 ) {
						bool was_bad = bad_segment( (conn.ipoints[1] as GuiPoint).x, (conn.ipoints[1] as GuiPoint).y, (conn.ipoints[2] as GuiPoint).x, (conn.ipoints[2] as GuiPoint).y );
						if( conn.ipoints.Count > 3 && ( was_bad || !bad_segment(conn.first.x, conn.first.y, vertical ? (conn.ipoints[2] as GuiPoint).x : conn.first.x, vertical ? conn.first.y : (conn.ipoints[2] as GuiPoint).y ) ) ) {
							if( vertical ) {
								(conn.ipoints[2] as GuiPoint).y = conn.first.y;
							} else {
								(conn.ipoints[2] as GuiPoint).x = conn.first.x;
							}
							conn.remove_child( conn.ipoints[1] as GuiBound );
							conn.remove_point( 1 );
							new_segment_to_drag = i;
							segnum = 0;
							normalize_quadric_points( ref segnum );
							i = -1;			// start from the beginning
						}

					// the last segment
					} else if( i == conn.ipoints.Count - 2 ) {
						bool was_bad = bad_segment( (conn.ipoints[i-1] as GuiPoint).x, (conn.ipoints[i-1] as GuiPoint).y, (conn.ipoints[i] as GuiPoint).x, (conn.ipoints[i] as GuiPoint).y );
						if( conn.ipoints.Count > 3 && (was_bad || !bad_segment(conn.second.x, conn.second.y, vertical ? (conn.ipoints[i-1] as GuiPoint).x : conn.second.x, vertical ? conn.second.y : (conn.ipoints[i-1] as GuiPoint).y )) ) {
							conn.remove_child( conn.ipoints[i] as GuiBound );
							conn.remove_point( i );

							if( vertical ) {
								(conn.ipoints[i-1] as GuiPoint).y = conn.second.y;
							} else {
								(conn.ipoints[i-1] as GuiPoint).x = conn.second.x;
							}

							new_segment_to_drag = i;
							if( segnum > i )
								segnum -= 2;
							normalize_quadric_points( ref segnum );
							i = -1;			// start from the beginning
						}

					// middle segment, more than 3 segments
					} else if( conn.ipoints.Count > 4 ) {
						int nval = 0;

						// calculate new position, abort on intersections
						if( size != 0 ) {
							bool was_good = !bad_segment( (conn.ipoints[i-1] as GuiPoint).x, (conn.ipoints[i-1] as GuiPoint).y, (conn.ipoints[i] as GuiPoint).x, (conn.ipoints[i] as GuiPoint).y )
								&& !bad_segment( (conn.ipoints[i+1] as GuiPoint).x, (conn.ipoints[i+1] as GuiPoint).y, (conn.ipoints[i+2] as GuiPoint).x, (conn.ipoints[i+2] as GuiPoint).y );

							if( vertical ) {
								if( i+2 == conn.ipoints.Count-1 || i != 1 && segnum == i + 1  )
									nval = (conn.ipoints[i+2] as GuiPoint ).y;
								else
									nval = (conn.ipoints[i-1] as GuiPoint ).y;
								if( bad_segment( (conn.ipoints[i-1] as GuiPoint).x, nval, (conn.ipoints[i+2] as GuiPoint).x, nval ) && was_good )
									continue;    // cannot collapse, because of intersections
							} else {
								if( i+2 == conn.ipoints.Count-1 || i != 1 && segnum == i + 1 )
									nval = (conn.ipoints[i+2] as GuiPoint ).x;
								else
									nval = (conn.ipoints[i-1] as GuiPoint ).x;
								if( bad_segment( nval, (conn.ipoints[i-1] as GuiPoint).y, nval, (conn.ipoints[i+2] as GuiPoint).y ) && was_good )
									continue;    // cannot collapse, because of intersections
							}
						}

						// remove two points
						conn.remove_child( conn.ipoints[i] as GuiBound );
						conn.remove_point( i );
						conn.remove_child( conn.ipoints[i] as GuiBound );
						conn.remove_point( i );

						if( size != 0 ) {
							if( vertical )
								(conn.ipoints[i-1] as GuiPoint).y = (conn.ipoints[i] as GuiPoint).y = nval;
							else
								(conn.ipoints[i-1] as GuiPoint).x = (conn.ipoints[i] as GuiPoint).x = nval;
						}
						new_segment_to_drag = i;
						if( segnum >= i  )
							segnum -= 2;
						normalize_quadric_points( ref segnum );
						i = -1;			// start from the beginning
					}
				}
			}
		}

		// user moves conn_point
		private void fixup_quadric_endpoint_movement( GuiConnectionPoint conn_point ) {
			GuiPoint neighbour;
			int x, y;

			if( conn.ipoints.Count == 2 ) {
				int dx = conn.first.x - conn.second.x, dy = conn.first.y - conn.second.y;
				GuiIntermPoint pt = null;

				if( dx != 0 && dy != 0 ) {
					pt = conn.insert_point( 0 );
					conn.add_child( pt, null );
					GuiPathFinder.select_pos_for_middle_point( conn.first, conn.second, out x, out y );
					pt.x = x;
					pt.y = y;
				}
			} else if( conn.ipoints.Count == 3 ) {
				GuiIntermPoint pt = conn.ipoints[1] as GuiIntermPoint;
				GuiPathFinder.select_pos_for_middle_point( conn.first, conn.second, out x, out y );
				pt.x = x;
				pt.y = y;

			} else if( conn.ipoints.Count >= 3 ) {
				bool vertical;
				if( conn_point == conn.first ) {
					neighbour = conn.ipoints[1] as GuiPoint;
					vertical = neighbour.x - (conn.ipoints[2] as GuiPoint).x == 0;
				} else {
					neighbour = conn.ipoints[conn.ipoints.Count - 2] as GuiPoint;
					vertical = neighbour.x - (conn.ipoints[conn.ipoints.Count - 3] as GuiPoint).x  == 0;						
				}
			
				vertical = !vertical;	// we have to invert the value to obtain the direction of the segment being moved
				if( vertical ) {
					neighbour.x = conn_point.x;
				} else {
					neighbour.y = conn_point.y;
				}

			}
		}

		// shortens the connection by removing quadric loops
		/*
		private void remove_quadric_loops() {
			int i, len, sgn1, sgn2, len1, len2;
			bool repeat, vertical, need_rescan;

			if( conn.ipoints.Count < 4 )
				return;

			do {
				repeat = false;
				for( i = 0; i < conn.ipoints.Count - 3; i++ ) {
					vertical = (conn.ipoints[i] as GuiPoint).x == (conn.ipoints[i+1] as GuiPoint).x;
					if( vertical ) {
						len1 = (conn.ipoints[i+1] as GuiPoint).y - (conn.ipoints[i] as GuiPoint).y;
						len2 = (conn.ipoints[i+2] as GuiPoint).y - (conn.ipoints[i+3] as GuiPoint).y;
						sgn1 = Math.Sign( len1 );
						sgn2 = Math.Sign( len2 );
					} else {
						len1 = (conn.ipoints[i+1] as GuiPoint).x - (conn.ipoints[i] as GuiPoint).x;
						len2 = (conn.ipoints[i+2] as GuiPoint).x - (conn.ipoints[i+3] as GuiPoint).x;
						sgn1 = Math.Sign( len1 );
						sgn2 = Math.Sign( len2 );
					}

					if( sgn1 == sgn2 ) {
						int x1, y1, x2, y2;
						if( Math.Abs( len1 ) <= Math.Abs( len2 ) )
							len = len1;
						else
							len = len2;

						x1 = (conn.ipoints[i+1] as GuiPoint).x;
						y1 = (conn.ipoints[i+1] as GuiPoint).y;
						x2 = (conn.ipoints[i+2] as GuiPoint).x;
						y2 = (conn.ipoints[i+2] as GuiPoint).y;
	
						if( !vertical ) {
							x2 = x1 = x1 - len;
						} else {
							y2 = y1 = y1 - len;
						}						
                        
						do {
							need_rescan = false;
							foreach( IAroundObject obj in conn.parent.AroundObjects ) {
								Rectangle rect = obj.AroundRect;
								if( Geometry.rect_inters_with_quadric_segment( rect, x1, y1, x2, y2 ) ) {
									if( !vertical ) {
										if( (conn.ipoints[i+1] as GuiPoint).x <= rect.X + rect.Width / 2 ) {
											x1 = x2 = rect.X;
										} else {
											x1 = x2 = rect.Right;
										}
									} else { // horizontal
										if( (conn.ipoints[i+1] as GuiPoint).y <= rect.Y + rect.Height / 2 ) {
											y1 = y2 = rect.Y;
										} else {
											y1 = y2 = rect.Bottom;
										}
									}
									need_rescan = true;
									break;
								}
							}
						} while( need_rescan );
                        						
						int affected, r, segnum = i+1;

						if( !vertical ) {
							r = move_quadric_segment( ref segnum, true, x1 - (conn.ipoints[i+1] as GuiPoint).x, out affected, false );
						}
						else {
							r = move_quadric_segment( ref segnum, false, y1 - (conn.ipoints[i+1] as GuiPoint).y, out affected, false );
						}
						if( r != -1 )  {
							normalize_quadric_points();
							repeat = true;						
							break;
						}						
					}
				}
			} while( repeat );
		}  */
		/*
		private void fixup_quadric_segment_movement( GuiConnectionPoint conn_point ) {
			int i;
			GuiItem conn_item;
			Rectangle rect;

			if( conn_point.item == null || !(conn_point.item is GuiItem) )
				return;

			conn_item = conn_point.item as GuiItem;

			rect = conn_item.AroundRect;
			rect.X++;
			rect.Y++;
			rect.Width -= 2;
			rect.Height -= 2;
			for( i = 1; i < conn.ipoints.Count - 1; i++ ) {
				if( rect.Contains( (conn.ipoints[i] as GuiPoint).x, (conn.ipoints[i] as GuiPoint).y ) ) {
					int segnum, ptnum = i;
					move_quadric_point( ref ptnum, out segnum, (conn.ipoints[i] as GuiPoint).x + conn_point.move_dx, (conn.ipoints[i] as GuiPoint).y + conn_point.move_dy );
					return;
				}
			}

			for( i = 0; i < conn.ipoints.Count - 1; i++ ) {
				int x1 = (conn.ipoints[i] as GuiPoint).x;
				int y1 = (conn.ipoints[i] as GuiPoint).y;
				int x2 = (conn.ipoints[i+1] as GuiPoint).x;
				int y2 = (conn.ipoints[i+1] as GuiPoint).y;
				int delta_x, delta_y;

				bool vertical = x1 - x2 == 0;
				delta_x = delta_y = 0;
				rect = conn_item.AroundRect;

				if( vertical ) {
					rect.Width /= 2;
					if( Geometry.rect_inters_with_quadric_segment( rect, x1, y1, x2, y2 ) ) {
						delta_x = rect.X - x1;
					} else {
						rect.X += rect.Width;
						if( Geometry.rect_inters_with_quadric_segment( rect, x1, y1, x2, y2 ) ) {
							delta_x = rect.Right - x1;
						}
					}
				} else {
					rect.Height /= 2;
					
					if( Geometry.rect_inters_with_quadric_segment( rect, x1, y1, x2, y2 ) ) {
						delta_y = rect.Y - y1;
					} else {
						rect.Y += rect.Height;
						if( Geometry.rect_inters_with_quadric_segment( rect, x1, y1, x2, y2 ) ) {
							delta_y = y1 - rect.Bottom;
						}
					}					
				}

				if( delta_x != 0 || delta_y != 0 ) {
					int affected, segnum = i;

					//ignored_segments.Add( segnum );
					if( vertical )
						move_quadric_segment( ref segnum, true, delta_x, out affected, true );
					else
						move_quadric_segment( ref segnum, false, delta_y, out affected, true );
				}
			}
		}  */

		/*private void create_quadric_connection( bool delete_points ) {
			if( delete_points ) {
				for( int i = 1; i < conn.ipoints.Count - 1; i++ ) {
					conn.remove_child( conn.ipoints[i] as GuiBound );
				}
			}
		}*/

		// changes the connection to not intersect with objects from 'r'
		// if connection is changing and 'states' is not null, stores the original state to hash
		public override bool CheckIntersection(ArrayList arobjs, Hashtable states) {
			return false;
			/*int x1, x2, y1, y2, ox1, oy1, ox2, oy2, r, i;
			bool vertical;
			ArrayList path;
			int ret = 0;

			for( i = 0; i < conn.ipoints.Count - 1; i++ ) {
				x1 = (conn.ipoints[i] as GuiPoint).x;
				y1 = (conn.ipoints[i] as GuiPoint).y;
				x2 = (conn.ipoints[i+1] as GuiPoint).x;
				y2 = (conn.ipoints[i+1] as GuiPoint).y;

				vertical = x1 == x2;
				path = null;

				foreach( IAroundObject obj in conn.parent.AroundObjects ) {
					r = Geometry.rect_inters_with_quadric_segment_pt( obj.AroundRect, x1, y1, x2, y2, out ox1, out oy1, out ox2, out oy2 );
					if( r != 0 ) {
						GuiIntermPoint pt1;
						if( r == 1 ) {
						} else if( r == 2 ) {
						}
						
						if( r == 2)
							path = GuiPathFinder.turn_round_object_quadric( obj, ox1, oy1, ox2, oy2, conn.parent.AroundObjects );
						if( path != null && path.Count > 0 && r == 2 ) {
							(obj as GuiItem).Invalidate();
							pt1 = conn.insert_point( i );
							pt1.x = ox2;
							pt1.y = oy2;
							conn.add_child( pt1, null );

							pt1 = conn.insert_point( i );
							pt1.x = ox1;
							pt1.y = oy1;
							conn.add_child( pt1, null );

							i++;
							foreach( Point pt in path ) {
								pt1 = conn.insert_point( i );
								pt1.x = pt.X;
								pt1.y = pt.Y;
								conn.add_child( pt1, null );
								i++;
							}
							ret++;
							i++;
							break;
						}
					}
				}
			}
			return ret != 0;*/
		}

		public override void OptimizeConnection() {

		}

		#region "DEBUG routines"

		private void check_current_connection( ) {
#if DEBUG
			bool vertical = conn.first.x == (conn.ipoints[1] as GuiPoint).x;
			int x1 = conn.first.x, y1 = conn.first.y, x2, y2;
			for( int i = 1; i < conn.ipoints.Count; i++ ) {
				x2 = (conn.ipoints[i] as GuiPoint).x;
				y2 = (conn.ipoints[i] as GuiPoint).y;
				if( x1 == x2 && y1 == y2 && conn.ipoints.Count != 2 ) {
					show_points();
					throw new Exception( "two identical points" );
				}
				if( vertical && x1 != x2 || !vertical && y1 != y2 ) {
					show_points();
					throw new Exception( "wrong quadric line" );
				}
				x1 = x2; y1 = y2;
				vertical = !vertical;
			}
#endif
		}

		private void show_points() {
			System.Diagnostics.Debug.WriteLine( "conection: " + conn.ID );
			for( int i = 0; i < conn.ipoints.Count; i++ ) {
				GuiPoint pt = conn.ipoints[i] as GuiPoint;
				System.Diagnostics.Debug.WriteLine( "[" + i + "]: (" + pt.x + "," + pt.y + ")" );
			}
		}

		#endregion
	}
}
