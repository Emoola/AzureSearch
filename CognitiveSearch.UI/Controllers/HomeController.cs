using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.CognitiveSearch.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.Azure.Search.Models;
using CognitiveSearch.UI.Models;
using Newtonsoft.Json.Linq;

namespace CognitiveSearch.UI.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration { get; set; }
        private DocumentSearchClient _docSearch { get; set; }
        // private SearchClientHelper _searchHelper { get; set; }

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _docSearch = new DocumentSearchClient(configuration);
            // _searchHelper = new SearchClientHelper(configuration);
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Search()
        {
            return View();
        }

        //ignore
        public IActionResult Graph()
        {
            return View();
        }
        

        [HttpPost]
        public IActionResult Search(string q)
        {
            
            var searchidId = _docSearch.GetSearchId().ToString();

            if (searchidId != string.Empty)
                TempData["searchId"] = searchidId;

            TempData["query"] = q;
            TempData["applicationInstrumentationKey"] = _configuration.GetSection("InstrumentationKey")?.Value;

            return View();
        }

        [HttpPost]
        public IActionResult GetDocuments(string q = "", SearchFacet[] searchFacets = null, int currentPage = 1)
        {
            var token = GetContainerSasUri();
            var selectFilter = _docSearch.Model.SelectFilter;

            if (!string.IsNullOrEmpty(q))
            {
                q = q.Replace("-", "").Replace("?", "");
            }
            var response = _docSearch.Search(q, searchFacets, selectFilter, currentPage);
            var facetResults = new List<object>();
            var tagsResults = new List<object>();

            

            if (response!= null && response.Facets != null)
            {
                // Return only the selected facets from the Search Model
                foreach (var facetResult in response.Facets.Where(f => _docSearch.Model.Facets.Where(x => x.Name == f.Key).Any()))
                {

                    facetResults.Add(new
                    {
                        key = facetResult.Key,
                        value = facetResult.Value
                    });

                }
                
                // Just create a list of tags for now.
                // TODO: This does not need to be a dictionary.
                foreach (var tag in _docSearch.Model.Tags)
                {
                    tagsResults.Add(new
                    {
                        key = tag.Name,
                        value = tag.Name
                    });
                }
            }

            try
            {
                return new JsonResult(new DocumentResult { Results = response.Results, Facets = facetResults, Tags = tagsResults, Count = Convert.ToInt32(response.Count), Token = token });
            }
            catch (System.NullReferenceException) 
            {
                Search();
                return View();
            }
            
        }

        [HttpPost]
        public IActionResult GetDocumentById(string id = "")
        {
            var token = GetContainerSasUri();

            var response = _docSearch.LookUp(id);
            var facetResults = new List<object>();

            return new JsonResult(new DocumentResult { Result = response, Token = token });
        }

        private string GetContainerSasUri()
        {
            string sasContainerToken;
            string accountName = _configuration.GetSection("StorageAccountName")?.Value;
            string accountKey = _configuration.GetSection("StorageAccountKey")?.Value;
            string containerAddress = _configuration.GetSection("StorageContainerAddress")?.Value;
            CloudBlobContainer container = new CloudBlobContainer(new Uri(containerAddress), new StorageCredentials(accountName, accountKey));

            SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read
            };

            sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);
            return sasContainerToken;
        }

        // add by lesley
        [HttpGet]
        public JObject GetFDNodes(string q)
        {
            JObject dataset = new JObject();
            int MaxEdges = 20;
            int MaxLevels = 3;
            int CurrentLevel = 1;
            int CurrentNodes = 0;

            // var FDEdgeList = new List<FDGraphEdges>();
            List<FDGraphEdges> FDEdgeList = new List<FDGraphEdges>();

            // Create a node map that will map a facet to a node - nodemap[0] always equals the q term
            // var NodeMap = new Dictionary<string, int>();
            Dictionary<string, int> NodeMap = new Dictionary<string, int>();
            NodeMap[q] = CurrentNodes;

            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            var NextLevelTerms = new List<string>();
            NextLevelTerms.Add(q);

            // Iterate through the nodes up to 3 levels deep to build the nodes or when I hit max number of nodes
            while ((NextLevelTerms.Count() > 0) && (CurrentLevel <= MaxLevels) && (FDEdgeList.Count() < MaxEdges))
            {
                q = NextLevelTerms.First();
                NextLevelTerms.Remove(q);
                if (NextLevelTerms.Count() == 0)
                    CurrentLevel++;

                System.Diagnostics.Debug.WriteLine("debugging GetFDNodes function");

                var facetName = "keyphrases";
                DocumentSearchResult response = _docSearch.GetFacets(q, facetName, 10);
                if (response != null)
                {
                    System.Diagnostics.Debug.WriteLine(response);

                    IList<FacetResult> facetVals = (response.Facets)[facetName];
                    System.Diagnostics.Debug.WriteLine("facet");
                    System.Diagnostics.Debug.WriteLine(facetVals);
                    foreach (var facet in facetVals)
                    {
                        int node = -1;
                        if (NodeMap.TryGetValue(facet.Value.ToString(), out node) == false)
                        {
                            // This is a new node
                            CurrentNodes++;
                            node = CurrentNodes;
                            NodeMap[facet.Value.ToString()] = node;
                        }
                        // Add this facet to the fd list
                        if (NodeMap[q] != NodeMap[facet.Value.ToString()])
                        {
                            FDEdgeList.Add(new FDGraphEdges { source = NodeMap[q], target = NodeMap[facet.Value.ToString()] });
                            if (CurrentLevel < MaxLevels)
                                NextLevelTerms.Add(facet.Value.ToString());
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("null response");
                }
            }

            JArray nodes = new JArray();
            foreach (var entry in NodeMap)
            {
                nodes.Add(JObject.Parse("{name: \"" + entry.Key.Replace("\"", "") + "\"}"));
            }

            JArray edges = new JArray();
            foreach (var entry in FDEdgeList)
            {
                edges.Add(JObject.Parse("{source: " + entry.source + ", target: " + entry.target + "}"));
            }

            dataset.Add(new JProperty("edges", edges));
            dataset.Add(new JProperty("nodes", nodes));

            // Create the fd data object to return
            return dataset;
        }

        public class FDGraphEdges
        {
            public int source { get; set; }
            public int target { get; set; }
        }

        /*
        [HttpPost]
        public IActionResult GenerateGraph(string q="", SearchFacet[] searchFacets = null, int currentPage = 1) {
            var selectFilter = _docSearch.Model.SelectFilter;

            if (!string.IsNullOrEmpty(q))
            {
                q = q.Replace("-", "").Replace("?", "");
            }
            var response = _docSearch.Search(q, searchFacets, selectFilter, currentPage);
            var facetResults = new List<object>();
            foreach (var facetResult in response.Facets.Where(f => _docSearch.Model.Facets.Where(x => x.Name == f.Key).Any()))
                {
                facetResults.Add(new
                {
                    key = facetResult.Key,
                    value = facetResult.Value
                });
            }
            return View();
        }

        public class FDGraphEdges
        {
            public int source { get; set; }
            public int target { get; set; }
        }

        public JObject GetFacetGraphNodes(string q, string facetName)
        {
            JObject dataset = new JObject();
            int MaxEdges = 20;
            int MaxLevels = 3;
            int CurrentLevel = 1;
            int CurrentNodes = 0;

            //List<FDGraphEdges> FDEdgeList = new List<FDGraphEdges>();
            // Create a node map that will map a facet to a node - nodemap[0] always equals the q term
            Dictionary<string, int> NodeMap = new Dictionary<string, int>();
            NodeMap[q] = CurrentNodes;
            return dataset;
        }
        */

    }
}
