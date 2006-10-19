using System;
using System.Drawing;
using System.Xml.Serialization;
using System.Collections;

namespace CDS.GUI {

	public abstract class View {

		public enum EditOperation {
            Cut, Copy, Paste, Delete, SelectAll
		};

		[XmlAttribute] public string name;
		[XmlIgnore] public ViewCtrl cview;
		[XmlIgnore] public MouseAgent mouseagent;
		[XmlIgnore] public Project proj;
		public abstract void Paint( Graphics g, Rectangle r, int offx, int offy );
		public abstract void PostLoad();

		public abstract void DoOperation( EditOperation op );
		public abstract bool IfEnabled( EditOperation op );

		[XmlIgnore] public abstract Undo undo { get; }
	}

	public abstract class Operation {

		public abstract void Do();
		public abstract void Undo();
	}

	public interface IStateObject {
        void Apply( ObjectState v );
		ObjectState GetState();
		ArrayList Associated { get; }
	}

	public abstract class ObjectState {
	}

	public interface IUndoNotification {
		void RefreshUndoState();
	}

	public class StateOperation : Operation {
		public IStateObject v;
		public ObjectState before, after;

		public StateOperation( IStateObject o, ObjectState s1, ObjectState s2 ) {
			v = o;
			before = s1;
			after = s2;
		}

		public override void Do() {
			v.Apply( after );
		}

		public override void Undo() {
			v.Apply( before );
		}
	}

	public class MultipleOperation : Operation {
		public ArrayList l = new ArrayList(); // of Operation
        
		public override void Do() {
			foreach( Operation o in l )
				o.Do();
		}

		public override void Undo() {
			foreach( Operation o in l )
				o.Undo();
		}
	}

	public abstract class Undo {

		public abstract bool can_undo { get; }
		public abstract bool can_redo { get; }

		public abstract void DoUndo();
		public abstract void DoRedo();
		public abstract void KillStack();
	}
}
