using System;
using System.Drawing;
using System.Collections;

namespace CDS.GUI
{
	/// <summary>
	/// General routines of plane geometry
	/// </summary>
	public class Geometry {

		// class has no instances
		private Geometry() { }

		public enum Direction {
			South = 0, East = 1, North = 2, West = 3, Null
		};

		/// <summary>
		/// (nx,ny) is a nearest point to (x,y), which lies on segment (p1,p2)
		/// </summary>
		public static void nearest_point_from_segment( int x, int y, Point p1, Point p2, out int nx, out int ny ) {
			float A, B, C1, C2;
			int dx = p1.X - p2.X, dy = p1.Y - p2.Y;

			//Console.WriteLine( x + " " + y + " " + p1.ToString() + p2.ToString() );

			if( Math.Abs(dy) > Math.Abs(dx) ) {
				A = 1; B = - (float)dx/dy;
			} else {
				B = 1; A = - (float)dy/dx;
			}

			C1 = - ( A*p1.X + B*p1.Y );
			C2 = - ( -B*x + A*y );

			if( Math.Abs(dy) > Math.Abs(dx) ) {
				ny = -(int)(( B*C1 + C2 ) / ( B*B + 1 ));
				nx = (int)(- ( B*ny + C1 ));
			} else {
				nx = -(int)(( A*C1 - C2 ) / ( A*A + 1 ));
				ny = (int)(- ( A*nx + C1 ));
			}

			if( Math.Sign( nx - p1.X ) == Math.Sign( nx - p2.X ) && Math.Sign( ny - p1.Y ) == Math.Sign( ny - p2.Y ) ) {
				if( ((p1.X-x)*(p1.X-x) + (p1.Y-y)*(p1.Y-y)) > ((p2.X-x)*(p2.X-x) + (p2.Y-y)*(p2.Y-y)) ) {
					nx = p2.X; ny = p2.Y;
				} else {
					nx = p1.X; ny = p1.Y;
				}
			}
		}

		public static float point_to_uy( int x, int y, Point p1, Point p2 ) {
			return (float)(Math.Sqrt((p1.X-x)*(p1.X-x)+(p1.Y-y)*(p1.Y-y)) / Math.Sqrt((p1.X-p2.X)*(p1.X-p2.X)+(p1.Y-p2.Y)*(p1.Y-p2.Y)));
		}

		public static void uy_to_point( float uy, Point p1, Point p2, out int x, out int y ) {
			x = p1.X + (int)((p2.X - p1.X)*uy);
			y = p1.Y + (int)((p2.Y - p1.Y)*uy);
		}

		public static void nearest_point_from_segment_list( int x, int y, ArrayList /* of GuiPoint */ list, out int segnum, out int rx, out int ry ) {
			GuiPoint p = (GuiPoint)list[0];
			Point p1 = new Point(p.x,p.y), p2 = new Point(0,0);
			int mindist = -1;
			int x2, y2;
			int i = 1;

			segnum = 0;
			rx = p.x;
			ry = p.y;

			do {
				p = (GuiPoint)list[i];
				p2.X = p.x;
				p2.Y = p.y;

				nearest_point_from_segment( x, y, p1, p2, out x2, out y2 );
                int dist = (int)Math.Sqrt( (x-x2)*(x-x2)+(y-y2)*(y-y2) );

				if( dist < mindist || mindist == -1 ) {
                    segnum = i-1;
					rx = x2;
					ry = y2;
					mindist = dist;
				}

				p1 = p2;
				i++;
			} while( i < list.Count );
		}

		public static bool intersect_segment_and_segment( int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4 ) {

			long ua, ub, div;

			div = ((long)(y4-y3))*(x2-x1) - ((long)(x4-x3))*(y2-y1);
			ua = ((long)(x4-x3))*(y1-y3)-((long)(y4-y3))*(x1-x3);
			ub = ((long)(x2-x1))*(y1-y3)-((long)(y2-y1))*(x1-x3);

			if( div < 0 ) {
				div = -div;
				ua = -ua;
				ub = -ub;
			}

			if( div == 0 || ua > div || ua < 0 || ub < 0 || ub > div )
				return false;
			
			return true;
		}

		public static bool rectangle_intersects_with_line( Rectangle r, int x1, int y1, int x2, int y2 ) {

			if( r.IsEmpty )
				return false;

			if( r.Contains(	x1, y1 ) || r.Contains( x2, y2 ) )
				return true;

			if( intersect_segment_and_segment( r.Left, r.Top, r.Right, r.Top, x1, y1, x2, y2 )
				|| intersect_segment_and_segment( r.Left, r.Top, r.Left, r.Bottom, x1, y1, x2, y2 )
				|| intersect_segment_and_segment( r.Left, r.Bottom, r.Right, r.Bottom, x1, y1, x2, y2 )
				|| intersect_segment_and_segment( r.Right, r.Top, r.Right, r.Bottom, x1, y1, x2, y2 ) )
				return true;

			return false;			
		}

		public static bool rect_inters_with_quadric_segment( Rectangle r, int x1, int y1, int x2, int y2 ) {
			if( r.IsEmpty )
				return false;
			if( x1 == x2 )
				return x1 > r.Left && x1 < r.Right && !( y1 <= r.Top && y2 <= r.Top ) && !( y1 >= r.Bottom && y2 >= r.Bottom );
			else if( y1 == y2 )
				return y1 > r.Top && y1 < r.Bottom && !( x1 <= r.Left && x2 <= r.Left ) && !( x1 >= r.Right && x2 >= r.Right );
			else
				throw new Exception( "line is not quadric" );
		}

		public static int rect_inters_with_quadric_segment_pt( Rectangle rect, int x1, int y1, int x2, int y2, out int ox1, out int oy1, out int ox2, out int oy2 ) {
			int ret = 0;
			bool invert = false;

			ox1 = oy1 = ox2 = oy2 = 0;

			if( rect.IsEmpty )
				return ret;

			if( x1 == x2 ) {
				if( x1 > rect.Left && x1 < rect.Right ) {
					int down = y1, upper = y2;
					if( y2 < y1 ) {
						down = y2;
						upper = y1;
						invert = true;
					}
					if( down < rect.Top && upper > rect.Top ) {
						ret++;
						ox1 = x1;
						oy1 = rect.Top;

						if( upper > rect.Bottom ) {
							ret++;
							ox2 = x1;
							oy2 = rect.Bottom;
						}
					} else if( down > rect.Top && upper > rect.Bottom ) {
						ret++;
						ox1 = x1;
						oy1 = rect.Bottom;
					}
				}
			} else if( y1 == y2 ) {
				if( y1 > rect.Top && y1 < rect.Bottom ) {
					int left = x1, right = x2;
					if( x2 < x1 ) {
						left = x2;
						right = x1;
						invert = true;
					}
					if( left < rect.Left && right > rect.Left ) {
						ret++;
						ox1 = rect.Left;
						oy1 = y1;

						if( right > rect.Right ) {
							ret++;
							ox2 = rect.Right;
							oy2 = y1;
						}
					} else if( left > rect.Left && right > rect.Right ) {
						ret++;
						ox1 = rect.Right;
						oy1 = y1;
					}
				}
			}
			if( invert && ret == 2 ) {
				int t;

				t = ox1;
				ox1 = ox2;
				ox2 = t;

				t = oy1;
				oy1 = oy2;
				oy2 = t;
			}

			return ret;
		}

		public static int segment_length( int x1, int y1, int x2, int y2 ) {
			return (int) Math.Sqrt( (x2 - x1)*(x2 - x1) + (y2 - y1)*(y2 - y1) );	
		}

		public static void polygon_around_segment( ref Point[] pol, int x1, int y1, int x2, int y2, int delta ) {
			int dx = x2 - x1, dy = y2 - y1;
			double div = Math.Sqrt( dx*dx + dy*dy )/(double)delta;

			if( div > 0 ) {
				dx = (int)(dx/div);
				dy = (int)(dy/div);
				
				int nx1 = x1 - dx, ny1 = y1 - dy, nx2 = x2 + dx, ny2 = y2 + dy;
				int temp = dx;
				dx = -dy; dy = temp;

				pol[0].X = nx1 + dx; pol[0].Y = ny1 + dy;
				pol[1].X = nx2 + dx; pol[1].Y = ny2 + dy;
				pol[2].X = nx2 - dx; pol[2].Y = ny2 - dy;
				pol[3].X = nx1 - dx; pol[3].Y = ny1 - dy;
			} else {
				pol[0].X = x1 - delta; pol[0].Y = y1 - delta; 
				pol[1].X = x1 + delta; pol[1].Y = y1 - delta; 
				pol[2].X = x1 + delta; pol[2].Y = y1 + delta; 
				pol[3].X = x1 - delta; pol[3].Y = y1 + delta;
			}
		}

		#region EndPoint decorations of Connection

		public static void point_triangle_on_segment( out Point[] pol, int x1, int y1, int x2, int y2, int delta, int delta2 ) {
			int dx = x2 - x1, dy = y2 - y1;
			double div = Math.Sqrt( dx*dx + dy*dy )/(double)delta, div2 = Math.Sqrt( dx*dx + dy*dy )/(double)delta2;
			pol = new Point[3];

			if( div > 0 ) {
				dx = (int)(dx/div);
				dy = (int)(dy/div);

				int nx1 = x1 + dx, ny1 = y1 + dy;
				int temp = dx;
				dx = -(int)((y2 - y1)/div2); 
				dy = (int)((x2 - x1)/div2);

				pol[0].X = x1; pol[0].Y = y1;
				pol[1].X = nx1 + dx; pol[1].Y = ny1 + dy;
				pol[2].X = nx1 - dx; pol[2].Y = ny1 - dy;
			} else {
				pol[0].X = x1; pol[0].Y = y1;
				pol[1].X = x1-delta2; pol[1].Y = y1+delta;
				pol[2].X = x1+delta2; pol[2].Y = y1+delta;
			}
		}

		public static void point_rhomb_on_segment( out Point[] pol, int x1, int y1, int x2, int y2, int delta, int delta2 ) {
			int dx = x2 - x1, dy = y2 - y1;
			double div = Math.Sqrt( dx*dx + dy*dy )/(double)delta, div2 = Math.Sqrt( dx*dx + dy*dy )/(double)delta2;
			pol = new Point[4];

			if( div > 0 ) {
				dx = (int)(dx/div);
				dy = (int)(dy/div);

				int nx1 = x1 + dx, ny1 = y1 + dy;
				int temp = dx;
				dx = -(int)((y2 - y1)/div2); 
				dy = (int)((x2 - x1)/div2);

				pol[0].X = x1; pol[0].Y = y1;
				pol[1].X = nx1 + dx; pol[1].Y = ny1 + dy;
				pol[3].X = nx1 - dx; pol[3].Y = ny1 - dy;
				pol[2].X = 2*nx1-x1; pol[2].Y = 2*ny1 - y1;
			} else {
				pol[0].X = x1; pol[0].Y = y1;
				pol[1].X = x1-delta2; pol[1].Y = y1+delta;
				pol[3].X = x1+delta2; pol[3].Y = y1+delta;
				pol[2].X = x1; pol[2].Y = y1+2*delta;
			}
		}

		#endregion

		#region Direction

		public static void shift_direction( int x, int y, out int ox, out int oy, Direction d, int delta ) {
			ox = x + (d == Direction.West ? -delta : d == Direction.East ? +delta : 0 );
			oy = y + (d == Direction.North ? -delta : d == Direction.South ? +delta : 0 );
		}

		#endregion
	}
}
