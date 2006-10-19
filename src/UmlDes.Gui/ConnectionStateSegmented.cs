using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using CDS.CSharp;
using CDS.Controls;
using System.Xml.Serialization;

namespace CDS.GUI {

	public class ConnectionStateSegmented : ConnectionState {

		public ConnectionStateSegmented( GuiConnection c ) {
			conn = c;
		}

		public override GuiConnectionStyle style {
			get {
				return GuiConnectionStyle.Segmented;
			}
		}

		public override void EndPointPositionChanging(GuiConnectionPoint movepoint) {
		}

		public override void DoCreationFixup() {
		}

		public override void Moving(int x, int y, ref int ux, ref float uy) {
			int x2, y2;

			if( ux >= 0 && ux < conn.ipoints.Count - 1 ) {
				Geometry.nearest_point_from_segment( x, y, new Point( ((GuiPoint)conn.ipoints[ux]).x, ((GuiPoint)conn.ipoints[ux]).y ),
					new Point( ((GuiPoint)conn.ipoints[ux+1]).x, ((GuiPoint)conn.ipoints[ux+1]).y ), out x2, out y2 );
				if( ( (x-x2)*(x-x2)+(y-y2)*(y-y2) ) > 9 ) {

					conn.Invalidate();
					GuiIntermPoint gp = conn.insert_point( ux );
					gp.x = x;
					gp.y = y;
					conn.add_child( gp, null );
					ux = -ux-2;
					conn.Invalidate();
				}
			} else if( ux < 0 ) {
				int n = -ux-1;
				if( n > 0 && n < conn.ipoints.Count - 1 ) {
					conn.Invalidate();
					Geometry.nearest_point_from_segment( x, y, new Point( ((GuiPoint)conn.ipoints[n-1]).x, ((GuiPoint)conn.ipoints[n-1]).y ),
						new Point( ((GuiPoint)conn.ipoints[n+1]).x, ((GuiPoint)conn.ipoints[n+1]).y ), out x2, out y2 );
					if( ( (x-x2)*(x-x2)+(y-y2)*(y-y2) ) <= 9 ) {
						ux = n - 1;
						uy = Geometry.point_to_uy( x2, y2, new Point( ((GuiPoint)conn.ipoints[n-1]).x, ((GuiPoint)conn.ipoints[n-1]).y ),
							new Point( ((GuiPoint)conn.ipoints[n+1]).x, ((GuiPoint)conn.ipoints[n+1]).y ) );
						conn.remove_child( (GuiBinded)conn.ipoints[n] );
						conn.remove_point( n );

					} else {
						((GuiPoint)conn.ipoints[n]).x = x;
						((GuiPoint)conn.ipoints[n]).y = y;
					}
					conn.Invalidate();
				}
			}
		}

		public override bool CheckIntersection(ArrayList arobjs, Hashtable states) {
			return false;
		}

		public override void OptimizeConnection() {

		}


	}
}
