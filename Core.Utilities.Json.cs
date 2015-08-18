using System.Net;

namespace Core.Utilities
{
    public static class Json
    {
        public static string GetJsonString(string url, string username = null, string password = null, bool addExplicitAcceptHeader = false)
        {
            string result;

            using (var webClient = new WebClient())
            {
                // Add Network Credentials id Username and Password
                if (username != null && password != null)
                {
                    webClient.Credentials = new NetworkCredential(username, password);
                }

                if (addExplicitAcceptHeader)
                {
                    webClient.Headers.Add("Accept", "application/json");
                }

                result = webClient.DownloadString(url);
            }
            return result;
        }

        public static string GetJsonString(string url, string token, bool addExplicitAcceptHeader = false)
        {
            string result;

            using (var webClient = new WebClient())
            {
                // Add token
                if (token != null)
                {
                    webClient.Headers.Add("Authorization", "Bearer " + token);
                }

                if (addExplicitAcceptHeader)
                {
                    webClient.Headers.Add("Accept", "application/json");
                }

                result = webClient.DownloadString(url);
            }
            return result;
        }


        public static HttpWebResponse GetHttpWebResponse(string url)
        {
            //requesting the particular web page    
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            //geting the response from the request url    
            return (HttpWebResponse)httpRequest.GetResponse();
        }

        public static string SendJsonString(string json, string url, string username = null, string password = null, bool usePutMethod = true)
        {
            string result;
            var method = "PUT";

            using (var webClient = new WebClient())
            {
                // Add Network Credentials id Username and Password
                if (username != null && password != null)
                {
                    webClient.Credentials = new NetworkCredential(username, password);
                }

                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";

                //Check to see which Method you like to use
                if (!usePutMethod)
                {
                    method = "POST";
                }

                try
                {
                    //PUT Data to url
                    result = webClient.UploadString(url, method, json);
                }
                catch (WebException ex)
                {
                    result = ex.Message;
                }

            }

            return result;
        }

        public static string SendJsonString(string json, string url, string token, bool usePutMethod = true)
        {
            string result;
            var method = "PUT";

            using (var webClient = new WebClient())
            {
                // Add token
                if (token != null)
                {
                    webClient.Headers.Add("Authorization", "Bearer " + token);
                }

                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";

                //Check to see which Method you like to use
                if (!usePutMethod)
                {
                    method = "POST";
                }

                try
                {
                    //PUT Data to url
                    result = webClient.UploadString(url, method, json);
                }
                catch (WebException ex)
                {
                    result = ex.Message;
                }

            }

            return result;
        }
    }
}
