using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EF_OrigCatalog_Nissan_Test
{
    class Program
    {
        public static object getRandomEngineLock = new object();
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    List<Elcats_account> accounts = null;
                    using (var nissanDb = new NissanDbDataContext())
                    {
                        accounts = nissanDb.Elcats_accounts.ToList();
                    }
                    if (accounts == null)
                        throw new Exception("Error occurs while trying to access account table");
                    Parallel.ForEach(accounts, new ParallelOptions { MaxDegreeOfParallelism = 1 /*accounts.Count*/ }, (a) =>
                    {
                        using (var p = new Parser(a))
                        {
                            p.MainLoopAsync().Wait();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.WriteLogException(ex);
                }
            }
        }
    }
}
