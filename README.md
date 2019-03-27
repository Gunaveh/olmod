## olmod - Overload mod

**Loader for full custom Overload scripting**

[Overload](https://playoverload.com) is a registered trademark of [Revival Productions, LLC](https://www.revivalprod.com).
This is an unaffiliated, unsupported tool. Use at your own risk.

#### How to run

- If you installed Overload using Steam or GOG Galaxy you can extract olmod
  anywhere and it will find Overload automatically.

- If you installed Overload yourself or on linux, extract olmod in the Overload main
  directory (where `Overload.exe` / `Overload.x86_64` is also located).
  
- Run `olmod.exe` / `olmod.sh` (linux) instead of `Overload.exe`

- Use `olmodserver.bat` / `olmodserver.sh` (linux) to start a server

#### What does it do

Currently it shows a modified indicator in the main menu, allows
access to the unfinished Monsterball multiplayer mode and puts a
MP player in observer mode if it uses a pilot name starting with
OBSERVER.

The Monsterball multiplayer mode only works when both server and clients run olmod.

The observer mode only works when the server and the observer client run olmod.

#### How to build

- Open solution in Visual Studio 2017

- Fix references in GameMod project

  If your Overload copy is not in
  `C:\Program Files (x86)\Steam\steamapps\common\Overload` you need to
  adjust the references to `UnityEngine.CoreModule.dll` and
  `Assembly-CSharp.dll`.
  They are in `Overload_Data\Managed`.

- Run

#### How does it work

The regular `Overload.exe` just runs `UnityMain` from `UnityPlayer.dll`.
The replacement `olmod.exe` also runs `UnityMain`, but intercepts calls from Unity to the mono C# engine
to also load `GameMod.dll`. The file `GameMod.dll` contains a C# class that
uses [Harmony](https://github.com/pardeike/Harmony) to modify the game
scripting code in memory.