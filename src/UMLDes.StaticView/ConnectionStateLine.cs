using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace UMLDes.GUI {

	public class ConnectionStateLine : ConnectionState {

		public ConnectionStateLine( GuiConnection c ) {
			conn = c;
		}

		public override GuiConnectionStyle style {
			get {
				return GuiConnectionStyle.Line;
			}
		}

		public override void EndPointPositionChanging(GuiConnectionPoint movepoint) {

			SetupEndPointsPositions();
		}

		private void SetupEndPointsPositions() {
            /*int cpx1 = conn.first.x, cpy1 = conn.first.y, cpx2 = conn.second.x, cpy2 = conn.second.y;

			if( conn.first.item is IHasCenter ) {
				cpx1 = ((IHasCenter)conn.first.item).Center.X;
				cpy1 = ((IHasCenter)conn.first.item).Center.Y;
			}

			if( conn.second.item is IHasCenter ) {
				cpx2 = ((IHasCenter)conn.second.item).Center.X;
				cpy2 = ((IHasCenter)conn.second.item).Center.Y;
			}

			int x1 = cpx1, y1 = cpy1, x2 = cpx2, y2 = cpy2;

			if( conn.first.item is GuiItem )
				Geometry.rectangle_intersects_with_line( ((GuiItem)conn.first.item).place, cpx1, cpy1, cpx2, cpy2, out x1, out y1 );

			if( conn.second.item is GuiItem )
				Geometry.rectangle_intersects_with_line( ((GuiItem)conn.second.item).place, cpx1, cpy1, cpx2, cpy2, out x2, out y2 );
*/
			/*conn.first.item.coord_nearest( x1, y1, out conn.first.ux, out conn.first.uy );
			conn.second.item.coord_nearest( x2, y2, out conn.second.ux, out conn.second.uy );
			conn.first.UpdatePosition(false);
			conn.second.UpdatePosition(false);*/
			//conn.first.x = x1;
			//conn.first.y = y1;
			//conn.second.x = x2;
			//conn.second.y = y2;
		}

		public override void DoCreationFixup( bool converted ) {
			while( 2 < conn.ipoints.Count )
				if( converted )
					conn.remove_point_child( 1 );
				else
					conn.remove_point( 1 );

			SetupEndPointsPositions();
		}

		public override void Moving(int x, int y, ref int ux, ref float uy) {
		}

		public override bool CheckIntersection(ArrayList arobjs, Hashtable states) {
			return false;
		}

		public override void OptimizeConnection() {
		}


	}
}
