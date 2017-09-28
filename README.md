metafang2
========================

Interfaces with a Metasploit RPC instance to generate .NET executables that run x86/x64 shell code in a platform-agnostic way. One binary to rule them all.

Log in to the msfrpcd instance with a url like "https://127.0.0.1:55553/api", and the credentials set up when starting msfrpcd.

Requires metasploit-sharp (http://github.com/brandonprry/metasploit-sharp/) and msgpack-cli.

Currently, shell code run on some linux will be blocked by SELinux. Will eventually look into implementing something like this: http://www.akkadia.org/drepper/selinux-mem.html

