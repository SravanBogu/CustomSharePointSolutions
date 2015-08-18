using Newtonsoft.Json.Linq;
using System;

namespace Integration.Facebook
{
    public static class FbHelper
    {
        //returns Facebook Feed
        public static ICollection<FbFeedItem> ReadFeed(string fbUserId)
        {
            /***
             * Graph API v2.4
             * /{user-id}/feed 
             * The feed of posts (including status updates) and links published by this person, or by others on this person's profile. There are other edges which provide filtered versions of this edge:
             * /{user-id}/links shows only the links that were published by this person.
             * /{user-id}/posts shows only the posts that were published by this person.
             * /{user-id}/statuses shows only the status update posts that were published by this person.
             * /{user-id}/tagged shows only the posts that this person was tagged in.
             * 
             ***/

            const string UriJson = "https://graph.facebook.com/v2.4/{0}/posts?fields=message,description,created_time,id,name,link,picture,from,status_type&limit=20&access_token=1644304715814486%7CSFV9STcNi0uYs-1Ivqa7_Sbe19E";
            
            var returnFbFeedItems = new List<FbFeedItem>();

            try
            {
                var fbJsonString = Core.Utilities.Json.GetJsonString(String.Format(UriJson, fbUserId), null, null, true);
                //create a stream to hold the contents of the response and create Facebook Object 
                var fbObject = JsonConvert.DeserializeObject<FbFeedJson>(fbJsonString);

                if (fbObject == null || !fbObject.Data.Any()) return returnFbFeedItems;

                else
                {
                    if (fbObject.Data != null)
                    {
                        //return feed
                        returnFbFeedItems = fbObject.Data.Select(fbItem => new FbFeedItem
                        {
                            Message = fbItem.Message,
                            Description = fbItem.Description,
                            CreatedTime = fbItem.CreatedTime,
                            PostId = fbItem.Id,
                            Picture = fbItem.Picture,
                            Link = fbItem.Link,
                            Name = fbItem.Name,
                            FromName = fbItem.From.Name,
                            FromId = fbItem.From.Id,
                            StatusType = fbItem.StatusType
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex);
            }
            return returnFbFeedItems.ToList();
        }
    }
}
