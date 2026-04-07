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
                    // Количество рецептов пользователя
                    var userRecipesCount = context.Recipes
                        .Count(r => r.UserId == SessionManager.CurrentUserId);
                    TotalRecipesCount.Text = userRecipesCount.ToString();

                    // Количество избранных рецептов
                    var favoriteCount = context.FavoriteRecipes
                        .Count(f => f.UserId == SessionManager.CurrentUserId);
                    FavoriteRecipesCount.Text = favoriteCount.ToString();

                    // Количество просмотренных рецептов
                    var viewedCount = context.RecipeHistory
                        .Count(h => h.UserId == SessionManager.CurrentUserId);
                    ViewedRecipesCount.Text = viewedCount.ToString();

                    // Количество продуктов в холодильнике пользователя
                    var trackedProductsCount = context.FridgeItems
                        .Count(f => f.UserId == SessionManager.CurrentUserId);
                    TrackedProductsCount.Text = trackedProductsCount.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}");
                // Устанавливаем значения по умолчанию в случае ошибки
                TotalRecipesCount.Text = "0";
                FavoriteRecipesCount.Text = "0";
                ViewedRecipesCount.Text = "0";
                TrackedProductsCount.Text = "0";
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

                    // Загружаем продукты с истекающим сроком годности
                    var expiringItems = context.FridgeItems
                        .Where(f => f.UserId == SessionManager.CurrentUserId
                                 && f.ExpiryDate <= threeDaysLater
                                 && f.ExpiryDate >= today)
                        .OrderBy(f => f.ExpiryDate)
                        .ToList();

                    // Загружаем названия продуктов отдельно (если нужно)
                    var productIds = expiringItems.Select(f => f.ProductId).Distinct().ToList();
                    var products = context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToDictionary(p => p.Id, p => p.Name);

                    NotificationsPanel.Children.Clear();

                    if (expiringItems.Count == 0)
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

                    foreach (var item in expiringItems)
                    {
                        var daysLeft = (item.ExpiryDate - today).Days;
                        var productName = products.ContainsKey(item.ProductId)
                            ? products[item.ProductId]
                            : $"Продукт #{item.ProductId}";

                        var text = daysLeft == 0
                            ? $"• {productName} истекает сегодня!"
                            : daysLeft == 1
                                ? $"• {productName} испортится завтра!"
                                : $"• {productName} испортится через {daysLeft} дн.";

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
                NotificationsPanel.Children.Clear();
                NotificationsPanel.Children.Add(new TextBlock
                {
                    Text = "Не удалось загрузить уведомления",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999"))
                });
            }
        }

        private void LoadRecentRecipes()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Загружаем историю просмотров пользователя
                    var recentHistory = context.RecipeHistory
                        .Where(h => h.UserId == SessionManager.CurrentUserId)
                        .OrderByDescending(h => h.ViewedAt)
                        .Take(4)
                        .ToList();

                    var recipeIds = recentHistory.Select(h => h.RecipeId).Distinct().ToList();

                    var recipes = context.Recipes
                        .Where(r => recipeIds.Contains(r.Id))
                        .ToDictionary(r => r.Id, r => r.Title);

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

                    // Добавляем карточки в порядке просмотра (сначала самые новые)
                    foreach (var history in recentHistory)
                    {
                        if (recipes.ContainsKey(history.RecipeId))
                        {
                            RecentRecipesPanel.Children.Add(
                                CreateRecipeCard(history.RecipeId, recipes[history.RecipeId]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки недавних рецептов: {ex.Message}");
                RecentRecipesPanel.Children.Clear();
                RecentRecipesPanel.Children.Add(new TextBlock
                {
                    Text = "Не удалось загрузить недавние рецепты",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999"))
                });
            }
        }

        private void LoadRecommendations()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Получаем ID продуктов пользователя
                    var userProductIds = context.FridgeItems
                        .Where(f => f.UserId == SessionManager.CurrentUserId)
                        .Select(f => f.ProductId)
                        .Distinct()
                        .ToList();

                    // Если у пользователя есть продукты, ищем рецепты с этими продуктами
                    List<Recipes> recommendedRecipes = new List<Recipes>();

                    if (userProductIds.Any())
                    {
                        // Находим рецепты, которые используют продукты пользователя
                        var recipeIdsWithUserProducts = context.Ingredients
                            .Where(i => userProductIds.Contains(i.ProductId))
                            .Select(i => i.RecipeId)
                            .Distinct()
                            .ToList();

                        recommendedRecipes = context.Recipes
                            .Where(r => recipeIdsWithUserProducts.Contains(r.Id))
                            .OrderBy(r => Guid.NewGuid())
                            .Take(3)
                            .ToList();
                    }

                    // Если рецептов с продуктами пользователя недостаточно, добавляем случайные
                    if (recommendedRecipes.Count < 3)
                    {
                        var existingIds = recommendedRecipes.Select(r => r.Id).ToList();
                        var additionalRecipes = context.Recipes
                            .Where(r => !existingIds.Contains(r.Id))
                            .OrderBy(r => Guid.NewGuid())
                            .Take(3 - recommendedRecipes.Count)
                            .ToList();

                        recommendedRecipes.AddRange(additionalRecipes);
                    }

                    RecommendationsPanel.Children.Clear();

                    if (recommendedRecipes.Count == 0)
                    {
                        RecommendationsPanel.Children.Add(new TextBlock
                        {
                            Text = "Нет рекомендаций",
                            FontSize = 13,
                            Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString("#666"))
                        });
                        return;
                    }

                    foreach (var recipe in recommendedRecipes)
                    {
                        RecommendationsPanel.Children.Add(
                            CreateRecipeCard(recipe.Id, recipe.Title));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рекомендаций: {ex.Message}");
                RecommendationsPanel.Children.Clear();
                RecommendationsPanel.Children.Add(new TextBlock
                {
                    Text = "Не удалось загрузить рекомендации",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999"))
                });
            }
        }

        private void SwitchAccountBorder_Click(object sender, MouseButtonEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из аккаунта?",
                "Смена аккаунта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Очищаем данные пользователя
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
            NavigationService?.Navigate(new GuestReciepe());

        private void btnShoppingList_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new ListProducts());

        private void btnMyProducts_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new EmptyGroceryList());

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

        private void btnShowAllRecipes_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new RecipeHistoryPage());
        }

        private void btnSettingUser_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new UserInformation());

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }
    }
}