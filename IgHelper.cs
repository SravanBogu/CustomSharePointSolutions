//Helper class
using Newtonsoft.Json;

namespace Integration.Instagram
{
    public class IgHelper
    {
        //returns Instagram object
        public static ICollection<IgFeedItem> ReadFeed(string IgUserId, string AccessToken)
        {
            //gets recent media photos; when count=3, it returns 2 recent photos 
            const string UriJson = "https://api.instagram.com/v1/users/{0}/media/recent/?access_token={1}&count=3";

            var returnIgFeedItems = new List<IgFeedItem>();

            try
            {
                var IgJsonString = Core.Utilities.Json.GetJsonString(String.Format(UriJson, IgUserId, AccessToken), null, null, true);
                //create a stream to hold the contents of the response and create Instagram Object 
                var IgObject = JsonConvert.DeserializeObject<IgFeedJson>(IgJsonString);

                if (IgObject == null || !IgObject.Data.Any()) return returnIgFeedItems;

                else
                {
                    if (IgObject.Data != null)
                    {
                        //return feed
                        returnIgFeedItems = IgObject.Data.Select(IgItem => new IgFeedItem
                        {
                            Caption = IgItem.Caption.Text,
                            Link = IgItem.Link,
                            SmallImage = IgItem.Images.LowResolution.Url,
                            ThumbnailImage = IgItem.Images.Thumbnail.Url,
                            LargeImage = IgItem.Images.StandardResolution.Url,
                            Type = IgItem.Type,
                            CreatedTime = IgItem.CreatedTime
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex);
            }
            return returnIgFeedItems.ToList();
        }
    }
}
