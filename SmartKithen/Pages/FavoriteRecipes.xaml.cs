using SmartKithen.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class FavoriteRecipesPage : Page
    {
        public FavoriteRecipesPage()
        {
            InitializeComponent();
            Loaded += FavoriteRecipesPage_Loaded;
        }

        private void FavoriteRecipesPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFavorites();
        }

        private void LoadFavorites()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var favorites = context.FavoriteRecipes
                        .Include("Recipes")
                        .Where(f => f.UserId == SessionManager.CurrentUserId)
                        .OrderByDescending(f => f.AddedDate)
                        .ToList();

                    FavoritesPanel.Children.Clear();
                    FavCountText.Text = favorites.Count.ToString();

                    if (favorites.Count == 0)
                    {
                        EmptyState.Visibility = Visibility.Visible;
                        return;
                    }

                    EmptyState.Visibility = Visibility.Collapsed;

                    foreach (var fav in favorites)
                    {
                        FavoritesPanel.Children.Add(CreateFavoriteCard(fav));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки избранного: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateFavoriteCard(FavoriteRecipes fav)
        {
            var recipe = fav.Recipes;

            // Карточка
            var card = new Border
            {
                Width = 175,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 15, 15),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                Opacity = 0.08,
                ShadowDepth = 3
            };

            var content = new StackPanel { Orientation = Orientation.Vertical };

            // Изображение рецепта
            var imageContainer = new Border
            {
                Height = 120,
                CornerRadius = new CornerRadius(15, 15, 0, 0),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F0F0F0"))
            };

            var image = new Image
            {
                Source = ImageLoader.LoadRecipeImage(recipe?.ImagePath),
                Stretch = System.Windows.Media.Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            imageContainer.Child = image;
            content.Children.Add(imageContainer);

            // Внутреннее содержимое
            var innerContent = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(12, 10, 12, 10)
            };

            // Название
            innerContent.Children.Add(new TextBlock
            {
                Text = recipe?.Title ?? "Без названия",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Время приготовления
            var timeText = recipe?.CookingTime.HasValue == true
                ? $"🕐 {recipe.CookingTime.Value} мин"
                : "🕐 Не указано";

            innerContent.Children.Add(new TextBlock
            {
                Text = timeText,
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Дата добавления
            innerContent.Children.Add(new TextBlock
            {
                Text = $"Добавлено {fav.AddedDate:dd.MM.yyyy}",
                FontSize = 10,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Кнопка удалить из избранного
            var removeBtn = new Border
            {
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Cursor = Cursors.Hand
            };

            var removeBtnInner = new Button
            {
                Content = "✕ Убрать",
                FontSize = 10,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                Padding = new Thickness(8, 4, 8, 4),
                Tag = fav.Id
            };

            removeBtnInner.Click += RemoveFromFavorites_Click;
            removeBtn.Child = removeBtnInner;
            innerContent.Children.Add(removeBtn);

            content.Children.Add(innerContent);
            card.Child = content;

            // Клик по карточке — открываем рецепт
            int recipeId = recipe?.Id ?? 0;
            card.MouseLeftButtonDown += (s, e) =>
            {
                // Не открываем если кликнули по кнопке удаления
                if (e.OriginalSource is Button ||
                    (e.OriginalSource as FrameworkElement)?.Parent is Button)
                    return;

                if (recipeId > 0)
                    NavigationService?.Navigate(new RecipeDetails(recipeId));
            };

            return card;
        }

        private void RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.Tag is int favId)) return;

            var confirm = MessageBox.Show("Убрать рецепт из избранного?", "Избранное",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var fav = context.FavoriteRecipes.FirstOrDefault(f => f.Id == favId);
                    if (fav != null)
                    {
                        context.FavoriteRecipes.Remove(fav);
                        context.SaveChanges();
                    }
                }

                // Перезагружаем список
                LoadFavorites();
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