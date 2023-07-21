# Khroma Coder 1.0.18 Readme
[![KCIcon](https://raw.githubusercontent.com/KarstSkarn/KhromaCoder/main/KhromaCoderLogo.ico "KCIcon")](https://raw.githubusercontent.com/KarstSkarn/KhromaCoder/main/KhromaCoderLogo.ico "KCIcon")


- YouTube Demo Video: https://www.youtube.com/watch?v=30oVjJnQ5Og
- Official Discord Server: https://discord.gg/xYuM8uxkAY

### Khroma Coder 1.0.18 Version Features
- Fixed bugs happening with small files; now it can encode any small file without any issue.
- Some UI bugfixes and corrections.
- Minor level of optimization in the Encoding / Decoding process.
- Minor corrections in the way it fetches some file data.
- Ensured that can encode any size of file (Refering to big files) but stills pending some level of optimization.
- Initially I did trim unused dependencies code but some of them didn't like that very much so It keeps having more or less the same size.

### Khroma Coder 1.0.17 Version Features
- Main release; just basic features.
- Possibility to encode files with 4bpxx, 16bppx and 24bppx densities in .mp4 files and decoding them (Mono 1bppx / Dual 2bppx are disabled, check the manual for more details).
- Windows 10/11 Compatible.
- x64 Compatible only.
- Includes an encoded test file.
- Friendly UI.
- Basic user manual in <.pdf> format released.

### Common FAQ
- **Does encrypt the files that are converted to the encoded video?**
  Not at all; files are plainly converted into video following the data density rules stated in the options. Nothing is encoded beyond that.
- **Requires installation?**
  No. The executable is completely standalone and that's why its so relatively big.
- **Can encrypt any file extension (Including .zip)?**
  Yes. The program doesn't really care about the file extension at all. It just encodes it ignoring completely what kind of file it is.

### File Security Check
File **is 100% safe**. You can check the following hashes below. Despite this some minor AntiVirus software may flag it as some kind of threat possibly because the software is not signed and some dependency triggering something.

Same issue can be easily found even with empty programs arround the internet as you can see [here (Stack Overflow exposing same issue)](https://stackoverflow.com/questions/60340213/what-could-be-causing-virustotal-to-flag-an-empty-program-as-a-trojan "here (Stack Overflow exposing same issue)").

For transparency purposes here you have the hashes for both the *.zip* and directly the executable below.

###### Khroma Coder 1.0.18 Release File Security Check

**Khroma Coder 1.0.18 Release.zip SHA 256**

`b9a0576325d8240c13cb2fc8f663bb1c0274891191df429b2390d726434765f7`
VirusTotal [link here.](https://www.virustotal.com/gui/file/b9a0576325d8240c13cb2fc8f663bb1c0274891191df429b2390d726434765f7 "link here.")


**KhromaCoder.exe (1.0.18) SHA 256**

`65d10da25f606f0aaeefc160e526cac1ee5928c0e3f047148f6be8320c70e805`
VirusTotal [link here.](https://www.virustotal.com/gui/file/65d10da25f606f0aaeefc160e526cac1ee5928c0e3f047148f6be8320c70e805 "link here.")

###### Khroma Coder 1.0.17 Release File Security Check

**Khroma Coder 1.0.17 Release.zip SHA 256**

`cd17a640371b53aa01304335794b625af792924ad4a22fc282b4be0ee14bde72`
VirusTotal [link here.](https://www.virustotal.com/gui/file/cd17a640371b53aa01304335794b625af792924ad4a22fc282b4be0ee14bde72 "link here.")


**KhromaCoder.exe (1.0.17) SHA 256**

`36896be97e643a63b4dff6dd26f6d66f6f3c2407f5f519d9db59994a62a51ca8`
VirusTotal [link here.](https://www.virustotal.com/gui/file/36896be97e643a63b4dff6dd26f6d66f6f3c2407f5f519d9db59994a62a51ca8 "link here.")

------------

[![CC Licence](https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png "CC Licence")](https://creativecommons.org/licenses/by-nc-sa/4.0/ "CC Licence")
