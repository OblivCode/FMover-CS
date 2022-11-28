
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FileMover
{
    internal class Program
    {
        static string input_path, output_path;
        static int max_memory = 0, free_memory_space = 500000, min_memory = 1000, two_gigs = 2147483591, counter = 0;
        static Dictionary<string, byte[]?> file_entries = new Dictionary<string, byte[]?>();
        static List<string> big_files = new List<string>();
        static void Main(string[] args)
        {


            Console.Write("Path to move files from: ");
            input_path = Console.ReadLine();

            Console.Write("Path to move files to: ");
            output_path = Console.ReadLine();

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                max_memory = GetFreeMemory() - free_memory_space;
                if(max_memory < min_memory)
                {
                    Console.WriteLine($"Not enough memory! ({min_memory} KBs minimum)");
                    return;
                }
            }
            //
            string[] input_paths = Directory.GetFileSystemEntries(input_path, "", SearchOption.AllDirectories);
            int max_size = max_memory > int.MaxValue ? int.MaxValue  : max_memory;
            foreach (string path in input_paths)
            {
                
                string relative_path = path.Replace(input_path, "").Trim();
                Console.WriteLine("Reading: " + relative_path);
                if (File.Exists(path))
                {
                    
                    byte[] data;
                    float file_size = new FileInfo(path).Length;
                    if (file_size > two_gigs)
                    {
                        Console.WriteLine("BIG FILE!");
                        big_files.Add(path);
                    }
                    else
                    {
                        if (GetFreeMemory() < free_memory_space + file_size)
                            WorkEntries();
                        FileStream fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        data = new byte[(int)file_size];
                        fstream.Read(data, 0, (int)file_size);
                        file_entries.Add(path, data);
                    }
                        
                }
                else
                    file_entries.Add(path, null);

                if (GetFreeMemory() < free_memory_space)
                    WorkEntries();

            }
            if (file_entries.Count > 0)
                WorkEntries();
            Console.WriteLine($"{file_entries.Count} entries.");
            Console.Read();
            WorkBigFiles();
        }

        static void WorkBigFiles()
        {
            Console.WriteLine("Working big files");
            long bytes_left, file_size;
            int bytes_to_read = two_gigs;
            byte[] buffer;
            FileStream fs, fs1;
            string new_path, relative_path;

            foreach (var path in big_files)
            {
                

                relative_path = path.Replace(input_path, "").Trim();
                new_path = output_path + relative_path;

                fs = File.OpenRead(path);
                fs1 = File.OpenWrite(new_path);
                file_size = new FileInfo(path).Length;

                int chunk_count = (int)Math.Ceiling((double)file_size / bytes_to_read);

                Console.WriteLine("Split into " + chunk_count + " chunks");
                Console.Title = relative_path+ ": " + chunk_count + " chunks";

                for (int i = 1; i < chunk_count+1; i++)
                {
                    bytes_left = file_size - fs.Position;
                    if (bytes_to_read > bytes_left)
                        buffer = new byte[(int)bytes_left];
                    else
                        buffer = new byte[bytes_to_read];

                   Console.WriteLine($"Reading Chunk {i}: {buffer.Length} bytes");
                   fs.Read(buffer, 0, buffer.Length);

                    Console.WriteLine("Writing...");
                    fs1.Write(buffer, 0, buffer.Length);
                    buffer = null;
                }

                fs1.Close();
                fs.Close();
                counter++;
            }
            big_files.Clear();
        }
        static void WorkEntries()
        { 
            string relative_path, path, new_path;
            byte[]? data;

            foreach(var entry in file_entries)
            {
                path = entry.Key;
                relative_path = path.Replace(input_path, "").Trim();
                new_path = output_path + relative_path;
                data = entry.Value;


                Console.WriteLine("Writing: "+ relative_path);

                string? parent_dir = Directory.GetParent(new_path)?.FullName;
                if (parent_dir != null)
                    Directory.CreateDirectory(parent_dir);


                if (data == null)
                    Directory.CreateDirectory(new_path);
                else
                {
                    FileStream fs = File.OpenWrite(new_path);
                    fs.Write(data, 0, data.Length);
                    fs.Close();
                }
                counter++;
            }

            file_entries.Clear();
        }

        static int GetFreeMemory()
        {
            string free_memory = GetWmicOutput("OS get FreePhysicalMemory /Value").Trim().Split("=")[1];
            return int.Parse(free_memory);
        }

       
       
        static string GetWmicOutput(string query, bool redirectStandardOutput = true)
        {
            var info = new ProcessStartInfo("wmic");
            info.Arguments = query;
            info.RedirectStandardOutput = redirectStandardOutput;
            var output = "";
            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }
            return output.Trim();
        }
    }
}