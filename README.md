# TxtCsvHelper

TxtCsvHelper is a library to assist in parsing CSV, or any other form of delimited files. You can fill models, fill dynamics, or simply get a list of fields by line.

## Installation

Use the package manager [console](https://www.nuget.org/packages/TxtCsvHelper/) to install TxtCsvHelper.

```bash
Install-Package TxtCsvHelper -Version 1.1.8
```

## Usage
using TxtCsvHelper;

To return an IEnumerable<T>. 
Parser can take a Stream, ReadStream, FileStream, MemoryStream or a string followed by 3 optional parameters: the delimiter character (will default to a comma). 
A bool if there is a header line (will default to true). 
A bool if there are spaces between delimiters and fields (will default to false). Deserialize takes a type of a class (model)
  
using(Parser pars = new Parser(streamvar, delimiter: ',', hasHeader: true, hasSpaces: false)<br>
{<br>
	var models = pars.Deserialize<Name>();<br>
}

To return an IEnumerable of type dynamic. Parser can take a Stream, ReadStream, FileStream, MemoryStream or a string. 
Followed by 3 optional parameters: the delimiter character (will default to a comma). 
A bool if there is a header line (will default to true but needs to be true for dynamic types). 
A bool if there are spaces between delimiters and fields (will default to false)

using(Parser pars = new Parser(streamvar, delimiter: ',', hasHeaders: true, hasSpaces: false)<br>
{<br>
	var models = pars.Deserialize();<br>
}

if the included header row does not match your model call deserialize in this way and set indexes in model

using (ReadStream rs = new ReadStream(stream: streamvar))<br>
using (Parser pars = new Parser(rs, delimiter: ',', hasHeaders: true, hasSpaces: false))<br>          
{<br>
	rs.ReadLine();<br>
	IEnumerable<Name> records = pars.Deserialize<Name>();<br>
}

To return an IEnumerable<string> for each line, ReadStream acts exactly like StreamReader. 
Parser in this case takes 3 optional parameters: the delimiter character (will default to a comma). 
A bool if there is a header line (will default to true). A bool if there are spaces between delimiters and fields (will default to false). 
SplitLine takes a Line of delimited fields. Followed by 2 optional parameters: the delimiter character (will default to a comma). 
A bool if there are spaces between delimiters and fields (will default to false)

using (ReadStream rs = new ReadStream(postedFile.OpenReadStream())<br>
using(Parser pars = new Parser())<br>
{<br>
	while(rs.Peek() >= 0)<br>
		IEnumerable<string> substrings = p.Splitline(rs.Readline());<br>
		//do something with the strings<br>
}

If no header exists, you may declare the index of the field in the model with [SetIndex()]
simply put the index starting with 0 inside the parentheses

using TxtCsvHelper;

public class Name<br>
    {<br>
        [SetIndex(1)]<br>
        public string LastName { get; set; }<br>
        [SetIndex(0)]<br>
        public string FirstName { get; set; }<br>
        [SetIndex(2)]<br>
        public string MiddleName { get; set; }<br>
        [SetIndex(3)]<br>
        public int Age { get; set; }<br>
    }
```
