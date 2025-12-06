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
    public partial class Authorization : Page
    {
        private bool _isPasswordVisible = false;

        public Authorization()
        {
            InitializeComponent();
            Loaded += Authorization_Loaded;

            // Обработчики нажатия клавиш
            tbEmail.KeyDown += TbEmail_KeyDown;
            pbPassword.KeyDown += PbPassword_KeyDown;
            tbVisiblePassword.KeyDown += TbVisiblePassword_KeyDown;

            // Обработчики изменения текста
            pbPassword.PasswordChanged += PbPassword_PasswordChanged;
            tbVisiblePassword.TextChanged += TbVisiblePassword_TextChanged;
        }

        private void Authorization_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на поле логина
            tbEmail.Focus();
        }

        // Кнопка "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        // Кнопка "Забыли пароль?"
        private void btnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция восстановления пароля временно недоступна.\nОбратитесь к администратору.",
                "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Кнопка "Показать/скрыть пароль"
        private void btnShowPass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isPasswordVisible = !_isPasswordVisible;

                if (_isPasswordVisible)
                {
                    // Показываем пароль
                    passwordBorder.Visibility = Visibility.Collapsed;
                    textPasswordBorder.Visibility = Visibility.Visible;
                    tbVisiblePassword.Text = pbPassword.Password;
                    btnShowPassword.Content = "🙈";
                    tbVisiblePassword.Focus();
                    tbVisiblePassword.CaretIndex = tbVisiblePassword.Text.Length;
                }
                else
                {
                    // Скрываем пароль
                    passwordBorder.Visibility = Visibility.Visible;
                    textPasswordBorder.Visibility = Visibility.Collapsed;
                    pbPassword.Password = tbVisiblePassword.Text;
                    btnShowPassword.Content = "👁️";
                    pbPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // При изменении пароля в PasswordBox обновляем TextBox если он видимый
        private void PbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                tbVisiblePassword.Text = pbPassword.Password;
            }
        }

        // При изменении текста в TextBox обновляем PasswordBox
        private void TbVisiblePassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                pbPassword.Password = tbVisiblePassword.Text;
            }
        }

        // Кнопка "ВОЙТИ"
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем заполнение полей
                if (string.IsNullOrWhiteSpace(tbEmail.Text))
                {
                    MessageBox.Show("Введите логин", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    tbEmail.Focus();
                    return;
                }

                // Получаем пароль в зависимости от того, какое поле видимо
                string password = _isPasswordVisible ? tbVisiblePassword.Text : pbPassword.Password;

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (_isPasswordVisible)
                        tbVisiblePassword.Focus();
                    else
                        pbPassword.Focus();
                    return;
                }

                // Аутентификация пользователя
                Users authenticatedUser = AuthenticateUser(tbEmail.Text, password);

                if (authenticatedUser != null)
                {
                    // Сохраняем текущего пользователя
                    App.CurrentUser = authenticatedUser;

                    // Переходим на главную страницу пользователя
                    NavigationService?.Navigate(new MainPageUser());

                    // Показываем приветствие
                    MessageBox.Show($"Добро пожаловать, {authenticatedUser.Name}!",
                        "Авторизация успешна", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    pbPassword.Password = "";
                    tbVisiblePassword.Text = "";
                    tbEmail.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод аутентификации пользователя
        private Users AuthenticateUser(string login, string password)
        {
            using (var context = new SmartKitchenEntities())
            {
                // Ищем пользователя по логину
                var user = context.Users.FirstOrDefault(u => u.Login == login);

                if (user != null)
                {
                    // ВНИМАНИЕ: В реальном проекте используйте хэширование паролей!
                    // Здесь простое сравнение для демонстрации
                    if (user.PasswordHash == password)
                    {
                        return user;
                    }
                }

                return null;
            }
        }

        // Кнопка "Продолжить как гость"
        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Устанавливаем пользователя как гостя
                App.CurrentUser = new Users
                {
                    Id = 0,
                    Login = "guest",
                    Name = "Гость",
                    PasswordHash = ""
                };

                // Переходим на главную страницу гостя
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

        // Кнопка "Зарегистрироваться"
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Registration());
        }

        // Обработка нажатия Enter в поле логина
        private void TbEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isPasswordVisible)
                    tbVisiblePassword.Focus();
                else
                    pbPassword.Focus();
            }
        }

        // Обработка нажатия Enter в поле пароля (PasswordBox)
        private void PbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }

        // Обработка нажатия Enter в видимом поле пароля (TextBox)
        private void TbVisiblePassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }
    }
}