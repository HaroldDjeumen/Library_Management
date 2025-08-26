using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Librarymanage
{
    /// <summary>
    /// Interaction logic for SignUp_Page.xaml
    /// </summary>
    public partial class SignUp_Page : Page
    {
        private Frame _mainFrame;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";


        public SignUp_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void SignupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordTextBox.Password;
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();


            if (!ValidateUserInput(password, email, phone))
                return;

            // Confirm password
            if (PasswordTextBox.Password != ConfirmPasswordTextBox.Password)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            // Save user
            AddUserToDatabase(username, password, email, phone);
            MessageBox.Show("User registered successfully!");

            // Optional: Go back to Login Page
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private bool IsPasswordValid(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsUpper))
                return false;

            if (!password.Any(char.IsLower))
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;

            return true;
        }


        private bool IsEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // must contain @
            if (!email.Contains("@"))
                return false;

            // must contain .
            if (!email.Contains("."))
                return false;

            // very basic structure check
            int atIndex = email.IndexOf("@");
            int dotIndex = email.LastIndexOf(".");

            if (atIndex < 1 || dotIndex < atIndex + 2 || dotIndex == email.Length - 1)
                return false;

            return true;
        }

        private bool IsPhoneValid(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // allow "0xxxxxxxxx" (10 digits)
            if (phone.Length == 10 && phone.StartsWith("0") && phone.All(char.IsDigit))
                return true;

            // allow "+27xxxxxxxxx" (12 digits)
            if (phone.Length == 12 && phone.StartsWith("+27") && phone.Skip(3).All(char.IsDigit))
                return true;

            return false;
        }

        private bool ValidateUserInput(string password, string email, string phone)
        {
            if (!IsPasswordValid(password))
            {
                MessageBox.Show("Password must be at least 8 characters long, with uppercase, lowercase, number, and special character.");
                return false;
            }

            if (!IsEmailValid(email))
            {
                MessageBox.Show("Please enter a valid email address.");
                return false;
            }

            if (!IsPhoneValid(phone))
            {
                MessageBox.Show("Please enter a valid phone number (e.g. 0821234567 or +27821234567).");
                return false;
            }

            return true;
        }

        public static void AddUserToDatabase(string username, string password, string email, string phone)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT 
                     (SELECT COUNT(*) FROM Users WHERE Username = @Username) AS UsernameCount,
                     (SELECT COUNT(*) FROM Users WHERE Email = @Email) AS EmailCount,
                     (SELECT COUNT(*) FROM Users WHERE Phone = @Phone) AS PhoneCount;
";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            List<string> duplicates = new List<string>();

                            if (reader.GetInt64(0) > 0)
                                duplicates.Add("Username");
                            if (reader.GetInt64(1) > 0)
                                duplicates.Add("Email");
                            if (reader.GetInt64(2) > 0)
                                duplicates.Add("Phone");

                            if (duplicates.Count > 0)
                            {
                                string message = string.Join(" and ", duplicates) + " already exist(s) in the database.";
                                MessageBox.Show(message);
                                return;
                            }
                        }
                    }
                }


                // Insert the new user
                query = "INSERT INTO Users (Username, Password, Email, Phone, JoinDate) VALUES (@Username, @Password, @Email, @Phone, @JoinDate)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
