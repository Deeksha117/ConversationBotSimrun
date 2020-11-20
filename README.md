
# Teams Conversation Bot

SimRUN is a Microsoft Teams conversation bot created to manage a Virtual Running event.

'SimRUN' allowed people to log their entry for the day in terms of "How many kms run"
The architecture consisted of a webservice and a SQL database. Azure WebService provided APIs to interact and saved the data to the SQL server based on user inputs.

This bot has been created using [Bot Framework](https://dev.botframework.com). This sample shows
how to incorporate basic conversational flow into a Teams application. It also illustrates a few of the Teams specific calls you can make from your bot.

## Prerequisites

- Microsoft Teams is installed and you have an account
- [.NET Core SDK](https://dotnet.microsoft.com/download) version 3.1
- [ngrok](https://ngrok.com/) or equivalent tunnelling solution

##To Try
1) If you are using Visual Studio
   - Launch Visual Studio
   - File -> Open -> Project/Solution
   - Select `TeamsConversationBot.csproj` file

1) For running locally - Run ngrok - point to port 3978

    ```bash
    ngrok http -host-header=rewrite 3978
    ```

1) Create [Bot Framework registration resource](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart-registration) in Azure
    - Use the current `https` URL you were given by running ngrok. Append with the path `/api/messages` used by this sample
    - Ensure that you've [enabled the Teams Channel](https://docs.microsoft.com/en-us/azure/bot-service/channel-connect-teams?view=azure-bot-service-4.0)
    - __*If you don't have an Azure account*__ you can use this [Bot Framework registration](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/create-a-bot-for-teams#register-your-web-service-with-the-bot-framework)

1) Update the `appsettings.json` configuration for the bot to use the Microsoft App Id and App Password from the Bot Framework registration. (Note the App Password is referred to as the "client secret" in the azure portal and you can always create a new client secret anytime.)

1) __*This step is specific to Teams.*__
    - **Edit** the `manifest.json` contained in the  `teamsAppManifest` folder to replace your Microsoft App Id (that was created when you registered your bot earlier) *everywhere* you see the place holder string `<<YOUR-MICROSOFT-APP-ID>>` (depending on the scenario the Microsoft App Id may occur multiple times in the `manifest.json`)
    - **Zip** up the contents of the `teamsAppManifest` folder to create a `manifest.zip`
    - **Upload** the `manifest.zip` to Teams (in the Apps view click "Upload a custom app")

1) Run your bot, either from Visual Studio with `F5` or using `dotnet run` in the appropriate folder.

## Interacting with the bot

You can interact with this bot by sending it a message, or selecting a command from the command list. The bot will respond to the following strings.

1. **Hi**
  - **Result:** The bot will send the welcome card for you to interact with, provde you a list of all possible commands that bot understands
2. **Add me to DNS team**
  - **Result:** The bot will respond to the message if user was succesfully added or not.
  In case of success - the response is "Sucessfully registered you to team DNS"
  In case of failure - the response is "Please enter valid team name from list"
3. **get my total, get team total, get rank, get overall team stats, get team stats for today**
  - **Result:** The bot will send a 1-on-1 message to each member in the current conversation based on the query after fetching aggregarted data from SQL server

You can select an option from the command list by typing ```@TeamsConversationBot``` into the compose message area and ```What can I do?``` text above the compose area.

## Deploy the bot to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.

## Further reading

- [How Microsoft Teams bots work](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-basics-teams?view=azure-bot-service-4.0&tabs=javascript)
   This is the doc I followed https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/create-a-bot-for-teams
 
   And picked skeleton source code form https://github.com/microsoft/BotBuilder-Samples/tree/main/samples/csharp_dotnetcore/57.teams-conversation-bot
