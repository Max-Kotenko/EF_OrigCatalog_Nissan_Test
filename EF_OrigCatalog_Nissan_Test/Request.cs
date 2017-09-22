using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EF_OrigCatalog_Nissan_Test
{
    class Request: IDisposable
    {
        protected HttpClient httpClient;
        protected WebProxy webProxy;
        protected HttpClientHandler httpClientHandler;

        public Request(string proxy_address, string proxy_port, string proxy_username, string proxy_password, CookieContainer cookies, TimeSpan timeout, List<KeyValuePair<string, string>> Headers)
        {
            InitProxy(proxy_address, proxy_port, proxy_username, proxy_password);
            InitClientHandler(cookies);
            InitHttpClient(timeout);
            InitHeaders(Headers);
        }

        public async Task<string> GetRequestAsync(string url)
        {
            var html = await LoadHtmlFromFileAsync(url);
            if (!string.IsNullOrWhiteSpace(html))
                return html;

            html = await GetAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;
            await SaveHtmlIntoFileAsync(html, url);
            return html;
        }
        public async Task<string> PostRequestAsync(string url, FormUrlEncodedContent webForms)
        {
            try
            {
                using (HttpResponseMessage response = await httpClient.PostAsync(url, webForms))
                {
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();
                    if (IsHtmlValid(html))
                    {
                        return html;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
            return null;
        }

        async Task<string> GetAsync(string url)
        {
            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();
                    if (IsHtmlValid(html))
                    {
                        return html;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
            return null;
        }
        //Save/Load File
        async Task<string> LoadHtmlFromFileAsync(string link)
        {
            try
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    string HTMLfilename = Path.Combine(@"D:\OriginalCatalogs\NISSAN\NISSAN\html", GetMd5Hash(md5Hash, link) + ".HTML");
                    if (File.Exists(HTMLfilename))
                    {
                        using (StreamReader sr = new StreamReader(HTMLfilename))
                        {
                            return await sr.ReadToEndAsync();
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
                return null;
            }
        }
        async Task SaveHtmlIntoFileAsync(string html, string link)
        {
            try
            {
                int i = 0;
                using (MD5 md5Hash = MD5.Create())
                {
                    string HTMLfilename = null;
                    HTMLfilename = Path.Combine(@"D:\OriginalCatalogs\NISSAN\NISSAN\html", GetMd5Hash(md5Hash, link) + ".HTML");

                    while (i++ < 20)
                    {
                        try
                        {
                            if (!File.Exists(HTMLfilename))
                            {
                                using (var PagestreamWriter = new StreamWriter(HTMLfilename, false))
                                {
                                    await PagestreamWriter.WriteLineAsync(html);
                                }
                                return;
                            }
                            else
                                return;
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLogException(ex);
                        }
                    }
                }
                if (i >= 20)
                {
                    Logger.WriteLogText("Так и не удалось сохранить файл.\t" + Environment.NewLine + link);
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
        }
        //Validate HTML
        bool IsHtmlValid(string html)
        {
            return !html.Contains("Сервис временно недоступен");
        }

        //Initialization
        private void InitProxy(string proxy_address, string proxy_port, string proxy_username, string proxy_password)
        {
            try
            {
                string proxyUri = proxy_address + ":" + proxy_port;
                NetworkCredential proxyCreds = null;
                webProxy = null;
                if (string.IsNullOrWhiteSpace(proxy_username) || string.IsNullOrWhiteSpace(proxy_password))
                {
                    webProxy = new WebProxy(proxyUri, false)
                    {
                        UseDefaultCredentials = false
                    };
                }
                else
                {
                    proxyCreds = new NetworkCredential(
                    proxy_username,
                    proxy_password
                    );

                    webProxy = new WebProxy(proxyUri, false)
                    {
                        UseDefaultCredentials = false,
                        Credentials = proxyCreds
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
        }
        private void InitClientHandler(CookieContainer cookies)
        {
            try
            {
                this.httpClientHandler = new HttpClientHandler()
                {
                    Proxy = webProxy,
                    PreAuthenticate = true,
                    UseDefaultCredentials = false,
                    AllowAutoRedirect = true
                };
                this.httpClientHandler.CookieContainer = cookies;
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
        }
        private void InitHttpClient(TimeSpan timeout)
        {
            try
            {
                httpClient = new HttpClient(httpClientHandler)
                {
                    Timeout = timeout
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLogException(ex);
            }
        }
        private void InitHeaders(List<KeyValuePair<string, string>> Headers)
        {
            foreach (var header in Headers)
            {
                try
                {
                    this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    Logger.WriteLogException(ex);
                }
            }
        }

        //Interfaces implement
        public void Dispose()
        {
            if (httpClientHandler != null)
                this.httpClientHandler.Dispose();
            if (httpClient != null)
                this.httpClient.Dispose();
        }

        //Static
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public static CookieContainer GetCookieContainer(List<KeyValuePair<string, string>> cookies, string site)
        {
            CookieContainer cookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                cookieContainer.Add(new Cookie(cookie.Key, cookie.Value, "/", site));
            }
            return cookieContainer;
        }
        public static List<KeyValuePair<string, string>> GetHeaders()
        {
            return new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8"),
                    new KeyValuePair<string, string>("Accept-Language", "ru,en-US;q=0.8,en;q=0.6,uk;q=0.4"),
                    new KeyValuePair<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.101 Safari/537.36")
                };
        }
    }




}
