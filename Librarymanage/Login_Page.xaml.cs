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
    /// Interaction logic for Login_Page.xaml
    /// </summary>
    public partial class Login_Page : Page
    {
        private Frame _mainFrame;

        public Login_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Add login logic here
        }

        private void SignupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainFrame.Navigate(new SignUp_Page(_mainFrame));
        }
    }
}
