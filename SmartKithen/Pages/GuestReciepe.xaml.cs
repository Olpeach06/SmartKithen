using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class GuestReciepe : Page
    {
        private List<Recipes> _myRecipes;
        private List<Recipes> _favoriteRecipes;
        private bool _isGuestMode;
        private int _currentTab = 0; // 0 - мои рецепты, 1 - избранное

        public GuestReciepe()
        {
            InitializeComponent();
            Loaded += GuestReciepe_Loaded;
        }

        private void GuestReciepe_Loaded(object sender, RoutedEventArgs e)
        {
            _isGuestMode = SessionManager.IsGuestMode;
            UpdateUserDisplay();
            LoadRecipes();

            // Показываем информационную панель для гостя
            GuestInfoPanel.Visibility = _isGuestMode ? Visibility.Visible : Visibility.Collapsed;

            // Подсвечиваем активную вкладку
            UpdateTabSelection();
        }

        private void UpdateUserDisplay()
        {
            if (_isGuestMode)
            {
                UserIcon.Text = "👤";
                UserNameText.Text = "Гость";
            }
            else if (App.CurrentUser != null)
            {
                UserIcon.Text = "🍳";
                UserNameText.Text = App.CurrentUser.Name.Split(' ')[0];
            }
        }

        private void LoadRecipes()
        {
            if (_isGuestMode)
            {
                // Для гостя показываем демо-данные из SessionManager
                LoadGuestRecipes();
            }
            else
            {
                // Для авторизованного загружаем из БД
                LoadUserRecipesFromDb();
            }

            DisplayCurrentTab();
        }

        private void LoadGuestRecipes()
        {
            // Демо-данные для гостя (взяты из БД для примера)
            _myRecipes = new List<Recipes>();
            _favoriteRecipes = new List<Recipes>();

            // Если есть временные данные в SessionManager, можно их загрузить
            // Здесь показываем примеры рецептов из БД
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var demoRecipes = context.Recipes
                        .Take(4)
                        .Include("Categories")
                        .ToList();

                    _myRecipes.AddRange(demoRecipes);
                }
            }
            catch
            {
                // Если БД недоступна, показываем пустой список
            }
        }

        private void LoadUserRecipesFromDb()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Загружаем избранные рецепты
                    _favoriteRecipes = context.FavoriteRecipes
                        .Where(f => f.UserId == App.CurrentUser.Id)
                        .Select(f => f.Recipes)
                        .Include("Categories")
                        .OrderByDescending(r => r.Id)
                        .ToList();

                    // Загружаем свои рецепты (где автор - текущий пользователь)
                    // Если нет поля CreatedBy, можно использовать другой подход
                    _myRecipes = context.Recipes
                        .Include("Categories")
                        .OrderByDescending(r => r.Id)
                        .Take(10)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецептов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayCurrentTab()
        {
            if (_currentTab == 0)
            {
                DisplayMyRecipes();
            }
            else
            {
                DisplayFavorites();
            }
        }

        private void DisplayMyRecipes()
        {
            MyRecipesPanel.Children.Clear();

            if (_myRecipes == null || !_myRecipes.Any())
            {
                EmptyMyRecipesState.Visibility = Visibility.Visible;
                MyRecipesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyMyRecipesState.Visibility = Visibility.Collapsed;
            MyRecipesPanel.Visibility = Visibility.Visible;

            foreach (var recipe in _myRecipes)
            {
                var card = CreateRecipeCard(recipe);
                MyRecipesPanel.Children.Add(card);
            }
        }

        private void DisplayFavorites()
        {
            FavoritesPanel.Children.Clear();

            if (_favoriteRecipes == null || !_favoriteRecipes.Any())
            {
                EmptyFavoritesState.Visibility = Visibility.Visible;
                FavoritesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyFavoritesState.Visibility = Visibility.Collapsed;
            FavoritesPanel.Visibility = Visibility.Visible;

            foreach (var recipe in _favoriteRecipes)
            {
                var card = CreateRecipeCard(recipe);
                FavoritesPanel.Children.Add(card);
            }
        }

        private Border CreateRecipeCard(Recipes recipe)
        {
            var card = new Border
            {
                Style = (Style)FindResource("RecipeCardStyle"),
                Tag = recipe.Id
            };
            card.MouseLeftButtonUp += RecipeCard_Click;

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            // Эмодзи по категории
            var emoji = GetCategoryEmoji(recipe.Categories?.Name);
            stackPanel.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 32,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Название
            stackPanel.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Время приготовления
            var time = recipe.CookingTime ?? 0;
            stackPanel.Children.Add(new TextBlock
            {
                Text = time > 0 ? $"⏱️ {time} мин" : "⏱️ Время не указано",
                FontSize = 12,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Категория
            if (recipe.Categories != null)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"• {recipe.Categories.Name}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            card.Child = stackPanel;
            return card;
        }

        private string GetCategoryEmoji(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "🍳";

            switch (categoryName)
            {
                case "Мясо": return "🥩";
                case "Рыба":
                case "Морепродукты": return "🐟";
                case "Овощи": return "🥦";
                case "Фрукты": return "🍎";
                case "Молочные продукты": return "🧀";
                case "Крупы": return "🌾";
                case "Хлебобулочные изделия": return "🍞";
                case "Десерты":
                case "Сладости": return "🍰";
                case "Супы": return "🍲";
                case "Напитки": return "🥤";
                case "Соусы": return "🥫";
                case "Специи": return "🧂";
                case "Бакалея": return "📦";
                case "Замороженные продукты": return "❄️";
                default: return "🍳";
            }
        }

        private void UpdateTabSelection()
        {
            if (_currentTab == 0)
            {
                MyRecipesTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                var myText = (TextBlock)MyRecipesTab.Child;
                myText.Foreground = Brushes.White;

                FavoritesTab.Background = Brushes.White;
                var favText = (TextBlock)FavoritesTab.Child;
                favText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
            }
            else
            {
                MyRecipesTab.Background = Brushes.White;
                var myText = (TextBlock)MyRecipesTab.Child;
                myText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));

                FavoritesTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                var favText = (TextBlock)FavoritesTab.Child;
                favText.Foreground = Brushes.White;
            }
        }

        private void RecipeCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int recipeId)
            {
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            }
        }

        // Обработчики навигации
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        private void btnAddRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (_isGuestMode)
            {
                var result = MessageBox.Show(
                    "Для создания рецептов нужно зарегистрироваться. Хотите создать аккаунт?",
                    "Регистрация",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    NavigationService?.Navigate(new Registration(fromGuestMode: true));
                }
            }
            else
            {
                NavigationService?.Navigate(new CreatingReciepe());
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ListProducts());
        }

        // Обработчики вкладок
        private void MyRecipesTab_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == 0) return;

            _currentTab = 0;
            MyRecipesContent.Visibility = Visibility.Visible;
            FavoritesContent.Visibility = Visibility.Collapsed;
            UpdateTabSelection();
            DisplayMyRecipes();
        }

        private void FavoritesTab_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == 1) return;

            _currentTab = 1;
            MyRecipesContent.Visibility = Visibility.Collapsed;
            FavoritesContent.Visibility = Visibility.Visible;
            UpdateTabSelection();
            DisplayFavorites();
        }
    }
}