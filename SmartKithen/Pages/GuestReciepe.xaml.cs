using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using SmartKithen.AppData;

namespace SmartKithen.Pages
{
    public partial class GuestReciepe : Page
    {
        private int _recipeId;
        private Recipes _currentRecipe;

        // Конструктор без параметров
        public GuestReciepe()
        {
            InitializeComponent();
            Loaded += GuestReciepe_Loaded;
        }

        // Конструктор с параметром ID рецепта - ИСПРАВЛЕНИЕ
        public GuestReciepe(int recipeId) : this()
        {
            _recipeId = recipeId;
        }

        private void GuestReciepe_Loaded(object sender, RoutedEventArgs e)
        {
            if (_recipeId > 0)
            {
                LoadRecipeData(_recipeId);
            }
            else
            {
                ShowEmptyState();
            }
        }

        // Загрузка данных рецепта
        private void LoadRecipeData(int recipeId)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    _currentRecipe = context.Recipes
                        .Include(r => r.Categories)
                        .Include(r => r.Ingredients)
                        .Include(r => r.RecipeSteps)
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
                // Показываем информацию о рецепте в MessageBox
                ShowRecipeInfoInMessage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения рецепта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Показ информации о рецепте в MessageBox
        private void ShowRecipeInfoInMessage()
        {
            try
            {
                string ingredients = "";
                string steps = "";

                // Получаем ингредиенты
                using (var context = new SmartKitchenEntities())
                {
                    var recipeIngredients = context.Ingredients
                        .Where(i => i.RecipeId == _currentRecipe.Id)
                        .Include(i => i.Products)
                        .ToList();

                    if (recipeIngredients.Any())
                    {
                        ingredients = "\n\nИнгредиенты:\n" + string.Join("\n",
                            recipeIngredients.Select(i =>
                                $"• {i.Products?.Name ?? "Неизвестно"}: {i.Quantity} {i.Unit ?? "шт."}"));
                    }

                    var recipeSteps = context.RecipeSteps
                        .Where(s => s.RecipeId == _currentRecipe.Id)
                        .OrderBy(s => s.StepNumber)
                        .ToList();

                    if (recipeSteps.Any())
                    {
                        steps = "\n\nШаги приготовления:\n" + string.Join("\n",
                            recipeSteps.Select(s => $"{s.StepNumber}. {s.Description}"));
                    }
                }

                // Категория
                string category = "Без категории";
                if (_currentRecipe.CategoryId.HasValue)
                {
                    using (var context = new SmartKitchenEntities())
                    {
                        var cat = context.Categories.FirstOrDefault(c => c.Id == _currentRecipe.CategoryId.Value);
                        if (cat != null)
                        {
                            category = cat.Name;
                        }
                    }
                }

                MessageBox.Show(
                    $"🍽️ {_currentRecipe.Title}\n\n" +
                    $"📂 Категория: {category}\n" +
                    $"⏱️ Время приготовления: {_currentRecipe.CookingTime ?? 0} мин.\n" +
                    $"📝 Описание: {_currentRecipe.Description ?? "Нет описания"}\n" +
                    ingredients + steps,
                    "Просмотр рецепта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения информации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Показ пустого состояния
        private void ShowEmptyState()
        {
            // Используем существующий XAML для пустого состояния
        }

        // Кнопка "Назад"
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        // Кнопка "+ Добавить"
        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Эта функция доступна только зарегистрированным пользователям.\n" +
                          "Зарегистрируйтесь, чтобы добавлять рецепты!",
                          "Доступ ограничен",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        // Кнопка "Добавить рецепт"
        private void btnAddFirstRecipe_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление рецептов доступно только зарегистрированным пользователям.\n" +
                          "Создайте аккаунт, чтобы сохранять свои рецепты!",
                          "Регистрация",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            ShowRegistrationPrompt();
        }

        // Показать предложение регистрации
        private void ShowRegistrationPrompt()
        {
            var result = MessageBox.Show(
                "Хотите зарегистрироваться и получить полный доступ ко всем функциям?",
                "Регистрация",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                NavigationService?.Navigate(new Registration());
            }
        }

        // Кнопка "Войти в аккаунт" (добавьте в XAML Click="btnLogin_Click")
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }
    }
}