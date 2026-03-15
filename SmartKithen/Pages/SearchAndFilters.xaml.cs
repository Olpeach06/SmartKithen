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
        // Храним все рецепты — фильтруем на клиенте без лишних запросов
        private List<Recipes> _allRecipes = new List<Recipes>();

        public SearchAndFilters()
        {
            InitializeComponent();
            Loaded += SearchAndFilters_Loaded;
        }

        private void SearchAndFilters_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllRecipes();
            LoadCategories();
            ApplyFilters();
        }

        private void LoadAllRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    _allRecipes = context.Recipes
                        .Include("Categories")
                        .OrderBy(r => r.Title)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецептов: {ex.Message}");
            }
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

                    CategoriesPanel.Children.Clear();

                    // "Все категории" — первый RadioButton
                    var allRadio = new RadioButton
                    {
                        Content = "Все категории",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#555")),
                        GroupName = "Category",
                        IsChecked = true,
                        Tag = 0,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    CategoriesPanel.Children.Add(allRadio);

                    foreach (var cat in categories)
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
                        CategoriesPanel.Children.Add(radio);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text.Trim();
            var results = _allRecipes.AsEnumerable();

            // Поиск по названию
            if (!string.IsNullOrEmpty(searchText))
            {
                results = results.Where(r =>
                    r.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Фильтр по времени — если ни один не выбран, игнорируем фильтр
            var timeChecked = (Time30CheckBox.IsChecked == true)
                           || (Time3060CheckBox.IsChecked == true)
                           || (Time60CheckBox.IsChecked == true);

            if (timeChecked)
            {
                results = results.Where(r =>
                {
                    var time = r.CookingTime ?? 0;
                    if (Time30CheckBox.IsChecked == true && time < 30) return true;
                    if (Time3060CheckBox.IsChecked == true && time >= 30 && time <= 60) return true;
                    if (Time60CheckBox.IsChecked == true && time > 60) return true;
                    return false;
                });
            }

            // Фильтр по сложности (по времени)
            var diffChecked = (DifficultyEasyCheckBox.IsChecked == true)
                           || (DifficultyMediumCheckBox.IsChecked == true)
                           || (DifficultyHardCheckBox.IsChecked == true);

            if (diffChecked)
            {
                results = results.Where(r =>
                {
                    var time = r.CookingTime ?? 0;
                    if (DifficultyEasyCheckBox.IsChecked == true && time < 30) return true;
                    if (DifficultyMediumCheckBox.IsChecked == true && time >= 30 && time <= 60) return true;
                    if (DifficultyHardCheckBox.IsChecked == true && time > 60) return true;
                    return false;
                });
            }

            // Фильтр по категории
            var selectedCategoryId = GetSelectedCategoryId();
            if (selectedCategoryId != 0)
            {
                results = results.Where(r => r.CategoryId == selectedCategoryId);
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

            ResultsCountText.Text = finalList.Count == 0
                ? "Ничего не найдено"
                : GetCountLabel(finalList.Count);

            RenderResults(finalList);
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
                    ? "Рецептов по выбранным фильтрам не найдено"
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
                Width = 220,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 15, 15),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            card.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                Opacity = 0.08,
                ShadowDepth = 2
            };

            var content = new StackPanel { Orientation = Orientation.Vertical };

            // Эмодзи по категории
            var emoji = GetCategoryEmoji(recipe.Categories?.Name);
            content.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 28,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Название
            content.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 15,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Время и сложность
            var time = recipe.CookingTime ?? 0;
            var difficulty = GetDifficultyLabel(time);
            var timeText = time > 0 ? $"{time} мин" : "— мин";

            var infoRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            infoRow.Children.Add(new TextBlock
            {
                Text = timeText,
                FontSize = 12,
                Foreground = Brushes.Gray
            });
            infoRow.Children.Add(new TextBlock
            {
                Text = $"  •  {difficulty}",
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1"))
            });
            content.Children.Add(infoRow);

            // Категория
            if (recipe.Categories != null)
            {
                content.Children.Add(new TextBlock
                {
                    Text = recipe.Categories.Name,
                    FontSize = 11,
                    Foreground = Brushes.Gray
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

        private string GetDifficultyLabel(int cookingTime)
        {
            if (cookingTime == 0) return "Не указано";
            if (cookingTime < 30) return "Простая";
            if (cookingTime <= 60) return "Средняя";
            return "Сложная";
        }

        private string GetCategoryEmoji(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "🍽️";

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
                default: return "🍽️";
            }
        }

        private string GetCountLabel(int count)
        {
            if (count % 100 >= 11 && count % 100 <= 19)
                return $"{count} рецептов найдено";

            switch (count % 10)
            {
                case 1: return $"{count} рецепт найден";
                case 2:
                case 3:
                case 4: return $"{count} рецепта найдено";
                default: return $"{count} рецептов найдено";
            }
        }

        // Поиск в реальном времени
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClearSearchButton != null)
            {
                ClearSearchButton.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            if (ResultsPanel != null)
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

            // Сбрасываем сложность
            DifficultyEasyCheckBox.IsChecked = false;
            DifficultyMediumCheckBox.IsChecked = false;
            DifficultyHardCheckBox.IsChecked = false;

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

        // Заглушки для старых хендлеров если они вдруг остались в xaml
        private void Button_Click(object sender, RoutedEventArgs e) { }
        private void PastaCarbonaraButton_Click(object sender, RoutedEventArgs e) { }
        private void PastaBologneseButton_Click(object sender, RoutedEventArgs e) { }
        private void PastaSeafoodButton_Click(object sender, RoutedEventArgs e) { }
        private void PastaMushroomButton_Click(object sender, RoutedEventArgs e) { }
        private void ShowMoreButton_Click(object sender, RoutedEventArgs e) { }
    }
}