using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using ConsoleAppCheckLifeTimeFiles.Models;
using NLog;

namespace ConsoleAppCheckLifeTimeFiles
{
    class Program
    {
        // Интервал проверки времени жизни файла в секундах
        const int IntervalCheckLifeTimeFiles = 10;
        //допустимое время жизни файла в секундах
        const int AllowLifeTimeFiles = 30; //2 минуты

        private static Logger logger = LogManager.GetCurrentClassLogger();


        private static Timer timerCkeckLifeTimeFiles;
        static void Main(string[] args)
        {
            WriteInfo("Начало работы!");

            List<string> listDir = new List<string>();
            listDir.Add(@"D:\Test\1");
            listDir.Add(@"D:\Test\2");
            listDir.Add(@"D:\Test\3");
            listDir.Add(@"D:\Test\4");

            //словарь с директориями из настройки и информацией по файлам в них
            Dictionary<string, List<InfoLifeTimeFiles>> dicDirCheck = new Dictionary<string, List<InfoLifeTimeFiles>>();
            //заполнение словаря ключами
            foreach (string dir in listDir)
            {
                dicDirCheck.Add(dir, new List<InfoLifeTimeFiles>()); 
            }
            
            timerCkeckLifeTimeFiles = new Timer(new TimerCallback(OnTimerCkeckLifeTimeFiles), dicDirCheck, 0, Timeout.Infinite);

            Console.WriteLine("Для завершения программы нажмите любую кнопку на клавиатуре!");
            Console.ReadKey();

            logger.Info("Завершение работы программы!");
            timerCkeckLifeTimeFiles.Change(Timeout.Infinite, Timeout.Infinite);
            timerCkeckLifeTimeFiles.Dispose();
        }

        private static void WriteInfo(string strInfo)
        {
            Console.WriteLine(strInfo);
            logger.Info(strInfo);
        }
        private static void WriteError(string strError, Exception ex = null)
        {
            Console.WriteLine(strError);
            logger.Error(ex, strError);
        }

        /// <summary>
        /// Проверка временных метрик файлов по таймеру, что они не превысили допустимого значения
        /// </summary>
        /// <param name="state"></param>
        private static void OnTimerCkeckLifeTimeFiles(object state)
        {
            Dictionary<string, List<InfoLifeTimeFiles>>  dicDirCheck = state as Dictionary<string, List<InfoLifeTimeFiles>>;

            bool? isCkeckLifeTimeFiles = IsCkeckLifeTimeFiles(dicDirCheck);

            if (isCkeckLifeTimeFiles == true)
                WriteInfo("Нет файлов, время жизни которых превышено.");                
            else if(isCkeckLifeTimeFiles == false)
            {
                WriteError("Есть файлы, время жизни которых превышено, завершение работы программы.");
                Environment.Exit(0);
            }
            else
            {
                WriteError("Ошибка при проверке времени жизни файлов, завершение работы программы!");
                Environment.Exit(0);
            }

            //запуск очередной проверки через заданный интервал времени IntervalCheckLifeTimeFiles
            timerCkeckLifeTimeFiles.Change(IntervalCheckLifeTimeFiles * 1000, Timeout.Infinite);
        }

        /// <summary>
        /// Проверка временных метрик файлов, что они не превысили допустимого значения
        /// </summary>
        /// <returns>
        /// true  - успешная проверка, нет файлов, у которых истекло время жизни файлов
        /// false - неудача - есть файлы, у которых превышено время жизни файлов
        /// null  - при выполнении задания произошла ошибка
        /// </returns>
        private static bool? IsCkeckLifeTimeFiles(Dictionary<string, List<InfoLifeTimeFiles>> dicDirCheck)
        {
            bool? res = true;

            try
            {
                //после запуска обновить ключи нельзя
                List<string> keysDicDirCheck = new List<string>(dicDirCheck.Keys);
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
                                WriteError($"Обнаружен файл {fileNew}, время жизни которого превысило допустимое значение. " +
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
                    lock(dicDirCheck[dirDic])
                    {
                        dicDirCheck[dirDic] = listInfoLifeTimeFilesNew;
                    }
                }

                return res;
            }
            catch(Exception ex)
            {
                WriteError("Ошибка при проверке временных метрик файлов, что они не превысили допустимого значения", ex);
                return null;
            }
        }
    }
}
