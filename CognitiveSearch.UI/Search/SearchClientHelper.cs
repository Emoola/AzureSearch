using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.CognitiveSearch.Search
{
    public class SearchClientHelper
    {
        private static SearchServiceClient _searchClient;

        public static string errorMessage;
        private IConfiguration configuration;

        private string searchServiceName { get; set; } // = Configuration.GetSection("AzureStorageConnectionString")?.Value;"cogsearchtemplate2";
        private string apiKey { get; set; } //"726F031FBF03C63DF036781C1353340B";
        private string IndexName { get; set; } // = "chromerindex";

        public SearchClientHelper(string serviceName, string apiKey, string indexName)
        {
            try
            {
                this.IndexName = indexName;
                this.searchServiceName = serviceName;
                this.apiKey = apiKey;
                _searchClient = new SearchServiceClient(serviceName, new SearchCredentials(apiKey));
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public SearchClientHelper(IConfiguration configuration)
        {
            this.configuration = configuration;

            searchServiceName = configuration.GetSection("SearchServiceName")?.Value;
            apiKey = configuration.GetSection("SearchApiKey")?.Value;
            IndexName = configuration.GetSection("SearchIndexName")?.Value;

            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));

        }

        public DocumentSearchResult GetFacets(string searchText, string facetName, int maxCount = 30)
        {
            // Execute search based on query string
            try
            {
                SearchParameters sp = new SearchParameters()
                {
                    SearchMode = SearchMode.Any,
                    Top = 0,
                    Select = new List<String>() { "id" },
                    Facets = new List<String>() { $"{facetName}, count:{maxCount}" },
                    QueryType = QueryType.Full
                };

                System.Diagnostics.Debug.WriteLine("GetFacets function");
                System.Diagnostics.Debug.WriteLine(sp);
                return _searchClient.Indexes.GetClient(IndexName).Documents.Search(searchText, sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }
    }
}