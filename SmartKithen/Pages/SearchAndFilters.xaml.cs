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
    public partial class SearchAndFilters : Page
    {
        private List<Recipes> _allRecipes = new List<Recipes>();
        private List<MealCategories> _allMealCategories = new List<MealCategories>();

        public SearchAndFilters()
        {
            InitializeComponent();
            Loaded += SearchAndFilters_Loaded;
        }

        private void SearchAndFilters_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllRecipes();
            LoadMealCategories();
            ApplyFilters();
        }

        private void LoadAllRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    _allRecipes = context.Recipes
                        .Include("MealCategories")
                        .OrderBy(r => r.Title)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецептов: {ex.Message}");
            }
        }

        private void LoadMealCategories()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Загружаем только активные категории блюд
                    _allMealCategories = context.MealCategories
                        .Where(mc => mc.IsActive == true)
                        .OrderBy(mc => mc.Name)
                        .ToList();

                    CategoriesPanel.Children.Clear();

                    // "Все категории"
                    var allRadio = new RadioButton
                    {
                        Content = "Все категории",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#1A5D34")),
                        GroupName = "Category",
                        IsChecked = true,
                        Tag = 0,
                        Margin = new Thickness(0, 0, 0, 8),
                        FontWeight = FontWeights.Medium
                    };
                    allRadio.Checked += CategoryRadio_Checked;
                    CategoriesPanel.Children.Add(allRadio);

                    // Категории блюд из БД
                    foreach (var cat in _allMealCategories)
                    {
                        var radio = new RadioButton
                        {
                            Content = cat.Name,
                            FontSize = 13,
                            Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#555")),
                            GroupName = "Category",
                            Tag = cat.Id,
                            Margin = new Thickness(0, 0, 0, 8)
                        };
                        radio.Checked += CategoryRadio_Checked;
                        CategoriesPanel.Children.Add(radio);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий блюд: {ex.Message}");
            }
        }

        private void CategoryRadio_Checked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                var searchText = SearchTextBox.Text.Trim();
                var results = _allRecipes.AsEnumerable();

                // Поиск по названию
                if (!string.IsNullOrEmpty(searchText))
                {
                    results = results.Where(r =>
                        r.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Фильтр по времени
                bool hasTimeFilter = Time30CheckBox.IsChecked == true ||
                                     Time3060CheckBox.IsChecked == true ||
                                     Time60CheckBox.IsChecked == true;

                if (hasTimeFilter)
                {
                    results = results.Where(r =>
                    {
                        var time = r.CookingTime ?? 0;
                        if (Time30CheckBox.IsChecked == true && time > 0 && time <= 30) return true;
                        if (Time3060CheckBox.IsChecked == true && time > 30 && time <= 60) return true;
                        if (Time60CheckBox.IsChecked == true && time > 60) return true;
                        return false;
                    });
                }

                // Фильтр по категории блюда (MealCategoryId)
                var selectedCategoryId = GetSelectedCategoryId();
                if (selectedCategoryId != 0)
                {
                    results = results.Where(r => r.MealCategoryId == selectedCategoryId);
                }

                var finalList = results.ToList();

                // Обновляем заголовок результатов
                if (string.IsNullOrEmpty(searchText))
                {
                    SearchQueryText.Text = "Все рецепты";
                }
                else
                {
                    SearchQueryText.Text = $"Результаты по запросу «{searchText}»";
                }

                ResultsCountText.Text = GetCountLabel(finalList.Count);

                RenderResults(finalList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}");
            }
        }

        private int GetSelectedCategoryId()
        {
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is RadioButton radio && radio.IsChecked == true)
                    return (int)radio.Tag;
            }
            return 0;
        }

        private void RenderResults(List<Recipes> recipes)
        {
            ResultsPanel.Children.Clear();

            if (recipes.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                ResultsPanel.Visibility = Visibility.Collapsed;

                EmptyStateText.Text = string.IsNullOrEmpty(SearchTextBox.Text.Trim())
                    ? "Рецепты не найдены"
                    : $"Ничего не найдено по запросу «{SearchTextBox.Text.Trim()}»";
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            foreach (var recipe in recipes)
            {
                ResultsPanel.Children.Add(CreateResultCard(recipe));
            }
        }

        private Border CreateResultCard(Recipes recipe)
        {
            var card = new Border
            {
                Width = 200,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 15, 15),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            card.Effect = new DropShadowEffect
            {
                BlurRadius = 8,
                Opacity = 0.08,
                ShadowDepth = 2
            };

            var content = new StackPanel { Orientation = Orientation.Vertical };

            // Эмодзи по категории блюда
            var emoji = GetMealCategoryEmoji(recipe.MealCategories?.Name);
            content.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 32,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Название
            content.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                MaxHeight = 40
            });

            // Время
            var time = recipe.CookingTime ?? 0;
            var timeText = time > 0 ? $"⏱️ {time} мин" : "⏱️ Время не указано";

            content.Children.Add(new TextBlock
            {
                Text = timeText,
                FontSize = 12,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Категория блюда
            if (recipe.MealCategories != null)
            {
                // Если есть иконка, показываем её
                var categoryText = string.IsNullOrEmpty(recipe.MealCategories.Icon)
                    ? $"• {recipe.MealCategories.Name}"
                    : $"{recipe.MealCategories.Icon} {recipe.MealCategories.Name}";

                content.Children.Add(new TextBlock
                {
                    Text = categoryText,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            card.Child = content;

            int recipeId = recipe.Id;
            card.MouseLeftButtonDown += (s, e) =>
            {
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            };

            return card;
        }

        private string GetMealCategoryEmoji(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "🍽️";

            switch (categoryName)
            {
                case "Супы": return "🥣";
                case "Салаты": return "🥗";
                case "Горячие блюда": return "🍲";
                case "Паста и каши": return "🍝";
                case "Выпечка": return "🥐";
                case "Десерты": return "🍰";
                case "Напитки": return "🥤";
                case "Закуски": return "🍢";
                case "Соусы": return "🥫";
                case "Вегетарианские": return "🥬";
                default: return "🍽️";
            }
        }

        private string GetCountLabel(int count)
        {
            if (count == 0) return "Ничего не найдено";
            return $"Найдено: {count} {GetCountWord(count)}";
        }

        private string GetCountWord(int count)
        {
            if (count % 100 >= 11 && count % 100 <= 19)
                return "рецептов";

            switch (count % 10)
            {
                case 1: return "рецепт";
                case 2:
                case 3:
                case 4: return "рецепта";
                default: return "рецептов";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearSearchButton.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            ApplyFilters();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SearchTextBox.Focus();
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем время
            Time30CheckBox.IsChecked = false;
            Time3060CheckBox.IsChecked = false;
            Time60CheckBox.IsChecked = false;

            // Сбрасываем категорию на "Все"
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is RadioButton radio)
                    radio.IsChecked = (int)radio.Tag == 0;
            }

            // Сбрасываем строку поиска
            SearchTextBox.Text = "";

            ApplyFilters();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}