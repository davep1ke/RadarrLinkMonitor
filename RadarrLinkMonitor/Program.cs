using IWshRuntimeLibrary;
using System.Text;
using System;
using System.Web;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.Json;

namespace ServarrLinkMonitor
{
    public class ServarrLinkMonitor
    {


        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                ServarrLinkMonitor.CreateObject(args[0]);
            }
            else
            {
                Console.WriteLine("Pass in the name of a Servarr Instance (e.g. Sonarr)");
            }
        }

        private static void CreateObject(string instanceName)
        {
            
            settings Settings = settings.load(instanceName);
            if (!Settings.ServarrURL.EndsWith("/")) { Settings.ServarrURL += "/"; }

            Console.Out.WriteLine("Servarr Link Monitor - checking " + Settings.ServarrURL + " for new files");
            string URL = Settings.ServarrURL + Settings.apiPath + "history?page=1&pageSize=" + Settings.ServarrMaxHistory + "&sortkey=date&sortDir=desc&apikey=" + Settings.ServarrAPI;

            

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";
            request.ContentType = "application/json";
            //request.ContentLength = DATA.Length;
            

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream())
                {
                    if (webStream != null)
                    {
                        using StreamReader responseReader = new(webStream);
                        string response = responseReader.ReadToEnd();


                        JsonDocument jo = JsonDocument.Parse(response);

                        //Console.Write(jo.RootElement.First);  


                        //open the response and parse it using JSON. Query for newly imported files

                        foreach (JsonElement element in jo.RootElement.GetProperty("records").EnumerateArray())
                        {
                            try
                            {
                                //.SelectTokens("records[*].data.importedPath")) 
                                string title = element.GetProperty("sourceTitle").ToString();
                                JsonElement elData = element.GetProperty("data");

                                bool success = elData.TryGetProperty("importedPath", out JsonElement elImportPath);
                                if (!success)
                                {
                                    Console.Out.WriteLine("No import path for " + title + " - failed / still processing?");
                                }
                                else
                                {

                                    string importPath = elImportPath.ToString();

                                    //go through these and create the link. 
                                    bool found = false;
                                    foreach (grabbedFile g in Settings.recentGrabs)
                                    {
                                        if (g.filename == importPath) { found = true; }
                                    }

                                    if (found == true)
                                    {
                                        Console.Out.WriteLine("Already processed " + importPath);
                                    }
                                    else
                                    {
                                        Console.Out.WriteLine("Processing        " + importPath);

                                        //Work out the filename
                                        string filename = Path.GetFileNameWithoutExtension(importPath);

                                        //invalid chars
                                        foreach (char invalidchar in System.IO.Path.GetInvalidFileNameChars())
                                        {
                                            filename = filename.Replace(invalidchar, '_');
                                        }

                                        filename = Settings.destinationFolder + @"\" + filename + ".lnk";

                                        //apply any replacements to the path
                                        string destination = importPath;
                                        foreach (replacement r in Settings.replacements)
                                        {
                                            destination = destination.Replace(r.source, r.replace);
                                        }
                                        //replace unix paths with windows ones.
                                        destination = destination.Replace(@"/", @"\");

                                        //TODO - for linux use mslink.sh http://www.mamachine.org/mslink/index.en.html 

                                        //craete the link file. 
                                        var wsh = new IWshShell_Class();
                                        try
                                        {
                                            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(filename) as IWshRuntimeLibrary.IWshShortcut;
                                            shortcut.TargetPath = destination;
                                            shortcut.Save();
                                        }

                                        catch (Exception e)
                                        {

                                            Console.Out.WriteLine("--------Failed to create shortcut---------");
                                            Console.Out.WriteLine(filename);
                                            Console.Out.WriteLine(e.Message);
                                            Console.Out.WriteLine(e.InnerException);
                                            Console.Out.WriteLine(e.StackTrace);
                                            Console.Out.WriteLine("-----------------");

                                        }
                                        //log it
                                        Settings.recentGrabs.Add(new grabbedFile(importPath));
                                    }
                                }
                            }
                            catch (Exception e)
                            {

                                Console.Out.WriteLine("-----Failed to parse record------------");
                                Console.Out.WriteLine(e.Message);
                                Console.Out.WriteLine(e.InnerException);
                                Console.Out.WriteLine(e.StackTrace);
                                Console.Out.WriteLine("-----------------");

                            }

                        }
                    }
                }
                Console.Out.WriteLine("Servarr Link Monitor completed, saving and exiting.");
                Settings.save();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("-----------------");
                Console.Out.WriteLine(e.Message);
                Console.Out.WriteLine(e.InnerException);
                Console.Out.WriteLine(e.StackTrace);
                Console.Out.WriteLine(e.ToString());

            }            
        }
    }
}