# Marketing Coding Test #
## The Challenge: Modify a Web-based Search App ##

This .NET Core demo app uses [Lucene.NET](https://lucenenet.apache.org/) to index data from the included FilmsInfo.csv file into a search engine. When you run the app, press the reload button to load the data into the search engine. After that, you can enter search terms and press the search button to search for matching movie results. 

This coding test should take 2 hours. For this test, add as many of the basic features as possible in the time allotted. If you have extra time, select one of the advanced features to implement. If you wish to include extra JavaScript libraries, feel free to use a CDN.

## Basic Features: :seedling: ##
1. Display **Voting Average** in the returned search results.
2. Finsh conecting the **Voting Average (Minimum)** so that it filters the results that are below the minimum selected values. 
1. Add **Release Date** to the index and display it in the returned search results. _(:bulb: Hint: You will need to reload the index after making changes to the indexing code.)_
1. Add a way to filter the search by date range for **Release Date**.
1. Show off your css skills - improve the styling and layout of the page and/or search results. 

## Advanced Features: :mortar_board: ##
1. Autocomplete  - suggest search terms as the user types in the search box.
1. Stemming - when searching for “engineer”, the search should also return results for “engineering”, “engineers”, and “engineered”.
1. Spell checking - present corrected search terms for user misspellings.

## We’re looking for the following: :eyeglasses: ##

- Can you research and learn something new that you might not be entirely familiar with?
- Can you manage your time effectively to deliver something of value? 
- Can you design something that is easy-to-use and visually pleasing?
- Can you use version control correctly?
- Can you follow instructions and meet scope requirements?

:warning: When you are finished, please update your readme with a description of which features you implemented and any feedback on the test. Then submit the GitHub link to your finished project to us via email. 

## Updates: ##
- 2023-06-08 Added some sample code to serve as a starting point (delete indexed data, index new data, perform simple search on indexed data).
- 2023-06-19 Added a note to update the readme with their own notes and experience.
- 2023-09-08 Added notes that advanced features are optional due to reduced time expectations. Added notes about what we're looking for.
- 2023-09-25 Updates to reduce the scope of the assignment and further shorten time requirements.
- 2024-07-09 Added notes to clarify that assignment github link should be submitted via email.

## Updates by Krupali : ##
- Implemented basic features
    a. Voting Average - Added in the return search details. The results are now filtered to show movies with vote average greater than or equal to the value passed.
    b. Release Date - Added release date in the return search details. Included release date as an advanced search parameter. The results are now filtered based on value passed.
    c. Enhanced Page Styling and Layout
        - Added "Advanced Search" Button: Clicking this button will open the advanced search filters.
        - Implemented Hover Effects: Added hover effects on movie titles in the search results.
        - Formatted Movie Details: Movie details are now displayed in a pipe-separated format (Duration | Revenue | Voting Average | Release Date).
        - Duration: Formatted to a readable format (example: 2h 15m).
        - Revenue: Formatted to display values in the nearest thousands or millions (example: $136.98M).

- Implemented advanced features
    a. Autocomplete: This feature has been implemented and provides users with up to 10 suggestions as they begin typing movie names.
       example: If the user inputs "veg" then application provides 10 suggestions like "Leaving Las Vegas","I shot a man in Vegas", etc.

    b. Stemming: This feature has been implemented to display both the original movie titles and their corresponding stemmed versions based on the input text.
       example: If the user inputs "engineered" the results returned include stemmed words like "engineer", "engineering" and "engineers".

    c. Spell Check: This feature has been implemented to provide spell check suggestions for autocomplete. Users will receive movie title suggestions with corrected spelling.
       example: If the user inputs "conspiract", the autocompleted suggestions include moovies like "Shadow Conspiracy", "Conspiracy theory", etc.

- ## Additional Notes : ##
    - The previous functionality, where advanced filters are applied only when there is input in the search bar, remains unchanged.
    - When a specific movie is selected from the suggestions, the search results will display the selected movie along with other movies containing the relevant keywords. Note that stemming is not applied in this scenario. Additionally, movies with special or accented characters may require further development for improved handling.
    - When a keyword like "Jump" is searched, the application first performs stemming and then provides results for all variations of the stemmed words.

## Future Possible Enhancements:##
    - Improved Pagination: Enhance the pagination system for better user experience.
    - Sorting Options: Add sorting functionality to organize search results based on various criteria.
    - Improve the search logic to include accented characters, stopset words and special charactes.

## Feedback
    - The dataset provides very good test case scenarios with respect to stop keywords
    - This test helped me to get good exposure and learning to lucene.net technologies such as phrase queries, boolean query, term query