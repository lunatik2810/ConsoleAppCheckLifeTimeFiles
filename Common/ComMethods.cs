using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class ComMethods
    {
        /// <summary>
        /// Преобразует строку String в число Int
        /// </summary>
        /// <param name="str">строка, которую нужно преоброзвать в число</param>
        /// <returns>
        /// int - успешное преобразование
        /// null - произошла ошибка в процессе преобразования
        /// </returns>
        public static int? StringToInt(string str, int res)
        {
            try
            {
                bool success = int.TryParse(str, out res);
                if (success)
                    return res;
                else
                    return null;
            }
            catch(Exception ex)
            {
                ComData.logger.Error(ex, $"Ошибка в процессе преобразования строки '{str}' в число");
                return null;
            }            
        }

    }
}
