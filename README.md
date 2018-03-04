# Planetzine
A sample app using Azure Cosmos DB.

This project demonstrates how to build a news web app that uses a globally distributed Azure Cosmos DB. It demonstrates the following functionality:
* Reading from the nearest database location ("local reads")
* Search articles by author, tags or full-text search
* Populating with sample articles (from Wikipedia)
* Measuring the number of Request Units consumed

## Setup
### Deploy to Azure
[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjahlen%2Fplanetzine%2Fmaster%2Fazuredeploy.json)

You must chose a unique Site Name, for example **planetzine-yourname**.

* If you have provided a *Secondary Location*, the template will create an Azure Traffic Manager and two web apps. The address would then be something like: **http://planetzine-yourname.trafficmanager.net**
* If you don't have a *Secondary Location*, there will only be one web app. The address will be something like: **http://planetzine-yourname-region.azurewebsites.net**

Note that to support Azure Traffic Manager, you must select at least Sku S1.

### Test locally
* Create the necessary services in Azure (either manually or by clicking the "Deploy to Azure button" above: 
  * Azure Cosmos DB account
  * One or more Azure Web Apps
  * Azure Traffic Manager
* Edit **Web.config**
  * **EndpointURL** and **AuthKey** must be changed to point to your own account
  * The remaining parameters can optionally be changed, but their default values should work.

You can run the web app locally in Visual Studio. You can also replace the Azure Cosmos DB account with the Azure Cosmos DB emulator.


## More reading
Visit [my blog](https://www.johanahlen.info/en/tag/azure-cosmos-db/) for more reading about Azure Cosmos DB


## Screenshots
![Planetzine screenshot 1](/SCREENSHOT1.png?raw=true "Planetzine screenshot 1")
![Planetzine screenshot 2](/SCREENSHOT2.png?raw=true "Planetzine screenshot 2")

