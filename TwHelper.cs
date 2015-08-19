using System;
using LinqToTwitter;
using Newtonsoft.Json;
using Core.Domain;
using Core.Data;
using Integration.Twitter.Domain;

namespace Integration.Twitter
{
    public class TwHelper
    {
        private readonly string _key = string.Empty;
        private readonly string _secret = string.Empty;
        private readonly string _token = string.Empty;
        private readonly string _tokenSecret = string.Empty;

        public Helper(string key, string secret, string token, string tokenSecret)
        {
            _key = key;
            _secret = secret;
            _token = token;
            _tokenSecret = tokenSecret;
        }

        private TwitterContext GetContext()
        {
            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = _key,
                    ConsumerSecret = _secret,
                    AccessToken = _token,
                    AccessTokenSecret = _tokenSecret
                }
            };

            return new TwitterContext(auth);
        }

        /// <summary>
        /// Gets User Tweets using Search API
        /// </summary>
        /// <param name="twitterCtx"></param>
        /// <param name="twitterAccountToDisplay"></param>
        /// <returns></returns>
        public async Task GetSearchTweetsAsync(string twitterAccountToDisplay, ulong sinceID)
        {
            try
            {
                var twitterCtx = GetContext();
                //string twitterAccountToDisplay = "Linq2Twitr"; 
                int count = 5;
                int maxStatuses = 30;
                int lastStatusCount = 0;
                //last tweet processed on previous query set
                //ulong sinceID = 544516702412723892;
                ulong maxID;
                var statusList = new List<Status>();

                //only count
                var searchResponse =
                    await
                    (from search in twitterCtx.Search
                     where search.Type == SearchType.Search &&
                         search.Query == "from:@" + twitterAccountToDisplay + " -retweets" && // -filter:retweets // -RT
                         search.Count == count
                     select search)
                    .SingleOrDefaultAsync();

                if (searchResponse != null && searchResponse.Statuses != null)
                {
                    List<Status> newStatuses = searchResponse.Statuses;
                    // first tweet processed on current query
                    maxID = newStatuses.Min(status => status.StatusID) - 1;
                    statusList.AddRange(newStatuses);

                    do
                    {
                        // now add sinceID and maxID
                        searchResponse =
                            await
                            (from search in twitterCtx.Search
                             where search.Type == SearchType.Search &&
                                   search.Query == "from:@" + twitterAccountToDisplay + " -retweets" && // -filter:retweets // -RT
                                   search.Count == count &&
                                   search.SinceID == sinceID &&
                                   search.MaxID == maxID
                             select search)
                            .SingleOrDefaultAsync(); //.ToList();

                        if (searchResponse == null)
                            break;

                        if (searchResponse.Count > 0 && searchResponse.Statuses.Count > 0)
                        {
                            newStatuses = searchResponse.Statuses;
                            // first tweet processed on current query
                            maxID = newStatuses.Min(status => status.StatusID) - 1;
                            statusList.AddRange(newStatuses);

                            lastStatusCount = newStatuses.Count;
                        }
                        if (searchResponse.Count > 0 && searchResponse.Statuses.Count == 0)
                        {
                            lastStatusCount = 0;
                        }
                    }
                    while (lastStatusCount != 0 && statusList.Count < maxStatuses); //(searchResponse.Count != 0 && statusList.Count < 30);

                    //searchResponse.Statuses.ForEach(tweet => Console.WriteLine( "User: {0}, Tweet: {1}", tweet.User.ScreenNameResponse, tweet.Text));

                    var tweetsOnlyList = from status in statusList //searchResponse.Statuses
                                         //Exclude replies and retweets
                                         where status.InReplyToScreenName == null && status.IncludeMyRetweet == false
                                         select new Status()
                                         {
                                             Text = status.Text,
                                             ScreenName = status.User.ScreenNameResponse,
                                             StatusID = status.StatusID,
                                             CreatedAt = status.CreatedAt
                                             //ProfileImageUrlHttps = status.ProfileImageUrlHttps
                                         };

                    //To display Tweets
                    //foreach (var tweet in tweetsOnlyList)
                    //{
                    //    Console.WriteLine("User: {0},\n Tweet: {1},\n maxID: {2},\n CreatedAt: {3}\n",
                    //                       tweet.ScreenNameResponse,
                    //                       tweet.Text,
                    //                       tweet.StatusID,
                    //                       tweet.CreatedAt);
                    //}

                    //Set Tweets to Database
                    SetTweetsOnly(tweetsOnlyList);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex);
            }
        }

        /// <summary>
        /// Sets User Tweets from GetSearchTweetsAsync
        /// </summary>
        /// <param name="statusTweets"></param>
        /// <returns></returns>
        private List<OnlyTweet> SetTweetsOnly(IEnumerable<Status> statusTweets)  //IQueryable
        {
            var sTweetsOnlyList = new List<OnlyTweet>();

            try
            {
                // map to list
                sTweetsOnlyList = statusTweets
                            .Select(sTweet => new OnlyTweet()
                            {
                                CreatedAt = sTweet.CreatedAt,
                                ScreenName = sTweet.ScreenName,
                                Text = sTweet.Text,
                                StatusID = Convert.ToInt64(sTweet.StatusID)
                            })
                            .ToList();

                //Thread.Sleep(1000);

                //Write to Database
                SetTweetsOnlyToDB(sTweetsOnlyList);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex);
            }

            return sTweetsOnlyList;
            //return new List<OnlyTweet>();
        }

        /// <summary>
        /// Stores Twitter Feed to database
        /// </summary>
        /// <param name="sTweetsOnlyList"></param>
        /// <returns></returns>
        public bool SetTweetsOnlyToDB(List<OnlyTweet> sTweetsOnlyList)
        {
            var sTweetExist = false;
            try
            {
                using (var db = new OnlyTweetContext())
                {
                    // Create and save a new status tweet 
                    //Console.Write("Text for new status tweet : ");

                    int count = 0;
                    foreach (var statusTweet in sTweetsOnlyList)
                    {
                        if (statusTweet != null)
                        {
                            //Console.WriteLine((count + 1) + ". {0}-{1}\n\t{2}", statusTweet.CreatedAt, statusTweet.Text, statusTweet.ScreenName);

                            // Check if Status Tweets already exists in the database 
                            var query = from t in db.OnlyTweetDB
                                        where t.Text == statusTweet.Text
                                        orderby t.Text
                                        select t;
                            sTweetExist = false;

                            //Console.WriteLine("Reading all Status Tweets in the database...");
                            foreach (var item in query)
                            {
                                if (String.Equals(item.Text, statusTweet.Text) && String.Equals(item.CreatedAt, statusTweet.CreatedAt))
                                {
                                    // Status Tweet already exists, do not duplicate
                                    Console.WriteLine("Status tweet already exists: \n{0}", statusTweet.Text);
                                    sTweetExist = true;
                                }

                            }

                            //Write Status Tweet to database if title doesn't exist in Db
                            if (!sTweetExist)
                            {
                                var sTweet = new OnlyTweet
                                {
                                    CreatedAt = statusTweet.CreatedAt,
                                    ScreenName = statusTweet.ScreenName,
                                    Text = statusTweet.Text,
                                    StatusID = statusTweet.StatusID
                                };
                                db.OnlyTweetDB.Add(sTweet);
                                db.SaveChanges();

                                count++;
                                sTweetExist = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("No status tweets found in the database.");
                        }
                    }

                    //Console.WriteLine("Press any key to exit...");
                    //Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                string innerMessage =
                (ex.InnerException != null) ?
                ex.InnerException.Message : String.Empty;
                Console.WriteLine("{0}\n{1}", ex.Message, innerMessage);
            }
            return sTweetExist;
        }
    }
}
