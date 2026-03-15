using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class RecipeDetails : Page
    {
        private int _recipeId;
        private Recipes _currentRecipe;

        public RecipeDetails()
        {
            InitializeComponent();
            Loaded += RecipeDetails_Loaded;
        }

        public RecipeDetails(int recipeId) : this()
        {
            _recipeId = recipeId;
        }

        private void RecipeDetails_Loaded(object sender, RoutedEventArgs e)
        {
            if (_recipeId > 0)
            {
                LoadRecipeData(_recipeId);
            }
            else
            {
                MessageBox.Show("Рецепт не выбран", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.GoBack();
            }
        }

        private void LoadRecipeData(int recipeId)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    _currentRecipe = context.Recipes
                        .Include("Categories")
                        .Include("Ingredients")
                        .Include("Ingredients.Products")
                        .Include("RecipeSteps")
                        .FirstOrDefault(r => r.Id == recipeId);

                    if (_currentRecipe != null)
                    {
                        DisplayRecipeData();

                        // Записываем в историю только для авторизованного пользователя
                        if (SessionManager.IsLoggedIn)
                            AddToHistory(recipeId);
                    }
                    else
                    {
                        MessageBox.Show("Рецепт не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService?.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецепта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
            }
        }

        private void DisplayRecipeData()
        {
            // Название
            RecipeTitleText.Text = _currentRecipe.Title.ToUpper();

            // Описание
            RecipeDescriptionText.Text = !string.IsNullOrEmpty(_currentRecipe.Description)
                ? _currentRecipe.Description
                : "Описание отсутствует";

            // Время приготовления
            CookingTimeText.Text = _currentRecipe.CookingTime.HasValue
                ? $"{_currentRecipe.CookingTime.Value} мин"
                : "Не указано";

            // Категория вместо сложности
            DifficultyText.Text = _currentRecipe.Categories != null
                ? _currentRecipe.Categories.Name
                : "Не указана";

            LoadIngredients();
            LoadSteps();
        }

        private void LoadIngredients()
        {
            IngredientsPanel.Children.Clear();

            if (_currentRecipe.Ingredients == null || !_currentRecipe.Ingredients.Any())
            {
                IngredientsPanel.Children.Add(new TextBlock
                {
                    Text = "Список ингредиентов отсутствует",
                    FontSize = 14,
                    Foreground = Brushes.Gray
                });
                return;
            }

            foreach (var ingredient in _currentRecipe.Ingredients.OrderBy(i => i.Id))
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 12);

                var checkBox = new CheckBox
                {
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var nameText = new TextBlock
                {
                    Text = ingredient.Products?.Name ?? "Неизвестный продукт",
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var quantityText = new TextBlock
                {
                    Text = $"{ingredient.Quantity} {ingredient.Unit ?? "шт."}",
                    FontSize = 13,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(checkBox, 0);
                Grid.SetColumn(nameText, 1);
                Grid.SetColumn(quantityText, 2);

                row.Children.Add(checkBox);
                row.Children.Add(nameText);
                row.Children.Add(quantityText);

                IngredientsPanel.Children.Add(row);
            }
        }

        private void LoadSteps()
        {
            StepsPanel.Children.Clear();

            if (_currentRecipe.RecipeSteps == null || !_currentRecipe.RecipeSteps.Any())
            {
                StepsPanel.Children.Add(new TextBlock
                {
                    Text = "Шаги приготовления отсутствуют",
                    FontSize = 14,
                    Foreground = Brushes.Gray
                });
                return;
            }

            foreach (var step in _currentRecipe.RecipeSteps.OrderBy(s => s.StepNumber))
            {
                var border = new Border
                {
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(20, 15, 20, 15),
                    Margin = new Thickness(0, 0, 0, 15),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#F5FFF9")),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#C8E6C9")),
                    BorderThickness = new Thickness(1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                // Номер шага в круглом badge
                var numberBorder = new Border
                {
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1A5D34")),
                    CornerRadius = new CornerRadius(15),
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };

                var numberText = new TextBlock
                {
                    Text = step.StepNumber.ToString(),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                numberBorder.Child = numberText;

                var descriptionText = new TextBlock
                {
                    Text = step.Description,
                    FontSize = 14,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(numberBorder, 0);
                Grid.SetColumn(descriptionText, 1);
                grid.Children.Add(numberBorder);
                grid.Children.Add(descriptionText);

                border.Child = grid;
                StepsPanel.Children.Add(border);
            }
        }

        // Запись в историю просмотра
        private void AddToHistory(int recipeId)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Проверяем, есть ли уже запись за сегодня — не дублируем
                    var today = DateTime.Today;
                    var alreadyViewed = context.RecipeHistory.Any(
                        h => h.UserId == SessionManager.CurrentUserId
                          && h.RecipeId == recipeId
                          && h.ViewedAt >= today);

                    if (!alreadyViewed)
                    {
                        var history = new RecipeHistory
                        {
                            UserId = SessionManager.CurrentUserId,
                            RecipeId = recipeId,
                            ViewedAt = DateTime.Now
                        };
                        context.RecipeHistory.Add(history);
                        context.SaveChanges();
                    }
                }
            }
            catch
            {
                // История не критична — молча игнорируем
            }
        }

        private void AddToFavorites()
        {
            if (!SessionManager.IsLoggedIn)
            {
                ShowRegistrationPrompt();
                return;
            }

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Проверяем, не добавлен ли уже
                    var alreadyFavorite = context.FavoriteRecipes.Any(
                        f => f.UserId == SessionManager.CurrentUserId
                          && f.RecipeId == _recipeId);

                    if (alreadyFavorite)
                    {
                        MessageBox.Show("Рецепт уже в избранном.", "Избранное",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var favorite = new FavoriteRecipes
                    {
                        UserId = SessionManager.CurrentUserId,
                        RecipeId = _recipeId,
                        AddedDate = DateTime.Now
                    };
                    context.FavoriteRecipes.Add(favorite);
                    context.SaveChanges();
                }

                MessageBox.Show("Рецепт добавлен в избранное!", "Избранное",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddIngredientsToShoppingList()
        {
            if (_currentRecipe.Ingredients == null || !_currentRecipe.Ingredients.Any())
            {
                MessageBox.Show("У этого рецепта нет ингредиентов.", "Список покупок",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    int added = 0;

                    foreach (var ingredient in _currentRecipe.Ingredients)
                    {
                        // Проверяем, есть ли уже этот продукт в холодильнике у пользователя
                        var inFridge = context.FridgeItems.Any(
                            f => f.UserId == SessionManager.CurrentUserId
                              && f.ProductId == ingredient.ProductId);

                        if (!inFridge)
                        {
                            // Добавляем в холодильник как "нужно купить"
                            // ExpiryDate ставим через месяц как заглушку
                            var item = new FridgeItems
                            {
                                UserId = SessionManager.CurrentUserId,
                                ProductId = ingredient.ProductId,
                                Quantity = ingredient.Quantity,
                                ExpiryDate = DateTime.Today.AddMonths(1)
                            };
                            context.FridgeItems.Add(item);
                            added++;
                        }
                    }

                    context.SaveChanges();

                    var message = added > 0
                        ? $"Добавлено {added} ингредиент(ов) в список.\nУже имеющиеся пропущены."
                        : "Все ингредиенты уже есть в вашем списке продуктов.";

                    MessageBox.Show(message, "Список покупок",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в список: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCookingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CookingMode());
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var next = context.Recipes
                        .Where(r => r.Id > _recipeId)
                        .OrderBy(r => r.Id)
                        .FirstOrDefault();

                    if (next == null)
                        next = context.Recipes.OrderBy(r => r.Id).FirstOrDefault();

                    if (next != null)
                        NavigationService?.Navigate(new RecipeDetails(next.Id));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var addToFav = new MenuItem { Header = "❤️ Добавить в избранное" };
            addToFav.Click += (s, args) => AddToFavorites();

            var share = new MenuItem { Header = "📤 Поделиться" };
            share.Click += (s, args) => ShareRecipe();

            var print = new MenuItem { Header = "🖨️ Распечатать" };
            print.Click += (s, args) => PrintRecipe();

            menu.Items.Add(addToFav);
            menu.Items.Add(share);
            menu.Items.Add(print);

            if (sender is Button button)
            {
                menu.PlacementTarget = button;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void AddToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Добавить все ингредиенты в список покупок?",
                "Список покупок",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            if (SessionManager.IsLoggedIn)
                AddIngredientsToShoppingList();
            else
                ShowRegistrationPrompt();
        }

        private void ShowRegistrationPrompt()
        {
            var result = MessageBox.Show(
                "Для этой функции нужен аккаунт. Зарегистрироваться?",
                "Требуется регистрация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                NavigationService?.Navigate(new Registration());
        }

        private void ShareRecipe()
        {
            var text = $"{_currentRecipe.Title}\n\n{_currentRecipe.Description}";
            Clipboard.SetText(text);
            MessageBox.Show("Название и описание скопированы в буфер обмена.", "Поделиться",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintRecipe()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    MessageBox.Show("Рецепт отправлен на печать.", "Печать",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRatingStars(decimal rating)
        {
            int full = (int)rating;
            bool half = (rating - full) >= 0.5m;
            string stars = new string('★', full);
            if (half) stars += "½";
            stars += new string('☆', 5 - full - (half ? 1 : 0));
            return stars;
        }
    }
}