using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class StockLedgerControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridLedger;
        private DateTimePicker dtpLedgerDate;
        private Label lblTotalSummary;

        public StockLedgerControl()
        {
            InitializeComponent();
            LoadLedger();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Daily Stock Ledger Book";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Control Action Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Date picker label
            Label lblDate = new Label();
            lblDate.Text = "Select Ledger Date:";
            lblDate.Location = new Point(0, 12);
            lblDate.AutoSize = true;
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            actionPanel.Controls.Add(lblDate);

            // Date Picker
            dtpLedgerDate = new DateTimePicker();
            dtpLedgerDate.Format = DateTimePickerFormat.Short;
            dtpLedgerDate.Size = new Size(160, 28);
            dtpLedgerDate.Location = new Point(lblDate.Left + lblDate.PreferredWidth + 10, 7);
            dtpLedgerDate.Font = Theme.MainFont;
            dtpLedgerDate.ValueChanged += (s, e) => LoadLedger();
            actionPanel.Controls.Add(dtpLedgerDate);

            // Search Panel container (aligned right)
            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(250, 36);
            searchPanel.Location = new Point(660, 7);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.TextChanged += (s, e) => LoadLedger();
            searchPanel.Controls.Add(txtSearch);
            actionPanel.Controls.Add(searchPanel);

            // Search Label next to search box
            Label lblSearch = new Label();
            lblSearch.Text = "🔍 Filter Product:";
            lblSearch.AutoSize = true;
            lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblSearch, Theme.TextDark, Theme.BoldFont);
            
            // Adjust position dynamically on resize to avoid overlap at all window sizes
            actionPanel.Resize += (s, e) => {
                lblSearch.Location = new Point(searchPanel.Left - lblSearch.PreferredWidth - 10, 12);
            };
            lblSearch.Location = new Point(searchPanel.Left - lblSearch.PreferredWidth - 10, 12);
            actionPanel.Controls.Add(lblSearch);

            this.Controls.Add(actionPanel);

            // GridView
            gridLedger = new DataGridView();
            gridLedger.Size = new Size(910, 480);
            gridLedger.Location = new Point(20, 125);
            gridLedger.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridLedger);
            this.Controls.Add(gridLedger);

            // Summary Label at bottom
            lblTotalSummary = new Label();
            lblTotalSummary.Text = "";
            lblTotalSummary.Location = new Point(20, 615);
            lblTotalSummary.Size = new Size(910, 25);
            lblTotalSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Theme.StyleLabel(lblTotalSummary, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblTotalSummary);
        }

        private void LoadLedger()
        {
            try
            {
                DateTime selectedDate = dtpLedgerDate.Value.Date;
                DateTime startDate = selectedDate;
                DateTime endDate = selectedDate.AddDays(1);

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            p.Name AS [Brand Name],
                            
                            -- Fresh Arrival (Purchases on day D)
                            ISNULL(purchasesOnDay.Qty, 0) AS [Fresh Arrival],
                            
                            -- Sale (Net Sales on day D: Sales - Returns)
                            ISNULL(salesOnDay.Qty, 0) - ISNULL(returnsOnDay.Qty, 0) AS [Sale],
                            
                            -- Closing Balance (Stock at end of day D)
                            p.Stock 
                              + ISNULL(salesAfterDay.Qty, 0) 
                              - ISNULL(purchasesAfterDay.Qty, 0) 
                              - ISNULL(returnsAfterDay.Qty, 0) AS [Closing Balance]
                        FROM Products p
                        OUTER APPLY (
                            SELECT SUM(pd.Quantity) AS Qty
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases pur ON pd.PurchaseId = pur.Id
                            WHERE pd.ProductId = p.Id AND pur.PurchaseDate >= @startDate AND pur.PurchaseDate < @endDate
                        ) purchasesOnDay
                        OUTER APPLY (
                            SELECT SUM(sd.Quantity) AS Qty
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId = s.Id
                            WHERE sd.ProductId = p.Id AND s.SaleDate >= @startDate AND s.SaleDate < @endDate
                        ) salesOnDay
                        OUTER APPLY (
                            SELECT SUM(srd.Quantity) AS Qty
                            FROM SalesReturnDetails srd
                            INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                            WHERE srd.ProductId = p.Id AND sr.ReturnDate >= @startDate AND sr.ReturnDate < @endDate
                        ) returnsOnDay
                        OUTER APPLY (
                            SELECT SUM(pd.Quantity) AS Qty
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases pur ON pd.PurchaseId = pur.Id
                            WHERE pd.ProductId = p.Id AND pur.PurchaseDate >= @endDate
                        ) purchasesAfterDay
                        OUTER APPLY (
                            SELECT SUM(sd.Quantity) AS Qty
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId = s.Id
                            WHERE sd.ProductId = p.Id AND s.SaleDate >= @endDate
                        ) salesAfterDay
                        OUTER APPLY (
                            SELECT SUM(srd.Quantity) AS Qty
                            FROM SalesReturnDetails srd
                            INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                            WHERE srd.ProductId = p.Id AND sr.ReturnDate >= @endDate
                        ) returnsAfterDay
                        WHERE (p.Name LIKE @search OR p.Category LIKE @search)
                        ORDER BY p.Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        cmd.Parameters.AddWithValue("@search", $"%{txtSearch.Text.Trim()}%");

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable rawDt = new DataTable();
                            da.Fill(rawDt);

                            // Build final display table with calculated Opening Balance and Total
                            DataTable displayDt = new DataTable();
                            displayDt.Columns.Add("Brand Name", typeof(string));
                            displayDt.Columns.Add("Opening Balance", typeof(int));
                            displayDt.Columns.Add("Fresh Arrival", typeof(int));
                            displayDt.Columns.Add("Total", typeof(int));
                            displayDt.Columns.Add("Sale", typeof(int));
                            displayDt.Columns.Add("Closing Balance", typeof(int));

                            int totalOpening = 0;
                            int totalFresh = 0;
                            int totalSale = 0;
                            int totalClosing = 0;

                            foreach (DataRow r in rawDt.Rows)
                            {
                                string brandName = r["Brand Name"].ToString();
                                int freshArrival = Convert.ToInt32(r["Fresh Arrival"]);
                                int sale = Convert.ToInt32(r["Sale"]);
                                int closingBalance = Convert.ToInt32(r["Closing Balance"]);

                                int openingBalance = closingBalance + sale - freshArrival;
                                int total = openingBalance + freshArrival;

                                displayDt.Rows.Add(brandName, openingBalance, freshArrival, total, sale, closingBalance);

                                totalOpening += openingBalance;
                                totalFresh += freshArrival;
                                totalSale += sale;
                                totalClosing += closingBalance;
                            }

                            gridLedger.DataSource = displayDt;

                            if (gridLedger.Columns["Brand Name"] != null) gridLedger.Columns["Brand Name"].FillWeight = 200;
                            
                            string[] numCols = { "Opening Balance", "Fresh Arrival", "Total", "Sale", "Closing Balance" };
                            foreach (var col in numCols)
                            {
                                if (gridLedger.Columns[col] != null)
                                {
                                    gridLedger.Columns[col].DefaultCellStyle.Format = "N0";
                                    gridLedger.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    gridLedger.Columns[col].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                                }
                            }

                            lblTotalSummary.Text = $"Summary for {selectedDate:dd MMM yyyy}:   Opening Stock: {totalOpening:N0}   |   Fresh Arrival: {totalFresh:N0}   |   Total Stock: {(totalOpening + totalFresh):N0}   |   Sales: {totalSale:N0}   |   Closing Stock: {totalClosing:N0}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily ledger book: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
