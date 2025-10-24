using SmartKithen.AppData;
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
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string firstName = tbFirstName.Text.Trim();
                string lastName = tbLastName.Text.Trim();
                string email = tbEmail.Text.Trim();
                string password = pbPassword.Password.Trim();
                string confirmPassword = pbConfirmPassword.Password.Trim();

                // Проверка заполненности
                if (string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(confirmPassword))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка паролей
                if (password != confirmPassword)
                {
                    MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка, существует ли уже пользователь с таким email
                var existingUser = AppConnect.model01.Users.FirstOrDefault(u => u.Login == email);
                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким email уже зарегистрирован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Объединяем имя и фамилию в одно поле
                string fullName = $"{firstName} {lastName}";

                // Создаём нового пользователя
                var newUser = new Users
                {
                    Name = fullName,
                    Login = email,
                    PasswordHash = password
                };

                // Добавляем и сохраняем
                AppConnect.model01.Users.Add(newUser);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация успешно завершена! 🎉", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на страницу авторизации
                NavigationService.Navigate(new Authorization());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Authorization());
        }

        private void btnShowPass_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция показа пароля будет добавлена позже 😉");
        }

        private void btnShowConfirmPass_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция показа подтверждения пароля будет добавлена позже 😉");
        }
    }
}