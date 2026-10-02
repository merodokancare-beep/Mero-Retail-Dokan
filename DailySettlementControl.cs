using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class DailySettlementControl : UserControl
    {
        private DateTimePicker dtpSettlementDate;
        private Button btnRefresh;

        // UI Controls
        private Label lblTotalSaleToday;
        private Label lblDueToday;
        private Label lblPrevDueRepayments;
        private Label lblOpeningCashText;
        private Label lblOnlinePayment;
        private Label lblRefundsToday;
        private Label lblExpectedCash;
        private TextBox txtActualCash;
        private TextBox txtRemarks;
        private Label lblStatusMessage;
        private Button btnSaveSettlement;

        // History
        private DataGridView gridHistory;

        // State calculations
        private decimal cashSales = 0;
        private decimal prevDueRepayments = 0;
        private decimal todayDueRepayments = 0;
        private decimal duesCreated = 0;
        private decimal expectedCash = 0;
        private decimal totalSaleToday = 0;
        private decimal dueTodayUnpaid = 0;
        private decimal openingCash = 0;
        private decimal cashRefunds = 0;
        private decimal onlineRefunds = 0;
        private decimal onlineSales = 0;
        private decimal onlineDueRepayments = 0;
        private decimal totalOnlinePayment = 0;
        private Label lblCrossNotice;
        private bool _hasRetriedSchemaInit = false;

        public DailySettlementControl()
        {
            InitializeComponent();
            LoadOpeningCash();
            LoadTodayMetrics();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 680);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Daily Cash Register & Settlement";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Top bar - Date Selection
            Panel topBar = new Panel();
            topBar.Size = new Size(this.Width - 40, 45);
            topBar.Location = new Point(20, 60);
            topBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            topBar.BackColor = Color.Transparent;

            Label lblDate = new Label();
            lblDate.Text = "Settlement Date:";
            lblDate.Location = new Point(0, 12);
            lblDate.AutoSize = true;
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            topBar.Controls.Add(lblDate);

            dtpSettlementDate = new DateTimePicker();
            dtpSettlementDate.Size = new Size(150, 28);
            dtpSettlementDate.Location = new Point(190, 8);
            dtpSettlementDate.Font = Theme.MainFont;
            dtpSettlementDate.Format = DateTimePickerFormat.Short;
            dtpSettlementDate.Value = DateTime.Today;
            dtpSettlementDate.ValueChanged += (s, e) => { _hasRetriedSchemaInit = false; LoadOpeningCash(); LoadTodayMetrics(); };
            topBar.Controls.Add(dtpSettlementDate);

            btnRefresh = new Button();
            btnRefresh.Text = "🔄 Refresh Metrics";
            btnRefresh.Size = new Size(180, 32);
            btnRefresh.Location = new Point(370, 6);
            Theme.StylePrimaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => { _hasRetriedSchemaInit = false; LoadOpeningCash(); LoadTodayMetrics(); };
            topBar.Controls.Add(btnRefresh);

            this.Controls.Add(topBar);

            // Unified Card for Settlement Fields
            Panel mainPanel = new Panel();
            mainPanel.Size = new Size(this.Width - 40, 320);
            mainPanel.Location = new Point(20, 115);
            mainPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mainPanel.BackColor = Color.Transparent;
            this.Controls.Add(mainPanel);

            Panel cardMain = Theme.CreateCard(this.Width - 40, 320);
            cardMain.Dock = DockStyle.Fill;
            cardMain.BackColor = Color.FromArgb(17, 24, 39);
            mainPanel.Controls.Add(cardMain);

            // COLUMN 1: Daily Figures & Dues
            Label lblCol1Header = new Label();
            lblCol1Header.Text = "DAILY REVENUE & CASH SUMMARY";
            lblCol1Header.Location = new Point(20, 15);
            lblCol1Header.AutoSize = true;
            Theme.StyleLabel(lblCol1Header, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardMain.Controls.Add(lblCol1Header);

            // 1. Total Sale Today
            Label lblSaleTitle = new Label();
            lblSaleTitle.Text = "Total Sale Today:";
            lblSaleTitle.Location = new Point(20, 42);
            lblSaleTitle.AutoSize = true;
            Theme.StyleLabel(lblSaleTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblSaleTitle);

            lblTotalSaleToday = new Label();
            lblTotalSaleToday.Text = "Rs. 0.00";
            lblTotalSaleToday.Location = new Point(270, 42);
            lblTotalSaleToday.AutoSize = true;
            Theme.StyleLabel(lblTotalSaleToday, Theme.Accent, Theme.BoldFont);
            cardMain.Controls.Add(lblTotalSaleToday);

            // 2. Due Today
            Label lblDueTitle = new Label();
            lblDueTitle.Text = "Due Today (Unpaid):";
            lblDueTitle.Location = new Point(20, 70);
            lblDueTitle.AutoSize = true;
            Theme.StyleLabel(lblDueTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblDueTitle);

            lblDueToday = new Label();
            lblDueToday.Text = "Rs. 0.00";
            lblDueToday.Location = new Point(270, 70);
            lblDueToday.AutoSize = true;
            Theme.StyleLabel(lblDueToday, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblDueToday);

            // 3. Previous Due Collections
            Label lblPrevDueTitle = new Label();
            lblPrevDueTitle.Text = "Prev Date Due Repayment:";
            lblPrevDueTitle.Location = new Point(20, 98);
            lblPrevDueTitle.AutoSize = true;
            Theme.StyleLabel(lblPrevDueTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblPrevDueTitle);

            lblPrevDueRepayments = new Label();
            lblPrevDueRepayments.Text = "Rs. 0.00";
            lblPrevDueRepayments.Location = new Point(270, 98);
            lblPrevDueRepayments.AutoSize = true;
            Theme.StyleLabel(lblPrevDueRepayments, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblPrevDueRepayments);

            // 4. Online Payment (Card/QR)
            Label lblOnlineTitle = new Label();
            lblOnlineTitle.Text = "Online Payment (Card/QR):";
            lblOnlineTitle.Location = new Point(20, 126);
            lblOnlineTitle.AutoSize = true;
            Theme.StyleLabel(lblOnlineTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblOnlineTitle);

            lblOnlinePayment = new Label();
            lblOnlinePayment.Text = "Rs. 0.00";
            lblOnlinePayment.Location = new Point(270, 126);
            lblOnlinePayment.AutoSize = true;
            Theme.StyleLabel(lblOnlinePayment, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblOnlinePayment);

            // 5. Total Refunds Made Today
            Label lblRefundTitle = new Label();
            lblRefundTitle.Text = "Refunds Given Today:";
            lblRefundTitle.Location = new Point(20, 154);
            lblRefundTitle.AutoSize = true;
            Theme.StyleLabel(lblRefundTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblRefundTitle);

            lblRefundsToday = new Label();
            lblRefundsToday.Text = "Rs. 0.00";
            lblRefundsToday.Location = new Point(270, 154);
            lblRefundsToday.AutoSize = true;
            lblRefundsToday.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Theme.StyleLabel(lblRefundsToday, Theme.Danger, lblRefundsToday.Font);
            cardMain.Controls.Add(lblRefundsToday);

            // 6. Expected/Total Cash in Drawer
            Label lblExpectedTitle = new Label();
            lblExpectedTitle.Text = "Total Cash in Drawer:";
            lblExpectedTitle.Location = new Point(20, 184);
            lblExpectedTitle.AutoSize = true;
            Theme.StyleLabel(lblExpectedTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblExpectedTitle);

            lblExpectedCash = new Label();
            lblExpectedCash.Text = "Rs. 0.00";
            lblExpectedCash.Location = new Point(270, 184);
            lblExpectedCash.AutoSize = true;
            lblExpectedCash.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            Theme.StyleLabel(lblExpectedCash, Theme.Success, lblExpectedCash.Font);
            cardMain.Controls.Add(lblExpectedCash);

            // 7. Explicit Refund Notice & Settlement Alert Box
            lblCrossNotice = new Label();
            lblCrossNotice.Text = "";
            lblCrossNotice.Location = new Point(20, 216);
            lblCrossNotice.Size = new Size(410, 95);
            lblCrossNotice.Visible = true;
            lblCrossNotice.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblCrossNotice.ForeColor = Color.FromArgb(245, 158, 11);
            cardMain.Controls.Add(lblCrossNotice);

            // Opening Cash hidden control kept for backwards compatibility
            lblOpeningCashText = new Label();
            lblOpeningCashText.Text = "Rs. 0.00";
            lblOpeningCashText.Visible = false;
            cardMain.Controls.Add(lblOpeningCashText);

            // Middle vertical separator
            Panel sepCol = new Panel();
            sepCol.Size = new Size(1, 280);
            sepCol.Location = new Point(440, 20);
            sepCol.BackColor = Theme.AlternateRow;
            cardMain.Controls.Add(sepCol);

            // COLUMN 2: Verification Input
            Label lblCol2Header = new Label();
            lblCol2Header.Text = "SETTLEMENT RECONCILIATION";
            lblCol2Header.Location = new Point(470, 15);
            lblCol2Header.AutoSize = true;
            Theme.StyleLabel(lblCol2Header, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardMain.Controls.Add(lblCol2Header);

            // Enter Cash in Hand
            Label lblActTitle = new Label();
            lblActTitle.Text = "Enter Cash In Hand *:";
            lblActTitle.Location = new Point(470, 50);
            lblActTitle.AutoSize = true;
            Theme.StyleLabel(lblActTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblActTitle);

            txtActualCash = new TextBox();
            txtActualCash.Size = new Size(150, 26);
            txtActualCash.Location = new Point(660, 47);
            Theme.StyleTextBox(txtActualCash);
            txtActualCash.Text = "0.00";
            txtActualCash.TextChanged += (s, e) => UpdateCalculations();
            cardMain.Controls.Add(txtActualCash);

            // Remarks
            Label lblRemarks = new Label();
            lblRemarks.Text = "Remarks / Notes:";
            lblRemarks.Location = new Point(470, 95);
            lblRemarks.AutoSize = true;
            Theme.StyleLabel(lblRemarks, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblRemarks);

            txtRemarks = new TextBox();
            txtRemarks.Size = new Size(340, 26);
            txtRemarks.Location = new Point(470, 120);
            Theme.StyleTextBox(txtRemarks);
            txtRemarks.Text = "Daily settlement reconciliation completed.";
            cardMain.Controls.Add(txtRemarks);

            // Status message
            lblStatusMessage = new Label();
            lblStatusMessage.Text = "Reconciliation: Enter counted cash.";
            lblStatusMessage.Location = new Point(470, 160);
            lblStatusMessage.Size = new Size(410, 45);
            Theme.StyleLabel(lblStatusMessage, Theme.TextDark, new Font("Segoe UI Semibold", 9.5F, FontStyle.Italic | FontStyle.Bold));
            cardMain.Controls.Add(lblStatusMessage);

            // Save Settlement Button
            btnSaveSettlement = new Button();
            btnSaveSettlement.Text = "🔒 Save & Close Register";
            btnSaveSettlement.Size = new Size(210, 42);
            btnSaveSettlement.Location = new Point(470, 215);
            Theme.StyleSuccessButton(btnSaveSettlement);
            btnSaveSettlement.Click += BtnSaveSettlement_Click;
            cardMain.Controls.Add(btnSaveSettlement);

            // Dynamic layout adjustment for column 2 and separator
            cardMain.Resize += (s, e) =>
            {
                int midX = cardMain.Width / 2;
                sepCol.Location = new Point(midX, 20);
                sepCol.Height = cardMain.Height - 40;
                lblCrossNotice.Width = Math.Max(200, midX - 35);

                int col2Left = midX + 25;
                lblCol2Header.Location = new Point(col2Left, 15);
                lblActTitle.Location = new Point(col2Left, 45);
                txtActualCash.Location = new Point(col2Left + 185, 42);
                lblRemarks.Location = new Point(col2Left, 85);
                txtRemarks.Location = new Point(col2Left, 110);
                txtRemarks.Width = Math.Max(260, cardMain.Width - col2Left - 30);
                lblStatusMessage.Location = new Point(col2Left, 155);
                lblStatusMessage.Width = Math.Max(260, cardMain.Width - col2Left - 30);
                btnSaveSettlement.Location = new Point(col2Left, 220);
            };

            // BOTTOM PANEL: Historical Log
            Label lblHistoryHeader = new Label();
            lblHistoryHeader.Text = "Historical Reconciliation Log Book";
            lblHistoryHeader.Location = new Point(20, 450);
            lblHistoryHeader.AutoSize = true;
            Theme.StyleLabel(lblHistoryHeader, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblHistoryHeader);

            gridHistory = new DataGridView();
            gridHistory.Location = new Point(20, 480);
            gridHistory.Size = new Size(this.Width - 40, this.Height - 500);
            gridHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Theme.StyleGrid(gridHistory);
            this.Controls.Add(gridHistory);
        }

        private void LoadOpeningCash()
        {
            // As per user specification: No opening cash balance / rollforward. Each day starts at Rs. 0.00.
            openingCash = 0;
            if (lblOpeningCashText != null) lblOpeningCashText.Text = "Rs. 0.00";
        }

        private void LoadTodayMetrics()
        {
            DateTime selectDate = dtpSettlementDate.Value.Date;
            DateTime todayStart = selectDate.AddHours(6);
            DateTime todayEnd = selectDate.AddDays(1).AddHours(6);

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Cash Sales Today (accurately handles Cash and Split cash portions)
                    string salesSql = @"
                        SELECT ISNULL(SUM(
                            CASE 
                                WHEN CashAmount > 0 OR OnlineAmount > 0 THEN CashAmount
                                WHEN PaymentMethod = 'Cash' THEN AmountPaid
                                ELSE 0 
                            END
                        ), 0) 
                        FROM Sales 
                        WHERE SaleDate >= @todayStart AND SaleDate < @todayEnd";
                    using (SqlCommand cmd = new SqlCommand(salesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        cashSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 1b. Online Sales Today (accurately handles Card/UPI/QR and Split online portions)
                    string onlineSalesSql = @"
                        SELECT ISNULL(SUM(
                            CASE 
                                WHEN CashAmount > 0 OR OnlineAmount > 0 THEN OnlineAmount
                                WHEN (PaymentMethod IN ('Card', 'QR Pay', 'UPI') OR PaymentMethod LIKE '%Online%') AND PaymentMethod NOT LIKE 'Split%' THEN AmountPaid
                                ELSE 0 
                            END
                        ), 0) 
                        FROM Sales 
                        WHERE SaleDate >= @todayStart AND SaleDate < @todayEnd";
                    using (SqlCommand cmd = new SqlCommand(onlineSalesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        onlineSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 2. New Dues Created Today
                    string duesCreatedSql = "SELECT ISNULL(SUM(DueAmount), 0) FROM Sales WHERE SaleDate >= @todayStart AND SaleDate < @todayEnd";
                    using (SqlCommand cmd = new SqlCommand(duesCreatedSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        duesCreated = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 3. Previous Due Repayments (Cash)
                    string prevDuesSql = @"
                        SELECT ISNULL(SUM(cp.Amount), 0)
                        FROM CustomerPayments cp
                        LEFT JOIN Sales s ON cp.SaleId = s.Id
                        WHERE cp.PaymentDate >= @todayStart AND cp.PaymentDate < @todayEnd 
                          AND cp.PaymentMethod = 'Cash'
                          AND (s.SaleDate < @todayStart OR cp.SaleId IS NULL)";

                    using (SqlCommand cmd = new SqlCommand(prevDuesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        prevDueRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 4. Today's Due Repayments (Cash)
                    string totalRepaymentsSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE PaymentDate >= @todayStart AND PaymentDate < @todayEnd AND PaymentMethod = 'Cash'";
                    decimal totalRepayments = 0;
                    using (SqlCommand cmd = new SqlCommand(totalRepaymentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        totalRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                    todayDueRepayments = totalRepayments - prevDueRepayments;
                    if (todayDueRepayments < 0) todayDueRepayments = 0;

                    // 4b. Online Due Repayments Today (Card / QR Pay / UPI)
                    string onlineRepaymentsSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE PaymentDate >= @todayStart AND PaymentDate < @todayEnd AND PaymentMethod IN ('Card', 'QR Pay', 'UPI')";
                    using (SqlCommand cmd = new SqlCommand(onlineRepaymentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        onlineDueRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 5. Refunds Today (Cash & Online)
                    string refundsSql = "SELECT ISNULL(SUM(CashRefund), 0), ISNULL(SUM(OnlineRefund), 0) FROM SalesReturns WHERE ReturnDate >= @todayStart AND ReturnDate < @todayEnd";
                    using (SqlCommand cmd = new SqlCommand(refundsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@todayStart", todayStart);
                        cmd.Parameters.AddWithValue("@todayEnd", todayEnd);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cashRefunds = rdr.GetDecimal(0);
                                onlineRefunds = rdr.GetDecimal(1);
                            }
                        }
                    }
                }

                // Calculations
                totalSaleToday = cashSales + onlineSales + duesCreated - (cashRefunds + onlineRefunds);
                dueTodayUnpaid = duesCreated - todayDueRepayments;
                if (dueTodayUnpaid < 0) dueTodayUnpaid = 0;

                totalOnlinePayment = (onlineSales + onlineDueRepayments) - onlineRefunds;

                lblTotalSaleToday.Text = $"Rs. {totalSaleToday:N2}";
                lblDueToday.Text = $"Rs. {dueTodayUnpaid:N2}";
                lblPrevDueRepayments.Text = $"Rs. {prevDueRepayments:N2}";
                lblOnlinePayment.Text = $"Rs. {totalOnlinePayment:N2}";

                decimal totalRefunds = cashRefunds + onlineRefunds;
                if (totalRefunds > 0)
                {
                    if (cashRefunds > 0 && onlineRefunds > 0)
                        lblRefundsToday.Text = $"Rs. {totalRefunds:N2} (Cash: {cashRefunds:N0}, Online: {onlineRefunds:N0})";
                    else if (cashRefunds > 0)
                        lblRefundsToday.Text = $"Rs. {cashRefunds:N2} (Cash)";
                    else
                        lblRefundsToday.Text = $"Rs. {onlineRefunds:N2} (Online)";
                }
                else
                {
                    lblRefundsToday.Text = "Rs. 0.00";
                }

                UpdateCalculations();
            }
            catch (Exception ex)
            {
                // Self-healing: If an older database backup was restored externally or columns are missing,
                // automatically initialize and upgrade the database schema, then retry once.
                if (!_hasRetriedSchemaInit && (ex.Message.Contains("Invalid column name") || ex.Message.Contains("Invalid object name")))
                {
                    _hasRetriedSchemaInit = true;
                    try
                    {
                        DatabaseHelper.InitializeDatabase();
                        LoadTodayMetrics();
                        return;
                    }
                    catch { }
                }

                MessageBox.Show($"Error loading daily financials: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCalculations()
        {
            decimal todayNetCash = cashSales + prevDueRepayments + todayDueRepayments - cashRefunds;
            // No opening balance - drawer only reflects today's net cash
            expectedCash = todayNetCash;
            lblExpectedCash.Text = $"Rs. {expectedCash:N2}";

            if (expectedCash < 0)
            {
                lblExpectedCash.ForeColor = Theme.Danger;
            }
            else
            {
                lblExpectedCash.ForeColor = Theme.Success;
            }

            // Explicit Messages about Any Refunds Made During Settlement
            decimal totalRefunds = cashRefunds + onlineRefunds;
            if (totalRefunds > 0)
            {
                lblCrossNotice.Visible = true;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"📢 REFUND NOTICE TODAY (Total: Rs. {totalRefunds:N2}):");

                if (cashRefunds > 0)
                {
                    sb.AppendLine($"• Cash Refund: Rs. {cashRefunds:N2} returned in cash from drawer.");
                }
                if (onlineRefunds > 0)
                {
                    sb.AppendLine($"• Online Refund: Rs. {onlineRefunds:N2} refunded via bank/UPI.");
                }

                if (cashRefunds > 0 && todayNetCash < 0)
                {
                    decimal deficit = Math.Abs(todayNetCash);
                    sb.AppendLine($"⚠️ Cash Deficit Alert: Cash refund exceeds cash sales by Rs. {deficit:N2}.");
                    sb.AppendLine($"(Items sold online were refunded in cash; transfer Rs. {deficit:N2} from bank to drawer).");
                    lblCrossNotice.ForeColor = Color.FromArgb(251, 146, 60); // Amber orange
                }
                else
                {
                    lblCrossNotice.ForeColor = Color.FromArgb(147, 197, 253); // Soft sky blue
                }

                lblCrossNotice.Text = sb.ToString().TrimEnd();
            }
            else
            {
                lblCrossNotice.Visible = true;
                lblCrossNotice.ForeColor = Color.FromArgb(156, 163, 175);
                lblCrossNotice.Text = "ℹ️ No sales refunds were issued today.";
            }

            decimal actualCash = 0;
            decimal.TryParse(txtActualCash.Text.Trim(), out actualCash);

            decimal variance = actualCash - expectedCash;

            if (Math.Abs(variance) < 0.01m)
            {
                lblStatusMessage.ForeColor = Theme.Success;
                lblStatusMessage.Text = $"Settlement Closed & Matched! ✓\n(Rs. {actualCash:N2} Matches Rs. {expectedCash:N2})";
            }
            else if (variance < 0)
            {
                lblStatusMessage.ForeColor = Theme.Danger;
                lblStatusMessage.Text = $"Cash Shortage! Unmatched by Rs. {Math.Abs(variance):N2}\n(Expected: Rs. {expectedCash:N2} | Actual: Rs. {actualCash:N2})";
            }
            else
            {
                lblStatusMessage.ForeColor = Theme.Warning;
                lblStatusMessage.Text = $"Cash Surplus! Unmatched by Rs. {variance:N2}\n(Expected: Rs. {expectedCash:N2} | Actual: Rs. {actualCash:N2})";
            }
        }

        private void BtnSaveSettlement_Click(object sender, EventArgs e)
        {
            decimal actualCash = 0;
            if (!decimal.TryParse(txtActualCash.Text.Trim(), out actualCash) || actualCash < 0)
            {
                MessageBox.Show("Please enter a valid cash amount physically counted.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal variance = actualCash - expectedCash;
            string remarks = txtRemarks.Text.Trim();
            DateTime selectDate = dtpSettlementDate.Value;

            // Check if settlement already exists for this date to prevent double logs
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", selectDate.Date);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            DialogResult confirm = MessageBox.Show($"A settlement has already been saved for {selectDate:yyyy-MM-dd}.\nDo you want to overwrite it?", "Confirm Settlement Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes)
                            {
                                return;
                            }
                            
                            // Delete old record to overwrite
                            using (SqlCommand delCmd = new SqlCommand("DELETE FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date", conn))
                            {
                                delCmd.Parameters.AddWithValue("@date", selectDate.Date);
                                delCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Save
                    string query = @"
                        INSERT INTO DailySettlements (SettlementDate, OpeningCash, CashSales, DueCollections, CardQRSales, DuesCreated, ExpectedCash, ActualCash, Variance, SettlementBy, Remarks, Refunds)
                        VALUES (@date, @opening, @sales, @collections, @cardQRSales, @dues, @expected, @actual, @variance, @user, @remarks, @refunds)";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", selectDate);
                        cmd.Parameters.AddWithValue("@opening", openingCash);
                        cmd.Parameters.AddWithValue("@sales", cashSales);
                        cmd.Parameters.AddWithValue("@collections", prevDueRepayments + todayDueRepayments);
                        cmd.Parameters.AddWithValue("@cardQRSales", totalOnlinePayment);
                        cmd.Parameters.AddWithValue("@dues", duesCreated);
                        cmd.Parameters.AddWithValue("@expected", expectedCash);
                        cmd.Parameters.AddWithValue("@actual", actualCash);
                        cmd.Parameters.AddWithValue("@variance", variance);
                        cmd.Parameters.AddWithValue("@user", Session.UserId);
                        cmd.Parameters.AddWithValue("@remarks", remarks);
                        cmd.Parameters.AddWithValue("@refunds", cashRefunds);
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Register settlement for {selectDate:yyyy-MM-dd} saved successfully!", "Settlement Logged", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settlement ledger: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.SettlementDate as [Date],
                               s.OpeningCash as [Opening Cash],
                               s.CashSales as [Cash Sales],
                               s.DueCollections as [Dues Collections],
                               s.CardQRSales as [Online Payment],
                               s.Refunds as [Cash Refunds],
                               s.ExpectedCash as [Expected Cash],
                               s.ActualCash as [Actual Cash],
                               s.Variance as [Variance],
                               u.FullName as [Settled By],
                               s.Remarks as [Remarks]
                        FROM DailySettlements s
                        LEFT JOIN Users u ON s.SettlementBy = u.Id
                        ORDER BY s.SettlementDate DESC, s.Id DESC";

                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridHistory.DataSource = dt;

                        // Formatting
                        if (gridHistory.Columns["Opening Cash"] != null) gridHistory.Columns["Opening Cash"].Visible = false;
                        if (gridHistory.Columns["Cash Sales"] != null) gridHistory.Columns["Cash Sales"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Dues Collections"] != null) gridHistory.Columns["Dues Collections"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Online Payment"] != null) gridHistory.Columns["Online Payment"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Cash Refunds"] != null) gridHistory.Columns["Cash Refunds"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Expected Cash"] != null) gridHistory.Columns["Expected Cash"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Actual Cash"] != null) gridHistory.Columns["Actual Cash"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Variance"] != null) gridHistory.Columns["Variance"].DefaultCellStyle.Format = "N2";

                        if (gridHistory.Columns["Date"] != null) gridHistory.Columns["Date"].FillWeight = 110;
                        if (gridHistory.Columns["Opening Cash"] != null) gridHistory.Columns["Opening Cash"].FillWeight = 90;
                        if (gridHistory.Columns["Cash Sales"] != null) gridHistory.Columns["Cash Sales"].FillWeight = 90;
                        if (gridHistory.Columns["Dues Collections"] != null) gridHistory.Columns["Dues Collections"].FillWeight = 90;
                        if (gridHistory.Columns["Online Payment"] != null) gridHistory.Columns["Online Payment"].FillWeight = 100;
                        if (gridHistory.Columns["Cash Refunds"] != null) gridHistory.Columns["Cash Refunds"].FillWeight = 90;
                        if (gridHistory.Columns["Expected Cash"] != null) gridHistory.Columns["Expected Cash"].FillWeight = 95;
                        if (gridHistory.Columns["Actual Cash"] != null) gridHistory.Columns["Actual Cash"].FillWeight = 95;
                        if (gridHistory.Columns["Variance"] != null) gridHistory.Columns["Variance"].FillWeight = 80;
                        if (gridHistory.Columns["Settled By"] != null) gridHistory.Columns["Settled By"].FillWeight = 100;
                        if (gridHistory.Columns["Remarks"] != null) gridHistory.Columns["Remarks"].FillWeight = 160;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settlement history register: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
