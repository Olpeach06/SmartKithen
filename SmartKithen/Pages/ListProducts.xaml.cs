using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        private List<FridgeItems> _userFridgeItems;
        private List<ShoppingItem> _shoppingList;
        private Dictionary<int, bool> _selectedRecipes = new Dictionary<int, bool>();
        private Dictionary<int, bool> _selectedShoppingItems = new Dictionary<int, bool>();
        private bool _isGuestMode;

        // Класс для элемента списка покупок
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

            // ИСПРАВЛЕНО: Проверка на null без оператора ?.
            if (this.NavigationService != null)
            {
                this.NavigationService.LoadCompleted += NavigationService_LoadCompleted;
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
            _isGuestMode = SessionManager.IsGuestMode;
            UpdateUserDisplay();
            ConfigureUIMode();
            LoadUserData();
        }

        private void ConfigureUIMode()
        {
            if (_isGuestMode)
            {
                // Для гостя скрываем секцию с рецептами
                RecipesSection.Visibility = Visibility.Collapsed;

                // Изменяем текст подсказки для гостя
                EmptyProductsTitle.Text = "Список покупок пуст";
                EmptyProductsMessage.Text = "Добавьте продукты из рецептов или нажмите «Добавить продукт»";
            }
            else
            {
                // Для авторизованного показываем секцию с рецептами
                RecipesSection.Visibility = Visibility.Visible;
                EmptyProductsMessage.Text = "Выберите рецепты и нажмите «Добавить недостающие продукты»";
            }
        }

        private void UpdateUserDisplay()
        {
            if (UserNameText != null)
            {
                if (!_isGuestMode && App.CurrentUser != null)
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

        private void LoadUserData()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    if (!_isGuestMode)
                    {
                        // Загружаем избранные рецепты пользователя
                        _userRecipes = context.FavoriteRecipes
                            .Where(f => f.UserId == App.CurrentUser.Id)
                            .Select(f => f.Recipes)
                            .Take(20)
                            .ToList();

                        // Загружаем продукты из холодильника
                        _userFridgeItems = context.FridgeItems
                            .Where(f => f.UserId == App.CurrentUser.Id)
                            .Include("Products")
                            .Include("Products.Categories")
                            .OrderBy(f => f.ExpiryDate)
                            .ToList();

                        // Загружаем список покупок из БД
                        _shoppingList = LoadShoppingListFromDb(context);

                        DisplayRecipes();
                    }
                    else
                    {
                        // Для гостя - загружаем данные из SessionManager
                        _userRecipes = new List<Recipes>();
                        _userFridgeItems = new List<FridgeItems>();
                        _shoppingList = LoadGuestShoppingListFromSession();
                    }

                    DisplayShoppingList();
                    UpdateUIState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ShoppingItem> LoadShoppingListFromDb(SmartKitchenEntities context)
        {
            var list = new List<ShoppingItem>();

            foreach (var item in _userFridgeItems)
            {
                string categoryName = GetCategoryFromDb(item.Products.Categories?.Name);

                list.Add(new ShoppingItem
                {
                    Id = item.Id,
                    Name = item.Products.Name,
                    Quantity = item.Quantity,
                    Unit = item.Products.DefaultUnit,
                    Category = categoryName,
                    IsFromRecipe = false,
                    ProductId = item.ProductId
                });
            }

            return list;
        }

        private string GetCategoryFromDb(string dbCategory)
        {
            if (string.IsNullOrEmpty(dbCategory)) return "Прочее";

            // Группируем категории из БД в наши основные группы
            if (dbCategory.Contains("Мясо") || dbCategory.Contains("Рыба") ||
                dbCategory.Contains("Морепродукты") || dbCategory.Contains("Птица"))
                return "Мясо/Рыба";

            if (dbCategory.Contains("Молочные") || dbCategory.Contains("Сыр") ||
                dbCategory.Contains("Йогурт") || dbCategory.Contains("Сливки"))
                return "Молочные";

            if (dbCategory.Contains("Овощи") || dbCategory.Contains("Фрукты") ||
                dbCategory.Contains("Зелень") || dbCategory.Contains("Грибы"))
                return "Овощи/Фрукты";

            if (dbCategory.Contains("Крупы") || dbCategory.Contains("Макароны") ||
                dbCategory.Contains("Хлеб") || dbCategory.Contains("Бакалея"))
                return "Бакалея";

            if (dbCategory.Contains("Замороженные"))
                return "Замороженные";

            if (dbCategory.Contains("Напитки"))
                return "Напитки";

            if (dbCategory.Contains("Соусы") || dbCategory.Contains("Специи"))
                return "Соусы/Специи";

            return "Прочее";
        }

        private List<ShoppingItem> LoadGuestShoppingListFromSession()
        {
            var list = new List<ShoppingItem>();

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

            return list;
        }

        private string GetCategoryForProduct(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return "Прочее";

            // База данных категорий по названиям продуктов
            string[] meatFish = { "бекон", "курица", "мясо", "говядина", "свинина", "рыба", "креветки", "фарш", "индейка", "печень" };
            string[] dairy = { "сливки", "пармезан", "сыр", "молоко", "йогурт", "творог", "сметана", "кефир", "масло" };
            string[] vegFruit = { "салат", "помидоры", "овощи", "фрукты", "яблоко", "банан", "картофель", "морковь", "лук", "чеснок", "капуста", "огурец" };
            string[] bakery = { "хлеб", "макароны", "крупа", "рис", "гречка", "мука", "овсянка", "спагетти" };
            string[] frozen = { "замороженные", "мороженое", "пельмени", "вареники" };
            string[] drinks = { "вода", "сок", "напиток", "чай", "кофе", "лимонад" };
            string[] sauces = { "соус", "кетчуп", "майонез", "горчица", "специи", "соль", "перец" };

            string lowerName = productName.ToLower();

            if (meatFish.Any(m => lowerName.Contains(m))) return "Мясо/Рыба";
            if (dairy.Any(d => lowerName.Contains(d))) return "Молочные";
            if (vegFruit.Any(v => lowerName.Contains(v))) return "Овощи/Фрукты";
            if (bakery.Any(b => lowerName.Contains(b))) return "Бакалея";
            if (frozen.Any(f => lowerName.Contains(f))) return "Замороженные";
            if (drinks.Any(d => lowerName.Contains(d))) return "Напитки";
            if (sauces.Any(s => lowerName.Contains(s))) return "Соусы/Специи";

            return "Прочее";
        }

        private void DisplayRecipes()
        {
            RecipesPanel.Children.Clear();

            if (_userRecipes == null || !_userRecipes.Any())
            {
                EmptyRecipesState.Visibility = Visibility.Visible;
                RecipesPanel.Visibility = Visibility.Collapsed;
                GenerateForSelectedButton.IsEnabled = false;
                return;
            }

            EmptyRecipesState.Visibility = Visibility.Collapsed;
            RecipesPanel.Visibility = Visibility.Visible;
            GenerateForSelectedButton.IsEnabled = true;

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

                if (!string.IsNullOrEmpty(recipe.Description))
                {
                    textStack.Children.Add(new TextBlock
                    {
                        Text = recipe.Description,
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888")),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }

                stackPanel.Children.Add(checkBox);
                stackPanel.Children.Add(textStack);
                border.Child = stackPanel;

                RecipesPanel.Children.Add(border);
            }
        }

        private void DisplayShoppingList()
        {
            // Очищаем панели с продуктами
            MeatFishPanel.Children.Clear();
            DairyPanel.Children.Clear();
            VegFruitPanel.Children.Clear();
            OtherPanel.Children.Clear();

            if (_shoppingList == null || !_shoppingList.Any())
            {
                return;
            }

            // Группируем по категориям
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

        private void DisplayShoppingCategory(IEnumerable<ShoppingItem> items, StackPanel panel, string categoryName)
        {
            if (items == null || !items.Any()) return;

            var categoryBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackPanel = new StackPanel();

            // Заголовок категории
            stackPanel.Children.Add(new TextBlock
            {
                Text = categoryName,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Продукты
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
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                    Width = 120,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Если продукт из рецепта, выделяем его
                if (item.IsFromRecipe)
                {
                    nameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                    nameText.FontWeight = FontWeights.SemiBold;
                }

                productPanel.Children.Add(nameText);
                productPanel.Children.Add(new TextBlock
                {
                    Text = $"{item.Quantity} {item.Unit}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                    VerticalAlignment = VerticalAlignment.Center
                });

                stackPanel.Children.Add(productPanel);
            }

            categoryBorder.Child = stackPanel;
            panel.Children.Add(categoryBorder);
        }

        private void UpdateUIState()
        {
            bool hasProducts = _shoppingList != null && _shoppingList.Any();
            EmptyProductsState.Visibility = hasProducts ? Visibility.Collapsed : Visibility.Visible;
            ActionButtonsPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;

            // Показываем/скрываем панели категорий
            MeatFishPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;
            DairyPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;
            VegFruitPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;
            OtherPanel.Visibility = hasProducts ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateItemsCount(int count)
        {
            if (ItemsCountText != null)
            {
                string word = GetCountWord(count, "позиция", "позиции", "позиций");
                ItemsCountText.Text = $"({count} {word})";
            }
        }

        private string GetCountWord(int count, string one, string two, string five)
        {
            if (count % 100 >= 11 && count % 100 <= 19)
                return five;

            int lastDigit = count % 10;
            if (lastDigit == 1)
                return one;
            else if (lastDigit == 2 || lastDigit == 3 || lastDigit == 4)
                return two;
            else
                return five;
        }

        private void ShowAddProductDialog()
        {
            // ИСПРАВЛЕНО: Убираем дублирование метода (в коде было два раза)
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

            // Название продукта
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

            // Категория
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

            // Количество и единицы измерения
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

            // Кнопки
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

        private void AddManualProduct(string name, string category, decimal quantity, string unit)
        {
            try
            {
                var tempId = new Random().Next(1000, 9999);

                if (_isGuestMode)
                {
                    // Для гостя - создаем временный продукт
                    var tempProduct = new TemporaryShoppingItem
                    {
                        ProductId = tempId,
                        ProductName = name,
                        Quantity = quantity,
                        Unit = unit
                    };

                    SessionManager.GuestShoppingList.Add(tempProduct);

                    // Перезагружаем список
                    _shoppingList = LoadGuestShoppingListFromSession();
                }
                else
                {
                    // Для авторизованного - создаем запись в БД
                    using (var context = new SmartKitchenEntities())
                    {
                        // Ищем существующий продукт по названию
                        var existingProduct = context.Products
                            .FirstOrDefault(p => p.Name.ToLower() == name.ToLower());

                        int productId;

                        if (existingProduct != null)
                        {
                            productId = existingProduct.Id;
                        }
                        else
                        {
                            // Если продукта нет, создаем новый
                            var newProduct = new Products
                            {
                                Name = name,
                                CategoryId = GetCategoryIdFromName(category),
                                DefaultUnit = unit
                            };
                            context.Products.Add(newProduct);
                            context.SaveChanges();
                            productId = newProduct.Id;
                        }

                        // Добавляем в холодильник
                        var fridgeItem = new FridgeItems
                        {
                            UserId = App.CurrentUser.Id,
                            ProductId = productId,
                            Quantity = quantity,
                            ExpiryDate = DateTime.Today.AddMonths(1)
                        };
                        context.FridgeItems.Add(fridgeItem);
                        context.SaveChanges();

                        // Перезагружаем данные
                        _userFridgeItems = context.FridgeItems
                            .Where(f => f.UserId == App.CurrentUser.Id)
                            .Include("Products")
                            .Include("Products.Categories")
                            .ToList();

                        _shoppingList = LoadShoppingListFromDb(context);
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

        private int GetCategoryIdFromName(string categoryName)
        {
            // Здесь нужно получить ID категории из БД по названию
            // Для демо возвращаем заглушку
            return 1; // В реальном проекте нужно заменить на поиск в БД
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
                    // Получаем ингредиенты из выбранных рецептов
                    var ingredients = context.Ingredients
                        .Where(i => selectedRecipeIds.Contains(i.RecipeId))
                        .Include("Products")
                        .Include("Products.Categories")
                        .ToList();

                    // Группируем по продуктам
                    var neededItems = ingredients
                        .GroupBy(i => i.ProductId)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            Product = g.First().Products,
                            TotalNeeded = g.Sum(i => i.Quantity),
                            Unit = g.First().Unit
                        })
                        .ToList();

                    // Проверяем, что есть в холодильнике
                    var missingItems = new List<ShoppingItem>();

                    foreach (var needed in neededItems)
                    {
                        var inFridge = _userFridgeItems?
                            .FirstOrDefault(f => f.ProductId == needed.ProductId);

                        decimal available = inFridge?.Quantity ?? 0;
                        decimal missing = needed.TotalNeeded - available;

                        if (missing > 0)
                        {
                            string category = GetCategoryFromDb(needed.Product.Categories?.Name);

                            missingItems.Add(new ShoppingItem
                            {
                                Id = -DateTime.Now.Millisecond - missingItems.Count,
                                Name = needed.Product.Name,
                                Quantity = missing,
                                Unit = needed.Unit,
                                Category = category,
                                IsFromRecipe = true,
                                ProductId = needed.ProductId
                            });
                        }
                    }

                    if (missingItems.Any())
                    {
                        // Добавляем недостающие продукты в список покупок
                        _shoppingList.AddRange(missingItems);

                        // Для авторизованных сохраняем в БД
                        if (!_isGuestMode)
                        {
                            foreach (var item in missingItems)
                            {
                                var shoppingItem = new FridgeItems
                                {
                                    UserId = App.CurrentUser.Id,
                                    ProductId = item.ProductId.Value,
                                    Quantity = item.Quantity,
                                    ExpiryDate = DateTime.Today.AddDays(7)
                                };
                                context.FridgeItems.Add(shoppingItem);
                            }
                            context.SaveChanges();

                            // Перезагружаем список из БД
                            _shoppingList = LoadShoppingListFromDb(context);
                        }

                        DisplayShoppingList();
                        UpdateUIState();

                        MessageBox.Show($"Добавлено {missingItems.Count} недостающих продуктов!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Все необходимые продукты уже есть в холодильнике!", "Информация",
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
            var checkedIds = _selectedShoppingItems
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (checkedIds.Any())
            {
                if (_isGuestMode)
                {
                    // Для гостя - удаляем из SessionManager
                    var itemsToRemove = _shoppingList
                        .Where(i => checkedIds.Contains(i.Id))
                        .Select(i => i.ProductId)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();

                    SessionManager.RemoveCheckedGuestItems(itemsToRemove);

                    // Перезагружаем список
                    _shoppingList = LoadGuestShoppingListFromSession();
                }
                else
                {
                    // Для авторизованного - удаляем из БД
                    try
                    {
                        using (var context = new SmartKitchenEntities())
                        {
                            var itemsToRemove = context.FridgeItems
                                .Where(f => checkedIds.Contains(f.Id));
                            context.FridgeItems.RemoveRange(itemsToRemove);
                            context.SaveChanges();
                        }

                        _shoppingList.RemoveAll(i => checkedIds.Contains(i.Id));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                _selectedShoppingItems.Clear();
                DisplayShoppingList();
                UpdateUIState();

                MessageBox.Show("Отмеченные продукты удалены", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportToClipboard()
        {
            try
            {
                var items = new List<string>();

                void CollectFromPanel(StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is Border border && border.Child is StackPanel categoryStack)
                        {
                            foreach (var element in categoryStack.Children)
                            {
                                if (element is StackPanel productPanel && productPanel.Children.Count >= 3)
                                {
                                    var name = (productPanel.Children[1] as TextBlock)?.Text;
                                    var quantity = (productPanel.Children[2] as TextBlock)?.Text;
                                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(quantity))
                                    {
                                        items.Add($"• {name} - {quantity}");
                                    }
                                }
                            }
                        }
                    }
                }

                CollectFromPanel(MeatFishPanel);
                CollectFromPanel(DairyPanel);
                CollectFromPanel(VegFruitPanel);
                CollectFromPanel(OtherPanel);

                if (items.Any())
                {
                    var text = "Список покупок:\n\n" + string.Join("\n", items);
                    Clipboard.SetText(text);
                    MessageBox.Show("Список скопирован в буфер обмена!", "Экспорт",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Нет продуктов для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
            NavigationService?.GoBack();
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