using SmartKithen.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class RecipeHistoryPage : Page
    {
        public RecipeHistoryPage()
        {
            InitializeComponent();
            Loaded += RecipeHistoryPage_Loaded;
        }

        private void RecipeHistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем, авторизован ли пользователь
            if (SessionManager.CurrentUserId == 0 || SessionManager.IsGuestMode)
            {
                MessageBox.Show("История просмотров доступна только авторизованным пользователям.",
                    "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.GoBack();
                return;
            }

            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Пробуем загрузить с Include для Recipes
                    var historyQuery = context.RecipeHistory
                        .Where(h => h.UserId == SessionManager.CurrentUserId)
                        .OrderByDescending(h => h.ViewedAt);

                    // Пытаемся использовать Include, если свойство существует
                    try
                    {
                        // Вариант 1: если свойство называется "Recipes"
                        var historyWithInclude = context.RecipeHistory
                            .Include("Recipes")
                            .Where(h => h.UserId == SessionManager.CurrentUserId)
                            .OrderByDescending(h => h.ViewedAt)
                            .ToList();

                        LoadHistoryFromList(historyWithInclude);
                        return;
                    }
                    catch
                    {
                        // Если не получилось с "Recipes", пробуем "Recipe"
                        try
                        {
                            var historyWithInclude = context.RecipeHistory
                                .Include("Recipe")
                                .Where(h => h.UserId == SessionManager.CurrentUserId)
                                .OrderByDescending(h => h.ViewedAt)
                                .ToList();

                            LoadHistoryFromList(historyWithInclude);
                            return;
                        }
                        catch
                        {
                            // Если Include не работает, загружаем без Include
                            var historyWithoutInclude = context.RecipeHistory
                                .Where(h => h.UserId == SessionManager.CurrentUserId)
                                .OrderByDescending(h => h.ViewedAt)
                                .ToList();

                            // Подгружаем рецепты отдельно
                            var recipeIds = historyWithoutInclude.Select(h => h.RecipeId).Distinct().ToList();
                            var recipes = context.Recipes
                                .Where(r => recipeIds.Contains(r.Id))
                                .ToDictionary(r => r.Id, r => r);

                            foreach (var item in historyWithoutInclude)
                            {
                                if (recipes.ContainsKey(item.RecipeId))
                                {
                                    // Устанавливаем навигационное свойство через рефлексию или просто используем словарь при отображении
                                    // Временно сохраним в локальной переменной
                                }
                            }

                            LoadHistoryFromList(historyWithoutInclude, recipes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistoryFromList(System.Collections.Generic.List<RecipeHistory> history, System.Collections.Generic.Dictionary<int, Recipes> recipes = null)
        {
            HistoryPanel.Children.Clear();

            if (history.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;

            // Группируем по дате
            var grouped = history
                .GroupBy(h => h.ViewedAt.Date)
                .OrderByDescending(g => g.Key);

            foreach (var group in grouped)
            {
                // Заголовок дня
                var dayLabel = GetDayLabel(group.Key);

                var dayHeader = new TextBlock
                {
                    Text = dayLabel,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999")),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                HistoryPanel.Children.Add(dayHeader);

                // Карточка-контейнер для группы
                var groupCard = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(5),
                    Margin = new Thickness(0, 0, 0, 20)
                };

                groupCard.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12,
                    Opacity = 0.08,
                    ShadowDepth = 3
                };

                var groupStack = new StackPanel { Orientation = Orientation.Vertical };

                // Убираем дубли — один рецепт показываем один раз за день
                var uniqueInDay = group
                    .GroupBy(h => h.RecipeId)
                    .Select(g => g.OrderByDescending(h => h.ViewedAt).First())
                    .ToList();

                for (int i = 0; i < uniqueInDay.Count; i++)
                {
                    var item = uniqueInDay[i];
                    var isLast = i == uniqueInDay.Count - 1;

                    // Получаем рецепт из словаря, если он есть
                    Recipes recipe = null;
                    if (recipes != null && recipes.ContainsKey(item.RecipeId))
                    {
                        recipe = recipes[item.RecipeId];
                    }
                    else
                    {
                        recipe = item.Recipes;
                    }

                    groupStack.Children.Add(CreateHistoryRow(item, recipe, isLast));
                }

                groupCard.Child = groupStack;
                HistoryPanel.Children.Add(groupCard);
            }
        }

        private void LoadHistoryFromList(System.Collections.Generic.List<RecipeHistory> history)
        {
            LoadHistoryFromList(history, null);
        }

        private Border CreateHistoryRow(RecipeHistory item, Recipes recipe, bool isLast)
        {
            var row = new Border
            {
                Padding = new Thickness(18, 14, 18, 14),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F0F0F0")),
                BorderThickness = isLast
                    ? new Thickness(0)
                    : new Thickness(0, 0, 0, 1),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Эмодзи
            var emoji = new TextBlock
            {
                Text = GetRecipeEmoji(recipe?.Title),
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };

            // Название и время просмотра
            var info = new StackPanel { Orientation = Orientation.Vertical };

            info.Children.Add(new TextBlock
            {
                Text = recipe?.Title ?? item.Recipes?.Title ?? "Удалённый рецепт",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34"))
            });

            info.Children.Add(new TextBlock
            {
                Text = item.ViewedAt.ToString("HH:mm"),
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 3, 0, 0)
            });

            // Стрелка
            var arrow = new TextBlock
            {
                Text = "›",
                FontSize = 18,
                Foreground = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(emoji, 0);
            Grid.SetColumn(info, 1);
            Grid.SetColumn(arrow, 2);

            grid.Children.Add(emoji);
            grid.Children.Add(info);
            grid.Children.Add(arrow);

            row.Child = grid;

            // Клик — открываем рецепт
            int recipeId = item.RecipeId;
            row.MouseLeftButtonDown += (s, e) =>
            {
                if (recipeId > 0)
                    NavigationService?.Navigate(new RecipeDetails(recipeId));
                else
                    MessageBox.Show("Рецепт был удалён", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            };

            return row;
        }

        private string GetRecipeEmoji(string title)
        {
            if (string.IsNullOrEmpty(title)) return "🍽️";

            // Приводим к нижнему регистру для сравнения
            string lowerTitle = title.ToLower();

            // Простая эмодзи-подсказка по названию
            if (lowerTitle.Contains("суп")) return "🥣";
            if (lowerTitle.Contains("салат")) return "🥗";
            if (lowerTitle.Contains("пирог")) return "🥧";
            if (lowerTitle.Contains("торт")) return "🍰";
            if (lowerTitle.Contains("каша")) return "🥣";
            if (lowerTitle.Contains("макарон")) return "🍝";
            if (lowerTitle.Contains("паста")) return "🍝";
            if (lowerTitle.Contains("курица")) return "🍗";
            if (lowerTitle.Contains("рыб")) return "🐟";
            if (lowerTitle.Contains("мяс")) return "🥩";

            return "🍽️";
        }

        private string GetDayLabel(DateTime date)
        {
            if (date == DateTime.Today)
                return "Сегодня";
            if (date == DateTime.Today.AddDays(-1))
                return "Вчера";
            return date.ToString("d MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));
        }

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Очистить всю историю просмотров?", "История",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var all = context.RecipeHistory
                        .Where(h => h.UserId == SessionManager.CurrentUserId);
                    context.RecipeHistory.RemoveRange(all);
                    context.SaveChanges();
                }

                LoadHistory();

                MessageBox.Show("История просмотров очищена", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}