using MarketingCodingAssignment.Models;
using MarketingCodingAssignment.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

/* Changes by Krupali on 30/07/2024
 * MethodName                       Changes
 * =========================================================================================================================
    
	 Search()                         Added new bool parameter to check if movie name is selected from the list
     Autocomplete()                   New method added which will call service for auto complete suggestions        

*/

namespace MarketingCodingAssignment.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SearchEngine _searchEngine;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _searchEngine = new SearchEngine();
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public JsonResult Search(string searchString, int start, int rows, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum
            , DateTime? releaseDateFrom, DateTime? releaseDateTo,bool isSuggestionSelected)
        {
            // Validate date range
            if (releaseDateFrom.HasValue && releaseDateTo.HasValue && releaseDateFrom.Value > releaseDateTo.Value)
            {
                return Json(new { error = "Release Date From cannot be after Release Date To" });
                
            }

            SearchResultsViewModel searchResults = _searchEngine.Search(searchString, start, rows, durationMinimum, durationMaximum, voteAverageMinimum,releaseDateFrom,releaseDateTo, isSuggestionSelected);
            return Json(new {searchResults});
        }

        public ActionResult ReloadIndex()
        {
            DeleteIndex();
            PopulateIndex();
            return RedirectToAction("Index", "Home");
        }

        // Delete the contents of the lucene index 
        public void DeleteIndex()
        {
            _searchEngine.DeleteIndex();
            return;
        }

        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndex()
        {
            _searchEngine.PopulateIndexFromCsv();
            return;
        }

        // Autocomplete when user type in something
        [HttpGet("autocomplete")]
        public IActionResult Autocomplete(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new string[0]);
            }

            var results = _searchEngine.FetchDataForAutoComplete(term);
            return Json(results);
        }

    }
}

