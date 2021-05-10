# TxtCsvHelper

TxtCsvHelper is a library to assist in parsing CSV, or any other form of delimited files. You can fill models, fill dynamics, or simply get a list of fields by line.

## Installation

Use the package manager [console](https://www.nuget.org/packages/TxtCsvHelper/) to install TxtCsvHelper.

```bash
Install-Package TxtCsvHelper -Version 1.2.1
```

## Usage
```
using TxtCsvHelper;
```

To return an IEnumerable<T>. Dynamic types must be IEnumerable<dynamic>
Parser can take a Stream, ReadStream, FileStream, MemoryStream or a string followed by 3 optional parameters: the delimiter character (will default to a comma). 
A bool if there is a header line (will default to true). 
A bool if there are spaces between delimiters and fields (will default to false). Deserialize takes a type.
```
using(Parser pars = new Parser(streamvar, delimiter: ',', hasHeader: true, hasSpaces: false))
{
	var models = pars.Deserialize<Person>();
}
```
To return an IEnumerable<string> for each line, ReadStream acts exactly like StreamReader. 
Parser in this case takes 3 optional parameters: the delimiter character (will default to a comma). 
A bool if there is a header line (will default to true). A bool if there are spaces between delimiters and fields (will default to false). 
SplitLine takes a Line of delimited fields. Followed by 2 optional parameters: the delimiter character (will default to a comma). 
A bool if there are spaces between delimiters and fields (will default to false)
```
using (ReadStream rs = new ReadStream(postedFile.OpenReadStream()))
using(Parser pars = new Parser())
{
	while(rs.Peek() >= 0)
		var substrings = p.Splitline(rs.Readline());
		//do something with the strings
}
```
If no header exists, you may declare the index of the field in the model with [SetIndex()]
simply put the index starting with 0 inside the parentheses
If a header exists but the names do not match your model, you may declare the header name with [HeaderName()]
simply put the column name in the parentheses
```
public class Person
{
	[SetIndex(1)]
	[HeaderName(Last)]
        public string LastName { get; set; }
        [SetIndex(0)]
        public string FirstName { get; set; }
        [SetIndex(2)]
        public string MiddleName { get; set; }
        [SetIndex(3)]
        public int Age { get; set; }
}
```
