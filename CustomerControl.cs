using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class CustomerControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridCustomers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRecordPayment;

        public CustomerControl()
        {
            InitializeComponent();
            LoadCustomers();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Customer CRM Directory";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Search Panel container
            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(300, 36);
            searchPanel.Location = new Point(630, 15);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);
            this.Controls.Add(searchPanel);

            // GridView
            gridCustomers = new DataGridView();
            gridCustomers.Size = new Size(910, 505);
            gridCustomers.Location = new Point(20, 125);
            gridCustomers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCustomers);
            this.Controls.Add(gridCustomers);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Customer";
            btnAdd.Size = new Size(160, 40);
            btnAdd.Location = new Point(0, 0);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            actionPanel.Controls.Add(btnAdd);

            btnEdit = new Button();
            btnEdit.Text = "📝 Edit Selected";
            btnEdit.Size = new Size(160, 40);
            btnEdit.Location = new Point(180, 0);
            Theme.StylePrimaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;
            actionPanel.Controls.Add(btnEdit);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected";
            btnDelete.Size = new Size(160, 40);
            btnDelete.Location = new Point(360, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            btnRecordPayment = new Button();
            btnRecordPayment.Text = "💳 Record Payment";
            btnRecordPayment.Size = new Size(180, 40);
            btnRecordPayment.Location = new Point(540, 0);
            Theme.StyleSuccessButton(btnRecordPayment);
            btnRecordPayment.Click += BtnRecordPayment_Click;
            actionPanel.Controls.Add(btnRecordPayment);

            this.Controls.Add(actionPanel);
        }

        private void LoadCustomers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Name, Phone, Email, Address,
                               (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = Customers.Id), 0) -
                                ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = Customers.Id), 0)) AS [Due Balance],
                               CreatedAt as [Registered Date] 
                        FROM Customers 
                        WHERE Name LIKE @search OR Phone LIKE @search OR Address LIKE @search
                        ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridCustomers.DataSource = dt;
                        }
                    }
                }

                // Hide Id column beautifully
                if (gridCustomers.Columns["Id"] != null)
                {
                    gridCustomers.Columns["Id"].Visible = false;
                }

                // Format Due Balance column
                if (gridCustomers.Columns["Due Balance"] != null)
                {
                    gridCustomers.Columns["Due Balance"].DefaultCellStyle.Format = "N2";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (CustomerDialog dlg = new CustomerDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();
            string phone = selectedRow.Cells["Phone"].Value?.ToString() ?? "";
            string email = selectedRow.Cells["Email"].Value?.ToString() ?? "";
            string address = selectedRow.Cells["Address"].Value?.ToString() ?? "";

            using (CustomerDialog dlg = new CustomerDialog(customerId, name, phone, email, address))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string customerName = selectedRow.Cells["Name"].Value.ToString();

            if (customerName == "Walk-in Customer")
            {
                MessageBox.Show("Cannot delete the system-seeded default 'Walk-in Customer'.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal dueBalance = 0;
            if (selectedRow.Cells["Due Balance"].Value != null && selectedRow.Cells["Due Balance"].Value != DBNull.Value)
            {
                dueBalance = Convert.ToDecimal(selectedRow.Cells["Due Balance"].Value);
            }

            if (dueBalance != 0)
            {
                MessageBox.Show($"Cannot delete customer '{customerName}' because they have an active outstanding balance of Rs. {dueBalance:N2}.\nPlease clear all dues and settle accounts before deleting.", "Account Balance Settle Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete customer '{customerName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Customers WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", customerId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRecordPayment_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to record payment.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string customerName = selectedRow.Cells["Name"].Value.ToString();
            decimal currentDue = Convert.ToDecimal(selectedRow.Cells["Due Balance"].Value);

            if (customerName == "Walk-in Customer")
            {
                MessageBox.Show("Cannot record payments for 'Walk-in Customer'.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (RecordPaymentDialog dlg = new RecordPaymentDialog(customerId, customerName, currentDue))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        // Nested Customer Dialog for modal add/edit
        private class CustomerDialog : Form
        {
            private int? customerId = null;
            private TextBox txtName;
            private TextBox txtPhone;
            private TextBox txtEmail;
            private TextBox txtAddress;
            private Button btnSave;
            private Button btnCancel;

            public CustomerDialog()
            {
                InitializeComponent("Add Customer");
            }

            public CustomerDialog(int id, string name, string phone, string email, string address)
            {
                this.customerId = id;
                InitializeComponent("Edit Customer");
                txtName.Text = name;
                txtPhone.Text = phone;
                txtEmail.Text = email;
                txtAddress.Text = address;
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(400, 480); // Sets inner client area precisely
                this.AutoScaleMode = AutoScaleMode.Dpi; // Robust DPI scaling auto-resizing!
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Form header
                Label lblHeader = new Label();
                lblHeader.Text = title;
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Fields placement
                int startY = 80;
                int gapY = 70;

                // Name
                Label lblName = new Label();
                lblName.Text = "Full Name *";
                lblName.Location = new Point(20, startY);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(340, 30);
                txtName.Location = new Point(20, startY + 25);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Phone
                Label lblPhone = new Label();
                lblPhone.Text = "Phone Number";
                lblPhone.Location = new Point(20, startY + gapY);
                lblPhone.AutoSize = true;
                Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPhone);

                txtPhone = new TextBox();
                txtPhone.Size = new Size(340, 30);
                txtPhone.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtPhone);
                this.Controls.Add(txtPhone);

                // Email
                Label lblEmail = new Label();
                lblEmail.Text = "Email Address";
                lblEmail.Location = new Point(20, startY + (gapY * 2));
                lblEmail.AutoSize = true;
                Theme.StyleLabel(lblEmail, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblEmail);

                txtEmail = new TextBox();
                txtEmail.Size = new Size(340, 30);
                txtEmail.Location = new Point(20, startY + (gapY * 2) + 25);
                Theme.StyleTextBox(txtEmail);
                this.Controls.Add(txtEmail);

                // Address
                Label lblAddress = new Label();
                lblAddress.Text = "Address";
                lblAddress.Location = new Point(20, startY + (gapY * 3));
                lblAddress.AutoSize = true;
                Theme.StyleLabel(lblAddress, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAddress);

                txtAddress = new TextBox();
                txtAddress.Size = new Size(340, 30);
                txtAddress.Location = new Point(20, startY + (gapY * 3) + 25);
                Theme.StyleTextBox(txtAddress);
                this.Controls.Add(txtAddress);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Details";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 380);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 380);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtName.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string email = txtEmail.Text.Trim();
                string address = txtAddress.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(phone))
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            string checkSql = customerId == null
                                ? "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Phone <> '' AND Phone IS NOT NULL"
                                : "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Id <> @id AND Phone <> '' AND Phone IS NOT NULL";
                            using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@phone", phone);
                                if (customerId != null)
                                {
                                    cmd.Parameters.AddWithValue("@id", customerId.Value);
                                }
                                int count = (int)cmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    MessageBox.Show("A customer with this phone number is already registered.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error validating phone number: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        if (customerId == null)
                        {
                            // INSERT
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Customers (Name, Phone, Email, Address) 
                                VALUES (@name, @phone, @email, @address)", conn))
                            {
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // UPDATE
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Customers 
                                SET Name = @name, Phone = @phone, Email = @email, Address = @address 
                                WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", customerId.Value);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving customer details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Record Payment Dialog for modal collections
        private class RecordPaymentDialog : Form
        {
            private class UnpaidSaleInfo
            {
                public int Id { get; set; }
                public decimal Remaining { get; set; }
            }

            private int customerId;
            private string customerName;
            private decimal currentDue;

            private Label lblCustomerName;
            private Label lblOutstandingDue;
            private TextBox txtAmountToPay;
            private ComboBox comboPayMethod;
            private TextBox txtRemarks;
            private Button btnSave;
            private Button btnCancel;

            public RecordPaymentDialog(int customerId, string customerName, decimal currentDue)
            {
                this.customerId = customerId;
                this.customerName = customerName;
                this.currentDue = currentDue;
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                this.Text = "Record Customer Payment";
                this.ClientSize = new Size(400, 480);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "Record Due Payment";
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int startY = 80;
                int gapY = 70;

                // Customer Info
                lblCustomerName = new Label();
                lblCustomerName.Text = $"Customer: {customerName}";
                lblCustomerName.Location = new Point(20, startY);
                lblCustomerName.AutoSize = true;
                Theme.StyleLabel(lblCustomerName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCustomerName);

                // Outstanding Due
                lblOutstandingDue = new Label();
                lblOutstandingDue.Text = $"Outstanding Due: Rs. {currentDue:N2}";
                lblOutstandingDue.Location = new Point(20, startY + 25);
                lblOutstandingDue.AutoSize = true;
                Theme.StyleLabel(lblOutstandingDue, Theme.Success, Theme.BoldFont);
                this.Controls.Add(lblOutstandingDue);

                // Payment Amount
                Label lblAmount = new Label();
                lblAmount.Text = "Payment Amount (Rs.) *";
                lblAmount.Location = new Point(20, startY + gapY);
                lblAmount.AutoSize = true;
                Theme.StyleLabel(lblAmount, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAmount);

                txtAmountToPay = new TextBox();
                txtAmountToPay.Size = new Size(340, 30);
                txtAmountToPay.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtAmountToPay);
                txtAmountToPay.Text = currentDue > 0 ? currentDue.ToString("0.00") : "0.00";
                this.Controls.Add(txtAmountToPay);

                // Payment Method
                Label lblPayMethod = new Label();
                lblPayMethod.Text = "Payment Method *";
                lblPayMethod.Location = new Point(20, startY + (gapY * 2));
                lblPayMethod.AutoSize = true;
                Theme.StyleLabel(lblPayMethod, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPayMethod);

                comboPayMethod = new ComboBox();
                comboPayMethod.Size = new Size(340, 30);
                comboPayMethod.Location = new Point(20, startY + (gapY * 2) + 25);
                comboPayMethod.DropDownStyle = ComboBoxStyle.DropDownList;
                comboPayMethod.Items.AddRange(new string[] { "Cash", "Card", "QR Pay" });
                comboPayMethod.SelectedIndex = 0;
                comboPayMethod.BackColor = Theme.Primary;
                comboPayMethod.ForeColor = Theme.TextLight;
                comboPayMethod.Font = Theme.MainFont;
                this.Controls.Add(comboPayMethod);

                // Remarks
                Label lblRemarks = new Label();
                lblRemarks.Text = "Remarks";
                lblRemarks.Location = new Point(20, startY + (gapY * 3));
                lblRemarks.AutoSize = true;
                Theme.StyleLabel(lblRemarks, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblRemarks);

                txtRemarks = new TextBox();
                txtRemarks.Size = new Size(340, 30);
                txtRemarks.Location = new Point(20, startY + (gapY * 3) + 25);
                Theme.StyleTextBox(txtRemarks);
                txtRemarks.Text = "Due settlement payment";
                this.Controls.Add(txtRemarks);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Record Payment";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 390);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 390);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtAmountToPay.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                if (!decimal.TryParse(txtAmountToPay.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid payment amount greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string payMethod = comboPayMethod.SelectedItem?.ToString() ?? "Cash";
                string remarks = txtRemarks.Text.Trim();

                if (amount > currentDue)
                {
                    DialogResult confirm = MessageBox.Show($"The entered amount (Rs. {amount:N2}) is greater than the outstanding due (Rs. {currentDue:N2}).\nDo you want to proceed?", "Confirm Overpayment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes)
                    {
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // 1. Fetch customer's unpaid invoices
                        var unpaidSales = new System.Collections.Generic.List<UnpaidSaleInfo>();
                        string getUnpaidSalesSql = @"
                            SELECT s.Id, s.DueAmount,
                                   ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0) as PaidSoFar
                            FROM Sales s
                            WHERE s.CustomerId = @custId AND s.DueAmount > 0
                            ORDER BY s.SaleDate ASC, s.Id ASC";

                        using (SqlCommand cmd = new SqlCommand(getUnpaidSalesSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    int saleId = rdr.GetInt32(0);
                                    decimal due = rdr.GetDecimal(1);
                                    decimal paid = rdr.GetDecimal(2);
                                    decimal remaining = due - paid;
                                    if (remaining > 0)
                                    {
                                        unpaidSales.Add(new UnpaidSaleInfo { Id = saleId, Remaining = remaining });
                                    }
                                }
                            }
                        }

                        // 2. Allocate payment amount inside SQL Transaction
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                decimal remainingPay = amount;
                                int saleIdx = 0;

                                while (remainingPay > 0 && saleIdx < unpaidSales.Count)
                                {
                                    var activeSale = unpaidSales[saleIdx];
                                    decimal alloc = Math.Min(remainingPay, activeSale.Remaining);

                                    string insertPaySql = @"
                                        INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                        VALUES (@custId, GETDATE(), @amount, @payMethod, @remarks, @userId, @saleId)";
                                    using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@custId", customerId);
                                        cmd.Parameters.AddWithValue("@amount", alloc);
                                        cmd.Parameters.AddWithValue("@payMethod", payMethod);
                                        cmd.Parameters.AddWithValue("@remarks", remarks);
                                        cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                        cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                        cmd.ExecuteNonQuery();
                                    }

                                    remainingPay -= alloc;
                                    saleIdx++;
                                }

                                // 3. Record overpayment remainder as unlinked (SaleId = null)
                                if (remainingPay > 0)
                                {
                                    string insertPaySql = @"
                                        INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                        VALUES (@custId, GETDATE(), @amount, @payMethod, @remarks, @userId, NULL)";
                                    using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@custId", customerId);
                                        cmd.Parameters.AddWithValue("@amount", remainingPay);
                                        cmd.Parameters.AddWithValue("@payMethod", payMethod);
                                        cmd.Parameters.AddWithValue("@remarks", remarks);
                                        cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                trans.Commit();
                            }
                            catch
                            {
                                trans.Rollback();
                                throw;
                            }
                        }
                    }
                    MessageBox.Show("Payment recorded and allocated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error recording payment: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
