using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.MobileServices;
using ContosoBankBot.DataModels;
using System.Threading.Tasks;

namespace ContosoBankBot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<Customers> customersTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://newdbapp.azurewebsites.net/");
            this.customersTable = this.client.GetTable<Customers>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task<List<Customers>> GetCustomers()
        {
            return await this.customersTable.ToListAsync();
        }

        public async Task AddTimeline(Customers timeline)
        {
            await this.customersTable.InsertAsync(timeline);
        }


    }
}