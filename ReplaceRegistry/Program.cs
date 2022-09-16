using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security;

namespace ReplaceRegistry
{
    public static class Program
    {
        //some subkey need system account
        //use PsExec.exe (run as admin) from SysinternalsSuite for run as system
        //PsExec.exe -i -s ReplaceRegistry.exe

        const string find = "D:\\Windows Kits";
        const string replace = "C:\\Program Files (x86)\\Windows Kits";
        static readonly ConcurrentQueue<RegistryKey> registryKeys = new ConcurrentQueue<RegistryKey>();
        public static void Main(string[] args)
        {
            SearchAndReplace(Registry.LocalMachine);
            SearchAndReplace(Registry.CurrentUser);
            SearchAndReplace(Registry.Users);
            SearchAndReplace(Registry.ClassesRoot);
            SearchAndReplace(Registry.CurrentConfig);
            //SearchAndReplace(Registry.PerformanceData);
            Console.ReadLine();
        }

        static void Work()
        {
            if (registryKeys.TryDequeue(out RegistryKey key))
            {
                SearchAndReplace(key);
                key.Dispose();
            }
            else
            {
                Console.WriteLine("TryDequeue failed");
            }
        }

        static void SearchAndReplace(RegistryKey registryKey)
        {
            foreach (var name in registryKey.GetValueNames())
            {
                if (registryKey.GetValueKind(name) == RegistryValueKind.String)
                {
                    string data = registryKey.GetValue(name) as string;
                    if (!string.IsNullOrWhiteSpace(data) && data.StartsWith(find))
                    {
                        string r = data.Replace(find, replace);
                        registryKey.SetValue(name, r);
                        Console.WriteLine($"{data} => {r}");
                    }
                }
            }
            foreach (var name in registryKey.GetSubKeyNames())
            {
                try
                {
                    registryKeys.Enqueue(registryKey.OpenSubKey(name, true));
                }
                catch (SecurityException se)
                {
                    Debug.WriteLine($"Can't access {registryKey}\\{name}");
                }
            }
            Task.Run(Work);
        }
    }
}