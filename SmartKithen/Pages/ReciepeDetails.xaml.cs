using System;
using System.Collections.Generic;
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
        private List<int> _selectedIngredientIds = new List<int>();

        public RecipeDetails()
        {
            InitializeComponent();
            Loaded += RecipeDetails_Loaded;

            // Подписываемся на события кнопок
            BackButton.Click += BackButton_Click;
            StartCookingButton.Click += StartCookingButton_Click;
            AddToShoppingListButton.Click += AddToShoppingListButton_Click;
            AddToFavoritesButton.Click += AddToFavoritesButton_Click;
            ShareButton.Click += ShareButton_Click;
            PrintButton.Click += PrintButton_Click;
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
            RecipeTitleText.Text = _currentRecipe.Title;

            // Описание
            RecipeDescriptionText.Text = !string.IsNullOrEmpty(_currentRecipe.Description)
                ? _currentRecipe.Description
                : "Описание отсутствует";

            // Время приготовления
            CookingTimeText.Text = _currentRecipe.CookingTime.HasValue
                ? $"{_currentRecipe.CookingTime.Value} мин"
                : "Время не указано";

            // Категория
            if (_currentRecipe.Categories != null)
            {
                CategoryText.Text = _currentRecipe.Categories.Name;

                if (CategoryBadge != null)
                {
                    CategoryBadge.Visibility = Visibility.Visible;
                    var textBlock = FindVisualChild<TextBlock>(CategoryBadge);
                    if (textBlock != null)
                        textBlock.Text = _currentRecipe.Categories.Name;
                }
            }
            else
            {
                CategoryText.Text = "Без категории";
                if (CategoryBadge != null)
                    CategoryBadge.Visibility = Visibility.Collapsed;
            }

            LoadIngredients();
            LoadSteps();
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    var descendant = FindVisualChild<T>(child);
                    if (descendant != null)
                        return descendant;
                }
            }
            return null;
        }

        private void LoadIngredients()
        {
            IngredientsPanel.Children.Clear();
            _selectedIngredientIds.Clear();

            if (_currentRecipe.Ingredients == null || !_currentRecipe.Ingredients.Any())
            {
                IngredientsPanel.Children.Add(new TextBlock
                {
                    Text = "Список ингредиентов отсутствует",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
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
                    Tag = ingredient.Id,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkBox.Checked += IngredientCheckBox_Checked;
                checkBox.Unchecked += IngredientCheckBox_Unchecked;

                var nameText = new TextBlock
                {
                    Text = ingredient.Products?.Name ?? "Неизвестный продукт",
                    FontSize = 14,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var quantityText = new TextBlock
                {
                    Text = $"{ingredient.Quantity} {ingredient.Unit ?? "шт."}",
                    FontSize = 13,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
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

        private void IngredientCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int ingredientId)
            {
                if (!_selectedIngredientIds.Contains(ingredientId))
                    _selectedIngredientIds.Add(ingredientId);
            }
        }

        private void IngredientCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int ingredientId)
            {
                _selectedIngredientIds.Remove(ingredientId);
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
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
                return;
            }

            foreach (var step in _currentRecipe.RecipeSteps.OrderBy(s => s.StepNumber))
            {
                var border = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5FFF9")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9")),
                    BorderThickness = new Thickness(1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var numberBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    CornerRadius = new CornerRadius(12),
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };

                var numberText = new TextBlock
                {
                    Text = step.StepNumber.ToString(),
                    FontSize = 12,
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
                    LineHeight = 20
                };

                Grid.SetColumn(numberBorder, 0);
                Grid.SetColumn(descriptionText, 1);
                grid.Children.Add(numberBorder);
                grid.Children.Add(descriptionText);

                border.Child = grid;
                StepsPanel.Children.Add(border);
            }
        }

        private void AddToHistory(int recipeId)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
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
            catch { }
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

        private void AddSelectedToShoppingList()
        {
            if (!SessionManager.IsLoggedIn)
            {
                ShowRegistrationPrompt();
                return;
            }

            if (!_selectedIngredientIds.Any())
            {
                MessageBox.Show("Вы не выбрали ни одного ингредиента.", "Список покупок",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    int added = 0;

                    foreach (var ingredientId in _selectedIngredientIds)
                    {
                        var ingredient = _currentRecipe.Ingredients
                            .FirstOrDefault(i => i.Id == ingredientId);

                        if (ingredient != null)
                        {
                            var inFridge = context.FridgeItems.Any(
                                f => f.UserId == SessionManager.CurrentUserId
                                  && f.ProductId == ingredient.ProductId);

                            if (!inFridge)
                            {
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
                    }

                    context.SaveChanges();

                    MessageBox.Show($"Добавлено {added} ингредиентов в список покупок!",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                    _selectedIngredientIds.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        // Обработчики кнопок
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void StartCookingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CookingMode());
        }

        private void AddToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedToShoppingList();
        }

        private void AddToFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            AddToFavorites();
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var text = $"{_currentRecipe.Title}\n\n{_currentRecipe.Description}";
            Clipboard.SetText(text);
            MessageBox.Show("Название и описание скопированы в буфер обмена.", "Поделиться",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
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
    }
}