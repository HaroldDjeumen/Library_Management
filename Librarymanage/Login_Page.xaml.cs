using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Librarymanage
{
    
    public partial class Login_Page : Page
    {
        private Frame _mainFrame;
        private string filePath = "users.txt";

        public Login_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordTextBox.Password;

            // Check if file exists
            if (!File.Exists(filePath))
            {
                MessageBox.Show("No users found. Please register first.");
                return;
            }

            // Check if username and password match
            var users = File.ReadAllLines(filePath);
            foreach (var user in users)
            {
                var userDetails = user.Split(',');
                if (userDetails[0] == username && userDetails[1] == password)
                {
                    MessageBox.Show("Login successful!");

                    if (username == "Admin123")
                    {
                        // Navigate to Admin Page
                        _mainFrame.Navigate(new Admin_Page(_mainFrame)); //Open Admin Page
                    }
                    else
                    {
                        // Navigate to Library Page
                        _mainFrame.Navigate(new Library_Page(_mainFrame, username)); //Open Library Page
                    }

                    return;
                }
            }

            // If no match found
            MessageBox.Show("Incorrect username or password.");
        }

        private void SignupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainFrame.Navigate(new SignUp_Page(_mainFrame));
        }
    }
}
