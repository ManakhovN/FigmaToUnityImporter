# Figma To Unity Importer

## Overall information
Tool that imports nodes from Figma into the Unity.
Before using I want u to notice, that the project is raw, and I can't promise you that I will develop it intensively.
Some features are not ready yet. Some features are impossible to quickly recreate in Unity.

## Contributing
You are welcome to contribute. And you are free to use this project for any purpose.

## Install
you can add `https://github.com/ManakhovN/FigmaToUnityImporter.git?path=/Assets/FigmaImporter` to Package Manager

## Usage
Firstly, you need to put FigmaImporter into your project.
Then new menu option will appear.
![Figma importer menu option](./ReadmeImages/step0.png)

Now press OpenOauthUrl button.

![Figma importer window](./ReadmeImages/step1.png)

It will redirect you to the access allowance page. Press "Allow access there"

![Figma access](./ReadmeImages/step2.png)

Then the callback page will be opened. Copy ClientCode and State into Figma Editor window. And press "GetToken" button. 

![Client params](./ReadmeImages/step3.png)

If token appeared, then you did everything right. If not, repeat the step with access allowance.

![Token](./ReadmeImages/step4.png)

Now you can copy the node link, and put it in the URL field in unity.

![Node link](./ReadmeImages/step5.png)

Now OpenScene with Canvas.
And press the "GetFile" button (I will rename it, I promise, but later). It takes some time to generate the node. And that's all.

![Get File](./ReadmeImages/step6.png)

There is also one thing with Fonts.
If you got the error about Font. You should add it in FontLinks.asset scriptable object.

![Font](./ReadmeImages/step7.png)
