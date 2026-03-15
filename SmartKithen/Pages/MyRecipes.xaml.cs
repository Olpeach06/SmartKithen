using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SmartKithen.Pages
{
    public partial class MyRecipes : Page
    {
        // Сколько рецептов грузим за раз
        private const int PageSize = 6;
        private int _currentPage = 0;

        // Полный список после фильтрации — храним чтобы не дёргать БД лишний раз
        private List<Recipes> _filteredRecipes = new List<Recipes>();

        private bool _searchPlaceholder = true;
        private bool _sortAscending = true;

        public MyRecipes()
        {
            InitializeComponent();
            Loaded += MyRecipes_Loaded;
        }

        private void MyRecipes_Loaded(object sender, RoutedEventArgs e)
        {
            HeaderUserName.Text = SessionManager.CurrentUserName.Split(' ')[0];
            LoadCategories();
            LoadRecipes();
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var categories = context.Categories
                        .OrderBy(c => c.Name)
                        .ToList();

                    CategoryFilter.Items.Clear();
                    CategoryFilter.Items.Add(new ComboBoxItem
                    {
                        Content = "Все категории",
                        Tag = 0
                    });

                    foreach (var cat in categories)
                    {
                        CategoryFilter.Items.Add(new ComboBoxItem
                        {
                            Content = cat.Name,
                            Tag = cat.Id
                        });
                    }

                    CategoryFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void LoadRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var query = context.Recipes
                        .Include("Categories")
                        .AsQueryable();

                    // Фильтр по категории
                    var selectedCategory = CategoryFilter.SelectedItem as ComboBoxItem;
                    if (selectedCategory != null && (int)selectedCategory.Tag != 0)
                    {
                        var categoryId = (int)selectedCategory.Tag;
                        query = query.Where(r => r.CategoryId == categoryId);
                    }

                    // Фильтр по поиску
                    var searchText = _searchPlaceholder ? "" : SearchBox.Text.Trim();
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query = query.Where(r => r.Title.Contains(searchText));
                    }

                    // Сортировка
                    query = _sortAscending
                        ? query.OrderBy(r => r.Title)
                        : query.OrderByDescending(r => r.Title);

                    _filteredRecipes = query.ToList();
                }

                _currentPage = 0;
                RecipesPanel.Children.Clear();
                RenderPage();

                RecipesCountText.Text = $"({_filteredRecipes.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецептов: {ex.Message}");
            }
        }

        private void RenderPage()
        {
            var toShow = _filteredRecipes
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .ToList();

            if (_currentPage == 0 && toShow.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                LoadMoreButton.Visibility = Visibility.Collapsed;
                EmptyStateText.Text = string.IsNullOrEmpty(SearchBox.Text) || _searchPlaceholder
                    ? "Рецептов пока нет"
                    : $"Ничего не найдено по запросу «{SearchBox.Text}»";
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;

            foreach (var recipe in toShow)
            {
                RecipesPanel.Children.Add(CreateRecipeCard(recipe));
            }

            // Показываем кнопку "Загрузить ещё" если есть ещё рецепты
            var totalShown = (_currentPage + 1) * PageSize;
            LoadMoreButton.Visibility = totalShown < _filteredRecipes.Count
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private Border CreateRecipeCard(Recipes recipe)
        {
            var card = new Border
            {
                Width = 220,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 16, 16),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            card.Effect = new DropShadowEffect
            {
                BlurRadius = 15,
                Opacity = 0.08,
                ShadowDepth = 5
            };

            var content = new StackPanel { Orientation = Orientation.Vertical };

            // Иконка времени в правом углу
            var topRow = new Grid();
            topRow.Margin = new Thickness(0, 0, 0, 10);

            var timeBadge = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Right,
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                BorderThickness = new Thickness(1)
            };
            timeBadge.Child = new TextBlock
            {
                Text = "⏱️",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            topRow.Children.Add(timeBadge);
            content.Children.Add(topRow);

            // Категория
            content.Children.Add(new TextBlock
            {
                Text = recipe.Categories?.Name ?? "Без категории",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Название
            content.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Время и рейтинг
            var infoRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var timeText = recipe.CookingTime.HasValue
                ? $"{recipe.CookingTime.Value} мин"
                : "— мин";

            infoRow.Children.Add(new TextBlock
            {
                Text = timeText,
                FontSize = 12,
                Foreground = Brushes.Gray
            });

            infoRow.Children.Add(new TextBlock
            {
                Text = " ★★★★",
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFA500")),
                Margin = new Thickness(10, 0, 0, 0)
            });

            content.Children.Add(infoRow);

            // Плашка автора
            var authorBadge = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                BorderThickness = new Thickness(1)
            };
            authorBadge.Child = new TextBlock
            {
                Text = $"⌛ {SessionManager.CurrentUserName.Split(' ')[0]}",
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            content.Children.Add(authorBadge);

            card.Child = content;

            // Клик — открываем рецепт
            int recipeId = recipe.Id;
            card.MouseLeftButtonDown += (s, e) =>
            {
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            };

            return card;
        }

        // Поиск
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_searchPlaceholder)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#333"));
                _searchPlaceholder = false;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Поиск...";
                SearchBox.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#999"));
                _searchPlaceholder = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_searchPlaceholder)
                LoadRecipes();
        }

        // Фильтр по категории
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем что страница уже загружена
            if (RecipesPanel != null)
                LoadRecipes();
        }

        // Сортировка
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = !_sortAscending;
            SortButton.Content = _sortAscending ? "Сортировка ▼" : "Сортировка ▲";
            LoadRecipes();
        }

        // Загрузить ещё
        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            RenderPage();
        }

        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CreatingReciepe());
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}