using System.Windows.Forms;

namespace BooTools.Plugins.EnvironmentVariableEditor
{
    public class VariableEditForm : Form
    {
        public string VariableName => _txtName.Text;
        public string VariableValue => _txtValue.Text;

        private TextBox _txtName = null!;
        private TextBox _txtValue = null!;

        public VariableEditForm(string name = "", string value = "")
        {
            InitializeComponent();
            _txtName.Text = name;
            _txtValue.Text = value;
        }

        private void InitializeComponent()
        {
            this.Text = "编辑变量";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            
            var lblName = new Label { Text = "变量名:", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Margin = new Padding(3, 6, 3, 3) };
            _txtName = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            
            var lblValue = new Label { Text = "变量值:", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Margin = new Padding(3, 6, 3, 3) };
            _txtValue = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, Margin = new Padding(3) };

            mainTable.Controls.Add(lblName, 0, 0);
            mainTable.SetColumnSpan(lblName, 2);
            mainTable.Controls.Add(_txtName, 0, 0);
            mainTable.SetColumnSpan(_txtName, 2);
            
            mainTable.Controls.Add(lblValue, 0, 1);
            mainTable.SetColumnSpan(lblValue, 2);
            mainTable.Controls.Add(_txtValue, 0, 1);
            mainTable.SetColumnSpan(_txtValue, 2);

            var buttonFlowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            var btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(90, 30), Margin = new Padding(10, 15, 3, 3) };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(90, 30), Margin = new Padding(3, 15, 3, 3) };
            
            buttonFlowPanel.Controls.Add(btnCancel);
            buttonFlowPanel.Controls.Add(btnOK);
            mainTable.Controls.Add(buttonFlowPanel, 0, 2);
            mainTable.SetColumnSpan(buttonFlowPanel, 2);

            this.Controls.Add(mainTable);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}