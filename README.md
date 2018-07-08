# VNR Dictionary Filter
Performs various operations on Visual Novel Read (VNR) dictionary files.

# Usage Guide:
Input parameter details are as follows:
<pre><code>
DictFilter.exe
DictFilter.exe file_id   id [-g]
DictFilter.exe element   element_name value
DictFilter.exe merge     fileA fileB
DictFilter.exe remove    file file_id
DictFilter.exe print

Details:
   file_id   Returns game specific terms.
             File Ids can be found from Edit Dialog under Game
             info page. Multiple File Ids should be separated
             by comma. if -g is sepecified then full dictionary
             will be created, including global terms.
   element   Returns terms where <element_name> has value matching
             the given <value>. <value> can be a regular expression.
   merge     Merges two dictionary files and produces a new file.
             Both files should be present in current directory.
             Each file must have a root element as parent to make xml valid.
   remove    Remove game specific terms from given dict file.
             File Ids can be found from Edit Dialog under Game
             info page. Multiple File Ids should be separated by comma.
   print     Prints Id, Special, Pattern, Text and Game Id to xml file.

   If no parameter is provided, then global terms will be returned.
   </code></pre>