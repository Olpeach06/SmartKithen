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
        }

        private void MainPageGuest_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGuestData();
            LoadRecommendedRecipes();
        }

        // Загрузка данных для гостя
        private void LoadGuestData()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Загружаем общее количество рецептов
                    var totalRecipes = context.Recipes.Count();

                    // Обновляем счетчик рецептов на кнопке
                    if (txtRecipeCount != null)
                    {
                        txtRecipeCount.Text = totalRecipes.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                if (txtRecipeCount != null)
                {
                    txtRecipeCount.Text = "0";
                }
            }
        }

        // Загрузка случайных рекомендуемых рецептов
        private void LoadRecommendedRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var recommendedRecipes = context.Recipes
                        .OrderBy(r => Guid.NewGuid())
                        .Take(3)
                        .ToList();

                    // Очищаем Grid с рекомендациями
                    if (RecommendedRecipesGrid != null)
                    {
                        RecommendedRecipesGrid.Children.Clear();

                        // Добавляем каждый рецепт как кнопку
                        foreach (var recipe in recommendedRecipes)
                        {
                            var border = new Border
                            {
                                Background = System.Windows.Media.Brushes.White,
                                CornerRadius = new CornerRadius(10),
                                Padding = new Thickness(15, 12, 15, 12),
                                Margin = new Thickness(5),
                                BorderBrush = System.Windows.Media.Brushes.LightGray,
                                BorderThickness = new Thickness(1),
                                Cursor = System.Windows.Input.Cursors.Hand,
                                Tag = recipe.Id
                            };

                            border.MouseLeftButtonUp += RecommendedRecipe_Click;

                            var textBlock = new TextBlock
                            {
                                Text = recipe.Title,
                                FontSize = 14,
                                Foreground = System.Windows.Media.Brushes.Green,
                                FontWeight = FontWeights.Medium,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = TextAlignment.Center
                            };

                            border.Child = textBlock;
                            RecommendedRecipesGrid.Children.Add(border);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки рекомендаций: {ex.Message}");
            }
        }

        // Кнопка "Сохранить прогресс"
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

        // Кнопка "Список покупок"
        private void btnShoppingList_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new GuestProduct());
        }

        // Кнопка "Случайный рецепт"
        private void btnRandomRecipe_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomRecipeDetail();
        }

        // Показать предложение регистрации
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

        // Показать детали случайного рецепта
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
                        // ИСПРАВЛЕНО: Переход на RecipeDetails вместо MessageBox
                        NavigationService?.Navigate(new RecipeDetails(randomRecipe.Id));
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

        // Клик по рекомендуемому рецепту
        private void RecommendedRecipe_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int recipeId)
            {
                // ИСПРАВЛЕНО: Переход на RecipeDetails
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            }
        }

        // Кнопка поиска
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        // Кнопка настроек
        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new GuestMode());
        }

        // Кнопка "Рецепты" (переход к списку всех рецептов)
        private void btnRecipes_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new GuestReciepe());
        }

        // Кнопка "Добавить рецепт"
        private void btnAddRecipe_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CreatingGuestReciepe());
        }

        // Кнопка "Выйти из гостевого режима"
        private void btnExitGuest_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из гостевого режима?\nВесь прогресс будет потерян.",
                "Выход из гостевого режима",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUser = null;
                NavigationService?.Navigate(new HomePage());
            }
        }

        // Клик по статусу "Гость" (контекстное меню)
        private void GuestStatus_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
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
                exitItem.Click += (s, args) => btnExitGuest_Click(s, args);

                menu.Items.Add(registerItem);
                menu.Items.Add(loginItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(exitItem);

                menu.PlacementTarget = element;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        // Дополнительный метод для обновления данных (если нужно)
        public void RefreshData()
        {
            LoadGuestData();
            LoadRecommendedRecipes();
        }
    }
}