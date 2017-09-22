using AngleSharp.Dom.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EF_OrigCatalog_Nissan_Test
{
    class Parser : IDisposable
    {
        Elcats_account _Account;
        Proxy proxy;
        Request request;
        
        public Parser(Elcats_account account)
        {
            _Account = account;
            ChangeProxy();
        }
        void ChangeProxy()
        {
            proxy = GetRandomProxy();
            request = GetRequest(_Account.cookie, proxy);
        }


        //Parser
        public async Task MainLoopAsync()
        {
            while (true)
            {
                try
                {
                    while (true)
                    {
                        var engine = GetRandomEngine();
                        await ParseLvl1Async(engine);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLogException(ex);
                }
            }
        }
        async Task ParseLvl1Async(nissan_engine engine)
        {
            try
            {
                var html = await GetAsync_EternalCycle(engine.link);
                using (PageDocument doc = new PageDocument(html))
                {
                    var lvl2_list = doc.Select_All("div.CTreeView>ul>ul>li>a");
                    foreach (IHtmlAnchorElement lvl2 in lvl2_list)
                    {
                        string lvl2_name = lvl2.TextContent;
                        List<string> js_params = PageDocument.GetJSLinkParams(lvl2).ToList();
                        var lvl1_parent = doc.Select_Single(string.Format("div.CTreeView>ul>li[name='{0}']>a", js_params[1]));
                        string lvl1_name = lvl1_parent.TextContent;
                        var lvl1_id = AddLvl1(lvl1_name);
                        var lvl2_id = AddLvl2(lvl2_name, Convert.ToInt32(lvl1_id));

                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
        }
        string AddLvl1(string name)
        {
            while (true)
            {
                try
                {
                    using (var nissanDb = new NissanDbDataContext())
                    {
                        nissan_lvl1 new_lvl1 = new nissan_lvl1();
                        new_lvl1.name = name;

                        if (!nissanDb.nissan_lvl1s.Any(x => x.name == name))
                        {
                            nissanDb.nissan_lvl1s.InsertOnSubmit(new_lvl1);
                            nissanDb.SubmitChanges();
                        }
                        var lvl1 = nissanDb.nissan_lvl1s.FirstOrDefault(x => x.name == name);
                        if (lvl1 != null)
                            return lvl1.id.ToString();
                        //else return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLogException(ex);
                }
            }
            return null;

        }
        string AddLvl2(string name, int lvl1_id)
        {
            while (true)
            {
                try
                {
                    using (var nissanDb = new NissanDbDataContext())
                    {
                        nissan_lvl2 new_lvl2 = new nissan_lvl2();
                        new_lvl2.name = name;
                        new_lvl2.lvl1_id = lvl1_id;

                        if (!nissanDb.nissan_lvl2s.Any(x => x.name == name && x.lvl1_id == lvl1_id))
                        {
                            nissanDb.nissan_lvl2s.InsertOnSubmit(new_lvl2);
                            nissanDb.SubmitChanges();
                        }
                        var lvl2 = nissanDb.nissan_lvl2s.FirstOrDefault(x => x.name == name && x.lvl1_id == lvl1_id);
                        if (lvl2 != null)
                            return lvl2.id.ToString();
                        //else return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLogException(ex);
                }
            }
            return null;

        }
        async Task<string> GetAsync_EternalCycle(string url)
        {
            string content = null;
            while (content == null)
            {
                content = await request.GetRequestAsync(url);
                if (content == null)
                {
                    ChangeProxy();
                }
            }
            return content;
        }

        
        //Static func
        static Request GetRequest(string account, Proxy proxy)
        {
            List<KeyValuePair<string, string>> cookies = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>(".CAUTH", account)
                };
            var request = new Request(proxy.host, proxy.port, proxy.username, proxy.password,
                Request.GetCookieContainer(cookies, "www.japancats.ru"), TimeSpan.FromMinutes(5), Request.GetHeaders());
            if (request != null)
                return request;
            else
                throw new Exception("Cant build request object for some reason");
        }
        static Proxy GetRandomProxy()
        {
            using (var nissanDb = new NissanDbDataContext())
            {
                var proxy_count = nissanDb.Proxies.Count();
                int index = new Random().Next(proxy_count);
                var proxy = nissanDb.Proxies.Skip(index).FirstOrDefault();
                if (proxy != null)
                    return proxy;
                else
                    throw new Exception("Error occurs while trying to access proxy table");
            }
        }
        static nissan_engine GetRandomEngine()
        {
            lock (Program.getRandomEngineLock)
            {
                using (var nissanDb = new NissanDbDataContext())
                {
                    var engine = nissanDb.nissan_engines.FirstOrDefault(x => x.Is_Done == 0);
                    if (engine != null)
                    {
                        engine.Is_Done = 2;
                        nissanDb.SubmitChanges();

                        return engine;
                    }
                    else
                        throw new Exception("Error occurs while trying to access engine table");
                }
            }

        }
        public static void GetDateFormat(string original_str, out string date_start, out string date_end)
        {
            if (string.IsNullOrWhiteSpace(original_str))
            {
                date_start = null;
                date_end = null;
                return;
            }
            try
            {
                if (!(original_str.Contains("-") || original_str.Contains(".")))
                {
                    date_start = null;
                    date_end = null;
                    date_start = original_str.Trim();
                }
                else
                {
                    date_start = null;
                    date_end = null;

                    var split = original_str.Split('-');
                    if (split[0] == "...")
                        date_start = null;
                    else
                        date_start = split[0].Trim();
                    if (split[1] == "...")
                        date_end = null;
                    else
                        date_end = split[1].Trim();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
                date_start = null;
                date_end = null;
            }

        }

        public void Dispose()
        {
            _Account = null;
            proxy = null;
            request.Dispose();
        }
    }

}
