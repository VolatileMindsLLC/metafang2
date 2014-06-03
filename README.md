metafang2
========================

Interfaces with a Metasploit RPC instance to generate .NET executables that run x86/x64 shell code in a platform-agnostic way. One binary to rule them all.

Log in to the msfrpcd instance with a url like "https://127.0.0.1:55553/api", and the credentials set up when starting msfrpcd.

Under active development. Requires metasploit-sharp (http://github.com/brandonprry/metasploit-sharp/) and msgpack-cli (either MsgPack.Mono or MsgPack).

Currently, shell code run on some linux will be blocked by SELinux. Will eventually look into implementing something like this: http://www.akkadia.org/drepper/selinux-mem.html

It is expected that 'gmcs' is in your PATH in order to compile the C# code that is generated. At some point, research into Mono.Csharp should be done, but this library was really built for REPL and not on the fly compilation. 

The only official binaries are available on ExploitHub: https://exploithub.com/metafang-2-0-for-linux.html
