namespace BooTools.Plugins.EnvironmentVariableEditor
{
    partial class EnvironmentEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageUser = new System.Windows.Forms.TabPage();
            this.lvUser = new System.Windows.Forms.ListView();
            this.colUserName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUserValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageSystem = new System.Windows.Forms.TabPage();
            this.lvSystem = new System.Windows.Forms.ListView();
            this.colSystemName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSystemValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabPageUser.SuspendLayout();
            this.tabPageSystem.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabPageUser);
            this.tabControl.Controls.Add(this.tabPageSystem);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(760, 537);
            this.tabControl.TabIndex = 0;
            // 
            // tabPageUser
            // 
            this.tabPageUser.Controls.Add(this.lvUser);
            this.tabPageUser.Location = new System.Drawing.Point(4, 24);
            this.tabPageUser.Name = "tabPageUser";
            this.tabPageUser.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUser.Size = new System.Drawing.Size(752, 509);
            this.tabPageUser.TabIndex = 0;
            this.tabPageUser.Text = "用户变量";
            this.tabPageUser.UseVisualStyleBackColor = true;
            // 
            // lvUser
            // 
            this.lvUser.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colUserName,
            this.colUserValue});
            this.lvUser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvUser.FullRowSelect = true;
            this.lvUser.HideSelection = false;
            this.lvUser.Location = new System.Drawing.Point(3, 3);
            this.lvUser.Name = "lvUser";
            this.lvUser.Size = new System.Drawing.Size(746, 503);
            this.lvUser.TabIndex = 0;
            this.lvUser.UseCompatibleStateImageBehavior = false;
            this.lvUser.View = System.Windows.Forms.View.Details;
            // 
            // colUserName
            // 
            this.colUserName.Text = "变量名";
            this.colUserName.Width = 250;
            // 
            // colUserValue
            // 
            this.colUserValue.Text = "值";
            this.colUserValue.Width = 450;
            // 
            // tabPageSystem
            // 
            this.tabPageSystem.Controls.Add(this.lvSystem);
            this.tabPageSystem.Location = new System.Drawing.Point(4, 24);
            this.tabPageSystem.Name = "tabPageSystem";
            this.tabPageSystem.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSystem.Size = new System.Drawing.Size(752, 509);
            this.tabPageSystem.TabIndex = 1;
            this.tabPageSystem.Text = "系统变量";
            this.tabPageSystem.UseVisualStyleBackColor = true;
            // 
            // lvSystem
            // 
            this.lvSystem.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSystemName,
            this.colSystemValue});
            this.lvSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSystem.FullRowSelect = true;
            this.lvSystem.HideSelection = false;
            this.lvSystem.Location = new System.Drawing.Point(3, 3);
            this.lvSystem.Name = "lvSystem";
            this.lvSystem.Size = new System.Drawing.Size(746, 503);
            this.lvSystem.TabIndex = 0;
            this.lvSystem.UseCompatibleStateImageBehavior = false;
            this.lvSystem.View = System.Windows.Forms.View.Details;
            // 
            // colSystemName
            // 
            this.colSystemName.Text = "变量名";
            this.colSystemName.Width = 250;
            // 
            // colSystemValue
            // 
            this.colSystemValue.Text = "值";
            this.colSystemValue.Width = 450;
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdd.Location = new System.Drawing.Point(12, 565);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "添加(&A)";
            this.btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnEdit
            // 
            this.btnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnEdit.Location = new System.Drawing.Point(93, 565);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 2;
            this.btnEdit.Text = "编辑(&E)";
            this.btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDelete.Location = new System.Drawing.Point(174, 565);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "删除(&D)";
            this.btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(616, 565);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "保存(&S)";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(697, 565);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消(&C)";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRefresh.Location = new System.Drawing.Point(255, 565);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 6;
            this.btnRefresh.Text = "刷新(&R)";
            this.btnRefresh.UseVisualStyleBackColor = true;
            // 
            // EnvironmentEditorForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1176, 900);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.tabControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "EnvironmentEditorForm";
            this.Text = "环境变量编辑器";
            this.tabControl.ResumeLayout(false);
            this.tabPageUser.ResumeLayout(false);
            this.tabPageSystem.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageUser;
        private System.Windows.Forms.ListView lvUser;
        private System.Windows.Forms.TabPage tabPageSystem;
        private System.Windows.Forms.ListView lvSystem;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ColumnHeader colUserName;
        private System.Windows.Forms.ColumnHeader colUserValue;
        private System.Windows.Forms.ColumnHeader colSystemName;
        private System.Windows.Forms.ColumnHeader colSystemValue;
        private System.Windows.Forms.Button btnRefresh;
    }
}
