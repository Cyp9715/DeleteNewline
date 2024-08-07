If you paste text with newlines, the text with the newlines deleted is automatically copied to the clipboard.

## Introduce

<img src=https://i.imgur.com/B5RI2uF.png>

It is often necessary to insert the PDF text into a translator unless the research paper is written in your language.  
Unfortunately, PDFs have newlines that prevent translator from recognizing the text.  
this project was created to easily delete these newline characters.

You can use other programs or websites, but the convenience provided by DeleteNewline will be the best.

## Release environment
* OS : Windows  
* .NET : 8.0 LTS

Currently, my personal use environment stays in Windows, so there is no plan to support MAC environment.

## How to Use?
* Copy the text and copy it to the appropriate application window.  
* Copy the text, right-click on the appropriate program window, and then click the Paste button.  
* Drag the text and press the **Alt + F1** button. (called keybind)

Text with Newline removed is located on your 'clipboard'. You can check it by using the Win + V key or Ctrl + V key.

Additionally supports the Delete Newline regular expression.  
Users can conveniently extract the converted string using a combination of regular expressions and keybind options.

Please note that the keybind option may not run in certain programs due to permission issues. (Ex:League of Legends Client, Etc...) In this case. Run the program with administrator privileges.

## Download

Delete Newline is free.

The executable file (exe) of Delete Newline is provided only up to version 1.2.7.  
Starting from version 1.3.0, it can be downloaded using the [MS Store](https://apps.microsoft.com/store/detail/delete-newline/9NC17SL0VV5S).

## sneak peek Ver 2.0

You will be able to apply Regex in Chain form as shown below.  

![image](https://github.com/Cyp9715/DeleteNewline/assets/16573620/448f17f4-3ff8-4767-bb14-a487609c2061)

The problem is that I am late in updating this due to my personal schedule.  
If you are a programmer, you can use the feature by cloning and building the latest version.

I plan to optimize the code written to date in version 1.4.0 and fix all bugs if possible, so it could be completed in the second half of 2024.
