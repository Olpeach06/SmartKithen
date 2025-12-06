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
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли сохраненная сессия
            if (App.CurrentUser != null)
            {
                if (App.CurrentUser.Id > 0) // Авторизованный пользователь
                {
                    NavigationService?.Navigate(new MainPageUser());
                }
                else // Гость
                {
                    NavigationService?.Navigate(new MainPageGuest());
                }
            }
        }

        // Кнопка "НАЧАТЬ КАК ГОСТЬ"
        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.CurrentUser = new AppData.Users
                {
                    Id = 0,
                    Login = "guest",
                    Name = "Гость",
                    PasswordHash = ""
                };

                NavigationService?.Navigate(new MainPageGuest());

                MessageBox.Show("Вы вошли как гость. Некоторые функции могут быть ограничены.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Войти в аккаунт"
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        // Кнопка "Создать аккаунт"
        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Registration());
        }

        // Кнопка "О приложении"
        private void AboutBt_Click(object sender, RoutedEventArgs e)
        {
            string aboutText =
                "SmartKitchen - Умный холодильник\n" +
                "Версия: 1.0.0\n\n" +
                "Функции:\n" +
                "• Управление холодильником\n" +
                "• Поиск рецептов\n" +
                "• Планирование меню\n" +
                "• Учет пищевой ценности\n\n" +
                "© 2024 SmartKitchen Team";

            MessageBox.Show(aboutText, "О приложении",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Кнопка "Помощь"
        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            string helpText =
                "Справка:\n\n" +
                "Как гость:\n" +
                "• Просмотр рецептов\n" +
                "• Поиск рецептов\n" +
                "• Просмотр продуктов\n\n" +
                "Как пользователь:\n" +
                "• Все функции гостя\n" +
                "• Свой холодильник\n" +
                "• Сохранение рецептов\n" +
                "• Планирование меню";

            MessageBox.Show(helpText, "Помощь",
                MessageBoxButton.OK, MessageBoxImage.Question);
        }
    }
}