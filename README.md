# .Net Core application example for Cloud Foundry

### Description

Full description can be found in this article: __________

This is an example of .NET Core web application based on a microservices architecture using Cloud Foundry, Steeltoe library, RabbitMQ message broker and Microsoft Cognitive Services API.
The app allows users to detect human faces on a photo, extract avatars from them and send by email.
The solution consists of a website and two services. All communication between the website and the services is done via RabbitMQ queues. Implementation of the Competing Consumers pattern makes the system reliable and scalable, while also helping us to decouple this system into small components.

### Building

Enter your credentials for SMTP server and Microsoft Cognitive Services account in source code (AvatarMaker.Recognizer/Program.cs, AvatarMaker.EmailSender/Program.cs)

You have to publish all 3 projects, run these commands in corresponding folders:

```
dotnet restore
dotnet publish -o publish -c Release
```

### Publishing to Cloud Foundry

Create an instance of RabbitMQ service:

```
cf create-service p-rabbitmq standard avatarmaker.rmq
cf push -p publish

```

Deploy applications, run this command for each of 3 projects:

```
cf push -p publish

```

### How to use

Open the deployed web app (AvatarMaker.Web) in browser, enter URL of any image and your email and click the button.


Enjoy!