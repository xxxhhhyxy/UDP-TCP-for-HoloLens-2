#  UDP-TCP-for-HoloLens-2

This is a HoloLens2-MRTK-ready template, with TCP and UDP web modules integrated, based on:
 
    ·Unity 2020.3.3f1(LTS)     
    ·OpenXR features    
    ·Windows Mixed Reality Toolkit (MRTK) 2.7.2
    
   
For the users:

    ·Step 1. download it and deploy this project to HoloLens2 directly.
    ·Step 2. In Unity Editor, go to the top and click : Mixed Reality--> Toolkit --> Untilities --> Configure Project for MRTK --> Apply Settings, this step will help your project re-configure for MRTK and avoid errors.
    ·Step 3. In Unity Editor, find the scene "SampleScene" in the folder "Scenes", in this scene you can find the gameobject "UDP Communication" as an example
    ·Step 4. f_Init() is the initialization function in both TCP and UDP scripts. In my project, I use a globalCtrl script to call this function and then activate the web connection; but for you, you can directly change the name of this function to be "Start()", so it will start automatically.

One finding is that the TCP connection reads too slowly, I don't know why. I will be very happy if one day you let me know that you create a better TCP connection.

Tips: 

    ·Step 1. You had better name the solution folder as "APP" when you build this project from Unity, because I have already add this name in the gitignore file             
    ·Step 2. Give me a Star please, this took me 2 full weeks because I am not a professional programmer.




xxxhhhyxy

03.11.2021
