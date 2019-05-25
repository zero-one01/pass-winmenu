using System;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class StatusMessage
	{
		public GpgStatusCode StatusCode { get; }
		public string RawStatusCode { get; }
		public string Message { get; }

		public StatusMessage(string rawStatusCode, string message)
		{
			RawStatusCode = rawStatusCode;
			Message = message;
			if (Enum.TryParse(rawStatusCode, false, out GpgStatusCode parsedStatusCode))
			{
				StatusCode = parsedStatusCode;
			}
			else
			{
				StatusCode = GpgStatusCode.UnknownStatusCode;
			}
		}

		public override string ToString() => $"[{RawStatusCode}] {Message}";
	}
}
