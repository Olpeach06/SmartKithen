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

            // Создаём три начальных ингредиента
            IngredientsPanel.Children.Add(CreateIngredientRow());
            IngredientsPanel.Children.Add(CreateIngredientRow());
            IngredientsPanel.Children.Add(CreateIngredientRow());

            // Убрали создание начальных шагов — пользователь добавляет сам
            _stepCount = 0;

            RefreshIngredientCheckboxes();
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

        // Собираем текущие названия ингредиентов из панели
        private List<string> GetCurrentIngredientNames()
        {
            var names = new List<string>();

            foreach (var child in IngredientsPanel.Children)
            {
                if (!(child is Grid row)) continue;
                var nameBox = GetTextBoxFromBorder(row, 0);
                var name = nameBox?.Text.Trim();
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            return names;
        }

        // Обновляем чекбоксы ингредиентов во всех шагах
        private void RefreshIngredientCheckboxes()
        {
            var ingredientNames = GetCurrentIngredientNames();

            foreach (var child in StepsPanel.Children)
            {
                if (!(child is Grid stepRow)) continue;
                UpdateCheckboxesInStep(stepRow, ingredientNames);
            }
        }

        // Обновляем чекбоксы в конкретном шаге
        private void UpdateCheckboxesInStep(Grid stepRow, List<string> ingredientNames)
        {
            // Чекбоксы хранятся в WrapPanel — это последний элемент строки шага
            WrapPanel checkboxPanel = null;
            foreach (var child in stepRow.Children)
            {
                if (child is WrapPanel wp)
                {
                    checkboxPanel = wp;
                    break;
                }
            }

            if (checkboxPanel == null) return;

            // Запоминаем уже отмеченные
            var checkedNames = new HashSet<string>();
            foreach (var cb in checkboxPanel.Children)
            {
                if (cb is CheckBox checkbox && checkbox.IsChecked == true)
                    checkedNames.Add(checkbox.Content?.ToString() ?? "");
            }

            checkboxPanel.Children.Clear();

            foreach (var name in ingredientNames)
            {
                var checkbox = new CheckBox
                {
                    Content = name,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 10, 6),
                    IsChecked = checkedNames.Contains(name),
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#555"))
                };
                checkboxPanel.Children.Add(checkbox);
            }

            // Если ингредиентов нет — показываем подсказку
            if (ingredientNames.Count == 0)
            {
                checkboxPanel.Children.Add(new TextBlock
                {
                    Text = "Сначала добавьте ингредиенты",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999")),
                    FontStyle = FontStyles.Italic
                });
            }
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            IngredientsPanel.Children.Add(CreateIngredientRow());
            // Обновляем чекбоксы во всех шагах после добавления ингредиента
            RefreshIngredientCheckboxes();
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            _stepCount++;
            StepsPanel.Children.Add(CreateStepRow(_stepCount));
        }

        private Grid CreateIngredientRow()
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

            // Название
            var nameBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };

            var nameBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            nameBox.TextChanged += (s, e) => RefreshIngredientCheckboxes();
            nameBorder.Child = nameBox;
            Grid.SetColumn(nameBorder, 0);

            // Количество
            var qtyBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(8, 0, 8, 0),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            qtyBorder.Child = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(qtyBorder, 1);

            // Единица измерения
            var unitBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1)
            };
            var unitCombo = new ComboBox
            {
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center
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
                FontSize = 13,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CFA1C1")),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var capturedRow = row;
            deleteBtn.Click += (s, e) =>
            {
                IngredientsPanel.Children.Remove(capturedRow);
                RefreshIngredientCheckboxes();
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
            var row = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
            Grid.SetRow(numberBorder, 0);

            // Поле текста шага
            var textBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(10, 0, 0, 6),
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
                Height = 70,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetColumn(textBorder, 1);
            Grid.SetRow(textBorder, 0);

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
            Grid.SetRow(deleteBtn, 0);

            // Панель чекбоксов ингредиентов
            var checkboxesContainer = new Border
            {
                Margin = new Thickness(45, 0, 36, 0),
                Padding = new Thickness(10, 8, 10, 8),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F5FFF9")),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#C8E6C9")),
                BorderThickness = new Thickness(1)
            };

            var checkboxesInner = new StackPanel { Orientation = Orientation.Vertical };

            checkboxesInner.Children.Add(new TextBlock
            {
                Text = "Ингредиенты этого шага:",
                FontSize = 11,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A5D34")),
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var checkboxWrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Наполняем текущими ингредиентами
            var currentIngredients = GetCurrentIngredientNames();
            if (currentIngredients.Count == 0)
            {
                checkboxWrap.Children.Add(new TextBlock
                {
                    Text = "Сначала добавьте ингредиенты",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#999")),
                    FontStyle = FontStyles.Italic
                });
            }
            else
            {
                foreach (var name in currentIngredients)
                {
                    checkboxWrap.Children.Add(new CheckBox
                    {
                        Content = name,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 10, 4),
                        Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#555"))
                    });
                }
            }

            checkboxesInner.Children.Add(checkboxWrap);
            checkboxesContainer.Child = checkboxesInner;

            Grid.SetColumn(checkboxesContainer, 0);
            Grid.SetColumnSpan(checkboxesContainer, 3);
            Grid.SetRow(checkboxesContainer, 1);

            row.Children.Add(numberBorder);
            row.Children.Add(textBorder);
            row.Children.Add(deleteBtn);
            row.Children.Add(checkboxesContainer);

            return row;
        }

        private void RenumberSteps()
        {
            int number = 1;
            foreach (var child in StepsPanel.Children)
            {
                if (!(child is Grid row)) continue;

                var numberBorder = row.Children[0] as Border;
                var textBlock = numberBorder?.Child as TextBlock;
                if (textBlock != null)
                    textBlock.Text = number.ToString();

                number++;
            }
            _stepCount = number - 1;
        }

        // Собираем выбранные ингредиенты для конкретного шага
        private List<string> GetCheckedIngredients(Grid stepRow)
        {
            var result = new List<string>();

            foreach (var child in stepRow.Children)
            {
                if (!(child is Border container)) continue;

                var innerStack = container.Child as StackPanel;
                if (innerStack == null) continue;

                foreach (var innerChild in innerStack.Children)
                {
                    if (!(innerChild is WrapPanel wrap)) continue;

                    foreach (var item in wrap.Children)
                    {
                        if (item is CheckBox cb && cb.IsChecked == true)
                            result.Add(cb.Content?.ToString() ?? "");
                    }
                }
            }

            return result;
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

            int? categoryId = null;
            if (CategoryComboBox.SelectedItem is ComboBoxItem selectedCat)
                categoryId = (int)selectedCat.Tag;

            var ingredientEntries = CollectIngredients();
            var stepRows = StepsPanel.Children.OfType<Grid>().ToList();

            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    // Сохраняем рецепт
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

                    // Сохраняем ингредиенты и запоминаем name -> Ingredients объект
                    var savedIngredients = new Dictionary<string, Ingredients>();

                    foreach (var ing in ingredientEntries)
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

                        var ingredient = new Ingredients
                        {
                            RecipeId = recipe.Id,
                            ProductId = product.Id,
                            Quantity = ing.Quantity,
                            Unit = ing.Unit
                        };
                        context.Ingredients.Add(ingredient);
                        context.SaveChanges();

                        savedIngredients[ing.Name.ToLower()] = ingredient;
                    }

                    // Сохраняем шаги и связи с ингредиентами
                    for (int i = 0; i < stepRows.Count; i++)
                    {
                        var stepRow = stepRows[i];
                        var textBorder = stepRow.Children
                            .OfType<Border>()
                            .FirstOrDefault(b => b.Child is TextBox);
                        var stepText = (textBorder?.Child as TextBox)?.Text.Trim() ?? "";

                        if (string.IsNullOrEmpty(stepText)) continue;

                        var step = new RecipeSteps
                        {
                            RecipeId = recipe.Id,
                            StepNumber = i + 1,
                            Description = stepText
                        };
                        context.RecipeSteps.Add(step);
                        context.SaveChanges();

                        // Сохраняем связи шага с ингредиентами
                        var checkedNames = GetCheckedIngredients(stepRow);
                        foreach (var name in checkedNames)
                        {
                            if (savedIngredients.TryGetValue(name.ToLower(), out var ing))
                            {
                                context.StepIngredients.Add(new StepIngredients
                                {
                                    StepId = step.Id,
                                    IngredientId = ing.Id
                                });
                            }
                        }
                        context.SaveChanges();
                    }
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

                var name = nameBox?.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                decimal.TryParse(quantityBox?.Text.Trim(), out decimal quantity);
                var unit = (unitCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "г";

                result.Add(new IngredientEntry
                {
                    Name = name,
                    Quantity = quantity > 0 ? quantity : 1,
                    Unit = unit
                });
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