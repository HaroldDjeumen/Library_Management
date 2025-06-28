using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private Frame _mainFrame;

        public HomePage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame)); // Go to Login Page
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new SignUp_Page(_mainFrame)); // Go to Signup Page
        }
    }
}
