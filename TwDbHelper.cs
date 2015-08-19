using System;
using LinqToTwitter;
using Newtonsoft.Json;
using Core.Domain;
using Core.Data;
using Integration.Twitter.Domain;

namespace Integration.Twitter
{
    public static class TwDbHelper
    {
        public static TwitterContext GetContext()
        {
            const string _key = "enterKey";
            const string _secret = "enterSecret";
            const string _token = "enterToken";
            const string _tokenSecret = "enterTokenSecret";

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
        
        //Gets tweets and updates azure database
        public static string GetNewMaxIDandUpdateDB(string twitterAccountToDisplay, ulong sinceID = 544516702412723892)
        {
            string currentDate = DateTime.Now.ToString();
            string message = "";

            var doTask = GetSearchTweetsAsync(twitterAccountToDisplay, sinceID);
            doTask.Wait();

            message = "Please Print/Save this information for future records.\n Now the database is updated for "
                          + twitterAccountToDisplay + " with previous StatusID (" + sinceID.ToString() + ") at "
                          + currentDate + ".\n Now the new StatusID is " + GetNewMaxID(twitterAccountToDisplay).ToString();

            return message;
        }
        
        //Gets new maxID
        public static ulong GetNewMaxID(string twitterAccountToDisplay)
        {
            //string newStatusID = "No MaxID found"; //UInt64
            ulong newMaxID = 563722001925218304; // 544516702412723892;
            try
            {
                using (var db = new OnlyTweetContext())
                {
                    //Get only Status Tweet with New MaxID/StatusID from the database 
                    var query = (from t in db.OnlyTweetDB
                                 where t.ScreenName == twitterAccountToDisplay
                                 orderby t.CreatedAt descending
                                 select new
                                 {
                                     maxID = t.StatusID
                                 }).Take(1);

                    //Console.WriteLine("All Status Tweet in the database:");
                    foreach (var item in query)
                    {
                        //Console.WriteLine("New MaxID is: {0}", item.StatusID);                       
                        //newStatusID = item.maxID.ToString();
                        newMaxID = (ulong)item.maxID;
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                string innerMessage = (ex.InnerException != null) ? ex.InnerException.Message : String.Empty;
                //Console.WriteLine("{0}\n{1}", ex.Message, innerMessage);
            }

            return newMaxID; // newStatusID;
        }
        
        //Gets tweets asynchronously
        public static async Task GetSearchTweetsAsync(string twitterAccountToDisplay, ulong sinceID)
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
                         search.Query == "from:@" + twitterAccountToDisplay + " -retweets" && // -filter:retweets // -RT until:2012-10-26
                         search.Count == count
                     select search)
                    .SingleOrDefaultAsync();

                if (searchResponse != null && searchResponse.Statuses != null)
                {
                    List<Status> newStatuses = searchResponse.Statuses;
                    // first tweet processed on current query
                    maxID = newStatuses.Min(status => status.StatusID) - 1;
                    statusList.AddRange(newStatuses);

                    //adding if condition to fix Error : LinqToTwitter.TwitterQueryException: Missing or invalid url parameter 
                    //as SinceID is greater than MaxID, which is wrong according to the query.
                    if (sinceID < maxID)
                    {

                        do
                        {
                            // now add sinceID and maxID
                            searchResponse =
                                await
                                (from search in twitterCtx.Search
                                 where search.Type == SearchType.Search &&
                                       search.Query == "from:@" + twitterAccountToDisplay + " -retweets" && // -filter:retweets // -RT
                                       search.Count == count &&
                                     //search.Until == new DateTime(2014,12,1) &&
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
                    }

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
        
        //Map tweets to list before writing to database
        public static List<OnlyTweet> SetTweetsOnly(IEnumerable<Status> statusTweets)  //IQueryable
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
        }
        
        //Write tweets to azure database
        public static bool SetTweetsOnlyToDB(List<OnlyTweet> sTweetsOnlyList)
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

        // Gets Twitter Feed from azure database
        public static ICollection<StatusTweet> GetTweetsOnlyFeedFromDB(string twitterAccountToDisplay)
        {
            var stList = new List<StatusTweet>();
            try
            {
                using (var dbContext = new OnlyTweetContext())
                {
                    dbContext.Database.Connection.Open();
                    //Get top 20 Status Tweets from the database 
                    var query = (from t in dbContext.OnlyTweetDB
                                 where t.ScreenName == twitterAccountToDisplay
                                 orderby t.CreatedAt descending
                                 select t).Take(20);

                    //Console.WriteLine("All Status Tweet in the database:");
                    foreach (var item in query)
                    {
                        var st = new StatusTweet();
                        //Console.WriteLine("{0}:\n{1}\n\t{2}\n\t{3}\n\", item.StatusID, item.ScreenName, item.Text, item.CreatedAt);

                        st.Text = item.Text;
                        st.CreatedAt = item.CreatedAt;
                        st.StatusID = item.StatusID;
                        st.ScreenName = item.ScreenName;
                        st.StringStatusID = item.StatusID.ToString();

                        switch (item.ScreenName)
                        {
                            case "Linq2Twitr": //Sample Username: https://twitter.com/Linq2Twitr
                                st.ProfileImageUrlHttps = "https://pbs.twimg.com/profile_images/378800000625948439/57f4351535721aeedc632745ceaacfea_400x400.png";
                                break;
                            default:
                                st.ProfileImageUrlHttps = "";
                                break;
                        }

                        stList.Add(st);
                    }

                }
            }
            catch (Exception ex)
            {
                string innerMessage = (ex.InnerException != null) ? ex.InnerException.Message : String.Empty;
                //Console.WriteLine("{0}\n{1}", ex.Message, innerMessage);
            }

            return stList.ToList();
        }

    }
}
