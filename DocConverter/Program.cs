using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using MySql.Data.MySqlClient;


namespace DocConverter
{
    class Program
    {
        static XmlDocument xmlDoc = new XmlDocument();
        static List<FileInfo> docList = new List<FileInfo>(); 
        static string endpoint = "http://isis.delphi03.euromoneydigital.com/documents/";
        static void Main(string[] args)
        {

            //validate the arguments
            //if (args.Length > 0)
            //{                
                try
                {
                    string in_dir= @"C:\DocConverter\BCA";
                    string out_dir = @"C:\DocConverter\ISIS";


                    //////////////////////////////////////////////////////////
                    ////creates BCA-formatted documents from MySQL
                    //////////////////////////////////////////////////////////
                    //List<document> reports = new List<document>();
                    //reports = document.GetDataFromMySQL();

                    ////write the data on files                     
                    //foreach (document r in reports)
                    //{
                    //    Serialize(r, in_dir + @"\" + r.id + ".xml");
                    //}
                    //////////////////////////////////////////////////////////


                    //get the files from directory
                    //string in_dir = args[0].ToString();
                    //string out_dir = args[0].ToString();

                    DirectoryInfo dir = new DirectoryInfo(in_dir);
                    GetDocs(dir);

                    //loop through the files
                    foreach (FileInfo f in docList)
                    {                        
                        //get the document id
                        Console.WriteLine("Processing file:" + f.Name);
                        string doc = GetDocID(dir + @"\" + f.Name);

                        //only document with a valid id get processed
                        if (doc != "" && doc != null)
                        {
                            //check if the doc exist: if yes then DELETE
                            if (DocExist(doc))
                            {
                                DeleteDoc(doc);
                            }

                            //make the post
                            bool success = PostDoc();

                            if (success)
                            {
                                //write back the XML in the new format
                                WriteDocToFile(doc, out_dir + @"\" + f.Name);
                                Console.WriteLine(f.Name + "converted");

                                //apply the fix
                                FixDoc(out_dir + @"\" + f.Name);
                            }
                            else
                            {
                                Console.WriteLine("Unable to convert file " + f.Name);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unable to retrieve the document ID from file " + f.Name);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            //}
            //else
            //{
            //    Console.WriteLine("The directory switch is mandatory.");
            //}

        }

        #region Methods        

        public static void GetDocs(DirectoryInfo dir)
        {
            try
            {                
                foreach (FileInfo f in dir.GetFiles("*.xml"))
                {
                    docList.Add(f);
                }
            }
            catch
            {
                Console.WriteLine("Directory {0} could not be accessed.", dir.FullName);
                return;
            }
        }

        public static string GetDocID(string path)
        {
            try
            {
                xmlDoc.RemoveAll();
                xmlDoc.Load(path);

                return ((xmlDoc).DocumentElement).Attributes["id"].Value;
            }
            catch
            {                
                return null;
            }
        }

        public static bool DocExist(string doc)
        {
            string requestURL = endpoint + doc;
            HttpWebResponse response = MakeRequest(requestURL, "GET");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                response.Close();
                return true;
            }
            else
            {
                response.Close();
                return false;
            }
        }

        public static void GetDoc(string doc)
        {
            string requestURL = endpoint + doc;
            HttpWebResponse response = MakeRequest(requestURL, "GET");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("GET failed: " + response.StatusCode.ToString());
            }
            else
            {
                xmlDoc.Load(response.GetResponseStream());
            }
            response.Close();
        }

        public static void DeleteDoc(string doc)
        {
            string requestURL = endpoint + doc;
            HttpWebResponse response = MakeRequest(requestURL, "DELETE");
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine("DELETE failed: " + response.StatusCode.ToString());
            }
            response.Close();
        }

        public static bool PostDoc()
        {
            bool success = false;
            string requestURL = endpoint;
            HttpWebResponse response = MakeRequest(requestURL, "POST");
            if (response.StatusCode != HttpStatusCode.Created)
            {
                success = false;
                Console.WriteLine("POST failed: " + response.StatusCode.ToString());
            }
            else
            {
                success = true;
            }
            response.Close();
            return success;
        }

        public static void WriteDocToFile(string doc, string destination)
        {
            GetDoc(doc);
            xmlDoc.Save(destination);
        }

        public static HttpWebRequest PrepareRequest(string requestURL, string requestType)
        {
            string auth = "ISIS realm=" + "\"" + "bcaresearch.com" + "\"" + " token=" + "\"" + "MTpkZXZpY2U6Ym9iOjM6MTM4ODUzNDQwMDpBIFJhbmRvbSBTdHJpbmc6RkNIVGtTSWgvcFY4d3lSQW5ycDJKVCszK0hQVGlrd01uejhrQ3oyTlFIbz0=" + "\"" + "";
            string contentType = "application/vnd.euromoney.semarkdown-document.v1+xml";

            HttpWebRequest request = WebRequest.Create(requestURL) as HttpWebRequest;
            request.Method = requestType;
            request.ContentType = contentType;
            request.Headers.Add(HttpRequestHeader.Authorization, auth);

            if (requestType == "POST")
            {
                // Get the request stream. 
                string postData = xmlDoc.InnerXml
                    .Replace(System.Environment.NewLine, string.Empty)
                    .Replace("\n", String.Empty)
                    .Replace("\r", String.Empty)
                    .Replace("\t", String.Empty);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);                

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }
            return request;
        }

        public static HttpWebResponse MakeRequest(string requestURL, string requestType)
        {
            try
            {
                HttpWebRequest request = PrepareRequest(requestURL, requestType);
                WebResponse resp = request.GetResponse();
                HttpWebResponse response = resp as HttpWebResponse;
                
                return response;
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;
                response.Close();
                return response;
            }
        }
        
        public static void Serialize(dynamic entity, string destination)
        {
            System.IO.TextWriter fileStream = new System.IO.StreamWriter(destination);

            XmlSerializer s = new XmlSerializer(entity.GetType());
            s.Serialize(fileStream, (object)entity);

            fileStream.Close();



            XmlDocument xml = new XmlDocument();
            xml.Load(destination);

            XmlElement root = xml.DocumentElement;
            XmlAttribute value = xmlDoc.CreateAttribute("id");            
            XmlAttribute attr = root.SetAttributeNode(value);
            attr.Value = root.GetElementsByTagName("id")[0].InnerText;

            XmlAttribute value2 = xmlDoc.CreateAttribute("xmlns");
            XmlAttribute attr2 = root.SetAttributeNode(value2);
            attr2.Value = "urn:schemas-euromoney-com:delphi";

            XmlNode node = root.SelectSingleNode("id");
            node.ParentNode.RemoveChild(node);

            xml.Save(destination);

        }

        public static void FixDoc(string path)
        {
            XmlDocument xml = new XmlDocument();

            xml.Load(path);


            //Ontology namespaces
            xml.DocumentElement.GetElementsByTagName("rdf:RDF")[0].Attributes[1].Value = "http://data.euromoneyplc.com/ontologies/annotation/";
            xml.DocumentElement.GetElementsByTagName("rdf:RDF")[0].Attributes[2].Value = "http://data.euromoneyplc.com/annotation-types/";

            //Atom namespaces
            XmlElement root = xml.DocumentElement["atom:entry"];
            XmlAttribute value1 = xmlDoc.CreateAttribute("xmlns:an");
            XmlAttribute value2 = xmlDoc.CreateAttribute("xmlns:atom");

            XmlAttribute attr1 = root.SetAttributeNode(value1);
            attr1.Value = "urn:schemas-euromoney-com:annotation";

            XmlAttribute attr2 = root.SetAttributeNode(value2);
            attr2.Value = "http://www.w3.org/2005/Atom";


            //Link self
            root = xml.DocumentElement;

            XmlNode node = xml.CreateNode(XmlNodeType.Element, "link", null);            
            XmlNode reaf = xml.DocumentElement.SelectSingleNode("link");

            xml.DocumentElement.InsertAfter(node, reaf);


            XmlAttribute value3 = xmlDoc.CreateAttribute("rel");
            XmlAttribute attr3 = xml.DocumentElement["link"].SetAttributeNode(value3);
            attr3.Value = "self";

            XmlAttribute value4 = xmlDoc.CreateAttribute("href");
            XmlAttribute attr4 = xml.DocumentElement["link"].SetAttributeNode(value4);
            attr4.Value = endpoint + GetDocID(path);

            XmlAttribute value5 = xmlDoc.CreateAttribute("type");
            XmlAttribute attr5 = xml.DocumentElement["link"].SetAttributeNode(value5);
            attr5.Value = "application/vnd.euromoney.documents.v1+xml";



            //change reference names
            foreach (XmlElement e in xml.DocumentElement.GetElementsByTagName("ema:annotatedAs"))
            {
                if (e.Attributes[0].Value == "http://data.euromoneyplc.com/annotation-types/chartReference")
                {
                    e.Attributes[0].Value = "http://data.euromoneyplc.com/annotation-types/chart-reference";
                }

                if (e.Attributes[0].Value == "http://data.euromoneyplc.com/annotation-types/counterargument")
                {
                    e.Attributes[0].Value = "http://data.euromoneyplc.com/annotation-types/counter-argument";
                }
            }

            //save
            xml.Save(path);

            FindAndReplace(path);

        }


        public static void FindAndReplace(string path)
        {
            List<string> lines_out = new List<string>();
            List<string> lines = new List<string>(File.ReadAllLines(path));


            foreach (string line in lines)
            {
                string line_out = line.Replace("data.euromoneyplc.com", "data.emii.com").Replace("http://data.emii.com/ontologies/annotation", "http://data.emii.com/ontologies/annotation/");
                lines_out.Add(line_out);
            }

            File.WriteAllLines(path, lines_out);
        }

        #endregion


    }

    public class document
    {
        public string title { get; set; }
        public string service { get; set; }
        public string published { get; set; }
        public string id { get; set; }
        public string content { get; set; }

        public static List<document> GetDataFromMySQL()
        {
            string server = "localhost";
            string database = "bca_web";
            string uid = "root";
            string password = "TiyWzR1S";

            //initialize
            string connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            string query = "select b.title, b.prod service, b.published, filename id, a.body content from reports_annot a left join reports b on id = report_id where b.prod = 'GIS';";

            //open connection
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            //get the data
            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader dataReader = cmd.ExecuteReader();

            List<document> reports = new List<document>();
            while (dataReader.Read())
            {
                document r = new document();

                r.title = dataReader["title"].ToString();
                r.service = dataReader["service"].ToString();
                r.published = ((DateTime)dataReader["published"]).ToString("yyyy-MM-dd");
                r.id = dataReader["id"].ToString().Replace(".PDF","");
                r.content = dataReader["content"].ToString();

                reports.Add(r);
            }

            dataReader.Close();
            connection.Close();

            return reports;
        }
    }
}
