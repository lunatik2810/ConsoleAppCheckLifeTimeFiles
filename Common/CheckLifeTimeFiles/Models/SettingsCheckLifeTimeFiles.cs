using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.CheckLifeTimeFiles.Models
{
    public class SettingsCheckLifeTimeFiles
    {
        //Интервал проверки времени жизни файла в секундах
        public int IntervalCheckLifeTimeFiles { get; set; }
        //Допустимое время жизни файла в секундах
        public int AllowLifeTimeFiles { get; set; }
        //Справочник, где ключи - это пути к директориям, значение - список с информацией о файлах в директориях
        public Dictionary<string, List<InfoLifeTimeFiles>> DicDirsCheck { get; set; }
    }
}
