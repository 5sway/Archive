using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveApp
{
    /// <summary>
    /// Статический класс для хранения данных о текущем авторизованном пользователе.
    /// Данные сохраняются на протяжении всей сессии и используются для контроля доступа.
    /// </summary>
    public static class UserData
    {
        public static string CurrentUserRole { get; set; }
        public static int CurrentUserId { get; set; }
        public static string CurrentUserName { get; set; }
    }
}