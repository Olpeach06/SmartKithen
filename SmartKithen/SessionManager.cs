using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartKithen
{
    public static class SessionManager
    {
        public static int CurrentUserId => App.CurrentUser?.Id ?? 0;
        public static string CurrentUserName => App.CurrentUser?.Name ?? string.Empty;
        public static bool IsLoggedIn => App.CurrentUser != null && App.CurrentUser.Id != 0;
    }
}
