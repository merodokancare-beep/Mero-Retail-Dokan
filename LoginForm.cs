using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class LoginForm : Form
    {
        private Panel leftPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label userLabel;
        private TextBox txtUsername;
        private Panel userTxtPanel;
        private Label passLabel;
        private TextBox txtPassword;
        private Panel passTxtPanel;
        private Button btnLogin;
        private Button btnExit;
        private Label errLabel;

        public LoginForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
            this.Load += (s, e) => txtUsername.Focus();
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Secret developer key combination: Ctrl + Shift + L
            if (e.Control && e.Shift && e.KeyCode == Keys.L)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                using (DeveloperKeyGenForm keyGen = new DeveloperKeyGenForm())
                {
                    keyGen.ShowDialog(this);
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Mero Dokan - Login";
            this.Icon = Theme.AppIcon;
            this.ShowIcon = true;
            this.ClientSize = new Size(450, 520);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Primary;

            // Enable double buffering for smooth rendering
            this.DoubleBuffered = true;

            // Left panel indicator
            leftPanel = new Panel();
            leftPanel.Size = new Size(10, 520);
            leftPanel.Location = new Point(0, 0);
            leftPanel.BackColor = Theme.Accent;
            this.Controls.Add(leftPanel);

            // Official Logo PictureBox
            PictureBox picLoginLogo = new PictureBox();
            picLoginLogo.Size = new Size(76, 76);
            picLoginLogo.Location = new Point(40, 25);
            picLoginLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLoginLogo.BackColor = Color.Transparent;
            string logoFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon_512.png");
            if (!System.IO.File.Exists(logoFile)) logoFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
            if (System.IO.File.Exists(logoFile))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(logoFile);
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        picLoginLogo.Image = Image.FromStream(ms);
                    }
                }
                catch { }
            }
            this.Controls.Add(picLoginLogo);

            // Title
            titleLabel = new Label();
            titleLabel.Text = "Mero Dokan";
            titleLabel.Location = new Point(126, 28);
            titleLabel.AutoSize = true;
            Theme.StyleLabel(titleLabel, Theme.TextLight, new Font("Segoe UI Semibold", 24F, FontStyle.Bold));
            this.Controls.Add(titleLabel);

            // Subtitle
            subtitleLabel = new Label();
            subtitleLabel.Text = "Shop Management System";
            subtitleLabel.Location = new Point(130, 72);
            subtitleLabel.AutoSize = true;
            Theme.StyleLabel(subtitleLabel, Theme.TextDark, new Font("Segoe UI", 10.5F, FontStyle.Regular));
            this.Controls.Add(subtitleLabel);

            // Username Label
            userLabel = new Label();
            userLabel.Text = "Username";
            userLabel.Location = new Point(50, 130);
            userLabel.AutoSize = true;
            Theme.StyleLabel(userLabel, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(userLabel);

            // Username Input container panel
            userTxtPanel = new Panel();
            userTxtPanel.Size = new Size(350, 40);
            userTxtPanel.Location = new Point(50, 155);
            userTxtPanel.BackColor = Theme.Secondary;
            userTxtPanel.Padding = new Padding(10, 10, 10, 10);
            
            txtUsername = new TextBox();
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.BackColor = Theme.Secondary;
            txtUsername.ForeColor = Theme.TextLight;
            txtUsername.Font = Theme.MainFont;
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.Text = "admin"; // Default helper
            userTxtPanel.Controls.Add(txtUsername);
            this.Controls.Add(userTxtPanel);

            // Password Label
            passLabel = new Label();
            passLabel.Text = "Password";
            passLabel.Location = new Point(50, 205);
            passLabel.AutoSize = true;
            Theme.StyleLabel(passLabel, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(passLabel);

            // Password Input container panel
            passTxtPanel = new Panel();
            passTxtPanel.Size = new Size(350, 40);
            passTxtPanel.Location = new Point(50, 230);
            passTxtPanel.BackColor = Theme.Secondary;
            passTxtPanel.Padding = new Padding(10, 10, 10, 10);

            txtPassword = new TextBox();
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.BackColor = Theme.Secondary;
            txtPassword.ForeColor = Theme.TextLight;
            txtPassword.Font = Theme.MainFont;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.Text = ""; // Default helper
            passTxtPanel.Controls.Add(txtPassword);
            this.Controls.Add(passTxtPanel);

            // Error Label
            errLabel = new Label();
            errLabel.Text = "";
            errLabel.Location = new Point(50, 276);
            errLabel.Size = new Size(350, 20);
            errLabel.TextAlign = ContentAlignment.MiddleLeft;
            Theme.StyleLabel(errLabel, Theme.Danger, Theme.MainFont);
            this.Controls.Add(errLabel);

            // Login Button
            btnLogin = new Button();
            btnLogin.Text = "LOG IN";
            btnLogin.Size = new Size(350, 44);
            btnLogin.Location = new Point(50, 305);
            Theme.StylePrimaryButton(btnLogin);
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            // Exit Button
            btnExit = new Button();
            btnExit.Text = "EXIT APPLICATION";
            btnExit.Size = new Size(350, 38);
            btnExit.Location = new Point(50, 360);
            Theme.StyleDangerButton(btnExit);
            btnExit.Click += BtnExit_Click;
            this.Controls.Add(btnExit);

            // Database Connection Link
            LinkLabel lnkDbSettings = new LinkLabel();
            lnkDbSettings.Text = "⚙️ Configure Database Connection";
            lnkDbSettings.Location = new Point(50, 412);
            lnkDbSettings.Size = new Size(350, 20);
            lnkDbSettings.TextAlign = ContentAlignment.MiddleCenter;
            lnkDbSettings.ActiveLinkColor = Theme.AccentHover;
            lnkDbSettings.LinkColor = Theme.TextDark;
            lnkDbSettings.VisitedLinkColor = Theme.TextDark;
            lnkDbSettings.Font = Theme.MainFont;
            lnkDbSettings.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkDbSettings.LinkClicked += (s, e) => {
                using (var configForm = new DatabaseConfigForm())
                {
                    configForm.ShowDialog(this);
                }
            };
            this.Controls.Add(lnkDbSettings);

            // Reset Admin Password Link
            LinkLabel lnkResetAdmin = new LinkLabel();
            lnkResetAdmin.Text = "🔑 Reset Admin Password to Default";
            lnkResetAdmin.Location = new Point(50, 442);
            lnkResetAdmin.Size = new Size(350, 20);
            lnkResetAdmin.TextAlign = ContentAlignment.MiddleCenter;
            lnkResetAdmin.ActiveLinkColor = Theme.AccentHover;
            lnkResetAdmin.LinkColor = Theme.TextDark;
            lnkResetAdmin.VisitedLinkColor = Theme.TextDark;
            lnkResetAdmin.Font = Theme.MainFont;
            lnkResetAdmin.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkResetAdmin.LinkClicked += (s, e) => {
                var confirm = MessageBox.Show(
                    "Do you want to reset the 'admin' account password back to default ('admin')?\n\n" +
                    "This is helpful if a database backup was restored or you forgot the password.",
                    "Reset Administrator Password",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            string newHash = DatabaseHelper.HashPassword("admin");
                            using (SqlCommand cmd = new SqlCommand("UPDATE Users SET PasswordHash = @hash WHERE Username = 'admin'", conn))
                            {
                                cmd.Parameters.AddWithValue("@hash", newHash);
                                int count = cmd.ExecuteNonQuery();
                                if (count > 0)
                                {
                                    txtUsername.Text = "admin";
                                    txtPassword.Text = "admin";
                                    errLabel.Text = "";
                                    MessageBox.Show("Administrator password successfully reset to: admin\n\nYou can now click 'LOG IN'.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Admin user was not found in the database.", "User Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                    catch (Exception exReset)
                    {
                        MessageBox.Show($"Failed to reset admin password:\n{exReset.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            this.Controls.Add(lnkResetAdmin);

            // Enter key binding
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errLabel.Text = "Please enter both Username and Password.";
                return;
            }

            try
            {
                string hashedInput = DatabaseHelper.HashPassword(password);
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT Id, Username, FullName, Role 
                        FROM Users 
                        WHERE Username = @username AND PasswordHash = @password", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", hashedInput);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Session.UserId = reader.GetInt32(0);
                                Session.Username = reader.GetString(1);
                                Session.FullName = reader.GetString(2);
                                Session.Role = reader.GetString(3);

                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                errLabel.Text = "Invalid username or password.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errLabel.Text = "Database connection failed. Check LocalDB.";
                MessageBox.Show($"Connection Error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
