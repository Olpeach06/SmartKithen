using SmartKithen.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartKithen.Pages
{
    public partial class CookingMode : Page
    {
        private int _recipeId;
        private List<RecipeSteps> _steps = new List<RecipeSteps>();
        private int _currentStepIndex = 0;

        private DispatcherTimer _timer;
        private int _secondsLeft = 0;
        private bool _timerRunning = false;

        public CookingMode()
        {
            InitializeComponent();
            Loaded += CookingMode_Loaded;
        }

        public CookingMode(int recipeId) : this()
        {
            _recipeId = recipeId;
        }

        private void CookingMode_Loaded(object sender, RoutedEventArgs e)
        {
            InitTimer();

            if (_recipeId > 0)
                LoadSteps();
            else
                ShowNoStepsMessage();
        }

        private void InitTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void LoadSteps()
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var recipe = context.Recipes
                        .Include("RecipeSteps")
                        .Include("RecipeSteps.StepIngredients.Ingredients.Products.Units")  // ← ключевое добавление
                        .FirstOrDefault(r => r.Id == _recipeId);

                    if (recipe == null)
                    {
                        ShowNoStepsMessage();
                        return;
                    }

                    // Таймер — общее время рецепта
                    if (recipe.CookingTime.HasValue && recipe.CookingTime.Value > 0)
                        _secondsLeft = recipe.CookingTime.Value * 60;
                    else
                        _secondsLeft = 30 * 60; // дефолт 30 мин

                    _steps = recipe.RecipeSteps
                        .OrderBy(s => s.StepNumber)
                        .ToList();

                    if (_steps.Count == 0)
                    {
                        ShowNoStepsMessage();
                        UpdateTimerDisplay();
                        return;
                    }

                    _currentStepIndex = 0;
                    ShowMainContent();
                    ShowCurrentStep();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки шагов: {ex.Message}");
            }
        }

        // Показываем основную разметку (шаги есть)
        private void ShowMainContent()
        {
            MainContentPanel.Visibility = Visibility.Visible;
            NoStepsPanel.Visibility = Visibility.Collapsed;
            ProgressBorder.Visibility = Visibility.Visible;
            NavigationPanel.Visibility = Visibility.Visible;
        }

        // Показываем заглушку (шагов нет)
        private void ShowNoStepsMessage()
        {
            MainContentPanel.Visibility = Visibility.Collapsed;
            NoStepsPanel.Visibility = Visibility.Visible;
            ProgressBorder.Visibility = Visibility.Collapsed;
            NavigationPanel.Visibility = Visibility.Collapsed;
            StepIngredientsBlock.Visibility = Visibility.Collapsed;

            UpdateTimerDisplay();
        }

        private void ShowCurrentStep()
        {
            if (_steps.Count == 0) return;

            var step = _steps[_currentStepIndex];
            var total = _steps.Count;
            var current = _currentStepIndex + 1;
            var percent = (int)((current / (double)total) * 100);

            StepNumberText.Text = $"ШАГ {step.StepNumber}";
            StepDescriptionText.Text = step.Description;
            StepProgressText.Text = $"Шаг {current} из {total}";
            StepPercentText.Text = $"{percent}%";

            UpdateTimerDisplay();

            BackButton.IsEnabled = _currentStepIndex > 0;
            BackButton.Opacity = _currentStepIndex > 0 ? 1.0 : 0.4;

            var isLastStep = _currentStepIndex == _steps.Count - 1;
            NextButton.Content = isLastStep ? "Готово ✓" : "Далее →";

            LoadStepIngredients(step.Id);
        }

        private void LoadStepIngredients(int stepId)
        {
            try
            {
                using (var context = new SmartKitchenEntities())
                {
                    var stepIngredients = context.StepIngredients
                        .Include("Ingredients.Products.Units")   // ← важно!
                        .Where(si => si.StepId == stepId)
                        .Select(si => si.Ingredients)
                        .ToList();

                    StepIngredientsGrid.Children.Clear();
                    StepIngredientsGrid.ColumnDefinitions.Clear();

                    if (stepIngredients.Count == 0)
                    {
                        StepIngredientsBlock.Visibility = Visibility.Collapsed;
                        return;
                    }

                    StepIngredientsBlock.Visibility = Visibility.Visible;

                    // Настраиваем колонки (по одной на ингредиент + разделитель "+")
                    for (int i = 0; i < stepIngredients.Count; i++)
                    {
                        StepIngredientsGrid.ColumnDefinitions.Add(new ColumnDefinition
                        {
                            Width = new GridLength(1, GridUnitType.Star)
                        });

                        if (i < stepIngredients.Count - 1)
                        {
                            StepIngredientsGrid.ColumnDefinitions.Add(
                                new ColumnDefinition { Width = GridLength.Auto });
                        }
                    }

                    int colIndex = 0;
                    foreach (var ing in stepIngredients)
                    {
                        var card = BuildIngredientCard(ing);
                        Grid.SetColumn(card, colIndex);
                        StepIngredientsGrid.Children.Add(card);
                        colIndex++;

                        if (colIndex < stepIngredients.Count * 2 - 1)
                        {
                            var plus = new TextBlock
                            {
                                Text = "+",
                                FontSize = 24,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(10, 0, 10, 0)
                            };
                            Grid.SetColumn(plus, colIndex);
                            StepIngredientsGrid.Children.Add(plus);
                            colIndex++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ингредиентов шага: {ex.Message}");
            }
        }

        private Border BuildIngredientCard(Ingredients ing)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FC")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFA1C1")),
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = "🥘",
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = ing?.Products?.Name ?? "Продукт",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A5D34")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // ← Главное изменение здесь
            string unitDisplay = ing?.Products?.Units?.ShortName;
            if (string.IsNullOrEmpty(unitDisplay))
                unitDisplay = ing?.Unit ?? "г";  // fallback на старую строку или граммы

            stack.Children.Add(new TextBlock
            {
                Text = $"{ing?.Quantity:N2} {unitDisplay}",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            card.Child = stack;
            return card;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStepIndex == _steps.Count - 1)
            {
                StopTimer();
                var result = MessageBox.Show(
                    "Готово! Блюдо приготовлено 🎉\n\nВернуться к рецепту?",
                    "Готовка завершена",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    NavigationService?.GoBack();

                return;
            }

            _currentStepIndex++;
            ShowCurrentStep();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStepIndex <= 0) return;
            _currentStepIndex--;
            ShowCurrentStep();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRunning)
            {
                StopTimer();
                PauseButton.Content = "▶ Продолжить";
            }
            else
            {
                StartTimer();
                PauseButton.Content = "⏸ Пауза";
            }
        }

        private void StartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_timerRunning)
            {
                StartTimer();
                PauseButton.Content = "⏸ Пауза";
            }
        }

        private void PauseTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRunning)
            {
                StopTimer();
                PauseButton.Content = "▶ Продолжить";
            }
        }

        private void StartTimer()
        {
            if (_secondsLeft <= 0) return;
            _timerRunning = true;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timerRunning = false;
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_secondsLeft <= 0)
            {
                StopTimer();
                TimerText.Text = "00:00";
                PauseButton.Content = "⏸ Пауза";
                MessageBox.Show("Время приготовления вышло!",
                    "Таймер", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _secondsLeft--;
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            var minutes = _secondsLeft / 60;
            var seconds = _secondsLeft % 60;
            TimerText.Text = $"{minutes:D2}:{seconds:D2}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из режима готовки?",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StopTimer();
                NavigationService?.GoBack();
            }
        }
    }
}