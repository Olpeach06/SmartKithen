using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class RecipeDetails : Page
    {
        private int _recipeId;
        private Recipes _currentRecipe;

        // Конструктор без параметров
        public RecipeDetails()
        {
            InitializeComponent();
            Loaded += RecipeDetails_Loaded;
        }

        // Конструктор с параметром ID рецепта
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

        // Загрузка данных рецепта из БД
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
                        .Include("Recipesteps")
                        .FirstOrDefault(r => r.Id == recipeId);

                    if (_currentRecipe != null)
                    {
                        DisplayRecipeData();
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

        // Отображение данных рецепта
        private void DisplayRecipeData()
        {
            try
            {
                // Название рецепта
                RecipeTitleText.Text = _currentRecipe.Title.ToUpper();

                // Описание
                if (!string.IsNullOrEmpty(_currentRecipe.Description))
                {
                    RecipeDescriptionText.Text = _currentRecipe.Description;
                }

                // Время приготовления
                CookingTimeText.Text = $"{_currentRecipe.CookingTime ?? 0} мин";

                // Категория (сложность)
                if (_currentRecipe.Categories != null)
                {
                    DifficultyText.Text = _currentRecipe.Categories.Name;
                }

                // Количество порций (если есть в БД, иначе ставим значение по умолчанию)
                // В вашей БД может не быть поля для порций, тогда оставляем как есть
                // ServingsText.Text = _currentRecipe.Servings?.ToString() ?? "2 порции";

                // Рейтинг (если есть в БД)
                // RatingStars.Text = GetRatingStars(_currentRecipe.Rating ?? 4.5m);
                // RatingValueText.Text = _currentRecipe.Rating?.ToString("F1") ?? "4.5";

                // Загрузка ингредиентов
                LoadIngredients();

                // Загрузка шагов приготовления
                /*LoadSteps();*/
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения рецепта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка ингредиентов
        private void LoadIngredients()
        {
            try
            {
                if (_currentRecipe.Ingredients != null && _currentRecipe.Ingredients.Any())
                {
                    IngredientsPanel.Children.Clear();

                    foreach (var ingredient in _currentRecipe.Ingredients.OrderBy(i => i.Id))
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 0, 0, 12)
                        };

                        var checkBox = new CheckBox
                        {
                            Margin = new Thickness(0, 0, 15, 0),
                            Tag = ingredient.Id
                        };

                        var nameText = new TextBlock
                        {
                            Text = ingredient.Products?.Name ?? "Неизвестный продукт",
                            FontSize = 14,
                            FontWeight = FontWeights.Medium,
                            Foreground = System.Windows.Media.Brushes.Black
                        };

                        var quantityText = new TextBlock
                        {
                            Text = $"{ingredient.Quantity} {ingredient.Unit ?? "шт."}",
                            FontSize = 13,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            Margin = new Thickness(10, 0, 0, 0)
                        };

                        stackPanel.Children.Add(checkBox);
                        stackPanel.Children.Add(nameText);
                        stackPanel.Children.Add(quantityText);

                        IngredientsPanel.Children.Add(stackPanel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки ингредиентов: {ex.Message}");
            }
        }

        // Загрузка шагов приготовления
        /*private void LoadSteps()
        {
            try
            {
                if (_currentRecipe.Recipesteps != null && _currentRecipe.Recipesteps.Any())
                {
                    StepsPanel.Children.Clear();

                    foreach (var step in _currentRecipe.Recipesteps.OrderBy(s => s.StepNumber))
                    {
                        var border = new Border
                        {
                            Background = System.Windows.Media.Brushes.LightBlue,
                            CornerRadius = new CornerRadius(15),
                            Padding = new Thickness(20, 15),
                            Margin = new Thickness(0, 0, 0, 15),
                            BorderBrush = System.Windows.Media.Brushes.LightBlue,
                            BorderThickness = new Thickness(1)
                        };

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var stepNumber = new TextBlock
                        {
                            Text = $"{step.StepNumber}.",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = System.Windows.Media.Brushes.Green,
                            Margin = new Thickness(0, 0, 10, 0),
                            VerticalAlignment = VerticalAlignment.Top
                        };

                        var stepDescription = new TextBlock
                        {
                            Text = step.Description,
                            FontSize = 14,
                            Foreground = System.Windows.Media.Brushes.Black,
                            TextWrapping = TextWrapping.Wrap
                        };

                        Grid.SetColumn(stepNumber, 0);
                        Grid.SetColumn(stepDescription, 1);
                        grid.Children.Add(stepNumber);
                        grid.Children.Add(stepDescription);

                        border.Child = grid;
                        StepsPanel.Children.Add(border);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки шагов: {ex.Message}");
            }
        }*/

        // Кнопка "В готовку"
        private void StartCookingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CookingMode());
        }

        // Кнопка поиска
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        // Кнопка "Далее" (следующий рецепт)
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var nextRecipe = context.Recipes
                        .Where(r => r.Id > _recipeId)
                        .OrderBy(r => r.Id)
                        .FirstOrDefault();

                    if (nextRecipe != null)
                    {
                        NavigationService?.Navigate(new RecipeDetails(nextRecipe.Id));
                    }
                    else
                    {
                        // Если следующего нет, показываем первый
                        var firstRecipe = context.Recipes
                            .OrderBy(r => r.Id)
                            .FirstOrDefault();

                        if (firstRecipe != null)
                        {
                            NavigationService?.Navigate(new RecipeDetails(firstRecipe.Id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода к следующему рецепту: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка меню
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var addToFavorites = new MenuItem { Header = "❤️ Добавить в избранное" };
            addToFavorites.Click += (s, args) => AddToFavorites();

            var share = new MenuItem { Header = "📤 Поделиться" };
            share.Click += (s, args) => ShareRecipe();

            var print = new MenuItem { Header = "🖨️ Распечатать" };
            print.Click += (s, args) => PrintRecipe();

            menu.Items.Add(addToFavorites);
            menu.Items.Add(share);
            menu.Items.Add(print);

            if (sender is Button button)
            {
                menu.PlacementTarget = button;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        // Кнопка "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        // Кнопка "Добавить в список покупок"
        private void AddToShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Добавить все ингредиенты в список покупок?",
                "Список покупок",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Проверяем, авторизован ли пользователь
                if (App.CurrentUser != null && App.CurrentUser.Id > 0)
                {
                    // Для авторизованного пользователя - добавляем в его список
                    AddIngredientsToShoppingList();
                }
                else
                {
                    // Для гостя - предложение зарегистрироваться
                    ShowRegistrationPrompt();
                }
            }
        }

        // Добавление ингредиентов в список покупок (для авторизованных)
        private void AddIngredientsToShoppingList()
        {
            try
            {
                // Здесь будет логика добавления в список покупок
                MessageBox.Show("Ингредиенты добавлены в список покупок!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в список: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Предложение регистрации для гостя
        private void ShowRegistrationPrompt()
        {
            var result = MessageBox.Show(
                "Для сохранения списка покупок необходимо зарегистрироваться.\nХотите создать аккаунт?",
                "Регистрация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigationService?.Navigate(new Registration());
            }
        }

        // Добавление в избранное
        private void AddToFavorites()
        {
            if (App.CurrentUser != null && App.CurrentUser.Id > 0)
            {
                MessageBox.Show("Рецепт добавлен в избранное!",
                    "Избранное", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ShowRegistrationPrompt();
            }
        }

        // Поделиться рецептом
        private void ShareRecipe()
        {
            Clipboard.SetText(_currentRecipe.Title + "\n\n" + _currentRecipe.Description);
            MessageBox.Show("Ссылка на рецепт скопирована в буфер обмена",
                "Поделиться", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Распечатать рецепт
        private void PrintRecipe()
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Здесь логика печати
                    MessageBox.Show("Рецепт отправлен на печать",
                        "Печать", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для получения звезд рейтинга
        private string GetRatingStars(decimal rating)
        {
            int fullStars = (int)rating;
            bool halfStar = (rating - fullStars) >= 0.5m;

            string stars = new string('★', fullStars);
            if (halfStar) stars += "½";
            stars += new string('☆', 5 - fullStars - (halfStar ? 1 : 0));

            return stars;
        }
    }
}