# pass-winmenu

A simple, easy-to-use Windows interface for [pass](https://www.passwordstore.org/).

![demonstration GIF](https://i.imgur.com/Yf9XBQn.gif)

## Usage

Bring up the password menu with the keyboard shortcut `Ctrl Alt P`.
The password menu allows you to quickly browse through your passwords and select the right one.
Select the password by double-clicking it, or by using the arrow keys and pressing Enter.

The password will be decrypted using GPG, and your GPG key passphrase may be requested through pinentry.
The decrypted password will then be copied to your clipboard and/or entered into the active window,
depending on your `config.yaml` settings.

## Requirements

Pass-winmenu is built against .NET Framework 4.5.2, which should already be installed on every version
of Windows since Windows 7.
Additionally, GPG and Git are required for the application to function correctly.

## Configuration

On first run, the application will generate a `pass-winmenu.yaml` file 
(containing all its settings initialised to their default value) in its current directory. 
Edit the file and restart pass-winmenu to apply the changes.

## To do

- Add further Git integration
