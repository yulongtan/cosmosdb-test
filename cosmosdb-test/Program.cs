﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace cosmosdbtest
{
    class MainClass
    {

		private const string endpoint = "https://dsodocumentdb.documents.azure.com:443/";
		private const string primaryKey = "zdztF757TrQ0walwRd8Sa1FKfHJD9ESY0ECqwbN3BLIhy4xum27zuDS9F2KKiloPGHUv4nuJPRHQEM6X48Puxg==";

        private const string dbname = "testdb";
        private const string collectionid = "families";
        private const string documentid = "AndersenFamily";

        private static Database database;
        private static DocumentCollection collection;

        public static void Main(string[] args)
        {
			// Catch exceptions and log them to the console
			try
			{
                CreateDocumentClient().Wait();
			}
			catch (Exception e)
			{
				Exception baseException = e.GetBaseException();
				Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
			}
			Console.ReadKey();
        }

        private static async Task ReadDocument() {
            //Reads a Document resource where 
            // - db_id is the ID property of the Database
            // - coll_id is the ID property of the DocumentCollection
            // - doc_id is the ID property of the Document you wish to read. 
            using (var client = new DocumentClient(new Uri(endpoint), primaryKey))
            {
                var docUri = UriFactory.CreateDocumentUri(dbname, collectionid, documentid);
                Document document = await client.ReadDocumentAsync(docUri);
            }

        }

        // Client to create the document
		private static async Task CreateDocumentClient()
		{
			// Create a new instance of the DocumentClient

            using (var client = new DocumentClient(new Uri(endpoint), primaryKey))
			{
				database = client.CreateDatabaseQuery("SELECT * FROM c WHERE c.id = 'testdb'").AsEnumerable().First(); 
		  
				collection = client.CreateDocumentCollectionQuery(database.CollectionsLink,
				   "SELECT * FROM c WHERE c.id = 'families'").AsEnumerable().First();

				// await CreateDocuments(client);
				await QueryDocumentsWithPaging(client);
				QueryDocumentsWithLinq(client);
			}
		}

        // Creating documents
		private async static Task CreateDocuments(DocumentClient client)
		{
			Console.WriteLine();
			Console.WriteLine("**** Create Documents ****");
			Console.WriteLine();

			dynamic document1Definition = new
			{
				name = "New Customer 1",
				address = new
				{
					addressType = "Main Office",
					addressLine1 = "123 Main Street",
					location = new
					{
						city = "Brooklyn",
						stateProvinceName = "New York"
					},
					postalCode = "11229",
					countryRegionName = "United States"
				},
			};

			Document document1 = await CreateDocument(client, document1Definition);
			Console.WriteLine("Created document {0} from dynamic object", document1.Id);
			Console.WriteLine();
		}

        // Create document
		private async static Task<Document> CreateDocument(DocumentClient client, object documentObject)
		{

			var result = await client.CreateDocumentAsync(collection.SelfLink, documentObject);
			var document = result.Resource;

			Console.WriteLine("Created new document: {0}\r\n{1}", document.Id, document);
			return result;
		}

		// Querying a document
		private async static Task QueryDocumentsWithPaging(DocumentClient client)
		{
			Console.WriteLine();
			Console.WriteLine("**** Query Documents (paged results) ****");
			Console.WriteLine();
			Console.WriteLine("Querying for all documents");

            // select * from c where c.id = "AndersenFamily" would return the document that has this specific id
			var sql = "SELECT * FROM c";
			var query = client.CreateDocumentQuery(collection.SelfLink, sql).AsDocumentQuery();

			while (query.HasMoreResults)
			{
				var documents = await query.ExecuteNextAsync();

				foreach (var document in documents)
				{
					Console.WriteLine(" Id: {0}; Name: {1};", document.id, document.name);
				}
			}

			Console.WriteLine();
		}

		// Updating the documents
		private async static Task ReplaceDocuments(DocumentClient client)
		{

			Console.WriteLine();
			Console.WriteLine(">>> Replace Documents <<<");
			Console.WriteLine();
			Console.WriteLine("Querying for documents with 'isNew' flag");

			var sql = "SELECT * FROM c WHERE c.isNew = true";
			var documents = client.CreateDocumentQuery(collection.SelfLink, sql).ToList();

			Console.WriteLine("Documents with 'isNew' flag: {0} ", documents.Count);
			Console.WriteLine();
			Console.WriteLine("Quering for documents to be updated");

			sql = "SELECT * FROM c";
			documents = client.CreateDocumentQuery(collection.SelfLink, sql).ToList();
			Console.WriteLine("Found {0} documents to be updated", documents.Count);

			foreach (var document in documents)
			{
				document.isNew = true;
                // Replaces the old document with the new one
				var result = await client.ReplaceDocumentAsync(document._self, document);
				var updatedDocument = result.Resource;
				Console.WriteLine("Updated document 'isNew' flag: {0}", updatedDocument.isNew);
			}

			Console.WriteLine();
			Console.WriteLine("Querying for documents with 'isNew' flag");

			sql = "SELECT * FROM c WHERE c.isNew = true";
			documents = client.CreateDocumentQuery(collection.SelfLink, sql).ToList();
			Console.WriteLine("Documents with 'isNew' flag: {0}: ", documents.Count);
			Console.WriteLine();
		}

		private async static Task CreateDatabase(DocumentClient client)
		{
			Console.WriteLine();
			Console.WriteLine("******** Create Database *******");

			var databaseDefinition = new Database { Id = "mynewdb" };
			var result = await client.CreateDatabaseAsync(databaseDefinition);
			var database = result.Resource;

			Console.WriteLine(" Database Id: {0}; Rid: {1}",
			   database.Id, database.ResourceId);
			Console.WriteLine("******** Database Created *******");
		}

		private static void GetDatabases(DocumentClient client)
		{
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("******** Get Databases List ********");

			var databases = client.CreateDatabaseQuery().ToList();

			foreach (var database in databases)
			{
				Console.WriteLine(" Database Id: {0}; Rid: {1}", database.Id,
				   database.ResourceId);
			}

			Console.WriteLine();
			Console.WriteLine("Total databases: {0}", databases.Count);
		}
    }
}