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
            LoadTotalRecipesCount();
            LoadRecommendedRecipes();
        }

        // Загрузка общего количества рецептов
        private void LoadTotalRecipesCount()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var totalRecipes = context.Recipes.Count();
                    txtRecipeCount.Text = totalRecipes.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки количества рецептов: {ex.Message}");
                txtRecipeCount.Text = "0";
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

                    if (RecommendedRecipesGrid != null)
                    {
                        RecommendedRecipesGrid.Children.Clear();

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
            NavigationService?.Navigate(new ListProducts());
        }

        // Кнопка "Случайный рецепт"
        private void btnRandomRecipe_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomRecipeDetail();
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
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            }
        }

        // Кнопка настроек
        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new GuestMode());
        }

        // Кнопка "Рецепты" - переход на SearchAndFilters
        private void btnRecipes_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
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

        // Обновление данных (если нужно)
        public void RefreshData()
        {
            LoadTotalRecipesCount();
            LoadRecommendedRecipes();
        }
    }
}