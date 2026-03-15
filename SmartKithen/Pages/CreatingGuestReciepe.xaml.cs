using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class CreatingGuestReciepe : Page
    {
        private int _stepCount = 2;

        public CreatingGuestReciepe()
        {
            InitializeComponent();
            Loaded += CreatingGuestReciepe_Loaded;
        }

        private void CreatingGuestReciepe_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var categories = context.Categories
                        .OrderBy(c => c.Name)
                        .ToList();

                    CategoryComboBox.Items.Clear();

                    foreach (var cat in categories)
                    {
                        CategoryComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = cat.Name,
                            Tag = cat.Id
                        });
                    }

                    if (CategoryComboBox.Items.Count > 0)
                        CategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            IngredientsListPanel.Children.Add(CreateIngredientRow());
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            _stepCount++;
            StepsListPanel.Children.Add(CreateStepRow(_stepCount));
        }

        private Grid CreateIngredientRow()
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            // Название
            var nameBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            nameBorder.Child = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            Grid.SetColumn(nameBorder, 0);

            // Количество
            var qtyBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(10, 0, 10, 0),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            qtyBorder.Child = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            Grid.SetColumn(qtyBorder, 1);

            // Единица измерения
            var unitBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            var unitCombo = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(4),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            foreach (var unit in new[] { "г", "кг", "мл", "л", "шт", "ч.л.", "ст.л." })
                unitCombo.Items.Add(new ComboBoxItem { Content = unit });
            unitCombo.SelectedIndex = 0;
            unitBorder.Child = unitCombo;
            Grid.SetColumn(unitBorder, 2);

            // Кнопка удаления
            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 14,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Захватываем ссылку на row для удаления
            var capturedRow = row;
            deleteBtn.Click += (s, e) =>
            {
                IngredientsListPanel.Children.Remove(capturedRow);
            };
            Grid.SetColumn(deleteBtn, 3);

            row.Children.Add(nameBorder);
            row.Children.Add(qtyBorder);
            row.Children.Add(unitBorder);
            row.Children.Add(deleteBtn);

            return row;
        }

        private Grid CreateStepRow(int stepNumber)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            // Номер шага
            var numberBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                CornerRadius = new CornerRadius(8),
                Width = 35,
                Height = 35,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var numberText = new TextBlock
            {
                Text = stepNumber.ToString(),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            numberBorder.Child = numberText;
            Grid.SetColumn(numberBorder, 0);

            // Поле текста
            var textBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(10, 10, 10, 10),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            textBorder.Child = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetColumn(textBorder, 1);

            // Кнопка удаления
            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 14,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var capturedRow = row;
            deleteBtn.Click += (s, e) =>
            {
                StepsListPanel.Children.Remove(capturedRow);
                RenumberSteps();
            };
            Grid.SetColumn(deleteBtn, 2);

            row.Children.Add(numberBorder);
            row.Children.Add(textBorder);
            row.Children.Add(deleteBtn);

            return row;
        }

        // Перенумерация шагов после удаления
        private void RenumberSteps()
        {
            int number = 1;
            foreach (var child in StepsListPanel.Children)
            {
                if (!(child is Grid row)) continue;
                if (row.Children.Count == 0) continue;

                var numberBorder = row.Children[0] as Border;
                if (numberBorder == null) continue;

                var textBlock = numberBorder.Child as TextBlock;
                if (textBlock != null)
                    textBlock.Text = number.ToString();

                number++;
            }

            _stepCount = number - 1;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация названия
            var title = RecipeNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название рецепта.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RecipeNameTextBox.Focus();
                return;
            }

            // Время
            int? cookingTime = null;
            if (!string.IsNullOrWhiteSpace(TimeTextBox.Text))
            {
                if (int.TryParse(TimeTextBox.Text.Trim(), out int time) && time > 0)
                    cookingTime = time;
                else
                {
                    MessageBox.Show("Введите корректное время (целое число).", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TimeTextBox.Focus();
                    return;
                }
            }

            // Категория
            int? categoryId = null;
            if (CategoryComboBox.SelectedItem is ComboBoxItem selectedCat)
                categoryId = (int)selectedCat.Tag;

            // Ингредиенты
            var ingredients = new List<GuestIngredient>();
            foreach (var child in IngredientsListPanel.Children)
            {
                if (!(child is Grid row)) continue;

                var nameBox = GetTextBoxFromBorder(row, 0);
                var qtyBox = GetTextBoxFromBorder(row, 1);
                var unitCombo = GetComboBoxFromBorder(row, 2);

                var name = nameBox?.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                decimal.TryParse(qtyBox?.Text.Trim(), out decimal qty);
                var unit = (unitCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "г";

                ingredients.Add(new GuestIngredient
                {
                    Name = name,
                    Quantity = qty > 0 ? qty : 1,
                    Unit = unit
                });
            }

            // Шаги
            var steps = new List<string>();
            foreach (var child in StepsListPanel.Children)
            {
                if (!(child is Grid row)) continue;
                if (row.Children.Count < 2) continue;

                var textBorder = row.Children[1] as Border;
                var textBox = textBorder?.Child as TextBox;
                var text = textBox?.Text.Trim();

                if (!string.IsNullOrEmpty(text))
                    steps.Add(text);
            }

            // Сохраняем в сессию
            GuestSession.SaveRecipe(new GuestRecipeData
            {
                Title = title,
                Description = DescriptionTextBox.Text.Trim(),
                CookingTime = cookingTime,
                CategoryId = categoryId,
                Ingredients = ingredients,
                Steps = steps
            });

            // Предлагаем войти или зарегистрироваться
            var result = MessageBox.Show(
                $"Рецепт «{title}» сохранён в текущей сессии.\n\n" +
                "Чтобы не потерять его — войдите в аккаунт или зарегистрируйтесь.\n\n" +
                "Войти в аккаунт?",
                "Рецепт сохранён",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                NavigationService?.Navigate(new Authorization());
            else
                NavigationService?.Navigate(new GuestReciepe());
        }

        // Вспомогательные методы для сбора данных
        private TextBox GetTextBoxFromBorder(Grid grid, int column)
        {
            foreach (var child in grid.Children)
            {
                if (child is Border border && Grid.GetColumn(border) == column)
                    return border.Child as TextBox;
            }
            return null;
        }

        private ComboBox GetComboBoxFromBorder(Grid grid, int column)
        {
            foreach (var child in grid.Children)
            {
                if (child is Border border && Grid.GetColumn(border) == column)
                    return border.Child as ComboBox;
            }
            return null;
        }

        private void GuestModeButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Войдите в аккаунт, чтобы сохранять рецепты.",
                "Войти в аккаунт",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                NavigationService?.Navigate(new Authorization());
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.GoBack();

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Для загрузки фото необходим аккаунт.", "Гостевой режим",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}