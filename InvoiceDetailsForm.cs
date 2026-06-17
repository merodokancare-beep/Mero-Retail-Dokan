using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class InvoiceDetailsForm : Form
    {
        public InvoiceDetailsForm(string invoiceNumber)
        {
            InitializeComponent(invoiceNumber);
        }

        private void InitializeComponent(string invoiceNumber)
        {
            this.Text = $"Invoice Details - {invoiceNumber}";
            this.Size = new Size(1000, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Secondary;
            this.ForeColor = Theme.TextLight;
            this.Font = Theme.MainFont;

            Label lblTitle = new Label();
            lblTitle.Text = $"Invoice Details: {invoiceNumber}";
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblTitle);

            DataGridView gridItems = new DataGridView();
            gridItems.Location = new Point(20, 55);
            gridItems.Size = new Size(this.ClientSize.Width - 40, 240);
            gridItems.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridItems);
            this.Controls.Add(gridItems);

            Label lblTotal = new Label();
            lblTotal.Text = "SubTotal: Rs. 0.00  •  Discount: Rs. 0.00  •  Tax: Rs. 0.00  •  Grand Total: Rs. 0.00  •  Paid (Checkout): Rs. 0.00  •  Paid (Later): Rs. 0.00  •  Total Paid: Rs. 0.00  •  Due: Rs. 0.00";
            lblTotal.Location = new Point(20, 315);
            lblTotal.Size = new Size(this.ClientSize.Width - 40, 30);
            lblTotal.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblTotal, Theme.Success, Theme.BoldFont);
            this.Controls.Add(lblTotal);

            // Load items
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT p.Code as [Product Code], p.Name as [Product Name], 
                               sd.Quantity as [Qty], 
                               ISNULL((SELECT SUM(srd.Quantity) 
                                       FROM SalesReturnDetails srd 
                                       INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id 
                                       WHERE sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId), 0) as [Returned Qty],
                               sd.UnitPrice as [Unit Price], sd.Total as [Total Amount]
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        WHERE s.InvoiceNumber = @invNum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridItems.DataSource = dt;

                            if (gridItems.Columns["Unit Price"] != null) gridItems.Columns["Unit Price"].DefaultCellStyle.Format = "N2";
                            if (gridItems.Columns["Total Amount"] != null) gridItems.Columns["Total Amount"].DefaultCellStyle.Format = "N2";

                            if (gridItems.Columns["Product Code"] != null) gridItems.Columns["Product Code"].FillWeight = 60;
                            if (gridItems.Columns["Product Name"] != null) gridItems.Columns["Product Name"].FillWeight = 140;
                            if (gridItems.Columns["Qty"] != null) gridItems.Columns["Qty"].FillWeight = 40;
                            if (gridItems.Columns["Returned Qty"] != null) gridItems.Columns["Returned Qty"].FillWeight = 50;
                            if (gridItems.Columns["Unit Price"] != null) gridItems.Columns["Unit Price"].FillWeight = 60;
                            if (gridItems.Columns["Total Amount"] != null) gridItems.Columns["Total Amount"].FillWeight = 70;
                        }
                    }

                    // Get grand total and details
                    string totalQuery = @"
                        SELECT GrandTotal, Discount, Tax, SubTotal, AmountPaid,
                               ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = Sales.Id), 0) AS LaterPaid,
                               (DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = Sales.Id), 0)) AS CurrentDue,
                               ISNULL((SELECT SUM(TotalRefund) FROM SalesReturns WHERE SaleId = Sales.Id), 0) AS TotalRefund,
                               ISNULL((SELECT SUM(CashRefund) FROM SalesReturns WHERE SaleId = Sales.Id), 0) AS CashRefund
                        FROM Sales 
                        WHERE InvoiceNumber = @invNum";

                    using (SqlCommand cmd = new SqlCommand(totalQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                decimal grand = Convert.ToDecimal(rdr["GrandTotal"]);
                                decimal discount = Convert.ToDecimal(rdr["Discount"]);
                                decimal tax = Convert.ToDecimal(rdr["Tax"]);
                                decimal sub = Convert.ToDecimal(rdr["SubTotal"]);
                                decimal initialPaid = Convert.ToDecimal(rdr["AmountPaid"]);
                                decimal laterPaid = Convert.ToDecimal(rdr["LaterPaid"]);
                                decimal currentDue = Convert.ToDecimal(rdr["CurrentDue"]);
                                decimal totalRefund = Convert.ToDecimal(rdr["TotalRefund"]);
                                decimal cashRefund = Convert.ToDecimal(rdr["CashRefund"]);
                                decimal totalPaid = initialPaid + laterPaid;

                                if (totalRefund > 0)
                                {
                                    lblTotal.Text = $"SubTotal: Rs. {sub:N2}  •  Discount: Rs. {discount:N2}  •  Tax: Rs. {tax:N2}  •  Grand Total: Rs. {grand:N2}  •  Returned: Rs. {totalRefund:N2} (Cash Refund: Rs. {cashRefund:N2})  •  Net Grand: Rs. {grand - totalRefund:N2}  •  Paid (Checkout): Rs. {initialPaid:N2}  •  Paid (Later): Rs. {laterPaid:N2}  •  Refund Paid: -Rs. {cashRefund:N2}  •  Net Paid: Rs. {totalPaid - cashRefund:N2}  •  Due: Rs. {currentDue:N2}";
                                }
                                else
                                {
                                    lblTotal.Text = $"SubTotal: Rs. {sub:N2}  •  Discount: Rs. {discount:N2}  •  Tax: Rs. {tax:N2}  •  Grand Total: Rs. {grand:N2}  •  Paid (Checkout): Rs. {initialPaid:N2}  •  Paid (Later): Rs. {laterPaid:N2}  •  Total Paid: Rs. {totalPaid:N2}  •  Due: Rs. {currentDue:N2}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice items: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
