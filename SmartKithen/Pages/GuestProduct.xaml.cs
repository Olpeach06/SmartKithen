using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class GuestProduct : Page
    {
        // Контекст БД
        private SmartKitchenEntities db;

        public GuestProduct()
        {
            InitializeComponent();

            // Инициализация контекста БД8
            db = new SmartKitchenEntities();

            // Подписываемся на события
            Loaded += GuestProduct_Loaded;
            Unloaded += GuestProduct_Unloaded;
        }

        private void GuestProduct_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем, не авторизовался ли пользователь
            CheckUserStatus();
        }

        private void GuestProduct_Unloaded(object sender, RoutedEventArgs e)
        {
            // Освобождаем ресурсы при выгрузке страницы
            DisposeResources();
        }

        // Проверка статуса пользователя
        private void CheckUserStatus()
        {
            // Если пользователь авторизовался, перенаправляем на страницу пользователя
            if (App.CurrentUser != null && App.CurrentUser.Id > 0)
            {
                MessageBox.Show("Вы вошли в аккаунт! Переходим к вашим продуктам...",
                    "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.Navigate(new ListProducts());
            }
        }

        // Кнопка "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService?.GoBack();
            }
            else
            {
                NavigationService?.Navigate(new HomePage());
            }
        }

        // Кнопка "+ Добавить" в шапке
        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowGuestRestrictionMessage("добавления продуктов");
        }

        // Основная кнопка "Добавить продукт"
        private void btnAddFirstProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowGuestRestrictionMessage("добавления продуктов");
        }

        // Кнопка "Узнать больше"
        private void btnLearnMore_Click(object sender, RoutedEventArgs e)
        {
            ShowProductManagementInfo();
        }

        // Кнопка "Войти в аккаунт"
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        // Иконка обновления
        private void RefreshIcon_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Обновляем статус пользователя
                CheckUserStatus();

                // Показываем сообщение
                ShowToastMessage("Статус обновлён", "Проверка статуса пользователя выполнена.");
            }
        }

        // Показать меню для гостя (при клике на статус "Гость")
        private void ShowGuestMenu() 

        {
            var menu = new ContextMenu
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                FontSize = 14
            };

            // Стиль для пунктов меню
            var menuItemStyle = new Style(typeof(MenuItem));
            menuItemStyle.Setters.Add(new Setter(MenuItem.ForegroundProperty, System.Windows.Media.Brushes.DarkGreen));
            menuItemStyle.Setters.Add(new Setter(MenuItem.FontWeightProperty, FontWeights.Medium));
            menuItemStyle.Setters.Add(new Setter(MenuItem.PaddingProperty, new Thickness(15, 8, 15, 8)));

            var hoverTrigger = new Trigger
            {
                Property = MenuItem.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter(MenuItem.BackgroundProperty, System.Windows.Media.Brushes.LavenderBlush));
            hoverTrigger.Setters.Add(new Setter(MenuItem.ForegroundProperty, System.Windows.Media.Brushes.DarkMagenta));

            menuItemStyle.Triggers.Add(hoverTrigger);
            menu.ItemContainerStyle = menuItemStyle;

            // Пункт "Войти"
            var loginItem = new MenuItem
            {
                Header = "🔑 Войти в аккаунт"
            };
            loginItem.Click += (s, args) => NavigationService?.Navigate(new Authorization());

            // Пункт "Зарегистрироваться"
            var registerItem = new MenuItem
            {
                Header = "🆕 Зарегистрироваться"
            };
            registerItem.Click += (s, args) => NavigationService?.Navigate(new Registration());

            // Пункт "Посмотреть продукты"
            var viewProductsItem = new MenuItem
            {
                Header = "📦 Посмотреть все продукты"
            };
            viewProductsItem.Click += (s, args) => ShowAllProducts();

            // Пункт "Посмотреть рецепты"
            var viewRecipesItem = new MenuItem
            {
                Header = "🍳 Посмотреть рецепты"
            };
            viewRecipesItem.Click += (s, args) => ShowSampleRecipes();

            menu.Items.Add(loginItem);
            menu.Items.Add(registerItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(viewProductsItem);
            menu.Items.Add(viewRecipesItem);

            // Показываем меню рядом с элементом "Гость"
            menu.PlacementTarget = this;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // Обработчик клика по статусу "Гость" (через MouseLeftButtonUp в XAML)
        private void GuestStatus_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ShowGuestMenu();
            }
        }

        // Показать все продукты из БД
        private void ShowAllProducts()
        {
            try
            {
                var products = db.Products
                    .Include("Categories")
                    .OrderBy(p => p.Name)
                    .Take(15)
                    .Select(p => new
                    {
                        p.Name,
                        Category = p.Categories.Name,
                        p.DefaultUnit
                    })
                    .ToList();

                if (products.Any())
                {
                    string productList = "📦 Доступные продукты в системе:\n\n";
                    int counter = 1;

                    foreach (var product in products)
                    {
                        productList += $"{counter}. {product.Name}";
                        if (!string.IsNullOrEmpty(product.Category))
                            productList += $" ({product.Category})";
                        if (!string.IsNullOrEmpty(product.DefaultUnit))
                            productList += $" [{product.DefaultUnit}]";
                        productList += "\n";
                        counter++;
                    }

                    productList += $"\n📊 Всего в системе: {db.Products.Count()} продуктов\n";
                    productList += "Для управления продуктами требуется регистрация!";

                    MessageBox.Show(productList,
                        "Каталог продуктов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("В системе пока нет продуктов.",
                        "Каталог продуктов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить список продуктов: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Показать примеры рецептов из БД
        private void ShowSampleRecipes()
        {
            try
            {
                var recipes = db.Recipes
                    .OrderBy(r => r.Title)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Title,
                        r.Description,
                        IngredientCount = r.Ingredients.Count
                    })
                    .ToList();

                if (recipes.Any())
                {
                    string recipeList = "🍳 Примеры рецептов в системе:\n\n";
                    int counter = 1;

                    foreach (var recipe in recipes)
                    {
                        recipeList += $"{counter}. {recipe.Title}\n";
                        if (!string.IsNullOrEmpty(recipe.Description) && recipe.Description.Length > 100)
                            recipeList += $"   {recipe.Description.Substring(0, 100)}...\n";
                        else if (!string.IsNullOrEmpty(recipe.Description))
                            recipeList += $"   {recipe.Description}\n";
                        recipeList += $"   🥄 Ингредиентов: {recipe.IngredientCount}\n\n";
                        counter++;
                    }

                    recipeList += $"\n📊 Всего рецептов в системе: {db.Recipes.Count()}\n";
                    recipeList += "Для создания своих рецептов - зарегистрируйтесь!";

                    MessageBox.Show(recipeList,
                        "Каталог рецептов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить рецепты: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Показать сообщение об ограничениях гостевого режима
        private void ShowGuestRestrictionMessage(string action)
        {
            var result = MessageBox.Show(
                $"Функция {action} доступна только зарегистрированным пользователям.\n\n" +
                "✅ Возможности для пользователей:\n" +
                "   • Добавление/удаление продуктов\n" +
                "   • Отслеживание сроков годности\n" +
                "   • Уведомления о заканчивающихся продуктах\n" +
                "   • Автоматический список покупок\n" +
                "   • Расчет сроков годности\n\n" +
                "Хотите зарегистрироваться или войти?",
                "Доступ ограничен",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                ShowGuestMenu();
            }
        }

        // Показать информацию об управлении продуктами
        private void ShowProductManagementInfo()
        {
            string productInfo =
                "📦 Управление продуктами в Smart Kitchen:\n\n" +
                "1. 📝 Добавление продуктов:\n" +
                "   • Вручную через удобную форму\n" +
                "   • Импорт из любимых рецептов\n" +
                "   • Быстрый выбор из каталога\n\n" +
                "2. ⏰ Умное отслеживание:\n" +
                "   • Автоматические напоминания\n" +
                "   • Сортировка по сроку годности\n" +
                "   • Рекомендации по использованию\n\n" +
                "3. 📊 Аналитика и экономия:\n" +
                "   • Статистика использования\n" +
                "   • Рекомендации по закупкам\n" +
                "   • Экономия до 30% на продуктах\n\n" +
                "4. 🛒 Интеграции:\n" +
                "   • Автоматический список покупок\n" +
                "   • Синхронизация с рецептами\n" +
                "   • Умные подсказки";

            var result = MessageBox.Show(
                productInfo + "\n\nДля доступа к функциям требуется регистрация.\nОткрыть меню регистрации?",
                "Управление продуктами",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                ShowGuestMenu();
            }
        }

        // Показать тост-сообщение
        private void ShowToastMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Освобождение ресурсов
        private void DisposeResources()
        {
            try
            {
                if (db != null)
                {
                    db.Dispose();
                    db = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при освобождении ресурсов: {ex.Message}");
            }
        }
    }
}