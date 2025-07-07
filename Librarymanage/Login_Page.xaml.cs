using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace Librarymanage
{
    public partial class Login_Page : Page
    {
        private Frame _mainFrame;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";


        public Login_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordTextBox.Password;

            string userRole = GetUserRoleFromDatabase(username, password);

            if (userRole == "admin")
            {
                _mainFrame.Navigate(new Admin_Page(_mainFrame));
            }
            else if (userRole == "user")
            {
                _mainFrame.Navigate(new Library_Page(_mainFrame, username)); 
            }
            else
            {
                MessageBox.Show("Incorrect username or password.");
            }
        }

        private void SignupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainFrame.Navigate(new SignUp_Page(_mainFrame));
        }

        private string GetUserRoleFromDatabase(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Username FROM Users WHERE Username = @Username AND Password = @Password";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        // If username is admin123
                        if (result.ToString() == "admin123")
                        {
                            return "admin";
                        }
                        else
                        {
                            return "user";
                        }
                    }
                    else
                    {
                        return "notfound";
                    }
                }
            }
        }
    }
}
