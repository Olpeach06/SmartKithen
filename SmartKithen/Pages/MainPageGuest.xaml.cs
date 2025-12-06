using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class MainPageGuest : Page
    {
        public MainPageGuest()
        {
            InitializeComponent();
            Loaded += MainPageGuest_Loaded;

            // Добавляем обработчики для рецептов (если они есть в XAML)
            AddEventHandlers();
        }

        private void AddEventHandlers()
        {
            // Добавляем обработчики кликов по рецептам
            // Если у вас есть Border'ы с рецептами, добавьте им MouseLeftButtonUp="Recipe_Click"
        }

        private void MainPageGuest_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGuestData();
        }

        private void LoadGuestData()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var totalRecipes = context.Recipes.Count();
                    // Можно обновить UI с количеством рецептов

                    var recentRecipes = context.Recipes
                        .OrderByDescending(r => r.Id)
                        .Take(4)
                        .ToList();

                    var recommendedRecipes = context.Recipes
                        .OrderBy(r => Guid.NewGuid())
                        .Take(3)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void btnSaveProgress_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Для сохранения прогресса необходимо зарегистрироваться. Хотите создать аккаунт?",
                "Регистрация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigationService?.Navigate(new Registration());
            }
        }

        private void btnShoppingList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Эта функция доступна только зарегистрированным пользователям.\n" +
                          "Зарегистрируйтесь, чтобы получить доступ ко всем возможностям!",
                          "Доступ ограничен",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        private void btnLowStock_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Эта функция доступна только зарегистрированным пользователям.\n" +
                          "Зарегистрируйтесь, чтобы отслеживать продукты в холодильнике!",
                          "Доступ ограничен",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        private void btnRandomRecipe_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomRecipeDetail();
        }

        private void ShowRegistrationPrompt()
        {
            var result = MessageBox.Show(
                "Хотите зарегистрироваться и получить полный доступ ко всем функциям?",
                "Регистрация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigationService?.Navigate(new Registration());
            }
        }

        private void ShowRandomRecipeDetail()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var randomRecipe = context.Recipes
                        .OrderBy(r => Guid.NewGuid())
                        .FirstOrDefault();

                    if (randomRecipe != null)
                    {
                        var result = MessageBox.Show(
                            $"🎲 Случайный рецепт:\n\n" +
                            $"🍽️ {randomRecipe.Title}\n" +
                            $"⏱️ Время приготовления: {randomRecipe.CookingTime ?? 0} мин.\n\n" +
                            $"Хотите посмотреть подробнее?",
                            "Случайный рецепт",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            // ИСПРАВЛЕНО: Используем правильное название класса GuestReciepe
                            NavigationService?.Navigate(new GuestReciepe(randomRecipe.Id));
                        }
                    }
                    else
                    {
                        MessageBox.Show("Рецепты не найдены", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе рецепта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики кликов по рецептам (если добавите в XAML)
        private void Recipe_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomRecipeDetail();
        }

        private void SearchIcon_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Поиск доступен в полной версии приложения",
                "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        private void SettingsIcon_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Настройки доступны зарегистрированным пользователям",
                "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        private void GuestStatus_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var registerItem = new MenuItem
            {
                Header = "🆕 Зарегистрироваться",
                FontSize = 14
            };
            registerItem.Click += (s, args) => NavigationService?.Navigate(new Registration());

            var loginItem = new MenuItem
            {
                Header = "🔑 Войти",
                FontSize = 14
            };
            loginItem.Click += (s, args) => NavigationService?.Navigate(new Authorization());

            var exitItem = new MenuItem
            {
                Header = "🚪 Выйти",
                FontSize = 14
            };
            exitItem.Click += (s, args) => ExitGuestMode();

            menu.Items.Add(registerItem);
            menu.Items.Add(loginItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(exitItem);

            if (sender is FrameworkElement element)
            {
                menu.PlacementTarget = element;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        private void ExitGuestMode()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из гостевого режима?\nВесь прогресс будет потерян.",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUser = null;
                NavigationService?.Navigate(new HomePage());
            }
        }
    }
}