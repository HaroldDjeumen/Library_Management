using System;
using System;
using System.Data.SQLite;

namespace Librarymanage
{
    public class BotManager
    {
        private string connectionString;
        private int numberOfBots;

        public BotManager(string dbConnectionString, int botsToRun)
        {
            connectionString = dbConnectionString;
            numberOfBots = botsToRun;
        }

       
    }
}

