using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Collections.Generic;
using System.IO;
using VideoFrameAnalyzer;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace Bot_Application1
{

    internal class Program
    {
        private static void Main(string[] args)
        {
            // Create grabber. 
            FrameGrabber<Face[]> grabber = new FrameGrabber<Face[]>();

            // Create Face API Client.
            FaceServiceClient faceClient = new FaceServiceClient("b84106a40d9f4bf789f458a9ee3fc7e2");

            // Set up a listener for when we acquire a new frame.
            grabber.NewFrameProvided += (s, e) =>
            {
                Console.WriteLine("New frame acquired at {0}", e.Frame.Metadata.Timestamp);
            };

            // Set up Face API call.
            grabber.AnalysisFunction = async frame =>
            {
                Console.WriteLine("Submitting frame acquired at {0}", frame.Metadata.Timestamp);
                // Encode image and submit to Face API. 
                return await faceClient.DetectAsync(frame.Image.ToMemoryStream(".jpg"));
            };

            // Set up a listener for when we receive a new result from an API call. 
            grabber.NewResultAvailable += (s, e) =>
            {
                if (e.TimedOut)
                    Console.WriteLine("API call timed out.");
                else if (e.Exception != null)
                    Console.WriteLine("API call threw an exception.");
                else
                    Console.WriteLine("New result received for frame acquired at {0}. {1} faces detected", e.Frame.Metadata.Timestamp, e.Analysis.Length);
            };

            // Tell grabber when to call API.
            // See also TriggerAnalysisOnPredicate
            grabber.TriggerAnalysisOnInterval(TimeSpan.FromMilliseconds(3000));

            // Start running in the background.
            grabber.StartProcessingCameraAsync().Wait();

            // Wait for keypress to stop
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            // Stop, blocking until done.
            grabber.StopProcessingAsync().Wait();
        }
    }

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            const string emotionApiKey = "0ff0835aadf24ccfb62571df22cf458e";

            //Vision SDK classes
            EmotionServiceClient visionClient = new EmotionServiceClient(emotionApiKey);
            Emotion[] analysisResult = null;

            if (activity == null || activity.GetActivityType() != ActivityTypes.Message)
            {

                //add code to handle errors, or non-messaging activities
            }

            //If the user uploaded an image, read it, and send it to the Vision API
            if (activity.Attachments.Any() && activity.Attachments.First().ContentType.Contains("image"))
            {
                //stores image url (parsed from attachment or message)
                string uploadedImageUrl = activity.Attachments.First().ContentUrl; ;
                uploadedImageUrl = System.Web.HttpUtility.UrlDecode(uploadedImageUrl.Substring(uploadedImageUrl.IndexOf("file=") + 5));

                using (Stream imageFileStream = File.OpenRead(uploadedImageUrl))
                {
                    try
                    {
                        analysisResult = await visionClient.RecognizeAsync(imageFileStream);
                    }
                    catch (Exception e)
                    {
                        analysisResult = null; //on error, reset analysis result to null
                    }
                }
            }
            //Else, if the user did not upload an image, determine if the message contains a url, and send it to the Vision API
            else
            {
                try
                {
                    analysisResult = await visionClient.RecognizeAsync(activity.Text);
                }
                catch (Exception e)
                {
                    analysisResult = null; //on error, reset analysis result to null
                }
            }

            Activity reply = activity.CreateReply("Did you upload an image? I'm more of a visual person. " +
                                      "Try sending me an image or an image url"); //default reply
            if (analysisResult != null)
            {
                Scores emotionScores = analysisResult[0].Scores;

                //Retrieve list of emotions for first face detected and sort by emotion score (desc)
                IEnumerable<KeyValuePair<string, float>> emotionList = new Dictionary<string, float>()
        {
            { "angry", emotionScores.Anger},
            { "contemptuous", emotionScores.Contempt },
            { "disgusted", emotionScores.Disgust },
            { "frightened", emotionScores.Fear },
            { "happy", emotionScores.Happiness},
            { "neutral", emotionScores.Neutral},
            { "sad", emotionScores.Sadness },
            { "surprised", emotionScores.Surprise}
        }
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .ToList();

                KeyValuePair<string, float> topEmotion = emotionList.ElementAt(0);
                string topEmotionKey = topEmotion.Key;
                float topEmotionScore = topEmotion.Value;

                reply = activity.CreateReply("I found a face! I am " + (int)(topEmotionScore * 100) +
                                             "% sure the person seems " + topEmotionKey);
            }
            await connector.Conversations.ReplyToActivityAsync(reply);
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
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
}