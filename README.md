# Planetzine
A sample app using Azure Cosmos DB.

This project shows how to build a news web app that uses a globally distributed Azure Cosmos DB. It demonstrates the following functionality:
* Reading from the nearest database location ("local reads")
* Search articles by author, tags or full-text search
* Populating with sample articles (from Wikipedia)
* Measuring the number of Request Units consumed

## Setup
### Test in Azure
[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjahlen%2Fplanetzine%2Fmaster%2Fazuredeploy.json)

Click the "Deploy to Azure" button. You must chose a unique Site Name, for example **planetzine-yourname**.

* If you have provided a *Secondary Location*, there will be an Azure Traffic Manager and two web apps. The address would then be something like: **http://planetzine-yourname.trafficmanager.net**
* If you don't have a *Secondary Location*, there will only be one web app and no Azure Traffic Manager. The address will be something like: **http://planetzine-yourname-region.azurewebsites.net**

Have patience! The deployment in the Azure Portal can take around 10 minutes.

Note that to support Azure Traffic Manager, you must select at least Sku S1.

### Develop and test locally
* Create an Azure Cosmos DB account in Azure (or use the Azure Cosmos DB Emulator). Select the SQL API.
* Download the source code and open in Visual Studio. Edit **Web.config**.
  * **EndpointURL** and **AuthKey** must be changed to point to your Azure Cosmos DB account
  * The remaining parameters can optionally be changed, but their default values should work.

You can create an Azure Cosmos DB account for free [here](https://azure.microsoft.com/en-us/try/cosmosdb/).


## More reading
Visit [my blog](https://www.johanahlen.info/en/tag/azure-cosmos-db/) for more reading about Azure Cosmos DB


## Screenshots
![Planetzine screenshot 1](/SCREENSHOT1.png?raw=true "Planetzine screenshot 1")
![Planetzine screenshot 2](/SCREENSHOT2.png?raw=true "Planetzine screenshot 2")

