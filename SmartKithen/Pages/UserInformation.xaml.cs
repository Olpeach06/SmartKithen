using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            // Проверяем, авторизован ли пользователь
            if (SessionManager.CurrentUserId == 0)
            {
                MessageBox.Show("Пожалуйста, войдите в аккаунт.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.Navigate(new Authorization());
                return;
            }
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
                    if (App.CurrentUser != null)
                    {
                        App.CurrentUser.Name = name;
                        App.CurrentUser.Login = login;
                    }
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

                    // Удаляем избранные рецепты, если таблица есть в контексте
                    if (context.FavoriteRecipes != null)
                    {
                        var favorites = context.FavoriteRecipes.Where(f => f.UserId == id);
                        if (favorites.Any())
                            context.FavoriteRecipes.RemoveRange(favorites);
                    }

                    // Удаляем историю просмотров, если таблица есть в контексте
                    if (context.RecipeHistory != null)
                    {
                        var history = context.RecipeHistory.Where(r => r.UserId == id);
                        if (history.Any())
                            context.RecipeHistory.RemoveRange(history);
                    }

                    var user = context.Users.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                        context.Users.Remove(user);

                    context.SaveChanges();
                }

                // Очищаем сессию
                App.CurrentUser = null;
                SessionManager.ClearGuestData();

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

        // Резервная копия данных пользователя
        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var recipes = context.Recipes
                        .Where(r => r.UserId == SessionManager.CurrentUserId)
                        .ToList();

                    if (recipes.Count == 0)
                    {
                        MessageBox.Show("У вас нет рецептов для резервного копирования.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Получаем папку документов пользователя
                    string documentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "SmartKitchen");

                    if (!Directory.Exists(documentsPath))
                        Directory.CreateDirectory(documentsPath);

                    // Создаём текстовый файл с данными рецептов
                    var backupContent = new StringBuilder();
                    backupContent.AppendLine("=== РЕЗЕРВНАЯ КОПИЯ РЕЦЕПТОВ SmartKitchen ===");
                    backupContent.AppendLine($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                    backupContent.AppendLine($"Пользователь: {App.CurrentUser?.Name ?? "Unknown"}");
                    backupContent.AppendLine($"Количество рецептов: {recipes.Count}");
                    backupContent.AppendLine(new string('=', 50));
                    backupContent.AppendLine();

                    foreach (var recipe in recipes)
                    {
                        backupContent.AppendLine($"РЕЦЕПТ: {recipe.Title}");
                        backupContent.AppendLine($"ID: {recipe.Id}");
                        backupContent.AppendLine($"Описание: {recipe.Description ?? "—"}");
                        backupContent.AppendLine($"Инструкции: {recipe.Instructions ?? "—"}");
                        backupContent.AppendLine($"Время приготовления: {(recipe.CookingTime.HasValue ? recipe.CookingTime + " мин" : "—")}");

                        // Добавляем ингредиенты
                        var ingredients = context.Ingredients
                            .Where(i => i.RecipeId == recipe.Id)
                            .ToList();

                        if (ingredients.Any())
                        {
                            backupContent.AppendLine("Ингредиенты:");
                            foreach (var ingredient in ingredients)
                            {
                                var product = ingredient.Products;
                                backupContent.AppendLine($"  - {product?.Name ?? "Unknown"}: {ingredient.Quantity} {ingredient.Unit}");
                            }
                        }

                        backupContent.AppendLine(new string('-', 40));
                        backupContent.AppendLine();
                    }

                    // Сохраняем в текстовый файл
                    string fileName = $"SmartKitchen_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                    string filePath = Path.Combine(documentsPath, fileName);

                    File.WriteAllText(filePath, backupContent.ToString(), Encoding.UTF8);

                    MessageBox.Show(
                        $"Резервная копия успешно создана!\n\n" +
                        $"Файл: {fileName}\n" +
                        $"Путь: {documentsPath}\n\n" +
                        $"Сохранено рецептов: {recipes.Count}",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Открываем папку в проводнике
                    System.Diagnostics.Process.Start("explorer.exe", documentsPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании резервной копии:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Экспорт рецептов в CSV
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var recipes = context.Recipes
                        .Where(r => r.UserId == SessionManager.CurrentUserId)
                        .ToList();

                    if (recipes.Count == 0)
                    {
                        MessageBox.Show("У вас нет рецептов для экспорта.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Получаем папку документов пользователя
                    string documentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "SmartKitchen");

                    if (!Directory.Exists(documentsPath))
                        Directory.CreateDirectory(documentsPath);

                    // Создаём CSV файл со сводкой рецептов
                    var csvContent = new StringBuilder();
                    csvContent.AppendLine("ID,Название,Описание,Время приготовления (мин),Ингредиентов");

                    foreach (var recipe in recipes)
                    {
                        string title = EscapeCsv(recipe.Title ?? "");
                        string description = EscapeCsv((recipe.Description ?? "").Length > 100 
                            ? (recipe.Description ?? "").Substring(0, 100) + "..." 
                            : recipe.Description ?? "");
                        string cookingTime = recipe.CookingTime?.ToString() ?? "—";

                        // Считаем ингредиенты
                        var ingredientCount = context.Ingredients
                            .Where(i => i.RecipeId == recipe.Id)
                            .Count();

                        csvContent.AppendLine(
                            $"{recipe.Id},\"{title}\",\"{description}\",{cookingTime},{ingredientCount}");
                    }

                    // Сохраняем CSV
                    string fileName = $"SmartKitchen_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                    string filePath = Path.Combine(documentsPath, fileName);

                    File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);

                    // Также создаём подробный файл со всеми рецептами и ингредиентами
                    ExportDetailedRecipes(documentsPath, recipes, context);

                    MessageBox.Show(
                        $"Экспорт успешно завершен!\n\n" +
                        $"Основной файл: {fileName}\n" +
                        $"Путь: {documentsPath}\n\n" +
                        $"Экспортировано рецептов: {recipes.Count}",
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Открываем папку в проводнике
                    System.Diagnostics.Process.Start("explorer.exe", documentsPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Экспорт подробной информации о рецептах
        private void ExportDetailedRecipes(string folderPath, List<Recipes> recipes, SmartKitchenEntities context)
        {
            try
            {
                var detailedCsv = new StringBuilder();
                detailedCsv.AppendLine("Название рецепта,Ингредиент,Количество,Единица измерения");

                foreach (var recipe in recipes)
                {
                    var ingredients = context.Ingredients
                        .Where(i => i.RecipeId == recipe.Id)
                        .ToList();

                    if (ingredients.Any())
                    {
                        foreach (var ingredient in ingredients)
                        {
                            string recipeName = EscapeCsv(recipe.Title ?? "");
                            string productName = EscapeCsv(ingredient.Products?.Name ?? "Unknown");
                            string quantity = ingredient.Quantity.ToString();
                            string unit = EscapeCsv(ingredient.Unit ?? "шт");

                            detailedCsv.AppendLine(
                                $"\"{recipeName}\",\"{productName}\",{quantity},\"{unit}\"");
                        }
                    }
                    else
                    {
                        // Если нет ингредиентов, всё равно добавляем строку с рецептом
                        string recipeName = EscapeCsv(recipe.Title ?? "");
                        detailedCsv.AppendLine($"\"{recipeName}\",—,—,—");
                    }
                }

                string detailedFileName = $"SmartKitchen_Recipes_Detailed_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                string detailedPath = Path.Combine(folderPath, detailedFileName);
                File.WriteAllText(detailedPath, detailedCsv.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Если ошибка при создании подробного файла, игнорируем
            }
        }

        // Вспомогательный метод для экранирования CSV
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Если строка содержит кавычки, запятые или переводы строк, требуется экранирование
            if (value.Contains("\"") || value.Contains(",") || value.Contains("\n"))
            {
                return value.Replace("\"", "\"\"");
            }
            return value;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}