# Planetzine
A sample app using Azure Cosmos DB.

This project demonstrates how to build a news web app that uses a globally distributed Azure Cosmos DB. It demonstrates the following functionality:
* Reading from the nearest database location ("local reads")
* Search articles by author, tags or full-text search
* Populating with sample articles (from Wikipedia)
* Measuring the number of Request Units consumed

## Setup
* Create the necessary services in Azure: 
  * Azure Cosmos DB account
  * One or more Azure Web Apps
* Edit **Web.config**
  * **EndpointURL** and **AuthKey** must be changed to your own account
  * The remaining parameters can optionally be changed, but their default values should work.
* Test run the web app locally in Visual Studio. That will help you resolve any configuration errors.
* Publish the web app to Azure.
* Finally, you can add an Azure Traffic Manager to automatically direct users to the nearest Web App.


## More reading
Visit [my blog](https://www.johanahlen.info/en/tag/azure-cosmos-db/) for more reading about Azure Cosmos DB


## Screenshots
![Planetzine screenshot 1](/SCREENSHOT1.png?raw=true "Planetzine screenshot 1")
![Planetzine screenshot 2](/SCREENSHOT2.png?raw=true "Planetzine screenshot 2")

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)
