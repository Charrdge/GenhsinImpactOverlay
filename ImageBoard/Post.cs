using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GameOverlay.Drawing;

namespace GenshinImpactOverlay.ImageBoard;

internal class Post
{
	public static string FontIndex { get; set; }
	public static string WhiteBrushIndex { get; set; }
	public static string BlackBrushIndex { get; set; }

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

	public int DrawPost(Graphics graphics, GraphicsWorker worker, int bottom, int left)
	{
		int row = 17;
		int symb = 6;
		int noImageRows = 6;
		int imgWidth = 100;
		int postWidth = 430;
		int postHeight = 0;

		if (Files is not null && Files.Length == 1)
		{
			#region Files
			File file = Files[0];

			file.DrawFileThumb(graphics, bottom, left, imgWidth, out int imgHeight);
			#endregion Files

			#region Text
			string text = CleanedComment;
			postHeight = imgHeight > (row * noImageRows) ? imgHeight : (row * noImageRows);

			if (EditString(ref text, (postWidth - (imgWidth + 10)) / symb, postHeight / (row - 1), out int _))
			{
				text += "Развернуть...";
				postHeight += row;
			}

			Point point = new(left + imgWidth + 10, bottom - imgHeight); // h, v

			graphics.DrawText(
				worker.Fonts[FontIndex], // Шрифт текста
				(SolidBrush)worker.Brushes[WhiteBrushIndex], // Цвет текста
				point, // Положение текста
				text); // Текст
			#endregion Text

			graphics.DrawLine((SolidBrush)worker.Brushes[WhiteBrushIndex], new Line(left - 5, bottom, left - 5, bottom - postHeight), 2f);
		}
		else
		{
			#region Text
			string text = CleanedComment;

			if (EditString(ref text, postWidth / symb, noImageRows, out int finalRowCount))
			{
				text += "Развернуть...";
				finalRowCount++;
			}

			postHeight += finalRowCount * row;

			Point point = new(left, bottom - postHeight); // h, v

			graphics.DrawText(
				worker.Fonts[FontIndex], // Шрифт текста
				(SolidBrush)worker.Brushes[WhiteBrushIndex], // Цвет текста
				point, // Положение текста
				text); // Текст
			#endregion Text

			#region Files
			if (Files is not null && Files.Length > 1)
			{
				postHeight += 10;

				int maxImageHeight = 0;

				for (int index = 0; index < Files.Length; index++)
				{
					File file = Files[index];

					file.DrawFileThumb(graphics, bottom - postHeight, left + ((imgWidth + 5) * index), imgWidth, out int height);

					if (height > maxImageHeight) maxImageHeight = height;
				}

				postHeight += maxImageHeight;
			}
			#endregion Files

			graphics.DrawLine((SolidBrush)worker.Brushes[WhiteBrushIndex], new Line(left - 5, bottom, left - 5, bottom - postHeight), 2f);
		}

		return postHeight;
	}

	private static bool EditString(ref string text, int maxStringLength, int maxRows, out int finalRows)
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

			while(true)
			{
				if (currentLine.Length + words[wordIndex].Length + 1 > maxStringLength)
				{
					if (currentLine.Length > 0)
					{
						if (finalRows < maxRows)
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
						if (finalRows < maxRows)
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