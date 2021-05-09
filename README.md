# FigmaToUnityImporter
##Overall information
Project that imports nodes from Figma into unity.
Before using I want u to notice, that the project is raw, and i can't promise you that I will develop it intensvely. 
##Contributing
I can't promise you, that I will develop this project too much. So you are welcome to contribute. And you are free to use this project for any purpose.
##Usage
Firstly, you need to put FigmaImporter into your project.
Then new menu option will appear.
![Figma importer menu option](./ReadmeImages/step0.png)

Now press OpenOauthUrl button.
![Figma importer window](./ReadmeImages/step1.png)

It will redirect you to access allowance page. Press "Allow access there"
![Figma access](./ReadmeImages/step2.png)

Then the callback page will be opened. Copy ClientCode and State into Figma Editor window. And press "GetToken" button. 
![Client params](./ReadmeImages/step3.png)

If token appeard, then you did everything right. If not, repeat step with accces allowance. 
![Token](./ReadmeImages/step4.png)

Now you can copy node link, and put it in URL field in unity.
![Node link](./ReadmeImages/step5.png)

Now OpenScene with Canvas.
And press "GetFile" button (I will rename it, I promise, but later). It will take some time to generate the node. And that's all.
![Get File](./ReadmeImages/step6.png)

There is also one thing with Fonts.
If you got error about Font. You should add it in FontLinks.asset scriptable object.
![Font](./ReadmeImages/step7.png)
