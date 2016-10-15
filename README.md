# pass-winmenu

A simple, easy-to-use password manager for Windows.

Pass-winmenu follows the philosophy of (and is compatible with) the Linux password manager [pass](https://www.passwordstore.org), which defines an open standard for password management that's easy to extend and customise to your personal requirements.

![demonstration GIF](https://i.imgur.com/Yf9XBQn.gif)

## Introduction

[Pass (https://www.passwordstore.org)](https://www.passwordstore.org) stores passwords as GPG-encrypted files organised into a directory structure.
Its simplicity and modularity offer many advantages:

- Cryptography is handled by GPG (Don't roll your own cryptography)
- GPG gives you a lot of control over the keys and algorithms used to encrypt your files
- The use of GPG makes it easy for other applications (such as password managers) to interact
  with your password store.
- The directory structure for passwords is intuitive and allows you to organise your passwords
  with your file manager.
- Because the passwords are simply stored in a directory tree, it's easy to synchronise your
  password store using any version control software of your choosing, giving you synchronisation, 
  file history, and redundancy all at the same time (provided you use multiple devices and/or a
  remote VCS server).
- Widespread availability of VCS software gives you the option to set up your own synchronisation server,
  giving you full control over your passwords.
  Alternatively, you can choose one of the many online version control services (such as GitHub)
  and store your passwords in a private repository.
- The password files are always encrypted and can only be decrypted with your private GPG key,
  which is secured with a passphrase. If someone gains access to your password files, they're useless
  even if said person additionally managed to get hold of your GPG keys.

Unfortunately, while there are many Linux integrations available, Windows integrations are scarce.

I wasn't happy with the existing ones, so I created my own, focusing on easy, keyboard-friendly
interaction and a minimal interface that stays out of your way.

## Usage

Bring up the password menu with the keyboard shortcut `Ctrl Alt P`.
The password menu allows you to quickly browse through your passwords and select the right one.
Select the right password file by double-clicking it, or by using the arrow keys and pressing Enter.

The password will be decrypted using GPG, and your GPG key passphrase may be requested through pinentry.
The decrypted password will then be copied to your clipboard and/or entered into the active window,
depending on your `config.yaml` settings.

## Configuration

Many configuration options are available to make pass-winmenu act according to your preferences.
See [here](https://github.com/Baggykiin/pass-winmenu/blob/master/pass-winmenu/embedded/default-config.yaml)
for an annotated list.

## Dependencies

Pass-winmenu is built against .NET Framework 4.5.2, which should already be installed on every version
of Windows since Windows 7.
Additionally, GPG and Git are required for the application to function correctly.

## Setup

Installing pass-winmenu is as easy as dropping the executable anywhere you want and running it.

You'll need to install GPG and Git if you don't have them installed yet.
Install them to any location of your choosing. Make sure they're added to your PATH!

By default, GPG should add itself to your PATH without you having to do anything.
Git will do so as well if you select the `Use Git from the Windows Command Prompt` option
during installation.

#### Download links:

Gpg: https://www.gpg4win.org/download.html

Git: https://git-scm.com/downloads

### Setting up GPG:

If you already have a GPG key, you may want to consider importing it and using that.
If you've never used GPG before, you can generate a new key:

`C:\Users\Baggykiin> gpg --gen-key`

Follow the instructions to generate your GPG keys. If it asks you what kind of keys
you want to generate, don't pick any of the `sign only` options, as they don't
include an encryption key, which is required for encrypting passwords.
The default, RSA and RSA, is recommended.

When it asks you for an email address, remember that address, as you'll need to enter
it again in a bit.

Finally, you'll be asked to enter a passphrase. Make sure this is a very secure,
unique passphrase, as it can be used to decrypt all your passwords, but don't
make it *too* hard  to enter, since you'll need to enter it regularly.

### Creating a new password store:

Create an empty directory in which you want to store your passwords.

`mkdir .password-store`

Save the email address you used for creating your GPG key into a `.gpg-id` file
in the root of your password directory (Use powershell, not command prompt).

```
cd .password-store
echo "myemail@example.com" | Out-File -Encoding utf8 .gpg-id
```

Now you can point pass-winmenu to your password store.
On first run, pass-winmenu will generate a `pass-winmenu.yaml` file 
(containing all its settings initialised to their default values) in its current directory and exit.
Open the file, read through it, edit the settings as necessary, and save it before
starting the application again. You should now have a working password manager.

To synchronise your passwords, initialise a new Git repository at the root of your password store:
```
git init
git add -A
git commit -m "Initialise password repository"
```

You'll also need a remote Git server. GitLab offers free private repositories, and GitHub does too if
you're a student. Alternatively, you can of course run your own Git server.

Add an empty repository on your Git provider of choice, then connect your password store to it.
Depending on where you're hosting your repository, it might differ a bit, but you'll usually
have to do something like this:

```
git remote add origin https://github.com/yourusername/password-store.git
git push --set-upstream origin master
```

### Accessing an existing password store on a different host

If you already have a password store and you want to access it from another computer, you'll have
to import your GPG keys on it. Follow the above instructions for installing GPG and Git, then export
your GPG keys on the machine where you already have a working password store:

`gpg --export-secret-key -a youremailaddress@example.com > private.key`

Copy the `private.key` file to the machine on which you're setting up your password store, and import it.

`gpg --import private.key`

Clone your password repository

`git clone https://github.com/yourusername/password-store.git`

Then run pass-winmenu, edit the generated `pass-winmenu.yaml` configuration file as necessary,
and start it again.
