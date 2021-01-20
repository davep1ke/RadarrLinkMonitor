﻿
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RadarrLinkMonitor
{
    public class RadarrLinkMonitor
    {


        static void Main(string[] args)
        {
            RadarrLinkMonitor.CreateObject();
        }

        private static void CreateObject()
        {
            settings Settings = settings.load();

            string URL = Settings.RadarrURL + "/api/history?page=1&pageSize=" + Settings.RadarrMaxHistory + "&sortkey=date&sortDir=desc&apikey=" + Settings.RadarrAPI;

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
                        using (StreamReader responseReader = new StreamReader(webStream))
                        {
                                string response = responseReader.ReadToEnd();

                                JObject jo = JObject.Parse(response);

                                Console.Write(jo.First);


                                //open the response and parse it using JSON. Query for newly imported files
                                foreach (JToken token in jo.SelectTokens("records[*].data.importedPath")) //jo.Children()
                                {
                                    //go through these and create the link. 
                                    bool found = false;
                                    foreach (grabbedFile g in Settings.recentGrabs)
                                    {
                                        if (g.filename == token.ToString()) { found = true; }
                                    }

                                    if(found == true)
                                    {
                                        Console.Out.WriteLine("Already processed " + token.ToString());
                                    }
                                    else
                                    {
                                        Console.Out.WriteLine("Processing        " + token.ToString());
                                        string filename = Path.GetFileNameWithoutExtension(token.ToString());
                                        
                                        //invalid chars
                                        foreach (char invalidchar in System.IO.Path.GetInvalidFileNameChars())
                                        {
                                            filename = filename.Replace(invalidchar, '_');
                                        }

                                        filename = Settings.destinationFolder + @"/" + filename + ".lnk";
                                        string destination = token.ToString();

                                        

                                    //apply any replacements
                                    foreach (replacement r in Settings.replacements)
                                    {
                                        destination = destination.Replace(r.source, r.replace);
                                    }

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

                                        Console.Out.WriteLine("-----------------");
                                        Console.Out.WriteLine(filename);
                                        Console.Out.WriteLine(e.Message);
                                        Console.Out.WriteLine(e.InnerException);
                                        Console.Out.WriteLine(e.StackTrace);
                                        Console.Out.WriteLine("-----------------");

                                    }
                                    //log it
                                    Settings.recentGrabs.Add(new grabbedFile(token.ToString()));
                                    }
                                }

                            
                            
                        }
                    }
                }
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