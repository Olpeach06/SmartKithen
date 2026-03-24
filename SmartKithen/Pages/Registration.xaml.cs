using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class Registration : Page
    {
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;
        private bool _fromGuestMode = false; // Флаг для режима гостя

        public Registration()
        {
            InitializeComponent();
            Loaded += Registration_Loaded;

            // Обработчики нажатия клавиш
            tbFirstName.KeyDown += TbFirstName_KeyDown;
            tbLastName.KeyDown += TbLastName_KeyDown;
            tbEmail.KeyDown += TbEmail_KeyDown;
            pbPassword.KeyDown += PbPassword_KeyDown;
            pbConfirmPassword.KeyDown += PbConfirmPassword_KeyDown;
            tbVisiblePassword.KeyDown += TbVisiblePassword_KeyDown;
            tbVisibleConfirmPassword.KeyDown += TbVisibleConfirmPassword_KeyDown;

            // Обработчики изменения текста
            pbPassword.PasswordChanged += PbPassword_PasswordChanged;
            tbVisiblePassword.TextChanged += TbVisiblePassword_TextChanged;
            pbConfirmPassword.PasswordChanged += PbConfirmPassword_PasswordChanged;
            tbVisibleConfirmPassword.TextChanged += TbVisibleConfirmPassword_TextChanged;
        }

        // Конструктор с параметром для режима гостя
        public Registration(bool fromGuestMode) : this()
        {
            _fromGuestMode = fromGuestMode;
        }

        private void Registration_Loaded(object sender, RoutedEventArgs e)
        {
            tbFirstName.Focus();

            // Если регистрация из гостевого режима, показываем уведомление
            if (_fromGuestMode && SessionManager.HasGuestData())
            {
                ShowGuestDataNotification();
            }
        }

        private void ShowGuestDataNotification()
        {
            var summary = SessionManager.GetGuestDataSummary();
            MessageBox.Show(
                $"У вас есть временные данные в гостевом режиме:\n{summary}\n\n" +
                "После регистрации они будут автоматически перенесены в ваш аккаунт.",
                "Перенос данных",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void btnShowPass_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                passwordBorder.Visibility = Visibility.Collapsed;
                textPasswordBorder.Visibility = Visibility.Visible;
                tbVisiblePassword.Text = pbPassword.Password;
                btnShowPassword.Content = "🙈";
                tbVisiblePassword.Focus();
                tbVisiblePassword.CaretIndex = tbVisiblePassword.Text.Length;
            }
            else
            {
                passwordBorder.Visibility = Visibility.Visible;
                textPasswordBorder.Visibility = Visibility.Collapsed;
                pbPassword.Password = tbVisiblePassword.Text;
                btnShowPassword.Content = "👁️";
                pbPassword.Focus();
            }
        }

        private void btnShowConfirmPass_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

            if (_isConfirmPasswordVisible)
            {
                confirmPasswordBorder.Visibility = Visibility.Collapsed;
                textConfirmPasswordBorder.Visibility = Visibility.Visible;
                tbVisibleConfirmPassword.Text = pbConfirmPassword.Password;
                btnShowConfirmPassword.Content = "🙈";
                tbVisibleConfirmPassword.Focus();
                tbVisibleConfirmPassword.CaretIndex = tbVisibleConfirmPassword.Text.Length;
            }
            else
            {
                confirmPasswordBorder.Visibility = Visibility.Visible;
                textConfirmPasswordBorder.Visibility = Visibility.Collapsed;
                pbConfirmPassword.Password = tbVisibleConfirmPassword.Text;
                btnShowConfirmPassword.Content = "👁️";
                pbConfirmPassword.Focus();
            }
        }

        private void PbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                tbVisiblePassword.Text = pbPassword.Password;
            }
        }

        private void TbVisiblePassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                pbPassword.Password = tbVisiblePassword.Text;
            }
        }

        private void PbConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isConfirmPasswordVisible)
            {
                tbVisibleConfirmPassword.Text = pbConfirmPassword.Password;
            }
        }

        private void TbVisibleConfirmPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPasswordVisible)
            {
                pbConfirmPassword.Password = tbVisibleConfirmPassword.Text;
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateRegistration())
                    return;

                string login = tbEmail.Text.Trim();
                string password = GetPassword();

                if (!IsLoginUnique(login))
                {
                    MessageBox.Show("Этот логин уже занят. Пожалуйста, выберите другой.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    tbEmail.Focus();
                    tbEmail.SelectAll();
                    return;
                }

                // Создание нового пользователя
                string fullName = $"{tbFirstName.Text.Trim()} {tbLastName.Text.Trim()}".Trim();

                Users newUser = new Users
                {
                    Login = login,
                    PasswordHash = password, // TODO: Добавить хэширование пароля
                    Name = fullName
                };

                int newUserId = 0;

                // Сохранение пользователя в БД
                using (var context = new SmartKitchenEntities())
                {
                    context.Users.Add(newUser);
                    int result = context.SaveChanges();

                    if (result > 0)
                    {
                        newUserId = newUser.Id; // Получаем ID созданного пользователя

                        // Если регистрация из гостевого режима и есть данные, переносим их
                        if (_fromGuestMode && SessionManager.HasGuestData())
                        {
                            TransferGuestData(context, newUserId);
                        }

                        // Автоматический вход после регистрации
                        App.CurrentUser = newUser;

                        // Переход на главную страницу пользователя
                        NavigationService?.Navigate(new MainPageUser());

                        string message = _fromGuestMode
                            ? $"Регистрация успешна! Ваши временные данные перенесены в аккаунт."
                            : $"Регистрация успешна! Добро пожаловать, {fullName}!";

                        MessageBox.Show(message, "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при сохранении пользователя. Попробуйте позже.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Получение пароля в зависимости от видимости
        private string GetPassword()
        {
            return _isPasswordVisible ? tbVisiblePassword.Text : pbPassword.Password;
        }

        // Перенос данных гостя в аккаунт пользователя
        private void TransferGuestData(SmartKitchenEntities context, int newUserId)
        {
            try
            {
                int transferredCount = 0;

                // Переносим список покупок из SessionManager.GuestShoppingList
                if (SessionManager.GuestShoppingList != null && SessionManager.GuestShoppingList.Any())
                {
                    foreach (var item in SessionManager.GuestShoppingList)
                    {
                        // Проверяем, нет ли уже такого продукта в холодильнике
                        var existingItem = context.FridgeItems
                            .FirstOrDefault(f => f.UserId == newUserId && f.ProductId == item.ProductId);

                        if (existingItem != null)
                        {
                            // Если есть, увеличиваем количество
                            existingItem.Quantity += item.Quantity;
                        }
                        else
                        {
                            // Если нет, добавляем новый
                            var fridgeItem = new FridgeItems
                            {
                                UserId = newUserId,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                ExpiryDate = DateTime.Today.AddMonths(1)
                            };
                            context.FridgeItems.Add(fridgeItem);
                        }
                        transferredCount++;
                    }
                }

                // Переносим продукты из SessionManager.GuestProducts (если есть)
                if (SessionManager.GuestProducts != null && SessionManager.GuestProducts.Any())
                {
                    foreach (var product in SessionManager.GuestProducts)
                    {
                        var existingItem = context.FridgeItems
                            .FirstOrDefault(f => f.UserId == newUserId && f.ProductId == product.ProductId);

                        if (existingItem != null)
                        {
                            existingItem.Quantity += product.Quantity;
                        }
                        else
                        {
                            var fridgeItem = new FridgeItems
                            {
                                UserId = newUserId,
                                ProductId = product.ProductId,
                                Quantity = product.Quantity,
                                ExpiryDate = product.ExpiryDate ?? DateTime.Today.AddMonths(1)
                            };
                            context.FridgeItems.Add(fridgeItem);
                        }
                        transferredCount++;
                    }
                }

                // Сохраняем изменения
                context.SaveChanges();

                // Очищаем временные данные гостя
                SessionManager.ClearGuestData();

                System.Diagnostics.Debug.WriteLine($"Перенесено {transferredCount} продуктов для пользователя ID: {newUserId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка переноса данных гостя: {ex.Message}");
                // Не прерываем регистрацию, если что-то пошло не так с переносом
                MessageBox.Show("Данные гостя не удалось перенести, но регистрация завершена успешно.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool ValidateRegistration()
        {
            // Проверка имени
            if (string.IsNullOrWhiteSpace(tbFirstName.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbFirstName.Focus();
                return false;
            }

            if (tbFirstName.Text.Length < 2)
            {
                MessageBox.Show("Имя должно содержать минимум 2 символа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbFirstName.Focus();
                tbFirstName.SelectAll();
                return false;
            }

            // Проверка фамилии
            if (string.IsNullOrWhiteSpace(tbLastName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbLastName.Focus();
                return false;
            }

            if (tbLastName.Text.Length < 2)
            {
                MessageBox.Show("Фамилия должна содержать минимум 2 символа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbLastName.Focus();
                tbLastName.SelectAll();
                return false;
            }

            // Проверка логина
            string login = tbEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbEmail.Focus();
                return false;
            }

            if (login.Length < 3)
            {
                MessageBox.Show("Логин должен содержать минимум 3 символа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbEmail.Focus();
                tbEmail.SelectAll();
                return false;
            }

            // Проверка на допустимые символы в логине
            if (!System.Text.RegularExpressions.Regex.IsMatch(login, @"^[a-zA-Z0-9_@.-]+$"))
            {
                MessageBox.Show("Логин может содержать только буквы, цифры и символы _ @ . -",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbEmail.Focus();
                tbEmail.SelectAll();
                return false;
            }

            // Проверка пароля
            string password = GetPassword();

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (_isPasswordVisible)
                    tbVisiblePassword.Focus();
                else
                    pbPassword.Focus();
                return false;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (_isPasswordVisible)
                {
                    tbVisiblePassword.Focus();
                    tbVisiblePassword.SelectAll();
                }
                else
                {
                    pbPassword.Focus();
                }
                return false;
            }

            // Проверка подтверждения пароля
            string confirmPassword = _isConfirmPasswordVisible ? tbVisibleConfirmPassword.Text : pbConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Подтвердите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (_isConfirmPasswordVisible)
                    tbVisibleConfirmPassword.Focus();
                else
                    pbConfirmPassword.Focus();
                return false;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (_isConfirmPasswordVisible)
                {
                    tbVisibleConfirmPassword.Focus();
                    tbVisibleConfirmPassword.SelectAll();
                }
                else
                {
                    pbConfirmPassword.Focus();
                    pbConfirmPassword.Password = "";
                }
                return false;
            }

            return true;
        }

        private bool IsLoginUnique(string login)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    return !context.Users.Any(u => u.Login.ToLower() == login.ToLower());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки логина: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        // Обработчики нажатия Enter
        private void TbFirstName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) tbLastName.Focus();
        }

        private void TbLastName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) tbEmail.Focus();
        }

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

        private void PbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isConfirmPasswordVisible)
                    tbVisibleConfirmPassword.Focus();
                else
                    pbConfirmPassword.Focus();
            }
        }

        private void TbVisiblePassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isConfirmPasswordVisible)
                    tbVisibleConfirmPassword.Focus();
                else
                    pbConfirmPassword.Focus();
            }
        }

        private void PbConfirmPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnRegister_Click(sender, e);
            }
        }

        private void TbVisibleConfirmPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnRegister_Click(sender, e);
            }
        }
    }
}