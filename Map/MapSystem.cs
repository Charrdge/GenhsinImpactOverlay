using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GenshinImpactOverlay.Map
{
	internal class MapSystem
	{
		public MapSystem()
		{
			string str = "https://act.hoyolab.com/ys/app/interactive-map/index.html?lang=en-us#/map/2" +
				"?shown_types=389&center=2671.00,-1158.00&zoom=-3.00";

			using (HttpRequestMessage threadsRequest = new(HttpMethod.Get, str))
			{
				HttpClient httpClient = new();
				var responce = httpClient.Send(threadsRequest);
				string text = responce.Content.ReadAsStringAsync().Result;
				Console.WriteLine(text);
			}
		}
	}
}
