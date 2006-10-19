using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CDS.GUI { 

	public class GuiPathFinder {

		public static Point[] polyline_quadric_DummyWay( GuiConnectionPoint p1, GuiConnectionPoint p2, ArrayList active ) {
			int x1 = p1.x, y1 = p1.y, x2 = p2.x, y2 = p2.y;
			Geometry.Direction d1 = Geometry.Direction.Null, d2 = Geometry.Direction.Null;
			Rectangle r1 = Rectangle.Empty, r2 = Rectangle.Empty;

			if( p1.item is GuiItem ) {
				d1 = (p1.item as GuiItem).direction(p1.ux);
				r1 = (p1.item as GuiItem).AroundRect;
			}
			if( p2.item is GuiItem ) {
				d2 = (p2.item as GuiItem).direction(p2.ux);
				r2 = (p2.item as GuiItem).AroundRect;
			}

			if( (x1 == x2 || y1 == y2) && !Geometry.rect_inters_with_quadric_segment( r1, x1, y1, x2, y2 ) &&
				!Geometry.rect_inters_with_quadric_segment( r2, x1, y1, x2, y2 ) )
				return new Point[] {};

			// try two solutions (x1, y2) and (x2, y1)
			int xm, ym;
			if( select_pos_for_middle_point( p1, p2, out xm, out ym ) )
				return new Point[] { new Point(xm, ym) }; 

			// try solutions of 3 segments
			bool sol1, sol2;
			Point[] ans1 = new Point[] { new Point(x1,y1), new Point(x2,y2) },
				ans2 = new Point[] { new Point(x1,y1), new Point(x2,y2) };
			sol2 = sol1 = false;

			if( (d1 == Geometry.Direction.East && x2 < x1 ) ||
				(d1 == Geometry.Direction.West && x2 > x1 ) ||
				(d1 == Geometry.Direction.South && y2 < y1 ) ||
				(d1 == Geometry.Direction.North && y2 > y1 ) ) {
				if( d1 == Geometry.Direction.North || d1 == Geometry.Direction.South ) {
					if( d2 == Geometry.Direction.North || d2 == Geometry.Direction.South ) {
						if( x2 > x1 ) {
							ans1[0].X = ans1[1].X = (r2.Left > r1.Right) ? (r2.Left+r1.Right)/2 : Math.Max( r1.Right, r2.Right );
						} else {
							ans1[0].X = ans1[1].X = (r2.Right < r1.Left) ? (r1.Left+r2.Right)/2 : Math.Min( r1.Left, r2.Left );
						}
					} else if( d2 == Geometry.Direction.West ) {
						ans1[0].X = ans1[1].X = r1.Left;
					} else {
						ans1[0].X = ans1[1].X = r1.Right;
					}
				} else {
					if( d2 == Geometry.Direction.West || d2 == Geometry.Direction.East ) {
						if( y2 > y1 ) {
							ans1[0].Y = ans1[1].Y = Math.Max( r1.Bottom, r2.Bottom );
						} else {
							ans1[0].Y = ans1[1].Y = Math.Min( r1.Top, r2.Top );
						}
					} else if( d2 == Geometry.Direction.North ) {
						ans1[0].Y = ans1[1].Y = r1.Top;
					} else {
						ans1[0].Y = ans1[1].Y = r1.Bottom;
					}
				}
				sol1 = true;
			}

			if((d2 == Geometry.Direction.East && x1 < x2 ) ||
				(d2 == Geometry.Direction.West && x1 > x2 ) ||
				(d2 == Geometry.Direction.South && y1 < y2 ) ||
				(d2 == Geometry.Direction.North && y1 > y2 ) ) {

				if( d2 == Geometry.Direction.North || d2 == Geometry.Direction.South ) {
					if( d1 == Geometry.Direction.North || d1 == Geometry.Direction.South ) {
						if( x1 > x2 ) {
							ans2[0].X = ans2[1].X = (r1.Left > r2.Right) ? (r1.Left+r2.Right)/2 : Math.Max( r1.Right, r2.Right );
						} else {
							ans2[0].X = ans2[1].X = (r1.Right < r2.Left) ? (r2.Left+r1.Right)/2 : Math.Min( r1.Left, r2.Left );
						}
					} else if( d1 == Geometry.Direction.West ) {
						ans2[0].X = ans2[1].X = r2.Left;
					} else {
						ans2[0].X = ans2[1].X = r2.Right;
					}
				} else {
					if( d1 == Geometry.Direction.West || d1 == Geometry.Direction.East ) {
						if( y1 > y2 ) {
							ans2[0].Y = ans2[1].Y = Math.Max( r1.Bottom, r2.Bottom );
						} else {
							ans2[0].Y = ans2[1].Y = Math.Min( r1.Top, r2.Top );
						}
					} else if( d1 == Geometry.Direction.North ) {
						ans2[0].Y = ans2[1].Y = r2.Top;
					} else {
						ans2[0].Y = ans2[1].Y = r2.Bottom;
					}
				}
				sol2 = true;
			}

			// try to select best one
			if( sol1 && sol2 ) {
				if( !Geometry.rect_inters_with_quadric_segment( r1, x1, y1, ans1[0].X, ans1[0].Y ) &&
					!Geometry.rect_inters_with_quadric_segment( r2, x1, y1, ans1[0].X, ans1[0].Y ) &&
					!Geometry.rect_inters_with_quadric_segment( r1, x2, y2, ans1[1].X, ans1[1].Y ) &&
					!Geometry.rect_inters_with_quadric_segment( r2, x2, y2, ans1[1].X, ans1[1].Y ) &&
					!Geometry.rect_inters_with_quadric_segment( r1, ans1[0].X, ans1[0].Y, ans1[1].X, ans1[1].Y ) &&
					!Geometry.rect_inters_with_quadric_segment( r2, ans1[0].X, ans1[0].Y, ans1[1].X, ans1[1].Y ) )
					sol2 = false;
				else
					sol1 = false;
			}

			return sol1 ? ans1 : ans2;
		}

		static public bool select_pos_for_middle_point( GuiConnectionPoint p1, GuiConnectionPoint p2, out int x, out int y ) {
			int x1 = p1.x, y1 = p1.y, x2 = p2.x, y2 = p2.y;
			Geometry.Direction d1 = Geometry.Direction.Null;
			Rectangle r1 = Rectangle.Empty, r2 = Rectangle.Empty;

			if( p1.item is GuiItem ) {
				d1 = (p1.item as GuiItem).direction(p1.ux);
				r1 = (p1.item as GuiItem).AroundRect;
			}
			if( p2.item is GuiItem ) {
				r2 = (p2.item as GuiItem).AroundRect;
			}

			// try two solutions (x1, y2) and (x2, y1)
			bool sol1 = !Geometry.rect_inters_with_quadric_segment( r1, x1, y1, x1, y2 ) &&
				!Geometry.rect_inters_with_quadric_segment( r2, x1, y1, x1, y2 ) &&
				!Geometry.rect_inters_with_quadric_segment( r1, x2, y2, x1, y2 ) &&
				!Geometry.rect_inters_with_quadric_segment( r2, x2, y2, x1, y2 );
			bool sol2 = !Geometry.rect_inters_with_quadric_segment( r1, x1, y1, x2, y1 ) &&
				!Geometry.rect_inters_with_quadric_segment( r2, x1, y1, x2, y1 ) &&
				!Geometry.rect_inters_with_quadric_segment( r1, x2, y2, x2, y1 ) &&
				!Geometry.rect_inters_with_quadric_segment( r2, x2, y2, x2, y1 );

			if( p2.item == null && r1.Contains( x2, y2 ) )
				sol1 = true;

			if( sol1 && sol2 ) {
				if( d1 == Geometry.Direction.South || d1 == Geometry.Direction.North )
					sol2 = false;  // select sol1
				else
					sol1 = false;
			}                

			if( sol1 || !sol2 && ( d1 == Geometry.Direction.South && y2-y1 > 0 || d1 == Geometry.Direction.North && y2-y1 < 0 || d1 == Geometry.Direction.West && x2-x1 > 0 || d1 == Geometry.Direction.East && x2-x1 < 0 ) ) {
				x = x1;
				y = y2;
			} else {
				x = x2;
				y = y1;
			}
			return sol1 || sol2;
		}
        

		public static ArrayList turn_round_object_quadric1( IAroundObject obj, int x1, int y1, int x2, int y2, Hashtable visited, ArrayList around_objects ) {
			Rectangle rect = obj.AroundRect;
			ArrayList path1 = new ArrayList();
			ArrayList path2 = new ArrayList();
			int dy, dx, i;
			int len1, len2;

			if( y1 == rect.Top || y1 == rect.Bottom ) {
				path1.Add( new Point(rect.Left, y1) );
				dy = y2 - y1;
				int y = y1 + Math.Sign( dy ) * rect.Height;
				len1 = x1 - rect.Left;

				if( Math.Abs( dy ) < rect.Height ) {
					if( x2 != rect.Left ) {						
						path1.Add( new Point( rect.Left, y ) );
						path1.Add( new Point( rect.Right, y ) );
						len1 += rect.Height + rect.Width;
						len1 += rect.Height - Math.Abs( dy );
					} else
						len1 += Math.Abs( dy );
				} else {
					if( x2 != rect.Left ) {
						path1.Add( new Point( rect.Left, y ) );
						len1 += rect.Height;
						len1 += x2 - rect.Left;
					}
				}

				path2.Add( new Point( rect.Right, y1 ) );
				len2 = rect.Right - x1;
				if( Math.Abs( dy ) < rect.Height ) {
					if( x2 != rect.Right ) {
						path2.Add( new Point( rect.Right, y ) );
						path2.Add( new Point( rect.Left, y ) );
						len2 += rect.Height + rect.Width;
						len2 += rect.Height - Math.Abs( dy );
					} else
						len2 += Math.Abs( dy );
				} else {
					if( x2 != rect.Right ) {
						path2.Add( new Point( rect.Right, y ) );
						len2 += rect.Height;
						len2 += rect.Right - x2;
					}
				}
			} else {
				path1.Add( new Point( x1, rect.Top ) );
				dx = x2 - x1;
				int x = x1 + Math.Sign( dx ) * rect.Width;

				len1 = y1 - rect.Top;
				if( Math.Abs( dx ) < rect.Width ) {
					if( y2 != rect.Top ) {
						path1.Add( new Point( x, rect.Top ) );
						path1.Add( new Point( x, rect.Bottom ) );
						len1 += rect.Width + rect.Height;
						len1 += rect.Width - Math.Abs( dx );
					} else
						len1 += Math.Abs( dx );
				} else {
					if( y2 != rect.Top ) {
						path1.Add( new Point( x, rect.Top ) );
						len1 += rect.Width;
						len1 += y2 - rect.Top;
					}
				}

				path2.Add( new Point( x1, rect.Bottom ) );
				len2 = rect.Bottom - y1;
				if( Math.Abs( dx ) < rect.Width ) {
					if( y2 != rect.Bottom ) {
						path2.Add( new Point( x, rect.Bottom ) );
						path2.Add( new Point( x, rect.Top ) );
						len2 += rect.Width + rect.Height;
						len2 += rect.Width - Math.Abs( dx );
					} else
						len2 += Math.Abs( dx );
				} else {
					if( y2 != rect.Bottom ) {
						path2.Add( new Point( x, rect.Bottom ) );
						len2 += rect.Width;
						len2 += rect.Bottom - y2;
					}
				}
			}

			Point first = new Point( x1, y1 );
			Point last = new Point( x2, y2 );
			Point prev = first;
			bool path1_good = true;
			for( i = 0; path1_good && i <= path1.Count; i++ ) {
				Point pt;
				if( i != path1.Count )
					pt = (Point)path1[i];
				else
					pt = last;
				foreach( IAroundObject o in around_objects ) {
					if( Geometry.rect_inters_with_quadric_segment( o.AroundRect, prev.X, prev.Y, pt.X, pt.Y ) ) {
						path1_good = false;
						break;
					}
				}
				prev = pt;
			}

			bool path2_good = true;
			prev = first;
			for( i = 0; path2_good && i <= path2.Count; i++ ) {
				Point pt;
				if( i != path2.Count )
					pt = (Point)path2[i];
				else
					pt = last;
				foreach( IAroundObject o in around_objects ) {
					if( Geometry.rect_inters_with_quadric_segment( o.AroundRect, prev.X, prev.Y, pt.X, pt.Y ) ) {
						path2_good = false;
						break;
					}
				}
				prev = pt;
			}

			if( path1_good && path2_good ) {
				if( len1 < len2 )
					return path1;
				else
					return path2;
			}

			if( path1_good )
				return path1;
			else
				return path2;
		}

		public static ArrayList turn_round_object_quadric( IAroundObject obj, int x1, int y1, int x2, int y2, ArrayList around_objs ) {
			Hashtable visited = new Hashtable();

			visited.Add( obj, obj );
			return turn_round_object_quadric1( obj, x1, y1, x2, y2, visited, around_objs );
		}
	}
}
