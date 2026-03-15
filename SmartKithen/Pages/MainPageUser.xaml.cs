using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class MainPageUser : Page
    {
        public MainPageUser()
        {
            InitializeComponent();
            Loaded += MainPageUser_Loaded;
        }

        private void MainPageUser_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
            LoadStatistics();
            LoadExpiryNotifications();
            LoadRecentRecipes();
            LoadRecommendations();
        }

        private void LoadUserData()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var user = context.Users
                        .FirstOrDefault(u => u.Id == SessionManager.CurrentUserId);

                    if (user == null) return;

                    var firstName = user.Name.Split(' ')[0];
                    UserNameText.Text = firstName;
                    WelcomeText.Text = $"Добро пожаловать на вашу умную кухню, {firstName}!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    TotalRecipesCount.Text = context.Recipes.Count().ToString();

                    // FavoriteRecipes и RecipeHistory раскомментируй после обновления EF модели
                    // FavoriteRecipesCount.Text = context.FavoriteRecipes
                    //     .Count(f => f.UserId == SessionManager.CurrentUserId).ToString();
                    // ViewedRecipesCount.Text = context.RecipeHistory
                    //     .Count(h => h.UserId == SessionManager.CurrentUserId).ToString();

                    FavoriteRecipesCount.Text = "0";
                    ViewedRecipesCount.Text = "0";

                    TrackedProductsCount.Text = context.FridgeItems
                        .Count(f => f.UserId == SessionManager.CurrentUserId).ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private void LoadExpiryNotifications()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var today = DateTime.Today;
                    var threeDaysLater = today.AddDays(3);

                    var expiring = context.FridgeItems
                        .Include("Products")
                        .Where(f => f.UserId == SessionManager.CurrentUserId
                                 && f.ExpiryDate <= threeDaysLater
                                 && f.ExpiryDate >= today)
                        .OrderBy(f => f.ExpiryDate)
                        .ToList();

                    NotificationsPanel.Children.Clear();

                    if (expiring.Count == 0)
                    {
                        NotificationsPanel.Children.Add(new TextBlock
                        {
                            Text = "Всё свежее, беспокоиться не о чем 👍",
                            FontSize = 13,
                            Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#666"))
                        });
                        return;
                    }

                    foreach (var item in expiring)
                    {
                        var daysLeft = (item.ExpiryDate - today).Days;

                        var text = daysLeft == 0
                            ? $"• {item.Products?.Name} истекает сегодня!"
                            : $"• {item.Products?.Name} испортится через {daysLeft} дн.";

                        NotificationsPanel.Children.Add(new TextBlock
                        {
                            Text = text,
                            FontSize = 13,
                            Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#666")),
                            Margin = new Thickness(0, 0, 0, 5)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}");
            }
        }

        private void LoadRecentRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Раскомментируй после обновления EF модели
                    // var recipes = context.RecipeHistory
                    //     .Include("Recipes")
                    //     .Where(h => h.UserId == SessionManager.CurrentUserId)
                    //     .OrderByDescending(h => h.ViewedAt)
                    //     .Select(h => h.Recipes)
                    //     .Distinct()
                    //     .Take(4)
                    //     .ToList();

                    // Пока показываем последние 4 рецепта из БД
                    var recipes = context.Recipes
                        .OrderByDescending(r => r.Id)
                        .Take(4)
                        .ToList();

                    RecentRecipesPanel.Children.Clear();

                    if (recipes.Count == 0)
                    {
                        RecentRecipesPanel.Children.Add(new TextBlock
                        {
                            Text = "Вы ещё не смотрели рецепты",
                            FontSize = 13,
                            Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#666"))
                        });
                        return;
                    }

                    foreach (var recipe in recipes)
                    {
                        RecentRecipesPanel.Children.Add(
                            CreateRecipeCard(recipe.Id, recipe.Title));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки недавних рецептов: {ex.Message}");
            }
        }

        private void LoadRecommendations()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // NEWID() через EF — берём все Id и рандомим на клиенте
                    var allIds = context.Recipes.Select(r => r.Id).ToList();

                    var random = new Random();
                    var randomIds = allIds
                        .OrderBy(x => random.Next())
                        .Take(3)
                        .ToList();

                    var recipes = context.Recipes
                        .Where(r => randomIds.Contains(r.Id))
                        .ToList();

                    RecommendationsPanel.Children.Clear();

                    foreach (var recipe in recipes)
                    {
                        RecommendationsPanel.Children.Add(
                            CreateRecipeCard(recipe.Id, recipe.Title));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рекомендаций: {ex.Message}");
            }
        }

        private void SwitchAccountBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из аккаунта?",
                "Смена аккаунта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            App.CurrentUser = null;
            NavigationService?.Navigate(new HomePage());
        }

        private Border CreateRecipeCard(int recipeId, string title)
        {
            var textBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            var card = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = textBlock
            };

            card.MouseLeftButtonDown += (s, e) =>
            {
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            };

            return card;
        }

        private void btnRecipes_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new MyRecipes());

        private void btnShoppingList_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new EmptyGroceryList());

        private void btnMyProducts_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new ListProducts());

        private void btnRandomRecipe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var allIds = context.Recipes.Select(r => r.Id).ToList();
                    if (allIds.Count == 0) return;

                    var randomId = allIds[new Random().Next(allIds.Count)];
                    NavigationService?.Navigate(new RecipeDetails(randomId));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void btnSettingUser_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new UserInformation());
    }
}