If you paste text with newlines, the text with the newlines deleted is automatically copied to the clipboard.

## Introduce
It is often necessary to insert the PDF text into a translator unless the research paper is written in your language.  
unfortunately, however, PDFs often have newlines, which are not well recognized by translators.  
because of this, I often have to delete newline characters one by one. This project was created to reduce this hassle.

you can use other programs or websites, but this program is the best for the convenience of the features it provides.

## Development environment
.NET : 6.0 LTS

## How to Use?
* Copy the text and copy it to the appropriate application window.  
* Copy the text, right-click on the appropriate program window, and then click the Paste button.  
* Drag the text and press the **Alt + F1** button.

Text with Newline removed is located on your 'clipboard'. You can check it by using the Win + V key or Ctrl + V key.

## Why develop this way?
Take a research paper as an example, and each paper has a different form. there are times when the distinction of Newline is ambiguous.  
therefore, rather than creating a program that handles all of these processes, I used this method to achieve maximum efficiency in some sections through these simple programs, reducing user fatigue.

## direction of development
I also thought about whether to perform the translation process itself through the app through API Key (Azure, Google) that each user has.  
but on reflection, I thought it was inappropriate for this program to perform so many functions.  
if necessary later, the OCR function can be added. There are currently no plans.

if you think there is a feature you need, please contribute.
