using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using MarketingCodingAssignment.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis.En;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Analysis.Hunspell;
using Lucene.Net.Search.Spell;
using CsvHelper.Configuration.Attributes;

/* Changes by Krupali on 30/07/2024
 * MethodName                       Changes
 * =========================================================================================================================
     PopulateIndexFromCSV()           Added release date
	 PopulateIndex()                  Added release date and vote average to the document
	 Search()                         Added new bool parameter to check if movie name is selected from the list
                                      Added Release date and Vote Average to the document
	 GetLuceneQuery()                 Changed code to set filter for voteaverage and release date
                                      Changed code to change query for stemming
     FetchDataForAutoComplete()       Added new method to get term for suggestion
     CheckSpelling()                  Added new method for spelling check
     GetSuggestionsForTerm()          After performing spelling check give suggestions for word
     PerformStemming()                Added new method to perform stemming (using Porter Stemmer)
                          
 */

namespace MarketingCodingAssignment.Services
{
    public class SearchEngine
    {
        // The code below is roughly based on sample code from: https://lucenenet.apache.org/

        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public SearchEngine()
        {

        }

        public List<FilmCsvRecord> ReadFilmsFromCsv()
        {
            List<FilmCsvRecord> records = new();
            string filePath = $"{System.IO.Directory.GetCurrentDirectory()}{@"\wwwroot\csv"}" + "\\" + "FilmsInfo.csv";
            using (StreamReader reader = new(filePath))
            using (CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                records = csv.GetRecords<FilmCsvRecord>().ToList();

            }
            using (StreamReader r = new(filePath))
            {
                string csvFileText = r.ReadToEnd();
            }
            return records;
        }

        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndexFromCsv()
        {
            // Get the list of films from the csv file
            var csvFilms = ReadFilmsFromCsv();

            // Convert to Lucene format
            List<FilmLuceneRecord> luceneFilms = csvFilms.Select(x => new FilmLuceneRecord
            {
                Id = x.Id,
                Title = x.Title,
                Overview = x.Overview,
                Runtime = int.TryParse(x.Runtime, out int parsedRuntime) ? parsedRuntime : 0,
                Tagline = x.Tagline,
                Revenue = long.TryParse(x.Revenue, out long parsedRevenue) ? parsedRevenue : 0,
                VoteAverage = double.TryParse(x.VoteAverage, out double parsedVoteAverage) ? parsedVoteAverage : 0,
                ReleaseDate = DateTime.TryParseExact(x.ReleaseDate, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                               DateTimeStyles.None, out DateTime parsedReleaseDate) ? parsedReleaseDate : DateTime.MinValue,
            }).ToList();

            // Write the records to the lucene index
            PopulateIndex(luceneFilms);

            return;
        }

        public void PopulateIndex(List<FilmLuceneRecord> films)
        {
            // Construct a machine-independent path for the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);

            // Create an analyzer to process the text
            StandardAnalyzer analyzer = new(AppLuceneVersion);

            // Create an index writer
            IndexWriterConfig indexConfig = new(AppLuceneVersion, analyzer);
            using IndexWriter writer = new(dir, indexConfig);

            //Add to the index
            foreach (var film in films)
            {
                //Get current datetime 
                DateTime? dateValue = film.ReleaseDate;
                string dateString = dateValue.HasValue ? dateValue.Value.ToString("yyyy-MM-dd") : null;

                Document doc = new()
                {
                    new StringField("Id", film.Id, Field.Store.YES),
                    new TextField("Title", film.Title, Field.Store.YES),
                    new TextField("Overview", film.Overview, Field.Store.YES),
                    new Int32Field("Runtime", film.Runtime, Field.Store.YES),
                    new TextField("Tagline", film.Tagline, Field.Store.YES),
                    new Int64Field("Revenue", film.Revenue ?? 0, Field.Store.YES),
                    new DoubleField("VoteAverage", film.VoteAverage ?? 0.0, Field.Store.YES),
                    new TextField("CombinedText", film.Title + " " + film.Tagline + " " + film.Overview, Field.Store.NO),
                    
                     // Add ReleaseDate only if it's not null
                    dateString != null ? new StringField("ReleaseDate", dateString, Field.Store.YES) : null

                };
                writer.AddDocument(doc);
            }

            writer.Flush(triggerMerge: false, applyAllDeletes: false);
            writer.Commit();

            return;
        }

        public void DeleteIndex()
        {
            // Delete everything from the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);
            StandardAnalyzer analyzer = new(AppLuceneVersion);
            IndexWriterConfig indexConfig = new(AppLuceneVersion, analyzer);
            using IndexWriter writer = new(dir, indexConfig);
            writer.DeleteAll();
            writer.Commit();
            return;
        }

        public SearchResultsViewModel Search(string searchString, int startPage, int rowsPerPage, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum
            , DateTime? releaseDateFrom, DateTime? releaseDateTo, bool isSuggestionSelected)
        {
            // Construct a machine-independent path for the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new(reader);

            int hitsLimit = 1000;
            TopScoreDocCollector collector = TopScoreDocCollector.Create(hitsLimit, true);

            var query = this.GetLuceneQuery(searchString, durationMinimum, durationMaximum, voteAverageMinimum, releaseDateFrom, releaseDateTo, isSuggestionSelected);

            searcher.Search(query, collector);

            int startIndex = (startPage) * rowsPerPage;
            TopDocs hits = collector.GetTopDocs(startIndex, rowsPerPage);
            ScoreDoc[] scoreDocs = hits.ScoreDocs;

            List<FilmLuceneRecord> films = new();
            foreach (ScoreDoc? hit in scoreDocs)
            {
                Document foundDoc = searcher.Doc(hit.Doc);
                FilmLuceneRecord film = new()
                {
                    Id = foundDoc.Get("Id").ToString(),
                    Title = foundDoc.Get("Title").ToString(),
                    Overview = foundDoc.Get("Overview").ToString(),
                    Runtime = int.TryParse(foundDoc.Get("Runtime"), out int parsedRuntime) ? parsedRuntime : 0,
                    Tagline = foundDoc.Get("Tagline").ToString(),
                    Revenue = long.TryParse(foundDoc.Get("Revenue"), out long parsedRevenue) ? parsedRevenue : 0,
                    VoteAverage = double.TryParse(foundDoc.Get("VoteAverage"), out double parsedVoteAverage) ? parsedVoteAverage : 0.0,
                    Score = hit.Score,
                    ReleaseDate = DateTime.TryParseExact(foundDoc.Get("ReleaseDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                               DateTimeStyles.None, out DateTime parsedReleaseDate) ? parsedReleaseDate : DateTime.MinValue

                };
                films.Add(film);
            }

            SearchResultsViewModel searchResults = new()
            {
                RecordsCount = hits.TotalHits,
                Films = films.ToList()
            };

            return searchResults;
        }
        private Query GetLuceneQuery(string searchString, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum
            , DateTime? releaseDateFrom, DateTime? releaseDateTo, bool isSuggestionSelected)
        {
            BooleanQuery bq = new BooleanQuery();
            if (string.IsNullOrWhiteSpace(searchString))
            {
                // If there's no search string, just return everything.
                return new MatchAllDocsQuery();
            }

            if (isSuggestionSelected)
            {
                var phrases = searchString.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.Trim())
                              .Where(p => !string.IsNullOrWhiteSpace(p));

                foreach (var phrase in phrases)
                {
                    if (!EnglishAnalyzer.DefaultStopSet.Contains(phrase.ToLowerInvariant()))
                    {
                        var terms = phrase.Split(' ')
                                     .Select(term => new Term("CombinedText", term.ToLowerInvariant()))
                                     .ToArray();

                        if (terms.Length > 0)
                        {
                            var phraseQuery = new PhraseQuery();
                            foreach (var term in terms)
                            {
                                phraseQuery.Add(term);
                            }

                            // Add PhraseQuery to BooleanQuery
                            bq.Add(phraseQuery, Occur.MUST);
                        }
                    }

                }
            }
            else
            {
                // Create a BooleanQuery to handle both original and stemmed terms
                var keywordQuery = new BooleanQuery();
                var stemmedQuery = new BooleanQuery();

                // Original search string
                foreach (var word in searchString.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var lowerCaseWord = word.ToLowerInvariant();
                    if (!EnglishAnalyzer.DefaultStopSet.Contains(lowerCaseWord))
                    {
                        var prefixQuery = new PrefixQuery(new Term("CombinedText", lowerCaseWord));
                        keywordQuery.Add(prefixQuery, Occur.SHOULD);
                    }
                }

                // Stemmed search string
                string stemSearchString = PerformStemming(searchString);
                foreach (var word in stemSearchString.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var lowerCaseWord = word.ToLowerInvariant();
                    if (!EnglishAnalyzer.DefaultStopSet.Contains(lowerCaseWord))
                    {
                        var prefixQuery1 = new PrefixQuery(new Term("CombinedText", lowerCaseWord));
                        stemmedQuery.Add(prefixQuery1, Occur.SHOULD);
                    }
                }

                // Combine keywordQuery and stemmedQuery with Occur.SHOULD
                var combinedQuery = new BooleanQuery();
                combinedQuery.Add(keywordQuery, Occur.SHOULD);
                combinedQuery.Add(stemmedQuery, Occur.SHOULD);

                // Add combined query to the main BooleanQuery
                bq.Add(combinedQuery, Occur.MUST);
            }

            Query rq = NumericRangeQuery.NewInt32Range("Runtime", durationMinimum, durationMaximum, true, true);
            bq.Add(rq, Occur.MUST);
            Query vaq = NumericRangeQuery.NewDoubleRange("VoteAverage", voteAverageMinimum, 10, true, true);
            bq.Add(vaq, Occur.MUST);

            // Format dates to Lucene-compatible format (yyyyMMdd)
            if (releaseDateFrom.HasValue && releaseDateTo.HasValue)
            {
                string fromDateStr = releaseDateFrom.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                string toDateStr = releaseDateTo.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

                Query drq = TermRangeQuery.NewStringRange("ReleaseDate", fromDateStr, toDateStr, true, true);
                bq.Add(drq, Occur.MUST);
            }
            return bq;
        }      

        #region OldGetLuceneQuery
        //private Query GetLuceneQuery(string searchString, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateFrom, DateTime? releaseDateTo)
        //{
        //    // Create the base BooleanQuery that will include filters that always apply
        //    BooleanQuery baseQueryBuilder = new BooleanQuery();

        //    // Add duration filter if specified
        //    if (durationMinimum.HasValue || durationMaximum.HasValue)
        //    {
        //        Query durationQuery = NumericRangeQuery.NewInt32Range("Runtime", durationMinimum, durationMaximum, true, true);
        //        baseQueryBuilder.Add(durationQuery, Occur.MUST);
        //    }

        //    // Add vote average filter if specified
        //    if (voteAverageMinimum.HasValue)
        //    {
        //        Query voteAverageQuery = NumericRangeQuery.NewDoubleRange("VoteAverage", voteAverageMinimum, 10, true, true);
        //        baseQueryBuilder.Add(voteAverageQuery, Occur.MUST);
        //    }

        //    // Add release date range filter if specified
        //    if (releaseDateFrom.HasValue && releaseDateTo.HasValue)
        //    {
        //        string fromDateStr = releaseDateFrom.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        //        string toDateStr = releaseDateTo.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        //        Query releaseDateQuery = TermRangeQuery.NewStringRange("ReleaseDate", fromDateStr, toDateStr, true, true);
        //        baseQueryBuilder.Add(releaseDateQuery, Occur.MUST);
        //    }

        //    // Add text search query if searchString is not null or empty
        //    if (!string.IsNullOrWhiteSpace(searchString))
        //    {
        //        var pq = new MultiPhraseQuery();
        //        foreach (var word in searchString.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
        //        {
        //            if (!EnglishAnalyzer.DefaultStopSet.Contains(word))
        //            {
        //                pq.Add(new Term("CombinedText", word.ToLowerInvariant()));
        //            }
        //        }
        //        baseQueryBuilder.Add(pq, Occur.MUST);
        //    }

        //    // Return the constructed BooleanQuery
        //    return baseQueryBuilder;
        //}
        #endregion


        public string[] FetchDataForAutoComplete(string term)
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");

            using FSDirectory dir = FSDirectory.Open(indexPath);
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new(reader);
            string correctedSentence = term;
            if (reader != null)
            {
                correctedSentence = CheckSpelling(term, reader);
            }

            var matches = GetSuggestionsForTerm(correctedSentence, searcher);
            return matches;
        }

        #region SpellingCheck
        public string CheckSpelling(string term, DirectoryReader reader)
        {

            DirectSpellChecker directSpellChecker = new DirectSpellChecker();

            // Assume `term` contains the input sentence you want to spell check
            string[] inputWords = term.Split(' '); // Split the input into words
            List<string> correctedWords = new List<string>(); // List to store corrected words

            // Iterate through each word to check for spelling suggestions
            foreach (string word in inputWords)
            {
                // Get spelling suggestions for each word
                var spellCheckSuggestions = directSpellChecker.SuggestSimilar(new Term("Title", word), 1, reader, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                // Check if suggestions are available
                if (spellCheckSuggestions != null && spellCheckSuggestions.Length > 0)
                {
                    foreach (var suggestion in spellCheckSuggestions)
                    {

                        // If there are suggestions, use the first suggestion
                        //correctedWords.Add(spellCheckSuggestions[0]);
                        correctedWords.Add(suggestion.String.ToString());
                    }
                }
                else
                {
                    // If no suggestions are available, keep the original word
                    correctedWords.Add(word);
                }
            }

            // Reconstruct the corrected sentence
            string correctedSentence = string.Join(" ", correctedWords);

            /// ------ Spelling check ends here
            return correctedSentence;
        }
        #endregion

        #region AutoComplete/Suggestions
        public string[] GetSuggestionsForTerm(string correctedSentence, IndexSearcher searcher)
        {
            /// ----- Auto Complete suggestions starts here
            // Split the input term into tokens
            string[] tokens = correctedSentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            BooleanQuery booleanQueryBuilder = new BooleanQuery();

            // Handle full term matches for all but the last token
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                // Use TermQuery for full matches on the preceding tokens
                booleanQueryBuilder.Add(new TermQuery(new Term("Title", tokens[i])), Occur.SHOULD);
            }

            // Handle the last token for partial matches (using PrefixQuery)
            string lastToken = tokens[tokens.Length - 1];
            booleanQueryBuilder.Add(new PrefixQuery(new Term("Title", lastToken)), Occur.SHOULD);

            // Combine all queries into a BooleanQuery
            TopDocs foundDocs = searcher.Search(booleanQueryBuilder, 10);

            var matches = foundDocs.ScoreDocs
                .Select(scoreDoc => searcher.Doc(scoreDoc.Doc).Get("Title"))
                .ToArray();

            return matches;

        }
        #endregion

        #region Stemming
        public string PerformStemming(string searchTerm)
        {
            PorterStemmer porterStemmer = new PorterStemmer();

            string[] words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> stemmedWords = new List<string>();

            foreach (string word in words)
            {
                porterStemmer.SetCurrent(word);
                bool isStemmed = porterStemmer.Stem();

                if (isStemmed)
                {
                    stemmedWords.Add(porterStemmer.Current.ToLowerInvariant());
                }
                else
                {
                    stemmedWords.Add(word);
                }
            }

            string stemmedSentence = string.Join(" ", stemmedWords);
            return stemmedSentence;
        }
        #endregion
    }
}

