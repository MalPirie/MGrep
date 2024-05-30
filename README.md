# MGrep

I deal with a lot of large log files as part of my day job. Finding instances of
strings in these files can be difficult, especially when the number of instances is
high. I was using an existing tool to do this, but it turned out to be missing files
and not finding all instances. So I decided to write my own tool.

I wanted something that was easy to use with the basic functionality I needed, but was
also fast and reliable.

## Usage

Enter the path from which to start the search. The `Browse` button opens a dialog to
select the folder. A history of previous folders is kept.

Enter the pattern for which to search. The _match case_, _match whole word_ and _use
regular expressions_ options can be used to refine the search. A history of search
patterns is kept.

Enter an optional list of files and folders patterns that should be included or
excluded from the search. The patterns are separated by the pipe character '|'.
Patterns to be excluded are prefixed by a minus sign. The _globbing_ and _include
subfolders_ options can be used to refine the use of the patterns.

Click the `Search` button to start the search. The results are displayed in the list
box. The tooltip for each match is the file name, relative to the search folder, of the
matching file.

The results can be saved to a file by clicking the `Export` button. The output is the
lines that matched the search pattern. No file information is included.

## Options

### Match case

The search is case insensitive by default. Check this option to make the search case
sensitive.

### Match whole word

The search matches only entire words and not text that is part of a larger word.

### Use regular expressions

The search pattern is treated as a literal string by default. Check this option to
treat the search pattern as a regular expression.

### Globbing

A glob is a term used to define patterns for matching file and directory names based
on wildcards. A star character `*` matches zero or more characters in the part of the
file path. Two star characters `**` matches an arbitrary folder depth. For example,
the pattern  `**\*.txt` matches all text files in all subfolders.

### Include subfolders

The search is limited to the specified folder by default. Check this option to include
all subfolders in the search. This option is mutually exclusive with the _globbing_
option.

### Include binary files

The search is limited to text files by default. Check this option to include binary
files in the search. A text file is defined as a file that has a BOM, byte order mark,
or does not contain a null character within the first 1000 characters.