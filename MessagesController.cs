using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;



namespace Bot_Application6
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                const string apiKey = "ba1daac461034aeb8cc805ee17ea286";
                string queryUri = "https://api.cognitive.microsoft.com/bing/v5.0/images/search"+"?q="+activity.Text+"&imageType=AnimatedGif"; //parameter to filter by GIF image type
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey); //authentication header to pass the API key
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                string bingRawResponse = null;
                BingImageSearchResponse bingJsonResponse = null;

                try
                {
                    bingRawResponse = await client.GetStringAsync(queryUri);
                    bingJsonResponse = JsonConvert.DeserializeObject<BingImageSearchResponse>(bingRawResponse);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Does anything come here ");
             
                    //add code to handle exceptions while calling the REST endpoint and/or deserializing the object
                }

                ImageResult[] imageResult = bingJsonResponse.value;
                if (imageResult == null || imageResult.Length == 0)
                {
                    //add code to handle the case where results are null or zero
                }
                string firstResult = imageResult[0].contentUrl; //we only need the first result for this example

                var replyMessage = activity.CreateReply();
                replyMessage.Recipient = activity.From;
                replyMessage.Type = ActivityTypes.Message;
                replyMessage.Text = $"Here is what i found:";
                replyMessage.Attachments = new System.Collections.Generic.List<Attachment>();
                replyMessage.Attachments.Add(new Attachment()
                {
                    ContentUrl = firstResult,
                    ContentType = "image/png"
                });
                ///Console.WriteLine("ContentUrl");
                
                //Reply to user message with image attachment
                await connector.Conversations.ReplyToActivityAsync(replyMessage);
               
                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }


    public class BingImageSearchResponse
    {
        public string _type { get; set; }
        public int totalEstimatedMatches { get; set; }
        public string readLink { get; set; }
        public string webSearchUrl { get; set; }
        public ImageResult[] value { get; set; }
    }

    public class ImageResult
    {
        public string name { get; set; }
        public string webSearchUrl { get; set; }
        public string thumbnailUrl { get; set; }
        public object datePublished { get; set; }
        public string contentUrl { get; set; }
        public string hostPageUrl { get; set; }
        public string contentSize { get; set; }
        public string encodingFormat { get; set; }
        public string hostPageDisplayUrl { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string accentColor { get; set; }
    }
}