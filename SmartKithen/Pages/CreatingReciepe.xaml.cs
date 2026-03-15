using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartKithen.Pages
{
    public partial class CreatingReciepe : Page
    {
        private int _stepCount = 2;

        public CreatingReciepe()
        {
            InitializeComponent();
            Loaded += CreatingReciepe_Loaded;
        }

        private void CreatingReciepe_Loaded(object sender, RoutedEventArgs e)
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
            IngredientsPanel.Children.Add(CreateIngredientRow());
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            _stepCount++;
            StepsPanel.Children.Add(CreateStepRow(_stepCount));
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

            var capturedRow = row;
            deleteBtn.Click += (s, e) =>
            {
                IngredientsPanel.Children.Remove(capturedRow);
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
            numberBorder.Child = new TextBlock
            {
                Text = stepNumber.ToString(),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(numberBorder, 0);

            // Поле текста
            var textBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(10, 0, 0, 0),
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
                StepsPanel.Children.Remove(capturedRow);
                RenumberSteps();
            };
            Grid.SetColumn(deleteBtn, 2);

            row.Children.Add(numberBorder);
            row.Children.Add(textBorder);
            row.Children.Add(deleteBtn);

            return row;
        }

        private void RenumberSteps()
        {
            int number = 1;
            foreach (var child in StepsPanel.Children)
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
            var title = TitleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название рецепта.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

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

            var ingredients = CollectIngredients();
            var steps = CollectSteps();

            int? categoryId = null;
            if (CategoryComboBox.SelectedItem is ComboBoxItem selectedCat)
                categoryId = (int)selectedCat.Tag;

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var recipe = new Recipes
                    {
                        Title = title,
                        Description = DescriptionTextBox.Text.Trim(),
                        CookingTime = cookingTime,
                        CategoryId = categoryId,
                        Instructions = ""
                    };

                    context.Recipes.Add(recipe);
                    context.SaveChanges();

                    foreach (var ing in ingredients)
                    {
                        var product = context.Products
                            .FirstOrDefault(p => p.Name.ToLower() == ing.Name.ToLower());

                        if (product == null)
                        {
                            product = new Products
                            {
                                Name = ing.Name,
                                CategoryId = 6,
                                DefaultUnit = ing.Unit
                            };
                            context.Products.Add(product);
                            context.SaveChanges();
                        }

                        context.Ingredients.Add(new Ingredients
                        {
                            RecipeId = recipe.Id,
                            ProductId = product.Id,
                            Quantity = ing.Quantity,
                            Unit = ing.Unit
                        });
                    }

                    for (int i = 0; i < steps.Count; i++)
                    {
                        context.RecipeSteps.Add(new RecipeSteps
                        {
                            RecipeId = recipe.Id,
                            StepNumber = i + 1,
                            Description = steps[i]
                        });
                    }

                    context.SaveChanges();
                }

                MessageBox.Show("Рецепт успешно сохранён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<IngredientEntry> CollectIngredients()
        {
            var result = new List<IngredientEntry>();

            foreach (var child in IngredientsPanel.Children)
            {
                if (!(child is Grid row)) continue;

                var nameBox = GetTextBoxFromBorder(row, 0);
                var quantityBox = GetTextBoxFromBorder(row, 1);
                var unitCombo = GetComboBoxFromBorder(row, 2);

                if (nameBox == null) continue;

                var name = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (!decimal.TryParse(quantityBox?.Text.Trim(), out decimal quantity))
                    quantity = 1;

                var unit = (unitCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "г";

                result.Add(new IngredientEntry
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = unit
                });
            }

            return result;
        }

        private List<string> CollectSteps()
        {
            var result = new List<string>();

            foreach (var child in StepsPanel.Children)
            {
                if (!(child is Grid row)) continue;
                if (row.Children.Count < 2) continue;

                var textBorder = row.Children[1] as Border;
                if (textBorder == null) continue;

                var textBox = textBorder.Child as TextBox;
                if (textBox == null) continue;

                var text = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                    result.Add(text);
            }

            return result;
        }

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

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.GoBack();

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Отменить создание рецепта? Данные не сохранятся.",
                "Отмена", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                NavigationService?.GoBack();
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция загрузки фото в разработке.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private class IngredientEntry
        {
            public string Name { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
        }
    }
}