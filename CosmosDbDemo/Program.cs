using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace CosmosDbDemo
{
    public class Program
    {
        private const string DatabaseId = "OnlineShop";
        private const string CollectionId = "Customer";
        private static DocumentClient _client;

        public static void Main(string[] args)
        {
            _client = new DocumentClient(new Uri("YourURI"),
                "YourAccountKey");

            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();

            CreateItemAsync(new Customer { Id = "1", Age = 28, Name = "Wolfgang" }).Wait();

            var getListofCustomers = GetItemsAsync(x => x.Name == "Wolfgang").Result.ToList();
            var getOneCustomer = GetItemAsync(getListofCustomers[0].Id).Result;
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<Document> CreateItemAsync(Customer item)
        {
            return await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                item);
        }

        public static async Task<Customer> GetItemAsync(string id)
        {
            try
            {
                Document document =
                    await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (Customer)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        public static async Task<IEnumerable<Customer>> GetItemsAsync(Expression<Func<Customer, bool>> predicate)
        {
            var query = _client.CreateDocumentQuery<Customer>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                    new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            var results = new List<Customer>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<Customer>());
            }

            return results;
        }


        public static async Task DeleteItemAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }
    }
}