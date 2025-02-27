using SharpToken;

namespace PromptBuilderApp
{
	public static class TokenCounter
	{
		public static int EstimateTokens(string text, string modelName)
		{
			// Map your model names to SharpToken encodings:
			//   GPT-4, GPT-3.5 => "cl100k_base" or use GetEncodingForModel(modelName)
			//   o1 Pro => "o200k_base" (assuming it’s GPT-based)
			//   o3-mini-high => "cl100k_base" (or whichever is close)
			// Adjust to your real model names/encodings.

			GptEncoding encoding = modelName switch
			{
				"GPT-4" or "GPT-3.5"
					=> GptEncoding.GetEncodingForModel(modelName.ToLower()),
				// e.g. "gpt-4" => "cl100k_base", 
				// "gpt-3.5-turbo" => "cl100k_base"

				"o1 Pro" => GptEncoding.GetEncoding("o200k_base"),
				"o3-mini-high" => GptEncoding.GetEncoding("cl100k_base"),

				_ => null
			};

			if (encoding != null)
			{
				return encoding.CountTokens(text);
			}
			else
			{
				// Fallback if not recognized
				return text.Length / 4;
			}
		}
	}
}
