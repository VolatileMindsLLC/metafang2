metafang2
========================

Interfaces with a Metasploit RPC instance to generate .NET executables that run x86/x64 shell code in a platform-agnostic way. One binary to rule them all.

Also provides an encryption mechanism that will bruteforce the payload's key at run time.

Log in to the msfrpcd instance with a url like "https://127.0.0.1:55553/api", and the credentials set up when starting msfrpcd.

Under active development. Requires metasploit-sharp (http://github.com/brandonprry/metasploit-sharp/) and msgpack-cli (either MsgPack.Mono or MsgPack).

Currently, shell code run on some linux will be blocked by SELinux. Will eventually look into implementing something like this: http://www.akkadia.org/drepper/selinux-mem.html

Something else to look at in the future is Mono.CSharp to run the payloads. This, however, would place a direct dependency on Mono. Perhaps this is an acceptable dependency on Macs and Linux, but on Windows this is not the case.
