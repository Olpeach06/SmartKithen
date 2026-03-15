using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class GuestReciepe : Page
    {
        public GuestReciepe()
        {
            InitializeComponent();
            Loaded += GuestReciepe_Loaded;
        }

        private void GuestReciepe_Loaded(object sender, RoutedEventArgs e)
        {
            if (GuestSession.HasPendingRecipe)
                ShowRecipe();
            // Если рецепта нет — заглушка уже отображается по умолчанию
        }

        private void ShowRecipe()
        {
            var recipe = GuestSession.PendingRecipe;

            // Прячем заглушку и блок с подсказкой
            // Добавляем карточку с данными рецепта динамически

            // Находим StackPanel — главный контейнер страницы
            var mainPanel = Content as Grid;
            if (mainPanel == null) return;

            var outerStack = mainPanel.Children[1] as StackPanel;
            if (outerStack == null) return;

            // Убираем блок-заглушку (индекс 1 — второй элемент после шапки)
            // и заменяем на карточку рецепта
            if (outerStack.Children.Count > 1)
                outerStack.Children.RemoveAt(1);

            var recipeCard = BuildRecipeCard(recipe);
            outerStack.Children.Insert(1, recipeCard);
        }

        private Border BuildRecipeCard(GuestRecipeData recipe)
        {
            var card = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(35, 30, 35, 30),
                Margin = new Thickness(40, 30, 40, 20)
            };

            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 25,
                Opacity = 0.12,
                ShadowDepth = 5
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Заголовок + время
            var titleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            titleRow.Children.Add(new TextBlock
            {
                Text = recipe.Title,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (recipe.CookingTime.HasValue)
            {
                titleRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(15, 0, 0, 0),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"⏱️ {recipe.CookingTime} мин",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#666"))
                    }
                });
            }

            stack.Children.Add(titleRow);

            // Описание
            if (!string.IsNullOrEmpty(recipe.Description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = recipe.Description,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#666")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                });
            }

            // Разделитель
            stack.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Ингредиенты
            if (recipe.Ingredients != null && recipe.Ingredients.Count > 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "🥦 Ингредиенты",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 12)
                });

                foreach (var ing in recipe.Ingredients)
                {
                    var row = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    row.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                        Margin = new Thickness(0, 0, 10, 0)
                    });

                    row.Children.Add(new TextBlock
                    {
                        Text = ing.Name,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = System.Windows.Media.Brushes.Black
                    });

                    row.Children.Add(new TextBlock
                    {
                        Text = $" — {ing.Quantity} {ing.Unit}",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#666"))
                    });

                    stack.Children.Add(row);
                }

                stack.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Height = 1,
                    Fill = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                    Margin = new Thickness(0, 15, 0, 20)
                });
            }

            // Шаги
            if (recipe.Steps != null && recipe.Steps.Count > 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "👨‍🍳 Шаги приготовления",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1A5D34")),
                    Margin = new Thickness(0, 0, 0, 12)
                });

                for (int i = 0; i < recipe.Steps.Count; i++)
                {
                    var stepRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                    stepRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    stepRow.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });

                    var numberBorder = new Border
                    {
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#1A5D34")),
                        CornerRadius = new CornerRadius(15),
                        Width = 30,
                        Height = 30,
                        Margin = new Thickness(0, 0, 12, 0),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    numberBorder.Child = new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(numberBorder, 0);

                    var stepText = new TextBlock
                    {
                        Text = recipe.Steps[i],
                        FontSize = 14,
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#333")),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(stepText, 1);

                    stepRow.Children.Add(numberBorder);
                    stepRow.Children.Add(stepText);
                    stack.Children.Add(stepRow);
                }
            }

            card.Child = stack;
            return card;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.GoBack();

        private void btnAddProduct_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new CreatingGuestReciepe());

        private void btnAddFirstRecipe_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new CreatingGuestReciepe());

        private void btnLogin_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new Authorization());
    }
}