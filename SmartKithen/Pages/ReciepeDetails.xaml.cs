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
        private Recipes _recipe;
        private bool _isAuth;

        public RecipeDetails() => InitializeComponent();
        public RecipeDetails(int id) : this() { _recipeId = id; Loaded += (s, e) => LoadData(); }

        private void LoadData()
        {
            _isAuth = !SessionManager.IsGuestMode;

            // Настраиваем видимость кнопок в зависимости от типа пользователя
            if (_isAuth)
            {
                // Авторизованный пользователь - только кнопка добавления всего рецепта
                AddRecipeToShoppingListButton.Visibility = Visibility.Visible;
                AddSelectedToShoppingListButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Гость - только кнопка добавления отмеченных продуктов
                AddRecipeToShoppingListButton.Visibility = Visibility.Collapsed;
                AddSelectedToShoppingListButton.Visibility = Visibility.Visible;
            }

            using (var db = new SmartKitchenEntities())
            {
                // Загружаем рецепт
                _recipe = db.Recipes.FirstOrDefault(r => r.Id == _recipeId);

                if (_recipe == null)
                {
                    NavigationService.GoBack();
                    return;
                }

                // Загружаем связанные данные
                db.Entry(_recipe).Reference(r => r.MealCategories).Load();
                db.Entry(_recipe).Collection(r => r.Ingredients).Load();

                // Загружаем продукты для ингредиентов
                foreach (var ingredient in _recipe.Ingredients)
                {
                    db.Entry(ingredient).Reference(i => i.Products).Load();
                }

                db.Entry(_recipe).Collection(r => r.RecipeSteps).Load();

                RecipeTitleText.Text = _recipe.Title;
                RecipeDescriptionText.Text = _recipe.Description ?? "Описание отсутствует";
                CookingTimeText.Text = $"{_recipe.CookingTime ?? 0} мин";

                // Скрываем категорию продукта
                CategoryText.Visibility = Visibility.Collapsed;
                // Скрываем Border с категорией продукта
                var categoryBorder = CategoryText.Parent as Border;
                if (categoryBorder != null) categoryBorder.Visibility = Visibility.Collapsed;

                // Категория блюда (MealCategory)
                if (_recipe.MealCategories != null)
                {
                    MealCategoryBorder.Visibility = Visibility.Visible;
                    MealCategoryIcon.Text = _recipe.MealCategories.Icon ?? "🍽️";
                    MealCategoryText.Text = _recipe.MealCategories.Name;
                }
                else
                {
                    MealCategoryBorder.Visibility = Visibility.Collapsed;
                }

                LoadIngredients();
                LoadSteps();

                // Сохраняем просмотр в историю (только для авторизованных)
                if (_isAuth)
                {
                    SaveToHistory(db);
                }
            }
        }

        private void SaveToHistory(SmartKitchenEntities db)
        {
            try
            {
                // Проверяем, был ли уже просмотр сегодня (чтобы не дублировать)
                var today = DateTime.Today;
                var existingToday = db.RecipeHistory
                    .FirstOrDefault(h => h.UserId == SessionManager.CurrentUserId
                        && h.RecipeId == _recipeId
                        && h.ViewedAt >= today);

                if (existingToday == null)
                {
                    var history = new RecipeHistory
                    {
                        UserId = SessionManager.CurrentUserId,
                        RecipeId = _recipeId,
                        ViewedAt = DateTime.Now
                    };
                    db.RecipeHistory.Add(history);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения истории: {ex.Message}");
            }
        }

        private System.Collections.Generic.List<int> SelectedIds = new System.Collections.Generic.List<int>();

        private void LoadIngredients()
        {
            IngredientsPanel.Children.Clear();
            SelectedIds.Clear();

            foreach (var ing in _recipe.Ingredients.OrderBy(i => i.Id))
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 12);

                // Чекбокс показываем только для гостей
                if (!_isAuth)
                {
                    var cb = new CheckBox
                    {
                        Margin = new Thickness(0, 0, 15, 0),
                        Tag = ing.Id,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cb.Checked += (s, e) => SelectedIds.Add((int)((CheckBox)s).Tag);
                    cb.Unchecked += (s, e) => SelectedIds.Remove((int)((CheckBox)s).Tag);

                    Grid.SetColumn(cb, 0);
                    row.Children.Add(cb);
                }

                var name = new TextBlock
                {
                    Text = ing.Products?.Name ?? "Неизвестный продукт",
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Определяем единицу измерения
                string unit = ing.Unit;
                if (string.IsNullOrEmpty(unit) && ing.Products != null)
                {
                    unit = ing.Products.DefaultUnit;
                }
                if (string.IsNullOrEmpty(unit))
                {
                    unit = "г";
                }

                var qty = new TextBlock
                {
                    Text = $"{ing.Quantity:N2} {unit}",
                    FontSize = 13,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                // Если чекбокс есть, то имя и количество сдвигаются на колонку 1 и 2
                // Если чекбокса нет, то имя и количество на колонку 0 и 1
                if (!_isAuth)
                {
                    Grid.SetColumn(name, 1);
                    Grid.SetColumn(qty, 2);
                }
                else
                {
                    Grid.SetColumn(name, 0);
                    Grid.SetColumn(qty, 1);
                }

                row.Children.Add(name);
                row.Children.Add(qty);

                IngredientsPanel.Children.Add(row);
            }
        }

        private void LoadSteps()
        {
            StepsPanel.Children.Clear();
            var steps = _recipe.RecipeSteps.OrderBy(s => s.StepNumber);

            if (!steps.Any())
            {
                // Если нет пошаговых инструкций, показываем описание
                var border = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5FFF9")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9")),
                    BorderThickness = new Thickness(1)
                };
                border.Child = new TextBlock
                {
                    Text = _recipe.Instructions ?? "Инструкции по приготовлению отсутствуют",
                    TextWrapping = TextWrapping.Wrap
                };
                StepsPanel.Children.Add(border);
            }
            else
            {
                foreach (var step in steps)
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
                    border.Child = new TextBlock
                    {
                        Text = $"{step.StepNumber}. {step.Description}",
                        TextWrapping = TextWrapping.Wrap
                    };
                    StepsPanel.Children.Add(border);
                }
            }
        }

        // Добавление отмеченных продуктов в список покупок (только для гостей)
        private void AddSelectedToShoppingList()
        {
            if (!SelectedIds.Any())
            {
                MessageBox.Show("Выберите продукты", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int added = 0;

            foreach (var ing in _recipe.Ingredients.Where(i => SelectedIds.Contains(i.Id)))
            {
                // Определяем единицу измерения
                string unit = ing.Unit;
                if (string.IsNullOrEmpty(unit) && ing.Products != null)
                {
                    unit = ing.Products.DefaultUnit;
                }
                if (string.IsNullOrEmpty(unit))
                {
                    unit = "г";
                }

                // Добавляем в список покупок гостя
                SessionManager.AddToGuestShoppingList(
                    ing.ProductId,
                    ing.Products?.Name ?? "Неизвестный продукт",
                    ing.Quantity,
                    unit);
                added++;
            }

            if (added > 0)
            {
                MessageBox.Show($"Добавлено {added} продуктов в список покупок", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Очищаем выбранные чекбоксы после добавления
                SelectedIds.Clear();
                // Перезагружаем ингредиенты, чтобы снять все чекбоксы
                LoadIngredients();
            }
        }

        // Добавление всего рецепта в список покупок (только для авторизованных)
        private void AddFullRecipeToShoppingList()
        {
            int added = 0;

            foreach (var ing in _recipe.Ingredients)
            {
                // Определяем единицу измерения
                string unit = ing.Unit;
                if (string.IsNullOrEmpty(unit) && ing.Products != null)
                {
                    unit = ing.Products.DefaultUnit;
                }
                if (string.IsNullOrEmpty(unit))
                {
                    unit = "г";
                }

                // Добавляем в список покупок пользователя
                SessionManager.AddToGuestShoppingList(
                    ing.ProductId,
                    ing.Products?.Name ?? "Неизвестный продукт",
                    ing.Quantity,
                    unit);
                added++;
            }

            if (added > 0)
            {
                MessageBox.Show($"Добавлено {added} продуктов из рецепта в список покупок", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Кнопка "Добавить отмеченное" (только для гостей)
        private void AddSelectedToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedToShoppingList();
        }

        // Кнопка "Добавить рецепт в список покупок" (только для авторизованных)
        private void AddRecipeToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            AddFullRecipeToShoppingList();
        }

        private void AddToFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAuth)
            {
                NavigationService?.Navigate(new Registration(true));
                return;
            }

            using (var db = new SmartKitchenEntities())
            {
                if (!db.FavoriteRecipes.Any(f => f.UserId == SessionManager.CurrentUserId && f.RecipeId == _recipeId))
                {
                    db.FavoriteRecipes.Add(new FavoriteRecipes
                    {
                        UserId = SessionManager.CurrentUserId,
                        RecipeId = _recipeId,
                        AddedDate = DateTime.Now
                    });
                    db.SaveChanges();
                    MessageBox.Show("Рецепт добавлен в избранное!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Этот рецепт уже в избранном", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();

        private void StartCookingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CookingMode(_recipeId));
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            string shareText = $"{_recipe.Title}\n\n{_recipe.Description}\n\nВремя приготовления: {_recipe.CookingTime ?? 0} мин.\n\n#SmartKitchen";
            Clipboard.SetText(shareText);
            MessageBox.Show("Информация о рецепте скопирована в буфер обмена!", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                MessageBox.Show("Функция печати в разработке. Используйте печать страницы браузера.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}