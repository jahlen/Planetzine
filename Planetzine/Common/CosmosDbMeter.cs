using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planetzine.Common
{
    public class CosmosDbMeter
    {
        private readonly RequestDelegate _next;

        public CosmosDbMeter(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Items["InitialRequestCharge"] = CosmosDbHelper.RequestCharge; // Preserve RequestCharge at the beginning of the request
            await _next.Invoke(context);
        }

        public static double GetConsumedRUs(HttpContext context)
        {
            return CosmosDbHelper.RequestCharge - (double)context.Items["InitialRequestCharge"]; // Check how much RequestCharge has increased
        }
    }
}
