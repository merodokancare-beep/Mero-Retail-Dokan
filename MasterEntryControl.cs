using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class MasterEntryControl : UserControl
    {
        private Panel tabHeaderPanel;
        private Panel tabContentPanel;

        private Button btnTabProducts;
        private Button btnTabCategory;
        private Button btnTabCustomers;
        private Button btnTabSuppliers;
        private Button btnTabUsers;

        // Cached instances of each master view for instant tab switching
        private ProductControl productView;
        private CategoryControl categoryView;
        private CustomerControl customerView;
        private SupplierControl supplierView;
        private UserManagementControl userView;

        private string currentActiveTab = "";

        // Event to notify parent (MainForm) when tab switches
        public event Action<string> OnTabChanged;

        public MasterEntryControl(string initialTab = "Product")
        {
            InitializeComponent();
            SelectTab(initialTab);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            bool isAdmin = string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // 1. Top Tab Header Navigation Panel
            tabHeaderPanel = new Panel();
            tabHeaderPanel.Dock = DockStyle.Top;
            tabHeaderPanel.Height = 52;
            tabHeaderPanel.BackColor = Theme.Primary;
            tabHeaderPanel.Padding = new Padding(20, 6, 20, 6);
            this.Controls.Add(tabHeaderPanel);

            FlowLayoutPanel flowTabs = new FlowLayoutPanel();
            flowTabs.Dock = DockStyle.Fill;
            flowTabs.FlowDirection = FlowDirection.LeftToRight;
            flowTabs.WrapContents = false;
            flowTabs.BackColor = Color.Transparent;
            tabHeaderPanel.Controls.Add(flowTabs);

            // Tab 1: Product Master
            btnTabProducts = CreateTabButton("📦  Product Master");
            btnTabProducts.Click += (s, e) => ShowTab("Product");
            flowTabs.Controls.Add(btnTabProducts);

            // Tab 2: Category Master
            btnTabCategory = CreateTabButton("📂  Category Master");
            btnTabCategory.Click += (s, e) => ShowTab("Category");
            flowTabs.Controls.Add(btnTabCategory);

            // Tab 3: Customer Master
            btnTabCustomers = CreateTabButton("👥  Customer Master");
            btnTabCustomers.Click += (s, e) => ShowTab("Customer");
            flowTabs.Controls.Add(btnTabCustomers);

            // Tab 4: Supplier Master
            btnTabSuppliers = CreateTabButton("🏢  Supplier Master");
            btnTabSuppliers.Click += (s, e) => ShowTab("Supplier");
            flowTabs.Controls.Add(btnTabSuppliers);

            // Tab 5: User Master (visible for Admin)
            if (isAdmin)
            {
                btnTabUsers = CreateTabButton("👤  User Master");
                btnTabUsers.Click += (s, e) => ShowTab("User");
                flowTabs.Controls.Add(btnTabUsers);
            }

            // 2. Tab Content Container Panel
            tabContentPanel = new Panel();
            tabContentPanel.Dock = DockStyle.Fill;
            tabContentPanel.BackColor = Theme.Secondary;
            this.Controls.Add(tabContentPanel);

            // Ensure proper docking order so tabHeaderPanel stays on top
            tabHeaderPanel.SendToBack();
            tabContentPanel.BringToFront();
        }

        private Button CreateTabButton(string text)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.AutoSize = true;
            btn.Height = 38;
            btn.Margin = new Padding(0, 0, 10, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = Theme.BoldFont;
            btn.Cursor = Cursors.Hand;
            btn.Padding = new Padding(12, 4, 12, 4);
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public void SelectTab(string tabKey)
        {
            if (string.IsNullOrEmpty(tabKey)) tabKey = "Product";

            if (tabKey.IndexOf("Category", StringComparison.OrdinalIgnoreCase) >= 0)
                ShowTab("Category");
            else if (tabKey.IndexOf("Customer", StringComparison.OrdinalIgnoreCase) >= 0)
                ShowTab("Customer");
            else if (tabKey.IndexOf("Supplier", StringComparison.OrdinalIgnoreCase) >= 0)
                ShowTab("Supplier");
            else if (tabKey.IndexOf("User", StringComparison.OrdinalIgnoreCase) >= 0)
                ShowTab("User");
            else
                ShowTab("Product");
        }

        private void ShowTab(string tabKey)
        {
            currentActiveTab = tabKey;

            // Highlight Tab Buttons
            StyleTabButton(btnTabProducts, tabKey == "Product");
            StyleTabButton(btnTabCategory, tabKey == "Category");
            StyleTabButton(btnTabCustomers, tabKey == "Customer");
            StyleTabButton(btnTabSuppliers, tabKey == "Supplier");
            if (btnTabUsers != null)
            {
                StyleTabButton(btnTabUsers, tabKey == "User");
            }

            // Hide currently active child views
            if (productView != null) productView.Visible = false;
            if (categoryView != null) categoryView.Visible = false;
            if (customerView != null) customerView.Visible = false;
            if (supplierView != null) supplierView.Visible = false;
            if (userView != null) userView.Visible = false;

            // Lazy-load requested view and display
            switch (tabKey)
            {
                case "Category":
                    if (categoryView == null)
                    {
                        categoryView = new CategoryControl();
                        categoryView.Dock = DockStyle.Fill;
                        tabContentPanel.Controls.Add(categoryView);
                    }
                    categoryView.Visible = true;
                    categoryView.BringToFront();
                    categoryView.Focus();
                    break;

                case "Customer":
                    if (customerView == null)
                    {
                        customerView = new CustomerControl();
                        customerView.Dock = DockStyle.Fill;
                        tabContentPanel.Controls.Add(customerView);
                    }
                    customerView.Visible = true;
                    customerView.BringToFront();
                    customerView.Focus();
                    break;

                case "Supplier":
                    if (supplierView == null)
                    {
                        supplierView = new SupplierControl();
                        supplierView.Dock = DockStyle.Fill;
                        tabContentPanel.Controls.Add(supplierView);
                    }
                    supplierView.Visible = true;
                    supplierView.BringToFront();
                    supplierView.Focus();
                    break;

                case "User":
                    if (userView == null)
                    {
                        userView = new UserManagementControl();
                        userView.Dock = DockStyle.Fill;
                        tabContentPanel.Controls.Add(userView);
                    }
                    userView.Visible = true;
                    userView.BringToFront();
                    userView.Focus();
                    break;

                case "Product":
                default:
                    if (productView == null)
                    {
                        productView = new ProductControl();
                        productView.Dock = DockStyle.Fill;
                        tabContentPanel.Controls.Add(productView);
                    }
                    productView.Visible = true;
                    productView.BringToFront();
                    productView.Focus();
                    break;
            }

            OnTabChanged?.Invoke(tabKey);
        }

        private void StyleTabButton(Button btn, bool isActive)
        {
            if (btn == null) return;

            if (isActive)
            {
                btn.BackColor = Theme.Accent;
                btn.ForeColor = Theme.TextLight;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }
            else
            {
                btn.BackColor = Color.FromArgb(17, 24, 39);
                btn.ForeColor = Theme.TextDark;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.AlternateRow;
                btn.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            }
        }

        public string ActiveTab => currentActiveTab;
    }
}
