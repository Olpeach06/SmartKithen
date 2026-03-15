using SmartKithen.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class UserInformation : Page
    {
        // Флаг — мышь внутри popup, чтобы не закрывать раньше времени
        private bool _mouseInPopup = false;

        public UserInformation()
        {
            InitializeComponent();
            Loaded += UserInformation_Loaded;
        }

        private void UserInformation_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
        }

        private void LoadUserData()
        {
            using (var context = new SmartKitchenEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == SessionManager.CurrentUserId);
                if (user == null) return;

                tbName.Text = user.Name;
                tbLogin.Text = user.Login;
                UserNameDisplay.Text = user.Name.Split(' ')[0];
                UserLoginDisplay.Text = $"@{user.Login}";
            }
        }

        // Переключение вкладок
        private void TabProfile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProfileTab.Visibility = Visibility.Visible;
            PasswordTab.Visibility = Visibility.Collapsed;
            SaveButtonBorder.Visibility = Visibility.Visible;

            TabProfileBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A5D34"));
            var profileText = TabProfileBorder.Child as TextBlock;
            if (profileText != null) profileText.Foreground = Brushes.White;

            TabPasswordBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#E8F5EE"));
            var passText = TabPasswordBorder.Child as TextBlock;
            if (passText != null) passText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A5D34"));
        }

        private void TabPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProfileTab.Visibility = Visibility.Collapsed;
            PasswordTab.Visibility = Visibility.Visible;
            SaveButtonBorder.Visibility = Visibility.Collapsed;

            TabPasswordBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A5D34"));
            var passText = TabPasswordBorder.Child as TextBlock;
            if (passText != null) passText.Foreground = Brushes.White;

            TabProfileBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#E8F5EE"));
            var profileText = TabProfileBorder.Child as TextBlock;
            if (profileText != null) profileText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A5D34"));
        }

        // Сохранение профиля
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var name = tbName.Text.Trim();
            var login = tbLogin.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Имя и логин не могут быть пустыми.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Проверяем, не занят ли логин другим пользователем
                    var loginTaken = context.Users.Any(
                        u => u.Login == login && u.Id != SessionManager.CurrentUserId);

                    if (loginTaken)
                    {
                        MessageBox.Show("Этот логин уже занят. Выберите другой.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var user = context.Users.FirstOrDefault(u => u.Id == SessionManager.CurrentUserId);
                    if (user == null) return;

                    user.Name = name;
                    user.Login = login;
                    context.SaveChanges();

                    // Обновляем отображение
                    UserNameDisplay.Text = name.Split(' ')[0];
                    UserLoginDisplay.Text = $"@{login}";

                    // Обновляем App.CurrentUser
                    App.CurrentUser.Name = name;
                    App.CurrentUser.Login = login;
                }

                MessageBox.Show("Данные успешно сохранены!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранение нового пароля
        private void btnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            var current = pbCurrentPassword.Password;
            var newPass = pbNewPassword.Password;
            var confirm = pbConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(current))
            {
                MessageBox.Show("Введите текущий пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("Введите новый пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != confirm)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                pbConfirmPassword.Clear();
                return;
            }

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var user = context.Users.FirstOrDefault(u => u.Id == SessionManager.CurrentUserId);
                    if (user == null) return;

                    // Проверяем текущий пароль
                    if (user.PasswordHash != current)
                    {
                        MessageBox.Show("Текущий пароль введён неверно.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        pbCurrentPassword.Clear();
                        return;
                    }

                    user.PasswordHash = newPass;
                    context.SaveChanges();
                }

                // Очищаем поля
                pbCurrentPassword.Clear();
                pbNewPassword.Clear();
                pbConfirmPassword.Clear();

                MessageBox.Show("Пароль успешно изменён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка смены пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Popup при наведении на имя
        private void UserNameBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            LogoutPopup.Visibility = Visibility.Visible;
        }

        private void UserNameBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Небольшая задержка через диспетчер, чтобы мышь успела попасть в popup
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_mouseInPopup)
                    LogoutPopup.Visibility = Visibility.Collapsed;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LogoutPopup_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _mouseInPopup = true;
        }

        private void LogoutPopup_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _mouseInPopup = false;
            LogoutPopup.Visibility = Visibility.Collapsed;
        }

        // Выход из аккаунта
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            App.CurrentUser = null;
            NavigationService?.Navigate(new Authorization());
        }

        // Удаление аккаунта
        private void btnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var first = MessageBox.Show(
                "Вы уверены, что хотите удалить аккаунт?\nВсе ваши данные будут удалены безвозвратно.",
                "Удаление аккаунта",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (first != MessageBoxResult.Yes) return;

            var second = MessageBox.Show(
                "Это действие необратимо. Продолжить?",
                "Последнее предупреждение",
                MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (second != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    int id = SessionManager.CurrentUserId;

                    // Удаляем все связанные данные пользователя
                    var fridgeItems = context.FridgeItems.Where(f => f.UserId == id);
                    context.FridgeItems.RemoveRange(fridgeItems);

                    var menuPlans = context.MenuPlans.Where(m => m.UserId == id);
                    context.MenuPlans.RemoveRange(menuPlans);

                    
                    //Если добавили FavoriteRecipes и RecipeHistory через EF — удаляем и их
                    //var favorites = context.FavoriteRecipes.Where(f => f.UserId == id);
                    //context.FavoriteRecipes.RemoveRange(favorites);
                    //var history = context.RecipeHistory.Where(r => r.UserId == id);
                    //context.RecipeHistory.RemoveRange(history);

                    var user = context.Users.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                        context.Users.Remove(user);

                    context.SaveChanges();
                }

                App.CurrentUser = null;
                MessageBox.Show("Аккаунт удалён.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.Navigate(new Authorization());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Заглушки для кнопок данных
        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция резервного копирования в разработке.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция экспорта в разработке.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}