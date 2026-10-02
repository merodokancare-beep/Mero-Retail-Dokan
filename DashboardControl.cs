using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class DashboardControl : UserControl
    {
        private Panel cardSales;
        private Panel cardPurchases;
        private Panel cardProducts;
        private Panel cardLowStock;

        private Label lblSalesVal;
        private Label lblPurchasesVal;
        private Label lblProductsVal;
        private Label lblLowStockVal;

        private DataGridView gridLowStock;
        private Panel chartPanel;

        private decimal totalSales = 0;
        private decimal totalPurchases = 0;
        private decimal totalCOGS = 0;
        private int productCount = 0;
        private int lowStockCount = 0;

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;
            this.DoubleBuffered = true;

            // Welcome Header
            Label lblWelcome = new Label();
            lblWelcome.Text = $"Welcome Back, {Session.FullName}";
            lblWelcome.Location = new Point(20, 15);
            lblWelcome.AutoSize = true;
            Theme.StyleLabel(lblWelcome, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblWelcome);

            Label lblRole = new Label();
            lblRole.Text = $"Role: {Session.Role} | Current Session Details";
            lblRole.Location = new Point(22, 45);
            lblRole.AutoSize = true;
            Theme.StyleLabel(lblRole, Theme.TextDark, Theme.MainFont);
            this.Controls.Add(lblRole);

            bool isAdmin = string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // Responsive Metric Cards Layout (Auto-stretches across full screen width)
            TableLayoutPanel cardsPanel = new TableLayoutPanel();
            cardsPanel.Location = new Point(20, 75);
            cardsPanel.Size = new Size(this.Width - 40, 105);
            cardsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardsPanel.RowCount = 1;
            cardsPanel.BackColor = Color.Transparent;
            cardsPanel.Padding = new Padding(0);
            cardsPanel.Margin = new Padding(0);

            if (isAdmin)
            {
                cardsPanel.ColumnCount = 4;
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

                // 1. Total Sales Card
                cardSales = Theme.CreateCard(200, 100);
                cardSales.Dock = DockStyle.Fill;
                cardSales.Margin = new Padding(0, 0, 10, 0);
                cardSales.BackColor = Color.FromArgb(17, 24, 39);
                lblSalesVal = CreateCardContent(cardSales, "TOTAL REVENUE", "Rs. 0.00", Theme.Success);
                cardsPanel.Controls.Add(cardSales, 0, 0);

                // 2. Total Purchases Card
                cardPurchases = Theme.CreateCard(200, 100);
                cardPurchases.Dock = DockStyle.Fill;
                cardPurchases.Margin = new Padding(5, 0, 5, 0);
                cardPurchases.BackColor = Color.FromArgb(17, 24, 39);
                lblPurchasesVal = CreateCardContent(cardPurchases, "TOTAL PURCHASES", "Rs. 0.00", Theme.TextLight);
                cardsPanel.Controls.Add(cardPurchases, 1, 0);

                // 3. Product Count Card
                cardProducts = Theme.CreateCard(200, 100);
                cardProducts.Dock = DockStyle.Fill;
                cardProducts.Margin = new Padding(5, 0, 5, 0);
                cardProducts.BackColor = Color.FromArgb(17, 24, 39);
                lblProductsVal = CreateCardContent(cardProducts, "TOTAL PRODUCTS", "0 Items", Theme.Accent);
                cardsPanel.Controls.Add(cardProducts, 2, 0);

                // 4. Low Stock Warning Card
                cardLowStock = Theme.CreateCard(200, 100);
                cardLowStock.Dock = DockStyle.Fill;
                cardLowStock.Margin = new Padding(10, 0, 0, 0);
                cardLowStock.BackColor = Color.FromArgb(17, 24, 39);
                lblLowStockVal = CreateCardContent(cardLowStock, "LOW STOCK WARNING", "0 Items", Theme.Danger);
                cardsPanel.Controls.Add(cardLowStock, 3, 0);
            }
            else
            {
                cardsPanel.ColumnCount = 2;
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

                cardProducts = Theme.CreateCard(200, 100);
                cardProducts.Dock = DockStyle.Fill;
                cardProducts.Margin = new Padding(0, 0, 10, 0);
                cardProducts.BackColor = Color.FromArgb(17, 24, 39);
                lblProductsVal = CreateCardContent(cardProducts, "TOTAL PRODUCTS", "0 Items", Theme.Accent);
                cardsPanel.Controls.Add(cardProducts, 0, 0);

                cardLowStock = Theme.CreateCard(200, 100);
                cardLowStock.Dock = DockStyle.Fill;
                cardLowStock.Margin = new Padding(10, 0, 0, 0);
                cardLowStock.BackColor = Color.FromArgb(17, 24, 39);
                lblLowStockVal = CreateCardContent(cardLowStock, "LOW STOCK WARNING", "0 Items", Theme.Danger);
                cardsPanel.Controls.Add(cardLowStock, 1, 0);
            }
            this.Controls.Add(cardsPanel);

            // Responsive Main Content Split (Chart on Left, Low Stock Grid on Right)
            TableLayoutPanel mainContentGrid = new TableLayoutPanel();
            mainContentGrid.Location = new Point(20, 195);
            mainContentGrid.Size = new Size(this.Width - 40, this.Height - 215);
            mainContentGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            mainContentGrid.RowCount = 1;
            mainContentGrid.BackColor = Color.Transparent;
            mainContentGrid.Padding = new Padding(0);
            mainContentGrid.Margin = new Padding(0);

            if (isAdmin)
            {
                mainContentGrid.ColumnCount = 2;
                mainContentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                mainContentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

                // Left Container: Chart Panel
                Panel chartContainer = new Panel();
                chartContainer.Dock = DockStyle.Fill;
                chartContainer.Margin = new Padding(0, 0, 10, 0);
                chartContainer.BackColor = Color.Transparent;

                Label lblChartTitle = new Label();
                lblChartTitle.Text = "Analytics Overview (Revenue vs Cost)";
                lblChartTitle.Dock = DockStyle.Top;
                lblChartTitle.Height = 30;
                Theme.StyleLabel(lblChartTitle, Theme.TextLight, Theme.SubHeaderFont);
                chartContainer.Controls.Add(lblChartTitle);

                chartPanel = new Panel();
                chartPanel.Dock = DockStyle.Fill;
                chartPanel.BackColor = Color.FromArgb(17, 24, 39);
                chartPanel.Paint += ChartPanel_Paint;
                chartPanel.Resize += (s, e) => chartPanel.Invalidate();
                chartContainer.Controls.Add(chartPanel);
                chartPanel.BringToFront();

                mainContentGrid.Controls.Add(chartContainer, 0, 0);

                // Right Container: Critical Stock Table
                Panel gridContainer = new Panel();
                gridContainer.Dock = DockStyle.Fill;
                gridContainer.Margin = new Padding(10, 0, 0, 0);
                gridContainer.BackColor = Color.Transparent;

                Label lblTableTitle = new Label();
                lblTableTitle.Text = "Critical Stock Replenishment Needed";
                lblTableTitle.Dock = DockStyle.Top;
                lblTableTitle.Height = 30;
                Theme.StyleLabel(lblTableTitle, Theme.TextLight, Theme.SubHeaderFont);
                gridContainer.Controls.Add(lblTableTitle);

                gridLowStock = new DataGridView();
                gridLowStock.Dock = DockStyle.Fill;
                Theme.StyleGrid(gridLowStock);
                gridContainer.Controls.Add(gridLowStock);
                gridLowStock.BringToFront();

                mainContentGrid.Controls.Add(gridContainer, 1, 0);
            }
            else
            {
                mainContentGrid.ColumnCount = 1;
                mainContentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

                Panel gridContainer = new Panel();
                gridContainer.Dock = DockStyle.Fill;
                gridContainer.Margin = new Padding(0);
                gridContainer.BackColor = Color.Transparent;

                Label lblTableTitle = new Label();
                lblTableTitle.Text = "Critical Stock Replenishment Needed";
                lblTableTitle.Dock = DockStyle.Top;
                lblTableTitle.Height = 30;
                Theme.StyleLabel(lblTableTitle, Theme.TextLight, Theme.SubHeaderFont);
                gridContainer.Controls.Add(lblTableTitle);

                gridLowStock = new DataGridView();
                gridLowStock.Dock = DockStyle.Fill;
                Theme.StyleGrid(gridLowStock);
                gridContainer.Controls.Add(gridLowStock);
                gridLowStock.BringToFront();

                mainContentGrid.Controls.Add(gridContainer, 0, 0);
            }
            this.Controls.Add(mainContentGrid);
        }

        private Label CreateCardContent(Panel card, string header, string initVal, Color valColor)
        {
            Label lblHeader = new Label();
            lblHeader.Text = header;
            lblHeader.Location = new Point(12, 12);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(lblHeader);

            Label lblVal = new Label();
            lblVal.Text = initVal;
            lblVal.Location = new Point(12, 40);
            lblVal.AutoSize = true;
            Theme.StyleLabel(lblVal, valColor, new Font("Segoe UI", 16F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void LoadDashboardData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    bool isAdmin = string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase);
                    if (isAdmin)
                    {
                        // 1. Get Sales Summary (Net Sales = Gross Sales - Refunds)
                        decimal salesRevenue = 0;
                        decimal returnedRefund = 0;

                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales", conn))
                        {
                            salesRevenue = (decimal)cmd.ExecuteScalar();
                        }

                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalRefund), 0) FROM SalesReturns", conn))
                        {
                            returnedRefund = (decimal)cmd.ExecuteScalar();
                        }

                        totalSales = salesRevenue - returnedRefund;
                        lblSalesVal.Text = $"Rs. {totalSales:N2}";

                        // 2. Get Purchases Summary (calculated as total purchase cost of product in stock)
                        string purchasesQuery = "SELECT ISNULL(SUM(Stock * PurchasePrice), 0) FROM Products";

                        using (SqlCommand cmd = new SqlCommand(purchasesQuery, conn))
                        {
                            totalPurchases = (decimal)cmd.ExecuteScalar();
                            lblPurchasesVal.Text = $"Rs. {totalPurchases:N2}";
                        }

                        // 2.b Get Cost of Goods Sold (COGS) for the chart (Net COGS = Gross COGS - Resellable Return Cost)
                        decimal grossCogs = 0;
                        decimal resellableReturnCost = 0;

                        string grossCogsQuery = "SELECT ISNULL(SUM(Quantity * PurchaseCostAtSale), 0) FROM SaleDetails";

                        using (SqlCommand cmd = new SqlCommand(grossCogsQuery, conn))
                        {
                            grossCogs = (decimal)cmd.ExecuteScalar();
                        }

                        string returnCostQuery = @"
                            SELECT ISNULL(SUM(srd.Quantity * sd.PurchaseCostAtSale), 0)
                            FROM SalesReturnDetails srd
                            INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                            INNER JOIN SaleDetails sd ON sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId
                            WHERE srd.ItemCondition = 'Resellable'";

                        using (SqlCommand cmd = new SqlCommand(returnCostQuery, conn))
                        {
                            resellableReturnCost = (decimal)cmd.ExecuteScalar();
                        }

                        totalCOGS = grossCogs - resellableReturnCost;
                    }

                    // 3. Get Products Count
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
                    {
                        productCount = (int)cmd.ExecuteScalar();
                        lblProductsVal.Text = $"{productCount} SKU(s)";
                    }

                    // 4. Get Low Stock Alert Count
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Stock <= MinStockLevel", conn))
                    {
                        lowStockCount = (int)cmd.ExecuteScalar();
                        lblLowStockVal.Text = $"{lowStockCount} Item(s)";
                        if (lowStockCount > 0)
                        {
                            cardLowStock.BackColor = Color.FromArgb(45, 15, 15); // subtle red bg
                        }
                    }

                    // 5. Load Low Stock List into Grid
                    using (SqlDataAdapter da = new SqlDataAdapter(@"
                        SELECT Code, Name, Stock as [Stock], MinStockLevel as [Min Level] 
                        FROM Products 
                        WHERE Stock <= MinStockLevel 
                        ORDER BY Stock ASC", conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridLowStock.DataSource = dt;

                        // Set custom column fill weights to give appropriate spacing and prevent text clipping
                        if (gridLowStock.Columns["Code"] != null) gridLowStock.Columns["Code"].FillWeight = 70;
                        if (gridLowStock.Columns["Name"] != null) gridLowStock.Columns["Name"].FillWeight = 150;
                        if (gridLowStock.Columns["Stock"] != null) gridLowStock.Columns["Stock"].FillWeight = 90;
                        if (gridLowStock.Columns["Min Level"] != null) gridLowStock.Columns["Min Level"].FillWeight = 90;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = chartPanel.Width;
            int h = chartPanel.Height;
            if (w < 80 || h < 80) return;

            int leftAxis = 50;
            int rightMargin = 25;
            int bottomAxis = h - 45;
            int topAxis = 45;

            // Draw clean background grid lines
            using (Pen gridPen = new Pen(Color.FromArgb(30, 41, 59), 1))
            {
                for (int i = topAxis; i < bottomAxis; i += 45)
                {
                    g.DrawLine(gridPen, leftAxis, i, w - rightMargin, i);
                }
            }

            // Draw Y and X axis
            using (Pen axisPen = new Pen(Theme.TextDark, 2))
            {
                g.DrawLine(axisPen, leftAxis, topAxis, leftAxis, bottomAxis); // Y axis
                g.DrawLine(axisPen, leftAxis, bottomAxis, w - rightMargin, bottomAxis); // X axis
            }

            // Draw beautiful custom bars
            decimal maxVal = Math.Max(totalSales, totalCOGS);
            if (maxVal == 0) maxVal = 1000; // prevent division by zero

            int chartH = Math.Max(50, bottomAxis - topAxis - 40);
            int chartPlotWidth = (w - rightMargin) - leftAxis;

            // Dynamically calculate bar width and spacing based on available chart width
            int barW = Math.Max(65, Math.Min(130, chartPlotWidth / 5));
            int barGap = Math.Max(35, barW / 2);
            int chartCenterX = leftAxis + chartPlotWidth / 2;

            int salesBarX = chartCenterX - barW - barGap / 2;
            int cogsBarX = chartCenterX + barGap / 2;

            // 1. Sales Bar
            int salesBarH = (int)((totalSales / maxVal) * chartH);
            Rectangle salesRect = new Rectangle(salesBarX, bottomAxis - salesBarH, barW, salesBarH);
            using (Brush b = new SolidBrush(Theme.Success))
            {
                g.FillRectangle(b, salesRect);
            }
            
            // Dynamically center 'Revenue' label below Sales bar
            string revenueLabel = "Revenue";
            SizeF revLabelSize = g.MeasureString(revenueLabel, Theme.BoldFont);
            float revLabelX = salesBarX + (barW - revLabelSize.Width) / 2f;
            using (Brush bText = new SolidBrush(Theme.TextLight))
            {
                g.DrawString(revenueLabel, Theme.BoldFont, bText, revLabelX, bottomAxis + 8);
            }

            // Dynamically center Sales amount above Sales bar
            string salesText = $"Rs. {totalSales:N0}";
            SizeF salesTextSize = g.MeasureString(salesText, Theme.MainFont);
            float salesTextX = salesBarX + (barW - salesTextSize.Width) / 2f;
            float salesTextY = Math.Max(topAxis - 20, bottomAxis - salesBarH - salesTextSize.Height - 5);
            using (Brush bText = new SolidBrush(Theme.TextLight))
            {
                g.DrawString(salesText, Theme.MainFont, bText, salesTextX, salesTextY);
            }

            // 2. Cost (Goods) Bar (using Cost of Goods Sold - COGS)
            int cogsBarH = (int)((totalCOGS / maxVal) * chartH);
            Rectangle cogsRect = new Rectangle(cogsBarX, bottomAxis - cogsBarH, barW, cogsBarH);
            using (Brush b = new SolidBrush(Theme.Accent))
            {
                g.FillRectangle(b, cogsRect);
            }

            // Dynamically center 'Cost (Goods)' label below Cost bar
            string costLabel = "Cost (Goods)";
            SizeF costLabelSize = g.MeasureString(costLabel, Theme.BoldFont);
            float costLabelX = cogsBarX + (barW - costLabelSize.Width) / 2f;
            using (Brush bText = new SolidBrush(Theme.TextLight))
            {
                g.DrawString(costLabel, Theme.BoldFont, bText, costLabelX, bottomAxis + 8);
            }

            // Dynamically center Cost amount above Cost bar
            string cogsText = $"Rs. {totalCOGS:N0}";
            SizeF cogsTextSize = g.MeasureString(cogsText, Theme.MainFont);
            float cogsTextX = cogsBarX + (barW - cogsTextSize.Width) / 2f;
            float cogsTextY = Math.Max(topAxis - 20, bottomAxis - cogsBarH - cogsTextSize.Height - 5);
            using (Brush bText = new SolidBrush(Theme.TextLight))
            {
                g.DrawString(cogsText, Theme.MainFont, bText, cogsTextX, cogsTextY);
            }

            // Net Profit Label - Positioned cleanly at top left
            decimal netProfit = totalSales - totalCOGS;
            Color pColor = netProfit >= 0 ? Theme.Success : Theme.Danger;
            string pSign = netProfit >= 0 ? "+" : "";
            string pText = $"Net Performance: {pSign}Rs. {netProfit:N2}";
            using (Brush bProfit = new SolidBrush(pColor))
            {
                g.DrawString(pText, Theme.SubHeaderFont, bProfit, leftAxis, 12);
            }
        }
    }
}
