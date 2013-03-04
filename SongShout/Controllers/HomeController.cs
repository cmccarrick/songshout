using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twitterizer;
using BitlyDotNET;
using SongShout.Utility;
using SongShout.Data;
using BitlyDotNET.Implementations;
using BitlyDotNET.Interfaces;
using Formo;


namespace SongShout
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {

            return View();
        }

        public ActionResult Index()
        {



            //   WebClient webClient = new WebClient();
            //  dynamic result = JsonValue.Parse(webClient.DownloadString("http://tinysong.com/s/tiny+dancer?format=json&key=4cf0d5f9a763df9bc64b22cb0ba7c46c"));


            //   string url = result[0].Url;
            //   string SongName = result[0].SongName;

            //New workflow
            //1. load the main page - enhancement - let them choose to send link from either Grooveshark, Rdio or Spotify 
            //2. The user searches for a song via ajax call ( enhancement to store searches and results locally in a document database)
            //3. user fills out form information (twitter handles, message)
            //4. post the form
            //5. check the database to see if we have the users twitter key
            //6. If not put all the form data in cache and redirect to Twitter to get key
            //7. on return from twitter store users credentials and get form items from cache
            //8. store all the tweetsong info in the database and shorten the URL with bit.ly (enhancement - first version only sends directly yo grooveshark)
            //9. create twitter status update on behalf of user merging the fields
            //10. Show success message
            //11. user comes back to the site to play their song.  Page has full user message, link to play song, affiliate links to buy song and ability to send a dedication themselves.


            //     WebClient webClient = new WebClient();
            //   JArray a = JArray.Parse(webClient.DownloadString("http://tinysong.com/s/tiny+dancer?format=json&key=4cf0d5f9a763df9bc64b22cb0ba7c46c"));





            //   ViewBag.Data = a;


            return View();
        }

        public ActionResult SongSubmitted(getSongViewModel model)
        {

            return View(model);

        }

        [HttpPost]
        public ActionResult Index(getSongViewModel model)
        {
            Response.BufferOutput = true;
            bool submitted = false;
            //check db to see if we have Twitter user

            var context = new SongShoutEntities();
            User user = (from s in context.Users where s.ScreenName == model.senderTwitterName select s).FirstOrDefault();
            if (user != null)
            //if (1 == 2)
            {
                //user exists - no need to contact Twitter to get Token
                //Store All message data in the database and send Tweet - Call method

                submitted =  PostMessage(user, model);
                return RedirectToAction("SongSubmitted", model);

            }
            else
            {


                //user does not exist 
                
                //must redirect user to Twitter to grant access and get tokens

 


                //using this handy tool: http://lostechies.com/chrismissal/2013/02/21/introducing-formo-dynamic-configuration/
                dynamic config = new Configuration();

                OAuthTokens tokens = new OAuthTokens();
                tokens.AccessToken = config.AccessToken;
                tokens.AccessTokenSecret = config.AccessTokenSecret;
                tokens.ConsumerKey = config.ConsumerKey;
                tokens.ConsumerSecret = config.ConsumerSecret;





                // Obtain a request token
             //   OAuthTokenResponse requestToken = OAuthUtility.GetRequestToken(config.ConsumerKey, config.ConsumerSecret, "http://localhost:63335/Home/TwitterCallback");
                OAuthTokenResponse requestToken = OAuthUtility.GetRequestToken(config.ConsumerKey, config.ConsumerSecret, "http://www.songshout.com/Home/TwitterCallback");

                // Direct or instruct the user to the following address:
                Uri authorizationUri = OAuthUtility.BuildAuthorizationUri(requestToken.Token);

                model.RequestToken = requestToken.Token;
                //Store form data in cache while gettin info from Twitter
                CacheHelper.Add(model, requestToken.Token);

               // Response.Redirect(authorizationUri.ToString(), true);

                return Redirect(authorizationUri.ToString());


            }


            

            return new EmptyResult();
           
         
              


        }

        public static string Truncate(string source, int length)
        {
            if (source.Length > length)
            {
                source = source.Substring(0, length);
            }
            return source;
        }


        protected bool PostMessage(User u, getSongViewModel model)
        {

            //must call the bit.ly API to get the shortened URL
            //bitly will return a 19 char shortened URL
            //this leaves 121 characters left to tweet.
            //The recipient username will also take space (up to 20 chars max)
            //I would also like to include a #hashtag
            //this would leave about 90 chars for a custom message from the user

            //enhancement to store the url hash in case the same url is returned to save a bit.ly API call.
            //would have to change the .net library since it does not support returning the hash now.


            IBitlyService s = new BitlyService("cmccarrick", "xxx");
            string shortened;
            string songshouturl;
            
           
            if (s.Shorten(model.Url, out shortened) == StatusCode.OK)        
                    {                
                        // do something with "shortened"        
                    }




            if (model.senderTwitterName.Contains("@"))
            {
                model.senderTwitterName = model.senderTwitterName.Replace("@", "");
            }

            if (model.recipientTwitterName.Contains("@"))
            {
                model.recipientTwitterName = model.recipientTwitterName.Replace("@", "");
            }

            var connection = new SongShoutEntities();
            var message = new Data.Message();
            message.CustomMessage = model.Message;
            message.FullMessage = " ";
            message.SenderScreenName = model.senderTwitterName;
            message.RecipientScreenName = model.recipientTwitterName;
            message.SenderUserId = u.UserId;
            message.SongShoutUrl = shortened;
            message.SongUrl = model.Url;
            message.SongId = model.SongId;
            message.SongName = model.SongName;
            message.ArtistName = model.ArtistName;

            connection.Messages.Add(message);
            connection.SaveChanges();


            string url = "http://www.songshout.com/Home/Detail/" + message.MessageId.ToString();

            if (s.Shorten(url, out songshouturl) == StatusCode.OK)
            {
                // do something with "shortened"        
            }

            

            //create the full message
            //format
            //1 + 15 + 94 + 20 + 10
            //@recipientTwitter + Message + songURL + Hashtag
            //94 max chars allowed in the custom message

            StringBuilder sb = new StringBuilder();
            sb.Append("@");
            sb.Append(model.recipientTwitterName);
            sb.Append(" ");
            sb.Append(Truncate(model.Message, 96));
            sb.Append(" ");
            sb.Append(songshouturl);
            sb.Append(" ");
            sb.Append("via @SongShout");
            model.FullMessage = sb.ToString();

            message.FullMessage = model.FullMessage;
            connection.SaveChanges();

            //save the message back to the database

            //save the message to the database

            //post to Twitter

            dynamic config = new Configuration();

            OAuthTokens tokens = new OAuthTokens();
            tokens.AccessToken = u.Token;
            tokens.AccessTokenSecret = u.TokenSecret;
            tokens.ConsumerKey = config.ConsumerKey;
            tokens.ConsumerSecret = config.ConsumerSecret;

            TwitterStatus.Update(tokens, model.FullMessage);

            return true;
            
        }

        public JArray getSongAjax(string SearchString)
        {

  


            WebClient webClient = new WebClient();
            //   JArray a = JArray.Parse(webClient.DownloadString("http://tinysong.com/s/tiny+dancer?format=json&key=xxx"));

            JArray a = JArray.Parse(webClient.DownloadString("http://tinysong.com/s/" + SearchString.Replace(" ", "+") + "?format=json&key=xxx&limit=5"));





            return a;

        }

        public ActionResult Twitter()
        {

            OAuthTokens tokens = new OAuthTokens();
            tokens.AccessToken = "15290567-jECDdHBVwHVSGtKNe18y1765t7NCXyP17PvlyWJFP";
            tokens.AccessTokenSecret = "L2iipX2uKQ4FBjkh0ES1Vs4ASV3JaS52LkiQvCR31Q";
            tokens.ConsumerKey = "rG2CcgK3CsS1TvtgehFgMQ";
            tokens.ConsumerSecret = "m6Ad8Af2jQNOy7OT4JUtThK4LS4ZlMwZh7SlWGJg9Y";




            // Obtain a request token
            OAuthTokenResponse requestToken = OAuthUtility.GetRequestToken("rG2CcgK3CsS1TvtgehFgMQ", "m6Ad8Af2jQNOy7OT4JUtThK4LS4ZlMwZh7SlWGJg9Y", "http://localhost:63335/Home/TwitterCallback");

            // Direct or instruct the user to the following address:
            Uri authorizationUri = OAuthUtility.BuildAuthorizationUri(requestToken.Token);

            Response.Redirect(authorizationUri.ToString(), true);



            //     TwitterResponse<TwitterUser> showUserResponse = TwitterUser.Show(tokens, "twit_er_izer");
            //   if (showUserResponse.Result == RequestResult.Success)
            //   {
            // DisplayUserDetails(showUserResponse.ResponseObject);
            //  }
            //  else
            //  {
            // DisplayError(showUserResponse.ErrorMessage);
            //  }

            //      TwitterResponse<TwitterStatus> tweetResponse = TwitterStatus.Update(tokens, "Just doing some testing for a new twitter app I am building.  Wish there was a sandbox.");
            //     if (tweetResponse.Result == RequestResult.Success)
            //     {
            // Tweet posted successfully!
            //    }
            //    else
            //    {
            // Something bad happened
            //    }

            return View();
        }

        public ActionResult TwitterCallback(string oauth_token, string oauth_verifier)
        {

           // Response.BufferOutput = true;
            dynamic config = new Configuration();
            OAuthTokenResponse accessToken = OAuthUtility.GetAccessToken(config.ConsumerKey, config.ConsumerSecret, oauth_token, oauth_verifier);


            getSongViewModel model = new getSongViewModel();


            if (!CacheHelper.Get(oauth_token, out model))
            {
                string text = oauth_token;
            }


            //Store new user in the database

            var connection = new SongShoutEntities();
            var user = new Data.User();
            user.ScreenName = model.senderTwitterName;
            user.Token = accessToken.Token;
            user.TokenSecret = accessToken.TokenSecret;
            user.TwitterUserId = accessToken.UserId;
            user.VerificationString = accessToken.VerificationString;
            connection.Users.Add(user);
            connection.SaveChanges();




            PostMessage(user, model);
            return   RedirectToAction("SongSubmitted", model);
          


         

        }


            public ActionResult Detail(string id)
                {
                    var model = new getSongViewModel();
                    int count = 0;

                //retreive the details of the songshout message from the database
                //display on the details page
                //option to play the song they were sent
                //option to buy the song or sign up for rdio, spotify, etc.
                //option to reply to person via twitter
                //option to send someone else a songshout
                    if (!string.IsNullOrEmpty(id))
                    {

                        //check to see if valid guid.  If not fail
                        Guid messageguid = new Guid(id);

                        var context = new SongShoutEntities();
                        Message message = (from m in context.Messages where m.MessageId == messageguid select m).FirstOrDefault();
                        if (message != null)
                        {
                            model.AlbumName = message.AlbumId;
                            model.ArtistName = message.ArtistName;
                            model.FullMessage = message.FullMessage;
                            model.recipientTwitterName = "@" + message.RecipientScreenName;
                            model.senderTwitterName = "@" + message.SenderScreenName;
                            model.SongId = message.SongId;
                            model.SongName = message.SongName;
                            model.Message = message.CustomMessage;
                            model.Url = message.SongUrl;
                            count = message.ViewCount;

                            count = count + 1;
                            message.ViewCount = count;
                            context.SaveChanges();
                        }
                        else
                        {
                            //bad url redirect
                        }

                    }
                    else
                    {
                        //bad URL

                    }
                     return View(model);
                }

    }
}
