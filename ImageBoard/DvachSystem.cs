using System.Text.Json;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class DvachSystem : ImageboardSystem
{
	public DvachSystem(GraphicsWorker worker, Action<string> updateLoadStatus) : base(worker, updateLoadStatus) { }

	protected override bool TryGetNewPosts(int threadNum, Post[] prevPosts, out Post[] newPosts)
	{
		HttpClient httpClient = new();
		string requestString = $"https://2ch.hk/api/mobile/v2/info/vg/{threadNum}";

		#region Check new thread
		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(strJson);
			JsonElement root = document.RootElement;
			JsonElement posts = root.GetProperty("thread").GetProperty("posts");

			if (posts.TryGetInt32(out int postCount) && postCount > 999)
			{
				int num = GetThreadNumByTag();

				if (!prevPosts.Any((arg) => arg.Num == num))
				{
					newPosts = Array.Empty<Post>();
					return false; //GetAllPosts(httpClient, thread);
				}
			}
		}
		#endregion Check new thread

		int postNum = prevPosts.Last().Num;

		requestString = $"https://2ch.hk/api/mobile/v2/after/vg/{threadNum}/{postNum}";

		#region Check new posts in thread
		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(strJson);
			JsonElement root = document.RootElement;
			JsonElement posts = root.GetProperty("posts");

			List<Post> postList = new();

			foreach (JsonElement json in posts.EnumerateArray())
			{
				Post? post = JsonSerializer.Deserialize<Post>(JsonDocument.Parse(json.ToString()));

				if (post is null) throw new Exception();

				if (!prevPosts.Any((arg) => arg.Num == post.Num))
				{
					postList.Add(post);
				}
			}

			newPosts = postList.ToArray();
			return true;
		}
		#endregion Check new posts in thread

		//UpdateShowingPosts();
	}

	protected override int GetThreadNumByTag(string tag = "genshin")
	{
		HttpClient httpClient = new();

		using (HttpRequestMessage threadsRequest = new(HttpMethod.Get, "https://2ch.hk/vg/catalog_num.json"))
		{
			var responce = httpClient.Send(threadsRequest);
			string text = responce.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(text);
			JsonElement root = document.RootElement;
			JsonElement threads = root.GetProperty("threads");

			foreach (JsonElement thread in threads.EnumerateArray())
			{
				if (thread.TryGetProperty("tags", out JsonElement tagJson) && tagJson.GetString() == tag)
				{
					return thread.GetProperty("num").GetInt32();
				}
			}

			throw new Exception();
		}
	}

	protected override Post[] GetAllPosts(int threadNum)
	{
		HttpClient httpClient = new();
		List<Post> postList = new();

		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, $"https://2ch.hk/vg/res/{threadNum}.json"))
		{
			var responce = httpClient.Send(postsRequest);
			string strJson = responce.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(strJson);
			JsonElement root = document.RootElement;
			JsonElement threads = root.GetProperty("threads");
			JsonElement posts = threads.EnumerateArray().First().GetProperty("posts");
			foreach (JsonElement json in posts.EnumerateArray())
			{
				JsonDocument doc = JsonDocument.Parse(json.ToString());
				//Console.WriteLine(doc.RootElement.ToString());
				//Console.WriteLine();
				Post? post = JsonSerializer.Deserialize<Post>(doc);

				if (post is null) throw new Exception();

				postList.Add(post);
			}
		}

		return postList.ToArray();
	}
}