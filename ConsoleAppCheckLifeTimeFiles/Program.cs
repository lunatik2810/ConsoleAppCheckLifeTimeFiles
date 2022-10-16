using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppCheckLifeTimeFiles.Models;
using ConsoleAppCheckLifeTimeFiles.Models.Configuration;
using Common;
using Common.CheckLifeTimeFiles;
using Common.CheckLifeTimeFiles.Models;

namespace ConsoleAppCheckLifeTimeFiles
{
    class Program
    {
        static WorkCheckLifeTimeFiles workCheckLifeTimeFiles;

        const string SectionSettingsCheckLifeTimeFiles = "SectionSettingsCheckLifeTimeFiles";
        //Интервал проверки времени жизни файла в секундах
        const string IntervalCheckLifeTimeFiles = "IntervalCheckLifeTimeFiles";
        //значение по умолчанию
        const int DefaultIntervalCheckLifeTimeFiles = 10;
        //Допустимое время жизни файла в секундах
        const string AllowLifeTimeFiles = "AllowLifeTimeFiles";
        const int DefaultAllowLifeTimeFiles = 30;

        static void Main(string[] args)
        {
            WriteInfo("Начало работы!");

            List<string> listDir = new List<string>();

            #region получение значений из конфига
            var a = ConfigurationManager.AppSettings.Get("KeyA");
            //var a = ConfigurationManager.AppSettings["KeyA"];
            var b = ConfigurationManager.AppSettings.Get("KeyB");
            var c = ConfigurationManager.AppSettings.Get("KeyC");

            //получить сразу все ключи
            var settings = ConfigurationManager.AppSettings;
            foreach (var key in settings.AllKeys)
            {
                Console.WriteLine(settings.Get(key));
            }

            //получение значения из своей секции customSection, для этого необходимо в шапке конфига объявить customSection
            var customValue1 = (ConfigurationManager.GetSection(SectionSettingsCheckLifeTimeFiles) as NameValueCollection).Get(IntervalCheckLifeTimeFiles);
            int intervalCheckLifeTimeFiles = DefaultIntervalCheckLifeTimeFiles;
            bool success = int.TryParse(customValue1, out intervalCheckLifeTimeFiles);
            if (!success)
                ComData.logger.Error($"Не удалось преобразовать параметр '{IntervalCheckLifeTimeFiles}' с полученным из конфигурации значением '{customValue1}' в числовое значение, присвоено значение по умолчанию '{DefaultIntervalCheckLifeTimeFiles}'");
            var customValue2 = (ConfigurationManager.GetSection(SectionSettingsCheckLifeTimeFiles) as NameValueCollection).Get(AllowLifeTimeFiles);
            int allowLifeTimeFiles = DefaultAllowLifeTimeFiles;
            success = int.TryParse(customValue2, out allowLifeTimeFiles);
            if (!success)
                ComData.logger.Error($"Не удалось преобразовать параметр '{AllowLifeTimeFiles}' с полученным из конфигурации значением '{customValue2}' в числовое значение, присвоено значение по умолчанию '{DefaultAllowLifeTimeFiles}'");


            //получение коллекции из конфигурации
            StartupFoldersConfigSection section = (StartupFoldersConfigSection)ConfigurationManager.GetSection("StartupFolders");

            if (section != null)
            {
                //Console.WriteLine(section.FolderItems[0].FolderKey);
                //Console.WriteLine(section.FolderItems[0].Path);

                foreach (FolderElement path in section.FolderItems)
                {
                    listDir.Add(path?.Path);
                }
            }

            WriteInfo($"Из конфигурационного файла получены параметры для работы по проверке временных меток файлов:" +
                $"{Environment.NewLine}Интервал проверки IntervalCheckLifeTimeFiles: '{intervalCheckLifeTimeFiles}'." +
                $"{Environment.NewLine}Допустимое время жизни файлов: '{allowLifeTimeFiles}'" +
                $"{Environment.NewLine}Папки для анализа находящихся в них файлов: '{string.Join(", ", listDir)}'");

            #endregion

            //словарь с директориями из настройки и информацией по файлам в них
            Dictionary<string, List<InfoLifeTimeFiles>> dicDirCheck = new Dictionary<string, List<InfoLifeTimeFiles>>();
            //заполнение словаря ключами
            foreach (string dir in listDir)
            {
                dicDirCheck.Add(dir, new List<InfoLifeTimeFiles>());
            }

            SettingsCheckLifeTimeFiles setCheckLifeTimeFiles = new SettingsCheckLifeTimeFiles { IntervalCheckLifeTimeFiles = intervalCheckLifeTimeFiles, AllowLifeTimeFiles = allowLifeTimeFiles, DicDirsCheck = dicDirCheck };
            workCheckLifeTimeFiles = new WorkCheckLifeTimeFiles(setCheckLifeTimeFiles);
            workCheckLifeTimeFiles.timerCkeckLifeTimeFiles = new Timer(new TimerCallback(OnTimerCkeckLifeTimeFiles), null, 0, Timeout.Infinite);

            Console.WriteLine("Для завершения программы нажмите любую кнопку на клавиатуре!");
            Console.ReadKey();

            ComData.logger.Info("Завершение работы программы!");
            workCheckLifeTimeFiles.timerCkeckLifeTimeFiles.Change(Timeout.Infinite, Timeout.Infinite);
            workCheckLifeTimeFiles.timerCkeckLifeTimeFiles.Dispose();
        }

        private static void WriteInfo(string strInfo)
        {
            Console.WriteLine(strInfo);
            ComData.logger.Info(strInfo);
        }
        private static void WriteError(string strError, Exception ex = null)
        {
            Console.WriteLine(strError);
            ComData.logger.Error(ex, strError);
        }

        /// <summary>
        /// Проверка временных метрик файлов по таймеру, что они не превысили допустимого значения
        /// </summary>
        /// <param name="state"></param>
        private static void OnTimerCkeckLifeTimeFiles(object obj)
        {
            bool? resWork = workCheckLifeTimeFiles.StartCheckLifeTimeFiles();

            if (resWork == true)
                Console.WriteLine("Нет файлов, время жизни которых превышено.");
            else if (resWork == false)
                Console.WriteLine("Есть файлы, время жизни которых превышено, завершение работы процесса проверки файлов.");
            else if (resWork == null)
                Console.WriteLine("Ошибка при проверке времени жизни файлов, завершение работы процесса проверки файлов!");
        }
    }
}

