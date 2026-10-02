using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class SplitPaymentDialog : Form
    {
        private decimal totalPayable = 0;
        private TextBox txtCash;
        private TextBox txtOnline;
        private Label lblTotalDisplay;
        private Label lblStatus;
        private Button btnConfirm;
        private Button btnCancel;
        private Button btnHalf;
        private Button btnAllCash;
        private Button btnAllOnline;
        private bool isAutoUpdating = false;

        public decimal CashAmount { get; private set; }
        public decimal OnlineAmount { get; private set; }

        public SplitPaymentDialog(decimal total, decimal initialCash = 0, decimal initialOnline = 0)
        {
            this.totalPayable = total;
            this.CashAmount = initialCash;
            this.OnlineAmount = initialOnline;

            InitializeComponent();

            isAutoUpdating = true;
            try
            {
                if (initialCash > 0 || initialOnline > 0)
                {
                    txtCash.Text = initialCash.ToString("0.00");
                    txtOnline.Text = initialOnline.ToString("0.00");
                }
                else
                {
                    // Default to 50/50 split
                    decimal half = Math.Round(total / 2m, 2);
                    txtCash.Text = half.ToString("0.00");
                    txtOnline.Text = (total - half).ToString("0.00");
                }
            }
            finally
            {
                isAutoUpdating = false;
            }

            RecalculateSplit();

            // Auto-focus and select all text in Cash box so typing immediately replaces it
            this.Load += (s, e) =>
            {
                txtCash.Focus();
                txtCash.SelectAll();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "Split Payment Breakdown";
            this.ClientSize = new Size(420, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Primary;

            // Header Title
            Label lblHeader = new Label();
            lblHeader.Text = "🔀 Split Payment";
            lblHeader.Location = new Point(25, 18);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblHeader);

            // Total Payable Display Card
            Panel totalCard = new Panel();
            totalCard.Location = new Point(25, 52);
            totalCard.Size = new Size(370, 50);
            totalCard.BackColor = Color.FromArgb(17, 24, 39);
            totalCard.Padding = new Padding(12, 10, 12, 10);

            Label lblTotalLabel = new Label();
            lblTotalLabel.Text = "Total Bill Amount:";
            lblTotalLabel.Location = new Point(12, 14);
            lblTotalLabel.AutoSize = true;
            Theme.StyleLabel(lblTotalLabel, Theme.TextDark, Theme.BoldFont);
            totalCard.Controls.Add(lblTotalLabel);

            lblTotalDisplay = new Label();
            lblTotalDisplay.Text = $"Rs. {totalPayable:N2}";
            lblTotalDisplay.Location = new Point(160, 10);
            lblTotalDisplay.Size = new Size(198, 30);
            lblTotalDisplay.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblTotalDisplay, Color.FromArgb(249, 115, 22), new Font("Segoe UI", 15F, FontStyle.Bold));
            totalCard.Controls.Add(lblTotalDisplay);

            this.Controls.Add(totalCard);

            // Cash input
            Label lblCash = new Label();
            lblCash.Text = "💵 Cash Amount (Rs.):";
            lblCash.Location = new Point(25, 115);
            lblCash.AutoSize = true;
            Theme.StyleLabel(lblCash, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(lblCash);

            txtCash = new TextBox();
            txtCash.Location = new Point(25, 138);
            txtCash.Size = new Size(370, 30);
            txtCash.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            Theme.StyleTextBox(txtCash);

            // Auto-calculate Online amount when Cash is edited
            txtCash.TextChanged += (s, e) =>
            {
                if (isAutoUpdating) return;
                if (txtCash.Focused)
                {
                    isAutoUpdating = true;
                    try
                    {
                        if (decimal.TryParse(txtCash.Text.Trim(), out decimal cashVal))
                        {
                            decimal remaining = totalPayable - cashVal;
                            if (remaining < 0) remaining = 0;
                            txtOnline.Text = remaining.ToString("0.00");
                        }
                        else if (string.IsNullOrEmpty(txtCash.Text.Trim()))
                        {
                            txtOnline.Text = totalPayable.ToString("0.00");
                        }
                    }
                    finally
                    {
                        isAutoUpdating = false;
                    }
                }
                RecalculateSplit();
            };

            txtCash.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && btnConfirm.Enabled)
                {
                    e.SuppressKeyPress = true;
                    BtnConfirm_Click(null, null);
                }
            };
            this.Controls.Add(txtCash);

            // Online (UPI / Card) input
            Label lblOnline = new Label();
            lblOnline.Text = "📱 Online (UPI / Card) Amount (Rs.):";
            lblOnline.Location = new Point(25, 180);
            lblOnline.AutoSize = true;
            Theme.StyleLabel(lblOnline, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(lblOnline);

            txtOnline = new TextBox();
            txtOnline.Location = new Point(25, 203);
            txtOnline.Size = new Size(370, 30);
            txtOnline.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            Theme.StyleTextBox(txtOnline);

            // Auto-calculate Cash amount when Online is edited
            txtOnline.TextChanged += (s, e) =>
            {
                if (isAutoUpdating) return;
                if (txtOnline.Focused)
                {
                    isAutoUpdating = true;
                    try
                    {
                        if (decimal.TryParse(txtOnline.Text.Trim(), out decimal onlineVal))
                        {
                            decimal remaining = totalPayable - onlineVal;
                            if (remaining < 0) remaining = 0;
                            txtCash.Text = remaining.ToString("0.00");
                        }
                        else if (string.IsNullOrEmpty(txtOnline.Text.Trim()))
                        {
                            txtCash.Text = totalPayable.ToString("0.00");
                        }
                    }
                    finally
                    {
                        isAutoUpdating = false;
                    }
                }
                RecalculateSplit();
            };

            txtOnline.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && btnConfirm.Enabled)
                {
                    e.SuppressKeyPress = true;
                    BtnConfirm_Click(null, null);
                }
            };
            this.Controls.Add(txtOnline);

            // Quick split shortcuts
            FlowLayoutPanel quickPanel = new FlowLayoutPanel();
            quickPanel.Location = new Point(25, 244);
            quickPanel.Size = new Size(370, 32);
            quickPanel.FlowDirection = FlowDirection.LeftToRight;
            quickPanel.WrapContents = false;

            btnHalf = CreateQuickButton("50 / 50");
            btnHalf.Click += (s, e) =>
            {
                isAutoUpdating = true;
                try
                {
                    decimal half = Math.Round(totalPayable / 2m, 2);
                    txtCash.Text = half.ToString("0.00");
                    txtOnline.Text = (totalPayable - half).ToString("0.00");
                }
                finally
                {
                    isAutoUpdating = false;
                }
                RecalculateSplit();
            };
            quickPanel.Controls.Add(btnHalf);

            btnAllCash = CreateQuickButton("All Cash");
            btnAllCash.Click += (s, e) =>
            {
                isAutoUpdating = true;
                try
                {
                    txtCash.Text = totalPayable.ToString("0.00");
                    txtOnline.Text = "0.00";
                }
                finally
                {
                    isAutoUpdating = false;
                }
                RecalculateSplit();
            };
            quickPanel.Controls.Add(btnAllCash);

            btnAllOnline = CreateQuickButton("All Online");
            btnAllOnline.Click += (s, e) =>
            {
                isAutoUpdating = true;
                try
                {
                    txtCash.Text = "0.00";
                    txtOnline.Text = totalPayable.ToString("0.00");
                }
                finally
                {
                    isAutoUpdating = false;
                }
                RecalculateSplit();
            };
            quickPanel.Controls.Add(btnAllOnline);

            this.Controls.Add(quickPanel);

            // Status label
            lblStatus = new Label();
            lblStatus.Location = new Point(25, 280);
            lblStatus.Size = new Size(370, 24);
            lblStatus.Font = Theme.BoldFont;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblStatus);

            // Action Buttons
            btnConfirm = new Button();
            btnConfirm.Text = "✓ Confirm Split";
            btnConfirm.Size = new Size(180, 40);
            btnConfirm.Location = new Point(215, 308);
            Theme.StyleSuccessButton(btnConfirm);
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(180, 40);
            btnCancel.Location = new Point(25, 308);
            Theme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);
        }

        private Button CreateQuickButton(string text)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(116, 28);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(31, 41, 55);
            btn.ForeColor = Theme.TextLight;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void RecalculateSplit()
        {
            decimal.TryParse(txtCash.Text.Trim(), out decimal cash);
            decimal.TryParse(txtOnline.Text.Trim(), out decimal online);

            decimal splitTotal = cash + online;
            decimal diff = totalPayable - splitTotal;

            if (Math.Abs(diff) < 0.01m)
            {
                lblStatus.Text = $"✓ Perfect Match (Total: Rs. {splitTotal:N2})";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129); // Emerald
                btnConfirm.Enabled = true;
            }
            else if (diff > 0)
            {
                lblStatus.Text = $"⚠️ Underpaid: Rs. {diff:N2} remaining to allocate";
                lblStatus.ForeColor = Color.FromArgb(245, 158, 11); // Amber
                btnConfirm.Enabled = false;
            }
            else
            {
                lblStatus.Text = $"⚠️ Overpaid by: Rs. {Math.Abs(diff):N2}";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68); // Red
                btnConfirm.Enabled = false;
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            decimal.TryParse(txtCash.Text.Trim(), out decimal cash);
            decimal.TryParse(txtOnline.Text.Trim(), out decimal online);

            CashAmount = cash;
            OnlineAmount = online;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
