using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using ContosoBankBot.Models;
using Microsoft.WindowsAzure.MobileServices;
using ContosoBankBot.DataModels;
using System.Collections.Generic;

namespace ContosoBankBot
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

                StateClient stateClient = activity.GetStateClient(); //to set up state
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);


                //Get/Set users property data
                var userMessage = activity.Text;

                string endOutput = "Hello";
                string balance_msg = "";
                double current_balance = 300.0;

                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hello again";
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                bool isExchangeRequest = true;

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    isExchangeRequest = false;
                }

                if (userMessage.ToLower().Contains("balance"))
                {
                    balance_msg = " your balance is $"+ current_balance.ToString();

                    isExchangeRequest = false;
                   
                }


                //get all customers data

                if (userMessage.ToLower().Equals("get customers"))
                {
                    List<Customers> customers = await AzureManager.AzureManagerInstance.GetCustomers();
                    endOutput = "";
                    foreach (Customers t in customers)
                    {
                        //endOutput += "[" + t.Date + "] First Name " + t.First_Name + ", Last Name " + t.Last_Name + "\n\n";
                        endOutput += t.First_Name + " "+ t.Last_Name + " Cheque Balance $"+t.Cheque+" Savings Balance $" +t.Savings +"\n\n";
                    }
                    isExchangeRequest = false;

                }

                //update customers data
                if (userMessage.ToLower().Equals("new customer"))
                {
                    Customers customer = new Customers()
                    {
                        First_Name = "John",
                        Last_Name = "Abraham",
                        Cheque = 100.0,
                        Savings = 3000.0,
                        Date = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.AddTimeline(customer);

                    isExchangeRequest = false;

                    endOutput = "New timeline added [" + customer.Date + "]";
                }




                if (!isExchangeRequest)
                {

                    // return our reply to the user
                    Activity infoReply = activity.CreateReply(endOutput +balance_msg);

                    await connector.Conversations.ReplyToActivityAsync(infoReply);
                }

                else
                {
                    // calculate something for us to return
                    HttpClient client = new HttpClient();
                    string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + activity.Text));
                    ExchangeRates.RootObject rootObject;
                    rootObject = JsonConvert.DeserializeObject<ExchangeRates.RootObject>(x);
                    double exchRates = rootObject.rates.NZD;


                    int length = (activity.Text ?? string.Empty).Length;

                    // return our reply to the user
                    Activity reply = activity.CreateReply($" Current Exchange rate of {activity.Text.ToUpper()} to NZD is {exchRates} ");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
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
}