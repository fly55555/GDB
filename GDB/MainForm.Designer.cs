
namespace GDB
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.xFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.breakPointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.registorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.localToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.watchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.callStackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disassemblyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.stepIntoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepOverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.executeTillReturnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.breakToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gDBVmwareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dCIHardwareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.localWindowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.localVEHToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rS232HyperDbgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exceptionSettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debuggerSettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.languageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBox_GDBCommand = new System.Windows.Forms.TextBox();
            this.textBox_GDBOutput = new System.Windows.Forms.TextBox();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UpdateBreakPoint = new System.Windows.Forms.Button();
            this.dataGridView_BreakPoint = new System.Windows.Forms.DataGridView();
            this.ExecuteTest = new System.Windows.Forms.Button();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.ConsoleViewPage = new System.Windows.Forms.TabPage();
            this.MemoryPage = new System.Windows.Forms.TabPage();
            this.BreakPointPage = new System.Windows.Forms.TabPage();
            this.CallStackPage = new System.Windows.Forms.TabPage();
            this.SpecialRegistersPage = new System.Windows.Forms.TabPage();
            this.TestPage = new System.Windows.Forms.TabPage();
            this.Value = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Registers = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listView_Registers = new System.Windows.Forms.ListView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listView_Disassembly = new GDB.UI.ListViewEx();
            this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Byte = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Opcode = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_BreakPoint)).BeginInit();
            this.tabControl2.SuspendLayout();
            this.BreakPointPage.SuspendLayout();
            this.TestPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.xFileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.runToolStripMenuItem,
            this.debugToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(0);
            this.menuStrip1.Size = new System.Drawing.Size(784, 24);
            this.menuStrip1.TabIndex = 6;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // xFileToolStripMenuItem
            // 
            this.xFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.xFileToolStripMenuItem.Name = "xFileToolStripMenuItem";
            this.xFileToolStripMenuItem.Size = new System.Drawing.Size(39, 24);
            this.xFileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.closeToolStripMenuItem.Text = "Close";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.breakPointsToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(42, 24);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // breakPointsToolStripMenuItem
            // 
            this.breakPointsToolStripMenuItem.Name = "breakPointsToolStripMenuItem";
            this.breakPointsToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.breakPointsToolStripMenuItem.Text = "BreakPoints";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.registorToolStripMenuItem,
            this.localToolStripMenuItem,
            this.watchToolStripMenuItem,
            this.outputToolStripMenuItem,
            this.memortToolStripMenuItem,
            this.callStackToolStripMenuItem,
            this.disassemblyToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(47, 24);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // registorToolStripMenuItem
            // 
            this.registorToolStripMenuItem.Name = "registorToolStripMenuItem";
            this.registorToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.registorToolStripMenuItem.Text = "Commands";
            // 
            // localToolStripMenuItem
            // 
            this.localToolStripMenuItem.Name = "localToolStripMenuItem";
            this.localToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.localToolStripMenuItem.Text = "Watch";
            // 
            // watchToolStripMenuItem
            // 
            this.watchToolStripMenuItem.Name = "watchToolStripMenuItem";
            this.watchToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.watchToolStripMenuItem.Text = "Locals";
            // 
            // outputToolStripMenuItem
            // 
            this.outputToolStripMenuItem.Name = "outputToolStripMenuItem";
            this.outputToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.outputToolStripMenuItem.Text = "Registers";
            // 
            // memortToolStripMenuItem
            // 
            this.memortToolStripMenuItem.Name = "memortToolStripMenuItem";
            this.memortToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.memortToolStripMenuItem.Text = "Memory";
            // 
            // callStackToolStripMenuItem
            // 
            this.callStackToolStripMenuItem.Name = "callStackToolStripMenuItem";
            this.callStackToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.callStackToolStripMenuItem.Text = "Call Stack";
            // 
            // disassemblyToolStripMenuItem
            // 
            this.disassemblyToolStripMenuItem.Name = "disassemblyToolStripMenuItem";
            this.disassemblyToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.disassemblyToolStripMenuItem.Text = "Disassembly";
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem1,
            this.stepIntoToolStripMenuItem,
            this.stepOverToolStripMenuItem,
            this.executeTillReturnToolStripMenuItem,
            this.breakToolStripMenuItem});
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(42, 24);
            this.runToolStripMenuItem.Text = "Run";
            // 
            // runToolStripMenuItem1
            // 
            this.runToolStripMenuItem1.Name = "runToolStripMenuItem1";
            this.runToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.runToolStripMenuItem1.Size = new System.Drawing.Size(272, 22);
            this.runToolStripMenuItem1.Text = "Run";
            this.runToolStripMenuItem1.Click += new System.EventHandler(this.runToolStripMenuItem1_Click);
            // 
            // stepIntoToolStripMenuItem
            // 
            this.stepIntoToolStripMenuItem.Name = "stepIntoToolStripMenuItem";
            this.stepIntoToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F11;
            this.stepIntoToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.stepIntoToolStripMenuItem.Text = "Step Into";
            this.stepIntoToolStripMenuItem.Click += new System.EventHandler(this.stepToolStripMenuItem_Click);
            // 
            // stepOverToolStripMenuItem
            // 
            this.stepOverToolStripMenuItem.Name = "stepOverToolStripMenuItem";
            this.stepOverToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F10;
            this.stepOverToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.stepOverToolStripMenuItem.Text = "Step Over";
            this.stepOverToolStripMenuItem.Click += new System.EventHandler(this.stepOverToolStripMenuItem_Click);
            // 
            // executeTillReturnToolStripMenuItem
            // 
            this.executeTillReturnToolStripMenuItem.Name = "executeTillReturnToolStripMenuItem";
            this.executeTillReturnToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F10)));
            this.executeTillReturnToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.executeTillReturnToolStripMenuItem.Text = "Execute till return";
            this.executeTillReturnToolStripMenuItem.Click += new System.EventHandler(this.executeTillReturnToolStripMenuItem_Click);
            // 
            // breakToolStripMenuItem
            // 
            this.breakToolStripMenuItem.Name = "breakToolStripMenuItem";
            this.breakToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Pause)));
            this.breakToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.breakToolStripMenuItem.Text = "Break                        ";
            this.breakToolStripMenuItem.Click += new System.EventHandler(this.breakToolStripMenuItem_Click);
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gDBVmwareToolStripMenuItem,
            this.dCIHardwareToolStripMenuItem,
            this.localWindowsToolStripMenuItem,
            this.localVEHToolStripMenuItem,
            this.rS232HyperDbgToolStripMenuItem});
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(59, 24);
            this.debugToolStripMenuItem.Text = "Debug";
            // 
            // gDBVmwareToolStripMenuItem
            // 
            this.gDBVmwareToolStripMenuItem.Name = "gDBVmwareToolStripMenuItem";
            this.gDBVmwareToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.gDBVmwareToolStripMenuItem.Text = "GDB Vmware";
            // 
            // dCIHardwareToolStripMenuItem
            // 
            this.dCIHardwareToolStripMenuItem.Name = "dCIHardwareToolStripMenuItem";
            this.dCIHardwareToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.dCIHardwareToolStripMenuItem.Text = "USB DCIHardware";
            // 
            // localWindowsToolStripMenuItem
            // 
            this.localWindowsToolStripMenuItem.Name = "localWindowsToolStripMenuItem";
            this.localWindowsToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.localWindowsToolStripMenuItem.Text = "Local Windows";
            // 
            // localVEHToolStripMenuItem
            // 
            this.localVEHToolStripMenuItem.Name = "localVEHToolStripMenuItem";
            this.localVEHToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.localVEHToolStripMenuItem.Text = "Local VEH";
            // 
            // rS232HyperDbgToolStripMenuItem
            // 
            this.rS232HyperDbgToolStripMenuItem.Name = "rS232HyperDbgToolStripMenuItem";
            this.rS232HyperDbgToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.rS232HyperDbgToolStripMenuItem.Text = "RS232 HyperDbg";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exceptionSettingToolStripMenuItem,
            this.debuggerSettingToolStripMenuItem,
            this.languageToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(66, 24);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // exceptionSettingToolStripMenuItem
            // 
            this.exceptionSettingToolStripMenuItem.Name = "exceptionSettingToolStripMenuItem";
            this.exceptionSettingToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.exceptionSettingToolStripMenuItem.Text = "Exception setting";
            // 
            // debuggerSettingToolStripMenuItem
            // 
            this.debuggerSettingToolStripMenuItem.Name = "debuggerSettingToolStripMenuItem";
            this.debuggerSettingToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.debuggerSettingToolStripMenuItem.Text = "Debugger setting";
            // 
            // languageToolStripMenuItem
            // 
            this.languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            this.languageToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.languageToolStripMenuItem.Text = "Language";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(47, 24);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // textBox_GDBCommand
            // 
            this.textBox_GDBCommand.Location = new System.Drawing.Point(23, 23);
            this.textBox_GDBCommand.Multiline = true;
            this.textBox_GDBCommand.Name = "textBox_GDBCommand";
            this.textBox_GDBCommand.Size = new System.Drawing.Size(200, 41);
            this.textBox_GDBCommand.TabIndex = 13;
            // 
            // textBox_GDBOutput
            // 
            this.textBox_GDBOutput.Location = new System.Drawing.Point(232, 23);
            this.textBox_GDBOutput.Multiline = true;
            this.textBox_GDBOutput.Name = "textBox_GDBOutput";
            this.textBox_GDBOutput.Size = new System.Drawing.Size(472, 114);
            this.textBox_GDBOutput.TabIndex = 14;
            // 
            // Column3
            // 
            this.Column3.HeaderText = "Open";
            this.Column3.Name = "Column3";
            this.Column3.Width = 50;
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Type";
            this.Column2.Name = "Column2";
            this.Column2.Width = 50;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Address";
            this.Column1.Name = "Column1";
            this.Column1.Width = 180;
            // 
            // UpdateBreakPoint
            // 
            this.UpdateBreakPoint.Location = new System.Drawing.Point(299, 97);
            this.UpdateBreakPoint.Name = "UpdateBreakPoint";
            this.UpdateBreakPoint.Size = new System.Drawing.Size(220, 40);
            this.UpdateBreakPoint.TabIndex = 15;
            this.UpdateBreakPoint.Text = "UpdateBreakPoint";
            this.UpdateBreakPoint.UseVisualStyleBackColor = true;
            // 
            // dataGridView_BreakPoint
            // 
            this.dataGridView_BreakPoint.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_BreakPoint.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3});
            this.dataGridView_BreakPoint.Location = new System.Drawing.Point(6, 6);
            this.dataGridView_BreakPoint.Name = "dataGridView_BreakPoint";
            this.dataGridView_BreakPoint.RowHeadersVisible = false;
            this.dataGridView_BreakPoint.RowTemplate.Height = 23;
            this.dataGridView_BreakPoint.Size = new System.Drawing.Size(287, 131);
            this.dataGridView_BreakPoint.TabIndex = 14;
            // 
            // ExecuteTest
            // 
            this.ExecuteTest.Location = new System.Drawing.Point(26, 84);
            this.ExecuteTest.Name = "ExecuteTest";
            this.ExecuteTest.Size = new System.Drawing.Size(200, 33);
            this.ExecuteTest.TabIndex = 15;
            this.ExecuteTest.Text = "ExecuteTest";
            this.ExecuteTest.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.ConsoleViewPage);
            this.tabControl2.Controls.Add(this.MemoryPage);
            this.tabControl2.Controls.Add(this.BreakPointPage);
            this.tabControl2.Controls.Add(this.CallStackPage);
            this.tabControl2.Controls.Add(this.SpecialRegistersPage);
            this.tabControl2.Controls.Add(this.TestPage);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(784, 133);
            this.tabControl2.TabIndex = 18;
            // 
            // ConsoleViewPage
            // 
            this.ConsoleViewPage.Location = new System.Drawing.Point(4, 23);
            this.ConsoleViewPage.Name = "ConsoleViewPage";
            this.ConsoleViewPage.Padding = new System.Windows.Forms.Padding(3);
            this.ConsoleViewPage.Size = new System.Drawing.Size(776, 106);
            this.ConsoleViewPage.TabIndex = 0;
            this.ConsoleViewPage.Text = "Console View";
            this.ConsoleViewPage.UseVisualStyleBackColor = true;
            // 
            // MemoryPage
            // 
            this.MemoryPage.Location = new System.Drawing.Point(4, 22);
            this.MemoryPage.Name = "MemoryPage";
            this.MemoryPage.Size = new System.Drawing.Size(776, 107);
            this.MemoryPage.TabIndex = 4;
            this.MemoryPage.Text = "Memory";
            this.MemoryPage.UseVisualStyleBackColor = true;
            // 
            // BreakPointPage
            // 
            this.BreakPointPage.Controls.Add(this.UpdateBreakPoint);
            this.BreakPointPage.Controls.Add(this.dataGridView_BreakPoint);
            this.BreakPointPage.Location = new System.Drawing.Point(4, 22);
            this.BreakPointPage.Name = "BreakPointPage";
            this.BreakPointPage.Padding = new System.Windows.Forms.Padding(3);
            this.BreakPointPage.Size = new System.Drawing.Size(776, 107);
            this.BreakPointPage.TabIndex = 2;
            this.BreakPointPage.Text = "BreakPoint";
            this.BreakPointPage.UseVisualStyleBackColor = true;
            // 
            // CallStackPage
            // 
            this.CallStackPage.Location = new System.Drawing.Point(4, 22);
            this.CallStackPage.Name = "CallStackPage";
            this.CallStackPage.Padding = new System.Windows.Forms.Padding(3);
            this.CallStackPage.Size = new System.Drawing.Size(776, 107);
            this.CallStackPage.TabIndex = 3;
            this.CallStackPage.Text = "CallStack";
            this.CallStackPage.UseVisualStyleBackColor = true;
            // 
            // SpecialRegistersPage
            // 
            this.SpecialRegistersPage.Location = new System.Drawing.Point(4, 22);
            this.SpecialRegistersPage.Name = "SpecialRegistersPage";
            this.SpecialRegistersPage.Size = new System.Drawing.Size(776, 107);
            this.SpecialRegistersPage.TabIndex = 5;
            this.SpecialRegistersPage.Text = "Special Registers";
            this.SpecialRegistersPage.UseVisualStyleBackColor = true;
            // 
            // TestPage
            // 
            this.TestPage.Controls.Add(this.textBox_GDBCommand);
            this.TestPage.Controls.Add(this.ExecuteTest);
            this.TestPage.Controls.Add(this.textBox_GDBOutput);
            this.TestPage.Location = new System.Drawing.Point(4, 22);
            this.TestPage.Name = "TestPage";
            this.TestPage.Padding = new System.Windows.Forms.Padding(3);
            this.TestPage.Size = new System.Drawing.Size(776, 107);
            this.TestPage.TabIndex = 1;
            this.TestPage.Text = "Test";
            this.TestPage.UseVisualStyleBackColor = true;
            // 
            // Value
            // 
            this.Value.Text = "Value";
            this.Value.Width = 135;
            // 
            // Registers
            // 
            this.Registers.Text = "Reg";
            this.Registers.Width = 40;
            // 
            // listView_Registers
            // 
            this.listView_Registers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Registers,
            this.Value});
            this.listView_Registers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Registers.FullRowSelect = true;
            this.listView_Registers.GridLines = true;
            this.listView_Registers.HideSelection = false;
            this.listView_Registers.Location = new System.Drawing.Point(0, 0);
            this.listView_Registers.Margin = new System.Windows.Forms.Padding(0);
            this.listView_Registers.Name = "listView_Registers";
            this.listView_Registers.Size = new System.Drawing.Size(200, 400);
            this.listView_Registers.TabIndex = 13;
            this.listView_Registers.UseCompatibleStateImageBehavior = false;
            this.listView_Registers.View = System.Windows.Forms.View.Details;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.listView_Registers);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.listView_Disassembly);
            this.splitContainer2.Size = new System.Drawing.Size(784, 400);
            this.splitContainer2.SplitterDistance = 200;
            this.splitContainer2.TabIndex = 0;
            // 
            // listView_Disassembly
            // 
            this.listView_Disassembly.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Address,
            this.Byte,
            this.Opcode});
            this.listView_Disassembly.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Disassembly.FullRowSelect = true;
            this.listView_Disassembly.GridLines = true;
            this.listView_Disassembly.HideSelection = false;
            this.listView_Disassembly.Location = new System.Drawing.Point(0, 0);
            this.listView_Disassembly.Name = "listView_Disassembly";
            this.listView_Disassembly.Size = new System.Drawing.Size(580, 400);
            this.listView_Disassembly.TabIndex = 14;
            this.listView_Disassembly.UseCompatibleStateImageBehavior = false;
            this.listView_Disassembly.View = System.Windows.Forms.View.Details;
            // 
            // Address
            // 
            this.Address.Text = "Address";
            this.Address.Width = 140;
            // 
            // Byte
            // 
            this.Byte.Text = "Byte";
            this.Byte.Width = 160;
            // 
            // Opcode
            // 
            this.Opcode.Text = "Opcode";
            this.Opcode.Width = 200;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl2);
            this.splitContainer1.Size = new System.Drawing.Size(784, 537);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.TabIndex = 10;
            // 
            // timer1
            // 
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WinGDB";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_BreakPoint)).EndInit();
            this.tabControl2.ResumeLayout(false);
            this.BreakPointPage.ResumeLayout(false);
            this.TestPage.ResumeLayout(false);
            this.TestPage.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem xFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem breakPointsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem registorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem localToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem watchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memortToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem callStackToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disassemblyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem stepIntoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stepOverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem executeTillReturnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem breakToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gDBVmwareToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dCIHardwareToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem localWindowsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem localVEHToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rS232HyperDbgToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exceptionSettingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debuggerSettingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem languageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.TextBox textBox_GDBCommand;
        private System.Windows.Forms.TextBox textBox_GDBOutput;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.Button UpdateBreakPoint;
        private System.Windows.Forms.DataGridView dataGridView_BreakPoint;
        private System.Windows.Forms.Button ExecuteTest;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage ConsoleViewPage;
        private System.Windows.Forms.TabPage BreakPointPage;
        private System.Windows.Forms.TabPage TestPage;
        private System.Windows.Forms.ColumnHeader Byte;
        private System.Windows.Forms.ColumnHeader Address;
        private GDB.UI.ListViewEx listView_Disassembly;
        private System.Windows.Forms.ColumnHeader Opcode;
        private System.Windows.Forms.ColumnHeader Value;
        private System.Windows.Forms.ColumnHeader Registers;
        private System.Windows.Forms.ListView listView_Registers;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabPage MemoryPage;
        private System.Windows.Forms.TabPage CallStackPage;
        private System.Windows.Forms.TabPage SpecialRegistersPage;
        private System.Windows.Forms.Timer timer1;
    }
}

