using System;

namespace Integration.Bing
{
    public static class BnHelper
    {
        // Get news results from Bing Search API
        public static List<NewsResult> GetBingNews(string query, string rootUrl, string market, string newsCategory)
        {
            const string _accountKey = "enterAccountKey";
            var newsItems = new List<NewsResult>();

            try
            {
                var bingContainer = new BingAPI.BingSearchContainer(new Uri(rootUrl));
                // Configure bingContainer to use your credentials.
                bingContainer.Credentials = new NetworkCredential(_accountKey, _accountKey);
                // Build the query, limiting to 15 results.
                // var newsQuery = bingContainer.News(query, null, market, null, null, null, null, newsCategory, null);
                var newsQuery = bingContainer.News(query, null, market, null, null, null, null, null, null);
                // Get top 15 news
                newsQuery = newsQuery.AddQueryOption("$top", 15);
                // Run the query and display the results.
                var newsResults = newsQuery.Execute();
                
                newsItems = newsResults
                            .Select(news => new NewsResult()
                            {
                                ContentTitle = Convert.ToString(news.Title),
                                ContentSource = Convert.ToString(news.Source),
                                ContentDescription = Convert.ToString(news.Description),
                                ContentID = (Guid)(news.ID),
                                ContentUrl = Convert.ToString(news.Url),
                                ContentDate = Convert.ToDateTime(news.Date),
                            })
                            .ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex);
            }
            return newsItems;
        }
    }
}
