using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using UMLDes.Model;
using UMLDes.GUI;
using UMLDes.Controls;

namespace UMLDes {

	public class MainWnd : UMLDes.Controls.FlatMenuForm {
		private System.ComponentModel.IContainer components;
		private UMLDes.Controls.FlatMenuItem menuItem4;
		private System.Windows.Forms.MainMenu mainMenu1;
		public System.Windows.Forms.ImageList treeImages;
		public UMLDes.Controls.FlatToolBar toolBar1;
		private System.Windows.Forms.ImageList toolbarImages;
		
		public UmlDesignerSolution p;
		private UMLDes.Controls.FlatMenuItem menuItem26;
		private UMLDes.Controls.FlatMenuItem menuItem31;
		private UMLDes.Controls.FlatMenuItem menu_About;
		private UMLDes.Controls.FlatMenuItem menu_NewProject;
		private UMLDes.Controls.FlatMenuItem menu_OpenProject;
		private UMLDes.Controls.FlatMenuItem menu_SaveProject;
		private UMLDes.Controls.FlatMenuItem menu_SaveProjAs;
		private UMLDes.Controls.FlatMenuItem menu_Print;
		private UMLDes.Controls.FlatMenuItem menu_Exit;
		private UMLDes.Controls.FlatMenuItem menu_Undo;
		private UMLDes.Controls.FlatMenuItem menu_Cut;
		private UMLDes.Controls.FlatMenuItem menu_Copy;
		private UMLDes.Controls.FlatMenuItem menu_Paste;
		private UMLDes.Controls.FlatMenuItem menu_Delete;
		private UMLDes.Controls.FlatMenuItem menu_SelectAll;
		private UMLDes.Controls.FlatMenuItem menu_AddFiles;
		private UMLDes.Controls.FlatMenuItem menu_AddStaticView;
		private UMLDes.Controls.FlatMenuItem menu_Parse;
		private UMLDes.Controls.FlatMenuItem menumain_Help;
		private UMLDes.Controls.FlatMenuItem menumain_File;
		private UMLDes.Controls.FlatMenuItem menumain_Edit;
		private UMLDes.Controls.FlatMenuItem menumain_Project;
		private UMLDes.Controls.FlatMenuItem menu_Redo;
		private UMLDes.Controls.FlatMenuItem menu_GC_Collect;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Splitter splitter1;
		private UMLDes.ViewCtrl ViewCtrl1;
		private UMLDes.Controls.UmlSolutionTree ProjectTree;
		private UMLDes.Controls.FlatMenuItem menu_PrintPreview;
		private UMLDes.Controls.FlatMenuItem menu_show_hints;
		private System.Windows.Forms.StatusBar statusBar1;
		internal System.Windows.Forms.StatusBarPanel status_panel;
		private UMLDes.Controls.FlatMenuItem menuItem1;
		private UMLDes.Controls.FlatMenuItem menu_SaveToImage;
		private UMLDes.Controls.FlatMenuItem menuItem2;
		private UMLDes.Controls.FlatMenuItem menu_ZoomIn;
		private UMLDes.Controls.FlatMenuItem menu_ZoomOut;
		private UMLDes.Controls.FlatMenuItem menu_copyAsImage;
		public ImageList list;
		public UMLDes.Controls.UmlSolutionTree SolutionTree { get { return ProjectTree; } }
		
		public MainWnd() {
			InitializeComponent();
			PostInitialize();
			list = toolbarImages;
			
			TurnOnProject( UmlDesignerSolution.createNew() );
		}

		#region ToolBar/Tree initialization

		void initialize_tree_view( UMLDes.Controls.UmlSolutionTree tv ) {
			tv.BackColor = System.Drawing.SystemColors.Window;
			tv.ImageList = this.treeImages;
			tv.LabelEdit = true;
			tv.BeforeLabelEdit +=new NodeLabelEditEventHandler(BeforeLabelEdit);
			tv.AfterLabelEdit +=new NodeLabelEditEventHandler(AfterLabelEdit);
			tv.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TreeMouseDown);
			tv.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TreeMouseUp);
			tv.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TreeMouseMove);
			tv.GiveFeedback += new System.Windows.Forms.GiveFeedbackEventHandler(this.tree_GiveFeedback);
		}

		UMLDes.Controls.FlatToolBarButton tool_undo, tool_redo, tool_cut, tool_copy, tool_paste;

		void PostInitialize() {
			initialize_tree_view( ProjectTree );

			UMLDes.Controls.MouseClickEvent m = new UMLDes.Controls.MouseClickEvent(ToolbarAction);
			UMLDes.Controls.FlatToolBarPanel p;

			//  project toolbar
			p = toolBar1.AddPanel( 0, "Standard" );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.New, "New project", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.Open, "Open project", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.Save, "Save", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.Saveas, "Save as", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Line, 0, null, null );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.add_file, "Add files", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.new_diagram, "New Static View", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.refresh, "Refresh model", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Line, 0, null, null );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.print, "Print", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.print_preview, "Print Preview", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Line, 0, null, null );
			tool_cut = p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.cut, "Cut", m );
			tool_copy = p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.copy, "Copy", m );
			tool_paste = p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.paste, "Paste", m );
			p.AddButton( UMLDes.Controls.FlatButtonType.Line, 0, null, null );
			tool_undo = p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.undo, "Undo", m );
			tool_redo = p.AddButton( UMLDes.Controls.FlatButtonType.Simple, (int)ToolBarIcons.redo, "Redo", m );
			tool_cut.disabled = tool_copy.disabled = tool_paste.disabled = true;

			// Scale menu
			p = toolBar1.AddPanel( 0, "Scale" );
			ComboBox cb = new ComboBox(); 
			cb.TabStop = false;
			cb.Size = new Size( 90, 20 );
			cb.DropDownStyle = ComboBoxStyle.DropDownList;
			cb.MaxDropDownItems = 15;

			for( int i = 0; i < ViewCtrl.scalevalue.Length; i += 2 )
				cb.Items.Add( (ViewCtrl.scalevalue[i] * 100 / ViewCtrl.scalevalue[i+1] ).ToString() + "%" );
			cb.SelectedIndex = 5;
			cb.SelectedIndexChanged += new EventHandler(ViewCtrl1.ScaleChanged);
			ViewCtrl1.scalecombo = cb;

			p.AddControl( cb );
		}

		private void menu_ZoomOut_Click(object sender, System.EventArgs e) {
			ViewCtrl1.ZoomOut();
		}

		private void menu_ZoomIn_Click(object sender, System.EventArgs e) {
			ViewCtrl1.ZoomIn();
		}

		#endregion

		#region ToolBar related

		void EnableButton( UMLDes.Controls.FlatToolBarButton b, bool en ) {
			if( !b.disabled != en ) {
				b.disabled = !b.disabled;
				b.parent.InvalidateButton( b );
			}
		}

		public void UpdateToolBar() {
			EnableButton( tool_undo, ViewCtrl1.Curr.undo.can_undo );
			EnableButton( tool_redo, ViewCtrl1.Curr.undo.can_redo );
		}

		void ToolbarAction( int index ) {
			switch( (ToolBarIcons)index ) {
				case ToolBarIcons.New:   // New
					if( !SaveChanges() )
						return;
					TurnOnProject( UmlDesignerSolution.createNew() );
					break;
				case ToolBarIcons.Open:   // Open
					LoadProject( null, null );
					break;
				case ToolBarIcons.Save:   // Save
					SaveProject( null, null );
					break;
				case ToolBarIcons.Saveas:   // Saveas
					SaveAsProject( null, null );
					break;
				case ToolBarIcons.add_file: // Add files
					AddFiles(null, null);
					break;
				case ToolBarIcons.new_diagram: // New Static view
					menu_AddStaticView_Click(null, null);
					break;
				case ToolBarIcons.refresh:  // Refresh tree
					RefreshProject(null, null);
					break;
				case ToolBarIcons.print:
					ViewCtrl1.Print(false);
					break;
				case ToolBarIcons.print_preview:
					ViewCtrl1.Print(true);
					break;
				case ToolBarIcons.undo:   // Undo
					ViewCtrl1.Curr.undo.DoUndo();
					break;
				case ToolBarIcons.redo:   // Redo
					ViewCtrl1.Curr.undo.DoRedo();
					break;
				case ToolBarIcons.cut:
				case ToolBarIcons.copy:
				case ToolBarIcons.paste:
					MessageBox.Show( "CopyPaste" + ((ToolBarIcons)index).ToString() );
					break;
			}
		}

		#endregion
		
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainWnd));
			this.menu_About = new UMLDes.Controls.FlatMenuItem();
			this.toolbarImages = new System.Windows.Forms.ImageList(this.components);
			this.menumain_Help = new UMLDes.Controls.FlatMenuItem();
			this.menu_show_hints = new UMLDes.Controls.FlatMenuItem();
			this.menu_GC_Collect = new UMLDes.Controls.FlatMenuItem();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menumain_File = new UMLDes.Controls.FlatMenuItem();
			this.menu_NewProject = new UMLDes.Controls.FlatMenuItem();
			this.menu_OpenProject = new UMLDes.Controls.FlatMenuItem();
			this.menu_SaveProject = new UMLDes.Controls.FlatMenuItem();
			this.menu_SaveProjAs = new UMLDes.Controls.FlatMenuItem();
			this.menuItem4 = new UMLDes.Controls.FlatMenuItem();
			this.menu_Print = new UMLDes.Controls.FlatMenuItem();
			this.menu_PrintPreview = new UMLDes.Controls.FlatMenuItem();
			this.menu_SaveToImage = new UMLDes.Controls.FlatMenuItem();
			this.menuItem1 = new UMLDes.Controls.FlatMenuItem();
			this.menu_Exit = new UMLDes.Controls.FlatMenuItem();
			this.menumain_Edit = new UMLDes.Controls.FlatMenuItem();
			this.menu_Undo = new UMLDes.Controls.FlatMenuItem();
			this.menu_Redo = new UMLDes.Controls.FlatMenuItem();
			this.menuItem26 = new UMLDes.Controls.FlatMenuItem();
			this.menu_Cut = new UMLDes.Controls.FlatMenuItem();
			this.menu_Copy = new UMLDes.Controls.FlatMenuItem();
			this.menu_Paste = new UMLDes.Controls.FlatMenuItem();
			this.menu_Delete = new UMLDes.Controls.FlatMenuItem();
			this.menuItem31 = new UMLDes.Controls.FlatMenuItem();
			this.menu_SelectAll = new UMLDes.Controls.FlatMenuItem();
			this.menu_copyAsImage = new UMLDes.Controls.FlatMenuItem();
			this.menuItem2 = new UMLDes.Controls.FlatMenuItem();
			this.menu_ZoomIn = new UMLDes.Controls.FlatMenuItem();
			this.menu_ZoomOut = new UMLDes.Controls.FlatMenuItem();
			this.menumain_Project = new UMLDes.Controls.FlatMenuItem();
			this.menu_AddFiles = new UMLDes.Controls.FlatMenuItem();
			this.menu_AddStaticView = new UMLDes.Controls.FlatMenuItem();
			this.menu_Parse = new UMLDes.Controls.FlatMenuItem();
			this.treeImages = new System.Windows.Forms.ImageList(this.components);
			this.toolBar1 = new UMLDes.Controls.FlatToolBar();
			this.panel1 = new System.Windows.Forms.Panel();
			this.ProjectTree = new UMLDes.Controls.UmlSolutionTree();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.ViewCtrl1 = new UMLDes.ViewCtrl();
			this.statusBar1 = new System.Windows.Forms.StatusBar();
			this.status_panel = new System.Windows.Forms.StatusBarPanel();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.status_panel)).BeginInit();
			this.SuspendLayout();
			// 
			// menu_About
			// 
			this.menu_About.Enabled = false;
			this.menu_About.ImageIndex = 14;
			this.menu_About.Images = this.toolbarImages;
			this.menu_About.Index = 0;
			this.menu_About.OwnerDraw = true;
			this.menu_About.Text = "&About";
			// 
			// toolbarImages
			// 
			this.toolbarImages.ImageSize = new System.Drawing.Size(16, 16);
			this.toolbarImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("toolbarImages.ImageStream")));
			this.toolbarImages.TransparentColor = System.Drawing.Color.Silver;
			// 
			// menumain_Help
			// 
			this.menumain_Help.ImageIndex = 0;
			this.menumain_Help.Images = null;
			this.menumain_Help.Index = 4;
			this.menumain_Help.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_About,
																						  this.menu_show_hints,
																						  this.menu_GC_Collect});
			this.menumain_Help.OwnerDraw = true;
			this.menumain_Help.Text = "&Help";
			this.menumain_Help.Popup += new System.EventHandler(this.Help_Popup);
			// 
			// menu_show_hints
			// 
			this.menu_show_hints.ImageIndex = 0;
			this.menu_show_hints.Images = null;
			this.menu_show_hints.Index = 1;
			this.menu_show_hints.OwnerDraw = true;
			this.menu_show_hints.Text = "Show &hints";
			// 
			// menu_GC_Collect
			// 
			this.menu_GC_Collect.ImageIndex = 0;
			this.menu_GC_Collect.Images = null;
			this.menu_GC_Collect.Index = 2;
			this.menu_GC_Collect.OwnerDraw = true;
			this.menu_GC_Collect.Text = "GC.Collect";
			this.menu_GC_Collect.Click += new System.EventHandler(this.menu_GC_Collect_Click);
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menumain_File,
																					  this.menumain_Edit,
																					  this.menuItem2,
																					  this.menumain_Project,
																					  this.menumain_Help});
			// 
			// menumain_File
			// 
			this.menumain_File.ImageIndex = 0;
			this.menumain_File.Images = null;
			this.menumain_File.Index = 0;
			this.menumain_File.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_NewProject,
																						  this.menu_OpenProject,
																						  this.menu_SaveProject,
																						  this.menu_SaveProjAs,
																						  this.menuItem4,
																						  this.menu_Print,
																						  this.menu_PrintPreview,
																						  this.menu_SaveToImage,
																						  this.menuItem1,
																						  this.menu_Exit});
			this.menumain_File.OwnerDraw = true;
			this.menumain_File.Text = "&File";
			// 
			// menu_NewProject
			// 
			this.menu_NewProject.ImageIndex = 0;
			this.menu_NewProject.Images = this.toolbarImages;
			this.menu_NewProject.Index = 0;
			this.menu_NewProject.OwnerDraw = true;
			this.menu_NewProject.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menu_NewProject.Text = "&New project";
			this.menu_NewProject.Click += new System.EventHandler(this.menu_NewProject_Click);
			// 
			// menu_OpenProject
			// 
			this.menu_OpenProject.ImageIndex = 1;
			this.menu_OpenProject.Images = this.toolbarImages;
			this.menu_OpenProject.Index = 1;
			this.menu_OpenProject.OwnerDraw = true;
			this.menu_OpenProject.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.menu_OpenProject.Text = "&Open project";
			this.menu_OpenProject.Click += new System.EventHandler(this.LoadProject);
			// 
			// menu_SaveProject
			// 
			this.menu_SaveProject.ImageIndex = 2;
			this.menu_SaveProject.Images = this.toolbarImages;
			this.menu_SaveProject.Index = 2;
			this.menu_SaveProject.OwnerDraw = true;
			this.menu_SaveProject.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.menu_SaveProject.Text = "&Save project";
			this.menu_SaveProject.Click += new System.EventHandler(this.SaveProject);
			// 
			// menu_SaveProjAs
			// 
			this.menu_SaveProjAs.ImageIndex = 0;
			this.menu_SaveProjAs.Images = null;
			this.menu_SaveProjAs.Index = 3;
			this.menu_SaveProjAs.OwnerDraw = true;
			this.menu_SaveProjAs.Text = "Save &As ...";
			this.menu_SaveProjAs.Click += new System.EventHandler(this.SaveAsProject);
			// 
			// menuItem4
			// 
			this.menuItem4.ImageIndex = 0;
			this.menuItem4.Images = null;
			this.menuItem4.Index = 4;
			this.menuItem4.OwnerDraw = true;
			this.menuItem4.Text = "-";
			// 
			// menu_Print
			// 
			this.menu_Print.ImageIndex = 18;
			this.menu_Print.Images = this.toolbarImages;
			this.menu_Print.Index = 5;
			this.menu_Print.OwnerDraw = true;
			this.menu_Print.Shortcut = System.Windows.Forms.Shortcut.CtrlP;
			this.menu_Print.Text = "&Print";
			this.menu_Print.Click += new System.EventHandler(this.menu_Print_Click);
			// 
			// menu_PrintPreview
			// 
			this.menu_PrintPreview.ImageIndex = 24;
			this.menu_PrintPreview.Images = this.toolbarImages;
			this.menu_PrintPreview.Index = 6;
			this.menu_PrintPreview.OwnerDraw = true;
			this.menu_PrintPreview.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftP;
			this.menu_PrintPreview.Text = "Print Pre&view";
			this.menu_PrintPreview.Click += new System.EventHandler(this.menu_PrintPreview_Click);
			// 
			// menu_SaveToImage
			// 
			this.menu_SaveToImage.ImageIndex = 0;
			this.menu_SaveToImage.Images = null;
			this.menu_SaveToImage.Index = 7;
			this.menu_SaveToImage.OwnerDraw = true;
			this.menu_SaveToImage.Shortcut = System.Windows.Forms.Shortcut.CtrlI;
			this.menu_SaveToImage.Text = "Save diagram as Image";
			this.menu_SaveToImage.Click += new System.EventHandler(this.menu_SaveToImage_Click);
			// 
			// menuItem1
			// 
			this.menuItem1.ImageIndex = 0;
			this.menuItem1.Images = null;
			this.menuItem1.Index = 8;
			this.menuItem1.OwnerDraw = true;
			this.menuItem1.Text = "-";
			// 
			// menu_Exit
			// 
			this.menu_Exit.ImageIndex = 0;
			this.menu_Exit.Images = null;
			this.menu_Exit.Index = 9;
			this.menu_Exit.OwnerDraw = true;
			this.menu_Exit.Shortcut = System.Windows.Forms.Shortcut.CtrlX;
			this.menu_Exit.Text = "E&xit";
			this.menu_Exit.Click += new System.EventHandler(this.Exit);
			// 
			// menumain_Edit
			// 
			this.menumain_Edit.ImageIndex = 0;
			this.menumain_Edit.Images = null;
			this.menumain_Edit.Index = 1;
			this.menumain_Edit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_Undo,
																						  this.menu_Redo,
																						  this.menuItem26,
																						  this.menu_Cut,
																						  this.menu_Copy,
																						  this.menu_Paste,
																						  this.menu_Delete,
																						  this.menuItem31,
																						  this.menu_SelectAll,
																						  this.menu_copyAsImage});
			this.menumain_Edit.OwnerDraw = true;
			this.menumain_Edit.Text = "&Edit";
			this.menumain_Edit.Popup += new System.EventHandler(this.EditMenuPopup);
			// 
			// menu_Undo
			// 
			this.menu_Undo.ImageIndex = 15;
			this.menu_Undo.Images = this.toolbarImages;
			this.menu_Undo.Index = 0;
			this.menu_Undo.OwnerDraw = true;
			this.menu_Undo.Shortcut = System.Windows.Forms.Shortcut.CtrlZ;
			this.menu_Undo.Text = "&Undo";
			this.menu_Undo.Click += new System.EventHandler(this.menu_Undo_Click);
			// 
			// menu_Redo
			// 
			this.menu_Redo.ImageIndex = 16;
			this.menu_Redo.Images = this.toolbarImages;
			this.menu_Redo.Index = 1;
			this.menu_Redo.OwnerDraw = true;
			this.menu_Redo.Shortcut = System.Windows.Forms.Shortcut.CtrlY;
			this.menu_Redo.Text = "&Redo";
			this.menu_Redo.Click += new System.EventHandler(this.menu_Redo_Click);
			// 
			// menuItem26
			// 
			this.menuItem26.ImageIndex = 0;
			this.menuItem26.Images = null;
			this.menuItem26.Index = 2;
			this.menuItem26.OwnerDraw = true;
			this.menuItem26.Text = "-";
			// 
			// menu_Cut
			// 
			this.menu_Cut.ImageIndex = 4;
			this.menu_Cut.Images = this.toolbarImages;
			this.menu_Cut.Index = 3;
			this.menu_Cut.OwnerDraw = true;
			this.menu_Cut.Shortcut = System.Windows.Forms.Shortcut.CtrlX;
			this.menu_Cut.Text = "Cu&t";
			this.menu_Cut.Click += new System.EventHandler(this.menu_Cut_Click);
			// 
			// menu_Copy
			// 
			this.menu_Copy.ImageIndex = 5;
			this.menu_Copy.Images = this.toolbarImages;
			this.menu_Copy.Index = 4;
			this.menu_Copy.OwnerDraw = true;
			this.menu_Copy.Shortcut = System.Windows.Forms.Shortcut.CtrlC;
			this.menu_Copy.Text = "&Copy";
			this.menu_Copy.Click += new System.EventHandler(this.menu_Copy_Click);
			// 
			// menu_Paste
			// 
			this.menu_Paste.ImageIndex = 6;
			this.menu_Paste.Images = this.toolbarImages;
			this.menu_Paste.Index = 5;
			this.menu_Paste.OwnerDraw = true;
			this.menu_Paste.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
			this.menu_Paste.Text = "&Paste";
			this.menu_Paste.Click += new System.EventHandler(this.menu_Paste_Click);
			// 
			// menu_Delete
			// 
			this.menu_Delete.ImageIndex = 19;
			this.menu_Delete.Images = this.toolbarImages;
			this.menu_Delete.Index = 6;
			this.menu_Delete.OwnerDraw = true;
			this.menu_Delete.Shortcut = System.Windows.Forms.Shortcut.Del;
			this.menu_Delete.Text = "&Delete";
			this.menu_Delete.Click += new System.EventHandler(this.menuDeleteClick);
			// 
			// menuItem31
			// 
			this.menuItem31.ImageIndex = 0;
			this.menuItem31.Images = null;
			this.menuItem31.Index = 7;
			this.menuItem31.OwnerDraw = true;
			this.menuItem31.Text = "-";
			// 
			// menu_SelectAll
			// 
			this.menu_SelectAll.ImageIndex = 0;
			this.menu_SelectAll.Images = null;
			this.menu_SelectAll.Index = 8;
			this.menu_SelectAll.OwnerDraw = true;
			this.menu_SelectAll.Shortcut = System.Windows.Forms.Shortcut.CtrlA;
			this.menu_SelectAll.Text = "Select &All";
			this.menu_SelectAll.Click += new System.EventHandler(this.menu_SelectAll_Click);
			// 
			// menu_copyAsImage
			// 
			this.menu_copyAsImage.ImageIndex = 0;
			this.menu_copyAsImage.Images = null;
			this.menu_copyAsImage.Index = 9;
			this.menu_copyAsImage.OwnerDraw = true;
			this.menu_copyAsImage.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftC;
			this.menu_copyAsImage.Text = "Copy diagram as Image";
			this.menu_copyAsImage.Click += new System.EventHandler(this.menu_copyAsImage_Click);
			// 
			// menuItem2
			// 
			this.menuItem2.ImageIndex = 0;
			this.menuItem2.Images = null;
			this.menuItem2.Index = 2;
			this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menu_ZoomIn,
																					  this.menu_ZoomOut});
			this.menuItem2.OwnerDraw = true;
			this.menuItem2.Text = "&View";
			// 
			// menu_ZoomIn
			// 
			this.menu_ZoomIn.ImageIndex = 0;
			this.menu_ZoomIn.Images = null;
			this.menu_ZoomIn.Index = 0;
			this.menu_ZoomIn.OwnerDraw = true;
			this.menu_ZoomIn.Shortcut = System.Windows.Forms.Shortcut.CtrlJ;
			this.menu_ZoomIn.Text = "Zoom in";
			this.menu_ZoomIn.Click += new System.EventHandler(this.menu_ZoomIn_Click);
			// 
			// menu_ZoomOut
			// 
			this.menu_ZoomOut.ImageIndex = 0;
			this.menu_ZoomOut.Images = null;
			this.menu_ZoomOut.Index = 1;
			this.menu_ZoomOut.OwnerDraw = true;
			this.menu_ZoomOut.Shortcut = System.Windows.Forms.Shortcut.CtrlK;
			this.menu_ZoomOut.Text = "Zoom out";
			this.menu_ZoomOut.Click += new System.EventHandler(this.menu_ZoomOut_Click);
			// 
			// menumain_Project
			// 
			this.menumain_Project.ImageIndex = 0;
			this.menumain_Project.Images = null;
			this.menumain_Project.Index = 3;
			this.menumain_Project.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																							 this.menu_AddFiles,
																							 this.menu_AddStaticView,
																							 this.menu_Parse});
			this.menumain_Project.OwnerDraw = true;
			this.menumain_Project.Text = "&Project";
			// 
			// menu_AddFiles
			// 
			this.menu_AddFiles.ImageIndex = 20;
			this.menu_AddFiles.Images = this.toolbarImages;
			this.menu_AddFiles.Index = 0;
			this.menu_AddFiles.OwnerDraw = true;
			this.menu_AddFiles.Shortcut = System.Windows.Forms.Shortcut.CtrlF;
			this.menu_AddFiles.Text = "&Add files";
			this.menu_AddFiles.Click += new System.EventHandler(this.AddFiles);
			// 
			// menu_AddStaticView
			// 
			this.menu_AddStaticView.ImageIndex = 13;
			this.menu_AddStaticView.Images = this.toolbarImages;
			this.menu_AddStaticView.Index = 1;
			this.menu_AddStaticView.OwnerDraw = true;
			this.menu_AddStaticView.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftN;
			this.menu_AddStaticView.Text = "&Add static view";
			this.menu_AddStaticView.Click += new System.EventHandler(this.menu_AddStaticView_Click);
			// 
			// menu_Parse
			// 
			this.menu_Parse.ImageIndex = 17;
			this.menu_Parse.Images = this.toolbarImages;
			this.menu_Parse.Index = 2;
			this.menu_Parse.OwnerDraw = true;
			this.menu_Parse.Shortcut = System.Windows.Forms.Shortcut.F5;
			this.menu_Parse.Text = "Parse files, refresh tree";
			this.menu_Parse.Click += new System.EventHandler(this.RefreshProject);
			// 
			// treeImages
			// 
			this.treeImages.ImageSize = new System.Drawing.Size(16, 16);
			this.treeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("treeImages.ImageStream")));
			this.treeImages.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// toolBar1
			// 
			this.toolBar1.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(219)), ((System.Byte)(216)), ((System.Byte)(209)));
			this.toolBar1.Dock = System.Windows.Forms.DockStyle.Top;
			this.toolBar1.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(219)), ((System.Byte)(216)), ((System.Byte)(209)));
			this.toolBar1.images = this.toolbarImages;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(784, 24);
			this.toolBar1.TabIndex = 10;
			this.toolBar1.TabStop = false;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.ProjectTree);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel1.Location = new System.Drawing.Point(0, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(216, 415);
			this.panel1.TabIndex = 13;
			// 
			// ProjectTree
			// 
			this.ProjectTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProjectTree.ImageIndex = -1;
			this.ProjectTree.Location = new System.Drawing.Point(0, 0);
			this.ProjectTree.Name = "ProjectTree";
			this.ProjectTree.SelectedImageIndex = -1;
			this.ProjectTree.Size = new System.Drawing.Size(216, 415);
			this.ProjectTree.TabIndex = 2;
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point(216, 24);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(3, 415);
			this.splitter1.TabIndex = 14;
			this.splitter1.TabStop = false;
			// 
			// ViewCtrl1
			// 
			this.ViewCtrl1.AllowDrop = true;
			this.ViewCtrl1.Curr = null;
			this.ViewCtrl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ViewCtrl1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this.ViewCtrl1.Location = new System.Drawing.Point(219, 24);
			this.ViewCtrl1.Name = "ViewCtrl1";
			this.ViewCtrl1.Size = new System.Drawing.Size(565, 415);
			this.ViewCtrl1.TabIndex = 15;
			// 
			// statusBar1
			// 
			this.statusBar1.Location = new System.Drawing.Point(0, 439);
			this.statusBar1.Name = "statusBar1";
			this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																						  this.status_panel});
			this.statusBar1.ShowPanels = true;
			this.statusBar1.Size = new System.Drawing.Size(784, 18);
			this.statusBar1.TabIndex = 1;
			// 
			// status_panel
			// 
			this.status_panel.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.status_panel.Text = "Ready";
			this.status_panel.ToolTipText = "Status";
			this.status_panel.Width = 768;
			// 
			// MainWnd
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(784, 457);
			this.Controls.Add(this.ViewCtrl1);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.toolBar1);
			this.Controls.Add(this.statusBar1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Location = new System.Drawing.Point(0, 0);
			this.Menu = this.mainMenu1;
			this.Name = "MainWnd";
			this.Text = "C# UML Designer";
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.status_panel)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Main, SaveChanges, OnClosing
		
		[STAThread]
		static void Main() {
			Application.Run(new MainWnd());
		}

		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if (components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		private bool SaveChanges() {
			if( p.modified ) {
				DialogResult r = MessageBox.Show( "Project has been modified. Do you want to save changes?", "Warning!", MessageBoxButtons.YesNoCancel );
				if( r == DialogResult.Cancel )
					return false;
				if( r == DialogResult.Yes )
					p.Save( false );
			}
			return true;
		}

		protected override void OnClosing(CancelEventArgs e) {
#if !DEBUG
			if( !SaveChanges() )
				e.Cancel = true;
#endif
			base.OnClosing (e);
		}

		#endregion

		#region Menu actions

		private void menu_NewProject_Click(object sender, System.EventArgs e) {
			ToolbarAction( (int)ToolBarIcons.New );
		}
		
		private void Exit(object sender, System.EventArgs e) {
			this.Close();
		}

	
		private void SaveProject(object sender, System.EventArgs e) {
			p.Save( false );
		}

		private void SaveAsProject(object sender, System.EventArgs e) {
			p.Save( true );
		}
		
		private void LoadProject(object sender, System.EventArgs e) {
			UmlDesignerSolution q = UmlDesignerSolution.Load(this);
			if( q != null )
				TurnOnProject( q );
		}
		
		public void RefreshTitle() {
			this.Text = "C# UML Designer: " + p.name + " [" + ViewCtrl1.Curr.name + "]";
		}

		private void TurnOnProject( UmlDesignerSolution p ) {

			if( p.diagrams.Count == 0 )
				return;

			if( this.p != null )
				this.p.container = null;

			this.p = p;
			p.container = this;
			ProjectTree.NewSolution( p );
			SelectView( (GUI.View)p.diagrams[0], true );
			UpdateToolBar();
		}

		ArrayList view_toolbar_panels = null;

		public GUI.View GetCurrentView() {
			return ViewCtrl1.Curr;
		}

		public void SelectView( GUI.View v, bool update ) {
			ViewCtrl1.Curr = v;
			if( update ) {
				if( view_toolbar_panels != null )
					foreach( FlatToolBarPanel panel in view_toolbar_panels )
						toolBar1.RemovePanel( panel );
				view_toolbar_panels = v.LoadToolbars();
				RefreshTitle();
				ViewCtrl1.Invalidate();
			}
		}
		
		private void AddFiles(object sender, System.EventArgs e) {
			p.AddFile();
		}
		
		private void RefreshProject(object sender, System.EventArgs e) {
			p.Refresh();
		}

		private void menu_Print_Click(object sender, System.EventArgs e) {
			ToolbarAction( (int)ToolBarIcons.print );
		}

		private void menu_PrintPreview_Click(object sender, System.EventArgs e) {
			ToolbarAction( (int)ToolBarIcons.print_preview );
		}

		private void menu_AddStaticView_Click(object sender, System.EventArgs e) {
			UMLDes.GUI.View v = p.newStaticView();
			SelectView( v, true );
		}

		#region Image Formats

		private struct FormatDescr {
			public System.Drawing.Imaging.ImageFormat format;
			public string ext, descr;

			public FormatDescr( System.Drawing.Imaging.ImageFormat format, string ext, string descr ) {
				this.format = format;
				this.ext = ext;
				this.descr = descr;
			}
		}

		private FormatDescr[] formats = new FormatDescr[] { 
			new FormatDescr( System.Drawing.Imaging.ImageFormat.Png, "png", "PNG" ),
			new FormatDescr( System.Drawing.Imaging.ImageFormat.Bmp, "bmp", "Bitmap" ), 
			new FormatDescr( System.Drawing.Imaging.ImageFormat.Gif, "gif", "GIF" ), 
			new FormatDescr( System.Drawing.Imaging.ImageFormat.Tiff, "tif", "TIFF" ), 
			new FormatDescr( System.Drawing.Imaging.ImageFormat.Jpeg, "jpg", "JPEG" ), 
		};

		#endregion

		private void menu_SaveToImage_Click(object sender, System.EventArgs e) {
			Bitmap bmp = ViewCtrl1.PrintToImage();
			if( bmp == null ) {
				MessageBox.Show( "Diagram is empty", "Nothing to save", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}

			SaveFileDialog sfd = new SaveFileDialog();
			string filter = String.Empty;
			for( int i = 0; i < formats.Length; i++ )
				filter += "|" + formats[i].descr + "(*."+ formats[i].ext + ")|*." + formats[i].ext;
			sfd.Filter = filter.Substring(1);
			sfd.FilterIndex = 0;
			sfd.AddExtension = true;
			sfd.Title = "Save To Image...";
			sfd.ValidateNames = true;
			sfd.FileName = ViewCtrl1.Curr.name;


			if( sfd.ShowDialog( this ) == DialogResult.OK ) {
				string ext = System.IO.Path.GetExtension( sfd.FileName ).ToLower();
				System.Drawing.Imaging.ImageFormat format = null;
				for( int i = 0; i < formats.Length; i++ )
					if( ext.Equals( "." + formats[i].ext ) )
						format = formats[i].format;
				if( format != null )
					bmp.Save( sfd.FileName, format );
				else
					MessageBox.Show( "Unknown extension: " + ext, "Cannot save", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			}
		}

		private void menu_copyAsImage_Click(object sender, System.EventArgs e) {
			Bitmap bmp = ViewCtrl1.PrintToImage();
			if( bmp == null ) {
				MessageBox.Show( "Diagram is empty", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}

			System.Windows.Forms.Clipboard.SetDataObject( bmp, false );
		}

		#endregion

		#region Edit menu operations

		private void EditMenuPopup(object sender, System.EventArgs e) {
			menu_Undo.Enabled = ViewCtrl1.Curr.undo.can_undo;
			menu_Redo.Enabled = ViewCtrl1.Curr.undo.can_redo;
			menu_Delete.Enabled = ViewCtrl1.Curr.IfEnabled( UMLDes.GUI.View.EditOperation.Delete );
			menu_Cut.Enabled = ViewCtrl1.Curr.IfEnabled( UMLDes.GUI.View.EditOperation.Cut );
			menu_Copy.Enabled = ViewCtrl1.Curr.IfEnabled( UMLDes.GUI.View.EditOperation.Copy );
			menu_Paste.Enabled = ViewCtrl1.Curr.IfEnabled( UMLDes.GUI.View.EditOperation.Paste );
			menu_SelectAll.Enabled = ViewCtrl1.Curr.IfEnabled( UMLDes.GUI.View.EditOperation.SelectAll );
		}

		private void menu_Undo_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.undo.DoUndo();
		}

		private void menu_Redo_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.undo.DoRedo();
		}

		private void menu_Copy_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( UMLDes.GUI.View.EditOperation.Copy );
		}

		private void menu_Paste_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( UMLDes.GUI.View.EditOperation.Paste );
		}

		private void menu_Cut_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( UMLDes.GUI.View.EditOperation.Cut );
		}

		private void menuDeleteClick(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( UMLDes.GUI.View.EditOperation.Delete );
		}

		private void menu_SelectAll_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( UMLDes.GUI.View.EditOperation.SelectAll );
		}

		#endregion

		#region Menu on tree items

		void RenameNode( object v, EventArgs ev ) {
			TreeNode tn = (TreeNode)((FlatMenuItem)v).Tag;
			tn.BeginEdit();
		}

		void TryDropDownMenu( int x, int y, object obj, TreeNode n ) {
			MenuItem[] mi = null;

			if( obj is UMLDes.GUI.View ) {
				mi = new FlatMenuItem[] { FlatMenuItem.Create( "Rename", null, 0, false, new EventHandler(RenameNode), n ) };
			}

			if( mi == null )
				return;
			ContextMenu m = new ContextMenu( mi );
			m.Show( ProjectTree, new Point( x, y ) );
		}

		#endregion

		#region Drag & Drop and other tree ops

		Rectangle dragbox = Rectangle.Empty;
		UmlObject dragobject;
		
		void TreeMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			TreeNode node = (sender as TreeView).GetNodeAt( e.X, e.Y);
			if( node == null )
				return;
			object obj = ProjectTree.GetNodeObject( node );

			dragbox = Rectangle.Empty;
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left ) {

				if( obj is UMLDes.GUI.View ) {
					if( e.Clicks == 2 ) {
						UMLDes.GUI.View v = obj as UMLDes.GUI.View;
						SelectView( v, true );
					}

				} else if( !(obj is UmlDesignerSolution) ) {
					Size dragSize = SystemInformation.DragSize;
					dragbox = new Rectangle(new Point(e.X - (dragSize.Width /2), e.Y - (dragSize.Height /2)), dragSize);
					dragobject = obj as UmlObject;
				}
			} else if( (e.Button & MouseButtons.Right) == MouseButtons.Right ) {
				ProjectTree.SelectedNode = node;
				if( obj != null )
					TryDropDownMenu( e.X, e.Y, obj, node );
			}
		}

		void TreeMouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left ) {
				if( dragbox != Rectangle.Empty && !dragbox.Contains( e.X,e.Y) && dragobject != null ) {
					ViewCtrl1.DragObject = dragobject;
					DragDropEffects dropEffect = ((TreeView)sender).DoDragDrop( dragobject.Name, DragDropEffects.Copy );
					///....
					dragbox = Rectangle.Empty;
				}
			}
		}
		
		void TreeMouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left )
				dragbox = Rectangle.Empty;
		}

		private void BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
			object obj = ProjectTree.GetNodeObject( e.Node );
			e.CancelEdit = !( obj != null && obj is UMLDes.GUI.View );
		}

		private void AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
			object obj = ProjectTree.GetNodeObject( e.Node );
			if( obj != null && obj is UMLDes.GUI.View ) {
				UMLDes.GUI.View v = obj as UMLDes.GUI.View;
				if( e.Label != null )
					v.name = e.Label;
			}
			RefreshTitle();
		}

		private void tree_GiveFeedback(object sender, System.Windows.Forms.GiveFeedbackEventArgs e) {

			e.UseDefaultCursors = false;
			if ((e.Effect & DragDropEffects.Copy) == DragDropEffects.Copy)
				Cursor.Current = Cursors.Hand;
			else 
				Cursor.Current = Cursors.No;
		}

		#endregion

		#region Help menu

		private void Help_Popup(object sender, System.EventArgs e) {
			menu_GC_Collect.Text = "GC.Collect (" + GC.GetTotalMemory(false)/1024 + " Kb alloc)";
		}

		private void menu_GC_Collect_Click(object sender, System.EventArgs e) {
			GC.Collect();
		}
		#endregion

		#region StatusBar related

		internal void SetStatus( string text ) {
			status_panel.Text = text;
		}

		internal void SetAdvise( string text ) {
			//advise_panel.Text = text;
		}

		#endregion
	}
}
