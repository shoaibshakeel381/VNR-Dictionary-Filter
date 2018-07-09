# VNR Dictionary Filter
Performs various operations on Visual Novel Reader (VNR) dictionary files.

# Usage Guide:
Input parameter details are as follows:
<pre><code>
Usage:
   DictFilter.exe                dictionary_file
   DictFilter.exe gamespecific   dictionary_file   game_file_id
   DictFilter.exe element        dictionary_file   element_name      value
   DictFilter.exe merge          dictionary_fileA  dictionary_fileB
   DictFilter.exe remove         dictionary_file   game_file_id

Details:
   gamespecific    Returns game specific terms. Filteration will be
                   done by Game File Ids. File Ids can be found from Edit
                   Dialog under Game info page. Multiple ids
                   should be separated by comma.
   element         Returns terms where element_name has value matching
                   the given value. Value can be a regular expression.
                   Any terms which don't have elemnt information will
                   be ignored.
   merge           Merges two dictionary files and produces a new file.
                   Both files should be present in current directory.
                   Each file must have a root element as parent to make xml valid.
   remove          Remove game specific terms from given dict file.
                   File Ids can be found from Edit Dialog under Game
                   info page. Multiple ids should be separated by comma.
                   If file_id is not specified then only disabled terms
                   will be removed.

   If only dictionary file is provided without any other parameters then global
   terms will be returned.
   If no parameter is provided, then this guide will be printed.

NOTE: Disabled terms will be always be ignored.
</code></pre>