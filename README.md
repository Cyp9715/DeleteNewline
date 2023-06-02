If you paste text with newlines, the text with the newlines deleted is automatically copied to the clipboard.

## Introduce

<img src=https://i.imgur.com/B5RI2uF.png>

It is often necessary to insert the PDF text into a translator unless the research paper is written in your language.  
Unfortunately, PDFs have newlines that prevent translator from recognizing the text.  
this project was created to easily delete these newline characters.

You can use other programs or websites, but the convenience provided by DeleteNewline will be the best.

## Release environment
OS : Windows  
.NET : 6.0 LTS

Currently, my personal use environment stays in Windows, so there is no plan to support MAC environment.

## How to Use?
* Copy the text and copy it to the appropriate application window.  
* Copy the text, right-click on the appropriate program window, and then click the Paste button.  
* Drag the text and press the **Alt + F1** button.

perhaps the last function is most useful.

Text with Newline removed is located on your 'clipboard'. You can check it by using the Win + V key or Ctrl + V key.

## Matter under consideration.
If there are no problems outside of development(Rule), I willing to connect translator such as Google, Papago, and Bing directly to the DeleteNewline. (Probably not the way to implement it using the API.)
