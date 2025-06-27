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
        private string filePath = "users.txt";


        public SignUp_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void SignupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordTextBox.Password;

            // Check if username already exists
            if (File.Exists(filePath))
            {
                var existingUsers = File.ReadAllLines(filePath);
                foreach (var user in existingUsers)
                {
                    var userDetails = user.Split(',');
                    if (userDetails[0] == username)
                    {
                        MessageBox.Show("Username already exists.");
                        return;
                    }
                }
            }

            // Validate password
            if (!IsPasswordValid(password))
            {
                MessageBox.Show("Password must be at least 8 characters long, with uppercase, lowercase, number, and special character.");
                return;
            }

            // Comfirm password
            if (PasswordTextBox.Password != ConfirmPasswordTextBox.Password)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }
            // Save user
            File.AppendAllText(filePath, $"{username},{password}{Environment.NewLine}");
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
    }
}
