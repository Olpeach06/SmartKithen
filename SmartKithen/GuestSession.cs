using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartKithen
{
    // Временное хранилище рецепта гостя до входа в аккаунт
    public static class GuestSession
    {
        public static GuestRecipeData PendingRecipe { get; private set; }

        public static void SaveRecipe(GuestRecipeData recipe)
        {
            PendingRecipe = recipe;
        }

        public static bool HasPendingRecipe => PendingRecipe != null;

        public static void Clear()
        {
            PendingRecipe = null;
        }
    }

    public class GuestRecipeData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int? CookingTime { get; set; }
        public int? CategoryId { get; set; }
        public List<GuestIngredient> Ingredients { get; set; } = new List<GuestIngredient>();
        public List<string> Steps { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class GuestIngredient
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}