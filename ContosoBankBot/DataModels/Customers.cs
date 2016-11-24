using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoBankBot.DataModels
{
    public class Customers
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "first_name")]
        public string First_Name { get; set; }

        [JsonProperty(PropertyName = "last_name")]
        public string Last_Name { get; set; }

        [JsonProperty(PropertyName = "cheque")]
        public double Cheque { get; set; }

        [JsonProperty(PropertyName = "savings")]
        public double Savings { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime Date { get; set; }

    }
}