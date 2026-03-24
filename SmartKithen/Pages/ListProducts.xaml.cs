using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SmartKithen.Pages
{
    public partial class ListProducts : Page
    {
        private List<Recipes> _userRecipes;
        private List<ShoppingItem> _shoppingList;
        private Dictionary<int, bool> _selectedRecipes = new Dictionary<int, bool>();
        private Dictionary<int, bool> _selectedShoppingItems = new Dictionary<int, bool>();
        private bool _isGuestMode;
        private int _nextTempId = 1000;

        private class ShoppingItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
            public string Category { get; set; }
            public bool IsFromRecipe { get; set; }
            public int? ProductId { get; set; }
        }

        public ListProducts()
        {
            InitializeComponent();
            Loaded += ListProducts_Loaded;

            // Безопасная подписка на событие NavigationService
            if (NavigationService != null)
            {
                NavigationService.LoadCompleted += NavigationService_LoadCompleted;
            }
        }

        private void NavigationService_LoadCompleted(object sender, NavigationEventArgs e)
        {
            RefreshData();
        }

        private void ListProducts_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                _isGuestMode = SessionManager.IsGuestMode;
                UpdateUserDisplay();
                ConfigureUIMode();
                LoadUserData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в RefreshData: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureUIMode()
        {
            if (_isGuestMode)
            {
                RecipesSection.Visibility = Visibility.Collapsed;
                EmptyProductsTitle.Text = "Список покупок пуст";
                EmptyProductsMessage.Text = "Добавьте продукты из рецептов или нажмите «Добавить продукт»";
            }
            else
            {
                RecipesSection.Visibility = Visibility.Visible;
                EmptyProductsMessage.Text = "Выберите рецепты и нажмите «Добавить недостающие продукты»";
            }
        }

        private void UpdateUserDisplay()
        {
            try
            {
                if (UserNameText != null)
                {
                    if (!_isGuestMode && App.CurrentUser != null && !string.IsNullOrEmpty(App.CurrentUser.Name))
                    {
                        string name = App.CurrentUser.Name.Split(' ')[0];
                        UserNameText.Text = name;
                    }
                    else
                    {
                        UserNameText.Text = "Гость";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateUserDisplay: {ex.Message}");
                if (UserNameText != null) UserNameText.Text = "Гость";
            }
        }

        private void LoadUserData()
        {
            try
            {
                if (!_isGuestMode && App.CurrentUser != null && App.CurrentUser.Id > 0)
                {
                    LoadUserRecipes();
                    LoadUserShoppingList();
                }
                else
                {
                    _userRecipes = new List<Recipes>();
                    _shoppingList = LoadGuestShoppingListFromSession();
                }

                DisplayRecipes();
                DisplayShoppingList();
                UpdateUIState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadUserData: {ex.Message}");
                // Инициализируем пустыми списками, чтобы страница не падала
                _userRecipes = new List<Recipes>();
                _shoppingList = new List<ShoppingItem>();
                DisplayRecipes();
                DisplayShoppingList();
                UpdateUIState();
            }
        }

        private void LoadUserRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var favoriteIds = context.FavoriteRecipes
                        .Where(f => f.UserId == App.CurrentUser.Id)
                        .Select(f => f.RecipeId)
                        .Take(20)
                        .ToList();

                    if (favoriteIds.Any())
                    {
                        _userRecipes = context.Recipes
                            .Where(r => favoriteIds.Contains(r.Id))
                            .ToList();

                        foreach (var recipe in _userRecipes)
                        {
                            context.Entry(recipe).Reference(r => r.MealCategories).Load();
                        }
                    }
                    else
                    {
                        _userRecipes = new List<Recipes>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadUserRecipes: {ex.Message}");
                _userRecipes = new List<Recipes>();
            }
        }

        private void LoadUserShoppingList()
        {
            try
            {
                _shoppingList = new List<ShoppingItem>();

                if (SessionManager.GuestTempData != null && App.CurrentUser != null)
                {
                    string key = $"ShoppingList_{App.CurrentUser.Id}";
                    if (SessionManager.GuestTempData.ContainsKey(key))
                    {
                        var list = SessionManager.GuestTempData[key] as List<ShoppingItem>;
                        if (list != null)
                        {
                            _shoppingList = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadUserShoppingList: {ex.Message}");
                _shoppingList = new List<ShoppingItem>();
            }
        }

        private List<ShoppingItem> LoadGuestShoppingListFromSession()
        {
            var list = new List<ShoppingItem>();

            try
            {
                if (SessionManager.GuestShoppingList != null && SessionManager.GuestShoppingList.Any())
                {
                    int id = 1;
                    foreach (var item in SessionManager.GuestShoppingList)
                    {
                        string category = GetCategoryForProduct(item.ProductName);

                        list.Add(new ShoppingItem
                        {
                            Id = id++,
                            Name = item.ProductName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Category = category,
                            IsFromRecipe = true,
                            ProductId = item.ProductId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadGuestShoppingListFromSession: {ex.Message}");
            }

            return list;
        }

        private string GetCategoryForProduct(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return "Прочее";

            string lowerName = productName.ToLower();

            if (new[] { "бекон", "курица", "мясо", "говядина", "свинина", "рыба", "креветки", "фарш", "индейка", "печень" }.Any(m => lowerName.Contains(m)))
                return "Мясо/Рыба";
            if (new[] { "сливки", "пармезан", "сыр", "молоко", "йогурт", "творог", "сметана", "кефир", "масло" }.Any(d => lowerName.Contains(d)))
                return "Молочные";
            if (new[] { "салат", "помидоры", "овощи", "фрукты", "яблоко", "банан", "картофель", "морковь", "лук", "чеснок", "капуста", "огурец" }.Any(v => lowerName.Contains(v)))
                return "Овощи/Фрукты";
            if (new[] { "хлеб", "макароны", "крупа", "рис", "гречка", "мука", "овсянка", "спагетти" }.Any(b => lowerName.Contains(b)))
                return "Бакалея";
            if (new[] { "замороженные", "мороженое", "пельмени", "вареники" }.Any(f => lowerName.Contains(f)))
                return "Замороженные";
            if (new[] { "вода", "сок", "напиток", "чай", "кофе", "лимонад" }.Any(d => lowerName.Contains(d)))
                return "Напитки";
            if (new[] { "соус", "кетчуп", "майонез", "горчица", "специи", "соль", "перец" }.Any(s => lowerName.Contains(s)))
                return "Соусы/Специи";

            return "Прочее";
        }

        private void DisplayRecipes()
        {
            try
            {
                RecipesPanel.Children.Clear();

                if (_userRecipes == null || !_userRecipes.Any())
                {
                    EmptyRecipesState.Visibility = Visibility.Visible;
                    RecipesPanel.Visibility = Visibility.Collapsed;
                    if (GenerateForSelectedButton != null) GenerateForSelectedButton.IsEnabled = false;
                    return;
                }

                EmptyRecipesState.Visibility = Visibility.Collapsed;
                RecipesPanel.Visibility = Visibility.Visible;
                if (GenerateForSelectedButton != null) GenerateForSelectedButton.IsEnabled = true;

                foreach (var recipe in _userRecipes)
                {
                    var border = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0")),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 0, 0, 12),
                        Margin = new Thickness(0, 0, 0, 12)
                    };

                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    var checkBox = new CheckBox
                    {
                        Margin = new Thickness(0, 0, 15, 0),
                        Tag = recipe.Id,
                        IsChecked = _selectedRecipes.ContainsKey(recipe.Id) && _selectedRecipes[recipe.Id],
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    checkBox.Checked += RecipeCheckBox_Changed;
                    checkBox.Unchecked += RecipeCheckBox_Changed;

                    var textStack = new StackPanel();
                    textStack.Children.Add(new TextBlock
                    {
                        Text = recipe.Title,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"))
                    });

                    if (recipe.MealCategories != null)
                    {
                        textStack.Children.Add(new TextBlock
                        {
                            Text = $"{recipe.MealCategories.Icon ?? "🍽️"} {recipe.MealCategories.Name}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }

                    stackPanel.Children.Add(checkBox);
                    stackPanel.Children.Add(textStack);
                    border.Child = stackPanel;

                    RecipesPanel.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в DisplayRecipes: {ex.Message}");
            }
        }

        private void DisplayShoppingList()
        {
            try
            {
                if (MeatFishPanel == null) return;

                MeatFishPanel.Children.Clear();
                DairyPanel.Children.Clear();
                VegFruitPanel.Children.Clear();
                OtherPanel.Children.Clear();

                if (_shoppingList == null || !_shoppingList.Any())
                {
                    return;
                }

                var meatFish = _shoppingList.Where(i => i.Category == "Мясо/Рыба");
                var dairy = _shoppingList.Where(i => i.Category == "Молочные");
                var vegFruit = _shoppingList.Where(i => i.Category == "Овощи/Фрукты");
                var bakery = _shoppingList.Where(i => i.Category == "Бакалея");
                var frozen = _shoppingList.Where(i => i.Category == "Замороженные");
                var drinks = _shoppingList.Where(i => i.Category == "Напитки");
                var sauces = _shoppingList.Where(i => i.Category == "Соусы/Специи");
                var other = _shoppingList.Where(i => i.Category == "Прочее");

                DisplayShoppingCategory(meatFish, MeatFishPanel, "🥩 МЯСО/РЫБА");
                DisplayShoppingCategory(dairy, DairyPanel, "🥛 МОЛОЧНЫЕ");
                DisplayShoppingCategory(vegFruit, VegFruitPanel, "🥦 ОВОЩИ/ФРУКТЫ");
                DisplayShoppingCategory(bakery, OtherPanel, "🍚 БАКАЛЕЯ");
                DisplayShoppingCategory(frozen, OtherPanel, "❄️ ЗАМОРОЖЕННЫЕ");
                DisplayShoppingCategory(drinks, OtherPanel, "🥤 НАПИТКИ");
                DisplayShoppingCategory(sauces, OtherPanel, "🧂 СОУСЫ/СПЕЦИИ");
                DisplayShoppingCategory(other, OtherPanel, "📦 ПРОЧЕЕ");

                UpdateItemsCount(_shoppingList.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в DisplayShoppingList: {ex.Message}");
            }
        }

        private void DisplayShoppingCategory(IEnumerable<ShoppingItem> items, StackPanel panel, string categoryName)
        {
            if (items == null || !items.Any() || panel == null) return;

            var categoryBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = categoryName,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                Margin = new Thickness(0, 0, 0, 15)
            });

            foreach (var item in items)
            {
                var productPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var checkBox = new CheckBox
                {
                    Margin = new Thickness(0, 0, 15, 0),
                    Tag = item.Id,
                    IsChecked = _selectedShoppingItems.ContainsKey(item.Id) && _selectedShoppingItems[item.Id],
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkBox.Checked += ShoppingItemCheckBox_Changed;
                checkBox.Unchecked += ShoppingItemCheckBox_Changed;

                productPanel.Children.Add(checkBox);

                var nameText = new TextBlock
                {
                    Text = item.Name,
                    FontSize = 14,
                    FontWeight = item.IsFromRecipe ? FontWeights.SemiBold : FontWeights.Medium,
                    Foreground = item.IsFromRecipe
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    Width = 120,
                    VerticalAlignment = VerticalAlignment.Center
                };

                productPanel.Children.Add(nameText);
                productPanel.Children.Add(new TextBlock
                {
                    Text = $"{item.Quantity:F1} {item.Unit}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                });

                stackPanel.Children.Add(productPanel);
            }

            categoryBorder.Child = stackPanel;
            panel.Children.Add(categoryBorder);
        }

        private void UpdateUIState()
        {
            try
            {
                bool hasProducts = _shoppingList != null && _shoppingList.Any();
                if (EmptyProductsState != null)
                    EmptyProductsState.Visibility = hasProducts ? Visibility.Collapsed : Visibility.Visible;
                if (ActionButtonsPanel != null)
                    ActionButtonsPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateUIState: {ex.Message}");
            }
        }

        private void UpdateItemsCount(int count)
        {
            if (ItemsCountText != null)
            {
                string word = GetCountWord(count);
                ItemsCountText.Text = $"({count} {word})";
            }
        }

        private string GetCountWord(int count)
        {
            if (count % 100 >= 11 && count % 100 <= 19) return "позиций";
            int lastDigit = count % 10;
            if (lastDigit == 1) return "позиция";
            if (lastDigit >= 2 && lastDigit <= 4) return "позиции";
            return "позиций";
        }

        private void ShowAddProductDialog()
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Добавление продукта",
                    Width = 400,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FC"))
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Название продукта:",
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                var nameTextBox = new TextBox
                {
                    FontSize = 14,
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stackPanel.Children.Add(nameTextBox);

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Категория:",
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                var categoryCombo = new ComboBox
                {
                    FontSize = 14,
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                categoryCombo.Items.Add("Мясо/Рыба");
                categoryCombo.Items.Add("Молочные");
                categoryCombo.Items.Add("Овощи/Фрукты");
                categoryCombo.Items.Add("Бакалея");
                categoryCombo.Items.Add("Замороженные");
                categoryCombo.Items.Add("Напитки");
                categoryCombo.Items.Add("Соусы/Специи");
                categoryCombo.Items.Add("Прочее");
                categoryCombo.SelectedIndex = 0;
                stackPanel.Children.Add(categoryCombo);

                var quantityPanel = new Grid();
                quantityPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                quantityPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                quantityPanel.Margin = new Thickness(0, 0, 0, 15);

                var quantityStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
                quantityStack.Children.Add(new TextBlock
                {
                    Text = "Количество:",
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 5)
                });
                var quantityTextBox = new TextBox
                {
                    FontSize = 14,
                    Padding = new Thickness(8),
                    Text = "1"
                };
                quantityStack.Children.Add(quantityTextBox);

                var unitStack = new StackPanel();
                unitStack.Children.Add(new TextBlock
                {
                    Text = "Единица измерения:",
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                var unitCombo = new ComboBox
                {
                    FontSize = 14,
                    Padding = new Thickness(8)
                };
                unitCombo.Items.Add("шт");
                unitCombo.Items.Add("г");
                unitCombo.Items.Add("кг");
                unitCombo.Items.Add("мл");
                unitCombo.Items.Add("л");
                unitCombo.Items.Add("уп");
                unitCombo.SelectedIndex = 0;
                unitStack.Children.Add(unitCombo);

                Grid.SetColumn(quantityStack, 0);
                Grid.SetColumn(unitStack, 1);
                quantityPanel.Children.Add(quantityStack);
                quantityPanel.Children.Add(unitStack);
                stackPanel.Children.Add(quantityPanel);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };

                var addButton = new Button
                {
                    Content = "➕ Добавить",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(20, 10, 20, 10),
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand,
                    Width = 120
                };
                addButton.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show("Введите название продукта", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!decimal.TryParse(quantityTextBox.Text, out decimal quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Введите корректное количество", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    AddManualProduct(
                        nameTextBox.Text.Trim(),
                        categoryCombo.SelectedItem.ToString(),
                        quantity,
                        unitCombo.SelectedItem.ToString()
                    );

                    dialog.Close();
                };

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                    BorderThickness = new Thickness(2),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(20, 10, 20, 10),
                    Cursor = Cursors.Hand,
                    Width = 120
                };
                cancelButton.Click += (s, args) => dialog.Close();

                buttonPanel.Children.Add(addButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);

                dialog.Content = new ScrollViewer
                {
                    Content = stackPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия диалога: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddManualProduct(string name, string category, decimal quantity, string unit)
        {
            try
            {
                var newItem = new ShoppingItem
                {
                    Id = _nextTempId++,
                    Name = name,
                    Quantity = quantity,
                    Unit = unit,
                    Category = category,
                    IsFromRecipe = false,
                    ProductId = null
                };

                if (_isGuestMode)
                {
                    var tempProduct = new TemporaryShoppingItem
                    {
                        ProductId = _nextTempId,
                        ProductName = name,
                        Quantity = quantity,
                        Unit = unit
                    };
                    SessionManager.GuestShoppingList.Add(tempProduct);
                    _shoppingList = LoadGuestShoppingListFromSession();
                }
                else
                {
                    string key = $"ShoppingList_{App.CurrentUser?.Id ?? 0}";
                    if (!SessionManager.GuestTempData.ContainsKey(key))
                    {
                        SessionManager.GuestTempData[key] = new List<ShoppingItem>();
                    }
                    var userList = SessionManager.GuestTempData[key] as List<ShoppingItem>;
                    if (userList != null)
                    {
                        userList.Add(newItem);
                        _shoppingList = userList;
                    }
                }

                DisplayShoppingList();
                UpdateUIState();

                MessageBox.Show($"Продукт «{name}» добавлен в список!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления продукта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateShoppingListFromSelectedRecipes()
        {
            try
            {
                var selectedRecipeIds = _selectedRecipes
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .ToList();

                if (!selectedRecipeIds.Any())
                {
                    MessageBox.Show("Выберите хотя бы один рецепт", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var context = new SmartKitchenEntities())
                {
                    var ingredients = context.Ingredients
                        .Where(i => selectedRecipeIds.Contains(i.RecipeId))
                        .ToList();

                    // Загружаем продукты для ингредиентов
                    var productIds = ingredients.Select(i => i.ProductId).Distinct().ToList();
                    var products = context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToDictionary(p => p.Id, p => p);

                    var newItems = new List<ShoppingItem>();

                    foreach (var ing in ingredients)
                    {
                        var product = products.ContainsKey(ing.ProductId) ? products[ing.ProductId] : null;
                        string productName = product?.Name ?? "Неизвестный продукт";
                        string unit = ing.Unit ?? product?.DefaultUnit ?? "г";
                        string category = GetCategoryForProduct(productName);

                        newItems.Add(new ShoppingItem
                        {
                            Id = _nextTempId++,
                            Name = productName,
                            Quantity = ing.Quantity,
                            Unit = unit,
                            Category = category,
                            IsFromRecipe = true,
                            ProductId = ing.ProductId
                        });
                    }

                    if (newItems.Any())
                    {
                        if (_isGuestMode)
                        {
                            foreach (var item in newItems)
                            {
                                SessionManager.AddToGuestShoppingList(
                                    item.ProductId ?? _nextTempId,
                                    item.Name,
                                    item.Quantity,
                                    item.Unit);
                            }
                            _shoppingList = LoadGuestShoppingListFromSession();
                        }
                        else
                        {
                            string key = $"ShoppingList_{App.CurrentUser?.Id ?? 0}";
                            if (!SessionManager.GuestTempData.ContainsKey(key))
                            {
                                SessionManager.GuestTempData[key] = new List<ShoppingItem>();
                            }
                            var userList = SessionManager.GuestTempData[key] as List<ShoppingItem>;
                            if (userList != null)
                            {
                                userList.AddRange(newItems);
                                _shoppingList = userList;
                            }
                        }

                        DisplayShoppingList();
                        UpdateUIState();

                        MessageBox.Show($"Добавлено {newItems.Count} продуктов в список покупок!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации списка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCheckedItems()
        {
            try
            {
                var checkedIds = _selectedShoppingItems
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .ToList();

                if (!checkedIds.Any()) return;

                if (_isGuestMode)
                {
                    var productIdsToRemove = _shoppingList
                        .Where(i => checkedIds.Contains(i.Id) && i.ProductId.HasValue)
                        .Select(i => i.ProductId.Value)
                        .ToList();

                    SessionManager.RemoveCheckedGuestItems(productIdsToRemove);
                    _shoppingList = LoadGuestShoppingListFromSession();
                }
                else
                {
                    string key = $"ShoppingList_{App.CurrentUser?.Id ?? 0}";
                    if (SessionManager.GuestTempData.ContainsKey(key))
                    {
                        var userList = SessionManager.GuestTempData[key] as List<ShoppingItem>;
                        if (userList != null)
                        {
                            userList.RemoveAll(i => checkedIds.Contains(i.Id));
                            _shoppingList = userList;
                        }
                    }
                }

                _selectedShoppingItems.Clear();
                DisplayShoppingList();
                UpdateUIState();

                MessageBox.Show("Отмеченные продукты удалены", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToClipboard()
        {
            try
            {
                if (_shoppingList == null || !_shoppingList.Any())
                {
                    MessageBox.Show("Нет продуктов для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var grouped = _shoppingList.GroupBy(i => i.Category);
                var lines = new List<string> { "СПИСОК ПОКУПОК", "", "" };

                foreach (var group in grouped.OrderBy(g => g.Key))
                {
                    lines.Add($"--- {group.Key} ---");
                    foreach (var item in group)
                    {
                        lines.Add($"  • {item.Name} — {item.Quantity:F1} {item.Unit}");
                    }
                    lines.Add("");
                }

                var text = string.Join(Environment.NewLine, lines);
                Clipboard.SetText(text);

                MessageBox.Show("Список скопирован в буфер обмена!", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики событий
        private void RecipeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int recipeId)
            {
                _selectedRecipes[recipeId] = checkBox.IsChecked == true;
            }
        }

        private void ShoppingItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int itemId)
            {
                _selectedShoppingItems[itemId] = checkBox.IsChecked == true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService != null && NavigationService.CanGoBack)
                    NavigationService.GoBack();
                else
                    NavigationService?.Navigate(new HomePage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка навигации назад: {ex.Message}");
                NavigationService?.Navigate(new HomePage());
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToClipboard();
        }

        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        private void GenerateForSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGuestMode)
            {
                GenerateShoppingListFromSelectedRecipes();
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAddProductDialog();
        }

        private void ClearCheckedButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Удалить отмеченные продукты из списка?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearCheckedItems();
            }
        }
    }
}