namespace EasyEPlanner.StandaloneEditor
{
    partial class StandaloneDevicesForm
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
            this.checkedListBox = new System.Windows.Forms.CheckedListBox();
            this.labelInfo = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.comboBoxTypes = new System.Windows.Forms.ComboBox();
            this.textBoxSearch = new System.Windows.Forms.TextBox();
            this.labelType = new System.Windows.Forms.Label();
            this.labelSearch = new System.Windows.Forms.Label();
            this.panelButtons.SuspendLayout();
            this.panelFilters.SuspendLayout();
            this.SuspendLayout();
            //
            // checkedListBox
            //
            this.checkedListBox.CheckOnClick = true;
            this.checkedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBox.FormattingEnabled = true;
            this.checkedListBox.Location = new System.Drawing.Point(0, 58);
            this.checkedListBox.Name = "checkedListBox";
            this.checkedListBox.Size = new System.Drawing.Size(434, 279);
            this.checkedListBox.Sorted = true;
            this.checkedListBox.TabIndex = 0;
            this.checkedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnCheckedChanged);
            //
            // labelInfo
            //
            this.labelInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelInfo.Location = new System.Drawing.Point(0, 337);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Padding = new System.Windows.Forms.Padding(4);
            this.labelInfo.Size = new System.Drawing.Size(434, 24);
            this.labelInfo.TabIndex = 1;
            this.labelInfo.Text = "Выберите устройства";
            //
            // panelButtons
            //
            this.panelButtons.Controls.Add(this.buttonCancel);
            this.panelButtons.Controls.Add(this.buttonOk);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 361);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(434, 44);
            this.panelButtons.TabIndex = 2;
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(347, 9);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Отмена";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.OnCancelClick);
            //
            // buttonOk
            //
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(266, 9);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 0;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.OnOkClick);
            //
            // panelFilters
            //
            this.panelFilters.Controls.Add(this.comboBoxTypes);
            this.panelFilters.Controls.Add(this.textBoxSearch);
            this.panelFilters.Controls.Add(this.labelType);
            this.panelFilters.Controls.Add(this.labelSearch);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Location = new System.Drawing.Point(0, 0);
            this.panelFilters.Name = "panelFilters";
            this.panelFilters.Size = new System.Drawing.Size(434, 58);
            this.panelFilters.TabIndex = 3;
            //
            // comboBoxTypes
            //
            this.comboBoxTypes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTypes.FormattingEnabled = true;
            this.comboBoxTypes.Location = new System.Drawing.Point(268, 27);
            this.comboBoxTypes.Name = "comboBoxTypes";
            this.comboBoxTypes.Size = new System.Drawing.Size(154, 21);
            this.comboBoxTypes.TabIndex = 3;
            this.comboBoxTypes.SelectedIndexChanged += new System.EventHandler(this.OnFilterChanged);
            //
            // textBoxSearch
            //
            this.textBoxSearch.Location = new System.Drawing.Point(12, 27);
            this.textBoxSearch.Name = "textBoxSearch";
            this.textBoxSearch.Size = new System.Drawing.Size(250, 20);
            this.textBoxSearch.TabIndex = 2;
            this.textBoxSearch.TextChanged += new System.EventHandler(this.OnFilterChanged);
            //
            // labelType
            //
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(265, 8);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(82, 13);
            this.labelType.TabIndex = 1;
            this.labelType.Text = "Тип устройства:";
            //
            // labelSearch
            //
            this.labelSearch.AutoSize = true;
            this.labelSearch.Location = new System.Drawing.Point(12, 8);
            this.labelSearch.Name = "labelSearch";
            this.labelSearch.Size = new System.Drawing.Size(44, 13);
            this.labelSearch.TabIndex = 0;
            this.labelSearch.Text = "Поиск:";
            //
            // StandaloneDevicesForm
            //
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(434, 405);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.checkedListBox);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 300);
            this.Name = "StandaloneDevicesForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Выбор устройств";
            this.panelButtons.ResumeLayout(false);
            this.panelFilters.ResumeLayout(false);
            this.panelFilters.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox checkedListBox;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Panel panelFilters;
        private System.Windows.Forms.ComboBox comboBoxTypes;
        private System.Windows.Forms.TextBox textBoxSearch;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.Label labelSearch;
    }
}
