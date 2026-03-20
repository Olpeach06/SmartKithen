using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartKithen.AppData;

namespace SmartKithen
{
    public static class SessionManager
    {
        public static int CurrentUserId => App.CurrentUser?.Id ?? 0;
        public static string CurrentUserName => App.CurrentUser?.Name ?? string.Empty;
        public static bool IsLoggedIn => App.CurrentUser != null && App.CurrentUser.Id != 0;

        // Флаг гостевого режима
        public static bool IsGuestMode => App.CurrentUser == null || App.CurrentUser.Id == 0;

        // Временные данные для гостя (ТОЛЬКО список покупок)
        public static List<TemporaryShoppingItem> GuestShoppingList { get; set; } = new List<TemporaryShoppingItem>();
        public static List<TemporaryProduct> GuestProducts { get; set; } = new List<TemporaryProduct>();

        // Словарь для временных данных (можно хранить что угодно)
        public static Dictionary<string, object> GuestTempData { get; set; } = new Dictionary<string, object>();

        // Очистка всех данных гостя
        public static void ClearGuestData()
        {
            GuestShoppingList?.Clear();
            GuestProducts?.Clear();
            GuestTempData?.Clear();
        }

        // Сохранить продукт в список покупок гостя
        public static void AddToGuestShoppingList(int productId, string productName, decimal quantity, string unit)
        {
            var existingItem = GuestShoppingList.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                GuestShoppingList.Add(new TemporaryShoppingItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Quantity = quantity,
                    Unit = unit
                });
            }
        }

        // Удалить продукт из списка покупок гостя
        public static void RemoveFromGuestShoppingList(int productId)
        {
            var item = GuestShoppingList.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
                GuestShoppingList.Remove(item);
        }

        // Очистить отмеченные продукты из списка гостя
        public static void RemoveCheckedGuestItems(List<int> productIds)
        {
            GuestShoppingList.RemoveAll(i => productIds.Contains(i.ProductId));
        }

        // Получить количество продуктов в списке гостя
        public static int GetGuestShoppingListCount()
        {
            return GuestShoppingList?.Count ?? 0;
        }

        // Обновить количество продукта в списке гостя
        public static void UpdateGuestShoppingItemQuantity(int productId, decimal newQuantity)
        {
            var item = GuestShoppingList.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity = newQuantity;
            }
        }

        // Перенести данные гостя в БД при регистрации
        public static void TransferGuestDataToDatabase(SmartKitchenEntities context, int newUserId)
        {
            try
            {
                // Переносим список покупок
                if (GuestShoppingList != null && GuestShoppingList.Any())
                {
                    foreach (var item in GuestShoppingList)
                    {
                        var fridgeItem = new FridgeItems
                        {
                            UserId = newUserId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            ExpiryDate = DateTime.Today.AddMonths(1) // По умолчанию месяц
                        };
                        context.FridgeItems.Add(fridgeItem);
                    }
                }

                // Переносим продукты (если есть отдельный список)
                if (GuestProducts != null && GuestProducts.Any())
                {
                    foreach (var product in GuestProducts)
                    {
                        // Здесь можно добавить логику для продуктов
                        // Например, создать записи в другой таблице
                        var fridgeItem = new FridgeItems
                        {
                            UserId = newUserId,
                            ProductId = product.ProductId,
                            Quantity = product.Quantity,
                            ExpiryDate = product.ExpiryDate ?? DateTime.Today.AddMonths(1)
                        };
                        context.FridgeItems.Add(fridgeItem);
                    }
                }

                context.SaveChanges();

                // Очищаем временные данные после успешного переноса
                ClearGuestData();

                System.Diagnostics.Debug.WriteLine($"Данные гостя успешно перенесены для пользователя ID: {newUserId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка переноса данных: {ex.Message}");
                throw;
            }
        }

        // Проверить, есть ли у гостя сохраненные данные
        public static bool HasGuestData()
        {
            return (GuestShoppingList != null && GuestShoppingList.Any()) ||
                   (GuestProducts != null && GuestProducts.Any());
        }

        // Получить информацию о данных гостя для отображения
        public static string GetGuestDataSummary()
        {
            int shoppingCount = GetGuestShoppingListCount();

            if (shoppingCount == 0)
                return "Нет временных данных";

            return $"📝 Продуктов в списке: {shoppingCount}";
        }

        // Получить все продукты гостя (объединенный список)
        public static List<TemporaryShoppingItem> GetAllGuestItems()
        {
            var allItems = new List<TemporaryShoppingItem>();
            allItems.AddRange(GuestShoppingList);

            // Добавляем продукты из GuestProducts, конвертируя в TemporaryShoppingItem
            foreach (var product in GuestProducts)
            {
                allItems.Add(new TemporaryShoppingItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Quantity = product.Quantity,
                    Unit = product.Unit
                });
            }

            return allItems;
        }
    }

    // Класс для временного хранения продуктов в списке покупок гостя
    public class TemporaryShoppingItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public bool IsChecked { get; set; } = false;
        public DateTime AddedDate { get; set; } = DateTime.Now;
    }

    // Класс для временного хранения продуктов гостя (с датой)
    public class TemporaryProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsChecked { get; set; } = false;
    }
}