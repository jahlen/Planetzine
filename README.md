# Planetzine
A sample app using Azure Cosmos DB.

This project demonstrates how to build a news web app that uses a globally distributed Azure Cosmos DB. Combined with the Azure Traffic Manager, this can give very low latency for users all over the world.

To test the app, do the following:
* Create the necessary services in Azure: an Azure Cosmos DB account and at least one Azure Web App. Use another name than Planetzine, since it has to be unique in Azure.
* Edit Web.config with your own EndpointURL and AuthKey. Also edit the preferred locations.
* Test run the web app locally in Visual Studio. That will help you resolve any configuration errors.
* Publish the web app to Azure. If you have deployed your Azure Cosmos DB to the same regions as the Web Apps, you should get very low latency.
* Finally, you can add an Azure Traffic Manager to automatically direct users to the nearest Web App.

Planetzine also contains functionality to do performance testing on your Azure Cosmos DB accounts.


## More reading
Visit [my blog](https://www.johanahlen.info/en/tag/azure-cosmos-db/) for more reading about Azure Cosmos DB


## Screenshots
