using Alphaleonis.Win32.Filesystem;
using Common.CheckLifeTimeFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.CheckLifeTimeFiles
{
    public class WorkCheckLifeTimeFiles
    {

        public Timer timerCkeckLifeTimeFiles;
        //Справочник, где ключи - это пути к директориям, значение - список с информацией о файлах в директориях
        private Dictionary<string, List<InfoLifeTimeFiles>> dicDirCheck = new Dictionary<string, List<InfoLifeTimeFiles>>();
        //Интервал проверки времени жизни файла в секундах
        private int IntervalCheckLifeTimeFiles;
        //Допустимое время жизни файла в секундах
        private int AllowLifeTimeFiles;

        public WorkCheckLifeTimeFiles(SettingsCheckLifeTimeFiles settings)
        {
            dicDirCheck = settings.DicDirsCheck;
            IntervalCheckLifeTimeFiles = settings.IntervalCheckLifeTimeFiles;
            AllowLifeTimeFiles = settings.AllowLifeTimeFiles;

            ComData.logger.Info($"Получены параметры для работы по проверке временных меток файлов:" +
                $"{Environment.NewLine}Интервал проверки IntervalCheckLifeTimeFiles: '{IntervalCheckLifeTimeFiles}'." +
                $"{Environment.NewLine}Допустимое время жизни файлов: '{AllowLifeTimeFiles}'" +
                $"{Environment.NewLine}Папки для анализа находящихся в них файлов: '{string.Join(", ", dicDirCheck.Keys)}'");
        }


        /// <summary>
        /// Запуск проверки временных меток для заданных файлов 
        /// и планирование запуска задания через заданный интервал времени
        /// </summary>
        /// <returns>
        /// true - время жизни файлов НЕ истекло
        /// false - время жизни файлов ИСТЕКЛО
        /// null - ошибка в процессе выполнения
        /// </returns>
        public bool? StartCheckLifeTimeFiles()
        {
            try
            {
                bool? isCkeckLifeTimeFiles = IsCkeckLifeTimeFiles(dicDirCheck);

                if (isCkeckLifeTimeFiles == true)
                    ComData.logger.Info("Нет файлов, время жизни которых превышено.");
                else if (isCkeckLifeTimeFiles == false)
                {
                    ComData.logger.Error("Есть файлы, время жизни которых превышено, завершение работы программы.");
                    timerCkeckLifeTimeFiles.Change(Timeout.Infinite, Timeout.Infinite);
                    timerCkeckLifeTimeFiles.Dispose();
                    return false;
                }
                else
                {
                    ComData.logger.Error("Ошибка при проверке времени жизни файлов, завершение работы программы!");
                    timerCkeckLifeTimeFiles.Change(Timeout.Infinite, Timeout.Infinite);
                    timerCkeckLifeTimeFiles.Dispose();
                    return null;
                }

                //запуск очередной проверки через заданный интервал времени IntervalCheckLifeTimeFiles
                timerCkeckLifeTimeFiles.Change(IntervalCheckLifeTimeFiles * 1000, Timeout.Infinite);
                ComData.logger.Info($"Запуск очередной проверки через заданный интервал времени '{IntervalCheckLifeTimeFiles}' сек");

                return true;
            }
            catch(Exception ex)
            {
                ComData.logger.Error(ex, "Ошибка при проверке времени жизни файлов.");
                return null;
            }

        }

        /// <summary>
        /// Проверка временных метрик файлов, что они не превысили допустимого значения
        /// </summary>
        /// <returns>
        /// true  - успешная проверка, нет файлов, у которых истекло время жизни файлов
        /// false - неудача - есть файлы, у которых превышено время жизни файлов
        /// null  - при выполнении задания произошла ошибка
        /// </returns>
        private bool? IsCkeckLifeTimeFiles(Dictionary<string, List<InfoLifeTimeFiles>> dicDirCheck)
        {
            bool? res = true;

            try
            {
                if (dicDirCheck == null)
                    throw new ArgumentException("В метод IsCkeckLifeTimeFiles передан словарь, равный null, такого быть не должно!");

                //после запуска обновить ключи нельзя
                List<string> keysDicDirCheck;                
                lock (dicDirCheck.Keys)
                {
                    keysDicDirCheck = new List<string>(dicDirCheck.Keys);
                }
                DateTime dateNow = DateTime.Now;

                foreach (var dirDic in keysDicDirCheck)
                {
                    List<InfoLifeTimeFiles> listInfoLifeTimeFiles = dicDirCheck[dirDic];
                    List<InfoLifeTimeFiles> listInfoLifeTimeFilesNew = new List<InfoLifeTimeFiles>();

                    string[] files = Directory.GetFiles(dirDic);

                    foreach (string fileNew in files)
                    {
                        //проверяю, был ли этот файл ранее в проверках 
                        InfoLifeTimeFiles resFind = listInfoLifeTimeFiles.Find(fileOld => fileOld.NameFile == fileNew);
                        if (resFind != null)
                        {
                            //раз уже был в проверках, то проверяю разницу текущего времени с временем его появления
                            if (dateNow.Subtract(resFind.AppearanceTimeFile).TotalSeconds >= AllowLifeTimeFiles)
                            {
                                //значит что-то сломалось, надо вернуть ошибку                                
                                ComData.logger.Error($"Обнаружен файл {fileNew}, время жизни которого превысило допустимое значение. " +
                                    $"{Environment.NewLine}Временная метка файла: '{resFind.AppearanceTimeFile}'." +
                                    $"{Environment.NewLine}Текущая дата: '{dateNow}'." +
                                    $"{Environment.NewLine}Разница: '{dateNow.Subtract(resFind.AppearanceTimeFile).TotalSeconds}' сек." +
                                    $"{Environment.NewLine}Допустимое значение: '{AllowLifeTimeFiles}' сек.");

                                return false;
                            }
                            else//допустимое время ещё не прошло
                                listInfoLifeTimeFilesNew.Add(resFind);
                        }
                        else//такого файла еще не было в проверках
                            listInfoLifeTimeFilesNew.Add(new InfoLifeTimeFiles()
                            {
                                NameFile = fileNew,
                                AppearanceTimeFile = dateNow
                            });
                    }

                    listInfoLifeTimeFiles = null;
                    lock (dicDirCheck[dirDic])
                    {
                        dicDirCheck[dirDic] = listInfoLifeTimeFilesNew;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                ComData.logger.Error("Ошибка при проверке временных метрик файлов, что они не превысили допустимого значения", ex);
                return null;
            }
        }

    }
}
