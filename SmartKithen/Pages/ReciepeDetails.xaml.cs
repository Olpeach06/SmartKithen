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

            // Кнопка "Добавить рецепт в список покупок" - видна только авторизованным
            AddRecipeToShoppingListButton.Visibility = _isAuth ? Visibility.Visible : Visibility.Collapsed;

            // Кнопка "Добавить отмеченное" - видна только гостям
            AddSelectedToShoppingListButton.Visibility = _isAuth ? Visibility.Collapsed : Visibility.Visible;

            using (var db = new SmartKitchenEntities())
            {
                _recipe = db.Recipes.Include("Categories").Include("Ingredients.Products").Include("RecipeSteps").FirstOrDefault(r => r.Id == _recipeId);
                if (_recipe == null) { NavigationService.GoBack(); return; }

                RecipeTitleText.Text = _recipe.Title;
                RecipeDescriptionText.Text = _recipe.Description ?? "Описание отсутствует";
                CookingTimeText.Text = $"{_recipe.CookingTime ?? 0} мин";
                CategoryText.Text = _recipe.Categories?.Name ?? "Без категории";

                LoadIngredients();
                LoadSteps();
            }
        }

        private System.Collections.Generic.List<int> SelectedIds = new System.Collections.Generic.List<int>();

        private void LoadIngredients()
        {
            IngredientsPanel.Children.Clear();
            foreach (var ing in _recipe.Ingredients.OrderBy(i => i.Id))
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 12);

                var cb = new CheckBox { Margin = new Thickness(0, 0, 15, 0), Tag = ing.Id, VerticalAlignment = VerticalAlignment.Center };
                cb.Checked += (s, e) => SelectedIds.Add((int)((CheckBox)s).Tag);
                cb.Unchecked += (s, e) => SelectedIds.Remove((int)((CheckBox)s).Tag);

                var name = new TextBlock { Text = ing.Products?.Name ?? "Неизвестный продукт", FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
                var qty = new TextBlock { Text = $"{ing.Quantity} {ing.Unit ?? "шт."}", FontSize = 13, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(cb, 0); Grid.SetColumn(name, 1); Grid.SetColumn(qty, 2);
                row.Children.Add(cb); row.Children.Add(name); row.Children.Add(qty);
                IngredientsPanel.Children.Add(row);
            }
        }

        private void LoadSteps()
        {
            StepsPanel.Children.Clear();
            foreach (var step in _recipe.RecipeSteps.OrderBy(s => s.StepNumber))
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
                border.Child = new TextBlock { Text = $"{step.StepNumber}. {step.Description}", TextWrapping = TextWrapping.Wrap };
                StepsPanel.Children.Add(border);
            }
        }

        private void AddItems(System.Collections.Generic.List<int> ids)
        {
            if (!ids.Any()) { MessageBox.Show("Выберите продукты"); return; }
            using (var db = new SmartKitchenEntities())
            {
                int added = 0;
                foreach (var ing in _recipe.Ingredients.Where(i => ids.Contains(i.Id)))
                {
                    if (_isAuth)
                    {
                        if (!db.FridgeItems.Any(f => f.UserId == SessionManager.CurrentUserId && f.ProductId == ing.ProductId))
                        {
                            db.FridgeItems.Add(new FridgeItems
                            {
                                UserId = SessionManager.CurrentUserId,
                                ProductId = ing.ProductId,
                                Quantity = ing.Quantity,
                                ExpiryDate = DateTime.Today.AddMonths(1)
                            });
                            added++;
                        }
                    }
                    else
                    {
                        SessionManager.AddToGuestShoppingList(ing.ProductId, ing.Products.Name, ing.Quantity, ing.Unit);
                        added++;
                    }
                }
                if (_isAuth) db.SaveChanges();

                if (added > 0)
                {
                    MessageBox.Show(_isAuth
                        ? $"Добавлено {added} продуктов в холодильник"
                        : $"Добавлено {added} продуктов в список покупок");
                }
                else
                {
                    MessageBox.Show("Все выбранные продукты уже есть в вашем списке");
                }
            }
        }

        // Кнопка "Добавить отмеченное" - для гостей
        private void AddSelectedToShoppingListButton_Click(object sender, RoutedEventArgs e) => AddItems(SelectedIds);

        // Кнопка "Добавить рецепт в список покупок" - для авторизованных
        private void AddRecipeToShoppingListButton_Click(object sender, RoutedEventArgs e) => AddItems(_recipe.Ingredients.Select(i => i.Id).ToList());

        private void AddToFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAuth) { NavigationService?.Navigate(new Registration(true)); return; }
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
                    MessageBox.Show("Добавлено в избранное!");
                }
                else MessageBox.Show("Уже в избранном");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
        private void StartCookingButton_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new CookingMode());
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"{_recipe.Title}\n\n{_recipe.Description}");
            MessageBox.Show("Название и описание скопированы в буфер обмена!");
        }
        private void PrintButton_Click(object sender, RoutedEventArgs e) => new PrintDialog().ShowDialog();
    }
}