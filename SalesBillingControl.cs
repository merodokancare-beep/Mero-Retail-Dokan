using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class SalesBillingControl : UserControl
    {
        private ComboBox comboCustomer;
        private TextBox txtProductCode;
        private TextBox txtQty;
        private TextBox txtPrice;
        private Label lblAvailableStock;
        private DataGridView gridCart;

        private Label lblSubTotal;
        private TextBox txtDiscount;
        private TextBox txtTax;
        private Label lblGrandTotal;
        private ComboBox comboPaymentMethod;
        private TextBox txtAmountPaid;
        private Label lblDueAmount;
        private CheckBox chkBillNotRequired;

        private Button btnAddItem;

        // Barcode scanning state
        private int currentProductId = 0;
        private string currentProductCode = "";
        private string currentProductName = "";
        private Button btnRemoveItem;
        private Button btnCheckout;
        private Button btnHold;
        private Button btnRecall;

        private DataTable cartTable;
        private decimal subTotal = 0;
        private decimal discount = 0;
        private decimal tax = 0;
        private decimal grandTotal = 0;
        private int currentSelectedStock = 0;

        // Print Document Fields
        private PrintDocument invoiceDoc;
        private PrintPreviewDialog previewDlg;
        private int lastSaleId = 0;

        public SalesBillingControl()
        {
            InitializeComponent();
            LoadDropdownData();
            InitializeCart();
            this.Load += (s, e) => txtProductCode.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Retail Sales Billing Checkout Terminal";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // LEFT PANEL: Product checkout selection
            Panel entryPanel = Theme.CreateCard(360, 520);
            entryPanel.Location = new Point(20, 65);
            entryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            Label lblEntryHeader = new Label();
            lblEntryHeader.Text = "Billing Operations";
            lblEntryHeader.Location = new Point(15, 15);
            Theme.StyleLabel(lblEntryHeader, Theme.TextLight, Theme.SubHeaderFont);
            entryPanel.Controls.Add(lblEntryHeader);

            // Customer Select
            Label lblCust = new Label();
            lblCust.Text = "Customer Name *";
            lblCust.Location = new Point(15, 55);
            lblCust.AutoSize = true;
            Theme.StyleLabel(lblCust, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblCust);

            comboCustomer = new ComboBox();
            comboCustomer.Size = new Size(330, 30);
            comboCustomer.Location = new Point(15, 78);
            comboCustomer.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCustomer.BackColor = Theme.Primary;
            comboCustomer.ForeColor = Theme.TextLight;
            comboCustomer.Font = Theme.MainFont;
            entryPanel.Controls.Add(comboCustomer);

            // Product Select
            Label lblProd = new Label();
            lblProd.Text = "Enter Product Code / Barcode *";
            lblProd.Location = new Point(15, 130);
            lblProd.AutoSize = true;
            Theme.StyleLabel(lblProd, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblProd);

            txtProductCode = new TextBox();
            txtProductCode.Size = new Size(330, 30);
            txtProductCode.Location = new Point(15, 153);
            Theme.StyleTextBox(txtProductCode);
            txtProductCode.KeyDown += TxtProductCode_KeyDown;
            entryPanel.Controls.Add(txtProductCode);

            // Live Stock Alert Info Label
            lblAvailableStock = new Label();
            lblAvailableStock.Text = "Available Stock: --";
            lblAvailableStock.Location = new Point(15, 195);
            lblAvailableStock.AutoSize = true;
            Theme.StyleLabel(lblAvailableStock, Theme.Warning, Theme.BoldFont);
            entryPanel.Controls.Add(lblAvailableStock);

            // Unit Sales Price
            Label lblPrice = new Label();
            lblPrice.Text = "Sales Unit Price (Rs.) *";
            lblPrice.Location = new Point(15, 230);
            lblPrice.AutoSize = true;
            Theme.StyleLabel(lblPrice, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblPrice);

            txtPrice = new TextBox();
            txtPrice.Size = new Size(330, 30);
            txtPrice.Location = new Point(15, 255);
            Theme.StyleTextBox(txtPrice);
            txtPrice.KeyDown += TxtQty_KeyDown;
            entryPanel.Controls.Add(txtPrice);

            // Quantity to Sell
            Label lblQty = new Label();
            lblQty.Text = "Sales Quantity *";
            lblQty.Location = new Point(15, 310);
            lblQty.AutoSize = true;
            Theme.StyleLabel(lblQty, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblQty);

            txtQty = new TextBox();
            txtQty.Size = new Size(330, 30);
            txtQty.Location = new Point(15, 335);
            Theme.StyleTextBox(txtQty);
            txtQty.Text = "1";
            txtQty.KeyDown += TxtQty_KeyDown;
            entryPanel.Controls.Add(txtQty);

            // Add Item Button
            btnAddItem = new Button();
            btnAddItem.Text = "🛒 Add to Cart";
            btnAddItem.Size = new Size(330, 45);
            btnAddItem.Location = new Point(15, 395);
            Theme.StyleSuccessButton(btnAddItem);
            btnAddItem.Click += BtnAddItem_Click;
            entryPanel.Controls.Add(btnAddItem);

            this.Controls.Add(entryPanel);

            // RIGHT PANEL: Cart Grid & Complex Live Calculator
            gridCart = new DataGridView();
            gridCart.Size = new Size(530, 280);
            gridCart.Location = new Point(400, 65);
            gridCart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCart);
            this.Controls.Add(gridCart);

            // Remove item button
            btnRemoveItem = new Button();
            btnRemoveItem.Text = "❌ Remove Item"; // Cleaner and fits perfectly in Large Font
            btnRemoveItem.Size = new Size(200, 40); // Expanded width and height to prevent text clipping
            btnRemoveItem.Location = new Point(400, 350); // Adjusted Y coordinate from 380 to 350
            btnRemoveItem.UseCompatibleTextRendering = true; // Force high-DPI text rendering compatibility
            btnRemoveItem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleDangerButton(btnRemoveItem);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            this.Controls.Add(btnRemoveItem);

            btnHold = new Button();
            btnHold.Text = "⏸️ Hold Cart";
            btnHold.Size = new Size(150, 40);
            btnHold.Location = new Point(610, 350); // Adjusted Y coordinate from 380 to 350
            btnHold.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleSecondaryButton(btnHold);
            btnHold.Click += BtnHold_Click;
            this.Controls.Add(btnHold);

            btnRecall = new Button();
            btnRecall.Text = "📂 Recall Cart";
            btnRecall.Size = new Size(160, 40);
            btnRecall.Location = new Point(770, 350); // Adjusted Y coordinate from 380 to 350
            btnRecall.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleSecondaryButton(btnRecall);
            btnRecall.Click += BtnRecall_Click;
            this.Controls.Add(btnRecall);

            // Complex live checkout panel
            Panel checkoutPanel = Theme.CreateCard(530, 230); // Expanded height from 195 to 230
            checkoutPanel.Location = new Point(400, 395); // Shuffled up from 425 to 395
            checkoutPanel.BackColor = Color.FromArgb(17, 24, 39);
            checkoutPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // SubTotal
            Label lblSub = new Label();
            lblSub.Text = "SubTotal:";
            lblSub.Location = new Point(15, 15);
            Theme.StyleLabel(lblSub, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblSub);

            lblSubTotal = new Label();
            lblSubTotal.Text = "Rs. 0.00";
            lblSubTotal.Location = new Point(150, 15); // Shifted from 120 to 150 to prevent overlap
            lblSubTotal.AutoSize = true;
            Theme.StyleLabel(lblSubTotal, Theme.TextLight, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblSubTotal);

            // Discount Input
            Label lblDisc = new Label();
            lblDisc.Text = "Discount (Rs.):";
            lblDisc.Location = new Point(15, 50);
            Theme.StyleLabel(lblDisc, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDisc);

            txtDiscount = new TextBox();
            txtDiscount.Size = new Size(90, 24); // Slightly reduced width to fit beautifully
            txtDiscount.Location = new Point(150, 47); // Shifted from 120 to 150
            Theme.StyleTextBox(txtDiscount);
            txtDiscount.Text = "0.00";
            txtDiscount.TextChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(txtDiscount);

            // Tax (SGST & IGST %) Input
            Label lblTx = new Label();
            lblTx.Text = "SGST & IGST (%):";
            lblTx.Location = new Point(15, 85);
            Theme.StyleLabel(lblTx, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblTx);

            txtTax = new TextBox();
            txtTax.Size = new Size(90, 24); // Adjusted width
            txtTax.Location = new Point(150, 82); // Shifted from 120 to 150
            Theme.StyleTextBox(txtTax);
            txtTax.Text = "0";
            txtTax.TextChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(txtTax);

            // Payment Method
            Label lblPay = new Label();
            lblPay.Text = "Payment Mode:";
            lblPay.Location = new Point(15, 120);
            Theme.StyleLabel(lblPay, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblPay);

            comboPaymentMethod = new ComboBox();
            comboPaymentMethod.Size = new Size(90, 28); // Adjusted width
            comboPaymentMethod.Location = new Point(150, 117); // Shifted from 120 to 150
            comboPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPaymentMethod.Items.AddRange(new string[] { "Cash", "Card", "QR Pay" });
            comboPaymentMethod.SelectedIndex = 0;
            comboPaymentMethod.BackColor = Theme.Primary;
            comboPaymentMethod.ForeColor = Theme.TextLight;
            comboPaymentMethod.Font = Theme.MainFont;
            checkoutPanel.Controls.Add(comboPaymentMethod);

            // Amount Paid
            Label lblAmtPaid = new Label();
            lblAmtPaid.Text = "Amount Paid (Rs.):";
            lblAmtPaid.Location = new Point(15, 155);
            Theme.StyleLabel(lblAmtPaid, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblAmtPaid);

            txtAmountPaid = new TextBox();
            txtAmountPaid.Size = new Size(90, 24);
            txtAmountPaid.Location = new Point(150, 152);
            Theme.StyleTextBox(txtAmountPaid);
            txtAmountPaid.Text = "0.00";
            txtAmountPaid.TextChanged += TxtAmountPaid_TextChanged;
            checkoutPanel.Controls.Add(txtAmountPaid);

            // Due Amount
            Label lblDue = new Label();
            lblDue.Text = "Due Amount:";
            lblDue.Location = new Point(15, 190);
            Theme.StyleLabel(lblDue, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDue);

            lblDueAmount = new Label();
            lblDueAmount.Text = "Rs. 0.00";
            lblDueAmount.Location = new Point(150, 190);
            lblDueAmount.AutoSize = true;
            Theme.StyleLabel(lblDueAmount, Theme.Warning, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDueAmount);

            // Divider vertical
            Panel div = new Panel();
            div.Size = new Size(1, 200); // Expanded from 165 to 200
            div.Location = new Point(255, 15); // Shifted slightly to center perfectly
            div.BackColor = Theme.Secondary;
            checkoutPanel.Controls.Add(div);

            // Grand Total (Big display)
            Label lblGrand = new Label();
            lblGrand.Text = "GRAND TOTAL";
            lblGrand.Location = new Point(270, 15);
            lblGrand.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblGrand, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            checkoutPanel.Controls.Add(lblGrand);

            lblGrandTotal = new Label();
            lblGrandTotal.Text = "Rs. 0.00";
            lblGrandTotal.Location = new Point(270, 35);
            lblGrandTotal.AutoSize = true;
            lblGrandTotal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblGrandTotal, Theme.Success, new Font("Segoe UI", 22F, FontStyle.Bold));
            checkoutPanel.Controls.Add(lblGrandTotal);

            // Bill Not Required Checkbox
            chkBillNotRequired = new CheckBox();
            chkBillNotRequired.Text = "Bill Not Required";
            chkBillNotRequired.Location = new Point(285, 115);
            chkBillNotRequired.AutoSize = true;
            chkBillNotRequired.Checked = true;
            chkBillNotRequired.ForeColor = Theme.TextLight;
            chkBillNotRequired.BackColor = Color.Transparent;
            chkBillNotRequired.Font = Theme.BoldFont;
            chkBillNotRequired.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkoutPanel.Controls.Add(chkBillNotRequired);

            // Checkout Button
            btnCheckout = new Button();
            btnCheckout.Text = "🖨️ Checkout & Print"; // Mixed-case fits much better under scaling
            btnCheckout.Size = new Size(245, 55);
            btnCheckout.Location = new Point(270, 140); // Shifted down from 105 to 140 to balance the taller layout
            btnCheckout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StylePrimaryButton(btnCheckout);
            btnCheckout.Click += BtnCheckout_Click;
            checkoutPanel.Controls.Add(btnCheckout);

            this.Controls.Add(checkoutPanel);

            // Setup Print Elements
            invoiceDoc = new PrintDocument();
            invoiceDoc.PrintPage += InvoiceDoc_PrintPage;
            previewDlg = new PrintPreviewDialog();
            previewDlg.Document = invoiceDoc;
            previewDlg.Size = new Size(600, 700);
        }

        private void InitializeCart()
        {
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductId", typeof(int));
            cartTable.Columns.Add("Code", typeof(string));
            cartTable.Columns.Add("Name", typeof(string));
            cartTable.Columns.Add("Qty", typeof(int));
            cartTable.Columns.Add("Price", typeof(decimal));
            cartTable.Columns.Add("Total", typeof(decimal));

            gridCart.DataSource = cartTable;
            
            if (gridCart.Columns["ProductId"] != null) gridCart.Columns["ProductId"].Visible = false;

            // Enable inline editing for Qty column in the grid
            gridCart.ReadOnly = false;
            foreach (DataGridViewColumn col in gridCart.Columns)
            {
                if (col.Name != "Qty")
                {
                    col.ReadOnly = true;
                }
                else
                {
                    col.ReadOnly = false;
                }
            }

            // Set Header Texts and Formatting
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].HeaderText = "Code";
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].HeaderText = "Product Name";
            if (gridCart.Columns["Qty"] != null) gridCart.Columns["Qty"].HeaderText = "Qty";
            if (gridCart.Columns["Price"] != null)
            {
                gridCart.Columns["Price"].HeaderText = "Price";
                gridCart.Columns["Price"].DefaultCellStyle.Format = "N2";
            }
            if (gridCart.Columns["Total"] != null)
            {
                gridCart.Columns["Total"].HeaderText = "Total";
                gridCart.Columns["Total"].DefaultCellStyle.Format = "N2";
            }

            // Set custom fill weights for beautiful proportional layout
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].FillWeight = 50;
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].FillWeight = 180;
            if (gridCart.Columns["Qty"] != null) gridCart.Columns["Qty"].FillWeight = 60;
            if (gridCart.Columns["Price"] != null) gridCart.Columns["Price"].FillWeight = 70;
            if (gridCart.Columns["Total"] != null) gridCart.Columns["Total"].FillWeight = 70;

            gridCart.CellValueChanged += GridCart_CellValueChanged;
            gridCart.CellValidating += GridCart_CellValidating;
        }

        private void GridCart_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (gridCart.Columns[e.ColumnIndex].Name == "Qty")
            {
                string input = e.FormattedValue.ToString();
                if (!int.TryParse(input, out int newQty) || newQty <= 0)
                {
                    MessageBox.Show("Please enter a valid positive quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                int prodId = Convert.ToInt32(gridCart.Rows[e.RowIndex].Cells["ProductId"].Value);
                
                int dbStock = 0;
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Stock FROM Products WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", prodId);
                            dbStock = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error during validation: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                    return;
                }

                if (newQty > dbStock)
                {
                    MessageBox.Show($"Insufficient stock! Active inventory only has {dbStock} unit(s) of this item.", "Out of Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void GridCart_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridCart.Columns[e.ColumnIndex].Name == "Qty")
            {
                DataGridViewRow row = gridCart.Rows[e.RowIndex];
                int qty = Convert.ToInt32(row.Cells["Qty"].Value);
                decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
                
                row.Cells["Total"].Value = qty * price;

                CalculateCheckoutTotals();

                int prodId = Convert.ToInt32(row.Cells["ProductId"].Value);
                if (prodId == currentProductId)
                {
                    RefreshAvailableStockDisplay();
                }
            }
        }

        private void LoadDropdownData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Customers
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Customers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            comboCustomer.DataSource = dt;
                            comboCustomer.DisplayMember = "Name";
                            comboCustomer.ValueMember = "Id";

                            // Set default to Walk-in Customer if exists
                            for (int i = 0; i < comboCustomer.Items.Count; i++)
                            {
                                DataRowView drv = comboCustomer.Items[i] as DataRowView;
                                if (drv["Name"].ToString() == "Walk-in Customer")
                                {
                                    comboCustomer.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading checkout directories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtProductCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadProductByCode(txtProductCode.Text);
            }
        }

        private void TxtQty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnAddItem_Click(null, null);
            }
        }

        private void LoadProductByCode(string code)
        {
            code = code.Trim();
            if (string.IsNullOrEmpty(code)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code, Name, SalesPrice, Stock FROM Products WHERE Code = @code", conn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                currentProductId = Convert.ToInt32(r["Id"]);
                                currentProductCode = r["Code"].ToString();
                                currentProductName = r["Name"].ToString();
                                txtPrice.Text = Convert.ToDecimal(r["SalesPrice"]).ToString("0.00");
                                currentSelectedStock = Convert.ToInt32(r["Stock"]);
                                
                                RefreshAvailableStockDisplay();

                                // Automatically add the scanned product to the cart
                                BtnAddItem_Click(null, null);
                            }
                            else
                            {
                                lblAvailableStock.Text = "Product not found!";
                                lblAvailableStock.ForeColor = Theme.Danger;
                                currentProductId = 0;
                                currentProductCode = "";
                                currentProductName = "";
                                txtPrice.Text = "0.00";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshAvailableStockDisplay()
        {
            if (currentProductId == 0)
            {
                lblAvailableStock.Text = "Available Stock: --";
                lblAvailableStock.ForeColor = Theme.Warning;
                return;
            }

            int cartQty = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == currentProductId)
                {
                    cartQty = (int)row["Qty"];
                    break;
                }
            }
            int available = currentSelectedStock - cartQty;
            lblAvailableStock.Text = $"Available Stock: {available} unit(s)";
            
            if (available <= 0)
            {
                lblAvailableStock.ForeColor = Theme.Danger;
            }
            else if (available < 5)
            {
                lblAvailableStock.ForeColor = Theme.Warning;
            }
            else
            {
                lblAvailableStock.ForeColor = Theme.Success;
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (currentProductId == 0)
            {
                MessageBox.Show("Please scan or enter a valid product code first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int prodId = currentProductId;
            if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid sales price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Real-time Stock Check
            int cartQty = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == prodId)
                {
                    cartQty = (int)row["Qty"];
                    break;
                }
            }

            if ((cartQty + qty) > currentSelectedStock)
            {
                MessageBox.Show($"Insufficient stock! You only have {currentSelectedStock} unit(s) in inventory. Cart currently holds {cartQty} unit(s).", "Out of Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string code = currentProductCode;
            string name = currentProductName;

            bool found = false;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == prodId)
                {
                    row["Qty"] = (int)row["Qty"] + qty;
                    row["Price"] = price;
                    row["Total"] = (int)row["Qty"] * price;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                cartTable.Rows.Add(prodId, code, name, qty, price, qty * price);
            }

            CalculateCheckoutTotals();

            // Clear input fields for next barcode scan
            txtProductCode.Clear();
            txtPrice.Text = "0.00";
            txtQty.Text = "1";
            currentProductId = 0;
            currentProductCode = "";
            currentProductName = "";
            lblAvailableStock.Text = "Available Stock: --";
            lblAvailableStock.ForeColor = Theme.Warning;
            txtProductCode.Focus();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (gridCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a checkout cart item to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            gridCart.Rows.Remove(gridCart.SelectedRows[0]);
            CalculateCheckoutTotals();
            RefreshAvailableStockDisplay();
        }

        private void TxtAmountPaid_TextChanged(object sender, EventArgs e)
        {
            RecalculateDue();
        }

        private void RecalculateDue()
        {
            decimal amountPaid = 0;
            if (decimal.TryParse(txtAmountPaid.Text.Trim(), out decimal parsedPaid))
            {
                amountPaid = parsedPaid;
            }

            decimal dueAmount = grandTotal - amountPaid;
            if (dueAmount < 0) dueAmount = 0;
            lblDueAmount.Text = $"Rs. {dueAmount:N2}";
        }

        private void CalculatorInput_Changed(object sender, EventArgs e)
        {
            decimal.TryParse(txtDiscount.Text.Trim(), out discount);
            
            decimal taxPercent = 0;
            if (decimal.TryParse(txtTax.Text.Trim(), out taxPercent))
            {
                tax = subTotal * (taxPercent / 100m);
            }
            else
            {
                tax = 0;
            }
            
            grandTotal = (subTotal - discount) + tax;
            if (grandTotal < 0) grandTotal = 0;

            lblGrandTotal.Text = $"Rs. {grandTotal:N2}";

            if (txtAmountPaid != null && !txtAmountPaid.Focused)
            {
                txtAmountPaid.Text = grandTotal.ToString("0.00");
            }
            RecalculateDue();
        }

        private void CalculateCheckoutTotals()
        {
            subTotal = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                subTotal += Convert.ToDecimal(row["Total"]);
            }
            lblSubTotal.Text = $"Rs. {subTotal:N2}";

            // Live calculate tax using the user-defined percentage when cart contents change
            CalculatorInput_Changed(null, null);
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (comboCustomer.SelectedValue == null)
            {
                MessageBox.Show("Please select a Customer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Billing cart is empty. Please add items to checkout.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal amountPaid = 0;
            if (!decimal.TryParse(txtAmountPaid.Text.Trim(), out amountPaid) || amountPaid < 0)
            {
                MessageBox.Show("Please enter a valid amount paid.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal dueAmount = grandTotal - amountPaid;
            if (dueAmount < 0) dueAmount = 0;

            if (dueAmount > 0 && comboCustomer.Text == "Walk-in Customer")
            {
                var confirmResult = MessageBox.Show(
                    "Dues cannot be registered for 'Walk-in Customer'.\nWould you like to register this customer now to proceed with credit sale?",
                    "Register Customer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes)
                {
                    return;
                }

                using (QuickCustomerRegistrationDialog regDlg = new QuickCustomerRegistrationDialog())
                {
                    if (regDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        string newCustName = regDlg.CustomerName;
                        string newCustPhone = regDlg.CustomerPhone;

                        try
                        {
                            int newCustomerId = 0;
                            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                            {
                                conn.Open();
                                string insertSql = @"
                                    INSERT INTO Customers (Name, Phone, Email, Address) 
                                    OUTPUT INSERTED.Id
                                    VALUES (@name, @phone, '', '')";
                                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@name", newCustName);
                                    cmd.Parameters.AddWithValue("@phone", newCustPhone);
                                    newCustomerId = (int)cmd.ExecuteScalar();
                                }
                            }

                            // Reload customer dropdown
                            LoadDropdownData();

                            // Select the newly added customer
                            comboCustomer.SelectedValue = newCustomerId;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to register customer: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        // User canceled registration dialog
                        return;
                    }
                }
            }

            int customerId = (int)comboCustomer.SelectedValue;
            string paymentMode = comboPaymentMethod.SelectedItem?.ToString() ?? "Cash";
            string invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insert Sales Header
                    int saleId = 0;
                    string headerSql = @"
                        INSERT INTO Sales (InvoiceNumber, CustomerId, SaleDate, SubTotal, Discount, Tax, GrandTotal, AmountPaid, DueAmount, PaymentMethod, CreatedBy) 
                        OUTPUT INSERTED.Id
                        VALUES (@invNum, @custId, GETDATE(), @sub, @disc, @tax, @grand, @paid, @due, @pay, @user)";

                    using (SqlCommand cmd = new SqlCommand(headerSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        cmd.Parameters.AddWithValue("@custId", customerId);
                        cmd.Parameters.AddWithValue("@sub", subTotal);
                        cmd.Parameters.AddWithValue("@disc", discount);
                        cmd.Parameters.AddWithValue("@tax", tax);
                        cmd.Parameters.AddWithValue("@grand", grandTotal);
                        cmd.Parameters.AddWithValue("@paid", amountPaid);
                        cmd.Parameters.AddWithValue("@due", dueAmount);
                        cmd.Parameters.AddWithValue("@pay", paymentMode);
                        cmd.Parameters.AddWithValue("@user", Session.UserId);
                        saleId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Insert Sale Details and Decrement Stock
                    foreach (DataRow row in cartTable.Rows)
                    {
                        int prodId = (int)row["ProductId"];
                        int qty = (int)row["Qty"];
                        decimal unitPrice = (decimal)row["Price"];
                        decimal total = (decimal)row["Total"];

                        // Details Insert (archiving the exact current purchase cost to protect reports)
                        string detailsSql = @"
                            INSERT INTO SaleDetails (SaleId, ProductId, Quantity, UnitPrice, Total, PurchaseCostAtSale) 
                            SELECT @saleId, @prodId, @qty, @price, @total, PurchasePrice 
                            FROM Products 
                            WHERE Id = @prodId";

                        using (SqlCommand cmd = new SqlCommand(detailsSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.Parameters.AddWithValue("@prodId", prodId);
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@price", unitPrice);
                            cmd.Parameters.AddWithValue("@total", total);
                            cmd.ExecuteNonQuery();
                        }

                        // Decrement Product Stock Level
                        string stockSql = @"
                            UPDATE Products 
                            SET Stock = Stock - @qty 
                            WHERE Id = @id";

                        using (SqlCommand cmd = new SqlCommand(stockSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@id", prodId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    
                    lastSaleId = saleId;
                    MessageBox.Show($"Sale transaction completed successfully!\nInvoice No: {invoiceNumber}", "Checkout Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Launch Print Preview Dialog if Bill Not Required is unchecked!
                    if (!chkBillNotRequired.Checked)
                    {
                        previewDlg.ShowDialog();
                    }

                    // Clear Screen
                    cartTable.Clear();
                    txtDiscount.Text = "0.00";
                    txtTax.Text = "0";
                    CalculateCheckoutTotals();
                    
                    txtProductCode.Clear();
                    txtPrice.Text = "0.00";
                    txtQty.Text = "1";
                    currentProductId = 0;
                    currentProductCode = "";
                    currentProductName = "";
                    lblAvailableStock.Text = "Available Stock: --";
                    lblAvailableStock.ForeColor = Theme.Warning;
                    txtProductCode.Focus();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Failed to checkout: {ex.Message}", "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Print Invoice Subsystem Drawing (GDI+ style)
        private void InvoiceDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int startX = 50;
            int startY = 50;

            Font fTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
            Font fSubTitle = new Font("Segoe UI", 9F, FontStyle.Italic);
            Font fRegular = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font fBold = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            Brush bDark = new SolidBrush(Color.Black);
            Pen pLine = new Pen(Color.Gray, 1);

            // Fetch checkout details from database dynamically for print
            string invNum = "", custName = "", custPhone = "", custAddr = "", dateStr = "", paymentMode = "";
            decimal sub = 0, disc = 0, tx = 0, grand = 0, amountPaidVal = 0, dueAmountVal = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.InvoiceNumber, s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, s.PaymentMethod,
                               c.Name, c.Phone, c.Address, s.AmountPaid, s.DueAmount 
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE s.Id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", lastSaleId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                invNum = r.GetString(0);
                                dateStr = r.GetDateTime(1).ToString("yyyy-MM-dd HH:mm");
                                sub = r.GetDecimal(2);
                                disc = r.GetDecimal(3);
                                tx = r.GetDecimal(4);
                                grand = r.GetDecimal(5);
                                paymentMode = r.GetString(6);
                                custName = r.GetString(7);
                                custPhone = r.IsDBNull(8) ? "" : r.GetString(8);
                                custAddr = r.IsDBNull(9) ? "" : r.GetString(9);
                                amountPaidVal = r.GetDecimal(10);
                                dueAmountVal = r.GetDecimal(11);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"Database Error Rendering Print: {ex.Message}", fRegular, bDark, startX, startY);
                return;
            }

            // Fetch profile settings dynamically for branding
            string shopName = "Mero Dokan Shop", shopPhone = "+977-1-4200000", shopEmail = "contact@merodokan.com", shopAddress = "Kathmandu, Nepal", logoPath = "", shopGSTIN = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, Phone, Email, Address, LogoPath, GSTIN FROM AppProfile", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                shopName = rdr["ShopName"].ToString();
                                shopPhone = rdr["Phone"].ToString();
                                shopEmail = rdr["Email"].ToString();
                                shopAddress = rdr["Address"].ToString();
                                logoPath = rdr["LogoPath"]?.ToString();
                                shopGSTIN = rdr["GSTIN"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch { }

            // Header Section
            int textShiftX = 0;
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    using (Image logo = Image.FromFile(logoPath))
                    {
                        g.DrawImage(logo, startX, startY - 10, 60, 60);
                        textShiftX = 75;
                    }
                }
                catch { }
            }

            g.DrawString(shopName, fTitle, bDark, startX + textShiftX, startY);
            g.DrawString($"{shopAddress} | Phone: {shopPhone} | Email: {shopEmail}", fSubTitle, bDark, startX + textShiftX, startY + 30);
            
            int headerOffset = 0;
            if (!string.IsNullOrEmpty(shopGSTIN))
            {
                g.DrawString($"GSTIN: {shopGSTIN}", fSubTitle, bDark, startX + textShiftX, startY + 48);
                headerOffset = 20;
            }

            // Draw scannable invoice number QR Code
            BarcodeHelper.DrawQRCode(g, invNum, 660, startY - 12, 60);

            g.DrawLine(pLine, startX, startY + 50 + headerOffset, 750, startY + 50 + headerOffset);

            // Customer Info Block
            g.DrawString($"Invoice No:  {invNum}", fBold, bDark, startX, startY + 65 + headerOffset);
            g.DrawString($"Invoice Date: {dateStr}", fRegular, bDark, 480, startY + 65 + headerOffset);
            
            g.DrawString($"Bill To:     {custName}", fRegular, bDark, startX, startY + 90 + headerOffset);
            g.DrawString($"Address:     {custAddr}", fRegular, bDark, startX, startY + 110 + headerOffset);
            g.DrawString($"Phone No:    {custPhone}", fRegular, bDark, startX, startY + 130 + headerOffset);

            g.DrawLine(pLine, startX, startY + 160 + headerOffset, 750, startY + 160 + headerOffset);

            // Table Headers
            int col1 = startX;
            int col2 = startX + 220;
            int col3 = startX + 350;
            int col4 = startX + 480;
            int col5 = startX + 600;

            int rowY = startY + 175 + headerOffset;
            g.DrawString("Product / Description", fBold, bDark, col1, rowY);
            g.DrawString("Qty Sold", fBold, bDark, col3, rowY);
            g.DrawString("Rate", fBold, bDark, col4, rowY);
            g.DrawString("Total Cost", fBold, bDark, col5, rowY);

            g.DrawLine(pLine, startX, rowY + 25, 750, rowY + 25);
            rowY += 35;

            // Render Items
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string detailsQuery = @"
                        SELECT p.Name, sd.Quantity, sd.UnitPrice, sd.Total 
                        FROM SaleDetails sd
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        WHERE sd.SaleId = @id";

                    using (SqlCommand cmd = new SqlCommand(detailsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", lastSaleId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string pName = r.GetString(0);
                                int qty = r.GetInt32(1);
                                decimal rate = r.GetDecimal(2);
                                decimal total = r.GetDecimal(3);

                                g.DrawString(pName, fRegular, bDark, col1, rowY);
                                g.DrawString(qty.ToString(), fRegular, bDark, col3, rowY);
                                g.DrawString($"Rs. {rate:F2}", fRegular, bDark, col4, rowY);
                                g.DrawString($"Rs. {total:F2}", fRegular, bDark, col5, rowY);

                                rowY += 25;
                            }
                        }
                    }
                }
            }
            catch { }

            g.DrawLine(pLine, startX, rowY + 10, 750, rowY + 10);
            rowY += 25;

            // Summary Totals
            int summaryX = col4 - 40;
            g.DrawString("Sub Total:", fRegular, bDark, summaryX, rowY);
            g.DrawString($"Rs. {sub:N2}", fRegular, bDark, col5, rowY);
            rowY += 20;

            g.DrawString("Discount Amount:", fRegular, bDark, summaryX, rowY);
            g.DrawString($"- Rs. {disc:N2}", fRegular, bDark, col5, rowY);
            rowY += 20;

            decimal taxPercent = sub > 0 ? (tx / sub) * 100m : 0m;
            g.DrawString($"SGST & IGST ({taxPercent:0.##}%):", fRegular, bDark, summaryX, rowY);
            g.DrawString($"Rs. {tx:N2}", fRegular, bDark, col5, rowY);
            rowY += 25;

            g.DrawLine(pLine, summaryX, rowY - 5, 750, rowY - 5);

            g.DrawString("GRAND TOTAL:", fBold, bDark, summaryX, rowY);
            g.DrawString($"Rs. {grand:N2}", fBold, bDark, col5, rowY);
            rowY += 20;

            g.DrawString("Amount Paid:", fRegular, bDark, summaryX, rowY);
            g.DrawString($"Rs. {amountPaidVal:N2}", fRegular, bDark, col5, rowY);
            rowY += 20;

            if (dueAmountVal > 0)
            {
                g.DrawString("Due Amount:", fBold, bDark, summaryX, rowY);
                g.DrawString($"Rs. {dueAmountVal:N2}", fBold, bDark, col5, rowY);
                rowY += 20;
            }
            rowY += 10;

            g.DrawString($"Payment Mode: {paymentMode}", fBold, bDark, startX, rowY - 20);

            g.DrawLine(pLine, startX, rowY + 10, 750, rowY + 10);
            rowY += 30;

            // Footer Message
            g.DrawString("Thank you for shopping at Mero Dokan! Please visit us again.", fBold, bDark, startX + 130, rowY);
        }

        private void BtnHold_Click(object sender, EventArgs e)
        {
            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Billing cart is empty. Please add items to put on hold.", "Hold Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int customerId = comboCustomer.SelectedValue != null ? (int)comboCustomer.SelectedValue : 0;
            string customerName = comboCustomer.Text;

            // Clone and copy cart contents
            DataTable cartCopy = cartTable.Copy();

            HeldTransaction transaction = new HeldTransaction
            {
                Id = $"HT-{DateTime.Now:yyyyMMddHHmmss}",
                CustomerId = customerId,
                CustomerName = string.IsNullOrEmpty(customerName) ? "Walk-in Customer" : customerName,
                Discount = discount,
                TaxPercentText = txtTax.Text,
                CartItems = cartCopy,
                HoldTime = DateTime.Now,
                GrandTotal = grandTotal
            };

            HeldTransactions.Add(transaction);

            // Clear Cart and Inputs
            cartTable.Clear();
            txtDiscount.Text = "0.00";
            txtTax.Text = "0";
            CalculateCheckoutTotals();

            // Clear left panel search/product fields
            txtProductCode.Clear();
            txtPrice.Text = "0.00";
            txtQty.Text = "1";
            currentProductId = 0;
            currentProductCode = "";
            currentProductName = "";
            lblAvailableStock.Text = "Available Stock: --";
            lblAvailableStock.ForeColor = Theme.Warning;

            MessageBox.Show("Transaction put on hold successfully.", "Transaction Held", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtProductCode.Focus();
        }

        private void BtnRecall_Click(object sender, EventArgs e)
        {
            if (HeldTransactions.Count == 0)
            {
                MessageBox.Show("No transactions are currently on hold.", "Recall Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (RecallDialog recallDlg = new RecallDialog(HeldTransactions))
            {
                if (recallDlg.ShowDialog(this) == DialogResult.OK)
                {
                    HeldTransaction selected = recallDlg.SelectedTransaction;
                    if (selected == null) return;

                    if (recallDlg.IsDeleteRequest)
                    {
                        var confirm = MessageBox.Show("Are you sure you want to delete this held transaction?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm == DialogResult.Yes)
                        {
                            HeldTransactions.Remove(selected);
                            MessageBox.Show("Held transaction deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // Overwrite active cart validation
                        if (cartTable.Rows.Count > 0)
                        {
                            var overwrite = MessageBox.Show("Active cart is not empty. Recalling will overwrite current cart contents.\nDo you want to proceed?", "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (overwrite != DialogResult.Yes)
                            {
                                return;
                            }
                        }

                        // Restore Customer
                        if (selected.CustomerId > 0)
                        {
                            comboCustomer.SelectedValue = selected.CustomerId;
                        }
                        else
                        {
                            comboCustomer.Text = selected.CustomerName;
                        }

                        // Restore Discount and Tax
                        txtDiscount.Text = selected.Discount.ToString("0.00");
                        txtTax.Text = selected.TaxPercentText;

                        // Restore Cart Items
                        cartTable.Clear();
                        foreach (DataRow row in selected.CartItems.Rows)
                        {
                            cartTable.ImportRow(row);
                        }

                        // Remove from Hold Queue
                        HeldTransactions.Remove(selected);

                        // Recalculate totals
                        CalculateCheckoutTotals();

                        MessageBox.Show("Transaction recalled successfully.", "Transaction Recalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtProductCode.Focus();
                    }
                }
            }
        }

        private class HeldTransaction
        {
            public string Id { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public decimal Discount { get; set; }
            public string TaxPercentText { get; set; }
            public DataTable CartItems { get; set; }
            public DateTime HoldTime { get; set; }
            public decimal GrandTotal { get; set; }
        }

        private static readonly List<HeldTransaction> HeldTransactions = new List<HeldTransaction>();

        private class RecallDialog : Form
        {
            private DataGridView gridHeld;
            private Button btnRecall;
            private Button btnDelete;
            private Button btnCancel;
            private List<HeldTransaction> transactionsList;
            
            public HeldTransaction SelectedTransaction { get; private set; }
            public bool IsDeleteRequest { get; private set; }

            public RecallDialog(List<HeldTransaction> transactions)
            {
                this.transactionsList = transactions;
                InitializeComponent();
                LoadTransactions();
            }

            private void InitializeComponent()
            {
                this.Text = "Recall Held Transactions";
                this.Size = new Size(650, 400);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Theme.Primary;
                this.Font = Theme.MainFont;
                this.ForeColor = Theme.TextLight;

                Label lblTitle = new Label();
                lblTitle.Text = "Select a transaction to resume or delete:";
                lblTitle.Location = new Point(20, 15);
                lblTitle.AutoSize = true;
                Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblTitle);

                gridHeld = new DataGridView();
                gridHeld.Location = new Point(20, 45);
                gridHeld.Size = new Size(590, 240);
                Theme.StyleGrid(gridHeld);
                this.Controls.Add(gridHeld);

                btnRecall = new Button();
                btnRecall.Text = "📂 Recall";
                btnRecall.Location = new Point(20, 300);
                btnRecall.Size = new Size(130, 40);
                Theme.StyleSuccessButton(btnRecall);
                btnRecall.Click += BtnRecall_Click;
                this.Controls.Add(btnRecall);

                btnDelete = new Button();
                btnDelete.Text = "🗑️ Delete";
                btnDelete.Location = new Point(170, 300);
                btnDelete.Size = new Size(130, 40);
                Theme.StyleDangerButton(btnDelete);
                btnDelete.Click += BtnDelete_Click;
                this.Controls.Add(btnDelete);

                btnCancel = new Button();
                btnCancel.Text = "Close";
                btnCancel.Location = new Point(480, 300);
                btnCancel.Size = new Size(130, 40);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnRecall;
            }

            private void LoadTransactions()
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Hold ID", typeof(string));
                dt.Columns.Add("Customer Name", typeof(string));
                dt.Columns.Add("Date & Time", typeof(string));
                dt.Columns.Add("Items Count", typeof(int));
                dt.Columns.Add("Grand Total", typeof(string));

                foreach (var t in transactionsList)
                {
                    int itemsCount = 0;
                    foreach (DataRow row in t.CartItems.Rows)
                    {
                        itemsCount += Convert.ToInt32(row["Qty"]);
                    }
                    dt.Rows.Add(t.Id, t.CustomerName, t.HoldTime.ToString("yyyy-MM-dd HH:mm:ss"), itemsCount, $"Rs. {t.GrandTotal:N2}");
                }

                gridHeld.DataSource = dt;
            }

            private void BtnRecall_Click(object sender, EventArgs e)
            {
                if (gridHeld.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a transaction first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string holdId = gridHeld.SelectedRows[0].Cells["Hold ID"].Value.ToString();
                SelectedTransaction = transactionsList.Find(t => t.Id == holdId);
                IsDeleteRequest = false;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            private void BtnDelete_Click(object sender, EventArgs e)
            {
                if (gridHeld.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a transaction to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string holdId = gridHeld.SelectedRows[0].Cells["Hold ID"].Value.ToString();
                SelectedTransaction = transactionsList.Find(t => t.Id == holdId);
                IsDeleteRequest = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private class QuickCustomerRegistrationDialog : Form
        {
            private TextBox txtName;
            private TextBox txtPhone;
            private Button btnRegister;
            private Button btnCancel;

            public string CustomerName { get; private set; }
            public string CustomerPhone { get; private set; }

            public QuickCustomerRegistrationDialog()
            {
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                this.Text = "Register Customer for Dues";
                this.ClientSize = new Size(400, 280);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;
                this.Font = Theme.MainFont;
                this.ForeColor = Theme.TextLight;

                Label lblHeader = new Label();
                lblHeader.Text = "Register Customer";
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Name Field
                Label lblName = new Label();
                lblName.Text = "Customer Name *";
                lblName.Location = new Point(20, 70);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(360, 30);
                txtName.Location = new Point(20, 95);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Phone Field
                Label lblPhone = new Label();
                lblPhone.Text = "Phone Number";
                lblPhone.Location = new Point(20, 140);
                lblPhone.AutoSize = true;
                Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPhone);

                txtPhone = new TextBox();
                txtPhone.Size = new Size(360, 30);
                txtPhone.Location = new Point(20, 165);
                Theme.StyleTextBox(txtPhone);
                this.Controls.Add(txtPhone);

                // Register Button
                btnRegister = new Button();
                btnRegister.Text = "Register & Continue";
                btnRegister.Size = new Size(170, 40);
                btnRegister.Location = new Point(20, 220);
                Theme.StyleSuccessButton(btnRegister);
                btnRegister.Click += BtnRegister_Click;
                this.Controls.Add(btnRegister);

                // Cancel Button
                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(170, 40);
                btnCancel.Location = new Point(210, 220);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnRegister;
                this.CancelButton = btnCancel;
                this.Load += (s, e) => txtName.Focus();
            }

            private void BtnRegister_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string phone = txtPhone.Text.Trim();

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
                            string checkSql = "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Phone <> '' AND Phone IS NOT NULL";
                            using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@phone", phone);
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

                this.CustomerName = name;
                this.CustomerPhone = phone;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
