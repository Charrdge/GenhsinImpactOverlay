using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using GameOverlay.Drawing;

using GenshinImpactOverlay.GraphicWorkers;

namespace GenshinImpactOverlay.ImageBoard;

internal class Post
{
	#region Resources
	public static FontHandler Font { get; set; }
	public static FontHandler BFont { get; set; }
	public static SolidBrushHandler WhiteBrush { get; set; }
	public static SolidBrushHandler BlackBrush { get; set; }
	#endregion Resources

	#region Required
	[JsonPropertyName("num")] public int Num { get; set; }

	[JsonPropertyName("parent")] public int Parent { get; set; }

	[JsonPropertyName("board")] public string Board { get; set; }

	[JsonPropertyName("timestamp")] public int Timestamp { get; set; }

	[JsonPropertyName("lasthit")] public int Lasthit { get; set; }

	[JsonPropertyName("date")] public string Date { get; set; }

	private string? _cleanedComment;
	/// <summary>
	/// Текст поста без символов разметки
	/// </summary>
	[JsonIgnore()] public string CleanedComment
	{ 
		get
		{
			if (_cleanedComment is null)
			{
				RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline;
				Regex regex = new(@"<(\w?\W?\s?)[^>]*>", regexOptions);

				_cleanedComment = regex
					.Replace(Comment
					.Replace("\u0026#47;", "/")
					.Replace("&gt;", ">")
					.Replace("<br>", "\n"), "");
			}
			return _cleanedComment;
		} 
	}
	/// <summary>
	/// Текст поста
	/// </summary>
	[JsonPropertyName("comment")] public string Comment { get; set; }

	[JsonPropertyName("sticky")] public int Sticky { get; set; }

	[JsonPropertyName("endless")] public int Endless { get; set; }

	[JsonPropertyName("closed")] public int Closed { get; set; }

	[JsonPropertyName("banned")] public int Banned { get; set; }

	[JsonPropertyName("op")] public int Op { get; set; }
	#endregion Required

	#region Optional
	[JsonPropertyName("files")] public File[]? Files { get; set; }

	[JsonPropertyName("views")] public int? Views { get; set; }

	[JsonPropertyName("email")] public string? Email { get; set; }

	[JsonPropertyName("subject")] public string? Subject { get; set; }

	[JsonPropertyName("name")] public string? Name { get; set; }

	[JsonPropertyName("icon")] public string? Icon { get; set; }

	[JsonPropertyName("trip")] public string? Trip { get; set; }

	[JsonPropertyName("trip_style")] public string? TripStyle { get; set; }

	[JsonPropertyName("tags")] public string? Tags { get; set; }

	[JsonPropertyName("likes")] public int? Likes { get; set; }

	[JsonPropertyName("dislikes")] public int? Dislikes { get; set; }
	#endregion Optional

	#region premath
	int? _lastBottom;
	int? _lastLeft;
	bool? _lastTargetPost;
	bool? _lastExtendPost;
	int? _lastRefsCount;

	string _mathText;
	Point _mathPoint;
	int _mathPostHeight;
	int _mathImgBottom;
	string _mathRefsText;
	int _mathRefsRowCount;
	#endregion premath
	public int DrawPost(Graphics graphics, float imgOpacity, int bottom, int left, bool targetPost = false, bool extendPost = false)
	{
		#region Nums
		int row = 17;
		int symb = 6;
		int noImageRows = 6;
		int imgWidth = 100;
		int postWidth = 430;
		int postHeight = 0;
		#endregion

		bool mathed = 
			_lastBottom == bottom && _lastLeft == left && _lastTargetPost == targetPost && _lastExtendPost == extendPost;

		#region refs
		int[] refs = GetPostReferences();
		
		if (refs.Length > 0)
		{
			if (mathed) mathed = _lastRefsCount == refs.Length;

			string text;
			int refRowCount;

			if (mathed)
			{
				text = _mathRefsText;
				refRowCount = _mathRefsRowCount;
			}
			else
			{
				string refStr = "";
				int length = 0;
				refRowCount = 1;

				foreach (var link in refs)
				{
					string add = $">>{link} ";

					if ((length * symb) + (add.Length * symb) > postWidth)
					{
						length = 0;
						refStr += "\n";
						refRowCount++;
					}

					refStr += add;
					length += add.Length;
				}

				_lastRefsCount = refs.Length;

				_mathRefsText = text = refStr;
				_mathRefsRowCount = refRowCount;

				postHeight += refRowCount * row;
			}

			if (BFont.IsInitialized && WhiteBrush.IsInitialized)
			{
				graphics.DrawText(
					Font, // Шрифт текста
					WhiteBrush, // Цвет текста
					new (left, bottom - postHeight), // Положение текста
					text); // Текст
			}

			//postHeight += row;
		}
		#endregion refs

		if (Files is not null && Files.Length == 1)
		{
			#region Files
			File file = Files[0];

			if (!mathed) _mathImgBottom = bottom - postHeight;

			file.DrawFileThumb(graphics, imgOpacity, _mathImgBottom, left, imgWidth, out int imgHeight);
			#endregion Files

			#region Text
			string text;
			Point point;
			if (mathed)
			{
				text = _mathText;
				point = _mathPoint;
			}
			else
			{
				text = CleanedComment;
				postHeight = imgHeight > (row * noImageRows) ? imgHeight : (row * noImageRows);

				bool cutted = EditString(ref text, (postWidth - (imgWidth + 10)) / symb, extendPost ? 0 : postHeight / (row - 1), out int textRows);

				postHeight = imgHeight > (row * textRows) ? imgHeight : (row * textRows);

				if (cutted)
				{
					text += "Развернуть...";
					postHeight += row;
				}

				_mathPoint = point = new(left + imgWidth + 10, bottom - postHeight); // h, v
				_mathText = text;
			}

			if (Font.IsInitialized && WhiteBrush.IsInitialized)
			{
				graphics.DrawText(
					Font, // Шрифт текста
					WhiteBrush, // Цвет текста
					point, // Положение текста
					text); // Текст
			}
			#endregion Text
		}
		else
		{
			#region Text
			string text;
			Point point;

			if (mathed)
			{
				text = _mathText;
				point = _mathPoint;
			}
			else
			{
				text = CleanedComment;

				if (EditString(ref text, postWidth / symb, extendPost ? 0 : noImageRows, out int finalRowCount))
				{
					text += "Развернуть...";
					finalRowCount++;
				}

				postHeight += finalRowCount * row;

				point = new(left, bottom - postHeight); // h, v
			}

			if (Font.IsInitialized && WhiteBrush.IsInitialized)
			{
				graphics.DrawText(
					Font, // Шрифт текста
					WhiteBrush, // Цвет текста
					point, // Положение текста
					text); // Текст
			}
			#endregion Text

			#region Files
			if (Files is not null && Files.Length > 1)
			{
				postHeight += 10;

				int maxImageHeight = 0;

				for (int index = 0; index < Files.Length; index++)
				{
					File file = Files[index];

					if (!mathed) _mathImgBottom = bottom - postHeight;

					file.DrawFileThumb(graphics, imgOpacity, _mathImgBottom, left + ((imgWidth + 5) * index), imgWidth, out int height);

					if (height > maxImageHeight) maxImageHeight = height;
				}

				postHeight += maxImageHeight;
			}
			#endregion Files
		}

		if (BFont.IsInitialized && WhiteBrush.IsInitialized)
		{
			int y = bottom - (mathed ? _mathPostHeight : postHeight) - row;
			graphics.DrawText(
				BFont, // Шрифт текста
				WhiteBrush, // Цвет текста
				new(left, y), // Положение текста
				$"№{Num}"); // Текст
		}
		postHeight += row;

		if ((targetPost ? BlackBrush : WhiteBrush).IsInitialized)
		{
			int endY = bottom - (mathed ? _mathPostHeight : postHeight);
			graphics.DrawLine(
				targetPost ? BlackBrush : WhiteBrush,
				new Line(left - 5, bottom, left - 5, endY), 2f);
		}

		if (mathed) return _mathPostHeight;
		else return _mathPostHeight = postHeight;

		static bool EditString(ref string text, int maxStringLength, int maxRows, out int finalRows)
		{
			finalRows = 0;

			string[] splitted = text.Split('\n');
			text = "";
			for (int index = 0; index < splitted.Length; index++)
			{
				string line = splitted[index];

				var words = line.Split(new Char[] { ' ' });
				int wordIndex = 0;
				var spaceLetter = " ";
				var currentLine = new StringBuilder();

				while (true)
				{
					if (currentLine.Length + words[wordIndex].Length + 1 > maxStringLength)
					{
						if (currentLine.Length > 0)
						{
							if (maxRows == 0 || finalRows < maxRows)
							{
								text += $"{currentLine}\n";
								finalRows += 1;
							}
							else
							{
								return true;
							}
						}
						currentLine.Remove(0, currentLine.Length);
					}
					currentLine.Append(words[wordIndex]);
					currentLine.Append(spaceLetter);
					wordIndex++;
					if (wordIndex == words.Length)
					{
						if (currentLine.Length > 0)
						{
							if (maxRows == 0 || finalRows < maxRows)
							{
								text += $"{currentLine}\n";
								finalRows += 1;
							}
							else
							{
								return true;
							}
						}
						break;
					}
				}
			}

			return false;
		}
	}

	#region Links
	private List<int>? _links;
	/// <summary>
	/// Возвращает номера всех постов на которые ссылается данный пост
	/// </summary>
	/// <returns></returns>
	public int[] GetPostLinks()
	{
		if (_links is null)
		{
			_links = new();

			RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
			Regex regex = new(@">>(\d+)", regexOptions);
			MatchCollection matches = regex.Matches(CleanedComment);
			foreach (var item in matches)
			{
				string? str = item.ToString();
				if (str is null) continue;
				_links.Add(Convert.ToInt32(str.Replace(">>", "")));
			}
		}
		return _links.ToArray();
	}

	private List<int> _postRefs = new();
	public void AddReference(int postReference)
	{
		if (_postRefs.Contains(postReference)) return;

		_postRefs.Add(postReference);
	}

	public void RemoveReference(int postReferense) => _postRefs.Remove(postReferense);

	/// <summary>
	/// Возвращает номера всех постов которые упоминают данный пост
	/// </summary>
	/// <returns></returns>
	public int[] GetPostReferences() => _postRefs.ToArray();
	#endregion Links
}