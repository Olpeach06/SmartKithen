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

namespace SmartKithen.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReciepeDetails.xaml
    /// </summary>
    public partial class ReciepeDetails : Page
    {
        public ReciepeDetails()
        {
            InitializeComponent();
        }

        private void StartCookingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CookingMode());
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void AddToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
