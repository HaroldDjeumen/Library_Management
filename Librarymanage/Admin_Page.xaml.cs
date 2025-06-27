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
    /// Interaction logic for Admin_Page.xaml
    /// </summary>
    public partial class Admin_Page : Page
    {
        private Frame _mainFrame;
        public Admin_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }
    }
}
