using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

namespace MeroDokan
{
    public static class DatabaseHelper
    {
        public class DbConfig
        {
            public string Server { get; set; } = "(localdb)\\MSSQLLocalDB";
            public string Database { get; set; } = "MeroDokanDB";
            public bool IntegratedSecurity { get; set; } = true;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public int ConnectionTimeout { get; set; } = 30;
            public int ConnectRetryCount { get; set; } = 3;
            public int ConnectRetryInterval { get; set; } = 10;
        }

        private static DbConfig _cachedConfig = null;
        private static string _cachedLocalDbServer = null;
        private static string _cachedLocalDbPipe = null;
        private static DateTime _lastResolvedTime = DateTime.MinValue;

        private static DbConfig GetCachedConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = LoadConfig();
            }
            return _cachedConfig;
        }

        public static string ConnectionString
        {
            get
            {
                return BuildConnectionString(GetCachedConfig());
            }
            set
            {
            }
        }

        public static string MasterConnectionString
        {
            get
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(ConnectionString);
                    builder.InitialCatalog = "master";
                    return builder.ConnectionString;
                }
                catch
                {
                    return "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;";
                }
            }
            set
            {
            }
        }

        public static string GetConfigFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localFile = Path.Combine(appDir, "dbconfig.txt");
            try
            {
                // Test write permissions
                string testFile = Path.Combine(appDir, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return localFile;
            }
            catch
            {
                // Fallback to LocalApplicationData
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeroDokan");
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
                return Path.Combine(appDataDir, "dbconfig.txt");
            }
        }

        public static DbConfig LoadConfig()
        {
            var config = new DbConfig();
            string path = GetConfigFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || !line.Contains("="))
                            continue;
                        
                        int idx = line.IndexOf('=');
                        string key = line.Substring(0, idx).Trim();
                        string val = line.Substring(idx + 1).Trim();

                        if (key.Equals("Server", StringComparison.OrdinalIgnoreCase)) config.Server = val;
                        else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase)) config.Database = val;
                        else if (key.Equals("IntegratedSecurity", StringComparison.OrdinalIgnoreCase)) config.IntegratedSecurity = bool.Parse(val);
                        else if (key.Equals("Username", StringComparison.OrdinalIgnoreCase)) config.Username = val;
                        else if (key.Equals("Password", StringComparison.OrdinalIgnoreCase)) config.Password = val;
                        else if (key.Equals("ConnectionTimeout", StringComparison.OrdinalIgnoreCase)) config.ConnectionTimeout = int.Parse(val);
                        else if (key.Equals("ConnectRetryCount", StringComparison.OrdinalIgnoreCase)) config.ConnectRetryCount = int.Parse(val);
                        else if (key.Equals("ConnectRetryInterval", StringComparison.OrdinalIgnoreCase)) config.ConnectRetryInterval = int.Parse(val);
                    }
                }
                catch { }
            }
            else
            {
                config.Server = ResolveFirstRunServer();
                SaveConfig(config);
            }
            return config;
        }

        public static void SaveConfig(DbConfig config)
        {
            try
            {
                string path = GetConfigFilePath();
                var sb = new StringBuilder();
                sb.AppendLine("Server=" + config.Server);
                sb.AppendLine("Database=" + config.Database);
                sb.AppendLine("IntegratedSecurity=" + config.IntegratedSecurity.ToString());
                sb.AppendLine("Username=" + config.Username);
                sb.AppendLine("Password=" + config.Password);
                sb.AppendLine("ConnectionTimeout=" + config.ConnectionTimeout.ToString());
                sb.AppendLine("ConnectRetryCount=" + config.ConnectRetryCount.ToString());
                sb.AppendLine("ConnectRetryInterval=" + config.ConnectRetryInterval.ToString());
                File.WriteAllText(path, sb.ToString());
                
                _cachedConfig = config;

                // Discard stale connections
                SqlConnection.ClearAllPools();
            }
            catch { }
        }

        private static void LoadConnectionString()
        {
            GetCachedConfig();
        }

        private static bool? _isConnectRetrySupported = null;
        public static bool IsConnectRetrySupported
        {
            get
            {
                if (!_isConnectRetrySupported.HasValue)
                {
                    _isConnectRetrySupported = TestKeywordSupport("Connect Retry Count");
                }
                return _isConnectRetrySupported.Value;
            }
        }

        private static bool TestKeywordSupport(string keyword)
        {
            try
            {
                using (var conn = new SqlConnection("Server=dummy;" + keyword + "=1;"))
                {
                    string s = conn.ConnectionString;
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static string BuildConnectionString(DbConfig config)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder();
                builder.DataSource = ResolveLocalDbServerName(config.Server);
                builder.InitialCatalog = config.Database;
                builder.IntegratedSecurity = config.IntegratedSecurity;
                if (!config.IntegratedSecurity)
                {
                    builder.UserID = config.Username;
                    builder.Password = config.Password;
                }
                builder.ConnectTimeout = config.ConnectionTimeout;
                
                string connStr = builder.ConnectionString;
                if (!connStr.EndsWith(";"))
                    connStr += ";";
                
                connStr += "Encrypt=False;TrustServerCertificate=True;";
                
                if (IsConnectRetrySupported)
                {
                    connStr += "Connect Retry Count=" + config.ConnectRetryCount + ";";
                    connStr += "Connect Retry Interval=" + config.ConnectRetryInterval + ";";
                }
                
                return connStr;
            }
            catch
            {
                return "Server=(localdb)\\MSSQLLocalDB;Database=MeroDokanDB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
            }
        }

        private static string FindSqlLocalDBPath()
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    proc.WaitForExit(1000);
                    return "sqllocaldb";
                }
            }
            catch { }

            var searchFolders = new System.Collections.Generic.List<string>();
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            string[] versions = { "160", "150", "140", "130", "120", "110" };
            foreach (var ver in versions)
            {
                if (!string.IsNullOrEmpty(pf))
                {
                    searchFolders.Add(Path.Combine(pf, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
                if (!string.IsNullOrEmpty(pf86))
                {
                    searchFolders.Add(Path.Combine(pf86, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
            }

            foreach (var folder in searchFolders)
            {
                string fullPath = Path.Combine(folder, "SqlLocalDB.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static System.Collections.Generic.List<string> GetLocalDBInstances(string localDbPath)
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("Microsoft"))
                            {
                                list.Add(trimmed);
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static void GetLocalDBInfo(string localDbPath, string instanceName, out string state, out string pipeName)
        {
            state = "Stopped";
            pipeName = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info \"" + instanceName + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("State:", StringComparison.OrdinalIgnoreCase))
                            {
                                int idx = trimmed.IndexOf(':');
                                if (idx != -1)
                                {
                                    state = trimmed.Substring(idx + 1).Trim();
                                }
                            }
                            int pipeIdx = trimmed.IndexOf("np:", StringComparison.OrdinalIgnoreCase);
                            if (pipeIdx != -1)
                            {
                                pipeName = trimmed.Substring(pipeIdx).Trim();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static string GetLocalDBDiagnostics()
        {
            var sb = new StringBuilder();
            string localDbPath = FindSqlLocalDBPath();
            if (string.IsNullOrEmpty(localDbPath))
            {
                sb.AppendLine("sqllocaldb utility not found in PATH or standard Program Files folders.");
                return sb.ToString();
            }

            sb.AppendLine("sqllocaldb executable path: " + localDbPath);

            // Run sqllocaldb -v
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(2000);
                    sb.AppendLine("Version: " + stdout.Trim() + " " + stderr.Trim());
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to run version check: " + ex.Message);
            }

            // Run sqllocaldb info
            System.Collections.Generic.List<string> instances = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(2000);
                    sb.AppendLine("Instances:\n" + stdout.Trim() + " " + stderr.Trim());

                    instances = new System.Collections.Generic.List<string>();
                    string[] lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("Microsoft"))
                        {
                            instances.Add(trimmed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to run info check: " + ex.Message);
            }

            // Run sqllocaldb info <instance>
            if (instances != null && instances.Count > 0)
            {
                foreach (var inst in instances)
                {
                    try
                    {
                        var info = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = localDbPath,
                            Arguments = "info \"" + inst + "\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        using (var proc = System.Diagnostics.Process.Start(info))
                        {
                            string stdout = proc.StandardOutput.ReadToEnd();
                            string stderr = proc.StandardError.ReadToEnd();
                            proc.WaitForExit(2000);
                            sb.AppendLine("\nInstance Details (" + inst + "):\n" + stdout.Trim() + " " + stderr.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("Failed to run info for " + inst + ": " + ex.Message);
                    }
                }
            }

            return sb.ToString();
        }

        private static void TryStartLocalDB()
        {
            string localDbPath = FindSqlLocalDBPath();
            if (string.IsNullOrEmpty(localDbPath))
            {
                return;
            }

            var instances = GetLocalDBInstances(localDbPath);
            
            // Ensure default instances are created if not present
            string[] defaultInstances = { "MSSQLLocalDB", "v11.0" };
            foreach (var defaultInst in defaultInstances)
            {
                bool exists = false;
                foreach (var inst in instances)
                {
                    if (string.Equals(inst, defaultInst, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    try
                    {
                        var createInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = localDbPath,
                            Arguments = "create " + defaultInst,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        using (var proc = System.Diagnostics.Process.Start(createInfo))
                        {
                            proc.WaitForExit(5000);
                        }
                    }
                    catch { }
                }
            }

            // Refresh instances list
            instances = GetLocalDBInstances(localDbPath);

            foreach (var instance in instances)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = localDbPath,
                        Arguments = "start \"" + instance + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    using (var proc = System.Diagnostics.Process.Start(startInfo))
                    {
                        proc.WaitForExit(3000);
                    }
                }
                catch
                {
                }
            }
        }

        public static void ResolveConnectionStrings()
        {
            _cachedLocalDbPipe = null;
            _lastResolvedTime = DateTime.MinValue;
            GetCachedConfig();
        }

        public static string ResolveFirstRunServer()
        {
            TryStartLocalDB();

            var serverList = new System.Collections.Generic.List<string>();

            // 1. Add dynamically discovered LocalDB instances first
            string localDbPath = FindSqlLocalDBPath();
            if (!string.IsNullOrEmpty(localDbPath))
            {
                var instances = GetLocalDBInstances(localDbPath);
                foreach (var inst in instances)
                {
                    string srvName = "(localdb)\\" + inst;
                    if (!serverList.Contains(srvName))
                    {
                        serverList.Add(srvName);
                    }
                }
            }

            // 2. Add standard static server names
            string[] standardServers = {
                "(localdb)\\MSSQLLocalDB",
                "(localdb)\\v11.0",
                ".\\SQLEXPRESS",
                "localhost\\SQLEXPRESS",
                "(local)\\SQLEXPRESS",
                "localhost",
                "."
            };

            foreach (var srv in standardServers)
            {
                if (!serverList.Contains(srv))
                {
                    serverList.Add(srv);
                }
            }

            // 3. Probe each server connection
            foreach (string server in serverList)
            {
                string testServer = server;
                if (server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
                {
                    string instanceName = server.Substring(10).Trim();
                    string state;
                    string pipeName;
                    GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);
                    if (!string.IsNullOrEmpty(pipeName))
                    {
                        testServer = pipeName;
                    }
                }

                string masterTest = "Server=" + testServer + ";Database=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=2;";
                try
                {
                    using (SqlConnection conn = new SqlConnection(masterTest))
                    {
                        conn.Open();
                        return server;
                    }
                }
                catch
                {
                    // Try next
                }
            }

            // Fallback: use the first server we probed, or (localdb)\MSSQLLocalDB if none
            return serverList.Count > 0 ? serverList[0] : "(localdb)\\MSSQLLocalDB";
        }

        public static string ResolveLocalDbServerName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
                return serverName;

            if (serverName.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
            {
                if (serverName.Equals(_cachedLocalDbServer, StringComparison.OrdinalIgnoreCase) && 
                    (DateTime.UtcNow - _lastResolvedTime).TotalSeconds < 10 &&
                    !string.IsNullOrEmpty(_cachedLocalDbPipe))
                {
                    return _cachedLocalDbPipe;
                }

                string instanceName = serverName.Substring(10).Trim();
                string localDbPath = FindSqlLocalDBPath();
                if (!string.IsNullOrEmpty(localDbPath))
                {
                    string state = "Stopped";
                    string pipeName = null;

                    // 1. Get initial state
                    GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);

                    // 2. If it is stopped or starting, trigger start command and clear connection pools
                    bool wasStopped = !state.Equals("Running", StringComparison.OrdinalIgnoreCase);
                    if (wasStopped)
                    {
                        try
                        {
                            SqlConnection.ClearAllPools();
                        }
                        catch { }

                        try
                        {
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = localDbPath,
                                Arguments = "start \"" + instanceName + "\"",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            };
                            using (var proc = System.Diagnostics.Process.Start(startInfo))
                            {
                                proc.WaitForExit(3000);
                            }
                        }
                        catch { }
                    }

                    // 3. Poll until state is "Running" and pipeName is available (up to 10 seconds timeout)
                    int attempts = 0;
                    while (attempts < 20) // 20 * 500ms = 10 seconds
                    {
                        GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);
                        if (state.Equals("Running", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pipeName))
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(500);
                        attempts++;
                    }

                    if (!string.IsNullOrEmpty(pipeName))
                    {
                        _cachedLocalDbServer = serverName;
                        _cachedLocalDbPipe = pipeName;
                        _lastResolvedTime = DateTime.UtcNow;
                        return pipeName;
                    }
                }
            }
            return serverName;
        }

        public static void InitializeDatabase()
        {
            try
            {
                // 1. Create Database if it doesn't exist
                using (SqlConnection masterConn = new SqlConnection(MasterConnectionString))
                {
                    masterConn.Open();
                    bool dbExists = false;
                    using (SqlCommand cmd = new SqlCommand("SELECT database_id FROM sys.databases WHERE name = 'MeroDokanDB'", masterConn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            dbExists = true;
                        }
                    }

                    if (!dbExists)
                    {
                        using (SqlCommand cmd = new SqlCommand("CREATE DATABASE MeroDokanDB", masterConn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // 2. Create Tables inside MeroDokanDB
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Users Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE Users (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Username NVARCHAR(50) NOT NULL UNIQUE,
                                PasswordHash NVARCHAR(255) NOT NULL,
                                FullName NVARCHAR(100) NOT NULL,
                                Role NVARCHAR(20) NOT NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Customers Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
                        BEGIN
                            CREATE TABLE Customers (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL,
                                Phone NVARCHAR(20) NULL,
                                Email NVARCHAR(100) NULL,
                                Address NVARCHAR(200) NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Suppliers Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
                        BEGIN
                            CREATE TABLE Suppliers (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL,
                                ContactPerson NVARCHAR(100) NULL,
                                Phone NVARCHAR(20) NULL,
                                Email NVARCHAR(100) NULL,
                                Address NVARCHAR(200) NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Categories Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                        BEGIN
                            CREATE TABLE Categories (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) UNIQUE NOT NULL
                            )
                        END", conn);

                    // Products Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                        BEGIN
                            CREATE TABLE Products (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Name NVARCHAR(150) NOT NULL,
                                Description NVARCHAR(500) NULL,
                                Category NVARCHAR(100) NULL,
                                PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                SalesPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Stock INT NOT NULL DEFAULT 0,
                                MinStockLevel INT NOT NULL DEFAULT 5,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Purchases Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
                        BEGIN
                            CREATE TABLE Purchases (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                PurchaseNumber NVARCHAR(50) NOT NULL UNIQUE,
                                SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
                                PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
                                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END", conn);

                    // PurchaseDetails Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseDetails')
                        BEGIN
                            CREATE TABLE PurchaseDetails (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                PurchaseId INT FOREIGN KEY REFERENCES Purchases(Id) ON DELETE CASCADE,
                                ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                Quantity INT NOT NULL,
                                PurchasePrice DECIMAL(18,2) NOT NULL
                            )
                        END", conn);

                    // Sales Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
                        BEGIN
                            CREATE TABLE Sales (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                                CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
                                SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
                                SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Tax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                GrandTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'AmountPaid')
                            BEGIN
                                ALTER TABLE Sales ADD AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DueAmount')
                            BEGIN
                                ALTER TABLE Sales ADD DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                        END", conn);

                    // SaleDetails Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleDetails')
                        BEGIN
                            CREATE TABLE SaleDetails (
                                  Id INT PRIMARY KEY IDENTITY(1,1),
                                  SaleId INT FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                                  ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                  Quantity INT NOT NULL,
                                  UnitPrice DECIMAL(18,2) NOT NULL,
                                  Total DECIMAL(18,2) NOT NULL,
                                  PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'PurchaseCostAtSale')
                            BEGIN
                                ALTER TABLE SaleDetails ADD PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                                
                                -- Backfill existing rows with current products' purchase price
                                EXEC('UPDATE sd
                                      SET sd.PurchaseCostAtSale = p.PurchasePrice
                                      FROM SaleDetails sd
                                      INNER JOIN Products p ON sd.ProductId = p.Id');
                            END
                        END", conn);

                    // ProductPriceHistory Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductPriceHistory')
                        BEGIN
                            CREATE TABLE ProductPriceHistory (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                OldPurchasePrice DECIMAL(18,2) NOT NULL,
                                NewPurchasePrice DECIMAL(18,2) NOT NULL,
                                OldSalesPrice DECIMAL(18,2) NOT NULL,
                                NewSalesPrice DECIMAL(18,2) NOT NULL,
                                ChangeDate DATETIME NOT NULL DEFAULT GETDATE(),
                                ChangedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                Source NVARCHAR(100) NOT NULL
                            )
                        END", conn);

                    // SalesReturns Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturns')
                        BEGIN
                            CREATE TABLE SalesReturns (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ReturnNumber NVARCHAR(50) UNIQUE NOT NULL,
                                SaleId INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                                ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
                                TotalRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CashRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SalesReturns') AND name = 'CashRefund')
                            BEGIN
                                ALTER TABLE SalesReturns ADD CashRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                        END", conn);

                    // SalesReturnDetails Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturnDetails')
                        BEGIN
                            CREATE TABLE SalesReturnDetails (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ReturnId INT NOT NULL FOREIGN KEY REFERENCES SalesReturns(Id) ON DELETE CASCADE,
                                ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id),
                                Quantity INT NOT NULL,
                                RefundPrice DECIMAL(18,2) NOT NULL,
                                Total DECIMAL(18,2) NOT NULL,
                                ItemCondition NVARCHAR(50) NOT NULL
                            )
                        END", conn);

                    // CustomerPayments Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerPayments')
                        BEGIN
                            CREATE TABLE CustomerPayments (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE CASCADE,
                                PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
                                Amount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                                Remarks NVARCHAR(200) NULL,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                SaleId INT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE SET NULL
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerPayments') AND name = 'SaleId')
                            BEGIN
                                ALTER TABLE CustomerPayments ADD SaleId INT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE SET NULL;
                            END
                        END", conn);

                    // DailySettlements Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailySettlements')
                        BEGIN
                            CREATE TABLE DailySettlements (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                SettlementDate DATETIME NOT NULL DEFAULT GETDATE(),
                                OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CashSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DueCollections DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CardQRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DuesCreated DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                ExpectedCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                ActualCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Variance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                SettlementBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                Remarks NVARCHAR(500) NULL
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DailySettlements') AND name = 'Refunds')
                            BEGIN
                                ALTER TABLE DailySettlements ADD Refunds DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                        END", conn);

                    // AppProfile Configuration Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppProfile')
                        BEGIN
                            CREATE TABLE AppProfile (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                OwnerName NVARCHAR(100) NOT NULL DEFAULT 'Shop Owner',
                                ShopName NVARCHAR(150) NOT NULL DEFAULT 'Mero Dokan Shop',
                                Phone NVARCHAR(50) NOT NULL DEFAULT '+977-1-4200000',
                                Email NVARCHAR(100) NOT NULL DEFAULT 'contact@merodokan.com',
                                Address NVARCHAR(200) NOT NULL DEFAULT 'Kathmandu, Nepal',
                                LogoPath NVARCHAR(500) NULL,
                                ProfilePicPath NVARCHAR(500) NULL,
                                ThemePreset NVARCHAR(50) NOT NULL DEFAULT 'Dark Slate',
                                FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium',
                                BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroDokan\DailyDatabaseBackup',
                                GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec',
                                GSTIN NVARCHAR(50) NULL
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'FontSizePreset')
                            BEGIN
                                ALTER TABLE AppProfile ADD FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium';
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'BackupFolderPath')
                            BEGIN
                                ALTER TABLE AppProfile ADD BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroDokan\DailyDatabaseBackup';
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'GoogleDriveAddress')
                            BEGIN
                                  ALTER TABLE AppProfile ADD GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec';
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'GSTIN')
                            BEGIN
                                ALTER TABLE AppProfile ADD GSTIN NVARCHAR(50) NULL;
                            END
                        END", conn);

                    // 3. Seed Default Admin User if none exists
                    int userCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                    {
                        userCount = (int)cmd.ExecuteScalar();
                    }

                    if (userCount == 0)
                    {
                        string adminPassHash = HashPassword("admin");
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Users (Username, PasswordHash, FullName, Role) 
                            VALUES (@username, @password, @fullname, @role)", conn))
                        {
                            cmd.Parameters.AddWithValue("@username", "admin");
                            cmd.Parameters.AddWithValue("@password", adminPassHash);
                            cmd.Parameters.AddWithValue("@fullname", "System Administrator");
                            cmd.Parameters.AddWithValue("@role", "Admin");
                            cmd.ExecuteNonQuery();
                        }

                        // Seed some default customers and suppliers for presentation
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Customers (Name, Phone, Email, Address) VALUES 
                            ('Walk-in Customer', '0000000000', 'walkin@merodokan.com', 'Local'),
                            ('Hari Prasad', '9841234567', 'hari@gmail.com', 'Kathmandu'),
                            ('Sita Kumari', '9851234567', 'sita@yahoo.com', 'Lalitpur');
                            
                            INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address) VALUES 
                            ('KTM Distributors', 'Ramesh Sen', '9801122334', 'ktmdist@gmail.com', 'New Road, Kathmandu'),
                            ('National Wholesalers', 'Binod Chaudhary', '9812233445', 'national@wholesaler.com', 'Birgunj');", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 4. Seed Default Categories if none exist
                    int categoryCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Categories", conn))
                    {
                        categoryCount = (int)cmd.ExecuteScalar();
                    }

                    if (categoryCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Categories (Name) VALUES 
                            ('Groceries'),
                            ('Beverages'),
                            ('Snacks'),
                            ('Electronics'),
                            ('Clothing'),
                            ('Cosmetics'),
                            ('Others')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 5. Seed Default AppProfile if none exists
                    int profileCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AppProfile", conn))
                    {
                        profileCount = (int)cmd.ExecuteScalar();
                    }

                    if (profileCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO AppProfile (OwnerName, ShopName, Phone, Email, Address, ThemePreset, BackupFolderPath, GoogleDriveAddress) 
                             VALUES ('Shop Owner', 'Mero Dokan Shop', '+977-1-4200000', 'contact@merodokan.com', 'Kathmandu, Nepal', 'Dark', 'D:\MeroDokan\DailyDatabaseBackup', 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Ensure the Google Drive Address is updated if it is still pointing to the old defaults
                    ExecuteNonQuery(@"
                        UPDATE AppProfile 
                        SET GoogleDriveAddress = 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec'
                        WHERE GoogleDriveAddress IN (
                            'https://drive.google.com/drive/folders/MeroDokanBackup',
                            'https://drive.google.com/drive/u/2/folders/1upBC2D3VeMMjnQt7byuvYtE1P80TsfKJ'
                        )", conn);

                    // Run chronological payments allocation migration
                    MigratePaymentsToSales();

                    // Backfill legacy Sales records to make them mathematically consistent in reports
                    ExecuteNonQuery(@"
                        UPDATE Sales 
                        SET AmountPaid = GrandTotal 
                        WHERE AmountPaid = 0.00 AND DueAmount = 0.00 AND GrandTotal > 0.00", conn);
                }
            }
            catch (SqlException ex)
            {
                string localDbPath = FindSqlLocalDBPath();
                if (string.IsNullOrEmpty(localDbPath))
                {
                    throw new Exception("Microsoft SQL Server LocalDB is not installed on this machine.\n\n" +
                                        "Please download and install Microsoft SQL Server LocalDB (v11.0 or newer, e.g. SQL Server 2019/2022 LocalDB) to run the application.\n" +
                                        "You can obtain the installer from Microsoft's SQL Server Express download page.\n\n" +
                                        "Error Details: " + ex.Message, ex);
                }
                else
                {
                    string diagnostics = GetLocalDBDiagnostics();
                    throw new Exception("Microsoft SQL Server LocalDB is installed, but the connection could not be established.\n\n" +
                                        "Please try resetting your LocalDB instance by running these commands in Command Prompt:\n" +
                                        "1. sqllocaldb stop MSSQLLocalDB\n" +
                                        "2. sqllocaldb delete MSSQLLocalDB\n" +
                                        "3. sqllocaldb create MSSQLLocalDB\n" +
                                        "4. sqllocaldb start MSSQLLocalDB\n" +
                                        "(Replace 'MSSQLLocalDB' with your actual instance name, such as 'v11.0', if different)\n\n" +
                                        "---------------------------------------\n" +
                                        "LOCALDB DIAGNOSTIC SYSTEM INFO:\n" +
                                        "---------------------------------------\n" +
                                        diagnostics + "\n" +
                                        "---------------------------------------\n\n" +
                                        "Error Details: " + ex.Message, ex);
                }
            }
        }

        private class SaleDueInfo
        {
            public int Id { get; set; }
            public decimal InitialDue { get; set; }
        }

        private class PaymentInfo
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Method { get; set; }
            public string Remarks { get; set; }
            public int? User { get; set; }
        }

        public static void MigratePaymentsToSales()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Check if there are any payments without SaleId
                    string checkSql = "SELECT COUNT(*) FROM CustomerPayments WHERE SaleId IS NULL";
                    int unlinkedPayments = 0;
                    using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                    {
                        unlinkedPayments = (int)cmd.ExecuteScalar();
                    }

                    if (unlinkedPayments == 0) return;

                    // Fetch all customers who have unlinked payments
                    var customerIds = new System.Collections.Generic.List<int>();
                    string getCustsSql = "SELECT DISTINCT CustomerId FROM CustomerPayments WHERE SaleId IS NULL";
                    using (SqlCommand cmd = new SqlCommand(getCustsSql, conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                customerIds.Add(rdr.GetInt32(0));
                            }
                        }
                    }

                    foreach (int custId in customerIds)
                    {
                        // Start a transaction for each customer
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Get all sales for this customer with their original due amount (GrandTotal - AmountPaid)
                                // ordered by date/id
                                var sales = new System.Collections.Generic.List<SaleDueInfo>();
                                string salesSql = "SELECT Id, GrandTotal, AmountPaid FROM Sales WHERE CustomerId = @custId ORDER BY SaleDate ASC, Id ASC";
                                using (SqlCommand cmd = new SqlCommand(salesSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@custId", custId);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        while (rdr.Read())
                                        {
                                            int saleId = rdr.GetInt32(0);
                                            decimal grand = rdr.GetDecimal(1);
                                            decimal paid = rdr.GetDecimal(2);
                                            decimal initialDue = grand - paid;
                                            if (initialDue > 0)
                                            {
                                                sales.Add(new SaleDueInfo { Id = saleId, InitialDue = initialDue });
                                            }
                                        }
                                    }
                                }

                                // Get all payments for this customer where SaleId is null
                                // ordered by payment date/id
                                var payments = new System.Collections.Generic.List<PaymentInfo>();
                                string paymentsSql = "SELECT Id, Amount, PaymentDate, PaymentMethod, Remarks, CreatedBy FROM CustomerPayments WHERE CustomerId = @custId AND SaleId IS NULL ORDER BY PaymentDate ASC, Id ASC";
                                using (SqlCommand cmd = new SqlCommand(paymentsSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@custId", custId);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        while (rdr.Read())
                                        {
                                            payments.Add(new PaymentInfo {
                                                Id = rdr.GetInt32(0),
                                                Amount = rdr.GetDecimal(1),
                                                Date = rdr.GetDateTime(2),
                                                Method = rdr.GetString(3),
                                                Remarks = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                                                User = rdr.IsDBNull(5) ? (int?)null : rdr.GetInt32(5)
                                            });
                                        }
                                    }
                                }

                                // Match payments to sales
                                int saleIdx = 0;
                                foreach (var pay in payments)
                                {
                                    decimal remainingPay = pay.Amount;
                                    bool isFirstAlloc = true;

                                    while (remainingPay > 0 && saleIdx < sales.Count)
                                    {
                                        var activeSale = sales[saleIdx];
                                        
                                        // Load how much has been allocated to this sale so far from database
                                        decimal allocatedSoFar = 0;
                                        string getAllocSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE SaleId = @saleId";
                                        using (SqlCommand cmd = new SqlCommand(getAllocSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                            allocatedSoFar = Convert.ToDecimal(cmd.ExecuteScalar());
                                        }

                                        decimal remainingDue = activeSale.InitialDue - allocatedSoFar;
                                        if (remainingDue <= 0)
                                        {
                                            saleIdx++;
                                            continue;
                                        }

                                        decimal alloc = Math.Min(remainingPay, remainingDue);
                                        
                                        if (isFirstAlloc)
                                        {
                                            // Update the first matching record in CustomerPayments
                                            string updatePaySql = "UPDATE CustomerPayments SET SaleId = @saleId, Amount = @amount WHERE Id = @payId";
                                            using (SqlCommand cmd = new SqlCommand(updatePaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                                cmd.Parameters.AddWithValue("@amount", alloc);
                                                cmd.Parameters.AddWithValue("@payId", pay.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                            isFirstAlloc = false;
                                        }
                                        else
                                        {
                                            // Insert a split payment record for the remainder
                                            string insertPaySql = @"
                                                INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                                VALUES (@custId, @date, @amount, @method, @remarks, @user, @saleId)";
                                            using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@custId", custId);
                                                cmd.Parameters.AddWithValue("@date", pay.Date);
                                                cmd.Parameters.AddWithValue("@amount", alloc);
                                                cmd.Parameters.AddWithValue("@method", pay.Method);
                                                cmd.Parameters.AddWithValue("@remarks", pay.Remarks);
                                                cmd.Parameters.AddWithValue("@user", (object)pay.User ?? DBNull.Value);
                                                cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }

                                        remainingPay -= alloc;
                                    }

                                    // If there is still payment left over after matching all sales (overpayment)
                                    if (remainingPay > 0)
                                    {
                                        if (isFirstAlloc)
                                        {
                                            // It remains unlinked (SaleId = null)
                                            string updatePaySql = "UPDATE CustomerPayments SET SaleId = NULL, Amount = @amount WHERE Id = @payId";
                                            using (SqlCommand cmd = new SqlCommand(updatePaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@amount", remainingPay);
                                                cmd.Parameters.AddWithValue("@payId", pay.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            // Insert split payment record with null SaleId
                                            string insertPaySql = @"
                                                INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                                VALUES (@custId, @date, @amount, @method, @remarks, @user, NULL)";
                                            using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@custId", custId);
                                                cmd.Parameters.AddWithValue("@date", pay.Date);
                                                cmd.Parameters.AddWithValue("@amount", remainingPay);
                                                cmd.Parameters.AddWithValue("@method", pay.Method);
                                                cmd.Parameters.AddWithValue("@remarks", pay.Remarks);
                                                cmd.Parameters.AddWithValue("@user", (object)pay.User ?? DBNull.Value);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }

                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                System.Diagnostics.Debug.WriteLine("Customer transaction failed: " + ex.Message);
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Migration failed: " + ex.Message);
            }
        }

        private static void ExecuteNonQuery(string sql, SqlConnection conn)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
