

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Xml.Schema;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
//using Altova.AltovaXML;

namespace MTConnectAgentCore 
{
    public class Data : IData
    {
        int realbuffersize;
        int buffersize;
        String sender;
        String instanceId;
        String version;
        XElement probe; //initialized from Devices.xml
        XElement probe2;
        XElement streams;
        XElement datastorage;
        private long sequence;
        //long minIndex;
        long minAtIndex;
        long firstSequence;
        long lastSequence;
        int bufferSizeCounter;
        XmlNamespaceManager namespaceManager;
        private static bool validationSuccess;
        public static String prefix;
        public string currentXSLTHREF ="";
        public string probeXSLTHREF ="";
        public string errorXSLTHREF = "";

        public Data()
        {
            //sequence = 1;
            //minIndex = 1;
            //bufferSizeCounter = 0;
            
        }
        public String getSender()
        {
            return this.sender;
        }
        public String getVersion()
        {
            return this.version;
        }

        

        public short loadConfig()
        {
            
            try {
                //creating xml from file
                XmlReader reader = XmlReader.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Devices.xml");
                probe = XElement.Load(reader);
                if (probe.GetPrefixOfNamespace(probe.Name.Namespace) == null)
                {
                    prefix = null;
                }
                else 
                {
                    prefix = probe.GetPrefixOfNamespace(probe.Name.Namespace);
                }
                probe2 = probe;
                //probe2.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Devices.xml");
                XmlNameTable nameTable = reader.NameTable;
                
                //XmlNameTable nameTable = probe2.NameTable;
                namespaceManager = new XmlNamespaceManager(nameTable);
                if (prefix==null)
                { namespaceManager.AddNamespace(string.Empty, MTConnectNameSpace.mtConnectUriDevices);  }
                else
                {
                    namespaceManager.AddNamespace(prefix, MTConnectNameSpace.mtConnectUriDevices);
 
                }
                namespaceManager.AddNamespace("mts", MTConnectNameSpace.mtConnectUriStreams);
                namespaceManager.AddNamespace("mt", MTConnectNameSpace.mtConnectUriDevices);
                //namespaceManager.AddNamespace(MTConnectNameSpace.mtConnectPrefix, MTConnectNameSpace.mtConnectUriError);
            }
            catch (Exception e)
            {
                throw new AgentException("Loading Devices.xml Failed.", e);
            }
            //validation of Devices.xml against MTConnectDevices.xsd
           /* ApplicationClass appXML = new ApplicationClass();
            XMLValidator XMLValidator = appXML.XMLValidator;
            StreamReader schemaIn = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "MTConnectDevices.xsd");
            StringBuilder stringb = new System.Text.StringBuilder();
            char[] buf = new char[16384];
            int count;
            do
            {
                count = schemaIn.Read(buf, 0, 16384);
                stringb.Append(buf, 0, count);
            } while (count > 0);


            XMLValidator.SchemaFromText = stringb.ToString();

            XMLValidator.InputXMLFromText = probe.ToString();
            bool IsValid = XMLValidator.IsValidWithExternalSchemaOrDTD();
            if(!IsValid)
                return ReturnValue.ERROR; //validation failed*/
            byte[] byteArray = Encoding.ASCII.GetBytes(probe.ToString());
            MemoryStream stream = new MemoryStream(byteArray); 
            validationSuccess = true;

            //XmlReader schemaStream = XmlReader.Create("MTConnectDevices.xsd");
            XmlSchemaSet sc = new XmlSchemaSet();
           // sc.Add("urn:mtconnect.org:MTConnectDevices:1.1", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "MTConnectDevices.xsd");
            sc.Add("urn:mtconnect.org:MTConnectDevices:1.1", new XmlTextReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MTConnectAgentCore.MTConnectDevices.xsd")));

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = sc;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationHandler);
            XmlReader Xreader = XmlReader.Create(stream, settings);
            while (Xreader.Read()) ;
            if (!validationSuccess)
                return ReturnValue.ERROR;
          
            

           XElement header = probe.Element(MTConnectNameSpace.mtDevices + "Header");
          
            try
            {
                this.buffersize = Int32.Parse(header.Attribute("bufferSize").Value);              
                
               
            }
            catch (Exception e)
            {
                throw new AgentException("Devices.xml's Header bufferSize value can not be converted into an integer.", e);
            }

             
            this.sender = header.Attribute("sender").Value;
            this.version = header.Attribute("version").Value;
            this.instanceId = header.Attribute("instanceId").Value;
                 

            
            streams = DataUtil.createStreams(probe, namespaceManager);
          
            datastorage = DataUtil.createDataStorage(probe2, namespaceManager, out sequence, out bufferSizeCounter, out minAtIndex, out realbuffersize, out firstSequence, out lastSequence, buffersize); //clone of probe without its name space
            
            return ReturnValue.SUCCESS;
        
        }

        private static void ValidationHandler(object sender, ValidationEventArgs args)
        {
            validationSuccess = false; //Validation failed

           

        }
        //createStreamFromDataStorage(xpathResults, count, from,at, ref streamclone, current, deviceId);
        private void createStreamFromDataStorage(IEnumerable<XElement> _data, long count, long from, long at, long frequency, ref XElement _streamclone, Boolean current, String _deviceIdLookingFor, out XElement header )//changed
        {
            
            lock (this)
            {
                if (from > sequence)
                {
                    _streamclone = Error.createError(this, Error.OUT_OF_RANGE,"\"from\" is greater than the last dataItem's sequence number +1.");
                    header = null;
                    return;
                }
                else if(from>0 && from < firstSequence)
                {
                    _streamclone = Error.createError(this, Error.OUT_OF_RANGE, "\"from\" is less than the minimum available sequence number. ");
                    header = null;
                    return;

                }
                else if (count < 1&& !current)
                {
                    _streamclone = Error.createError(this, Error.INVALID_REQUEST, "\"count\" is less than 1. ");
                    header = null;
                    return;
 
                }
                else if (at >= 0 && at < firstSequence)
                {
                    _streamclone = Error.createError(this, Error.OUT_OF_RANGE, "\"at\" is less than the minimum available sequence number. ");
                    header = null;
                    return;

                }
                else if (at > lastSequence)
                {
                    _streamclone = Error.createError(this, Error.OUT_OF_RANGE, "\"at\" is greater than the last dataItem's sequence number. ");
                    header = null;
                    return;

                }

                else
                {
                    if (_data.Count() > 0)
                    {
                        //_data is DataItem elements
                        if (_data.ElementAt(0).Name.LocalName.Equals("DataItem"))
                            handleDataItems(_data, count, from, at, frequency, ref _streamclone, current, _deviceIdLookingFor);
                        else
                        {
                            foreach (XElement d in _data)
                            {
                                
                                IEnumerable<XElement> dataItems = d.Descendants("DataItem");
                                handleDataItems(dataItems, count, from, at, frequency, ref _streamclone, current, _deviceIdLookingFor);
                            }
                        }
                    }
                    if (current)
                    {
                        if (at == -1)
                        {
                            header = getHeader(sequence);
                        }
                        else
                        {
                            header = getHeader(sequence);
                        }

                    }
                    else
                    {
                        if (from == 0)
                        {
                            if (firstSequence + count < sequence)
                                header = getHeader(firstSequence + count);
                            else
                                header = getHeader(sequence);
                        }
                        else
                        {
                            if (from + count - 1 < sequence)
                                header = getHeader(from + count);
                            else
                                header = getHeader(sequence);

                        }


                    }
                }

            }
        }

        private void createStreamFromDataStorage(ref XElement _streamclone, Boolean current, String _deviceIdLookingFor, out XElement header)//changed
        {
            
            lock (this)
            {
                long nextsequence = sequence;
                header = getHeader(nextsequence);
                //query from datastorage
               
               IEnumerable<XElement> dataItems = this.datastorage.Descendants("DataItem");
               
                
                handleDataItems(dataItems, 100, 0, -1, -1, ref _streamclone, current, _deviceIdLookingFor);

                if (!current && firstSequence+100<=sequence )
                {
                    nextsequence = firstSequence + 100;
                    header = getHeader(nextsequence);

                }
            }
        }
       

        //
        private void handleDataItems(IEnumerable<XElement> dataItems, long count, long from, long at, long sequency, ref XElement _streamclone, Boolean current, String _deviceIdLookingFor)//changed
        {
            
            foreach (XElement di in dataItems)
            {
                //name, id, type, and category are required
                String dataItem_category = di.Attribute("category").Value;
                String dataItem_id = di.Attribute("id").Value;
                          
                String dataitem_type = di.Attribute("type").Value;
                                


                    XAttribute temp = null;
                    String dataItem_subType = null;
                    String dataitem_name = null;
                    String dataitem_units = null;
                    if ((temp = di.Attribute("subType")) != null)
                        dataItem_subType = temp.Value; //ACTUAL, etc..
                    if ((temp = di.Attribute("name")) != null)
                        dataitem_name = temp.Value;
                    if ((temp = di.Attribute("units")) != null)
                        dataitem_units = temp.Value;
                    //for each data
                  
                    IEnumerable<XElement> dataElements = di.Elements("Data");
                    
                    if (dataElements.Count() != 0)
                    {

                        //prepare to add data to _streamclone
                        String deviceName = DataUtil.getDeviceName(di);
                        if (_deviceIdLookingFor != null && (_deviceIdLookingFor.Equals(deviceName) == false))
                            continue;
                        XElement deviceStream = _streamclone.XPathSelectElement("//mts:DeviceStream[@name='" + deviceName + "']",namespaceManager);
                        XElement dataItemNameElement = di.Parent.Parent;
                       
                        
                        IEnumerable<XElement> componentStreams = deviceStream.Descendants(MTConnectNameSpace.mtStreams+"ComponentStream");
                        XElement componentStream = null;
                        foreach (XElement cStream in componentStreams)
                        {
                            
                              if(cStream.Attribute("name").Value == dataItemNameElement.Attribute("name").Value)  
                            componentStream = cStream;
                        }
                        
                        //query category Samples or Events to place data
                        XElement samplesOrEventsOrCondition;
                        if (dataItem_category != "CONDITION")
                        {
                            samplesOrEventsOrCondition = componentStream.Element(MTConnectNameSpace.mtStreams+DataUtil.modifyString1(dataItem_category) + "s"); //get Samples or Events from SAMPLE or EVENT
                        }
                        else
                        {
                            samplesOrEventsOrCondition = componentStream.Element(MTConnectNameSpace.mtStreams+"Condition"); 
                        }

                        if (current)
                        {
                            if (at == -1)
                            {
                                if (dataItem_category == "CONDITION")
                                {
                                    XElement d = dataElements.Last();
                                    if (d.Attribute("condition").Value != "Normal")
                                    {
                                        int index=0;
                                        XElement[] datas=new XElement[dataElements.Count()];
                                        foreach (XElement dd in dataElements)
                                               {
                                                   datas[index]=dd;
                                                   index=index+1;
                                               }
                                        index = dataElements.Count() - 1;                              
                                        
                                        while (datas[index].Attribute("condition").Value == d.Attribute("condition").Value)
                                        {
                                            index = index - 1;
                                            if (index == -1)
                                                break;
                                            
                                        }
                                        for (int i = index + 1; i < dataElements.Count(); i++)
                                        {
                                            samplesOrEventsOrCondition.Add(DataUtil.createData(dataitem_type, dataitem_name, dataItem_subType, dataItem_id, datas[i], dataItem_category, dataitem_units));
 
                                        }
                                    }
                                    else
                                    {
                                        
                                        samplesOrEventsOrCondition.Add(DataUtil.createData(dataitem_type, dataitem_name, dataItem_subType, dataItem_id, d, dataItem_category, dataitem_units));
                                    }
                                }
                                else
                                {
                                  XElement d = dataElements.Last();
                                  samplesOrEventsOrCondition.Add(DataUtil.createData(dataitem_type, dataitem_name, dataItem_subType, dataItem_id, d, dataItem_category, dataitem_units));
                                }
                                
                            }
                            else
                            {
                                XElement d = dataElements.First();
                                foreach (XElement dd in dataElements)
                                {
                                    if (Convert.ToInt64(dd.Attribute("sequence").Value) > at)
                                        break;
                                    d = dd;
                                }
                                samplesOrEventsOrCondition.Add(DataUtil.createData(dataitem_type, dataitem_name, dataItem_subType, dataItem_id, d, dataItem_category, dataitem_units));
                            }
                        }
                        else //sample
                        {
                            
                            if (from == 0)
                            {   //then from = first sequence number in the buffer
                                lock (this)
                                {
                                    //from = minAtIndex;
                                    from = firstSequence;
                                }
                            }
                            long to = from + count - 1;
                            foreach (XElement d in dataElements)
                            {
                               
                                    XAttribute s = d.Attribute("sequence");
                                    long sequence = Convert.ToInt64(s.Value);
                                    if (sequence >= from && sequence <= to)
                                    {
                                        //assume timestamp must exisit in data
                                        samplesOrEventsOrCondition.Add(DataUtil.createData(dataitem_type, dataitem_name, dataItem_subType, dataItem_id, d, dataItem_category, dataitem_units));
                                    }
                                
                                
                            }
                        }
                    }
                
            }

        }
        //
            
        public short getCurrent(StreamWriter writer)
        {
            XElement streamclone = new XElement(streams);
            
            XElement header;
            createStreamFromDataStorage(ref streamclone, true, null, out header);
            return addHeaderAndXSTToSteram(streamclone, header, writer, null);
        }

        //http://127.0.0.1/devicename/sample
        public short getCurrentDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this, namespaceManager);
            if (device == null) //check if device exist
                return ReturnValue.ERROR;

            XElement streamclone = new XElement(streams);
            XElement header;
            createStreamFromDataStorage(ref streamclone, true, deviceId, out header);
            return addHeaderAndXSTToSteram(streamclone, header, writer, deviceId);
        }

        public short getCurrent(String xpath, StreamWriter writer, String at, String frequency)
        {
            return getCurrentDevice(null, xpath, writer, at, frequency);
        }

        //xpath starts with stream....
        public short getCurrentDevice(String deviceId, String xpath, StreamWriter writer, String at, String frequency)//changed
        {
            if (deviceId != null) //check if device exist
            {
                XElement device = DataUtil.getDevice(probe, deviceId, writer, this, namespaceManager);
                if (device == null)
                    return ReturnValue.ERROR;
            }
            long atNum, frequencyNum; //Signed 64-bit integer
            if (at == null)
                atNum = -1;
            else if (at.Trim().Equals(""))
                atNum = -1;
            else
            {
                atNum = Convert.ToInt64(at);
                if (atNum < 0)
                {
                    XElement mtxst = Error.createError(this, Error.INVALID_REQUEST, "\"at\" in Current Request is negative.");
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
            }

            if (frequency == null)
                frequencyNum = -1;
            else if (frequency.Trim().Equals(""))
                frequencyNum = -1;
            else
            {
                frequencyNum = Convert.ToInt64(frequency);
                if (frequencyNum < 0)
                {
                    XElement mtxst = Error.createError(this, Error.INVALID_REQUEST, "\"frequency\" in Current Request is negative.");
                    
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
            }
            if (frequencyNum == -1)
            {
                return getCurrentOrStreamDevice(deviceId, xpath, -1, -1, writer, true, atNum, -1);
            }
            else// need to deal with frequency parameter 
            {
                return getCurrentOrStreamDevice(deviceId, xpath, -1, -1, writer, true, atNum, -1);
            }
            
            
        }

                
        //all stream
        public short getStream(StreamWriter writer)
        {
            XElement streamclone = new XElement(streams);
            XElement header;
            
            createStreamFromDataStorage(ref streamclone, false, null, out header);
            return addHeaderAndXSTToSteram(streamclone, header, writer, null);
        }

        public short getStreamDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this, namespaceManager);
            if (device == null) //check if device exist
                return ReturnValue.ERROR;

            XElement streamclone = new XElement(streams);
            XElement header;
            createStreamFromDataStorage(ref streamclone, false, deviceId, out header);
            return addHeaderAndXSTToSteram(streamclone, header, writer, deviceId);
        }

        public short getStream(String xpath, String from, String count, StreamWriter writer, String frequency)
        {
            return getStreamDevice(null, xpath, from, count, writer, frequency);
        }

        //xpath starts with stream....
        public short getStreamDevice(String deviceId, String xpath, String from, String count, StreamWriter writer, String frequency)//changed
        {
            if (deviceId != null) //check if device exist
            {
                XElement device = DataUtil.getDevice(probe, deviceId, writer, this, namespaceManager);
                if (device == null)
                    return ReturnValue.ERROR;
            }
            long fromNum, countNum, frequencyNum; //Signed 64-bit integer
            if (from == null)
                fromNum = 0;
            else if (from.Trim().Equals(""))
                fromNum = 0;
            else
            {
                fromNum = Convert.ToInt64(from);
                if (fromNum < 0)
                {
                    XElement mtxst = Error.createError(this, Error.INVALID_REQUEST, "\"from\" in Sample Request is negative.");
                    
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
            }

            if (count == null)
                countNum = 100;
            else if ( count.Trim().Equals(""))
                countNum = 100;
            else
            {
                countNum = Convert.ToInt64(count);
                if (countNum < 0)
                {
                    XElement mtxst = Error.createError(this, Error.INVALID_REQUEST, "\"count\" in Sample Request is negative.");
                    
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
            }

            if (frequency == null)
                frequencyNum = -1;
            else if (frequency.Trim().Equals(""))
                frequencyNum = -1;
            else
            {
                frequencyNum = Convert.ToInt64(frequency);
                if (frequencyNum < 0)
                {
                    XElement mtxst =Error.createError(this, Error.INVALID_REQUEST, "\"frequency\" in Sample Request is negative.");
                    
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
            }

            if (frequencyNum == -1)
            {
                return getCurrentOrStreamDevice(deviceId, xpath, fromNum, countNum, writer, false, -1, -1);
            }
            else// need to deal with frequency parameter 
            {
                return getCurrentOrStreamDevice(deviceId, xpath, fromNum, countNum, writer, false, -1, -1);
            }
        }

        private short getCurrentOrStreamDevice(String deviceId, String path, long from, long count, StreamWriter writer, Boolean current, long at, long frequency )//changed
        {

            IEnumerable<XElement> xpathResults=null;
            //XDocument doc = new XDocument(this.datastorage);
            //XElement doc=
            if (path != null)
            {   ////http://ip/deviceId/sample?path=.... or //http://ip/deviceId/current?path=....

                try
                {
                    //xpathResults = this.datastorage.XPathSelectElements(path, namespaceManager);
                    xpathResults = this.datastorage.XPathSelectElements(path);
                }
                catch (System.Xml.XPath.XPathException e)
                {
                    XElement mtxst = Error.createError(this, Error.INVALID_PATH, e.Message);
                    
                    mtxst.Save(writer);
                    return ReturnValue.ERROR;
                }
             }
            else  //http://ip/deviceId/sample?from=2&count=100 //then all datItem
            {
                //xpathResults = this.datastorage.XPathSelectElements("//mt:DataItem", namespaceManager);
                //xpathResults = this.datastorage.XPathSelectElements("//mt:DataItem", namespaceManager);  
               // xpathResults = this.datastorage.Descendants(MTConnectNameSpace.mtDevices+"DataItem");
                xpathResults = this.datastorage.Descendants("DataItem");
            }

            if (xpathResults.Count() == 0)//changed
            {
               // XElement streamclone = new XElement(streams);
                //addHeaderAndXSTToSteram(streamclone, writer);
                //return ReturnValue.SUCCESS;
                XElement mtxst = Error.createError(this, Error.INVALID_PATH, "the path cannot be found.");
                
                mtxst.Save(writer);
                return ReturnValue.ERROR;                
            }
            else
            {
                XElement streamclone = new XElement(streams);
                XElement header=null;
                createStreamFromDataStorage(xpathResults, count, from, at, frequency, ref streamclone, current, deviceId, out header);
                
                if (streamclone.Name.LocalName == "Streams")
                {
                    addHeaderAndXSTToSteram(streamclone, header, writer, deviceId);
                    return ReturnValue.SUCCESS;
                }
                else
                { 
                    
                    streamclone.Save(writer);
                    return ReturnValue.ERROR;
                }
                    
            }
        }
    
      /*  private short addHeaderAndXSTToSteram(XElement streamclone, StreamWriter writer )//changed
        {
            streamclone = DataUtil.trimStream(streamclone);           
            XElement header = getHeader(getNextSequence(streamclone));
            XElement mtxst = Util.createStreamXST();
            mtxst.Add(header);
            mtxst.Add(streamclone);
            mtxst.Save(writer);
            return ReturnValue.SUCCESS;
        }*/

       /* private short addHeaderAndXSTToSteram(XElement streamclone, XElement header, StreamWriter writer, String deviceId)//changed
        {
            streamclone = DataUtil.trimStream(streamclone, deviceId, namespaceManager);     
            XElement mtxst = Util.createStreamXST();
            mtxst.Add(header);
            mtxst.Add(streamclone);
            
            //XmlDocument mtxmldoc = convertXElementtoXmlDocument(mtxst);
            if (this.currentXSLTHREF != "")
            {
                XmlReader reader = XmlReader.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + this.currentXSLTHREF);
                XElement xlst=XElement.Load(reader);
                xlst.Add(new XAttribute("id","stylesheet"));
                
                xlst.FirstNode.AddBeforeSelf(mtxst);
                XmlDocument mtxmldoc = convertXElementtoXmlDocument(xlst);
               

                XmlProcessingInstruction newPI;
                String PItext = "type='text/xsl' href='" + "#stylesheet" + "'";
                newPI = mtxmldoc.CreateProcessingInstruction("xml-stylesheet", PItext);
                mtxmldoc.InsertBefore(newPI, mtxmldoc.FirstChild);
                mtxmldoc.Save(writer);
            }
            else
            {
                mtxst.Save(writer);
            }
           
           // mtxst.Save(writer);
            return ReturnValue.SUCCESS;
        }*/
        private short addHeaderAndXSTToSteram(XElement streamclone, XElement header, StreamWriter writer, String deviceId)//changed
        {
            streamclone = DataUtil.trimStream(streamclone, deviceId, namespaceManager);
            XElement mtxst = Util.createStreamXST();
            mtxst.Add(header);
            mtxst.Add(streamclone);

            //XmlDocument mtxmldoc = convertXElementtoXmlDocument(mtxst);
            if (this.currentXSLTHREF != "")
            {
                XmlReader reader = XmlReader.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + this.currentXSLTHREF);
                XElement xlst = XElement.Load(reader);
                xlst.Add(new XAttribute("id", "stylesheet"));
                XElement xdoc = new XElement("doc");
                xdoc.Add(xlst);
                xdoc.Add(mtxst);
                Console.WriteLine(xdoc.ToString());

                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<!DOCTYPE doc [<!ATTLIST xsl:stylesheet id ID #REQUIRED>]>"+xdoc.ToString());
                
              

               // XmlDocument doc = convertXElementtoXmlDocument(xdoc);
                
               // XmlDocument doc2 = new XmlDocument();
               // XmlDocumentType doctype;
               // doctype = doc.CreateDocumentType("doc", null, null, "<!ATTLIST xsl:stylesheet id ID #REQURED>");
               // doc.AppendChild(doctype);
                //doc.AppendChild(docNode);
                
                XmlProcessingInstruction newPI;
                String PItext = "type='text/xsl' href='" + "#stylesheet" + "'";
                newPI = doc.CreateProcessingInstruction("xml-stylesheet", PItext);
                doc.InsertBefore(newPI, doc.FirstChild);
                
                
                
                
                
                Console.WriteLine(doc.OuterXml);
                
                doc.Save(writer);
            }
            else
            {
                mtxst.Save(writer);
            }

            // mtxst.Save(writer);
            return ReturnValue.SUCCESS;
        }


        private  XmlNode convertXElementtoXmlNode(XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc;
            }
        }

        private XmlDocument convertXElementtoXmlDocument(XElement mtxst)
        {
            
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = true;

            using (XmlWriter xw = XmlWriter.Create(sb, xws))
            {
                mtxst.WriteTo(xw);
            }
            XmlDocument doc = new XmlDocument();
            
           
            doc.LoadXml(sb.ToString());
            return doc;
        }
       
        public short getDebug(StreamWriter writer)
        {
            datastorage.Save(writer);
            return ReturnValue.SUCCESS;
        }

        //DAF 2008-07-31 Added 
        public short getVersion(StreamWriter writer)
        {
            string version2 = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //DAF 2008-08-22 Modified - bug #1106 
            XmlTextWriter xw = new XmlTextWriter(writer);
            xw.Formatting = Formatting.Indented; // optional
            xw.WriteStartElement("MTConnectAgent");
            xw.WriteAttributeString("version", version2);
            xw.WriteEndElement();
            return ReturnValue.SUCCESS;
        }
        public short getLog(StreamWriter writer)
        {
            while (true)
            {
                try
                {
                    return getLog2(writer);
                }
                catch (IOException)
                {
                    Thread.Sleep(10);
                }
            }
        }
        public short getLog2(StreamWriter writer)
        {
            try
            {
                string logdata = "Log not available."; //default
                if (File.Exists(LogToFile.currentLogFileName))
                {
                    FileStream file = new FileStream(LogToFile.currentLogFileName, FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite);

                    // Create a new stream to read from a file
                    StreamReader sr = new StreamReader(file);
                    logdata = sr.ReadToEnd();
                }
                // Read contents of file into a string
                string loghead = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><MTConnectAgent>";
                string logends = "</MTConnectAgent>";
                writer.Write(loghead + logdata + logends);
            }
            catch (IOException e)
            {
                throw e;
            }
            return ReturnValue.SUCCESS;
        }

        public short getConfig(StreamWriter writer)
        {
            string configini = "mtcagent.xml";
            try
            {
                FileStream file = new FileStream(configini, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                
                // Create a new stream to read from a file
                StreamReader sr = new StreamReader(file);

                // Read contents of file into a string
                string logdata = sr.ReadToEnd();
                writer.Write(logdata);
            }
            catch (Exception e)
            {
                Console.WriteLine("getConfig:Error:" + e.Message);
                LogToFile.Log("getConfig:Error: " + e.Message);
                return ReturnValue.ERROR;
            }
            return ReturnValue.SUCCESS;
        }
        //DAF 2008-07-31 End 

        //all probe
        public short getProbe(StreamWriter writer)
        {
            resetProbeHeader();
            //probe.Save(writer);
            XmlDocument mtxmldoc = convertXElementtoXmlDocument(probe);
            if (this.probeXSLTHREF != "")
            {

                XmlProcessingInstruction newPI;
                String PItext = "type='text/xsl' href='"+this.probeXSLTHREF+"'";
                newPI = mtxmldoc.CreateProcessingInstruction("xml-stylesheet", PItext);
                mtxmldoc.InsertBefore(newPI, mtxmldoc.FirstChild);
            }

            mtxmldoc.Save(writer);
            return ReturnValue.SUCCESS;
        }
        //xpath starts with probe....       
        public short getProbeDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this, namespaceManager);
            if (device != null)
            {
                //create new XElment Devices and put add device
                XElement newDevices = new XElement(MTConnectNameSpace.mtDevices+"Devices", device);
                XElement header = getProbeHeader();
                XElement mtxst = Util.createDeviceXST();
                mtxst.Add(header);
                mtxst.Add(newDevices);
                //mtxst.Save(writer);
                XmlDocument mtxmldoc = convertXElementtoXmlDocument(mtxst);
                if (this.probeXSLTHREF != "")
                {

                    XmlProcessingInstruction newPI;
                    String PItext = "type='text/xsl' href='" + this.probeXSLTHREF + "'";
                    newPI = mtxmldoc.CreateProcessingInstruction("xml-stylesheet", PItext);
                    mtxmldoc.InsertBefore(newPI, mtxmldoc.FirstChild);
                }

                mtxmldoc.Save(writer);
                return ReturnValue.SUCCESS;
            }
            else
                return ReturnValue.ERROR;
        }

        public String[] getDevices()
        {
            IEnumerable<XElement> devices = probe.XPathSelectElements("//Devices/Device");
            String[] names = new String[devices.Count()];
            int i = 0;
            foreach (XElement d in devices)
            {
                names[i++] = d.Attribute("name").Value;
            }
            return names;
        }

        private void resetProbeHeader()
        {
            probe.Element(MTConnectNameSpace.mtDevices+"Header").SetAttributeValue("creationTime", Util.GetDateTime());
            probe.Element(MTConnectNameSpace.mtDevices+"Header").SetAttributeValue("instanceId", instanceId);
        }

        public XElement getErrorHeader()
        {
            XElement header =
                new XElement(MTConnectNameSpace.mtError+"Header",
                    new XAttribute("creationTime", Util.GetDateTime()),
                    new XAttribute("instanceId", instanceId),
                    new XAttribute("sender", sender),
                    new XAttribute("bufferSize", buffersize),
                    new XAttribute("version", version)
                    );
            return header;
        }

        public XElement getProbeHeader()
        {
            XElement header =
                new XElement(MTConnectNameSpace.mtDevices + "Header",
                    new XAttribute("creationTime", Util.GetDateTime()),
                    new XAttribute("instanceId", instanceId),
                    new XAttribute("sender", sender),
                    new XAttribute("bufferSize", buffersize),
                    new XAttribute("version", version)
                    );
            return header;
        }

       private long getNextSequence(XElement ele)       

        {
            long nextSequence = 0;           
            long temp;
            IEnumerable<XElement> sampledata = ele.XPathSelectElements("//Samples").Elements();
            foreach (XElement d in sampledata)
            {
                temp = Convert.ToInt64(d.Attribute("sequence").Value);
                if (temp > nextSequence)
                    nextSequence = temp;
            }
            IEnumerable<XElement> eventdata = ele.XPathSelectElements("//Events").Elements();
            
            foreach( XElement d in eventdata)
            {
                temp = Convert.ToInt64(d.Attribute("sequence").Value);
                if (temp > nextSequence)
                    nextSequence = temp;
            }
            //
            IEnumerable<XElement> conditiondata = ele.XPathSelectElements("//Condition").Elements();
            foreach (XElement d in conditiondata)
            {
                temp = Convert.ToInt64(d.Attribute("sequence").Value);
                if (temp > nextSequence)
                    nextSequence = temp;
            }
            nextSequence++; //increment one
            return nextSequence;
        }

        //
    

        //private XElement getHeader(int nextSequence)
        private XElement getHeader(long nextSequence)//changed
        {
            XElement header =
                new XElement(MTConnectNameSpace.mtStreams + "Header",
                    new XAttribute("creationTime", Util.GetDateTime()),
                    new XAttribute("instanceId", instanceId),
                    new XAttribute("nextSequence", nextSequence.ToString()),
                    new XAttribute("sender", sender),
                    new XAttribute("bufferSize", buffersize),
                  
                    new XAttribute("firstSequence",firstSequence.ToString()),
                    new XAttribute("lastSequence",lastSequence.ToString()),
                    
                    new XAttribute("version", version)
                    );
            return header;
        }
              

        //Machine API in directory called

        public short StoreEvent(String _timestamp, String _dataItemId, String _value, String _code, String _nativeCode)
        {
            Thread.Sleep(10);
            lock (this)
            {
                XElement datastorage_dataitem = StoreSampleEventCommon( _dataItemId, "EVENT", _code, _nativeCode);

                XElement lastdata = datastorage_dataitem.Elements("Data").Last();


                if (lastdata.Value == _value)
                {
                    if (datastorage_dataitem.Attribute("type").Value != "ALARM")
                    {
                        //LogToFile.Log("The value to store for DataItem (id = \"" + _dataItemId + "\") is the same with previous stored value. ");
                        //throw new AgentException("The value to store for DataItem (id = \"" + _dataItemId + "\") is the same with previous stored value. ");
                    }
                    else
                    {
                        if (lastdata.Attribute("code").Value == _code & lastdata.Attribute("nativeCode").Value == _nativeCode)
                        {
                            //LogToFile.Log("The value to store for DataItem (id = \"" + _dataItemId + "\") is the same with previous stored value. ");
                            //throw new AgentException("The value to store for DataItem (id = \"" + _dataItemId + "\") is the same with previous stored value. ");
                        }
                        else
                        {
                            CheckBufferSize();
                            datastorage_dataitem.Add(DataUtil.createEventData(_timestamp, sequence + "", _value, _code, _nativeCode));
                            sequence++;
                            bufferSizeCounter++;
                            lastSequence = sequence - 1;
                        }
                    }
                }
                else
                {
                    CheckBufferSize();
                    datastorage_dataitem.Add(DataUtil.createEventData(_timestamp, sequence + "", _value, _code, _nativeCode));
                    sequence++;
                    bufferSizeCounter++;
                    lastSequence = sequence - 1;
                }
                return ReturnValue.SUCCESS;
            }
        }

        public short StoreSample(String _timestamp,  String _dataItemId, String _value)
        {
            Thread.Sleep(10);
            //may throw AgentException if _value can not converted into double
            //double dvalue = DataUtil.getDValue(_timestamp, _deviceName, _dataItemName,_value,_workPieceId, _partId);
            
            lock (this)
            {
                XElement datastorage_dataitem = StoreSampleEventCommon(_dataItemId, "SAMPLE",null, null);
                XElement lastdata = datastorage_dataitem.Elements("Data").Last();
                if (lastdata.Value == _value)   // value be post is the same with the latest posted value for the data
                {
                    
                }
                else
                {
                    CheckBufferSize();
                    datastorage_dataitem.Add(DataUtil.createSampleData(_timestamp, _value, sequence + ""));
                    sequence++;
                    bufferSizeCounter++;
                    lastSequence = sequence - 1;
                }
            }
            return ReturnValue.SUCCESS;
      
        }

        //
        public short StoreCondition(String _timestamp, String _dataItemId, String _condition, String _value, String _nativeCode, String _code)
        {
            Thread.Sleep(10);
            //may throw AgentException if _value can not converted into double
            //double dvalue = DataUtil.getDValue(_timestamp, _deviceName, _dataItemName,_value,_workPieceId, _partId);

            lock (this)
            {
                XElement datastorage_dataitem = StoreConditionCommon(_dataItemId, "CONDITION");
                XElement lastdata = datastorage_dataitem.Elements("Data").Last();
                if (lastdata.Attribute("condition").Value == _condition & lastdata.Value==_value)
                {
                    if (lastdata.Attribute("nativeCode").Value != _nativeCode)
                    {
                        CheckBufferSize();
                        datastorage_dataitem.Add(DataUtil.createConditionData(_timestamp, _value, _condition, sequence + "", _nativeCode, _code));
                        sequence++;
                        bufferSizeCounter++;
                        lastSequence = sequence - 1;
                    }
                    else
                    {
                        
 
                    }
                }
                else
                {
                    CheckBufferSize();
                    datastorage_dataitem.Add(DataUtil.createConditionData(_timestamp, _value, _condition, sequence + "", _nativeCode, _code));
                    sequence++;
                    bufferSizeCounter++;
                    lastSequence = sequence - 1;
                }
            }
            return ReturnValue.SUCCESS;

        }
        //

        //
        private XElement StoreConditionCommon( String _dataItemId, String categoryType)
        {

            
            XElement datastorage_dataitem = DataUtil.getDataItemFromId(datastorage, _dataItemId, namespaceManager);
            if (datastorage_dataitem == null)
            {
                LogToFile.Log("DataItem (id = \"" + _dataItemId + "\") not found.");
                throw new AgentException("DataItem (id = \"" + _dataItemId + "\") not found.");
            }

            String category = datastorage_dataitem.Attribute("category").Value;
           
            if (!category.Equals(categoryType))
            {
                if (category.Equals("SAMPLE"))
                {
                    LogToFile.Log("Use storeSample for DataItem (id = \"" + _dataItemId + "\").  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeSample for DataItem (id = \"" + _dataItemId + "\").  The DataItem category is " + category + ".");
                }
                else if (category.Equals("EVENT"))
                {
                    LogToFile.Log("Use storeEvent for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeEvent for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                }
                //
                else if (category.Equals("CONDITION"))
                {
                    LogToFile.Log("Use storeCondition for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeCondition for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                }
                //
            }
            return datastorage_dataitem;
        }
        
        
        //

        private XElement StoreSampleEventCommon( String _dataItemId, String categoryType, String _code, String _nativeCode )
        {
           
            
            XElement datastorage_dataitem = DataUtil.getDataItemFromId(datastorage, _dataItemId, namespaceManager);
            if (datastorage_dataitem == null)
            {
               // Console.WriteLine("not find dataitem");
                LogToFile.Log("DataItem (id = \"" + _dataItemId + "\")  not found.");
                throw new AgentException("DataItem (id = \"" + _dataItemId + "\")  not found.");
            }

            String category = datastorage_dataitem.Attribute("category").Value;
            String type = datastorage_dataitem.Attribute("type").Value;

            if (!category.Equals(categoryType))
            {
                if (category.Equals("SAMPLE"))
                {
                    LogToFile.Log("Use storeSample for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeSample for DataItem (id = \"" + _dataItemId + "\").  The DataItem category is " + category + ".");
                }
                else if (category.Equals("EVENT"))
                {
                    LogToFile.Log("Use storeEvent for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeEvent for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                }
                //
                else if (category.Equals("CONDITION"))
                {
                    LogToFile.Log("Use storeCondition for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");
                    throw new AgentException("Use storeCondition for DataItem (id = \"" + _dataItemId + "\") .  The DataItem category is " + category + ".");                
                }
                //
            }
            if(categoryType=="EVENT")
            {
                if (type.Equals("ALARM"))
                {
                    if (_code == null)
                    {
                        LogToFile.Log("Code is needed for DataItem (id = \"" + _dataItemId + "\") . ");
                        throw new AgentException("Code is needed for DataItem (id = \"" + _dataItemId + "\") . ");

                    }
                    else if (_nativeCode == null)
                    {
                        LogToFile.Log("NativeCode is needed for DataItem (id = \"" + _dataItemId + "\") . ");
                        throw new AgentException("NativeCode is needed for DataItem (id = \"" + _dataItemId + "\") . ");
                    }
                    else if (!Util.CheckAlarmCode(_code))
                    {
                        LogToFile.Log("The code value is not one of the recognizable code values.");
                        throw new AgentException(" The code value is not one of the recognizable code values.");
                    
                    }
                }

                else
                {
                    if (_code != null)
                    {
                        LogToFile.Log("Code is not needed for DataItem (id = \"" + _dataItemId + "\"). ");
                        throw new AgentException("Code is not needed for DataItem (id = \"" + _dataItemId + "\") . ");

                    }
                    else if (_nativeCode != null)
                    {
                        LogToFile.Log("NativeCode is not needed for DataItem (id = \"" + _dataItemId + "\"). ");
                        throw new AgentException("NativeCode is not needed for DataItem (id = \"" + _dataItemId + "\") . ");
                    }
                
                }

            }

            return datastorage_dataitem;    
        }
    
        private void CheckBufferSize()//changed
        {
            if (bufferSizeCounter >= realbuffersize)
            {
                XElement temp = datastorage.XPathSelectElement("//Data[@sequence='" + (++minAtIndex).ToString() + "']");
                XElement parentDataItem = temp.Parent;
                //parentDataItem.FirstNode.Remove();
                parentDataItem.Elements().First().Remove();            
                //minIndex=minAtIndex;
                //IEnumerable<XElement> dataItems=datastorage.XPathSelectElements("//DataItem");
                //foreach (XElement dataItem in dataItems)
                //{
                  //  IEnumerable<XElement> children=dataItem.Elements();
                  //  String sequence=children.First().Attribute("sequence").Value;
                  //  if(Convert.ToInt64(sequence)<minIndex)
                  //  { 
                  //      minIndex=Convert.ToInt64(sequence);
                  //  }
               // }
                bufferSizeCounter--;
                firstSequence++;
                    
                
            }
        }

    }
}
