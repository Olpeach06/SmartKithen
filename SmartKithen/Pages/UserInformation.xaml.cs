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
using SmartKithen;

namespace SmartKithen.Pages
{
    /// <summary>
    /// Логика взаимодействия для UserInformation.xaml
    /// </summary>
    public partial class UserInformation : Page
    {
        public UserInformation()
        {
            InitializeComponent();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnThemeToggle_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btnThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
