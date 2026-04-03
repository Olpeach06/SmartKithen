using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects; // Добавляем для DropShadowEffect

namespace SmartKithen.Pages
{
    public partial class GuestReciepe : Page
    {
        private List<Recipes> _myRecipes;
        private List<Recipes> _favoriteRecipes;
        private List<GuestRecipeData> _guestRecipes;
        private bool _isGuestMode;
        private int _currentTab = 0;

        public GuestReciepe()
        {
            InitializeComponent();
            Loaded += GuestReciepe_Loaded;
        }

        private void GuestReciepe_Loaded(object sender, RoutedEventArgs e)
        {
            _isGuestMode = SessionManager.IsGuestMode;
            UpdateUserDisplay();

            if (_isGuestMode)
            {
                FavoritesTab.Visibility = Visibility.Collapsed;
                MyRecipesTab.Margin = new Thickness(0);
                MyRecipesTab.CornerRadius = new CornerRadius(15);

                LoadGuestRecipes();
                _currentTab = 0;
                MyRecipesContent.Visibility = Visibility.Visible;
                FavoritesContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                FavoritesTab.Visibility = Visibility.Visible;
                MyRecipesTab.Margin = new Thickness(0, 0, 1, 0);
                MyRecipesTab.CornerRadius = new CornerRadius(15, 0, 0, 15);
                LoadUserRecipesFromDb();
            }

            DisplayCurrentTab();
            GuestInfoPanel.Visibility = _isGuestMode ? Visibility.Visible : Visibility.Collapsed;
            UpdateTabSelection();
        }

        private void UpdateUserDisplay()
        {
            if (_isGuestMode)
            {
                UserIcon.Text = "👤";
                UserNameText.Text = "Гость";
            }
            else if (App.CurrentUser != null)
            {
                UserIcon.Text = "🍳";
                UserNameText.Text = App.CurrentUser.Name.Split(' ')[0];
            }
        }

        private void LoadGuestRecipes()
        {
            _guestRecipes = new List<GuestRecipeData>();

            if (SessionManager.GuestTempData.ContainsKey("GuestRecipes"))
            {
                _guestRecipes = SessionManager.GuestTempData["GuestRecipes"] as List<GuestRecipeData> ?? new List<GuestRecipeData>();
            }

            _myRecipes = new List<Recipes>();
        }

        private void LoadUserRecipesFromDb()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Загружаем рецепты, созданные текущим пользователем
                    _myRecipes = context.Recipes
                        .Where(r => r.UserId == App.CurrentUser.Id)
                        .OrderByDescending(r => r.Id)
                        .ToList();

                    // Загружаем связанные данные для каждого рецепта
                    foreach (var recipe in _myRecipes)
                    {
                        context.Entry(recipe).Reference(r => r.MealCategories).Load();
                        context.Entry(recipe).Reference(r => r.Categories).Load();
                    }

                    // Загружаем избранные рецепты
                    var favoriteIds = context.FavoriteRecipes
                        .Where(f => f.UserId == App.CurrentUser.Id)
                        .Select(f => f.RecipeId)
                        .ToList();

                    if (favoriteIds.Any())
                    {
                        _favoriteRecipes = context.Recipes
                            .Where(r => favoriteIds.Contains(r.Id))
                            .OrderByDescending(r => r.Id)
                            .ToList();

                        foreach (var recipe in _favoriteRecipes)
                        {
                            context.Entry(recipe).Reference(r => r.MealCategories).Load();
                            context.Entry(recipe).Reference(r => r.Categories).Load();
                        }
                    }
                    else
                    {
                        _favoriteRecipes = new List<Recipes>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рецептов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _myRecipes = new List<Recipes>();
                _favoriteRecipes = new List<Recipes>();
            }
        }

        private void DisplayCurrentTab()
        {
            if (_currentTab == 0)
            {
                DisplayMyRecipes();
            }
            else
            {
                DisplayFavorites();
            }
        }

        private void DisplayMyRecipes()
        {
            MyRecipesPanel.Children.Clear();

            if (_isGuestMode)
            {
                if (_guestRecipes != null && _guestRecipes.Any())
                {
                    EmptyMyRecipesState.Visibility = Visibility.Collapsed;
                    MyRecipesPanel.Visibility = Visibility.Visible;

                    // Сортируем по дате создания (от новых к старым)
                    foreach (var guestRecipe in _guestRecipes.OrderByDescending(r => r.CreatedAt))
                    {
                        var card = CreateGuestRecipeCard(guestRecipe);
                        MyRecipesPanel.Children.Add(card);
                    }
                }
                else
                {
                    EmptyMyRecipesState.Visibility = Visibility.Visible;
                    MyRecipesPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (_myRecipes != null && _myRecipes.Any())
                {
                    EmptyMyRecipesState.Visibility = Visibility.Collapsed;
                    MyRecipesPanel.Visibility = Visibility.Visible;

                    foreach (var recipe in _myRecipes)
                    {
                        var card = CreateRecipeCard(recipe);
                        MyRecipesPanel.Children.Add(card);
                    }
                }
                else
                {
                    EmptyMyRecipesState.Visibility = Visibility.Visible;
                    MyRecipesPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void DisplayFavorites()
        {
            if (_isGuestMode)
            {
                FavoritesPanel.Children.Clear();
                EmptyFavoritesState.Visibility = Visibility.Visible;
                FavoritesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            FavoritesPanel.Children.Clear();

            if (_favoriteRecipes == null || !_favoriteRecipes.Any())
            {
                EmptyFavoritesState.Visibility = Visibility.Visible;
                FavoritesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyFavoritesState.Visibility = Visibility.Collapsed;
            FavoritesPanel.Visibility = Visibility.Visible;

            foreach (var recipe in _favoriteRecipes)
            {
                var card = CreateRecipeCard(recipe);
                FavoritesPanel.Children.Add(card);
            }
        }

        private Border CreateRecipeCard(Recipes recipe)
        {
            var card = new Border
            {
                Style = (Style)FindResource("RecipeCardStyle"),
                Tag = recipe.Id
            };
            card.MouseLeftButtonUp += RecipeCard_Click;

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            var emoji = GetMealCategoryEmoji(recipe.MealCategories?.Name);
            stackPanel.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 32,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var time = recipe.CookingTime ?? 0;
            stackPanel.Children.Add(new TextBlock
            {
                Text = time > 0 ? $"⏱️ {time} мин" : "⏱️ Время не указано",
                FontSize = 12,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            if (recipe.MealCategories != null)
            {
                var categoryText = string.IsNullOrEmpty(recipe.MealCategories.Icon)
                    ? $"• {recipe.MealCategories.Name}"
                    : $"{recipe.MealCategories.Icon} {recipe.MealCategories.Name}";

                stackPanel.Children.Add(new TextBlock
                {
                    Text = categoryText,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            card.Child = stackPanel;
            return card;
        }

        private Border CreateGuestRecipeCard(GuestRecipeData guestRecipe)
        {
            var card = new Border
            {
                Style = (Style)FindResource("RecipeCardStyle"),
                Tag = -1,
                Background = Brushes.White
            };

            // Эффект при наведении
            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F5"));
                card.Effect = new DropShadowEffect
                {
                    BlurRadius = 15,
                    Opacity = 0.2,
                    ShadowDepth = 5,
                    Color = (Color)ColorConverter.ConvertFromString("#CFA1C1")
                };
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = Brushes.White;
                card.Effect = new DropShadowEffect
                {
                    BlurRadius = 10,
                    Opacity = 0.1,
                    ShadowDepth = 3
                };
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            // Иконка временного рецепта
            var iconBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                CornerRadius = new CornerRadius(30),
                Width = 60,
                Height = 60,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            iconBorder.Child = new TextBlock
            {
                Text = "📝",
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stackPanel.Children.Add(iconBorder);

            // Название рецепта
            stackPanel.Children.Add(new TextBlock
            {
                Text = guestRecipe.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Время приготовления
            var time = guestRecipe.CookingTime ?? 0;
            stackPanel.Children.Add(new TextBlock
            {
                Text = time > 0 ? $"⏱️ {time} мин" : "⏱️ Время не указано",
                FontSize = 12,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Количество ингредиентов
            var ingredientsCount = guestRecipe.Ingredients?.Count ?? 0;
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"🥕 {ingredientsCount} ингр.",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Количество шагов
            var stepsCount = guestRecipe.Steps?.Count ?? 0;
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"📋 {stepsCount} шагов",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Плашка "Временный"
            var tempBadge = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE4B5")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            tempBadge.Child = new TextBlock
            {
                Text = "⏳ Временный",
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                FontWeight = FontWeights.Medium
            };
            stackPanel.Children.Add(tempBadge);

            card.Child = stackPanel;

            // Клик - показываем информацию
            card.MouseLeftButtonUp += (s, e) =>
            {
                string ingredientsList = "";
                if (guestRecipe.Ingredients != null && guestRecipe.Ingredients.Any())
                {
                    ingredientsList = string.Join("\n", guestRecipe.Ingredients.Take(5).Select(i => $"  • {i.Name} - {i.Quantity} {i.Unit}"));
                    if (guestRecipe.Ingredients.Count > 5)
                        ingredientsList += $"\n  ... и ещё {guestRecipe.Ingredients.Count - 5}";
                }

                string stepsList = "";
                if (guestRecipe.Steps != null && guestRecipe.Steps.Any())
                {
                    stepsList = string.Join("\n", guestRecipe.Steps.Take(3).Select((step, idx) => $"  {idx + 1}. {step}"));
                    if (guestRecipe.Steps.Count > 3)
                        stepsList += $"\n  ... и ещё {guestRecipe.Steps.Count - 3} шагов";
                }

                MessageBox.Show(
                    $"📖 {guestRecipe.Title}\n\n" +
                    $"📝 Описание: {(string.IsNullOrEmpty(guestRecipe.Description) ? "нет" : guestRecipe.Description)}\n" +
                    $"⏱️ Время: {(guestRecipe.CookingTime > 0 ? $"{guestRecipe.CookingTime} мин" : "не указано")}\n\n" +
                    $"🥕 Ингредиенты ({guestRecipe.Ingredients?.Count ?? 0}):\n{ingredientsList}\n\n" +
                    $"📋 Шаги ({guestRecipe.Steps?.Count ?? 0}):\n{stepsList}\n\n" +
                    $"📅 Создан: {guestRecipe.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                    "⚠️ Временный рецепт. Для постоянного сохранения зарегистрируйтесь.",
                    "Временный рецепт",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };

            return card;
        }

        private string GetMealCategoryEmoji(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "🍽️";

            switch (categoryName)
            {
                case "Супы": return "🥣";
                case "Салаты": return "🥗";
                case "Горячие блюда": return "🍲";
                case "Паста и каши": return "🍝";
                case "Выпечка": return "🥐";
                case "Десерты": return "🍰";
                case "Напитки": return "🥤";
                case "Закуски": return "🍢";
                case "Соусы": return "🥫";
                case "Вегетарианские": return "🥬";
                default: return "🍽️";
            }
        }

        private void UpdateTabSelection()
        {
            if (_isGuestMode)
            {
                MyRecipesTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                var myText = (TextBlock)MyRecipesTab.Child;
                myText.Foreground = Brushes.White;
                return;
            }

            if (_currentTab == 0)
            {
                MyRecipesTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                var myText = (TextBlock)MyRecipesTab.Child;
                myText.Foreground = Brushes.White;

                FavoritesTab.Background = Brushes.White;
                var favText = (TextBlock)FavoritesTab.Child;
                favText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
            }
            else
            {
                MyRecipesTab.Background = Brushes.White;
                var myText = (TextBlock)MyRecipesTab.Child;
                myText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));

                FavoritesTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34"));
                var favText = (TextBlock)FavoritesTab.Child;
                favText.Foreground = Brushes.White;
            }
        }

        private void RecipeCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int recipeId && recipeId > 0)
            {
                NavigationService?.Navigate(new RecipeDetails(recipeId));
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SearchAndFilters());
        }

        private void btnAddRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (_isGuestMode)
            {
                SessionManager.GuestTempData["PendingGuestRecipeCreation"] = true;
            }
            NavigationService?.Navigate(new CreatingReciepe());
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authorization());
        }

        private void MyRecipesTab_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == 0) return;

            _currentTab = 0;
            MyRecipesContent.Visibility = Visibility.Visible;
            FavoritesContent.Visibility = Visibility.Collapsed;
            UpdateTabSelection();
            DisplayMyRecipes();
        }

        private void FavoritesTab_Click(object sender, RoutedEventArgs e)
        {
            if (_isGuestMode) return;
            if (_currentTab == 1) return;

            _currentTab = 1;
            MyRecipesContent.Visibility = Visibility.Collapsed;
            FavoritesContent.Visibility = Visibility.Visible;
            UpdateTabSelection();
            DisplayFavorites();
        }

        public void RefreshRecipes()
        {
            if (_isGuestMode)
            {
                LoadGuestRecipes();
                DisplayCurrentTab();
            }
            else
            {
                LoadUserRecipesFromDb();
                DisplayCurrentTab();
            }
        }
    }
}