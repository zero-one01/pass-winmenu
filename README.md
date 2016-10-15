# pass-winmenu

A simple, easy-to-use Windows interface for [pass](https://www.passwordstore.org/),
built on open standards.

![demonstration GIF](https://i.imgur.com/Yf9XBQn.gif)

## Introduction

Pass stores passwords as GPG-encrypted files organised into a directory structure.
Its simplicity and modularity offer many advantages:

- Cryptography is handled by GPG (Don't roll your own cryptography)
- GPG gives you a lot of control over the keys and algorithms used to encrypt your files
- The use of GPG makes it easy for other applications (such as password managers) to interact
  with your password store.
- The directory structure for passwords is intuitive and allows you to organise your passwords
  with your file manager.
- Because the passwords are simply stored in a directory tree, it's easy to synchronise your
  password store using any version control software of your choosing, giving you synchronisation, 
  file history and redundancy all at the same time (provided you use multiple devices and/or a
  remote VCS server).
- Widespread availability of VCS software gives you the option to set up your own synchronisation server,
  giving you full control over your passwords.
  Alternatively, you can choose one of the many online version control services (such as GitHub)
  and store your passwords in a private repository.
- The password files are always encrypted and can only be decrypted with your private GPG key,
  which is secured by a passphrase. If someone gains access to your password files, they're useless
  even if said person additionally managed to get hold of your GPG keys.
  
## Usage

Bring up the password menu with the keyboard shortcut `Ctrl Alt P`.
The password menu allows you to quickly browse through your passwords and select the right one.
Select the password by double-clicking it, or by using the arrow keys and pressing Enter.

The password will be decrypted using GPG, and your GPG key passphrase may be requested through pinentry.
The decrypted password will then be copied to your clipboard and/or entered into the active window,
depending on your `config.yaml` settings.

## Dependencies

Pass-winmenu is built against .NET Framework 4.5.2, which should already be installed on every version
of Windows since Windows 7.
Additionally, GPG and Git are required for the application to function correctly.
They don't have to be added to your PATH, but the executables must be reachable somehow.

## Setup

Installing pass-winmenu is as easy as dropping the executable anywhere you want and running it.

You'll need to install GPG and Git if you don't have them installed yet.
Install them to any location of your choosing. Don't forget to add them to your PATH!

#### Download links:

Gpg: https://www.gpg4win.org/download.html

Git: https://git-scm.com/downloads

### Setting up GPG:

If you already have a GPG key, you may want to consider importing it and using that.
If you've never used GPG before, you can generate a new key:

`C:\Users\Baggykiin> gpg --gen-key`

### Creating a new password store:

Create an empty directory in which you want to store your passwords.

`C:\Users\Baggykiin> mkdir .password-store`

Save the email address you used for creating your GPG key into a `.gpg-id` file
in the root of your password directory.

```
C:\Users\Baggykiin> cd .password-store
C:\Users\Baggykiin\.password-store> echo "myemail@example.com" | Out-File -Encoding utf8 .gpg-id
```

Now you can point pass-winmenu to your password store.
On first run, pass-winmenu will generate a `pass-winmenu.yaml` file 
(containing all its settings initialised to their default values) in its current directory and exit.
Open the file, read through it, edit the settings as necessary and save it before
starting the application again. You should now have a working password manager.

To synchronise your passwords, initialise a new Git repository at the root of your password store,
and connect it to a remote server. GitLab offers free private repositories, and GitHub does too if
you're a student. Alternatively, you can run your own Git server, of course.
