

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections;
using System.Xml.XPath;
using System.IO;
using System.Xml;

namespace MTConnectAgentCore
{
    internal static class DataUtil
    {
        //return value as double if _value can be converted to double otherwise throw exception 
        internal static double getDValue(String _timestamp, String _deviceName, String _dataItemName, String _value, String _workPieceId, String _partId)
        {
            try
            {
                return Double.Parse(_value);
            }
            catch (ArgumentNullException)
            {
                String msg = "Missing value for " + StoreSampleToString(_timestamp, _deviceName, _dataItemName, _value, _workPieceId, _partId);
                LogToFile.Log(msg);
                throw new AgentException(msg);
            }
            catch (Exception ) //FormatException, OverflowException
            {
                String msg = "Value \"" + _value + "\" can not converted into number for " + StoreSampleToString(_timestamp, _deviceName, _dataItemName, _value, _workPieceId, _partId);
                LogToFile.Log(msg);
                throw new AgentException(msg);
            }
        }
       
        internal static String StoreSampleToString(String _timestamp, String _deviceName, String _dataItemName, String _value, String _workPieceId, String _partId)
        {
            String s = "StoreSample(timestamp = \"" + _timestamp + "\", deviceName = \"" + _deviceName + "\" dataItemName = \"" + _dataItemName + "\"" +"\" value = \"" + _value + "\"";
            if (_workPieceId != null)
                s = s + " workPieceId = \"" + _workPieceId + "\"";
            if (_partId != null)
                s = s + " partId = \"" + _partId + "\"";
            s = s + ").";
            return s;
        }

        internal static double convertToUnitFromNativeUnit(XElement dataItemElement, double dvalue)
        {
            XAttribute nativeScale;
            if ((nativeScale = dataItemElement.Attribute("nativeScale")) != null)
            {
                //no exception happens here because its type is xs:float if not validation fails
                double scale = Double.Parse(nativeScale.Value);
                dvalue = dvalue / scale;
                if (Double.IsInfinity(dvalue))
                    throw new AgentException("Dividing by nativeScale \"" + scale + "\" make the value infinity for DataItem name = \"" + dataItemElement.Attribute("name").Value + "\"."); 
            }
            XAttribute nativeUnits, units;
            if ((nativeUnits = dataItemElement.Attribute("nativeUnits")) != null && (units = dataItemElement.Attribute("units")) != null)
                return Util.convertUnit(dataItemElement.Attribute("name").Value, dvalue, nativeUnits.Value, units.Value);
            else
                return dvalue;
        }
        
        internal static XElement getDevice(XElement probe, String deviceId, StreamWriter writer, Data data, XmlNamespaceManager namespaceManager)
        {
           
            XElement devices = probe.Element(MTConnectNameSpace.mtDevices+"Devices");
            if (devices != null)
            {
             
                IEnumerable<XElement> deviceset = devices.Elements(MTConnectNameSpace.mtDevices + "Device");
                XElement device = null;
                foreach (XElement de in deviceset)
                {
                       if (de.Attribute("name").Value == deviceId)
                        device=de;
                }
                if (device == null)
                {
                    Error.createError(data, Error.NO_DEVICE).Save(writer);
                    return null;
                }
                else
                    return device;
            }
            else //Devices were not found from Devices.xml load at initialization
            {
                Error.createError(data, Error.INTERNAL_ERROR).Save(writer);
                return null;
            }
        }
      
        internal static String[] return_INVALID_URI_Error(Data data, StreamWriter writer, String extra_info)
        {
            Error.createError(data, Error.INVALID_URI, extra_info).Save(writer);
            return null;
        }

        //Remove ComponentStream if its Samples or Events have no child elements
        internal static XElement trimStream(XElement _s, String deviceId, XmlNamespaceManager namespaceManager)//changed
        {
            IEnumerable<XElement> componentStreams = _s.Descendants(MTConnectNameSpace.mtStreams + "ComponentStream");
           

            for (int i = 0; i < componentStreams.Count(); i++)
            {
                int s_counter = 0;
                int e_counter = 0;
                int c_counter = 0;
                XElement cs = componentStreams.ElementAt(i);
                XElement samples = cs.Element(MTConnectNameSpace.mtStreams+"Samples");
                if (samples != null)
                    s_counter = samples.Elements().Count();
                XElement events = cs.Element(MTConnectNameSpace.mtStreams+"Events");
                if (events != null)
                    e_counter = events.Elements().Count();
                XElement condition=cs.Element(MTConnectNameSpace.mtStreams+"Condition");
                if (condition != null)
                    c_counter = condition.Elements().Count();
                if (s_counter == 0 && samples!=null)
                    samples.Remove();
                if (e_counter == 0 && events!=null)
                    events.Remove();
                if (c_counter == 0 && condition!=null)
                    condition.Remove();
                if (s_counter == 0 && e_counter == 0 && c_counter==0)
                {
                    cs.Remove();
                    i--;
                   
                }
                
            }

            IEnumerable<XElement> devices = _s.Descendants(MTConnectNameSpace.mtStreams + "DeviceStream");
            if (deviceId != null)
            {
                for (int i = 0; i < devices.Count(); i++)
                {
                    XElement device = devices.ElementAt(i);
                    if (device.Attribute("name").Value != deviceId)
                    {
                        device.Remove();
                        i--;

                    }
                    
                }
            }
            else
            {
               
            }
            return _s;
        }

        internal static String getDataElementName(String _type)
        {
            //POSITION_XXX to Position_Xxx
            String name = "";
            String[] parts = _type.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                name += (parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1).ToLower());
            }
            return name;
        }  
        
    
        
        //
        internal static XElement getDataItemFromId(XElement _datastorage, String _id, XmlNamespaceManager namespaceManager)
        {
          
            XElement result = null;
           
            IEnumerable<XElement> dataItems = _datastorage.Descendants("DataItem");
          
           foreach (XElement dataItem in dataItems)
           {
               if(dataItem.Attribute("id").Value==_id)
                   result=dataItem;
           }
           return result;          
          
        }
        //

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName,xmlDocument.Attributes());
                xElement.Value = xmlDocument.Value;
                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Attributes(),xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }


        internal static XElement createDataStorage(XElement _probe, XmlNamespaceManager namespaceManager, out long sequence, out int bufferSizeCounter, out long minAtIndex, out int realbuffersize, out long firstSequence, out long lastSequence, int buffersize)//changed
        {
            sequence = 1;
            bufferSizeCounter = 0;
            
            XElement devices =new XElement(_probe.Element(MTConnectNameSpace.mtDevices+"Devices"));
           //XElement devices = _probe.Element(MTConnectNameSpace.mtDevices + "Devices");
           
            IEnumerable<XElement> dataItems =devices.Descendants(MTConnectNameSpace.mtDevices+"DataItem");
           
           
           foreach (XElement dataItem in dataItems)
            {
                if (!dataItem.HasElements)
                {
                    XElement InitialData = new XElement(MTConnectNameSpace.mtDevices+"Data", new XAttribute("timestamp", Util.GetDateTime()), new XAttribute("sequence", sequence.ToString()));
                    if (dataItem.Attribute("category").Value=="CONDITION")
                    { 
                        InitialData.Add(new XAttribute("condition","Unavailable"));
                    }
                    if (dataItem.Attribute("type").Value == "ALARM")
                    {
                        InitialData.Add(new XAttribute("code", "OTHER"), new XAttribute("nativeCode", "UNAVAILABLE"));
                    
                    }
                    InitialData.SetValue("UNAVAILABLE");
                    dataItem.Add(InitialData);
                        sequence++;
                        bufferSizeCounter++;
                }
                else if (dataItem.Element(MTConnectNameSpace.mtDevices + "Constraints").Elements().Count() == 1)
                {
                    XElement InitialData = new XElement(MTConnectNameSpace.mtDevices + "Data", new XAttribute("timestamp", Util.GetDateTime()), new XAttribute("sequence", sequence.ToString()));
                    if (dataItem.Attribute("category").Value == "CONDITION")
                    {
                        InitialData.Add(new XAttribute("condition", "Unavailable"));
                    }
                    if (dataItem.Attribute("type").Value == "ALARM")
                    {
                        InitialData.Add(new XAttribute("code", "OTHER"), new XAttribute("nativeCode", "UNAVAILABLE"));

                    }
                    InitialData.SetValue(dataItem.Element(MTConnectNameSpace.mtDevices + "Constraints").Element(MTConnectNameSpace.mtDevices + "Value").Value);
                    dataItem.Add(InitialData);
                    sequence++;
                    bufferSizeCounter++;
                }
                else if (dataItem.Element(MTConnectNameSpace.mtDevices + "Constraints").Elements().Count() > 1)
                {
                    XElement InitialData = new XElement(MTConnectNameSpace.mtDevices + "Data", new XAttribute("timestamp", Util.GetDateTime()), new XAttribute("sequence", sequence.ToString()));
                    if (dataItem.Attribute("category").Value == "CONDITION")
                    {
                        InitialData.Add(new XAttribute("condition", "Unavailable"));
                    }
                    if (dataItem.Attribute("type").Value == "ALARM")
                    {
                        InitialData.Add(new XAttribute("code", "OTHER"), new XAttribute("nativeCode", "UNAVAILABLE"));

                    }
                    InitialData.SetValue("UNAVAILABLE");
                    dataItem.Add(InitialData);
                    sequence++;
                    bufferSizeCounter++;
                }
            }
            //realbuffersize = buffersize + bufferSizeCounter - 1;
            realbuffersize = buffersize + bufferSizeCounter;
            minAtIndex = bufferSizeCounter;
            //firstSequence = minAtIndex;
            firstSequence = minAtIndex+1;
            //lastSequence = minAtIndex;
            lastSequence = minAtIndex;

            devices = DataUtil.RemoveAllNamespaces(devices);
            //XElement devices = _probe.Element(MTConnectNameSpace.mtDevices+"Devices");
            return devices;
            //return _probe.Element(MTConnectNameSpace.mtDevices + "Devices");
        }

        internal static void handleComponent(ref XElement component, ref long sequence,ref int bufferSizeCounter, XmlNamespaceManager namespaceManager)
        {
            IEnumerable<XElement> dataItems = component.Element(MTConnectNameSpace.mtDevices + "DataItems").Elements(MTConnectNameSpace.mtDevices + "DataItem");
            if (dataItems.Count() > 0)
            {
                foreach (XElement da in dataItems)
                {
                    XElement InitialData = new XElement(MTConnectNameSpace.mtDevices + "Data", new XAttribute("timestamp", Util.GetDateTime()), new XAttribute("sequence", sequence.ToString()));
                    if (da.Attribute("category").Value == "CONDITION")
                    {
                        InitialData.Add(new XAttribute("condition", "Unavailable"));
                    }
                    InitialData.SetValue("UNAVAILABLE");
                    da.Add(InitialData);
                    sequence++;
                    bufferSizeCounter++;
                }
            }
            IEnumerable<XElement> components = component.Element(MTConnectNameSpace.mtDevices + "Components").Elements();
            if (components.Count() > 0)
            {
                foreach (XElement subcomponent in components)
                {
                    XElement component2 = subcomponent;
                    handleComponent(ref component2, ref sequence, ref bufferSizeCounter, namespaceManager);


                }
            }

        }
        
        internal static XElement createStreams(XElement _probe, XmlNamespaceManager namespaceManager)
        {
            XElement streams = new XElement(MTConnectNameSpace.mtStreams+"Streams");
            IEnumerable<XElement> devices = _probe.Element(MTConnectNameSpace.mtDevices+"Devices").Elements(MTConnectNameSpace.mtDevices+"Device");
            foreach (XElement d in devices)
            {
                String devicename = d.Attribute("name").Value;
                String uuid = ""; //uuid is optional as of 7/1/08
                XAttribute temp;
                if ((temp = d.Attribute("uuid")) != null)
                    uuid = temp.Value;

                XElement devicestream = new XElement(MTConnectNameSpace.mtStreams+"DeviceStream", new XAttribute("name", devicename), new XAttribute("uuid", uuid));
                devicestream.Add(DataUtil.CreateComponentStream(d));
                handleComponents(ref devicestream, d, namespaceManager);
                streams.Add(devicestream);
            }
            return streams;
        }

        //create <ComponentStream> if <Components>'s child element is not <DataItems>
        internal static void handleComponents(ref XElement _devicestream, XElement device, XmlNamespaceManager namespaceManager)
        {
            IEnumerable<XElement> componentsList = device.Elements(MTConnectNameSpace.mtDevices+"Components");
            foreach (XElement componentsElement in componentsList)
            {
                foreach (XElement ctemp in componentsElement.Elements())
                {
                    String ename = ctemp.Name.LocalName;
                    if (!ename.Equals("DataItems") && !ename.Equals("Description"))
                    {
                        _devicestream.Add(DataUtil.CreateComponentStream(ctemp)); //Power, Axes, Controller
                        handleComponents(ref _devicestream, ctemp, namespaceManager);
                    }
                }
            }
            
        }
      
        /*
         * 1st upper and least lower
         */
        internal static String modifyString1(String _s)
        {
            return _s.Substring(0, 1).ToUpper() + _s.Substring(1).ToLower();
        }

        internal static XElement CreateComponentStream(XElement _ele)
        {
            XElement cs = new XElement(MTConnectNameSpace.mtStreams+"ComponentStream", new XAttribute("component", _ele.Name.LocalName), new XAttribute("name", _ele.Attribute("name").Value), new XAttribute("componentId", _ele.Attribute("id").Value));
            ArrayList categories = GetDataItemCategories(_ele);
            foreach (String s in categories)
            {
                cs.Add(new XElement(MTConnectNameSpace.mtStreams+s)); //<Samples> or <Events>or <Condition> in <ComponentStream>
            }
            return cs;
        }

        internal static String getCategoryElementName(String _category)
        {
            String c_lower = _category.ToLower();
            if (c_lower.Equals("sample"))
                return "Samples";
            else if (c_lower.Equals("event"))
                return "Events";
            //
            else if (c_lower.Equals("condition"))
                return "Condition";

            else
                return null;
        }

        internal static ArrayList getDataItemTypeAndCategory(IEnumerable<XElement> cdataitems, String _nameToMatch)
        {
            ArrayList list = new ArrayList();
            foreach (XElement di in cdataitems)
            {
                String dname = di.Attribute("name").Value;
                if (dname.Equals(_nameToMatch))
                {
                    list.Add(di.Attribute("type").Value); // to be element name
                    list.Add(di.Attribute("category").Value); // to put under
                }
            }
            return list;
        }

        internal static ArrayList GetDataItemCategories(XElement _ele)
        {
            ArrayList list = new ArrayList();
            bool hasSample = false;
            bool hasEvent=false;
            bool hasCondition=false;
            XElement dataitemsEle = _ele.Element(MTConnectNameSpace.mtDevices + "DataItems");
            if (dataitemsEle != null)
            {
                IEnumerable<XElement> dataitems = dataitemsEle.Elements(MTConnectNameSpace.mtDevices + "DataItem");
                foreach (XElement di in dataitems)
                {
                    String category = di.Attribute("category").Value;
                    if (category.Equals("SAMPLE") )
                    {
                        hasSample = true;
                    }
                    else if (category.Equals("EVENT"))
                    {
                        hasEvent = true;
                    }
                    else if (category.Equals("CONDITION"))
                    {
                        hasCondition = true;
                    }
                }
            }
            if (hasSample == true)
                list.Add("Samples");
            if (hasEvent == true)
                list.Add("Events");
            if (hasCondition == true)
                list.Add("Condition");
            return list;
        }
        
        internal static XElement createEventData(String _timestamp, String _sequence, String _value, String _code, String _nativeCode)
        {
            XElement re = new XElement("Data", new XAttribute("timestamp", _timestamp), new XAttribute("sequence", _sequence));
            if (_code != null)
                re.Add(new XAttribute("code", _code));
            if (_nativeCode != null)
                re.Add(new XAttribute("nativeCode", _nativeCode));
            re.SetValue(_value);
            return re;
        }
        
        internal static XElement createSampleData(String _timestamp, String _value, String _sequence)
        {
            XElement re = new XElement("Data", new XAttribute("timestamp", _timestamp), new XAttribute("sequence", _sequence));            
            re.SetValue(_value);
            return re;
        }

        //
        internal static XElement createConditionData(String _timestamp, String _value, String _condition, String _sequence, String _nativeCode, String _code)
        {
            XElement re = new XElement("Data", new XAttribute("timestamp", _timestamp), new XAttribute("sequence", _sequence), new XAttribute("condition", _condition));
            if (_nativeCode != null)
                re.Add(new XAttribute("nativeCode", _nativeCode));
            if (_code != null)
                re.Add(new XAttribute("code", _code));
            if(_value!=null)
            re.SetValue(_value);
            return re;
        }
        //
        internal static XElement createData(String _dataitem_type, String _dataitem_name, String _dataitem_subType, String _dataItem_id, XElement data, String _dataItem_category, String _dataItem_units)
        {
            String eleName = DataUtil.getDataElementName(_dataitem_type); //change POSITION to Position
            String timestamp = data.Attribute("timestamp").Value;
            String sequence = data.Attribute("sequence").Value;
            String category=_dataItem_category;
            XElement re;
            if (category == "CONDITION")
            {
                re = new XElement(MTConnectNameSpace.mtStreams + data.Attribute("condition").Value, new XAttribute("dataItemId", _dataItem_id), new XAttribute("type", _dataitem_type), new XAttribute("timestamp", timestamp), new XAttribute("sequence", sequence));
                //re = new XElement(data.Attribute("condition").Value, new XAttribute("dataItemId", _dataItem_id), new XAttribute("type", _dataitem_type), new XAttribute("timestamp", timestamp), new XAttribute("sequence", sequence));
            }
            else
            {
                 re= new XElement(MTConnectNameSpace.mtStreams + eleName,new XAttribute("dataItemId", _dataItem_id), new XAttribute("timestamp", timestamp), new XAttribute("sequence", sequence));
                //re = new XElement(eleName, new XAttribute("dataItemId", _dataItem_id), new XAttribute("timestamp", timestamp), new XAttribute("sequence", sequence));
            }
            if (data.Attribute("code") != null)
                re.Add(new XAttribute("code",data.Attribute("code").Value));
            if (data.Attribute("nativeCode") != null)
                re.Add(new XAttribute("nativeCode", data.Attribute("nativeCode").Value));
            if (_dataitem_name!=null)
                re.Add(new XAttribute("name", _dataitem_name));            
            if (_dataitem_subType != null)
                re.Add(new XAttribute("subType", _dataitem_subType));
            if (_dataItem_units != null)
                re.Add(new XAttribute("units", _dataItem_units));
            if (data.Value != null)
            re.SetValue(data.Value);
            return re;
        }
         
        // get Device element's name value recursively
        internal static String getDeviceName(XElement _ele)
        {
            //TODO: assumption Device's name is required
            
            XElement p = _ele.Parent;
            if (p.Name.LocalName.Equals("Device"))
                return p.Attribute("name").Value;
            else
                return getDeviceName(p);
        }

    }
}
