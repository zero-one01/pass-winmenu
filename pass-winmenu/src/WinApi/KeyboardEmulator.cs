using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PassWinmenu.Utilities;

namespace PassWinmenu.WinApi
{
	static class KeyboardEmulator
	{
		/// <summary>
		/// Sends text directly to the topmost window, as if it was entered by the user.
		/// </summary>
		/// <param name="text">The text to be sent to the active window.</param>
		internal static void EnterText(string text)
		{
			var inputs = new List<Input>();
			foreach (var ch in text)
			{
				var down = Input.FromCharacter(ch, KeyDirection.Down);
				var up = Input.FromCharacter(ch, KeyDirection.Up);
				inputs.Add(down);
				inputs.Add(up);
			}
			SendInputs(inputs);
		}

		internal static void EnterTab()
		{
			var inputs = new List<Input>
			{
				Input.FromKeyCode(VirtualKeyCode.Tab, KeyDirection.Down),
				Input.FromKeyCode(VirtualKeyCode.Tab, KeyDirection.Up),
			};
			SendInputs(inputs);
		}

		private static void SendInputs(List<Input> inputs)
		{
			var size = Marshal.SizeOf(typeof(Input));
			var success = NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), size);
			if (success != inputs.Count)
			{
				var exc = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
				throw exc;
			}
		}
	}
}
