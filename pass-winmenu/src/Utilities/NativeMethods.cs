using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PassWinmenu.Utilities
{
	internal class NativeMethods
	{
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs,
			[MarshalAs(UnmanagedType.LPArray), In] Input[] pInputs,
		 int cbSize);

		public static Process GetWindowProcess(IntPtr hWnd)
		{
			GetWindowThreadProcessId(hWnd, out uint pid);
			return Process.GetProcessById((int)pid);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Input
	{
		internal InputType Type;
		internal KeyboardInput Data;

		internal static Input FromKeyCode(VirtualKeyCode keyCode, KeyDirection direction)
		{
			KeyEventFlags flags = 0;
			if (keyCode.IsExtendedKey())
			{
				flags |= KeyEventFlags.ExtendedKey;
			}
			if (direction == KeyDirection.Up)
			{
				flags |= KeyEventFlags.KeyUp;
			}

			return new Input
			{
				Type = InputType.Keyboard,
				Data = new KeyboardInput
				{
					KeyCode = keyCode,
					ScanCode = 0,
					Flags = flags,
					Time = 0,
					ExtraInfo = IntPtr.Zero
				}
			};
		}

		internal static Input FromCharacter(char character, KeyDirection direction)
		{
			var flags = KeyEventFlags.Unicode;

			// If the scan code is preceded by a prefix byte that has the value 0xE0 (224),
			// we need to include the ExtendedKey flag in the Flags property.
			if ((character & 0xFF00) == 0xE000)
			{
				flags |= KeyEventFlags.ExtendedKey;
			}
			if (direction == KeyDirection.Up)
			{
				flags |= KeyEventFlags.KeyUp;
			}

			return new Input
			{
				Type = InputType.Keyboard,
				Data = new KeyboardInput
				{
					KeyCode = 0,
					ScanCode = character,
					Flags = flags,
					Time = 0,
					ExtraInfo = IntPtr.Zero
				}
			};
		}
	}

	internal enum KeyDirection
	{
		Up,
		Down,
	}

	/// <summary>
	/// A virtual key code, as defined on https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
	/// </summary>
	internal enum VirtualKeyCode : ushort
	{
		// Note: When adding new keys here, update the implementation of VirtualKeyCodeExtensions.IsExtendedKey
		// if the added key is an extended key. For more information, see:
		// https://docs.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input
		Tab = 0x09,
	}

	internal static class VirtualKeyCodeExtensions
	{
		public static bool IsExtendedKey(this VirtualKeyCode _)
		{
			return false;
		}
	}

	internal enum InputType : uint
	{
		Mouse = 0,
		Keyboard = 1,
		Hardware = 2
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct KeyboardInput
	{
		internal VirtualKeyCode KeyCode;
		internal ushort ScanCode;
		internal KeyEventFlags Flags;
		internal uint Time;
		internal IntPtr ExtraInfo;
		private uint padding;
		private uint padding_;
	}

	[Flags]
	internal enum KeyEventFlags : uint
	{
		ExtendedKey = 0x0001,
		KeyUp = 0x0002,
		ScanCode = 0x0008,
		Unicode = 0x0004
	}
}
