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
                double current_balance = 0.0;
                

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
                   
                    List<Customers> customers = await AzureManager.AzureManagerInstance.GetCustomers();
                    endOutput = "";
                    foreach (Customers t in customers)
                    {
                        if(t.ID.Equals("832c0921-6975-4b53-9593-b827fd21bb48"))
                        endOutput += " Cheque $"+t.Cheque+"\n\n Savings $" +t.Savings +"\n\n";
                    }
                    isExchangeRequest = false;
                   
                }

                // pay bill to a payee

                if (userMessage.ToLower().Contains("pay orcon"))
                {
                    string orconbill_string = userMessage.Substring(10);
                    double orconbill = Convert.ToDouble(orconbill_string);
                    double remaining_chq_balance;
                    double current_chq_balance = 0;
                    double current_savings_balance = 0;

                    List<Customers> customers = await AzureManager.AzureManagerInstance.GetCustomers();
                    
                    foreach (Customers t in customers)
                    {
                        if (t.ID.Equals("832c0921-6975-4b53-9593-b827fd21bb48"))
                        {
                            current_chq_balance = t.Cheque;
                            current_savings_balance = t.Savings;
                        }
                        
                    }

                    remaining_chq_balance = current_chq_balance - orconbill;
                    Customers customer = new Customers()
                    {
                        ID = "832c0921-6975-4b53-9593-b827fd21bb48",
                        First_Name = "John",
                        Last_Name = "Abraham",
                        Cheque = remaining_chq_balance,
                        Savings = 3000.0,
                        createdAt = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.UpdateCustomer(customer);

                    isExchangeRequest = false;

                    endOutput = "Orcon Bill paid,\n\n Remaining balance in Cheque $"+customer.Cheque + "\n\n Today " + customer.createdAt ;
                }

                //Transfer money from cheque to savings account

                if (userMessage.ToLower().Contains("transfer"))
                {
                    string amount_to_transfer_string = userMessage.Substring(32);
                    double amount_to_transfer = Convert.ToDouble(amount_to_transfer_string);
                    double remaining_chq_balance;
                    double current_chq_balance = 0;
                    double current_savings_balance = 0;
                    double updated_savings_balance = 0;

                    List<Customers> customers = await AzureManager.AzureManagerInstance.GetCustomers();

                    foreach (Customers t in customers)
                    {
                        if (t.ID.Equals("832c0921-6975-4b53-9593-b827fd21bb48"))
                        {
                            current_chq_balance = t.Cheque;
                            current_savings_balance = t.Savings;
                        }

                    }

                    remaining_chq_balance = current_chq_balance - amount_to_transfer;
                    updated_savings_balance = current_savings_balance + amount_to_transfer;
                    Customers customer = new Customers()
                    {
                        ID = "832c0921-6975-4b53-9593-b827fd21bb48",
                        First_Name = "John",
                        Last_Name = "Abraham",
                        Cheque = remaining_chq_balance,
                        Savings = updated_savings_balance,
                        createdAt = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.UpdateCustomer(customer);

                    isExchangeRequest = false;

                    endOutput ="$" +amount_to_transfer+" Tranferred form Cheque to Savings.\n\n Remaining amount in Cheque $" + customer.Cheque + "\n\n Updated amount in Savings $" + customer.Savings +"\n\n Today " + customer.createdAt;
                }


                //Bot card for bank website

                if (userMessage.ToLower().Equals("nzee"))
                {
                    Activity replyToConversation = activity.CreateReply("NZEE BANK");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://icons.iconarchive.com/icons/uiconstock/dynamic-flat-android/72/bank-icon.png"));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://www.westpac.co.nz/",
                        Type = "openUrl",
                        Title = "NZEE Bank Web Site"
                    };
                    cardButtons.Add(plButton);

                    CardAction p2Button = new CardAction()
                    {
                        Value = "tel:123123123",
                        Type = "call",
                        Title = "Call NZEE Bank"
                    };
                    cardButtons.Add(p2Button);

                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Visit NZEE Bank",
                        Subtitle = "Vist NZEE Bank Website for more info",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

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