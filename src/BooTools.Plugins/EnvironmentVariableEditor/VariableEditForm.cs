using System;
using System.Windows.Forms;

namespace BooTools.Plugins.EnvironmentVariableEditor
{
    public partial class VariableEditForm : Form
    {
        public string VariableName => txtName?.Text ?? "";
        public string VariableValue => txtValue?.Text ?? "";

        public VariableEditForm(string name = "", string value = "")
        {
            InitializeComponent();
            if (txtName != null) txtName.Text = name;
            if (txtValue != null) txtValue.Text = value;
        }

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblValue = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(13, 13);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(87, 15);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Variable Name:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(16, 32);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(356, 23);
            this.txtName.TabIndex = 1;
            // 
            // lblValue
            // 
            this.lblValue.AutoSize = true;
            this.lblValue.Location = new System.Drawing.Point(13, 68);
            this.lblValue.Name = "lblValue";
            this.lblValue.Size = new System.Drawing.Size(82, 15);
            this.lblValue.TabIndex = 2;
            this.lblValue.Text = "Variable Value:";
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(16, 87);
            this.txtValue.Multiline = true;
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(356, 100);
            this.txtValue.TabIndex = 3;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(216, 205);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(297, 205);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // VariableEditForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 241);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VariableEditForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Variable";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName = null!;
        private System.Windows.Forms.TextBox txtName = null!;
        private System.Windows.Forms.Label lblValue = null!;
        private System.Windows.Forms.TextBox txtValue = null!;
        private System.Windows.Forms.Button btnOK = null!;
        private System.Windows.Forms.Button btnCancel = null!;
    }
}
