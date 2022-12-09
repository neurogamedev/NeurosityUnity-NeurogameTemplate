# NeurosityUnity-NeurogameTemplate

![GitHub](https://img.shields.io/github/release/neuromodgames/NeurosityUnity-NeurogameTemplate?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/neuromodgames/NeurosityUnity-NeurogameTemplate?style=for-the-badge)

Neurogame Template using Ryan Turney's Neurosity SDK for Unity. Check examples of how your Neurosity device can easily be used in-game.

![DEMO_Unity-Template](https://user-images.githubusercontent.com/88777150/175022704-f246bcc2-8001-4bbd-bb8a-980f38e1c680.gif)

## What can you do with it?

You can use this template to start your first neurogame.
The template allows you to sign in to your devices and use a Notion Interfacer to customize the way you communicate with your Notion or Crown.
This project uses Unity 2022 and Text Mesh Pro as a future-proofing precaution.

## The shoulders of giants

This project was built upon the hard work of others. As such, it is also dependent on the scrutiny and update of the tools they so generously provide. Make sure to check their sites and repositories if you want to upgrade your own project or if you want to Build for mobile platforms.

- Thanks to [AJ Keller](https://www.linkedin.com/in/andrewjaykeller/) and [Alex Castillo](https://www.linkedin.com/in/alexcas/) for coming up with the [Crown](https://neurosity.co/) hardware and the [Neurosity SDK](https://docs.neurosity.co/docs/overview). 

- Thanks to [Ryan Turney](https://github.com/ryanturney) for developing the [Notion SDK for Unity](https://github.com/ryanturney/notion-unity). 

- Thanks to Unity Technologies for the [Starter Assets - Third Person Character Controller](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526) and for making [Unity](https://unity.com//) accessible to everyone.

## Where is the fun stuff?

If you want to modify how the handlers communicate with Unity, like I did, go to the *Assets/Scripts/Notion-Unity/Handlers* folder. The *Types* folder right beside it will help you with what kind of information is received from Firebase. Then link those functionalities to the `NotionInterfacer.cs` in the *Assets/Scripts/Template* folder.

If you're more into design and less into coding, you can find templates ready for you to use in the in the *Assets/Scenes* folder. In case you're wondering how the scripts in those scenes work, my contributions in the *Assets/Scripts/Template* folder are inundated with comments about how things work.

I left the Starter Assets folder untouched if you'd like to also use them for your prototypes.

## Dependencies
* [Unity 2022.2.0f1](https://unity3d.com/get-unity/download/archive)
* [Firebase for Unity Authentication](https://developers.google.com/unity/packages#firebase_authentication)
* [Firebase for Unity Realtime Database](https://developers.google.com/unity/packages#firebase_realtime_database)
* [Json.NET by jilleJr](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)
* [External Dependency Manager](https://developers.google.com/unity/packages#external_dependency_manager_for_unity)

Nota Bene: When updating to newer versions of Unity, be sure to update the external packages in *{Project Name}/Packages/* and in the `manifest.json` and `packages-lock.jason`.

## Using in Other Projects
Other apps will require your own Firebase project, you can follow [Firebase Documentation](https://firebase.google.com/docs/unity/setup) for help on that. There is a stub setup for this repo but any app developed using the Notion Unity SDK will eventually require you to setup your own Firebase account. This is currently a requirement as the Neurosity tech is built on top of Firebase and the Unity Firebase SDKs require `google-services.json` and `GoogleService-Into.plist` to be unique for each store app.

## !! Building and Sharing !!
Be sure to check my notes about encryption in the `NotionInterfacer.cs` in the *Assets/Scripts/Template* folder. Encryption is hard-coded and automatic deletion of your login info from the Device intances is not guarnteed. Try your best not to give away your login data when sharing your project.

## To Do

- Fix Logout() warnings stemming from processes being interrupted mid-thread.

## Frequently Asked Questions

**Q: Can this be used for mobile?**

A: Yes it can. Check my example reporsitory for mobile [here](https://github.com/neurogamedev/NeurosityUnity-MobileNeurogameTemplate).
