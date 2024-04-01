using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


namespace ServarrLinkMonitor
{
    public class settings
    {
        public List<grabbedFile> recentGrabs = new List<grabbedFile>();
        public List<replacement> replacements = new List<replacement>();

        public string instanceName = "Replacearr";

        public string ServarrURL;
        public string ServarrAPI;
        public string destinationFolder; //todo, make sure no trailing '/'
        public int ServarrMaxHistory = 200;

        public static string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ServarrLinkMonitor\";

        public static settings load(string instance)
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(settings));
                TextReader textReader = new StreamReader(foldername + instance + ".xml");
                settings setts = (settings)deserializer.Deserialize(textReader);
                textReader.Close();
                setts.trimHistory();
                return setts;
            }




            catch (Exception e)
            {
                if (e is System.IO.FileNotFoundException || e is System.IO.IOException)
                {
                    //create a new one
                    settings sets = new settings();

                    #region populate defaults
                    // TODO remove
                    sets.ServarrURL = @"http://<ServarrPath>:<Port>";
                    sets.ServarrAPI = @"<API>";
                    sets.destinationFolder = @"C:\temp";
                    sets.instanceName = instance;
                   
                    sets.replacements.Add(new replacement(@"Drive:\Path", @"\\server\share\"));
                    sets.save();

                    #endregion
                    return sets;

                }
                throw;
            }
        }

        public void save()
        {
            DirectoryInfo di = new DirectoryInfo(foldername);
            if (!di.Exists)
            {
                di.Create();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(settings));
            TextWriter textWriter = new StreamWriter(foldername + instanceName + ".xml");
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }

        /// <summary>
        /// Throws away history after 1000 files
        /// </summary>
        public void trimHistory()
        {
            bool quitloop = false;
            while (!quitloop)
            {
                if (recentGrabs.Count > 2000)
                {
                    Console.Out.WriteLine("Removing " + recentGrabs[0].filename + " from history");
                    recentGrabs.RemoveAt(0);
                    
                }
                else
                {
                    quitloop = true;
                }

            }

        }

    }



}
