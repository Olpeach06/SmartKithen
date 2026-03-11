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
    /// Логика взаимодействия для ListProducts.xaml
    /// </summary>
    public partial class ListProducts : Page
    {
        public ListProducts()
        {






            InitializeComponent();
        }

        private void RecipeCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearCheckedButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
