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

        private void Registration_Loaded(object sender, RoutedEventArgs e)
        {
            tbFirstName.Focus();
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

                if (!IsLoginUnique(tbEmail.Text))
                {
                    MessageBox.Show("Этот логин уже занят. Пожалуйста, выберите другой.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    tbEmail.Focus();
                    tbEmail.SelectAll();
                    return;
                }

                Users newUser = CreateNewUser();

                if (SaveUserToDatabase(newUser))
                {
                    App.CurrentUser = newUser;

                    // Переносим рецепт из гостевой сессии если есть
                    if (GuestSession.HasPendingRecipe)
                        TransferGuestRecipe(newUser.Id);

                    NavigationService?.Navigate(new MainPageUser());

                    MessageBox.Show($"Регистрация успешна! Добро пожаловать, {newUser.Name}!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении пользователя. Попробуйте позже.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TransferGuestRecipe(int userId)
        {
            try
            {
                var data = GuestSession.PendingRecipe;

                using (var context = new SmartKitchenEntities())
                {
                    var recipe = new Recipes
                    {
                        Title = data.Title,
                        Description = data.Description,
                        CookingTime = data.CookingTime,
                        CategoryId = data.CategoryId,
                        Instructions = ""
                    };

                    context.Recipes.Add(recipe);
                    context.SaveChanges();

                    foreach (var ing in data.Ingredients)
                    {
                        var product = context.Products
                            .FirstOrDefault(p => p.Name.ToLower() == ing.Name.ToLower());

                        if (product == null)
                        {
                            product = new Products
                            {
                                Name = ing.Name,
                                CategoryId = 6,
                                DefaultUnit = ing.Unit
                            };
                            context.Products.Add(product);
                            context.SaveChanges();
                        }

                        context.Ingredients.Add(new Ingredients
                        {
                            RecipeId = recipe.Id,
                            ProductId = product.Id,
                            Quantity = ing.Quantity,
                            Unit = ing.Unit
                        });
                    }

                    for (int i = 0; i < data.Steps.Count; i++)
                    {
                        context.RecipeSteps.Add(new RecipeSteps
                        {
                            RecipeId = recipe.Id,
                            StepNumber = i + 1,
                            Description = data.Steps[i]
                        });
                    }

                    context.SaveChanges();
                }

                GuestSession.Clear();

                MessageBox.Show(
                    $"Рецепт «{data.Title}» из гостевой сессии успешно сохранён в ваш аккаунт!",
                    "Рецепт перенесён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось перенести рецепт: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool ValidateRegistration()
        {
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

            if (string.IsNullOrWhiteSpace(tbEmail.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbEmail.Focus();
                return false;
            }

            if (tbEmail.Text.Length < 3)
            {
                MessageBox.Show("Логин должен содержать минимум 3 символа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbEmail.Focus();
                tbEmail.SelectAll();
                return false;
            }

            string password = _isPasswordVisible ? tbVisiblePassword.Text : pbPassword.Password;
            string confirmPassword = _isConfirmPasswordVisible ? tbVisibleConfirmPassword.Text : pbConfirmPassword.Password;

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
                    return !context.Users.Any(u => u.Login == login);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки логина: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private Users CreateNewUser()
        {
            string password = _isPasswordVisible ? tbVisiblePassword.Text : pbPassword.Password;

            return new Users
            {
                Login = tbEmail.Text.Trim(),
                PasswordHash = password,
                Name = $"{tbFirstName.Text.Trim()} {tbLastName.Text.Trim()}".Trim()
            };
        }

        private bool SaveUserToDatabase(Users newUser)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    context.Users.Add(newUser);
                    int result = context.SaveChanges();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        private void TbFirstName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tbLastName.Focus();
            }
        }

        private void TbLastName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tbEmail.Focus();
            }
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