using System.Windows.Forms;
using GenshinImpactOverlay.Menus;

namespace GenshinImpactOverlay.ImageBoard;

internal abstract class ImageboardSystem : IUseMenu
{
	private MenuItem? _menuItem;
	/// <summary>
	/// Название системы для генерации названий
	/// </summary>
	public string Sysname => nameof(DvachSystem);

	/// <summary>
	/// Разворот поста на котором фокус
	/// </summary>
	private bool ExtendFocused { get; set; } = true;

	/// <summary>
	/// Выбранный пост
	/// </summary>
	private Post? FocusPost { get; set; }

	/// <summary>
	/// Частота обновления постов
	/// </summary>
	private System.Timers.Timer GetNewPostsTimer { get; set; } = new(5000);

	/// <summary>
	/// Режим веток
	/// </summary>
	private bool NodeMode { get; set; } = false;

	/// <summary>
	/// Прозрачность изображений на постах
	/// </summary>
	private float Opacity { get; set; } = 0.5f;

	/// <summary>
	/// Все посты треда
	/// </summary>
	private LinkedList<Post> Posts { get; } = new();
	/// <summary>
	/// Посты для отображения
	/// </summary>
	private List<Post> ShowedPosts { get; set; } = new();

	/// <summary>
	/// Обработчик графики
	/// </summary>
	private GraphicsWorker Worker { get; init; }

	public ImageboardSystem(GraphicsWorker worker, Action<string> updateLoadStatus)
	{
		Worker = worker;

		Post.FontIndex = worker.AddFont("Consolas", 14);
		Post.BFontIndex = worker.AddFont("Consolas", 14, bold: true);

		Post.WhiteBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		Post.BlackBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		int threadNum = GetThreadNumByTag();

		foreach (var post in GetAllPosts(threadNum))
		{
			Posts.AddLast(post);
			SyncPostLinks(post);
		}

		UpdateShowingPosts();

		GetNewPostsTimer.Elapsed += (_, _) =>
		{
			int num = Posts.First.Value.Num;

			if (TryGetNewPosts(num, Posts.ToArray(), out Post[] newPosts))
			{
				foreach (var post in newPosts)
				{
					Posts.AddLast(post);
					SyncPostLinks(post);
				}
			}
			else
			{
				int threadNum = GetThreadNumByTag();

				foreach (var post in GetAllPosts(threadNum))
				{
					Posts.AddLast(post);
					SyncPostLinks(post);
				}
			}

			UpdateShowingPosts();
		};

		GetNewPostsTimer.AutoReset = true;
		GetNewPostsTimer.Enabled = true;

		Worker.OnDrawGraphics += Graphics_OnDrawGraphics;

		InputHook.OnKeyUp += InputHook_OnKeyUp;
	}

	protected abstract int GetThreadNumByTag(string tag = "genshin");
	protected abstract Post[] GetAllPosts(int threadNum);
	protected abstract bool TryGetNewPosts(int threadNum, Post[] prevPosts, out Post[] newPosts);

	private void Graphics_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e)
	{
		int escape = 15;
		int bottom = 720;
		int left = 80;
		int upper = 0;

		if (Posts.Count == 0) return;

		foreach (Post post in ShowedPosts.Reverse<Post>())
		{
			upper += post.DrawPost(Worker, e.Graphics, Opacity, bottom - upper, left, FocusPost == post, FocusPost == post && ExtendFocused);

			upper += escape; //небольшой отступ между постами
		}
	}

	private void InputHook_OnKeyUp(object? sender, EventsArgs.OnKeyUpEventArgs eventArgs)
	{

	}

	private void SyncPostLinks(Post post)
	{
		List<int> links = post.GetPostLinks().ToList();

		if (links.Count > 0)
		{
			foreach (var item in Posts)
			{
				if (!links.Any()) break;
				if (item is null) continue;

				if (links.Contains(item.Num))
				{
					//Console.Write($"{item.Num} ");
					item.AddReference(post.Num);
					_ = links.Remove(item.Num);
				}
			}
		}
	}

	private void UpdateShowingPosts()
	{
		if (FocusPost is null) ShowedPosts = new(Posts.TakeLast(5));
		else if (!NodeMode)
		{
			LinkedListNode<Post> TargetPostNode = Posts.FindLast(FocusPost) ?? throw new NullReferenceException();

			List<Post> list = new()
		{
			TargetPostNode.Value
		};

			var prepList = new List<Post>();
			LinkedListNode<Post>? previous = TargetPostNode.Previous;
			while (prepList.Count < 4)
			{
				if (previous is null) break;

				prepList.Add(previous.Value);

				previous = previous.Previous;
			}

			var nextList = new List<Post>();
			LinkedListNode<Post>? next = TargetPostNode.Next;
			while (nextList.Count < 4)
			{
				if (next is null) break;

				nextList.Add(next.Value);

				next = next.Next;
			}

			int index = 0;
			while (list.Count < 5)
			{
				if (prepList.Count > index)
				{
					list.Insert(0, prepList[index]);
				}
				if (nextList.Count > index)
				{
					list.Add(nextList[index]);
				}
				index++;
			}

			ShowedPosts = new(list);
		}
		else
		{

		}
	}

	#region IUseMenu
	MenuItem? IUseMenu.GetMenu(Action<MenuItem> updateMenuFunc, Action<bool?> keyInputSwitchFunc)
	{
		const string PATH = "Resources/Icons";

		_menuItem ??= new(Sysname, $"{PATH}/conversation.png", childMenus: new()
		{
			GetSettingsMenu(),
			GetSurfMenu(),
		});

		return _menuItem;

		MenuItem GetSettingsMenu()
		{
			const string SETNAME = "settings";

			List<MenuItem> childs = new()
			{
				new($"{Sysname}_{SETNAME}_{nameof(ExtendFocused)}", $"{PATH}/magnifying-glass.png", new() {
					{ Keys.Clear, () => ExtendFocused = !ExtendFocused }
				}),
				new($"{Sysname}_{SETNAME}_{nameof(Opacity)}", $"{PATH}/headphones.png", new() {
					{ Keys.Clear, () => keyInputSwitchFunc(null) },
					{ Keys.Left, () => { if (Opacity > 0f) Opacity -= 0.1f; } },
					{ Keys.Right, () => { if (Opacity < 1f) Opacity += 0.1f; } }
				}),
			};

			return new($"{Sysname}_{SETNAME}", $"{PATH}/pause-button.png", childMenus: childs);
		}

		MenuItem GetSurfMenu()
		{
			const string SETNAME = "surf";

			return new($"{Sysname}_{SETNAME}", $"{PATH}/mesh-network.png", new()
			{
				{ Keys.Clear, () => {
					if (FocusPost is null)
					{
						keyInputSwitchFunc(true);
						FocusPost = Posts.Last();
					}
					else
					{
						FocusPost = null;
						keyInputSwitchFunc(false);
					}

					UpdateShowingPosts();
				} },
				{ Keys.Up, () => {
					keyInputSwitchFunc(true);
					if (FocusPost is null) FocusPost = Posts.Last();
					else if (FocusPost != Posts.First()) FocusPost = Posts.FindLast(FocusPost).Previous.Value;
					else return;

					UpdateShowingPosts();
				} },
				{ Keys.Down, () => {
					if (FocusPost is null) return;
					else  if (FocusPost != Posts.Last()) FocusPost = Posts.FindLast(FocusPost).Next.Value;
					else
					{
						FocusPost = null;
						keyInputSwitchFunc(false);
					}
					UpdateShowingPosts();
				} },
			});
		}
	}
	#endregion
}