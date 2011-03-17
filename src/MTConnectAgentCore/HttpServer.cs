

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Web;

namespace MTConnectAgentCore
{
    internal delegate void delReceiveWebRequest(HttpListenerContext Context);

    internal class HttpServer
    {
        private HttpListener Listener;
        private bool IsStarted = false;
        internal event delReceiveWebRequest ReceiveWebRequest;
        private Data sharedData;

        internal HttpServer(Data _shared)
        {
            sharedData = _shared;
        }

        internal void Start()
        {
            if (this.IsStarted)
                return;
            if (this.Listener == null)
            {
                this.Listener = new HttpListener();
            }
            this.Listener.Prefixes.Add("http://+:80/");
            this.IsStarted = true;
            try
            {
                this.Listener.Start();
            }
            catch (System.Net.HttpListenerException)
            {
                throw new AgentException("Agent Start Failed.  Please make sure port 80 is available for Agent.");
            }
            IAsyncResult result = this.Listener.BeginGetContext(new AsyncCallback(WebRequestCallback), this.Listener);
        }

        internal virtual void Stop()
        {
            if (Listener != null)
            {
                this.Listener.Close();
                this.Listener = null;
                this.IsStarted = false;
            }
        }

        private void WebRequestCallback(IAsyncResult result)
        {
            if (this.Listener == null)
                return;
            try
            {
                // Get out the context object
                HttpListenerContext context = this.Listener.EndGetContext(result);
                //setup a new context for the next request
                this.Listener.BeginGetContext(new AsyncCallback(WebRequestCallback), this.Listener);
                if (this.ReceiveWebRequest != null)
                    this.ReceiveWebRequest(context);

                this.ProcessRequest(context);
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;

            }
            catch (Exception e)
            {
                if (this.Listener == null) //it seems when Stop is called this.listener.EndGetContext will throw NullPointException
                    return;
                else
                {
                    // Obtain a response object.
                    HttpListenerResponse response = this.Listener.GetContext().Response;
                    response.StatusCode = 500;
                    throw e;
                }
            }
        } //End of WebRequestCallback

        private void ProcessRequest(System.Net.HttpListenerContext Context)
        {
            HttpListenerRequest request = Context.Request;
            HttpListenerResponse response = Context.Response;
            //front support returning xml to Http client
            response.ContentType = "text/xml";
            //Construct a response
            response.StatusCode = 200;
            StreamWriter writer = new StreamWriter(response.OutputStream, System.Text.UTF8Encoding.UTF8);
            try
            {
                short rv = process2(request, writer);
                if (rv == ReturnValue.ERROR)
                {
                    response.StatusCode = 500;
                }
            }
            catch (System.Xml.XPath.XPathException e)  
            {
                //when XDocument.XPathSelectElements() throws XPathException but not when a client send path to query
                Error.createError(sharedData, Error.INTERNAL_ERROR, e.Message).Save(writer);
            }
            writer.Close();
        }

        private short createInvalidRequestError(StreamWriter writer, String extra)
        {
            if (extra == null)
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
            else
                Error.createError(sharedData, Error.INVALID_REQUEST, extra).Save(writer);
            return ReturnValue.ERROR;
        }

        private short process2(HttpListenerRequest request, StreamWriter writer)
        {
            if ( request.RawUrl.Equals("/")){
                (new XElement(MTConnectNameSpace.mtStreams+"MTConnectAgent", new XAttribute("state", "RUNNING"))).Save(writer);
                return ReturnValue.ERROR;
            }
            //segument[0] = / segumens[1] = sample, for http:/127.0.0.1/sample?path= is 
            String[] seguments = request.Url.Segments; // {/,devicename,sample

            if (seguments[0].StartsWith("/") == false)
                return createInvalidRequestError(writer, null);

            if (seguments.Length == 2) //http://<IP>/current or http://<IP>/current?path=...
            {
                String[] keys = request.QueryString.AllKeys;
                String path = seguments[1];
                if (keys.Length == 0)
                {
                    switch (path)//http://<IP>/current
                    {
                        case "probe":
                            return sharedData.getProbe(writer);
                        case "current":
                            return sharedData.getCurrent(writer);
                        case "sample":
                            return sharedData.getStream(writer);
                        case "debug":
                            return sharedData.getDebug(writer);
                        //DAF 2008-07-31 Added 
                        case "version":
                            return sharedData.getVersion(writer);
                        case "log":
                            return sharedData.getLog(writer);
                        case "config":
                            return sharedData.getConfig(writer);
                        //DAF 2008-07-31 End 
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
                else
                {
                    switch (path) //http://<IP>/current?path=...
                    {
                        case "storeSample":
                            return handleStoreSample(request.QueryString, writer);
                        case "storeEvent":
                            return handleStoreEvent(request.QueryString, writer);
                        case "storeCondition":
                            return handleStoreCondition(request.QueryString, writer);
                        case "current":
                            return handleCurrent(request.QueryString, null, writer);
                        case "sample":
                            return handleSample(request.QueryString, null, writer);
                        case "probe":
                            return sharedData.getProbe(writer);
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
            }
            else if (seguments.Length == 3) //http://<IP>/deviceName/current? => s[0] = "/" s[1] = "deviceName/", s[2] = current
            {
                String deviceName = null;
                if (!seguments[1].EndsWith("/"))
                    return createInvalidRequestError(writer, null);
                else
                    deviceName = seguments[1].Substring(0, seguments[1].Length - 1);

                String[] keys = request.QueryString.AllKeys;
                String path = seguments[2];
                if (keys.Length == 0)
                {
                    switch (path)
                    {
                        case "probe":
                            return sharedData.getProbeDevice(deviceName, writer);
                        case "current":
                            return sharedData.getCurrentDevice(deviceName, writer);
                        case "sample":
                            return sharedData.getStreamDevice(deviceName, writer);
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
                else
                {
                    switch (path)
                    {
                        case "current":
                            return handleCurrent(request.QueryString, deviceName, writer);
                        case "sample":
                            return handleSample(request.QueryString, deviceName, writer);
                        case "probe":
                            return sharedData.getProbeDevice(deviceName, writer);
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
            }
            else
            {
                return createInvalidRequestError(writer, null);
            }
        } // end of process2

        private short handleSample(System.Collections.Specialized.NameValueCollection queryString, String deviceName, StreamWriter writer)
        {
            String path, from, count, at, frequency;
            bool success = handleUrlRequest(queryString, out path, out from, out count, out at, out frequency);
            if (success == false)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
                return ReturnValue.ERROR;
            }
            else if (at != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"at\" parameter can not be in the Sample Request.").Save(writer);
                return ReturnValue.ERROR;
            }

            else
                if (deviceName == null)
                    return sharedData.getStream(path, from, count, writer, frequency);
                else
                    return sharedData.getStreamDevice(deviceName, path, from, count, writer, frequency);
        } // End of handleSample

        private short handleCurrent(System.Collections.Specialized.NameValueCollection queryString, String deviceName, StreamWriter writer)
        {
            String path, from, count, at, frequency;
            bool success = handleUrlRequest(queryString, out path, out from, out count,out at, out frequency);
            if (success == false)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
                return ReturnValue.ERROR;
            }            
            else if (from != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"from\" parameter can not be in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
            else if (count != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"count\" parameter can not be in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
           /* else if (path == null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"path\" parameter is not specified in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }*/
            else if (at != null && frequency != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"at\"  parameter must not be used with \"frequency\" in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
            else
            {
                if (deviceName == null)
                    return sharedData.getCurrent(path, writer, at, frequency);
                else
                    return sharedData.getCurrentDevice(deviceName, path, writer, at, frequency);
            }
        }

        //return error XElement
        //return null for success
        private bool handleUrlRequest(System.Collections.Specialized.NameValueCollection queryString, out String path, out String from, out String count,out String at, out String frequency )
        {
            //set default
            path = null;
            from = null;
            count = null;
            at = null;
            frequency = null;
            String[] keys = queryString.AllKeys;
            if (keys.Length == 0)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                String[] values = queryString.GetValues(keys[i]);
                switch (keys[i])
                {
                    case "path":
                        path = values[0];
                        break;
                    case "from":
                        from = values[0];
                        break;
                    case "count":
                        count = values[0];
                        break;
                    case "at":
                        at = values[0];
                        break;
                    case "frequency":
                        frequency = values[0];
                        break;
                    default:
                        return false;
                }
            } 
            return true;
        }

        private XElement handleStoreSampleStoreEventCommon(System.Collections.Specialized.NameValueCollection queryString,
            out String timestamp, out String dataItemId, out String condition, out String value,out String code, out String nativeCode )
        {
            timestamp = null;
            
            
            dataItemId = null;
            condition = null;
            value = null;
            code = null;
            nativeCode = null;
            

            // Get names of all keys into a string array.
            String[] keys = queryString.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                String[] values = queryString.GetValues(keys[i]);
                switch (keys[i])
                {
                    case "timestamp":
                        timestamp = values[0];
                        break;
                    
                    
                    case "dataItemId":
                        dataItemId = values[0];
                        break;
                    case "condition":
                        condition = values[0];
                        break;
                    case "value":
                        value = values[0];
                        break;
                    case "code":
                        code = values[0];
                        break;
                    case "nativeCode":
                        nativeCode = values[0];
                        break;                  
                   
                }
            }
            if (timestamp == null)
                // return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The timestamp is missing.");
                timestamp = Util.GetDateTime();
            //else if (deviceName == null)
               // return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The deviceName is missing.");
            if (dataItemId ==null)
                return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The dataItemId is missing.");
            //else if (value == null)
              //  return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The value is missing.");
            else
                return null; //success
        }

        //short StoreSample(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId);
        private short handleStoreSample(System.Collections.Specialized.NameValueCollection queryString, StreamWriter writer)
        {
            short storeSampleReturn = ReturnValue.ERROR; //default
            XElement returnElement;
            String timestamp, dataItemId, condition, value,code,nativeCode;
            if ((returnElement = handleStoreSampleStoreEventCommon(queryString, out timestamp, out dataItemId, out condition, out value, out code, out nativeCode)) == null)
            {
                if (value == null)
                    returnElement= MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The value is missing.");
                else if (condition != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not define \"condition\" .");
                else if(code != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not define \"code\" .");
                else if (nativeCode != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not define \"nativeCode\" .");

                else
                {
                    try
                    {
                        storeSampleReturn = sharedData.StoreSample(timestamp, dataItemId, value);

                        if (storeSampleReturn == ReturnValue.SUCCESS)
                            returnElement = new XElement(MTConnectNameSpace.mtStreams+"Acknowledge", new XAttribute("dateTime", Util.GetDateTime()));
                        else //not currently used
                            returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA);
                    }
                    catch (AgentException e)
                    {
                        returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, e.Message);
                    }
                }
            }
            returnElement.Save(writer);
            return storeSampleReturn;
        }

        // short StoreEvent(String timestamp, String deviceName, String dataItemId, String value);
        private short handleStoreEvent(System.Collections.Specialized.NameValueCollection queryString, StreamWriter writer)
        {
            short storeEventReturn = ReturnValue.ERROR; //default
            XElement returnElement;
            String timestamp, dataItemId, condition, value,code, nativeCode ;
            if ((returnElement = handleStoreSampleStoreEventCommon(queryString, out timestamp, out dataItemId, out condition, out value, out code, out nativeCode)) == null)
            {
                if (value == null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The value is missing.");
                else if (condition != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeEvent should not define \"condition\".");
                
                else
                {
                    try
                    {
                        storeEventReturn = sharedData.StoreEvent(timestamp, dataItemId, value,code, nativeCode);

                        if (storeEventReturn == ReturnValue.SUCCESS)
                            returnElement = new XElement(MTConnectNameSpace.mtStreams+"Acknowledge", new XAttribute("dateTime", Util.GetDateTime()));
                        else //not possible
                            returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA);
                    }
                    catch (AgentException e)
                    {
                        returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, e.Message);
                    }
                }
            }
            returnElement.Save(writer);
            return storeEventReturn;
        }

        private short handleStoreCondition(System.Collections.Specialized.NameValueCollection queryString, StreamWriter writer)
        {
            short storeConditionReturn = ReturnValue.ERROR; //default
            XElement returnElement;
            String timestamp,  dataItemId, condition, value,code, nativeCode;
            if ((returnElement = handleStoreSampleStoreEventCommon(queryString, out timestamp,  out dataItemId, out condition, out value, out code, out nativeCode)) == null)
            {
                 
                 if (condition == null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeCondition should define \"condition\".");
                 
                 
                 else if (!Util.CheckTypesOfCondition(condition))
                     returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The condition is not one of the four recognizable condition types.");
                 else
                 {
                     try
                     {
                         storeConditionReturn = sharedData.StoreCondition(timestamp, dataItemId, condition, value, nativeCode,code);

                         if (storeConditionReturn == ReturnValue.SUCCESS)
                             returnElement = new XElement(MTConnectNameSpace.mtStreams+"Acknowledge", new XAttribute("dateTime", Util.GetDateTime()));
                         else //not possible
                             returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA);
                     }
                     catch (AgentException e)
                     {
                         returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, e.Message);
                     }
                 }
            }
            returnElement.Save(writer);
            return storeConditionReturn;
        }
    }
}
