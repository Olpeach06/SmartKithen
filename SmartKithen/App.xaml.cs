using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SmartKithen
{
    public partial class App : Application
    {
        // Текущий пользователь
        public static AppData.Users CurrentUser { get; set; }

        // Контекст БД (будем создавать при необходимости)
        public static AppData.SmartKitchenEntities DatabaseContext
        {
            get
            {
                // Создаем контекст при первом обращении
                // Можно использовать паттерн Lazy<T> для более сложных сценариев
                return new AppData.SmartKitchenEntities();
            }
        }

        // Простое свойство для хранения настроек
        public static System.Collections.Hashtable AppSettings { get; } = new System.Collections.Hashtable();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CurrentUser = null; // Сбрасываем пользователя при запуске
        }


    }
}