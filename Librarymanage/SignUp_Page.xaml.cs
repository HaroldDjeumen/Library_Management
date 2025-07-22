using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;


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


            // Validate password
            if (!IsPasswordValid(password))
            {
                MessageBox.Show("Password must be at least 8 characters long, with uppercase, lowercase, number, and special character.");
                return;
            }

            // Confirm password
            if (PasswordTextBox.Password != ConfirmPasswordTextBox.Password)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            // Save user
            AddUserToDatabase(username, password);
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

        public static void AddUserToDatabase(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Check if the username already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (SQLiteCommand checkCommand = new SQLiteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Username", username);
                    long count = (long)checkCommand.ExecuteScalar();
                    if (count > 0)
                    {
                        MessageBox.Show("Username already exists in the database.");
                        return;
                    }
                }

                // Insert the new user
                string query = "INSERT INTO Users (Username, Password, JoinDate) VALUES (@Username, @Password, @JoinDate)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
