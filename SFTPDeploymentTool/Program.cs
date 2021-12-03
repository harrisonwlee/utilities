using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Renci.SshNet.Sftp;
/*
SFTP Deployment Tool for pushing build files to cPanel
Might be deprecated after the move to GCP, but still convenient to have! :) 

Author: Harrison Lee
*/
class Program
{
    static void Main(string[] args)
    {
        try {
            /* Read data from appsettings.json*/
            var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json").Build();

            var section = config.GetSection(nameof(SFTPConfig));
            var credentials = section.Get<SFTPConfig>();
            
            /* Local variables */
            String privateKeyFilePath = Path.Combine(Directory.GetCurrentDirectory(), credentials.PrivateKeyFile);
            String buildPath = Path.Combine(Directory.GetCurrentDirectory(), credentials.BuildDirectory);
            String serverPath = "/home/brokomjm/public_html/";
            List<String> extensionsToRemove = new List<String>{ ".css", ".js", ".otf", ".html", ".ico", ".jpg", ".txt" };
            FileInfo fi;
            Boolean replaceAssets = true;

            if ((args.Length > 0) && (args[0].Equals("false"))) {
                replaceAssets = false;
            }

            /* Begin SFTP Connection */
            using (SftpClient client = new SftpClient(credentials.Host, credentials.Port, credentials.Username, credentials.Password)) {
                Console.WriteLine(String.Format("Opening connection to {0} on port {1}.", credentials.Host, credentials.Port));
                client.Connect();
                if (client.IsConnected) {
                    // first replace all build files
                    Console.WriteLine(String.Format("Established connection to {0}.", credentials.Host));
                    client.ChangeDirectory(serverPath);
                    Console.WriteLine(String.Format("Changed directory to {0}. Deleting files...\n", serverPath));
                    foreach (var entry in client.ListDirectory(".")) {
                        if (extensionsToRemove.Any(s=>entry.FullName.Contains(s))) {
                            Console.WriteLine(String.Format("Deleted {0}", entry.FullName));
                            client.Delete(entry.FullName);
                        }
                    }
                    Console.WriteLine(String.Format("\nStarting file upload from {0}...\n", buildPath));
                    foreach (var file in Directory.GetFiles(buildPath)) {
                        fi = new FileInfo(file);
                        client.UploadFile(fi.OpenRead(), client.WorkingDirectory + "/" + fi.Name, true);
                        Console.WriteLine(String.Format("Uploaded {0}", file));
                    }
                    
                    // replace assets
                    if (replaceAssets) {
                        Console.WriteLine(String.Format("\nReplacing assets...\n"));
                        client.ChangeDirectory("assets/");
                        Console.WriteLine(String.Format("Changed directory successfully."));
                        
                        // fonts
                        Console.WriteLine(String.Format("Updating fonts..."));
                        client.ChangeDirectory("fonts/");
                        foreach (var entry in client.ListDirectory(".").Where(file => (file.Name != ".") && (file.Name != "..")).ToList()) {
                            client.Delete(entry.FullName);
                        }
                        Console.WriteLine("Fonts removed.");
                        foreach (var file in Directory.GetFiles(Path.Combine(buildPath, "assets/fonts"))) {
                            fi = new FileInfo(file);
                            client.UploadFile(fi.OpenRead(), client.WorkingDirectory + "/" + fi.Name);
                            Console.WriteLine(String.Format("Uploaded {0}", fi.Name));
                        }

                        // images
                        Console.WriteLine(String.Format("Updating images..."));
                        client.ChangeDirectory("../img/");
                        foreach (var entry in client.ListDirectory(".").Where(file => (file.Name != ".") && (file.Name != "..")).ToList()) {
                            client.Delete(entry.FullName);
                        }
                        Console.WriteLine("Images removed.");
                        foreach (var file in Directory.GetFiles(Path.Combine(buildPath, "assets/img"))) {
                            fi = new FileInfo(file);
                            client.UploadFile(fi.OpenRead(), client.WorkingDirectory + "/" + fi.Name);
                            Console.WriteLine(String.Format("Uploaded {0}", fi.Name));  
                        }

                        // logos
                        Console.WriteLine(String.Format("Updating logos..."));
                        client.ChangeDirectory("../logo/");
                        foreach (var entry in client.ListDirectory(".").Where(file => (file.Name != ".") && (file.Name != "..")).ToList()) {
                            client.Delete(entry.FullName);
                        }
                        Console.WriteLine("Logos removed.");
                        foreach (var file in Directory.GetFiles(Path.Combine(buildPath, "assets/logo"))) {
                            fi = new FileInfo(file);
                            client.UploadFile(fi.OpenRead(), client.WorkingDirectory + "/" + fi.Name);
                            Console.WriteLine(String.Format("Uploaded {0}", file));
                        }
                    }
                }
            }

        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }
}
